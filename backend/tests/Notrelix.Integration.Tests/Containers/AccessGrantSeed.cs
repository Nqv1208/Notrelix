using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Containers;

/// <summary>
/// Seeds the access grants a person holds, as the owner. Production writes them through the grant
/// projection when a membership is created; row-level security derives every tenant read from them, so a
/// test that seeds memberships directly and then runs code on the application login must seed the grants too.
/// </summary>
internal static class AccessGrantSeed
{
    public static async Task WorkspaceMemberAsync(
        PostgresTestContainer db, Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = db.CreateOwnerContext(tenant);
        var projection = new AccessGrantProjectionService(context);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspaceId, userId, role, DateTimeOffset.UtcNow, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    public static async Task AccountMemberAsync(
        PostgresTestContainer db, Guid accountId, Guid userId, AccountRole role)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = db.CreateOwnerContext(tenant);
        var projection = new AccessGrantProjectionService(context);
        await projection.SyncAccountMemberGrantAsync(
            accountId, userId, role, DateTimeOffset.UtcNow, CancellationToken.None);
        await context.SaveChangesAsync();
    }
}
