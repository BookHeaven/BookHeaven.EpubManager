using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BookHeaven.EpubManager.Epub.Constants;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.EpubManager.Epub.XML;
using BookHeaven.EpubManager.Extensions;

namespace BookHeaven.EpubManager.Epub.Services;

public interface IEpubWriter
{
	Task ReplaceMetadataAsync(string bookPath, EpubMetadata metadata);
	Task ReplaceCoverAsync(string bookPath, string newCoverPath);
}

public class EpubWriter : IEpubWriter
{
	private async Task<(string path, string xml)> LoadOpfAsync(string bookPath)
	{
		using var reader = new EpubReader();
		var opfPath = await reader.GetOpfPathAsync(bookPath);
		var xml = await reader.LoadFileContentAsync(opfPath);
		return (opfPath, xml);
	}
	    
	public async Task ReplaceMetadataAsync(string bookPath, EpubMetadata metadata)
	{
		var opf = await LoadOpfAsync(bookPath);

		var package = XDocument.Parse(opf.xml);
		var version = int.TryParse(package.Root?.Attribute("version")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 2;
		var metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");
			
		metadataElement.SetItem(Namespaces.Dc, "title", metadata.Title);
		var authorItem = metadataElement.SetItem(Namespaces.Dc, "creator", metadata.Author);
		metadataElement.SetItem(Namespaces.Dc, "description", metadata.Description ?? string.Empty);
		metadataElement.SetItem(Namespaces.Dc, "language", metadata.Language);
		metadataElement.SetItem(Namespaces.Dc, "publisher", metadata.Publisher ?? string.Empty);
		metadataElement.SetItem(Namespaces.Dc, "date", metadata.PublishDate ?? string.Empty);
		metadataElement.SetItem(Namespaces.Dc, "rights", metadata.Rights ?? string.Empty);
		if (authorItem is not null)
		{
			var fileAs = metadata.Author;
			if (metadata.Author.Contains(' '))
			{
				var parts = metadata.Author.Split(' ');
				fileAs = $"{parts.Last()}, {string.Join(" ", parts.Take(parts.Length - 1))}";
			}

			if (version == 3)
			{
				metadataElement.SetMetaItem("file-as", fileAs, version, authorItem.Attribute("id")?.Value);
			}
			else
			{
				authorItem.SetAttributeValue(XName.Get("file-as", Namespaces.Opf), fileAs);
			}
		}

		if (!string.IsNullOrWhiteSpace(metadata.Series))
		{
			metadataElement.SetMetaItem("calibre:series", metadata.Series, version);
			metadataElement.SetMetaItem("calibre:series_index", metadata.SeriesIndex?.ToString("0.##", CultureInfo.InvariantCulture)!, version);
		}
		

		using var archive = ZipFile.Open(bookPath, ZipArchiveMode.Update);
		var packageEntry = archive.GetEntry(opf.path)!;
		await using var stream = packageEntry.Open();
		stream.SetLength(0);
		var bytes = Encoding.UTF8.GetBytes(package.ToString());
		await stream.WriteAsync(bytes);
	}

	public async Task ReplaceCoverAsync(string bookPath, string newCoverPath)
	{
		//Find cover in meta elements
		var opf = await LoadOpfAsync(bookPath);

		var package = XDocument.Parse(opf.xml);
		var version = int.TryParse(package.Root?.Attribute("version")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 2;
		var rootFolder = Path.GetDirectoryName(opf.path)!;

		var metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");

		//Find cover in manifest
		var manifestElement = package.Descendants().First(x => x.Name.LocalName == "manifest");
		var coverPath = version switch
		{
			3 => manifestElement.Descendants().First(x => x.Attribute("properties")?.Value == "cover-image").Attribute("href")?.Value,
			_ => manifestElement.Descendants().First(x => x.Attribute("id")?.Value == metadataElement.Descendants().FirstOrDefault(m => m.Attribute("name")?.Value == "cover")?.Attribute("content")?.Value).Attribute("href")?.Value
		};
		
		if(coverPath is null) return;

		//Replace cover
		var imageAsBytes = await File.ReadAllBytesAsync(newCoverPath);
		using var archive = ZipFile.Open(bookPath, ZipArchiveMode.Update);
		var coverEntry = archive.GetEntry(Path.Combine(rootFolder, coverPath).Replace("\\", "/"));
		if (coverEntry == null) return;
		await using var stream = coverEntry.Open();
		stream.SetLength(0);
		await stream.WriteAsync(imageAsBytes);
	}
}