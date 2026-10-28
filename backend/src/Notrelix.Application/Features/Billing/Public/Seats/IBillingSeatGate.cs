namespace Notrelix.Application.Features.Billing.Public.Seats;

/// <summary>
/// Producer-owned seat decision. Callers ask whether one more person may join the
/// account's workspaces; they never see plans or limits. A person who already
/// holds the same kind of seat anywhere in the account needs no new seat.
/// </summary>
public interface IBillingSeatGate
{
    Task<bool> CanAdmitAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        bool asGuest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Same decision for an invitation that has no person yet: true when at least
    /// one seat of the kind is still free.
    /// </summary>
    Task<bool> HasFreeSeatAsync(
        Guid accountId,
        Guid workspaceId,
        bool asGuest,
        CancellationToken cancellationToken);
}
