using System.Threading.Tasks;
using BookHeaven.EpubManager.Entities;

namespace BookHeaven.EpubManager.Abstractions;

public interface IEbookWriter
{
    Task ReplaceMetadataAsync(string bookPath, Ebook ebook);
    Task ReplaceCoverAsync(string bookPath, string newCoverPath);
}