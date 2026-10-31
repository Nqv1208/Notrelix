using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

public sealed partial class DatabaseAccessTighteningTests
{
    private async Task<(Guid MemberRowId, Guid UserId)> AddMemberAsync(string name, WorkspaceRole role)
    {
        var user = await AppRoleMembers.AddAsync(
            _fixture, _a, name, role, WorkspaceMemberStatus.Active, "hash");
        var rowId = Guid.Parse(await OwnerScalarAsync(
            "SELECT id::text FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u",
            ("w", _a.WorkspaceId), ("u", user.Id)));
        return (rowId, user.Id);
    }

    private string AssignmentInsert(Guid memberRowId) =>
        "INSERT INTO governance.member_role_assignments (id, account_id, workspace_id, member_id, custom_role_id, assigned_at) " +
        $"VALUES (gen_random_uuid(), '{_a.AccountId}', '{_a.WorkspaceId}', '{memberRowId}', gen_random_uuid(), now())";

    [Fact]
    public async Task CustomRoleAssignments_AreWrittenOnlyByAWorkspaceAdmin_ForAnActiveMemberOfThatWorkspace()
    {
        var (targetRow, _) = await AddMemberAsync("Target", WorkspaceRole.Member);
        var (_, plainUser) = await AddMemberAsync("Plain", WorkspaceRole.Member);

        await AsAppAsync(plainUser, async (c, tx) =>
        {
            (await IsDeniedAsync(c, tx, AssignmentInsert(targetRow)))
                .Should().BeTrue("a plain member cannot assign roles");
        });

        await AsAppAsync(_a.User.Id, async (c, tx) =>
        {
            (await IsDeniedAsync(c, tx, AssignmentInsert(Guid.NewGuid())))
                .Should().BeTrue("the target must be a member of this workspace");
            (await IsDeniedAsync(c, tx, AssignmentInsert(targetRow)))
                .Should().BeFalse("an administrator assigns a role to an active member");
        });

        // A removed member is no longer a valid target.
        await OwnerScalarAsync("UPDATE workspace.workspace_members SET status = 'Removed' WHERE id = @i RETURNING 1", ("i", targetRow));
        await AsAppAsync(_a.User.Id, async (c, tx) =>
        {
            (await IsDeniedAsync(c, tx, AssignmentInsert(targetRow)))
                .Should().BeTrue("a removed member cannot be assigned a role");
        });
    }

    [Fact]
    public async Task CustomRoleAssignments_CannotBeDeletedOrRewrittenByAPlainMember()
    {
        var (targetRow, _) = await AddMemberAsync("Target2", WorkspaceRole.Member);
        var (_, plainUser) = await AddMemberAsync("Plain2", WorkspaceRole.Member);
        await OwnerScalarAsync(AssignmentInsert(targetRow) + " RETURNING 1");

        await AsAppAsync(plainUser, async (c, tx) =>
        {
            (await ExecAsync(c, tx, $"DELETE FROM governance.member_role_assignments WHERE workspace_id = '{_a.WorkspaceId}'"))
                .Should().Be(0, "the row is not even visible to a delete by a plain member");
            (await ExecAsync(c, tx, $"UPDATE governance.member_role_assignments SET custom_role_id = gen_random_uuid() WHERE workspace_id = '{_a.WorkspaceId}'"))
                .Should().Be(0);
        });
        (await OwnerScalarAsync("SELECT count(*) FROM governance.member_role_assignments")).Should().Be("1");
    }
}
