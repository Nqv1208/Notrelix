using Notrelix.Infrastructure.Auditing;
using Notrelix.Infrastructure.Governance.Services;

namespace Notrelix.Infrastructure;

public static class GovernanceRegistration
{
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionVersionProvider, PermissionVersionProvider>();
        return services;
    }
}
