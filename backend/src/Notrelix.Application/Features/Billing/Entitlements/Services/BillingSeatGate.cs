using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Billing.Abstractions;

namespace Notrelix.Application.Features.Billing.Entitlements.Services;

public sealed class BillingSeatGate : IBillingSeatGate
{
    private readonly IBillingCapabilityFacts _facts;
    private readonly IBillingUsageReader _usage;

    public BillingSeatGate(IBillingCapabilityFacts facts, IBillingUsageReader usage)
    {
        _facts = facts;
        _usage = usage;
    }

    public async Task<bool> CanAdmitAsync(
        Guid accountId, Guid workspaceId, Guid userId, bool asGuest, CancellationToken cancellationToken)
    {
        if (await _usage.HasSeatAsync(accountId, workspaceId, userId, asGuest, cancellationToken))
            return true;

        return await HasFreeSeatAsync(accountId, workspaceId, asGuest, cancellationToken);
    }

    public async Task<bool> HasFreeSeatAsync(
        Guid accountId, Guid workspaceId, bool asGuest, CancellationToken cancellationToken)
    {
        var fact = await _facts.GetCapabilityAsync(
            accountId,
            workspaceId,
            asGuest ? BillingCapabilityCode.GuestSeat : BillingCapabilityCode.MemberSeat,
            requestedAmount: 1,
            cancellationToken);

        return fact is { IsAvailable: true };
    }
}
