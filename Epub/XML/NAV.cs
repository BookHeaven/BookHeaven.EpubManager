using System.Collections.Generic;
using System.Xml.Serialization;
using BookHeaven.EpubManager.Epub.Constants;

namespace BookHeaven.EpubManager.Epub.XML;

[XmlRoot("nav", Namespace = Namespaces.Xhtml)]
internal class Nav
{
    [XmlElement("ol")]
    public List<NavOl> ChapterList { get; set; } = [];
}

internal class NavOl
{
    [XmlElement("li")]
    public List<NavLi> Chapter { get; set; } = [];
}

internal class NavLi
{
    [XmlElement("a")]
    public NavA Link { get; set; } = null!;
    [XmlElement("ol")]
    public List<NavOl> ChapterList { get; set; } = [];
}

internal class NavA
{
    [XmlAttribute("href")]
    public string Href { get; set; } = null!;
    [XmlText]
    public string Text { get; set; } = null!;
}
