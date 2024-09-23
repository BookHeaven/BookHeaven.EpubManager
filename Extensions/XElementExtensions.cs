using System.Linq;
using System.Xml.Linq;

namespace EpubManager.Extensions;

public static class XElementExtensions
{
    public static void SetItem(this XElement parentElement, XNamespace ns, string elementName, string? value)
	{
		if(string.IsNullOrEmpty(value))
		{
			return;
		}
		XElement? element = parentElement.Descendants().FirstOrDefault(x => x.Name.LocalName == elementName);
		if(element == null)
		{
			element = new XElement(ns + elementName);
			parentElement.Add(element);
		}
		element.Value = value;
	}
    
    public static void SetMetaItem(this XElement parentElement, string elementName, string? value)
	{
		if(string.IsNullOrEmpty(value))
		{
			return;
		}
		var element = parentElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "meta" && x.Attribute("name")?.Value == elementName);
		if(element == null)
		{
			element = new XElement("meta", new XAttribute("name", elementName), new XAttribute("content", value));
			parentElement.Add(element);
		}
		element.Value = value;
	}
}