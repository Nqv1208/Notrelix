namespace Notrelix.Application.Features.Accounts.Provisioning;

/// <summary>
/// Accounts-owned onboarding provisioning (spec 5.2): Identity registration and
/// first OAuth login create the personal Account and owner membership through
/// this service in the same request transaction.
/// </summary>
public interface IAccountProvisioningService
{
    Task<PersonalAccountProvisioningResult> ProvisionPersonalAccountAsync(
        Guid userId,
        string displayName,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}
