using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Application.Features.Accounts.Services;

public sealed class AccountMembershipProvisioner : IAccountMembershipProvisioner
{
    private readonly IAccountDbContext _context;

    public AccountMembershipProvisioner(IAccountDbContext context)
    {
        _context = context;
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
            return;
        }

        if (member.Status == AccountMemberStatus.Active)
            return;

        throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
            "This user cannot join the workspace because their account membership is not active.");
    }
}
