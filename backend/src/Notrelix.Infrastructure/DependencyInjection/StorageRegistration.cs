using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure;

public static class StorageRegistration
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddScoped<IStorageService,
            Storage.Providers.LocalStorageProvider>();
        return services;
    }
}
