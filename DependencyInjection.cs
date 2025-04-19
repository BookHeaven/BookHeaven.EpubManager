using BookHeaven.EpubManager.Epub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookHeaven.EpubManager;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EpubManager services
    /// </summary>
    /// <param name="services"></param>
    /// <param name="readerOnly">In case you only need to inject the reader</param>
    public static IServiceCollection AddEpubManager(this IServiceCollection services)
    {
        services.AddTransient<IEpubReader, Epub.Services.EpubReader>();
        services.AddTransient<IEpubWriter, EpubWriter>();
        return services;
    }
}