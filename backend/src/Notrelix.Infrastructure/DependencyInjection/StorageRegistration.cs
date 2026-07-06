using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure;

public static class StorageRegistration
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Storage.StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName));
        services.AddScoped<IStorageService,
            Storage.Providers.LocalStorageProvider>();
        return services;
    }
}
