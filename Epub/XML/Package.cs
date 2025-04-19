using System.Collections.Generic;
using System.Xml.Serialization;

namespace BookHeaven.EpubManager.Epub.XML
{
	[XmlRoot("package", Namespace = "http://www.idpf.org/2007/opf", IsNullable = false)]
	public class Package
	{
		[XmlAttribute("version")]
		public string Version { get; set; } = null!;

		[XmlAttribute("unique-identifier")]
		public string UniqueIdentifier { get; set; } = null!;

		[XmlElement("metadata")]
		public Metadata Metadata { get; set; } = null!;

		[XmlElement("manifest")]
		public Manifest Manifest { get; set; } = null!;

		[XmlElement("spine")]
		public Spine Spine { get; set; } = null!;

		[XmlElement("guide")]
		public Guide? Guide { get; set; }
	}

	public class Metadata
	{
		[XmlElement(ElementName = "title", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string> Titles { get; set; } = [];

		[XmlElement(ElementName = "language", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string> Languages { get; set; } = [];

		[XmlElement(ElementName = "identifier", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<Identifier> Identifiers { get; set; } = [];

		[XmlElement(ElementName = "creator", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<Creator>? Creators { get; set; } = [];

		[XmlElement(ElementName = "contributor", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<Contributor>? Contributors { get; set; } = [];

		[XmlElement(ElementName = "publisher", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string>? Publishers { get; set; } = [];

		[XmlElement(ElementName = "date", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string?>? Dates { get; set; } = [];

		[XmlElement(ElementName = "rights", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string>? Rights { get; set; } = [];

		[XmlElement(ElementName = "subject", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string>? Subjects { get; set; } = [];

		[XmlElement(ElementName = "type", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string>? Types { get; set; } = [];

		[XmlElement(ElementName = "description", Namespace = "http://purl.org/dc/elements/1.1/")]
		public List<string>? Descriptions { get; set; } = [];

		[XmlElement("meta")]
		public List<Meta> Meta { get; set; } = [];

	}

	public class Creator
	{
		[XmlAttribute("file-as")]
		public string? FileAs { get; set; }

		[XmlText]
		public string Name { get; set; } = null!;

		[XmlAttribute("role")]
		public string? Role { get; set; }
	}

	public class Contributor
    {
        [XmlAttribute("file-as")]
        public string? FileAs { get; set; }

        [XmlText]
        public string Name { get; set; } = null!;

        [XmlAttribute("role")]
        public string? Role { get; set; }
    }

	public class Identifier
	{
		[XmlAttribute(AttributeName = "id")]
		public string Id { get; set; } = null!;
		[XmlAttribute(Namespace = "http://www.idpf.org/2007/opf", AttributeName = "scheme")]
		public string Scheme { get; set; } = null!;
		[XmlText]
		public string Value { get; set; } = null!;
	}

	public class Meta
	{
		[XmlAttribute("name")]
		public string Name { get; set; } = null!;

		[XmlAttribute("content")]
		public string Content { get; set; } = null!;
	}

	public class Manifest
	{
		[XmlElement("item")]
		public List<Item> Items { get; set; } = null!;
	}

	public class Item
	{
		[XmlAttribute("id")]
		public string Id { get; set; } = null!;

		[XmlAttribute("href")]
		public string Href { get; set; } = null!;

		[XmlAttribute("media-type")]
		public string MediaType { get; set; } = null!;
		
		[XmlAttribute("properties")]
		public string? Properties { get; set; }
	}

	public class Spine
	{
		[XmlAttribute("toc")]
		public string? Toc { get; set; }

		[XmlElement("itemref")]
		public List<ItemRef> ItemRefs { get; set; } = [];
	}

	public class ItemRef
	{
		[XmlAttribute("idref")]
		public string IdRef { get; set; } = null!;

		[XmlAttribute("linear")]
		public string Linear { get; set; } = null!;
	}

	public class Guide
	{
		[XmlElement("reference")]
		public List<Reference> References { get; set; } = [];
	}

	public class Reference
	{
		[XmlAttribute("href")]
		public string Href { get; set; } = null!;

		[XmlAttribute("type")]
		public string Type { get; set; } = null!;

		[XmlAttribute("title")]
		public string Title { get; set; } = null!;

	}
}
