using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Infrastructure.Identity.Mfa;

namespace Notrelix.Infrastructure;

public static class MfaRegistration
{
    public static IServiceCollection AddMfaInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IMfaChallengeStore, MfaChallengeStore>();
        services.AddScoped<IMfaTotpService, MfaTotpService>();
        services.AddScoped<IMfaRecoveryCodeGenerator, MfaRecoveryCodeGenerator>();

        return services;
    }
}
