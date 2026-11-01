using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Placements.Commands.RebuildWorkspacePlacements;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaceMembership;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WG-DEBT-002 (accepted 2026-10-23) through the real pipeline on PostgreSQL: the five changed
/// baseline rows (InviteMember, RemoveMember, ManageWorkspaceSettings, CreateBoard at the
/// workspace root) and the guards they depend on (a non-Owner cannot assign Owner or Admin and
/// cannot remove, suspend or activate an Owner or an Admin), plus ViewMembers denied for a Guest
/// with the guest-safe own-membership read and shared-resource names that replace the list.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceRoleBaselineIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceRoleBaselineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(new Moq.Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);
        services.AddScoped<AccessGrantProjectionService>();
        services.AddScoped<IWorkspaceGrantProjectionService, WorkspaceGrantProjectionServiceAdapter>();
        services.AddScoped<IWorkspaceOwnerUpdateLocker, WorkspaceOwnerUpdateLocker>();
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var source = new Mock<IWorkItemProjectionSourceAdapter>();
        source.Setup(s => s.GetWorkspacePlacementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkItemPlacementSnapshot>());
        services.AddSingleton(source.Object);
        services.AddScoped<Notrelix.Application.Features.Analytics.Placements.Services.WorkspaceWorkItemPlacementService>();

        var tokens = new Mock<IOneTimeTokenService>();
        tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation)).Returns(() =>
            new IssuedOneTimeToken("raw", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), 1));
        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        services.AddSingleton(tokens.Object);
        services.AddSingleton(encryptor.Object);
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<InvitationInitialEntries>();
        services.AddSingleton(AlwaysAdmitSeatGate);
        services.AddScoped<Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipFacts, Notrelix.Application.Features.Accounts.Members.AccountMembershipFactsProvider>();
    }

    private static readonly Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate AlwaysAdmitSeatGate =
        Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g =>
            g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true));

    private async Task<(OrgStack Stack, Guid Admin, Guid Member, Guid Guest)> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        return (stack, admin, member, guest);
    }

    private async Task<Guid> NewUserAsync(OrgStack stack)
    {
        // An Account member who is not yet in the Workspace.
        return await _host.AddOutsiderAsync(stack);
    }

    private Task<WorkspaceMember> MemberRowAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.WorkspaceMembers.SingleAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId));

    private Task<bool> HasMemberRowAsync(OrgStack stack, Guid userId) =>
        _host.ReadAsync(c => c.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == userId));

    private ServiceProvider As(OrgStack stack, Guid userId) => _host.CreateProvider(stack.AccountId, userId, RegisterServices);

    // ── InviteMember: Owner + Admin; non-Owner assigns only Member or Guest ──

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Admin_AddsMemberOrGuest(WorkspaceRole role)
    {
        var (stack, admin, _, _) = await SeedAsync();
        var user = await NewUserAsync(stack);
        using var asAdmin = As(stack, admin);

        await OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, user, role));

        (await MemberRowAsync(stack, user)).Role.Should().Be(role);
    }

    [Fact]
    public async Task Admin_CannotAddUserOfAnotherAccountOrUnknownUser_AndNothingIsPersisted()
    {
        var (stack, admin, _, _) = await SeedAsync();
        var otherAccountUser = (await _host.SeedWorkspaceAsync()).OwnerId;
        using var asAdmin = As(stack, admin);

        foreach (var target in new[] { otherAccountUser, Guid.CreateVersion7() })
        {
            var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, target, WorkspaceRole.Member));

            (await act.Should().ThrowAsync<DomainBusinessRuleException>())
                .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_NotInAccount);
            (await HasMemberRowAsync(stack, target)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Admin_CannotAddMember_WhenNoSeatIsAvailable_AndNothingIsPersisted()
    {
        var (stack, admin, _, _) = await SeedAsync();
        var user = await NewUserAsync(stack);
        var noSeat = Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g =>
            g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(false));
        using var asAdmin = _host.CreateProvider(stack.AccountId, admin, s => { RegisterServices(s); s.AddSingleton(noSeat); });

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, user, WorkspaceRole.Member));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        (await HasMemberRowAsync(stack, user)).Should().BeFalse();
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task Admin_CannotAddOwnerOrAdmin_AndNothingIsPersisted(WorkspaceRole role)
    {
        var (stack, admin, _, _) = await SeedAsync();
        var user = await NewUserAsync(stack);
        using var asAdmin = As(stack, admin);

        var act = () => OrgTreeTestHost.SendAsync(asAdmin, new AddMemberCommand(stack.WorkspaceId, user, role));

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
        (await HasMemberRowAsync(stack, user)).Should().BeFalse();
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    public async Task MemberAndGuest_CannotAddMembers(string actorRole)
    {
        var (stack, _, member, guest) = await SeedAsync();
        var user = await NewUserAsync(stack);
        using var asActor = As(stack, actorRole == "Member" ? member : guest);

        var act = () => OrgTreeTestHost.SendAsync(asActor, new AddMemberCommand(stack.WorkspaceId, user, WorkspaceRole.Member));

        await act.Should().ThrowAsync<AppForbidden>();
        (await HasMemberRowAsync(stack, user)).Should().BeFalse();
    }

    [Fact]
    public async Task Admin_CanIssueAnInvitationWithoutEntries_MemberCannot()
    {
        var (stack, admin, member, _) = await SeedAsync();
        using var asAdmin = As(stack, admin);
        using var asMember = As(stack, member);

        await OrgTreeTestHost.SendAsync(asAdmin, new InviteMemberCommand(stack.WorkspaceId, "new@example.com", WorkspaceRole.Member));
        var denied = () => OrgTreeTestHost.SendAsync(asMember, new InviteMemberCommand(stack.WorkspaceId, "other@example.com", WorkspaceRole.Member));

        await denied.Should().ThrowAsync<AppForbidden>();
        (await _host.ReadAsync(c => c.WorkspaceInvitations.CountAsync(i => i.WorkspaceId == stack.WorkspaceId))).Should().Be(1);
    }

    // ── RemoveMember / Suspend / Activate: Owner + Admin; Admin acts only on Member and Guest ──

    [Fact]
    public async Task Admin_SuspendsActivatesAndRemoves_MemberAndGuestTargets()
    {
        var (stack, admin, member, guest) = await SeedAsync();
        using var asAdmin = As(stack, admin);

        foreach (var target in new[] { member, guest })
        {
            await OrgTreeTestHost.SendAsync(asAdmin, new SuspendMemberCommand(stack.WorkspaceId, target, (await MemberRowAsync(stack, target)).Version));
            (await MemberRowAsync(stack, target)).Status.Should().Be(WorkspaceMemberStatus.Suspended);

            await OrgTreeTestHost.SendAsync(asAdmin, new ActivateMemberCommand(stack.WorkspaceId, target, (await MemberRowAsync(stack, target)).Version));
            (await MemberRowAsync(stack, target)).Status.Should().Be(WorkspaceMemberStatus.Active);

            await OrgTreeTestHost.SendAsync(asAdmin, new RemoveMemberCommand(stack.WorkspaceId, target, (await MemberRowAsync(stack, target)).Version));
            (await MemberRowAsync(stack, target)).Status.Should().Be(WorkspaceMemberStatus.Removed);
        }
    }

    [Fact]
    public async Task Admin_CannotRemoveSuspendOrActivate_OwnerOrAnotherAdmin()
    {
        var (stack, admin, _, _) = await SeedAsync();
        var otherAdmin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Other admin");
        using var asAdmin = As(stack, admin);

        foreach (var target in new[] { stack.OwnerId, otherAdmin })
        {
            var version = (await MemberRowAsync(stack, target)).Version;
            foreach (var request in new object[]
            {
                new RemoveMemberCommand(stack.WorkspaceId, target, version),
                new SuspendMemberCommand(stack.WorkspaceId, target, version),
                new ActivateMemberCommand(stack.WorkspaceId, target, version),
            })
            {
                var act = () => OrgTreeTestHost.SendAsync(asAdmin, request);
                (await act.Should().ThrowAsync<DomainBusinessRuleException>(request.GetType().Name))
                    .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotActOnMember);
            }

            (await MemberRowAsync(stack, target)).Status.Should().Be(WorkspaceMemberStatus.Active);
        }
    }

    [Fact]
    public async Task Owner_StillSuspendsAnAdmin_AndCannotRemoveThemselfHere()
    {
        var (stack, admin, _, _) = await SeedAsync();
        using var asOwner = As(stack, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(asOwner, new SuspendMemberCommand(stack.WorkspaceId, admin, (await MemberRowAsync(stack, admin)).Version));
        (await MemberRowAsync(stack, admin)).Status.Should().Be(WorkspaceMemberStatus.Suspended);

        var ownerVersion = (await MemberRowAsync(stack, stack.OwnerId)).Version;
        var act = () => OrgTreeTestHost.SendAsync(asOwner, new RemoveMemberCommand(stack.WorkspaceId, stack.OwnerId, ownerVersion));
        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActOnSelf, "leaving is the self path; the last Owner cannot leave");
    }

    [Fact]
    public async Task MemberAndGuest_CannotRemoveSuspendOrActivate()
    {
        var (stack, _, member, guest) = await SeedAsync();
        var target = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Target");

        var targetVersion = (await MemberRowAsync(stack, target)).Version;
        foreach (var actor in new[] { member, guest })
        {
            using var asActor = As(stack, actor);
            foreach (var request in new object[]
            {
                new RemoveMemberCommand(stack.WorkspaceId, target, targetVersion),
                new SuspendMemberCommand(stack.WorkspaceId, target, targetVersion),
                new ActivateMemberCommand(stack.WorkspaceId, target, targetVersion),
            })
            {
                var act = () => OrgTreeTestHost.SendAsync(asActor, request);
                await act.Should().ThrowAsync<AppForbidden>(request.GetType().Name);
            }
        }

        (await MemberRowAsync(stack, target)).Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    // ── ManageWorkspaceSettings: Owner + Admin ──

    [Fact]
    public async Task WorkspaceSettings_AdminAllowed_MemberAndGuestDenied()
    {
        var (stack, admin, member, guest) = await SeedAsync();

        using (var asAdmin = As(stack, admin))
        {
            await OrgTreeTestHost.SendAsync(asAdmin, new RebuildWorkspacePlacementsCommand(stack.WorkspaceId));
        }

        foreach (var actor in new[] { member, guest })
        {
            using var asActor = As(stack, actor);
            var act = () => OrgTreeTestHost.SendAsync(asActor, new RebuildWorkspacePlacementsCommand(stack.WorkspaceId));
            await act.Should().ThrowAsync<AppForbidden>();
        }
    }

    // ── CreateBoard at the workspace root: Owner, Admin, Member; Guest denied ──

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    public async Task RootBoardCreation_IsAllowedForOwnerAdminAndMember(string role)
    {
        var (stack, admin, member, _) = await SeedAsync();
        var actor = role switch { "Owner" => stack.OwnerId, "Admin" => admin, _ => member };
        using var asActor = As(stack, actor);

        var result = (Result<Guid>)(await OrgTreeTestHost.SendIdempotentAsync(
            asActor, new CreateBoardInWorkspaceCommand(stack.WorkspaceId, $"{role} board", null, null, null)))!;

        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RootBoardCreation_IsDeniedForGuest_AndCreatesNothing()
    {
        var (stack, _, _, guest) = await SeedAsync();
        using var asGuest = As(stack, guest);

        var act = () => OrgTreeTestHost.SendIdempotentAsync(
            asGuest, new CreateBoardInWorkspaceCommand(stack.WorkspaceId, "Guest board", null, null, null));

        await act.Should().ThrowAsync<AppForbidden>();
        (await _host.ReadAsync(c => c.Boards.CountAsync(b => b.WorkspaceId == stack.WorkspaceId))).Should().Be(0);
    }

    // ── ViewMembers denied for a Guest: the guest-safe reads replace the member list ──

    [Fact]
    public async Task ViewMembers_GuestIsForbidden_WhileMemberAndAdminStillListMembers()
    {
        var (stack, admin, member, guest) = await SeedAsync();

        using (var asGuest = As(stack, guest))
        {
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(asGuest, new GetWorkspaceMembersQuery(stack.WorkspaceId)))
                .Should().ThrowAsync<AppForbidden>("the member list is not guest-safe (WG-DEBT-002 item 6)");
        }

        foreach (var reader in new[] { admin, member })
        {
            using var provider = As(stack, reader);
            ((await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceMembersQuery(stack.WorkspaceId))).Items).Should().HaveCount(4);
        }
    }

    [Fact]
    public async Task ListInvitations_GuestIsForbidden_AdminAndMemberList()
    {
        var (stack, admin, member, guest) = await SeedAsync();
        using (var asAdmin = As(stack, admin))
        {
            await OrgTreeTestHost.SendAsync(asAdmin, new InviteMemberCommand(stack.WorkspaceId, "invited@example.com", WorkspaceRole.Member));
        }

        using (var asGuest = As(stack, guest))
        {
            await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(asGuest, new GetWorkspaceInvitationsQuery(stack.WorkspaceId)))
                .Should().ThrowAsync<AppForbidden>("invited emails are sensitive and a Guest does not enumerate the workspace");
        }

        foreach (var reader in new[] { admin, member })
        {
            using var provider = As(stack, reader);
            var rows = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceInvitationsQuery(stack.WorkspaceId));
            rows.Items.Should().ContainSingle().Which.Email.Should().Be("invited@example.com");
        }
    }

    [Fact]
    public async Task GetMyMembership_GuestReadsOnlyTheirOwnRow()
    {
        var (stack, _, _, guest) = await SeedAsync();
        using var asGuest = As(stack, guest);

        var me = await OrgTreeTestHost.QueryAsync(asGuest, new GetMyWorkspaceMembershipQuery(stack.WorkspaceId));

        me.UserId.Should().Be(guest);
        me.Role.Should().Be("Guest");
        me.Status.Should().Be("Active");
        me.Name.Should().Be("Org Guest");
        me.Phone.Should().BeNull();
    }

    [Fact]
    public async Task GetMyMembership_ANonMemberIsRefused()
    {
        var (stack, _, _, _) = await SeedAsync();
        var outsider = await NewUserAsync(stack);
        using var asOutsider = As(stack, outsider);

        var act = () => OrgTreeTestHost.SendAsync(asOutsider, new GetMyWorkspaceMembershipQuery(stack.WorkspaceId));

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GuestOnASharedBoard_StillSeesAssigneeAndCommentAuthorNames_WithoutTheMemberList()
    {
        var (stack, _, _, guest) = await SeedAsync();
        var board = await _host.SeedBoardAsync(stack, "Shared");
        var item = await _host.SeedAssignedItemAsync(stack, board, stack.OwnerId, "Deliverable");
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Viewer);

        void RegisterGuestReads(IServiceCollection services)
        {
            RegisterServices(services);
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.ResourceSummary.ICollaborationResourceSummary,
                Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary.PostgresCollaborationResourceSummary>();
            services.AddScoped<Notrelix.Application.Features.Collaboration.Public.Watchers.IResourceFollowers,
                Notrelix.Infrastructure.CrossContext.Collaboration.Watchers.PostgresResourceFollowers>();
            services.AddScoped<Notrelix.Application.Features.WorkManagement.Ports.Collaboration.IWorkManagementCollaborationReadPort,
                Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration.WorkManagementCollaborationReadAdapter>();
        }

        // A guest is view-only (cannot comment), so a member writes the comment and the guest reads it.
        using (var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId, RegisterGuestReads))
            await OrgTreeTestHost.SendAsync(asOwner, CreateCommentCommand.ForBoardItem(item, "Looks good", null));
        using var asGuest = _host.CreateProvider(stack.AccountId, guest, RegisterGuestReads);

        var detail = await OrgTreeTestHost.QueryAsync(asGuest, new GetBoardItemQuery(item));
        var comments = await OrgTreeTestHost.QueryAsync(asGuest, GetCommentsQuery.ForBoardItem(item));

        detail.Members.Should().ContainSingle().Which.Name.Should().Be("Org Owner");
        comments.Should().ContainSingle().Which.UserName.Should().Be("Org Owner");
        await FluentActions.Awaiting(() => OrgTreeTestHost.SendAsync(asGuest, new GetWorkspaceMembersQuery(stack.WorkspaceId)))
            .Should().ThrowAsync<AppForbidden>();
    }
}
