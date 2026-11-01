using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Queries;

/// <summary>The caller's own workspaces: role, default flag, canLeave and the visibility of the counts.</summary>
public class GetMyWorkspacesHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IIdentityDefaultWorkspace> _defaults = new();
    private readonly Guid _otherUserId = Guid.CreateVersion7();

    private GetMyWorkspacesQueryHandler Sut() => new(DbContextMock.Object, RequestContextMock.Object, _defaults.Object);

    private Workspace Workspace(string name, bool archived = false)
    {
        var workspace = Notrelix.Domain.Workspaces.Workspaces.Workspace.Create(
            TestAccountId, _otherUserId, name, name.ToLowerInvariant().Replace(' ', '-'), TestNow, null);
        if (archived)
        {
            workspace.Archive(_otherUserId, TestNow.AddDays(1));
        }

        return workspace;
    }

    private WorkspaceMember Member(Workspace workspace, Guid userId, WorkspaceRole role) =>
        WorkspaceMember.Create(workspace.AccountId, workspace.Id, userId, role, userId, TestNow);

    [Fact]
    public async Task ListsOnlyTheCallersActiveMemberships_ArchivedOnlyForOwnersAndAdmins_OrderedByName()
    {
        var beta = Workspace("Beta");
        var alpha = Workspace("Alpha");
        var archived = Workspace("Zeta", archived: true);
        var archivedAsMember = Workspace("Omega", archived: true);
        var foreign = Workspace("Foreign");
        var left = Workspace("Left");
        var suspendedIn = Workspace("Suspended");

        var leftMe = Member(left, TestUserId, WorkspaceRole.Member);
        leftMe.Remove(2, TestUserId, TestNow);
        var suspendedMe = Member(suspendedIn, TestUserId, WorkspaceRole.Member);
        suspendedMe.Suspend(_otherUserId, TestNow, 2);

        SetupWorkspaces(beta, alpha, archived, archivedAsMember, foreign, left, suspendedIn);
        SetupMembers(
            Member(beta, TestUserId, WorkspaceRole.Member),
            Member(alpha, TestUserId, WorkspaceRole.Owner),
            Member(alpha, _otherUserId, WorkspaceRole.Owner),
            Member(archived, TestUserId, WorkspaceRole.Owner),
            Member(archivedAsMember, TestUserId, WorkspaceRole.Member),
            Member(foreign, _otherUserId, WorkspaceRole.Owner),
            leftMe,
            suspendedMe);

        var result = await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Select(w => w.Name).Should().Equal("Alpha", "Beta", "Zeta");
        result.Data!.Select(w => (w.Name, w.IsArchived)).Should().Equal(("Alpha", false), ("Beta", false), ("Zeta", true));
    }

    [Fact]
    public async Task EachRow_CarriesTheCallersOwnRole_AndTheActiveMemberCount()
    {
        var workspace = Workspace("Acme");
        var suspended = Member(workspace, Guid.CreateVersion7(), WorkspaceRole.Member);
        suspended.Suspend(_otherUserId, TestNow, 2);
        SetupWorkspaces(workspace);
        SetupMembers(
            Member(workspace, TestUserId, WorkspaceRole.Admin),
            Member(workspace, _otherUserId, WorkspaceRole.Owner),
            suspended);

        var row = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!.Single();

        row.Role.Should().Be("Admin");
        row.MemberCount.Should().Be(2);
        row.Slug.Should().Be("acme");
        row.CanLeave.Should().BeTrue();
    }

    [Fact]
    public async Task AGuest_NeverSeesTheMemberCountOrTheDepartmentCount()
    {
        var workspace = Workspace("Acme");
        SetupWorkspaces(workspace);
        SetupMembers(Member(workspace, TestUserId, WorkspaceRole.Guest), Member(workspace, _otherUserId, WorkspaceRole.Owner));
        SetupSpaces(Space.Create(TestAccountId, workspace.Id, "IT", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Department));

        var row = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!.Single();

        row.MemberCount.Should().BeNull();
        row.DepartmentCount.Should().BeNull();
    }

    [Fact]
    public async Task AMember_SeesTheMemberCount_ButNotTheDepartmentCount()
    {
        var workspace = Workspace("Acme");
        SetupWorkspaces(workspace);
        SetupMembers(Member(workspace, TestUserId, WorkspaceRole.Member), Member(workspace, _otherUserId, WorkspaceRole.Owner));
        SetupSpaces(Space.Create(TestAccountId, workspace.Id, "IT", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Department));

        var row = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!.Single();

        row.MemberCount.Should().Be(2);
        row.DepartmentCount.Should().BeNull();
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task AnOwnerOrAdmin_SeesTheActiveDepartmentCount_OnlyDepartments(WorkspaceRole role)
    {
        var workspace = Workspace("Acme");
        var archivedDepartment = Space.Create(TestAccountId, workspace.Id, "Old", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Department);
        archivedDepartment.Archive(_otherUserId, TestNow.AddDays(1), SpaceContentFact.Empty);
        SetupWorkspaces(workspace);
        SetupMembers(Member(workspace, TestUserId, role), Member(workspace, _otherUserId, WorkspaceRole.Owner));
        SetupSpaces(
            Space.Create(TestAccountId, workspace.Id, "IT", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Department),
            Space.Create(TestAccountId, workspace.Id, "HR", SpaceVisibility.Private, _otherUserId, TestNow, SpaceType.Department),
            Space.Create(TestAccountId, workspace.Id, "Projects", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Folder),
            archivedDepartment,
            Space.Create(TestAccountId, Guid.CreateVersion7(), "Elsewhere", SpaceVisibility.Workspace, _otherUserId, TestNow, SpaceType.Department));

        var row = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!.Single();

        row.DepartmentCount.Should().Be(2);
    }

    [Fact]
    public async Task OnlyTheDefaultWorkspaceIsFlagged_AndAForeignDefaultFlagsNothing()
    {
        var one = Workspace("One");
        var two = Workspace("Two");
        SetupWorkspaces(one, two);
        SetupMembers(
            Member(one, TestUserId, WorkspaceRole.Member), Member(one, _otherUserId, WorkspaceRole.Owner),
            Member(two, TestUserId, WorkspaceRole.Member), Member(two, _otherUserId, WorkspaceRole.Owner));

        _defaults.Setup(d => d.GetAsync(TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(two.Id);
        var flagged = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!;
        flagged.Single(w => w.IsDefault).Name.Should().Be("Two");

        _defaults.Setup(d => d.GetAsync(TestUserId, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!.Should().OnlyContain(w => !w.IsDefault);
    }

    [Fact]
    public async Task CanLeave_IsFalse_OnlyForTheLastActiveOwner()
    {
        var sole = Workspace("Sole");
        var shared = Workspace("Shared");
        SetupWorkspaces(sole, shared);
        SetupMembers(
            Member(sole, TestUserId, WorkspaceRole.Owner),
            Member(shared, TestUserId, WorkspaceRole.Owner),
            Member(shared, _otherUserId, WorkspaceRole.Owner));

        var rows = (await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None)).Data!;

        rows.Single(w => w.Name == "Sole").CanLeave.Should().BeFalse();
        rows.Single(w => w.Name == "Shared").CanLeave.Should().BeTrue();
    }

    [Fact]
    public async Task AnUnauthenticatedCaller_GetsAFailure_AndNothingIsRead()
    {
        RequestContextMock.Setup(c => c.UserId).Returns(Guid.Empty);

        var result = await Sut().Handle(new GetMyWorkspacesQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _defaults.Verify(d => d.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
