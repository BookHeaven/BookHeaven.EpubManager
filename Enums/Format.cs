using System.ComponentModel;

namespace BookHeaven.EpubManager.Enums;

public enum Format
{
    [Description("EPUB")]
    Epub = 0,
    [Description("PDF")]
    Pdf = 1,
}