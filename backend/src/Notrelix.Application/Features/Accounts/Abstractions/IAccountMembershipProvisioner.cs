namespace Notrelix.Application.Features.Accounts.Abstractions;

public interface IAccountMembershipProvisioner
{
    Task EnsureWorkspaceInviteeAccountMembershipAsync(
        Guid accountId,
        Guid userId,
        Guid invitedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
