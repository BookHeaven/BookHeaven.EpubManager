using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BookHeaven.EpubManager.Epub.Entities;

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
	public IReadOnlyList<Style> Styles { get; set; } = [];
		
	public List<SpineItem> Spine { get; set; } = [];
	
	public List<EpubChapter> TableOfContents { get; set; } = [];
	
	public EpubChapter? GetChapterFromTableOfContents(string? itemId)
	{
		return itemId is null ? null : GetTitle(TableOfContents);

		// Search recursively for the title of the chapter with the specified item id
		EpubChapter? GetTitle(List<EpubChapter> chapters)
		{
			foreach (var chapter in chapters)
			{
				if (chapter.ItemId == itemId)
					return chapter;
				var title = GetTitle(chapter.Chapters);
				if (title != null)
					return title;
			}
			return null;
		}
	}

	public int GetWordCount(int? untilChapterIndex = null) => Spine.Take((int)(untilChapterIndex != null ? untilChapterIndex : Spine.Count)).Sum(c => c.WordCount);
}

public class SpineItem
{
	public string Id { get; set; } = null!;
	public string? Title { get; set; }
	public string Href { get; set; } = null!;
	public string TextContent { get; set; } = null!;
	public int WordCount { get; set; }
	public List<string> Styles { get; set; } = [];
	public string? ParagraphClassName { get; set; }
	public bool IsContentProcessed { get; set; } = false;

	public int GetWordsPerPage(int pages) => WordCount / (pages == 0 ? 1 : pages);
}
	
public class Style
{
	public string Name { get; set; } = null!;
	public string Content { get; set; } = null!;
}

public class EpubChapter
{
	public string? ItemId { get; set; }
	public string? Title { get; set; }
	[JsonIgnore]
	public List<EpubChapter> Chapters { get; set; } = [];

	
	public bool ContainsChapter(string? itemId)
	{
		return itemId is not null && Contains(Chapters);

		bool Contains(List<EpubChapter> chapters)
		{
			return chapters.Any(chapter => chapter.ItemId == itemId || Contains(chapter.Chapters));
		}
	}
}