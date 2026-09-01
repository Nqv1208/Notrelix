using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Commands;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Members.Services;

/// <summary>
/// Producer-owned implementation of the Accounts public membership target
/// action. Owns the Account-side membership mutation; runs inside the caller's
/// request transaction under BOUND-TX-002 (see contract documentation).
/// </summary>
public sealed class AccountMembershipActions : IAccountMembershipActions
{
    private readonly IAccountDbContext _context;
    private readonly IAccessGrantProjectionService _grantProjection;

    public AccountMembershipActions(IAccountDbContext context, IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _grantProjection = grantProjection;
    }

    public async Task EnsureWorkspaceInviteeMembershipAsync(
        Guid accountId,
        Guid userId,
        Guid invitedBy,
        DateTimeOffset now,
        CancellationToken ct)
    {
        // Track pending Adds too so a repeated call inside one request
        // transaction stays an idempotent no-op as the public contract
        // promises; a DB query alone cannot see unsaved adds.
        var member = _context.AccountMembers.Local
            .FirstOrDefault(m => m.AccountId == accountId && m.UserId == userId)
            ?? await _context.AccountMembers
            .FirstOrDefaultAsync(m =>
                m.AccountId == accountId &&
                m.UserId == userId,
                ct);

        if (member is null)
        {
            var accountMember = AccountMember.Create(
                accountId,
                userId,
                AccountRole.Member,
                invitedBy,
                now);

            _context.AccountMembers.Add(accountMember);
            await _grantProjection.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Member, now, ct);
            return;
        }

        if (member.Status == AccountMemberStatus.Active)
            return;

        throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
            "Accounts_Membership_NotActive",
            "This user cannot join the workspace because their account membership is not active.");
    }
}
