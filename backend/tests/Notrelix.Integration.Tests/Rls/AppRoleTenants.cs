using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>One tenant of the application-role suites: a user who owns an account, a workspace and a board.</summary>
internal sealed record AppRoleTenant(
    User User,
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    string InvitationToken,
    string InvitationTokenHash,
    int InvitationHashVersion);

/// <summary>Seeds tenants as the owner so suites can assert what the application role does and does not see.</summary>
internal static class AppRoleTenants
{
    public static async Task<AppRoleTenant> SeedAsync(
        AppRoleTestContainer fixture, string email, string name, string passwordHash)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(email, name, passwordHash, now, hasPasswordCredential: true);
        user.ConfirmEmail(user.Id, now);
        var account = Account.Create(
            $"{name} Co", $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}"[..20], AccountType.Team, user.Id, now);
        var workspace = Workspace.Create(account.Id, user.Id, $"{name} WS", $"ws-{Guid.NewGuid():N}"[..16], now);
        var board = Board.Create(account.Id, workspace.Id, user.Id, $"{name} Roadmap", null, now);
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = fixture.CreateOwnerContext(tenant);
        ctx.Users.Add(user);
        ctx.Accounts.Add(account);
        ctx.Workspaces.Add(workspace);
        ctx.Boards.Add(board);
        ctx.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, user.Id, WorkspaceRole.Owner, user.Id, now));
        ctx.AccessGrants.Add(new AccessGrant(
            accountId: account.Id, workspaceId: workspace.Id, userId: user.Id, sourceContext: "Workspace",
            membershipStatus: "Active", roleCodes: ["Owner"], permissionCodes: [], isAccountAdmin: false,
            isWorkspaceAdmin: true, grantedAt: now));
        ctx.WorkspaceInvitations.Add(WorkspaceInvitation.Create(
            account.Id, workspace.Id, $"invitee-{Guid.NewGuid():N}@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion, user.Id, now.AddMinutes(-5)));
        await ctx.SaveChangesAsync();

        return new AppRoleTenant(user, account.Id, workspace.Id, board.Id, issued.RawToken, issued.TokenHash, issued.HashVersion);
    }
}
