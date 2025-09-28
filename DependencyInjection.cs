using BookHeaven.EpubManager.Abstractions;
using BookHeaven.EpubManager.Epub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookHeaven.EpubManager;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EpubManager services
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection AddEpubManager(this IServiceCollection services)
    {
        services.AddTransient<IEbookReader, EpubReader>();
        services.AddTransient<IEpubWriter, EpubWriter>();
        return services;
    }
}