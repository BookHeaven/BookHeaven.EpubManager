using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.EpubManager.Epub.XML;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace BookHeaven.EpubManager.Epub.Services;

public interface IEpubReader : IDisposable
{
	Task<EpubBook> ReadMetadataAsync(string path);
	Task<EpubBook> ReadAllAsync(string path);
	Task<string> ApplyHtmlProcessingAsync(string content);
}


public partial class EpubReader : IEpubReader
{
	private ZipArchive? _zipArchive;
	private SemaphoreSlim? _zipLock;
	
	private Package? _package;
	private string? _rootFolder;
	private readonly char[] _separator = [' ', '\n', '\r', '\t'];
	private string? _coverPath;
	
	private readonly ConcurrentDictionary<string, string> _contentCache = new();
	private readonly ConcurrentDictionary<string, byte[]> _imageCache = new();

	private readonly Dictionary<Type, XmlSerializer> _serializers = [];
	
	private enum CssEditMode
	{
		Replace,
		Add,
		Max,
		Remove,
		ReplaceProperty
	}

	private readonly List<CssProperty> _customStyles =
	[
		new() { Property = "line-height", CssVariable= "var(--line-height)", CssUnit = "em", Mode = CssEditMode.Add },
		new() { Property = "text-indent", CssVariable= "var(--text-indent)", CssUnit = "em", Mode = CssEditMode.Replace },
		new() { Property = "margin-top", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
		new() { Property = "margin-bottom", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
		new() { Property = "margin", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
		new() { Property = "font-size", CssVariable= "1", CssUnit = "em", Mode = CssEditMode.Max },
		new() { Property = "font-family", Mode = CssEditMode.Remove },
		new() { Property = "widows", Mode = CssEditMode.Remove },
		new() { Property = "orphans", Mode = CssEditMode.Remove },
		new() { Property = "padding-top", NewProperty = "margin-top",Mode = CssEditMode.ReplaceProperty},
		new() { Property = "padding-bottom", NewProperty = "margin-bottom",Mode = CssEditMode.ReplaceProperty}
	];
	
	private class CssProperty
	{
		public string Property { get; init; } = null!;
		public string NewProperty { get; init; } = null!;
		public string? CssVariable { get; init; }
		public string? CssUnit { get; init; }
		public CssEditMode Mode { get; init; }
	}

	public EpubReader()
	{
		_serializers[typeof(Package)] = new XmlSerializer(typeof(Package));
		_serializers[typeof(Container)] = new XmlSerializer(typeof(Container));
		_serializers[typeof(NCX)] = new XmlSerializer(typeof(NCX));
		_serializers[typeof(Nav)] = new XmlSerializer(typeof(Nav));
	}

	public async Task<EpubBook> ReadMetadataAsync(string path)
	{
		return await ReadAsync(path);
	}
	
	public async Task<EpubBook> ReadAllAsync(string path)
	{
		return await ReadAsync(path, false);
	} 
	

	/// <summary>
	/// Reads the contents of an epub file. Already calls LoadEpub.
	/// </summary>
	/// <param name="path">Physical File path</param>
	/// <param name="metadataOnly">Whether to only retrieve metadata or the contents as well. True by default.</param>
	/// <returns></returns>
	private async Task<EpubBook> ReadAsync(string path, bool metadataOnly = true)
	{
		var book = new EpubBook
		{
			FilePath = path
		};
		
		var packagePath = await GetOpfPathAsync(path);

		_rootFolder = Path.GetDirectoryName(packagePath)!;
		book.RootFolder = _rootFolder;

		_package = await ReadEntryAsync<Package>(packagePath);

		book.Cover = await LoadCoverImageAsBytesAsync();
		book.Metadata = MapMetadata(_package.Metadata);

		if (!metadataOnly)
		{
			// Load content (spine and chapters)
			book.Content = await LoadContent();
		}
		else
		{
			Dispose();
		}
			
		return book;
	}

	/// <summary>
	/// Gets the path to the OPF file inside the epub
	/// </summary>
	/// <returns>OPF path</returns>
	public async Task<string> GetOpfPathAsync(string epubPath)
	{
		_zipArchive = ZipFile.OpenRead(epubPath);
		_zipLock = new(1, 1);
		var container = await ReadEntryAsync<Container>("META-INF/container.xml");
		var rootFile = container.RootFiles.RootFile.First();
		return rootFile.FullPath;
	}

	private XmlSerializer GetSerializer<T>()
	{
		var type = typeof(T);
		if (!_serializers.TryGetValue(type, out var value))
		{
			value = new XmlSerializer(type);
			_serializers[type] = value;
		}
		return value;
	}

	/// <summary>
	/// Returns the absolute path of a file inside the epub
	/// </summary>
	/// <param name="path">Relative path</param>
	/// <returns>Absolute path</returns>
	private string? GetAbsolutePath(string? path)
	{
		if(string.IsNullOrEmpty(path))
		{
			return null;
		}

		if (path.IndexOf("../", StringComparison.Ordinal) >= 0)
		{
			path = path.Replace("../", "");
		}

		if (!string.IsNullOrEmpty(_rootFolder) && !path.StartsWith(_rootFolder))
		{
			path = _rootFolder + "/" + path;
		}

		return path;
	}

	/// <summary>
	/// Deserializes an entry from the epub file
	/// </summary>
	/// <typeparam name="T">Entry Type</typeparam>
	/// <param name="path">File path inside the epub</param>
	/// <returns>Object of type T</returns>
	/// <exception cref="Exception"></exception>
	private async Task<T> ReadEntryAsync<T>(string path)
	{
		var entry = _zipArchive!.GetEntry(GetAbsolutePath(path)!) ?? throw new Exception($"File not found inside epub. {GetAbsolutePath(path)}");

		await using var stream = entry.Open();
		var serializer = GetSerializer<T>();
		try
		{
			// Configure XML reader settings to enable DTD processing
			var readerSettings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse
			};

			using var reader = XmlReader.Create(stream, readerSettings);
			return (T)serializer.Deserialize(reader)!;
		}
		catch (Exception e)
		{
			throw new Exception("Error deserializing entry", e);
		}
	}

	/// <summary>
	/// Loads the cover image from the epub
	/// </summary>
	/// <returns>Image as bytes</returns>
	private async Task<byte[]?> LoadCoverImageAsBytesAsync()
	{
		var cover = 
			_package!.Manifest.Items.FirstOrDefault(item => item.Id == _package!.Metadata.Meta.FirstOrDefault(x => x.Name == "cover")?.Content) 
			?? _package.Manifest.Items.FirstOrDefault(x => x.Properties == "cover-image");
		if(cover is null)
		{
			throw new Exception("Cover not found");
		}

		return await LoadImageAsBytes(cover.Href);
	}

	/// <summary>
	/// Converts an image to bytes
	/// </summary>
	/// <param name="path">Path inside the epub</param>
	/// <returns>Image as bytes</returns>
	private async Task<byte[]> LoadImageAsBytes(string path)
	{
		if (_zipLock is null)
		{
			throw new Exception("EpubReader not initialized. Call GetOpfPathAsync first.");
		}
		
		var absolutePath = GetAbsolutePath(path)!;
    
		if (_imageCache.TryGetValue(absolutePath, out var cachedImage))
		{
			return cachedImage;
		}
		await _zipLock.WaitAsync();
		try
		{
			var entry = _zipArchive!.GetEntry(absolutePath);
			if (entry == null) return [];

			await using var stream = entry.Open();
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);
			var bytes = memory.ToArray();
			_imageCache[absolutePath] = bytes;
			return bytes;
		}
		finally
		{
			_zipLock.Release();
		}
	}

	/// <summary>
	/// Maps the metadata from the epub to the EpubMetadata object
	/// </summary>
	/// <param name="metadata">Metadata from epub</param>
	/// <returns></returns>
	private EpubMetadata MapMetadata(Metadata metadata)
	{
		return new EpubMetadata
		{
			Title = metadata.Titles.First(x => !string.IsNullOrEmpty(x)),
			Language = metadata.Languages.First(x => !string.IsNullOrEmpty(x)),
			Identifiers = metadata.Identifiers.Select(x => new EpubIdentifier { Scheme = x.Scheme, Value = x.Value }).ToList(),
			Authors = metadata.Creators!.Select(x => x.Name).ToList(),
			Publisher = metadata.Publishers!.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
			PublishDate = metadata.Dates!.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
			Rights = metadata.Rights!.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
			Subject = metadata.Subjects!.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
			Description = metadata.Descriptions!.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
			Series = metadata.Meta.FirstOrDefault(x => x.Name == "calibre:series")?.Content,
			SeriesIndex = decimal.TryParse(metadata.Meta.FirstOrDefault(x => x.Name == "calibre:series_index")?.Content, CultureInfo.InvariantCulture, out var index) ? index : null
		};
	}

	/// <summary>
	/// Loads the content of the epub, which includes both the Spine (index) and the chapters
	/// </summary>
	/// <returns></returns>
	private async Task<Content> LoadContent()
	{
		var content = new Content();
			
		var cssFiles = _package!.Manifest.Items.Where(x => x.MediaType.Equals("text/css")).ToList();
		var cssTasks = cssFiles.Select(async item =>
		{
			StringBuilder cssContent = new();
			var css = await LoadFileContentAsync(item.Href);
			cssContent.Append(css);
			var imports = CssImportRegex().Matches(css);
			foreach (var import in imports.Cast<Match>())
			{
				cssContent.Replace(import.Value, null);
			}
			var fontFaces = FontFaceRegex().Matches(css);
			foreach (var fontFace in fontFaces.Cast<Match>())
			{
				cssContent.Replace(fontFace.Value, null);
			}
			await ReplaceCssProperties(cssContent);
			return new Style { Name= item.Href, Content = cssContent.ToString()};
		});

		content.Styles = await Task.WhenAll(cssTasks);
			
		List<EpubChapter> chapters;
		EpubChapter? cover = null;
		var coverItem = _package!.Manifest.Items.FirstOrDefault(x => x.Id == _package.Spine.ItemRefs.FirstOrDefault()?.IdRef);
		if (coverItem != null)
		{
			_coverPath = coverItem.Href;
			cover = new()
			{
				ItemId = coverItem.Id,
				Title = "Cover",
			};
		}
			
		content.Spine = await MapSpineToSpineItemList();
			
			
		if (_package.Manifest.Items.Any(i => i.Properties == "nav"))
		{
			// V3 NAV TOC
			var nav = await LoadNavAsync(_package.Manifest.Items.First(i => i.Properties == "nav").Href);
			chapters = await MapNavToEpubChapters(nav.ChapterList.First().Chapter);
		}
		else if (_package!.Spine.Toc != null)
		{
			// V2 NCX TOC
			var ncx = await ReadEntryAsync<NCX>(_package.Manifest.Items.First(x => x.Id == _package.Spine.Toc).Href);
			chapters = await MapNavMapToEpubChapters(ncx.NavMap);
		}
		else
		{
			throw new Exception("Error parsing epub: No Table of Contents found");
		}
		if(cover != null)
		{
			if(chapters.Count == 1)
			{
				chapters.First().Chapters.Insert(0, cover);
			}
			else
			{
				chapters.Insert(0, cover);
			}
		}
			
		content.TableOfContents = chapters;

		return content;

	}

	/// <summary>
	/// Recursively loads the chapters from the NCX TOC
	/// </summary>
	/// <param name="navpoints">List of NXC NavPoints</param>
	/// <returns>List of EpubChapter</returns>
	private async Task<List<EpubChapter>> MapNavMapToEpubChapters(List<NCXNavPoint> navpoints)
	{
		var tasks = navpoints.Select(async navPoint =>
		{
			if (_coverPath != null && CleanPath(navPoint.Content?.Src) == _coverPath)
			{
				return null;
			}

			var chapter = new EpubChapter
			{
				Title = navPoint.NavLabel?.Text,
				ItemId = _package!.Manifest.Items.FirstOrDefault(x => x.Href == CleanPath(navPoint.Content?.Src))?.Id
			};

			if(navPoint.NavPoints.Count > 0)
			{
				chapter.Chapters = await MapNavMapToEpubChapters(navPoint.NavPoints);
			}
				

			return chapter;
		});

		var results = await Task.WhenAll(tasks);
		return results.Where(ch => ch != null).ToList()!;
	}

	/// <summary>
	/// Maps the V3 NAV TOC to an EpubChapter list recursively
	/// </summary>
	/// <param name="navItems">List of Nav li items</param>
	/// <returns>List of EpubChapter</returns>
	private async Task<List<EpubChapter>> MapNavToEpubChapters(List<NavLi> navItems)
	{
		var tasks = navItems.Select(async navItem =>
		{
			var chapter = new EpubChapter
			{
				Title = navItem.Link.Text,
				ItemId = _package!.Manifest.Items.FirstOrDefault(x => x.Href == CleanPath(navItem.Link.Href))?.Id
			};

			if(navItem.ChapterList.Count > 0)
			{
				chapter.Chapters = await MapNavToEpubChapters(navItem.ChapterList.First().Chapter);
			}
				
			return chapter;
		});
		var results = await Task.WhenAll(tasks);
		return results.ToList()!;
	}

	/// <summary>
	/// Maps the spine to a list of SpineItem
	/// </summary>
	/// <returns></returns>
	private async Task<List<SpineItem>> MapSpineToSpineItemList()
	{
		var items = await Task.WhenAll(_package!.Spine.ItemRefs.Select(async itemRef =>
		{
			var item = _package!.Manifest.Items.First(x => x.Id == itemRef.IdRef);
			var content = await LoadFileContentAsync(item.Href);
			return new SpineItem
			{
				Id = item.Id,
				TextContent = content,
				Title = GetChapterTitle(content),
				Href = item.Href,
				WordCount = GetWordCount(content),
				Styles = GetStylesheets(content),
				ParagraphClassName = GetParagraphClass(content)
			};
		}));
			
		return items.ToList();
	}

	/// <summary>
	/// Counts the words in a string, removing HTML tags first for accuracy
	/// </summary>
	/// <param name="content">Html</param>
	/// <returns>Word count</returns>
	private int GetWordCount(string content)
	{
		// Remove HTML tags
		var textOnly = HtmlRegex().Replace(content, string.Empty);

		// Count remaining words
		return textOnly.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Length;
	}
		
	/// <summary>
	/// Gets the title of a chapter from the html content
	/// </summary>
	/// <param name="content">Html</param>
	/// <returns>Title</returns>
	private string GetChapterTitle(string content)
	{
		var document = new HtmlDocument();
		document.LoadHtml(content);
		
		//var titleNode = document.DocumentNode.SelectSingleNode("//title");
		var titleNode = document.QuerySelector("title");
		return titleNode != null ? DecodeNumericEntities(titleNode.InnerText) : string.Empty;

		static string DecodeNumericEntities(string input)
		{
			return Regex.Replace(input, "&#([0-9]+);", match =>
			{
				var codePoint = int.Parse(match.Groups[1].Value);
				return char.ConvertFromUtf32(codePoint);
			});
		}
	}
		
	/// <summary>
	/// Gets the stylesheets referenced in the html content
	/// </summary>
	/// <param name="content">Html</param>
	/// <returns>List of paths</returns>
	private List<string> GetStylesheets(string content)
	{
		var document = new HtmlDocument();
		document.LoadHtml(content);
		var linkNodes = document.QuerySelectorAll("link[href]");
		return linkNodes == null ? [] : linkNodes.Select(link => link.Attributes["href"].Value).ToList();
	}

	/// <summary>
	/// Tries to find the most common class in the html content, which is likely to be the paragraph class
	/// </summary>
	/// <param name="content">Html</param>
	/// <returns>Name of the class</returns>
	private string? GetParagraphClass(string content)
	{
		const int minClassCount = 4;
		
		var classRegex = new Regex(@"class\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
		var matches = classRegex.Matches(content);
		var classFrequency = new Dictionary<string, int>();
		foreach (Match match in matches)
		{
			var classes = match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (var className in classes)
			{
				if (!classFrequency.TryAdd(className, 1))
				{
					classFrequency[className]++;
				}
			}
		}
		
		return classFrequency.OrderByDescending(c => c.Value).FirstOrDefault(c => c.Value > minClassCount).Key;
	}

	/// <summary>
	/// Removes the anchor from a path (if any)
	/// </summary>
	/// <param name="path">Path inside the epub</param>
	/// <returns>Cleaned path</returns>
	private string? CleanPath(string? path) => path != null && path.Contains('#') ? path[..path.IndexOf('#')] : path;
	
	/// <summary>
	/// Does some processing to the html such as removing external css references, replacing css properties and converting images to base64
	/// </summary>
	/// <param name="content">The original html</param>
	/// <returns>Processed html</returns>
	public async Task<string> ApplyHtmlProcessingAsync(string content)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(content);
		
		var linkNodes = doc.QuerySelectorAll("link[rel='stylesheet']");
		if (linkNodes != null)
		{
			foreach (var linkNode in linkNodes)
			{
				linkNode.Remove();
			}
		}
		
		
		var divWithImageNodes = doc.QuerySelectorAll("div > img:first-child:last-child");
		if (divWithImageNodes != null)
		{
			foreach (var divNode in divWithImageNodes)
			{
				divNode.ParentNode.SetAttributeValue("style", "margin: 0 auto;text-align:center;");
			}
		}
		
		var spans = doc.QuerySelectorAll("p span:first-child");
		foreach (var span in spans)
		{
			if(span is not { InnerText.Length: 1 }) continue;
			var letter = span.InnerText;
			var elementsToRemove = new List<HtmlNode> { span };

			var parent = span.ParentNode;
			while (parent.Name != "p")
			{
				elementsToRemove.Add(parent);
				parent = parent.ParentNode;
			}
			
			if (parent.Attributes.Contains("class"))
			{
				parent.Attributes["class"].Value += " drop-cap";
			}
			else
			{
				parent.SetAttributeValue("class", "drop-cap");
			}

			foreach (var node in elementsToRemove)
			{
				node.Remove();
			}
			parent.InnerHtml = letter + parent.InnerHtml;
			break;
		}
		
		var imageNodes = doc.QuerySelectorAll("img, image");
		if (imageNodes != null)
		{
			foreach (var imageNode in imageNodes)
			{
				var attributeName = imageNode.Name == "img" ? "src" : "href";

				var src = imageNode.Attributes.FirstOrDefault(a => a.Name == attributeName || a.Name.EndsWith(attributeName))?.Value;
				if (string.IsNullOrEmpty(src)) continue;
				var imageBytes = await LoadImageAsBytes(src);
				imageNode.SetAttributeValue(attributeName, $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}");
				if (imageNode.Attributes.Contains("class"))
				{
					imageNode.Attributes["class"].Value += " zoomable";
				}
				else
				{
					imageNode.SetAttributeValue("class", "zoomable");
				}
			}
		}

		StringBuilder result = new(doc.DocumentNode.OuterHtml);

		await ReplaceCssProperties(result);
		return result.ToString();

	}

	/// <summary>
	/// Replaces css properties with custom values
	/// </summary>
	/// <param name="content">Html</param>
	private async Task ReplaceCssProperties(StringBuilder content)
	{
		var contentString = content.ToString();
		var tasks = _customStyles.SelectMany(cSsProperty =>
		{
			Regex regex = new(@$"{cSsProperty.Property}:\s*([^;}}]+?)(;|}})");
			if (!regex.IsMatch(contentString))
			{
				return [Task.FromResult((original: (string?)null, replacement: (string?)null))];
			}

			var matches = regex.Matches(contentString);
			return matches.DistinctBy(m => m.Value).Select(async match =>
			{
				return await Task.Run(() =>
				{
					switch (cSsProperty.Mode)
					{
						case CssEditMode.Remove:
							return ((string?)match.Value.Trim(), "");
						case CssEditMode.ReplaceProperty:
							return ((string?)match.Value.Trim(), (string?)match.Value.Replace(cSsProperty.Property, cSsProperty.NewProperty).Trim());
					}
					
					var delimiter = match.Groups[2].Value;

					var values = match.Groups[1].Value.Split(' ').Select(v => v.Trim()).ToList();
					var processedValues = values.Select(value =>
					{
						if (!IsAboveZero(value))
						{
							return value;
						}
						
						return cSsProperty.Mode switch
						{
							CssEditMode.Replace => $"calc({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit})",
							CssEditMode.Add => $"calc({EnsureUnit(value, cSsProperty.CssUnit!)} + ({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit}))",
							CssEditMode.Max => $"max({value}, calc({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit}))",
							_ => value
						};
					});

					var replacement = $"{cSsProperty.Property}: {string.Join(" ", processedValues)}{delimiter}";

					return ((string?)match.Value.Trim(), (string?)replacement.Trim());
				});

			});
		});

		var results = await Task.WhenAll(tasks);
		results = results.Where(x => x is { Item1: not null, Item2: not null }).Distinct().ToArray();
		foreach (var (original, replacement) in results)
		{
			if (original != null && replacement != null)
			{
				content.Replace(original, replacement);
			}
		}
	}

	private bool IsAboveZero(string cssValue)
	{
		var numberMatch = NumberRegex().Match(cssValue);
		if (numberMatch.Success)
		{
			return double.Parse(numberMatch.Value) > 0;
		}
		return false;
	}

	private string EnsureUnit(string value, string unit)
	{
		if (Regex.IsMatch(value, @"[a-zA-Z%]+$"))
			return value;
		
		return value + unit;
	}

	/// <summary>
	/// Loads the content of a file inside the epub
	/// </summary>
	/// <param name="path">Path inside the epub</param>
	/// <returns>Content as string</returns>
	/// <exception cref="Exception"></exception>
	public async Task<string> LoadFileContentAsync(string path)
	{
		if (_zipLock is null)
		{
			throw new Exception("EpubReader not initialized. Call GetOpfPathAsync first.");
		}
		
		var absolutePath = GetAbsolutePath(path)!;
		
		if (_contentCache.TryGetValue(absolutePath, out var cachedContent))
		{
			return cachedContent;
		}
		await _zipLock.WaitAsync();
		try
		{
			var entry = _zipArchive!.GetEntry(absolutePath) ?? throw new Exception($"Could not load file: {path}");
			await using var stream = entry.Open();
			using var reader = new StreamReader(stream);
			var content = await reader.ReadToEndAsync();
			_contentCache[absolutePath] = content;
			return content;
		}
		finally
		{
			_zipLock.Release();
		}
			
	}
	
	/// <summary>
	/// Loads the nav file from the epub
	/// </summary>
	/// <param name="path">Path to load</param>
	/// <returns>Nav object</returns>
	private async Task<Nav> LoadNavAsync(string path)
	{
		var content = await LoadFileContentAsync(path);
		// Deserialize content from inside body tag into Nav using XDocument
		var doc = XDocument.Parse(content);
		var navContent = doc.Descendants().First(x => x.Name.LocalName == "body").Descendants().First(x => x.Name.LocalName == "nav").ToString();
		var serializer = GetSerializer<Nav>();
		return (Nav)serializer.Deserialize(new StringReader(navContent))!;
	}

	[GeneratedRegex(@"@import\s*[^;]+;")]
	private static partial Regex CssImportRegex();
	[GeneratedRegex(@"@font-face\s*{[^}]+}")]
	private static partial Regex FontFaceRegex();
	[GeneratedRegex(@"\d+\.?\d*")]
	private static partial Regex NumberRegex();
	[GeneratedRegex("<.*?>")]
	private static partial Regex HtmlRegex();

    public void Dispose()
    {
	    _rootFolder = null;
	    _package = null;
	    _coverPath = null;
	    _contentCache.Clear();
	    _imageCache.Clear();
	    _customStyles.Clear();
	    _zipArchive?.Dispose();
	    _zipLock?.Dispose();
	    GC.SuppressFinalize(this);
    }
}