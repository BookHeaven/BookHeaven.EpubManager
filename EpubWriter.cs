using EpubManager.Entities;
using EpubManager.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EpubManager.Extensions;

namespace EpubManager
{
    public interface IEpubWriter
    {
        Task ReplaceMetadata(string bookPath, EpubMetadata metadata);
        Task ReplaceCover(string bookPath, string newCoverPath);
    }

    public class EpubWriter(IEpubReader reader) : IEpubWriter
    {
        public async Task ReplaceMetadata(string bookPath, EpubMetadata metadata)
		{
			string opf = await reader.GetOpfPath(bookPath);
			string xml = await reader.LoadFileContent(opf);


			XDocument package = XDocument.Parse(xml);
			XElement metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");
			XNamespace ns = metadataElement.Name.Namespace;
			
			metadataElement.SetItem(ns, "title", metadata.Title);
			metadataElement.SetItem(ns, "creator", metadata.Author);
			metadataElement.SetItem(ns, "description", metadata.Description);
			metadataElement.SetItem(ns, "language", metadata.Language);
			metadataElement.SetItem(ns, "publisher", metadata.Publisher);
			metadataElement.SetItem(ns, "date", metadata.PublishDate);
			
			metadataElement.SetMetaItem("calibre:series", metadata.Series);
			metadataElement.SetMetaItem("calibre:series_index", metadata.SeriesIndex?.ToString("0.##"));

			using(ZipArchive archive = ZipFile.Open(bookPath, ZipArchiveMode.Update))
			{
				ZipArchiveEntry packageEntry = archive.GetEntry(opf)!;
				using(Stream stream = packageEntry.Open())
				{
					stream.SetLength(0);
					var bytes = Encoding.UTF8.GetBytes(package.ToString());
					await stream.WriteAsync(bytes);
				}
			}
		}

        public async Task ReplaceCover(string bookPath, string newCoverPath)
        {
            //Find cover in meta elements
            string opfPath = await reader.GetOpfPath(bookPath);
			string xml = await reader.LoadFileContent(opfPath);

			XDocument package = XDocument.Parse(xml);
			string rootFolder = Path.GetDirectoryName(opfPath)!;

			XElement metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");

			var coverId = metadataElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "meta" && x.Attribute("name")?.Value == "cover")?.Attribute("content")?.Value;

			//Find cover in manifest
			XElement manifestElement = package.Descendants().First(x => x.Name.LocalName == "manifest");
			XElement? coverElement = manifestElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "item" && x.Attribute("id")?.Value == coverId);

			//Replace cover
			if (coverElement != null)
			{
				var imageAsBytes = await File.ReadAllBytesAsync(newCoverPath);
				var coverPath = coverElement.Attribute("href")?.Value ?? null;
				if (coverPath == null) return;
				using (ZipArchive archive = ZipFile.Open(bookPath, ZipArchiveMode.Update))
				{
					ZipArchiveEntry coverEntry = archive.GetEntry(Path.Combine(rootFolder, coverPath))!;
					using (Stream stream = coverEntry.Open())
					{
						stream.SetLength(0);
						await stream.WriteAsync(imageAsBytes);
					}
				}
			}
        }
    }

    
}
