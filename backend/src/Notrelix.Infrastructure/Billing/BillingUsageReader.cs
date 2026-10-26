using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Billing;

/// <summary>
/// Account-wide usage counts for Billing. The counts come from definer functions that return
/// only numbers and only for a workspace the caller holds, because row-level security alone
/// would hide the account's other workspaces from a workspace-level caller.
/// </summary>
public sealed class BillingUsageReader : IBillingUsageReader
{
    private readonly ApplicationDbContext _context;

    public BillingUsageReader(ApplicationDbContext context) => _context = context;

    public async Task<BillingAccountUsage> GetAccountUsageAsync(
        Guid accountId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<UsageRow>($"""
                SELECT member_seats, guest_seats, storage_bytes
                FROM ops.billing_account_usage({accountId}, {workspaceId})
                """)
            .ToListAsync(cancellationToken);

        var row = rows.SingleOrDefault();
        return row is null
            ? new BillingAccountUsage(0, 0, 0)
            : new BillingAccountUsage(row.MemberSeats, row.GuestSeats, row.StorageBytes);
    }

    public async Task<bool> HasSeatAsync(
        Guid accountId, Guid workspaceId, Guid userId, bool guest, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<bool>($"SELECT ops.billing_has_seat({accountId}, {workspaceId}, {userId}, {guest}) AS \"Value\"")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId, CancellationToken cancellationToken)
        => _context.AccountMembers.AsNoTracking().AnyAsync(m =>
            m.AccountId == accountId
            && m.UserId == userId
            && m.Role == AccountRole.Owner
            && m.Status == AccountMemberStatus.Active, cancellationToken);

    private sealed class UsageRow
    {
        public int MemberSeats { get; set; }
        public int GuestSeats { get; set; }
        public long StorageBytes { get; set; }
    }
}
