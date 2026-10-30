using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Infrastructure.Auditing;

namespace Notrelix.Infrastructure;

public static class GovernanceRegistration
{
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IWorkspaceSharingPolicy, Services.WorkspaceSharingPolicy>();
        return services;
    }
}
