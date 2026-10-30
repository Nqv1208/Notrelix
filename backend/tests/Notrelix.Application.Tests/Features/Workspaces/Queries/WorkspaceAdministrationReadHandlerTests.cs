using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Queries;

/// <summary>
/// Read contract of the Workspaces administration queries: the workspace read
/// reports the aggregate version and counts only members whose participation
/// has not ended; the member list reports status and omits removed members;
/// the invitation list reports each invitation's status.
/// PostgreSQL behavior is proven by WorkspaceAdministrationReadIntegrationTests.
/// </summary>
public sealed class WorkspaceAdministrationReadHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IMemberPhoneLookup> _phones = new();

    public WorkspaceAdministrationReadHandlerTests()
    {
        _phones.Setup(p => p.FindSharedPhonesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, $"User {id:N}", null)).ToList());
    }

    [Fact]
    public async Task GetWorkspace_ReportsTheAggregateVersion_AndCountsOnlyMembersStillParticipating()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        workspace.Rename("Renamed", TestUserId, TestNow.AddMinutes(1));
        SetupWorkspaces(workspace);
        SetupMembers(Active(), Suspended(), Removed());

        var result = await new GetWorkspaceQueryHandler(DbContextMock.Object)
            .Handle(new GetWorkspaceQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Version.Should().Be(workspace.Version).And.BeGreaterThan(1, "a rename advances the version");
        result.Data.MemberCount.Should().Be(2, "a removed member no longer participates");
    }

    [Fact]
    public async Task GetWorkspaceMembers_ReportsStatus_AndOmitsRemovedMembers()
    {
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        var active = Active();
        var suspended = Suspended();
        var removed = Removed();
        SetupMembers(active, suspended, removed);

        var result = await new GetWorkspaceMembersQueryHandler(DbContextMock.Object, _actors.Object, _phones.Object, RequestContextMock.Object)
            .Handle(new GetWorkspaceMembersQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Select(m => (m.UserId, m.Status)).Should().BeEquivalentTo(new[]
        {
            (active.UserId, "Active"),
            (suspended.UserId, "Suspended"),
        });
        result.Data.Items.Should().NotContain(m => m.UserId == removed.UserId);
    }

    [Fact]
    public async Task GetWorkspaceInvitations_ReportsEachStatus()
    {
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        var pending = Invitation("pending@example.com");
        var revoked = Invitation("revoked@example.com");
        revoked.Revoke(TestUserId, TestNow.AddHours(1));
        SetupInvitations(pending, revoked);

        var result = await new GetWorkspaceInvitationsQueryHandler(DbContextMock.Object)
            .Handle(new GetWorkspaceInvitationsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Select(i => (i.Id, i.Status)).Should().BeEquivalentTo(new[]
        {
            (pending.Id, "Pending"),
            (revoked.Id, "Revoked"),
        });
    }

    private WorkspaceMember Active() => CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7());

    private WorkspaceMember Suspended()
    {
        var member = CreateMember(WorkspaceRole.Member, Guid.CreateVersion7());
        member.Suspend(TestUserId, TestNow, activeOwnerCount: 1);
        return member;
    }

    private WorkspaceMember Removed()
    {
        var member = CreateMember(WorkspaceRole.Member, Guid.CreateVersion7());
        member.Remove(activeOwnerCount: 1, TestUserId, TestNow);
        return member;
    }

    private WorkspaceInvitation Invitation(string email) => WorkspaceInvitation.Create(
        TestAccountId,
        TestWorkspaceId,
        email,
        WorkspaceRole.Member,
        InvitationTokenHash.Create(new string('a', 64)),
        1,
        TestUserId,
        TestNow);
}
