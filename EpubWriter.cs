using EpubManager.Entities;
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
			reader.Initialize(bookPath);
			var opf = await reader.GetOpfPath();
			var xml = await reader.LoadFileContent(opf);


			var package = XDocument.Parse(xml);
			var metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");
			var ns = metadataElement.Name.Namespace;
			
			metadataElement.SetItem(ns, "title", metadata.Title);
			metadataElement.SetItem(ns, "creator", metadata.Author);
			metadataElement.SetItem(ns, "description", metadata.Description);
			metadataElement.SetItem(ns, "language", metadata.Language);
			metadataElement.SetItem(ns, "publisher", metadata.Publisher);
			metadataElement.SetItem(ns, "date", metadata.PublishDate);
			
			metadataElement.SetMetaItem("calibre:series", metadata.Series);
			metadataElement.SetMetaItem("calibre:series_index", metadata.SeriesIndex?.ToString("0.##"));

			using(var archive = ZipFile.Open(bookPath, ZipArchiveMode.Update))
			{
				var packageEntry = archive.GetEntry(opf)!;
				using(var stream = packageEntry.Open())
				{
					stream.SetLength(0);
					var bytes = Encoding.UTF8.GetBytes(package.ToString());
					await stream.WriteAsync(bytes);
				}
			}
		}

        public async Task ReplaceCover(string bookPath, string newCoverPath)
        {
	        reader.Initialize(bookPath);
            //Find cover in meta elements
            var opfPath = await reader.GetOpfPath();
			var xml = await reader.LoadFileContent(opfPath);

			var package = XDocument.Parse(xml);
			var rootFolder = Path.GetDirectoryName(opfPath)!;

			var metadataElement = package.Descendants().First(x => x.Name.LocalName == "metadata");

			var coverId = metadataElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "meta" && x.Attribute("name")?.Value == "cover")?.Attribute("content")?.Value;

			//Find cover in manifest
			var manifestElement = package.Descendants().First(x => x.Name.LocalName == "manifest");
			var coverElement = manifestElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "item" && x.Attribute("id")?.Value == coverId);

			//Replace cover
			if (coverElement != null)
			{
				var imageAsBytes = await File.ReadAllBytesAsync(newCoverPath);
				var coverPath = coverElement.Attribute("href")?.Value ?? null;
				if (coverPath == null) return;
				using (var archive = ZipFile.Open(bookPath, ZipArchiveMode.Update))
				{
					var coverEntry = archive.GetEntry(Path.Combine(rootFolder, coverPath))!;
					using (var stream = coverEntry.Open())
					{
						stream.SetLength(0);
						await stream.WriteAsync(imageAsBytes);
					}
				}
			}
        }
    }

    
}
