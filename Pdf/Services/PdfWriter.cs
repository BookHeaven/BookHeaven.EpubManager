using System.IO;
using System.Threading.Tasks;
using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Entities;
using iText.IO.Image;
using iText.Kernel.Pdf;

namespace BookHeaven.EpubManager.Pdf.Services;

public class PdfWriter : IEbookWriter
{
    public Task ReplaceMetadataAsync(string bookPath, Ebook ebook)
    {
        using var pdfDocument = new PdfDocument(new iText.Kernel.Pdf.PdfWriter(bookPath));
        var info = pdfDocument.GetDocumentInfo();
        info.SetTitle(ebook.Title);
        info.SetAuthor(ebook.Author);
        if (!string.IsNullOrWhiteSpace(ebook.Synopsis))
            info.SetSubject(ebook.Synopsis);
        
        return Task.CompletedTask;
    }

    public Task ReplaceCoverAsync(string bookPath, string newCoverPath)
    {
        var tempPath = Path.GetTempFileName();

        using (var srcDoc = new PdfDocument(new iText.Kernel.Pdf.PdfReader(bookPath)))
        using (var destDoc = new PdfDocument(new iText.Kernel.Pdf.PdfWriter(tempPath)))
        {
            var coverImage = ImageDataFactory.Create(newCoverPath);
            var pageSize = srcDoc.GetFirstPage().GetPageSize();
            var coverPage = destDoc.AddNewPage(new iText.Kernel.Geom.PageSize(pageSize));
            var pdfCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(coverPage);
            pdfCanvas.AddImageFittedIntoRectangle(coverImage, pageSize, false);

            srcDoc.CopyPagesTo(2, srcDoc.GetNumberOfPages(), destDoc);
        }

        File.Copy(tempPath, bookPath, true);
        File.Delete(tempPath);
        return Task.CompletedTask;
    }
}