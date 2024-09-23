using System.IO;
using System.IO.Compression;

namespace EpubManager.Entities
{
	public class EpubFile
	{
		private readonly ZipArchive file;

		public EpubFile(string path)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("File not found", path);
			}

			file = Open(path);
		}

		private ZipArchive Open(string path)
		{
			return new ZipArchive(File.Open(path, FileMode.Open), ZipArchiveMode.Read);
		}
	}
}
