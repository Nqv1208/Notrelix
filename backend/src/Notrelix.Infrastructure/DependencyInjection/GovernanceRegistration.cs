using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Auditing;

namespace Notrelix.Infrastructure;

public static class GovernanceRegistration
{
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
