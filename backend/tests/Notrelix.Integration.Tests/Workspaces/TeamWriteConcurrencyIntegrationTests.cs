using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Team writes on PostgreSQL through the real pipeline (NRX-001, NRX-004,
/// GOV-028, WSP-014): a write carries the Team version it read and a stale
/// version is rejected as <c>common.precondition-failed</c> (HTTP 412) with the
/// Team unchanged; the last Lead cannot be removed or demoted; and Team
/// management is a workspace Owner/Admin capability that a plain Member lacks,
/// with the denied write leaving the Team unchanged.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class TeamWriteConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public TeamWriteConcurrencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Rename_WithCurrentVersion_Succeeds_AndStaleVersionIs412()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        var created = await VersionAsync(teamId);

        await OrgTreeTestHost.SendAsync(owner, new RenameTeamCommand(stack.WorkspaceId, teamId, "Platform Engineering", created));

        var renamed = await PersistedAsync(teamId);
        renamed.Name.Should().Be("Platform Engineering");
        renamed.Version.Should().BeGreaterThan(created, "the rename advanced the version the next write must carry");

        var stale = () => OrgTreeTestHost.SendAsync(owner, new RenameTeamCommand(stack.WorkspaceId, teamId, "Stale rename", created));
        (await stale.Should().ThrowAsync<PreconditionFailed>("the version read before the rename is stale"))
            .Which.ErrorCode.Should().Be("common.precondition-failed", "the API maps this error to 412 Precondition Failed");
        var afterStale = await PersistedAsync(teamId);
        afterStale.Name.Should().Be("Platform Engineering", "a rejected write leaves the Team unchanged");
        afterStale.Version.Should().Be(renamed.Version);

        var versionless = () => OrgTreeTestHost.SendAsync(owner, new RenameTeamCommand(stack.WorkspaceId, teamId, "No version", 0));
        await versionless.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>("a write without a version is not accepted");
        (await PersistedAsync(teamId)).Name.Should().Be("Platform Engineering");
    }

    [Fact]
    public async Task RemoveTeamMember_StaleVersion_Is412_AndMemberStaysActive()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        var beforeAdd = await VersionAsync(teamId);
        await OrgTreeTestHost.SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, member, "Member", beforeAdd));
        var afterAdd = await VersionAsync(teamId);
        afterAdd.Should().BeGreaterThan(beforeAdd);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new RemoveTeamMemberCommand(stack.WorkspaceId, teamId, member, beforeAdd));
        (await stale.Should().ThrowAsync<PreconditionFailed>())
            .Which.ErrorCode.Should().Be("common.precondition-failed");
        (await MemberStatusAsync(teamId, member)).Should().Be(TeamMemberStatus.Active, "a rejected removal keeps the member");
        (await VersionAsync(teamId)).Should().Be(afterAdd);

        await OrgTreeTestHost.SendAsync(owner, new RemoveTeamMemberCommand(stack.WorkspaceId, teamId, member, afterAdd));
        (await MemberStatusAsync(teamId, member)).Should().NotBe(TeamMemberStatus.Active, "the current version removes the member");
    }

    [Fact]
    public async Task LastLead_CannotBeRemovedOrDemoted()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        var version = await VersionAsync(teamId);
        (await MemberRoleAsync(teamId, stack.OwnerId)).Should().Be(TeamMemberRole.Lead, "the creator is the first Lead");

        var remove = () => OrgTreeTestHost.SendAsync(owner, new RemoveTeamMemberCommand(stack.WorkspaceId, teamId, stack.OwnerId, version));
        (await remove.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Team_CannotRemoveLastLead);

        var demote = () => OrgTreeTestHost.SendAsync(owner, new ChangeTeamMemberRoleCommand(stack.WorkspaceId, teamId, stack.OwnerId, "Member", version));
        (await demote.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Team_CannotDowngradeLastLead);

        (await MemberStatusAsync(teamId, stack.OwnerId)).Should().Be(TeamMemberStatus.Active);
        (await MemberRoleAsync(teamId, stack.OwnerId)).Should().Be(TeamMemberRole.Lead, "a rejected rule leaves the Lead in place");
        (await VersionAsync(teamId)).Should().Be(version, "a rejected write does not advance the version");
    }

    [Fact]
    public async Task ManageTeams_DeniedForMember_AllowedForAdmin()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain member");
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Workspace admin");
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        var version = await VersionAsync(teamId);

        var denied = () => OrgTreeTestHost.SendAsync(asMember, new RenameTeamCommand(stack.WorkspaceId, teamId, "Hijacked", version));
        await denied.Should().ThrowAsync<AppForbidden>("Team management is a workspace Owner/Admin capability (GOV-028)");
        var untouched = await PersistedAsync(teamId);
        untouched.Name.Should().Be("Platform", "a denied write changes nothing");
        untouched.Version.Should().Be(version);

        await OrgTreeTestHost.SendAsync(asAdmin, new RenameTeamCommand(stack.WorkspaceId, teamId, "Platform Engineering", version));
        (await PersistedAsync(teamId)).Name.Should().Be("Platform Engineering");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<Guid> CreateTeamAsync(ServiceProvider provider, Guid workspaceId, string name) =>
        ((Result<Guid>)(await OrgTreeTestHost.SendAsync(provider, new CreateTeamCommand(workspaceId, name, null)))!).Data;

    private Task<long> VersionAsync(Guid teamId) =>
        _host.ReadAsync(c => c.Teams.Where(t => t.Id == teamId).Select(t => t.Version).SingleAsync());

    private Task<(string Name, long Version)> PersistedAsync(Guid teamId) =>
        _host.ReadAsync(async c =>
        {
            var team = await c.Teams.Where(t => t.Id == teamId).Select(t => new { t.Name, t.Version }).SingleAsync();
            return (team.Name, team.Version);
        });

    private Task<TeamMemberStatus> MemberStatusAsync(Guid teamId, Guid userId) =>
        _host.ReadAsync(c => c.TeamMembers.Where(m => m.TeamId == teamId && m.UserId == userId).Select(m => m.Status).SingleAsync());

    private Task<TeamMemberRole> MemberRoleAsync(Guid teamId, Guid userId) =>
        _host.ReadAsync(c => c.TeamMembers.Where(m => m.TeamId == teamId && m.UserId == userId).Select(m => m.Role).SingleAsync());
}
