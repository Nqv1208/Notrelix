using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;
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
/// is denied, and a revoked invitation is dead.
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
            asIssuer, new InviteGuestToResourceCommand(BoardKind, boardId, "guest@example.com", "Editor")))!).Data;
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
}
