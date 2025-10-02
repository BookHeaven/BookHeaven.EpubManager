using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Enums;
using BookHeaven.EpubManager.Formats.Epub.Services;
using BookHeaven.EpubManager.Formats.Pdf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookHeaven.EpubManager;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EpubManager services
    /// </summary>
    /// <param name="services"></param>
    /// <param name="cachePath"></param>
    public static IServiceCollection AddEbookManager(this IServiceCollection services, string cachePath = "")
    {
        Globals.CachePath = cachePath;
        services.AddReaders();
        services.AddWriters();
        services.AddTransient<EbookManagerProvider>();
        return services;
    }

    private static void AddReaders(this IServiceCollection services)
    {
        services.AddKeyedTransient<IEbookReader, EpubReader>(Format.Epub);
        services.AddKeyedTransient<IEbookReader, PdfReader>(Format.Pdf);
    }
    
    private static void AddWriters(this IServiceCollection services)
    {
        services.AddKeyedTransient<IEbookWriter, EpubWriter>(Format.Epub);
        services.AddKeyedTransient<IEbookWriter, PdfWriter>(Format.Pdf);
    }
}