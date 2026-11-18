using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.ListResourceInvitations;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.Resource;

/// <summary>
/// The pending list of one Board's resource invitations: only pending, unexpired invitations of this
/// workspace and this exact resource, with who may resend or revoke each row. The pipeline already
/// requires manage permission on the resource; the handler decides the per-row <c>CanManage</c>.
/// </summary>
public class ListResourceInvitationsQueryHandlerTests : WorkspaceHandlerTestBase
{
    private const string BoardKind = WorkspaceInvitationResourceGrant.BoardKind;
    private const string Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
    private static readonly Guid BoardId = Guid.CreateVersion7();

    private readonly Mock<IActorLookupService> _actors = new();

    public ListResourceInvitationsQueryHandlerTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                (IReadOnlyList<ActorSnapshot>)ids.Select(id => new ActorSnapshot(id, "Name " + id.ToString("N")[..4], null)).ToList());
    }

    private ListResourceInvitationsQueryHandler Handler() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _actors.Object);

    private WorkspaceInvitation Invitation(
        Guid invitedBy, Guid? boardId = null, Guid? workspaceId = null, string email = "guest@example.com") =>
        WorkspaceInvitation.CreateForResource(
            TestAccountId, workspaceId ?? TestWorkspaceId, email,
            new WorkspaceInvitationResourceGrant(BoardKind, boardId ?? BoardId, "Viewer"),
            InvitationTokenHash.Create(Hash), 1, invitedBy, TestNow.AddDays(-1));

    private Task<Result<List<ResourceInvitationDto>>> ListAsync() =>
        Handler().Handle(new ListResourceInvitationsQuery(BoardKind, BoardId), CancellationToken.None);

    [Fact]
    public async Task List_ReturnsOnlyPendingUnexpiredInvitationsOfThisBoardAndWorkspace()
    {
        var pending = Invitation(TestUserId, email: "pending@example.com");
        var revoked = Invitation(TestUserId, email: "revoked@example.com");
        revoked.Revoke(TestUserId, TestNow);
        var expired = WorkspaceInvitation.CreateForResource(
            TestAccountId, TestWorkspaceId, "expired@example.com",
            new WorkspaceInvitationResourceGrant(BoardKind, BoardId, "Viewer"),
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-30));
        var otherBoard = Invitation(TestUserId, boardId: Guid.CreateVersion7(), email: "other-board@example.com");
        var otherWorkspace = Invitation(TestUserId, workspaceId: Guid.CreateVersion7(), email: "other-ws@example.com");
        var workspaceInvitation = WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, "member@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-1));
        SetupInvitations(pending, revoked, expired, otherBoard, otherWorkspace, workspaceInvitation);
        SetupMembers(CreateMember(WorkspaceRole.Member));

        var result = await ListAsync();

        result.Data.Select(r => r.Email).Should().Equal("pending@example.com");
        result.Data.Single().Level.Should().Be("Viewer");
        result.Data.Single().InvitedByName.Should().StartWith("Name ");
    }

    [Fact]
    public async Task List_IsEmptyWhenNothingIsPending()
    {
        SetupMembers(CreateMember(WorkspaceRole.Member));

        (await ListAsync()).Data.Should().BeEmpty();
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Owner)]
    public async Task List_AnyActorMayManageTheirOwnInvitation(WorkspaceRole role)
    {
        SetupInvitations(Invitation(TestUserId));
        SetupMembers(CreateMember(role));

        var row = (await ListAsync()).Data.Single();

        row.IssuedByYou.Should().BeTrue();
        row.CanManage.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Owner)]
    public async Task List_OwnerAndAdminMayManageAnotherPersonsInvitation(WorkspaceRole role)
    {
        SetupInvitations(Invitation(Guid.CreateVersion7()));
        SetupMembers(CreateMember(role));

        var row = (await ListAsync()).Data.Single();

        row.IssuedByYou.Should().BeFalse();
        row.CanManage.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task List_APlainBoardManagerMayNotManageAnotherPersonsInvitation(WorkspaceRole role)
    {
        SetupInvitations(Invitation(Guid.CreateVersion7()));
        SetupMembers(CreateMember(role));

        var row = (await ListAsync()).Data.Single();

        row.IssuedByYou.Should().BeFalse();
        row.CanManage.Should().BeFalse();
    }

    [Fact]
    public async Task List_ASuspendedAdminHasNoManageRight()
    {
        var member = CreateMember(WorkspaceRole.Admin);
        member.Suspend(Guid.CreateVersion7(), TestNow, activeOwnerCount: 2);
        SetupInvitations(Invitation(Guid.CreateVersion7()));
        SetupMembers(member);

        (await ListAsync()).Data.Single().CanManage.Should().BeFalse();
    }
}
