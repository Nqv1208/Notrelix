using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WSP-026 / GOV-028 through the real pipeline on PostgreSQL: an invitation naming
/// both Team and Space entries needs both ManageTeams and ManageSpaces, so a custom
/// rule granting only one of them does not authorize the mixed invitation, and the
/// handler never runs for a denied request. Admin is no longer invitable.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class InvitationMixedEntriesAuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public InvitationMixedEntriesAuthorizationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static void RegisterInvitationServices(IServiceCollection services)
    {
        var tokens = new Mock<IOneTimeTokenService>();
        tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation)).Returns(() =>
            new IssuedOneTimeToken("raw", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), 1));
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        services.AddSingleton(tokens.Object);
        services.AddSingleton(encryptor.Object);
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<InvitationInitialEntries>();
    }

    private async Task<(OrgStack Stack, Guid MemberId, Guid TeamId, Guid SpaceId)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain member");
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = ((Result<Guid>)(await OrgTreeTestHost.SendAsync(owner, new CreateTeamCommand(stack.WorkspaceId, "Platform", null)))!).Data;
        var spaceId = await _host.SeedSpaceAsync(stack, "Engineering", SpaceVisibility.Workspace, stack.OwnerId);
        return (stack, member, teamId, spaceId);
    }

    private static InviteMemberCommand Invite(OrgStack stack, string email, Guid? teamId, Guid? spaceId)
    {
        var entries = new List<InvitationInitialEntryInput>();
        if (teamId is { } t) entries.Add(new InvitationInitialEntryInput("Team", t, "Member"));
        if (spaceId is { } s) entries.Add(new InvitationInitialEntryInput("Space", s, "Viewer"));
        return new InviteMemberCommand(stack.WorkspaceId, email, WorkspaceRole.Member, entries);
    }

    private Task<int> InvitationCountAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.CountAsync(i => i.WorkspaceId == stack.WorkspaceId));

    [Fact]
    public async Task MixedInvitation_WithOnlyManageTeamsRule_IsDenied_AndPersistsNothing()
    {
        var (stack, member, teamId, spaceId) = await SeedAsync();
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageTeams, PermissionEffect.Allow);
        using var asMember = _host.CreateProvider(stack.AccountId, member, RegisterInvitationServices);

        var act = () => OrgTreeTestHost.SendAsync(asMember, Invite(stack, "mixed@example.com", teamId, spaceId));

        await act.Should().ThrowAsync<AppForbidden>("the Space entry also needs ManageSpaces");
        (await InvitationCountAsync(stack)).Should().Be(0);
    }

    [Fact]
    public async Task MixedInvitation_WithOnlyManageSpacesRule_IsDenied()
    {
        var (stack, member, teamId, spaceId) = await SeedAsync();
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageSpaces, PermissionEffect.Allow);
        using var asMember = _host.CreateProvider(stack.AccountId, member, RegisterInvitationServices);

        var act = () => OrgTreeTestHost.SendAsync(asMember, Invite(stack, "mixed@example.com", teamId, spaceId));

        await act.Should().ThrowAsync<AppForbidden>("the Team entry also needs ManageTeams");
        (await InvitationCountAsync(stack)).Should().Be(0);
    }

    [Fact]
    public async Task MixedInvitation_WithBothRules_IsAllowed()
    {
        var (stack, member, teamId, spaceId) = await SeedAsync();
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageTeams, PermissionEffect.Allow);
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageSpaces, PermissionEffect.Allow);
        using var asMember = _host.CreateProvider(stack.AccountId, member, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asMember, Invite(stack, "mixed@example.com", teamId, spaceId));

        (await InvitationCountAsync(stack)).Should().Be(1);
    }

    [Fact]
    public async Task SingleKindInvitations_NeedOnlyTheirOwnAction()
    {
        var (stack, member, teamId, spaceId) = await SeedAsync();
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, member, PermissionAction.ManageTeams, PermissionEffect.Allow);
        using var asMember = _host.CreateProvider(stack.AccountId, member, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asMember, Invite(stack, "team-only@example.com", teamId, null));
        var spaceOnly = () => OrgTreeTestHost.SendAsync(asMember, Invite(stack, "space-only@example.com", null, spaceId));

        await spaceOnly.Should().ThrowAsync<AppForbidden>();
        (await InvitationCountAsync(stack)).Should().Be(1);
    }

    [Fact]
    public async Task DenyRuleOnManageSpaces_BlocksAnAdminsMixedInvitation()
    {
        var (stack, _, teamId, spaceId) = await SeedAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Workspace admin");
        await _host.SeedRuleAsync(stack, PermissionSubjectType.User, admin, PermissionAction.ManageSpaces, PermissionEffect.Deny);
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin, RegisterInvitationServices);

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, Invite(stack, "mixed@example.com", teamId, spaceId));

        await act.Should().ThrowAsync<AppForbidden>();
        (await InvitationCountAsync(stack)).Should().Be(0);
    }

    [Fact]
    public async Task WorkspaceAdmin_MixedInvitation_IsAllowedByDefault()
    {
        var (stack, _, teamId, spaceId) = await SeedAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Workspace admin");
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asAdmin, Invite(stack, "mixed@example.com", teamId, spaceId));

        (await InvitationCountAsync(stack)).Should().Be(1);
    }

    [Fact]
    public async Task AdminInvitation_IsRejectedWithAStableRuleCode_AndPersistsNothing()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId, RegisterInvitationServices);

        var act = () => OrgTreeTestHost.SendAsync(owner, new InviteMemberCommand(stack.WorkspaceId, "admin@example.com", WorkspaceRole.Admin));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin);
        (await InvitationCountAsync(stack)).Should().Be(0);
    }
}
