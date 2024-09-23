using System.Collections.Generic;
using System.Xml.Serialization;

namespace EpubManager.XML
{
	[XmlRoot("container", Namespace = "urn:oasis:names:tc:opendocument:xmlns:container", IsNullable = false)]
	public class Container
	{
		[XmlElement("rootfiles")]
		public RootFiles RootFiles { get; set; } = null!;
	}

	public class RootFiles
	{
		[XmlElement("rootfile")]
		public List<RootFile> RootFile { get; set; } = null!;
	}

	public class RootFile
	{
		[XmlAttribute("full-path")]
		public string FullPath { get; set; } = null!;

		[XmlAttribute("media-type")]
		public string MediaType { get; set; } = null!;
	}
}
