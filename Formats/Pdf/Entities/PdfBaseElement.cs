using BookHeaven.EpubManager.Formats.Pdf.Enums;

namespace BookHeaven.EpubManager.Formats.Pdf.Entities;

internal abstract class PdfBaseElement
{
    public ElementType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float EndX { get; set; }
    public float SpaceWidth { get; set; }
    public float Height { get; set; }
    public float Width { get; set; }
    public Alignment Alignment { get; set; } = Alignment.Justify;
}