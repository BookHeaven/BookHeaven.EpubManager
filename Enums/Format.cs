using System.ComponentModel;

namespace BookHeaven.EbookManager.Enums;

public enum Format
{
    [Description("EPUB")]
    Epub = 1,
    [Description("PDF")]
    Pdf = 2,
}