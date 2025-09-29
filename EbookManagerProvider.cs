using System;
using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace BookHeaven.EpubManager;

public class EbookManagerProvider(IServiceProvider serviceProvider)
{
    public IEbookReader GetReader(Format format) => serviceProvider.GetRequiredKeyedService<IEbookReader>(format);
    public IEbookWriter? GetWriter(Format format) => serviceProvider.GetKeyedService<IEbookWriter>(format);
}