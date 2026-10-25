namespace Notrelix.Infrastructure.Data.Rls;

/// <summary>
/// Provisions the runtime logins over the owner connection (composition-root wiring, like
/// <see cref="RlsPolicyApplier"/>): the startup commands resolve this instead of naming the owner context.
/// </summary>
public sealed class RlsLoginRoleProvisioningService
{
    private readonly ApplicationDbContext _ownerContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RlsLoginRoleProvisioningService> _logger;

    public RlsLoginRoleProvisioningService(
        ApplicationDbContext ownerContext, IConfiguration configuration, ILogger<RlsLoginRoleProvisioningService> logger)
    {
        _ownerContext = ownerContext;
        _configuration = configuration;
        _logger = logger;
    }

    public Task ProvisionAsync(CancellationToken ct = default) =>
        RlsLoginRoleProvisioner.ProvisionAsync(_ownerContext, _configuration, _logger, ct);
}
