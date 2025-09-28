using System;
using System.Linq;
using BookHeaven.EpubManager.Entities;
using BookHeaven.EpubManager.Epub.Entities;
using Content = BookHeaven.EpubManager.Entities.Content;

namespace BookHeaven.EpubManager.Extensions.Mapping;

public static class EpubBookToEbook
{
    public static Ebook ToEbook(this EpubBook epubBook)
    {
        var metadata = epubBook.Metadata;
        var ebook = new Ebook
        {
            Title = metadata.Title,
            Author = metadata.Author,
            Language = metadata.Language,
            Series = metadata.Series ?? string.Empty,
            SeriesIndex = metadata.SeriesIndex,
            Publisher = metadata.Publisher ?? string.Empty,
            Synopsis = metadata.Description ?? string.Empty,
            PublishDate = metadata.PublishDate,
            Identifiers = metadata.Identifiers.Select(i => i.ToIdentifier()).ToList(),
            Cover = epubBook.Cover,
            Content = new Content
            {
                Stylesheets = epubBook.Content.Styles.Select(s => s.ToStylesheet()).ToList(),
                Chapters = epubBook.Content.Spine.Select(c => c.ToChapter()).ToList(),
                TableOfContents = epubBook.Content.TableOfContents.Select(c => c.ToTocEntry()).ToList()
            }
        };

        return ebook;
    }
    
    private static Stylesheet ToStylesheet(this Style style)
    {
        return new Stylesheet
        {
            Identifier = style.Name,
            Content = style.Content
        };
    }

    private static Identifier ToIdentifier(this EpubIdentifier identifier)
    {
        return new Identifier
        {
            Scheme = identifier.Scheme,
            Value = identifier.Value
        };
    }

    private static Chapter ToChapter(this SpineItem chapter)
    {
        return new Chapter
        {
            Title = chapter.Title,
            Content = chapter.TextContent,
            Identifier = chapter.Id,
            WordCount = chapter.WordCount,
            Stylesheets = chapter.Styles,
            ParagraphClassName = chapter.ParagraphClassName
        };
    }
    
    private static TocEntry ToTocEntry(this EpubChapter epubChapter)
    {
        return new TocEntry
        {
            Title = epubChapter.Title,
            Id = epubChapter.ItemId,
            Entries = epubChapter.Chapters.Select(c => c.ToTocEntry()).ToList()
        };
    }
}