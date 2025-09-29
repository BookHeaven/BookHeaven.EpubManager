using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Entities;
using BookHeaven.EpubManager.Enums;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout.Element;

namespace BookHeaven.EpubManager.Pdf.Services;

public class PdfReader : IEbookReader
{
    public Task<Ebook> ReadMetadataAsync(string path)
    {
        var ebook = new Ebook
        {
            Format = Format.Pdf
        };
        using var pdfDocument = new PdfDocument(new iText.Kernel.Pdf.PdfReader(path));

        // Use PdfCanvasProcessor to extract the first image from the first page
        var coverPage = pdfDocument.GetPage(1);
        var listener = new ImageRenderListener();
        var processor = new PdfCanvasProcessor(listener);
        processor.ProcessPageContent(coverPage);
        ebook.Cover = listener.FirstImageBytes;

        ebook.Title = pdfDocument.GetDocumentInfo().GetTitle() ?? System.IO.Path.GetFileNameWithoutExtension(path);
        ebook.Author = pdfDocument.GetDocumentInfo().GetAuthor() ?? "Unknown";
        ebook.Synopsis = pdfDocument.GetDocumentInfo().GetSubject();
        ebook.Pages = pdfDocument.GetNumberOfPages();

        return Task.FromResult(ebook);
    }

    // Listener to extract the first image found
    private class ImageRenderListener : IEventListener
    {
        public byte[]? FirstImageBytes { get; private set; }
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_IMAGE && FirstImageBytes == null)
            {
                var renderInfo = (ImageRenderInfo)data;
                var imageObject = renderInfo.GetImage();
                if (imageObject != null)
                {
                    FirstImageBytes = imageObject.GetImageBytes(true);
                }
            }
        }
        public ICollection<EventType>? GetSupportedEvents() => null;
    }

    public async Task<Ebook> ReadAllAsync(string path)
    {
        var ebook = await ReadMetadataAsync(path);

        using var pdfDocument = new PdfDocument(new iText.Kernel.Pdf.PdfReader(path));
        // Get outlines (table of contents)
        var outlines = pdfDocument.GetOutlines(false);
        var tree = pdfDocument.GetCatalog().GetNameTree(PdfName.Dests);
        var toc = await MapOutlinesToTableOfContents(pdfDocument, tree, outlines?.GetAllChildren() ?? []);
        if (toc.All(e => e.Id != "1"))
        {
            toc.Insert(0, new TocEntry { Id = "1", Title = "Cover" });
        }
        ebook.Content.TableOfContents = toc;
        ebook.Content.Chapters = MapPagesToChapters(pdfDocument, GetFlattenedToc(ebook.Content.TableOfContents));

        return ebook;
    }

    public Task<string> ApplyHtmlProcessingAsync(string content)
    {
        throw new NotSupportedException("PDF format does not support HTML processing.");
    }
    
    private static async Task<List<TocEntry>> MapOutlinesToTableOfContents(PdfDocument pdfDocument, PdfNameTree tree, IList<PdfOutline> outlines)
    {
        var tasks = outlines.Select(async outline =>
        {
            var entry = new TocEntry
            {
                Title = outline.GetTitle(),
            };
            
            var destination = outline.GetDestination();
            var page = (PdfDictionary?)destination?.GetDestinationPage(tree);
            if (page is not null)
            {
                entry.Id = pdfDocument.GetPageNumber(page).ToString();
            }

            if(outline.GetAllChildren().Count > 0)
            {
                entry.Entries = await MapOutlinesToTableOfContents(pdfDocument, tree,outline.GetAllChildren());
            }
				
            return entry;
        });
        return (await Task.WhenAll(tasks)).ToList();
    }

    private IReadOnlyList<Chapter> MapPagesToChapters(PdfDocument pdfDocument, IReadOnlyList<TocEntry> flattenedToc)
    {
        var chapters = new List<Chapter>();
        var coverChapter = new Chapter
        {
            Identifier = "1",
            WordCount = 1,
            Content = ConvertPdfPageToHtml(pdfDocument.GetPage(1)),
            IsContentProcessed = true
        };
        chapters.Add(coverChapter);

        if (flattenedToc.Count > 2)
        {
            // Group pages into chapters based on table of contents, starting from page 2
            for (var i = 1; i < flattenedToc.Count; i++)
            {
                var startPage = int.Parse(flattenedToc[i - 1].Id ?? "1");
                var endPage = int.Parse(flattenedToc[i].Id ?? pdfDocument.GetNumberOfPages().ToString());
                var chapter = new Chapter
                {
                    Identifier = flattenedToc[i].Id ?? (i + 1).ToString(),
                    Title = flattenedToc[i].Title,
                    WordCount = endPage - startPage,
                    IsContentProcessed = true
                };
                for (var j = startPage; j < endPage; j++)
                {
                    chapter.Content += ConvertPdfPageToHtml(pdfDocument.GetPage(j));
                }

                chapters.Add(chapter);
            }

        }
        else
        {
            var chapter = new Chapter
            {
                Identifier = "2",
                WordCount = pdfDocument.GetNumberOfPages() - 1,
                IsContentProcessed = true
            };

            for (var i = 2; i < pdfDocument.GetNumberOfPages() + 1; i++)
            {
                chapter.Content += ConvertPdfPageToHtml(pdfDocument.GetPage(i));
            }
            chapters.Add(chapter);
        }
        
        
        return chapters;
    }
    
    private static string ConvertPdfPageToHtml(PdfPage page)
    {
        using var tempDoc = new PdfDocument(new iText.Kernel.Pdf.PdfWriter(new System.IO.MemoryStream()));
        var xObject = page.CopyAsFormXObject(tempDoc);
        var image = new Image(xObject);
        using var ms = new System.IO.MemoryStream();
        var pdfImage = image.GetXObject().GetPdfObject();
        if (pdfImage != null)
        {
            var bytes = pdfImage.GetBytes();
            ms.Write(bytes, 0, bytes.Length);
        }
        var base64Image = Convert.ToBase64String(ms.ToArray());
        return $"<div><img src=\"data:image/png;base64,{base64Image}\" alt=\"\" /></div>";
    }
    
    private static IReadOnlyList<TocEntry> GetFlattenedToc(IReadOnlyList<TocEntry> tableOfContents)
    {
        var result = new List<TocEntry>();
        Flatten(tableOfContents);
        return result;

        void Flatten(IReadOnlyList<TocEntry> entries)
        {
            foreach (var entry in entries)
            {
                result.Add(entry);
                Flatten(entry.Entries);
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}