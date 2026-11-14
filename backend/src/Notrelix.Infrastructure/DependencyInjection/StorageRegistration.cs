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

        services.AddOptions<PresignedUrlOptions>()
            .Bind(configuration.GetSection(PresignedUrlOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PresignedUrlOptions>, PresignedUrlOptionsValidator>();

        // The provider is chosen once, from configuration: the S3 provider serves both the byte port and the pre-signed capability,
        // the filesystem provider serves the byte port and reports no pre-signed support.
        var provider = configuration.GetSection(StorageOptions.SectionName).GetValue<string>(nameof(StorageOptions.Provider))
            ?? StorageProviderNames.FileSystem;
        if (string.Equals(provider, StorageProviderNames.S3, StringComparison.Ordinal))
        {
            services.AddSingleton<Storage.Providers.S3StorageProvider>();
            services.AddScoped<IStorageService>(sp => sp.GetRequiredService<Storage.Providers.S3StorageProvider>());
            services.AddScoped<IPresignedObjectStorage>(sp => sp.GetRequiredService<Storage.Providers.S3StorageProvider>());
        }
        else
        {
            services.AddScoped<IStorageService, Storage.Providers.LocalStorageProvider>();
            services.AddScoped<IPresignedObjectStorage, Storage.Providers.NoPresignedObjectStorage>();
        }

        return services;
    }
}
