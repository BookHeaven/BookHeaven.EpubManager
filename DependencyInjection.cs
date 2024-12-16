using Microsoft.Extensions.DependencyInjection;

namespace EpubManager;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EpubManager services
    /// </summary>
    /// <param name="services"></param>
    /// <param name="readerOnly">In case you only need to inject the reader</param>
    public static IServiceCollection AddEpubManager(this IServiceCollection services, bool readerOnly = false)
    {
        services.AddScoped<IEpubReader, EpubReader>();
        if (!readerOnly)
        {
            services.AddScoped<IEpubWriter, EpubWriter>();
        }
        return services;
    }
}