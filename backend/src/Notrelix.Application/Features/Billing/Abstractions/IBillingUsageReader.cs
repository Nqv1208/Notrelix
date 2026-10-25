namespace Notrelix.Application.Features.Billing.Abstractions;

/// <summary>Account-wide usage counts that Billing meters.</summary>
public sealed record BillingAccountUsage(int MemberSeats, int GuestSeats, long StorageBytes);

/// <summary>
/// Read-only account-wide usage facts that Billing meters but other contexts own.
/// Counts always sum across every workspace of the account (account-scoped
/// entitlements, account-wide usage) and are only returned for a workspace the
/// caller holds. Implemented by Infrastructure.
/// </summary>
public interface IBillingUsageReader
{
    /// <summary>
    /// Distinct active non-guest members, distinct active guests and stored attachment bytes
    /// (not yet purged) across the account's workspaces.
    /// </summary>
    Task<BillingAccountUsage> GetAccountUsageAsync(
        Guid accountId, Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>True when the user already holds the kind of seat in any workspace of the account.</summary>
    Task<bool> HasSeatAsync(
        Guid accountId, Guid workspaceId, Guid userId, bool guest, CancellationToken cancellationToken);

    /// <summary>True when the user is an active Owner of the account.</summary>
    Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId, CancellationToken cancellationToken);
}
