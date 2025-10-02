using System;
using BookHeaven.EbookManager.Formats.Pdf.Enums;

namespace BookHeaven.EbookManager.Formats.Pdf.Entities;

internal class PdfImage : PdfBaseElement
{
    public string MimeType { get; set; } = "image/png";
    public byte[]? Data { get; set; }
    
    public string? Src { get; set; }
    
    public PdfImage() => Type = ElementType.Image;
    
    public string HtmlSource => Data is not null ? $"data:{MimeType};base64,{Convert.ToBase64String(Data)}" : Src?.Replace(Globals.CachePath, "/cache") ?? string.Empty;
}