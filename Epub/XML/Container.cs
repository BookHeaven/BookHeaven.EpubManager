using System.Collections.Generic;
using System.Xml.Serialization;
using BookHeaven.EpubManager.Epub.Constants;

namespace BookHeaven.EpubManager.Epub.XML;

[XmlRoot("container", Namespace = Namespaces.Container, IsNullable = false)]
internal class Container
{
	[XmlElement("rootfiles")]
	public RootFiles RootFiles { get; set; } = null!;
}

internal class RootFiles
{
	[XmlElement("rootfile")]
	public List<RootFile> RootFile { get; set; } = null!;
}

internal class RootFile
{
	[XmlAttribute("full-path")]
	public string FullPath { get; set; } = null!;

	[XmlAttribute("media-type")]
	public string MediaType { get; set; } = null!;
}