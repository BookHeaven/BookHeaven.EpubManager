using System;
using System.Threading.Tasks;
using BookHeaven.EpubManager.Entities;

namespace BookHeaven.EpubManager.Abstractions;

public interface IEbookReader : IDisposable
{
    Task<Ebook> ReadMetadataAsync(string path);
    Task<Ebook> ReadAllAsync(string path);
    Task<string> ApplyHtmlProcessingAsync(string content);
}