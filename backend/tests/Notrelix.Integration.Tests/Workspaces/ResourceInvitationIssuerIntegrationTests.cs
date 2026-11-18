using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.ListResourceInvitations;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WSP-026 through the real pipeline on PostgreSQL: the issuer of a resource
/// invitation may resend and revoke their own while they still manage the
/// resource; another manager cannot touch it, an issuer who lost the permission
/// is denied, and a revoked invitation is dead. A Workspace Owner or Admin acts on
/// anyone's invitation through the workspace resend and cancel endpoints, a plain
/// board manager does not, and an outsider or another workspace reaches nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ResourceInvitationIssuerIntegrationTests : IAsyncLifetime
{
    private const string BoardKind = "work-management.board";
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public ResourceInvitationIssuerIntegrationTests(PostgresTestContainer db)
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
    }

    private sealed record Seed(OrgStack Stack, Guid BoardId, Guid Issuer, Guid OtherManager, Guid InvitationId);

    private async Task<Seed> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var issuer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Issuer");
        var other = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Other manager");
        var boardId = await _host.SeedBoardAsync(stack, "Triển khai ERP Q4");
        await _host.SeedResourcePermissionAsync(stack, BoardKind, boardId, PermissionSubjectType.User, issuer, PermissionLevel.Manager);
        await _host.SeedResourcePermissionAsync(stack, BoardKind, boardId, PermissionSubjectType.User, other, PermissionLevel.Manager);

        using var asIssuer = _host.CreateProvider(stack.AccountId, issuer, RegisterInvitationServices);
        var invitationId = ((Result<Guid>)(await OrgTreeTestHost.SendAsync(
            asIssuer, new InviteGuestToResourceCommand(BoardKind, boardId, "guest@example.com", "Viewer")))!).Data;
        await _host.AgeInvitationAsync(invitationId);
        return new Seed(stack, boardId, issuer, other, invitationId);
    }

    private Task<WorkspaceInvitation> InvitationAsync(Guid id) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.SingleAsync(i => i.Id == id));

    [Fact]
    public async Task Issuer_ResendsOwnInvitation_WithAFreshToken()
    {
        var s = await SeedAsync();
        var before = await InvitationAsync(s.InvitationId);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asIssuer, new ResendResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        var after = await InvitationAsync(s.InvitationId);
        after.TokenGeneration.Should().Be(before.TokenGeneration + 1);
        after.Token.Value.Should().NotBe(before.Token.Value, "the old link must stop working");
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task Issuer_RevokesOwnInvitation_AndItCanNoLongerBeResentOrAccepted()
    {
        var s = await SeedAsync();
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asIssuer, new RevokeResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        var revoked = await InvitationAsync(s.InvitationId);
        revoked.Status.Should().Be(WorkspaceInvitationStatus.Revoked);
        FluentActions.Invoking(() => revoked.Accept(Guid.NewGuid(), OrgTreeTestHost.BaseTime))
            .Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        var resend = () => OrgTreeTestHost.SendAsync(
            asIssuer, new ResendResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));
        await resend.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task AnotherManager_CannotRevokeOrResend_AndTheInvitationStaysPending()
    {
        var s = await SeedAsync();
        var before = await InvitationAsync(s.InvitationId);
        using var asOther = _host.CreateProvider(s.Stack.AccountId, s.OtherManager, RegisterInvitationServices);

        var revoke = () => OrgTreeTestHost.SendAsync(asOther, new RevokeResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));
        var resend = () => OrgTreeTestHost.SendAsync(asOther, new ResendResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        await revoke.Should().ThrowAsync<AppNotFound>();
        await resend.Should().ThrowAsync<AppNotFound>();
        var after = await InvitationAsync(s.InvitationId);
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        after.TokenGeneration.Should().Be(before.TokenGeneration);
    }

    [Fact]
    public async Task PlainMember_CannotRevokeOrResend()
    {
        var s = await SeedAsync();
        var plain = await _host.AddMemberAsync(s.Stack, WorkspaceRole.Member, "Plain member");
        using var asPlain = _host.CreateProvider(s.Stack.AccountId, plain, RegisterInvitationServices);

        var revoke = () => OrgTreeTestHost.SendAsync(asPlain, new RevokeResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));
        var resend = () => OrgTreeTestHost.SendAsync(asPlain, new ResendResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        await revoke.Should().ThrowAsync<AppForbidden>();
        await resend.Should().ThrowAsync<AppForbidden>();
        (await InvitationAsync(s.InvitationId)).Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    private Task<int> InvitationCountAsync(Guid workspaceId) =>
        _host.ReadAsync(c => c.WorkspaceInvitations.CountAsync(i => i.WorkspaceId == workspaceId));

    [Fact]
    public async Task PlainMember_CannotInviteAGuest_AndNothingIsWritten()
    {
        var s = await SeedAsync();
        var before = await InvitationCountAsync(s.Stack.WorkspaceId);
        var plain = await _host.AddMemberAsync(s.Stack, WorkspaceRole.Member, "Plain member");
        using var asPlain = _host.CreateProvider(s.Stack.AccountId, plain, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asPlain, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "outsider@example.com", "Viewer"));

        await invite.Should().ThrowAsync<AppForbidden>();
        (await InvitationCountAsync(s.Stack.WorkspaceId)).Should().Be(before);
    }

    [Fact]
    public async Task Manager_CannotInviteAGuestAboveViewer_AndNothingIsWritten()
    {
        var s = await SeedAsync();
        var before = await InvitationCountAsync(s.Stack.WorkspaceId);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asIssuer, new InviteGuestToResourceCommand(BoardKind, s.BoardId, "outsider@example.com", "Editor"));

        (await invite.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Invitation_ResourceLevelAboveViewer);
        (await InvitationCountAsync(s.Stack.WorkspaceId)).Should().Be(before);
    }

    [Fact]
    public async Task Manager_CannotInviteAGuestToAnArchivedBoard_AndNothingIsWritten()
    {
        var s = await SeedAsync();
        var archived = await _host.SeedBoardAsync(
            s.Stack, "Archived board", transition: board => board.Archive(s.Stack.OwnerId, OrgTreeTestHost.BaseTime));
        await _host.SeedResourcePermissionAsync(s.Stack, BoardKind, archived, PermissionSubjectType.User, s.Issuer, PermissionLevel.Manager);
        var before = await InvitationCountAsync(s.Stack.WorkspaceId);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        var invite = () => OrgTreeTestHost.SendAsync(
            asIssuer, new InviteGuestToResourceCommand(BoardKind, archived, "outsider@example.com", "Viewer"));

        // The pipeline refuses the write with the reason (S3): the archived board reads, it takes no invitation.
        (await invite.Should().ThrowAsync<AppForbidden>()).Which.Message.Should().Contain("archived");
        (await InvitationCountAsync(s.Stack.WorkspaceId)).Should().Be(before);
    }

    [Fact]
    public async Task IssuerWhoLostManagePermission_CannotResendOrRevoke()
    {
        var s = await SeedAsync();
        await _host.WithSystemContextAsync(async seed =>
        {
            var entry = await seed.ResourcePermissions.SingleAsync(
                p => p.ResourceId == s.BoardId && p.SubjectId == s.Issuer);
            entry.ChangeLevel(PermissionLevel.Editor, s.Stack.OwnerId, OrgTreeTestHost.BaseTime);
        });
        var before = await InvitationAsync(s.InvitationId);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        var resend = () => OrgTreeTestHost.SendAsync(asIssuer, new ResendResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));
        var revoke = () => OrgTreeTestHost.SendAsync(asIssuer, new RevokeResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        await resend.Should().ThrowAsync<AppForbidden>();
        await revoke.Should().ThrowAsync<AppForbidden>();
        var after = await InvitationAsync(s.InvitationId);
        after.TokenGeneration.Should().Be(before.TokenGeneration);
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task InvitationOfAnotherBoard_IsNotFoundThroughThisBoard()
    {
        var s = await SeedAsync();
        var otherBoard = await _host.SeedBoardAsync(s.Stack, "Another board");
        await _host.SeedResourcePermissionAsync(s.Stack, BoardKind, otherBoard, PermissionSubjectType.User, s.Issuer, PermissionLevel.Manager);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);

        var revoke = () => OrgTreeTestHost.SendAsync(asIssuer, new RevokeResourceInvitationCommand(BoardKind, otherBoard, s.InvitationId));

        await revoke.Should().ThrowAsync<AppNotFound>();
        (await InvitationAsync(s.InvitationId)).Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    // ── The pending list and the Owner/Admin path (UC-WM-A20) ───────────────

    private static Task<List<ResourceInvitationDto>> ListAsync(ServiceProvider provider, Seed s) =>
        OrgTreeTestHost.QueryAsync<List<ResourceInvitationDto>>(provider, new ListResourceInvitationsQuery(BoardKind, s.BoardId));

    /// <summary>A workspace Admin who also manages this board, so the Share dialog (and its list) opens for them.</summary>
    private async Task<Guid> AddBoardManagingAdminAsync(Seed s)
    {
        var admin = await _host.AddMemberAsync(s.Stack, WorkspaceRole.Admin, "Admin");
        await _host.SeedResourcePermissionAsync(s.Stack, BoardKind, s.BoardId, PermissionSubjectType.User, admin, PermissionLevel.Manager);
        return admin;
    }

    [Fact]
    public async Task List_TellsEachManagerWhichRowsTheyMayResendOrRevoke()
    {
        var s = await SeedAsync();
        var admin = await AddBoardManagingAdminAsync(s);
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);
        using var asOther = _host.CreateProvider(s.Stack.AccountId, s.OtherManager, RegisterInvitationServices);
        using var asAdmin = _host.CreateProvider(s.Stack.AccountId, admin, RegisterInvitationServices);
        using var asOwner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId, RegisterInvitationServices);

        var issuerRow = (await ListAsync(asIssuer, s)).Should().ContainSingle().Subject;
        var otherRow = (await ListAsync(asOther, s)).Should().ContainSingle().Subject;
        var adminRow = (await ListAsync(asAdmin, s)).Should().ContainSingle().Subject;
        var ownerRow = (await ListAsync(asOwner, s)).Should().ContainSingle().Subject;

        (issuerRow.IssuedByYou, issuerRow.CanManage).Should().Be((true, true));
        (otherRow.IssuedByYou, otherRow.CanManage).Should().Be((false, false), "a plain board manager manages only their own");
        (adminRow.IssuedByYou, adminRow.CanManage).Should().Be((false, true));
        (ownerRow.IssuedByYou, ownerRow.CanManage).Should().Be((false, true));
        issuerRow.Id.Should().Be(s.InvitationId);
    }

    [Fact]
    public async Task List_IsHiddenFromAPlainMemberAnOutsiderAndAnotherWorkspace()
    {
        var s = await SeedAsync();
        var plain = await _host.AddMemberAsync(s.Stack, WorkspaceRole.Member, "Plain member");
        var outsider = await _host.AddOutsiderAsync(s.Stack);
        var other = await _host.SeedWorkspaceAsync();
        using var asPlain = _host.CreateProvider(s.Stack.AccountId, plain, RegisterInvitationServices);
        using var asOutsider = _host.CreateProvider(s.Stack.AccountId, outsider, RegisterInvitationServices);
        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId, RegisterInvitationServices);

        var plainList = () => ListAsync(asPlain, s);
        var outsiderList = () => ListAsync(asOutsider, s);
        var crossList = () => ListAsync(asOtherOwner, s);

        await plainList.Should().ThrowAsync<AppForbidden>();
        await outsiderList.Should().ThrowAsync<AppNotFound>("a non-member of the workspace gets NotFound");
        await crossList.Should().ThrowAsync<AppNotFound>("another workspace cannot reach this board");
    }

    [Fact]
    public async Task List_DropsAnInvitationOnceItIsRevoked()
    {
        var s = await SeedAsync();
        using var asIssuer = _host.CreateProvider(s.Stack.AccountId, s.Issuer, RegisterInvitationServices);
        await OrgTreeTestHost.SendAsync(asIssuer, new RevokeResourceInvitationCommand(BoardKind, s.BoardId, s.InvitationId));

        (await ListAsync(asIssuer, s)).Should().BeEmpty();
    }

    [Fact]
    public async Task Owner_ResendsAnotherPersonsInvitation_ThroughTheWorkspaceEndpoint()
    {
        var s = await SeedAsync();
        var before = await InvitationAsync(s.InvitationId);
        using var asOwner = _host.CreateProvider(s.Stack.AccountId, s.Stack.OwnerId, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asOwner, new ResendInvitationCommand(s.Stack.WorkspaceId, s.InvitationId));

        var after = await InvitationAsync(s.InvitationId);
        after.TokenGeneration.Should().Be(before.TokenGeneration + 1);
        after.Token.Value.Should().NotBe(before.Token.Value, "the old link must stop working");
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        after.InvitedBy.Should().Be(s.Issuer, "a resend by the Owner does not take over the invitation");
    }

    [Fact]
    public async Task Admin_RevokesAnotherPersonsInvitation_ThroughTheWorkspaceEndpoint()
    {
        var s = await SeedAsync();
        var admin = await AddBoardManagingAdminAsync(s);
        using var asAdmin = _host.CreateProvider(s.Stack.AccountId, admin, RegisterInvitationServices);

        await OrgTreeTestHost.SendAsync(asAdmin, new CancelInvitationCommand(s.Stack.WorkspaceId, s.InvitationId));

        var revoked = await InvitationAsync(s.InvitationId);
        revoked.Status.Should().Be(WorkspaceInvitationStatus.Revoked);
        FluentActions.Invoking(() => revoked.Accept(Guid.NewGuid(), OrgTreeTestHost.BaseTime))
            .Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>("a revoked link cannot be accepted");
    }

    [Fact]
    public async Task BoardManagerWhoIsNotOwnerOrAdmin_CannotUseTheWorkspaceEndpoints()
    {
        var s = await SeedAsync();
        var before = await InvitationAsync(s.InvitationId);
        using var asOther = _host.CreateProvider(s.Stack.AccountId, s.OtherManager, RegisterInvitationServices);

        var resend = () => OrgTreeTestHost.SendAsync(asOther, new ResendInvitationCommand(s.Stack.WorkspaceId, s.InvitationId));
        var cancel = () => OrgTreeTestHost.SendAsync(asOther, new CancelInvitationCommand(s.Stack.WorkspaceId, s.InvitationId));

        await resend.Should().ThrowAsync<AppForbidden>();
        await cancel.Should().ThrowAsync<AppForbidden>();
        var after = await InvitationAsync(s.InvitationId);
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        after.TokenGeneration.Should().Be(before.TokenGeneration);
    }

    [Fact]
    public async Task OutsiderAndAnotherWorkspacesOwner_CannotResendOrCancelThroughTheWorkspaceEndpoints()
    {
        var s = await SeedAsync();
        var before = await InvitationAsync(s.InvitationId);
        var outsider = await _host.AddOutsiderAsync(s.Stack);
        var other = await _host.SeedWorkspaceAsync();
        using var asOutsider = _host.CreateProvider(s.Stack.AccountId, outsider, RegisterInvitationServices);
        using var asOtherOwner = _host.CreateProvider(other.AccountId, other.OwnerId, RegisterInvitationServices);

        var outsiderCancel = () => OrgTreeTestHost.SendAsync(asOutsider, new CancelInvitationCommand(s.Stack.WorkspaceId, s.InvitationId));
        var crossCancel = () => OrgTreeTestHost.SendAsync(asOtherOwner, new CancelInvitationCommand(other.WorkspaceId, s.InvitationId));
        var crossResend = () => OrgTreeTestHost.SendAsync(asOtherOwner, new ResendInvitationCommand(other.WorkspaceId, s.InvitationId));

        await outsiderCancel.Should().ThrowAsync<AppNotFound>();
        await crossCancel.Should().ThrowAsync<AppNotFound>("the invitation belongs to another workspace");
        await crossResend.Should().ThrowAsync<AppNotFound>();
        var after = await InvitationAsync(s.InvitationId);
        after.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        after.TokenGeneration.Should().Be(before.TokenGeneration);
    }
}
