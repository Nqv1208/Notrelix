using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The workspace setting <c>AllowGuestInvites</c> through the real pipeline and policy engine on PostgreSQL: with it off,
/// nobody introduces a Guest by invitation (a Board or Page manager, or a workspace invitation with the Guest role) and the
/// refusal carries the stable code <c>workspace.guest_invites_off</c>; a resend of an earlier invitation is refused the same
/// way; revoking still works (closing access is never blocked); switching the setting back on lets the same invitation
/// through again. A person who could not make the request anyway keeps the plain refusal.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class GuestInvitesPolicyIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";
    private const string PageKind = "documents.page";
    private const string OffCode = "workspace.guest_invites_off";

    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public GuestInvitesPolicyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

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
        services.AddSingleton(new Mock<IAuditService>().Object);
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<IGuestResourceInvitationFacts, GuestResourceInvitationFactsAdapter>();
        services.AddScoped<IInvitationManageAuthorizer, InvitationManageAuthorizer>();
        services.AddScoped<InvitationInitialEntries>();
        services.AddSingleton(Moq.Mock.Of<Notrelix.Application.Features.Workspaces.Invitations.Services.IInvitationRateLimiter>());
    }

    private sealed record Seed(OrgStack Stack, Guid BoardId, Guid PageId, Guid Manager);

    private async Task<Seed> SeedAsync(bool guestInvites)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var manager = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Manager");
        var boardId = await _host.SeedBoardAsync(stack, "Triển khai ERP Q4");
        var pageId = await _host.SeedPageAsync(stack, "Quy trình onboarding");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, boardId, PermissionSubjectType.User, manager, PermissionLevel.Manager);
        await _host.SeedResourcePermissionAsync(stack, PageKind, pageId, PermissionSubjectType.User, manager, PermissionLevel.Manager);
        await SetGuestInvitesAsync(stack, guestInvites);
        return new Seed(stack, boardId, pageId, manager);
    }

    private Task SetGuestInvitesAsync(OrgStack stack, bool allow) => _host.SetGuestInvitesAsync(stack, allow);

    private Task<int> InvitationCountAsync(OrgStack stack) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.CountAsync(i => i.WorkspaceId == stack.WorkspaceId));

    private Task<WorkspaceInvitation> InvitationAsync(Guid id) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.SingleAsync(i => i.Id == id));

    private async Task<Guid> InviteAsync(Seed s, string kind, Guid resourceId, string email = "guest@example.com")
    {
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);
        var id = ((Result<Guid>)(await OrgTreeTestHost.SendAsync(
            asManager, new InviteGuestToResourceCommand(kind, resourceId, email, "Viewer")))!).Data;
        await _host.AgeInvitationAsync(id);
        return id;
    }

    [Fact]
    public async Task Manager_CannotInviteAGuestToABoard_WhenGuestInvitesAreOff_AndNothingIsWritten()
    {
        var s = await SeedAsync(guestInvites: false);
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asManager, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "guest@example.com", "Viewer"));

        (await invite.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(OffCode);
        (await InvitationCountAsync(s.Stack)).Should().Be(0);
    }

    [Fact]
    public async Task Manager_CannotInviteAGuestToAPage_WhenGuestInvitesAreOff_AndNothingIsWritten()
    {
        var s = await SeedAsync(guestInvites: false);
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asManager, new InviteGuestToResourceCommand(PageKind, s.PageId, "guest@example.com", "Viewer"));

        (await invite.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(OffCode);
        (await InvitationCountAsync(s.Stack)).Should().Be(0);
    }

    [Fact]
    public async Task WorkspaceOwner_CannotInviteAGuestToABoard_WhenGuestInvitesAreOff()
    {
        var s = await SeedAsync(guestInvites: false);
        using var asOwner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asOwner, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "guest@example.com", "Viewer"));

        (await invite.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(OffCode);
    }

    [Fact]
    public async Task APersonWhoCannotManageTheBoard_KeepsThePlainRefusal_WithoutLearningTheSetting()
    {
        var s = await SeedAsync(guestInvites: false);
        var plain = await _host.AddMemberAsync(s.Stack, WorkspaceRole.Member, "Plain member");
        using var asPlain = _host.CreateProvider(s.Stack.AccountId, plain, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asPlain, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "guest@example.com", "Viewer"));

        (await invite.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().BeNull();
    }

    [Fact]
    public async Task Resend_OfAnInvitationIssuedWhileOn_IsRefused_OnceGuestInvitesAreOff()
    {
        var s = await SeedAsync(guestInvites: true);
        var invitationId = await InviteAsync(s, BoardKind, s.BoardId);
        var before = await InvitationAsync(invitationId);
        await SetGuestInvitesAsync(s.Stack, false);
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);

        var resend = () => OrgTreeTestHost.SendAsync(
            asManager, new ResendResourceInvitationCommand(BoardKind, s.BoardId, invitationId));

        (await resend.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(OffCode);
        var after = await InvitationAsync(invitationId);
        after.TokenGeneration.Should().Be(before.TokenGeneration, "a refused resend keeps the old link");
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task Revoke_StillWorks_WhenGuestInvitesAreOff()
    {
        var s = await SeedAsync(guestInvites: true);
        var invitationId = await InviteAsync(s, PageKind, s.PageId);
        await SetGuestInvitesAsync(s.Stack, false);
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asManager, new RevokeResourceInvitationCommand(PageKind, s.PageId, invitationId));

        (await InvitationAsync(invitationId)).Status.Should().Be(WorkspaceInvitationStatus.Revoked);
    }

    [Fact]
    public async Task SwitchingGuestInvitesBackOn_LetsANewInvitationAndAResendThrough()
    {
        var s = await SeedAsync(guestInvites: false);
        using var asManager = _host.CreateProvider(s.Stack.AccountId, s.Manager, RegisterInvitationServices);
        var refused = () => OrgTreeTestHost.SendAsync(
            asManager, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "guest@example.com", "Viewer"));
        await refused.Should().ThrowAsync<AppForbidden>();

        await SetGuestInvitesAsync(s.Stack, true);
        var invitationId = await InviteAsync(s, BoardKind, s.BoardId);
        var before = await InvitationAsync(invitationId);
        await OrgTreeTestHost.SendAsync(
            asManager, new ResendResourceInvitationCommand(BoardKind, s.BoardId, invitationId));

        (await InvitationAsync(invitationId)).TokenGeneration.Should().Be(before.TokenGeneration + 1);
        (await InvitationCountAsync(s.Stack)).Should().Be(1);
    }

    [Fact]
    public async Task WorkspaceInvitation_WithTheGuestRole_IsRefused_WhenGuestInvitesAreOff_AndAMemberInvitationIsNot()
    {
        var s = await SeedAsync(guestInvites: false);
        using var asOwner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId, RegisterInvitationServices);

        var guest = () => OrgTreeTestHost.SendAsync(
            asOwner, new InviteMemberCommand(s.Stack.WorkspaceId, "guest@example.com", WorkspaceRole.Guest));
        await OrgTreeTestHost.SendAsync(
            asOwner, new InviteMemberCommand(s.Stack.WorkspaceId, "member@example.com", WorkspaceRole.Member));

        (await guest.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(OffCode);
        (await InvitationCountAsync(s.Stack)).Should().Be(1, "only the Member invitation was written");
    }

    [Fact]
    public async Task WorkspaceInvitation_WithTheGuestRole_IsAllowed_WhenGuestInvitesAreOn()
    {
        var s = await SeedAsync(guestInvites: true);
        using var asOwner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(
            asOwner, new InviteMemberCommand(s.Stack.WorkspaceId, "guest@example.com", WorkspaceRole.Guest));

        (await InvitationCountAsync(s.Stack)).Should().Be(1);
    }
}
