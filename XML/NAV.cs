using System.Collections.Generic;
using System.Xml.Serialization;

namespace BookHeaven.EpubManager.XML;

[XmlRoot("nav", Namespace = "http://www.w3.org/1999/xhtml")]
public class Nav
{
    [XmlElement("ol")]
    public List<NavOl> ChapterList { get; set; } = [];
}

public class NavOl
{
    [XmlElement("li")]
    public List<NavLi> Chapter { get; set; } = [];
}

public class NavLi
{
    [XmlElement("a")]
    public NavA Link { get; set; } = null!;
    [XmlElement("ol")]
    public List<NavOl> ChapterList { get; set; } = [];
}

public class NavA
{
    [XmlAttribute("href")]
    public string Href { get; set; } = null!;
    [XmlText]
    public string Text { get; set; } = null!;
}
