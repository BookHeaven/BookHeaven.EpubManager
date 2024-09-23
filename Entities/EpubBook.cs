using EpubManager.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EpubManager.Entities
{
	public class EpubBook
	{
		public string FilePath { get; set; } = null!;
		public string RootFolder { get; set; } = null!;
		public byte[]? Cover { get; set; }
		public EpubMetadata Metadata { get; set; } = new EpubMetadata();
		public Content Content { get; set; } = new();
	}

	public class EpubMetadata
	{
		public string Title { get; set; } = null!;
		public string Language { get; set; } = null!;
		public List<EpubIdentifier> Identifiers { get; set; } = [];
		public string Author => Authors.Count > 0 ? Authors.First() : string.Empty;
		public List<string> Authors { get; set; } = [];
		public string? Publisher { get; set; }
		public string? PublishDate { get; set; }
		public string? Rights { get; set; }
		public string? Subject { get; set; }
		public string? Description { get; set; }
		public string? Series { get; set; }
		public decimal? SeriesIndex { get; set; }
	}

	public class EpubIdentifier
	{
		public string Value { get; set; } = null!;
		public string Scheme { get; set; } = null!;
	}

	public class Content
	{
		public IReadOnlyList<string>? Styles { get; set; }

		public IReadOnlyList<EpubChapter> Spine { get; set; } = [];

		public IReadOnlyDictionary<int, EpubChapter> ReadingOrder => _readingOrder.Value;
		readonly Lazy<IReadOnlyDictionary<int, EpubChapter>> _readingOrder;
		//public List<EpubChapter> ReadingOrder => _readingOrder.Value;

		static List<EpubChapter> FlattenChapters(IEnumerable<EpubChapter> chapters)
		{
			List<EpubChapter> flattenedList = [];
			foreach (var chapter in chapters)
			{

				if (chapter.Title != null)
					flattenedList.Add(chapter);
				if (chapter.Chapters.Count != 0)
				{
					flattenedList.AddRange(FlattenChapters(chapter.Chapters));
				}
			}
			return flattenedList;
		}

		public Content()
		{
			_readingOrder = new Lazy<IReadOnlyDictionary<int, EpubChapter>>(() => FlattenChapters(Spine).Select((value, index) => new { value, index }).ToDictionary(x => x.index, x => x.value));
		}

		public int GetWordCount(int? untilChapterIndex = null) => ReadingOrder.Take((int)(untilChapterIndex != null ? untilChapterIndex : ReadingOrder.Count)).Sum(c => c.Value.WordCount);
	}

	public class EpubChapter
	{
		public string? Title { get; set; }
		public string? Path { get; set; }
		public string? Content { get; set; }
		public int WordCount { get; set; }

		[JsonIgnore]
		readonly Lazy<IEnumerable<EpubChapter>> _chapters;
		[JsonIgnore]
		public List<EpubChapter> Chapters { get; set; } = [];

		public EpubChapter()
		{
			_chapters = new Lazy<IEnumerable<EpubChapter>>(() => FlattenSpine(Chapters));
		}

		public int GetWordsPerPage(int pages)
		{
			return WordCount / pages;
		}

		public bool ContainsChapter(string path) => _chapters.Value.Any(x => x.Path == path);

		static List<EpubChapter> FlattenSpine(IEnumerable<EpubChapter> chapters)
		{
			return [.. chapters, .. chapters.SelectMany(np => FlattenSpine(np.Chapters ?? []))];
		}
	}
}
