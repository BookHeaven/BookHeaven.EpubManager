using System;
using System.IO;
using System.Threading.Tasks;
using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Entities;
using iText.IO.Image;
using iText.Kernel.Pdf;

namespace BookHeaven.EpubManager.Formats.Pdf.Services;

public class PdfWriter : IEbookWriter
{
    private static Task ProcessPdfAsync(string bookPath, Action<PdfDocument, PdfDocument> processAction)
    {
        var tempPath = Path.GetTempFileName();
        using (var srcDoc = new PdfDocument(new iText.Kernel.Pdf.PdfReader(bookPath)))
        using (var destDoc = new PdfDocument(new iText.Kernel.Pdf.PdfWriter(tempPath)))
        {
            processAction(srcDoc, destDoc);
        }
        File.Copy(tempPath, bookPath, true);
        File.Delete(tempPath);
        return Task.CompletedTask;
    }

    public Task ReplaceMetadataAsync(string bookPath, Ebook ebook)
    {
        return ProcessPdfAsync(bookPath, (srcDoc, destDoc) =>
        {
            srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), destDoc);
            var info = destDoc.GetDocumentInfo();
            info.SetTitle(ebook.Title);
            info.SetAuthor(ebook.Author);
            if (!string.IsNullOrWhiteSpace(ebook.Synopsis))
                info.SetSubject(ebook.Synopsis);
        });
    }

    public Task ReplaceCoverAsync(string bookPath, string newCoverPath)
    {
        return ProcessPdfAsync(bookPath, (srcDoc, destDoc) =>
        {
            var coverImage = ImageDataFactory.Create(newCoverPath);
            var pageSize = srcDoc.GetFirstPage().GetPageSize();
            var coverPage = destDoc.AddNewPage(new iText.Kernel.Geom.PageSize(pageSize));
            var pdfCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(coverPage);
            pdfCanvas.AddImageFittedIntoRectangle(coverImage, pageSize, false);
            srcDoc.CopyPagesTo(2, srcDoc.GetNumberOfPages(), destDoc);
        });
    }
}