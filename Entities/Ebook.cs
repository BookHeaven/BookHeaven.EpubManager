using System.Collections.Generic;
using System.Linq;
using BookHeaven.EpubManager.Enums;

namespace BookHeaven.EpubManager.Entities;

public class Ebook
{
    public Format Format { get; set; }
    public string Title { get; set; } = null!;
    public string? Synopsis { get; set; }
    public string Author { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string? Series { get; set; }
    public decimal? SeriesIndex { get; set; }
    public string? Publisher { get; set; }
    public string? PublishDate { get; set; }
    public IReadOnlyList<Identifier> Identifiers { get; set; } = [];
    public byte[]? Cover { get; set; }
    public Content Content { get; set; } = new();
    public int Pages { get; set; }
}

public class Identifier
{
    public string Scheme { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class Content
{
    public IReadOnlyList<TocEntry> TableOfContents { get; set; } = [];
    public IReadOnlyList<Chapter> Chapters { get; set; } = [];
    public IReadOnlyList<Stylesheet> Stylesheets { get; set; } = [];
    
    public TocEntry? GetChapterFromTableOfContents(string? itemId)
    {
        return itemId is null ? null : GetTitle(TableOfContents);

        // Search recursively for the title of the chapter with the specified item id
        TocEntry? GetTitle(IReadOnlyList<TocEntry> chapters)
        {
            foreach (var chapter in chapters)
            {
                if (chapter.Id == itemId)
                    return chapter;
                var title = GetTitle(chapter.Entries);
                if (title != null)
                    return title;
            }
            return null;
        }
    }
    
    public int GetWordCount(int? untilChapterIndex = null) => Chapters.Take((untilChapterIndex ?? Chapters.Count)).Sum(c => c.WordCount);
    
}

public class TocEntry
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public IReadOnlyList<TocEntry> Entries { get; set; } = [];
    
    public bool ContainsEntry(string? itemId)
    {
        return itemId is not null && Contains(Entries);

        bool Contains(IReadOnlyList<TocEntry> chapters)
        {
            return chapters.Any(chapter => chapter.Id == itemId || Contains(chapter.Entries));
        }
    }
}

public class Chapter
{
    public string Identifier { get; set; } = null!;
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public List<string> Stylesheets { get; set; } = [];
    public string? ParagraphClassName { get; set; }
    public bool IsContentProcessed { get; set; } = false;
    
    public int WordsPerPage(int pages) => WordCount / (pages == 0 ? 1 : pages);
}

public class Stylesheet
{
    public string Identifier { get; set; } = null!;
    public string Content { get; set; } = null!;
}