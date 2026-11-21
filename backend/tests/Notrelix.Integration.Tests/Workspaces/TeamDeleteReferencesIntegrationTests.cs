using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppConflict = Notrelix.Application.Common.Exceptions.ConflictException;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Deleting a Team that Spaces still list is refused (LD-16, stricter answer) with the stable code
/// <c>workspaces.team.still-referenced</c>, through the real pipeline on PostgreSQL. The refusal names only the
/// Spaces the caller can see and says "and others you cannot see" with no number when some are hidden; the Owner
/// or Admin removes the entries first; a Team nobody lists deletes; restore is always allowed and the members
/// return. A Board grant of a Team is not part of the check (named gap: a subject-keyed Governance read, 0.2).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class TeamDeleteReferencesIntegrationTests : IAsyncLifetime
{
    private const string Code = "workspaces.team.still-referenced";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public TeamDeleteReferencesIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedTeamAsync(OrgStack stack)
    {
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        return ((Result<Guid>)(await OrgTreeTestHost.SendAsync(owner, new CreateTeamCommand(stack.WorkspaceId, "Platform", null)))!).Data;
    }

    private Task<Guid> SeedSpaceListingAsync(OrgStack stack, string name, Guid teamId, SpaceVisibility visibility, bool archived = false) =>
        _host.SeedSpaceAsync(stack, name, visibility, stack.OwnerId, transition: space =>
        {
            space.AddMember(SpaceMemberSubjectType.Team, teamId, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
            if (archived) space.Archive(stack.OwnerId, OrgTreeTestHost.BaseTime, SpaceContentFact.Empty);
        });

    private Task<long> VersionAsync(Guid teamId) =>
        _host.ReadAsync(c => c.Teams.Where(t => t.Id == teamId).Select(t => t.Version).SingleAsync());

    private Task<bool> DeletedAsync(Guid teamId) =>
        _host.ReadAsync(c => c.Teams.IgnoreQueryFilters().Where(t => t.Id == teamId).Select(t => t.DeletedAt != null).SingleAsync());

    private static async Task<AppConflict> RefusedAsync(ServiceProvider provider, Guid workspaceId, Guid teamId, long version)
    {
        var delete = () => OrgTreeTestHost.SendAsync(provider, new DeleteTeamCommand(workspaceId, teamId, version));
        var refusal = (await delete.Should().ThrowAsync<AppConflict>()).Which;
        refusal.ErrorCode.Should().Be(Code);
        return refusal;
    }

    private static IReadOnlyList<TeamReferenceDto> References(AppConflict refusal) =>
        (IReadOnlyList<TeamReferenceDto>)refusal.Extensions["references"]!;

    [Fact]
    public async Task ATeamNoSpaceLists_IsDeleted_AndRestoreBringsItBackWithItsMembers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var teammate = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        var teamId = await SeedTeamAsync(stack);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await OrgTreeTestHost.SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, teammate, "Member", await VersionAsync(teamId)));

        await OrgTreeTestHost.SendAsync(owner, new DeleteTeamCommand(stack.WorkspaceId, teamId, await VersionAsync(teamId)));
        (await DeletedAsync(teamId)).Should().BeTrue();

        await OrgTreeTestHost.SendAsync(owner, new RestoreTeamCommand(stack.WorkspaceId, teamId));
        (await DeletedAsync(teamId)).Should().BeFalse("restore is always allowed");
        (await _host.ReadAsync(c => c.TeamMembers.CountAsync(m => m.TeamId == teamId && m.Status == TeamMemberStatus.Active)))
            .Should().Be(2, "the Lead who made the Team and the teammate return with it");
    }

    [Fact]
    public async Task ATeamASpaceStillLists_IsRefusedWithTheCode_ListsTheSpace_AndStaysUndeleted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var teamId = await SeedTeamAsync(stack);
        var space = await SeedSpaceListingAsync(stack, "Engineering", teamId, SpaceVisibility.Workspace);
        var version = await VersionAsync(teamId);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var refusal = await RefusedAsync(owner, stack.WorkspaceId, teamId, version);

        var listed = References(refusal);
        listed.Should().ContainSingle().Which.Should().BeEquivalentTo(new TeamReferenceDto("space", space, "Engineering", "Folder"));
        refusal.Extensions["moreReferencesHidden"].Should().Be(false);
        (await DeletedAsync(teamId)).Should().BeFalse();
        (await VersionAsync(teamId)).Should().Be(version, "a refused delete changes nothing");
    }

    [Fact]
    public async Task AnArchivedSpaceThatListsTheTeam_AlsoRefuses()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var teamId = await SeedTeamAsync(stack);
        await SeedSpaceListingAsync(stack, "Old project", teamId, SpaceVisibility.Workspace, archived: true);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var refusal = await RefusedAsync(owner, stack.WorkspaceId, teamId, await VersionAsync(teamId));

        References(refusal).Should().ContainSingle("it grants again when the Space is unarchived");
    }

    [Fact]
    public async Task RemovingTheEntriesFirst_LetsTheTeamBeDeleted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var teamId = await SeedTeamAsync(stack);
        var space = await SeedSpaceListingAsync(stack, "Engineering", teamId, SpaceVisibility.Workspace);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        await RefusedAsync(owner, stack.WorkspaceId, teamId, await VersionAsync(teamId));

        await OrgTreeTestHost.SendAsync(owner, new RemoveSpaceMemberCommand(stack.WorkspaceId, space, "Team", teamId, await _host.SpaceVersionAsync(space)));
        await OrgTreeTestHost.SendAsync(owner, new DeleteTeamCommand(stack.WorkspaceId, teamId, await VersionAsync(teamId)));

        (await DeletedAsync(teamId)).Should().BeTrue();
    }

    [Fact]
    public async Task TheRefusalListsOnlySpacesTheCallerCanSee_AndSaysThereAreOthersWithoutANumber()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var clerk = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Clerk");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, clerk, PermissionAction.ManageTeams, PermissionEffect.Allow);
        var teamId = await SeedTeamAsync(stack);
        var open = await SeedSpaceListingAsync(stack, "Engineering", teamId, SpaceVisibility.Workspace);
        await SeedSpaceListingAsync(stack, "Board of directors", teamId, SpaceVisibility.Private);
        await SeedSpaceListingAsync(stack, "Salary review", teamId, SpaceVisibility.Private);
        using var asClerk = _host.CreateProvider(stack.AccountId, clerk);

        var refusal = await RefusedAsync(asClerk, stack.WorkspaceId, teamId, await VersionAsync(teamId));

        References(refusal).Select(r => r.Id).Should().Equal(open);
        refusal.Extensions["moreReferencesHidden"].Should().Be(true);
        refusal.Extensions.Keys.Should().BeEquivalentTo(["references", "moreReferencesHidden"], "no count of the hidden Spaces is sent");
        System.Text.Json.JsonSerializer.Serialize(refusal.Extensions).Should().NotContain("Board of directors").And.NotContain("Salary review");
    }

    [Fact]
    public async Task WhenEveryReferencingSpaceIsHidden_TheListIsEmpty_ButTheRefusalStillHoldsAndSaysThereAreOthers()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var clerk = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Clerk");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, clerk, PermissionAction.ManageTeams, PermissionEffect.Allow);
        var teamId = await SeedTeamAsync(stack);
        await SeedSpaceListingAsync(stack, "Board of directors", teamId, SpaceVisibility.Private);
        using var asClerk = _host.CreateProvider(stack.AccountId, clerk);

        var refusal = await RefusedAsync(asClerk, stack.WorkspaceId, teamId, await VersionAsync(teamId));

        References(refusal).Should().BeEmpty();
        refusal.Extensions["moreReferencesHidden"].Should().Be(true);
        (await DeletedAsync(teamId)).Should().BeFalse("what the caller cannot see still blocks the delete");
    }

    [Fact]
    public async Task APlainMember_CannotDeleteATeam_AndTheCodeIsNotReachedByAnOutsider()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain member");
        var outsider = await _host.AddOutsiderAsync(stack);
        var teamId = await SeedTeamAsync(stack);
        await SeedSpaceListingAsync(stack, "Engineering", teamId, SpaceVisibility.Workspace);
        var version = await VersionAsync(teamId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asOutsider = _host.CreateProvider(stack.AccountId, outsider);

        var byMember = () => OrgTreeTestHost.SendAsync(asMember, new DeleteTeamCommand(stack.WorkspaceId, teamId, version));
        await byMember.Should().ThrowAsync<AppForbidden>("the refusal body is never shown to someone without ManageTeams");

        var byOutsider = () => OrgTreeTestHost.SendAsync(asOutsider, new DeleteTeamCommand(stack.WorkspaceId, teamId, version));
        await byOutsider.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>("an outsider learns nothing");
        (await DeletedAsync(teamId)).Should().BeFalse();
    }
}
