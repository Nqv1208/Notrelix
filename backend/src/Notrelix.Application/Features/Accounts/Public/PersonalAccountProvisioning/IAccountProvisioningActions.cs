namespace Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;

/// <summary>
/// Producer-owned public provisioning action for personal Account creation.
/// Owning context: Accounts — Identity calls this to provision a personal
/// Account and its owner AccountMember during registration.
///
/// BOUND-TX-004 (reviewed exception): this action currently executes inside the
/// caller's request transaction so that Identity User + personal Account +
/// owner AccountMember become visible atomically. Workflow owner: Identity
/// registration; Account mutation owner: Accounts. Extraction blocker: the seam
/// cannot become remote while this exception stands. Removal trigger:
/// Identity/Accounts physical extraction or an approved product decision
/// allowing asynchronous Account provisioning.
/// </summary>
public interface IAccountProvisioningActions
{
    /// <summary>
    /// Creates a personal Account and its owner AccountMember for the given
    /// user. The caller's request transaction commits both Identity and Accounts
    /// state atomically (BOUND-TX-004).
    /// </summary>
    Task<PersonalAccountProvisioningResult> ProvisionPersonalAccountAsync(
        Guid userId,
        string displayName,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}
