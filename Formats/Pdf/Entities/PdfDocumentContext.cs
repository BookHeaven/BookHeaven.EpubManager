using System.IO;
using iText.Kernel.Pdf;

namespace BookHeaven.EpubManager.Formats.Pdf.Entities;

public class PdfDocumentContext
{
    public required PdfDocument Document { get; set; }
    public required string Identifier { get; set; }
    
    public string CachePath => Path.Combine(Globals.CachePath, Identifier);
}