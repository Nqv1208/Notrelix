using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Members.Services;

public sealed class AccountMembershipProvisioner : IAccountMembershipProvisioner
{
    private readonly IAccountDbContext _context;
    private readonly IAccessGrantProjectionService _grantProjection;

    public AccountMembershipProvisioner(IAccountDbContext context, IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _grantProjection = grantProjection;
    }

    public async Task EnsureWorkspaceInviteeAccountMembershipAsync(
        Guid accountId,
        Guid userId,
        Guid invitedBy,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var member = await _context.AccountMembers
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
