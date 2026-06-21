namespace Notrelix.API.Auth;

public static class AuthorizationSetup
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("SystemAdmin", policy =>
                policy.RequireRole("SystemAdmin"));

            options.AddPolicy("InternalService", policy =>
                policy.RequireRole("InternalService"));
        });

        return services;
    }
}
