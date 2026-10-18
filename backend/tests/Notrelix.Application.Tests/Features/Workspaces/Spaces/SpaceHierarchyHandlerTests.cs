using Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.Workspaces;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces;

/// <summary>
/// Space hierarchy use-case orchestration (WSP-021, WSP-023, WSP-024, GOV-028):
/// facts are read under the hierarchy lock, placement is neighbor-relative,
/// the destination of a move is authorized, and subjects come from the same
/// Workspace. Closure correctness, RLS and the real lock are PostgreSQL proofs
/// (SpaceHierarchyIntegrationTests).
/// </summary>
public class SpaceHierarchyHandlerTests : WorkspaceHandlerTestBase
{
    private readonly InMemorySpaceStore _spaces = new();
    private readonly Mock<ISpaceTargetAuthorizer> _targetAuthorizer = new();

    private CreateSpaceCommandHandler CreateHandler() =>
        new(DbContextMock.Object, _spaces, RequestContextMock.Object, DateTimeProviderMock.Object);

    private MoveSpaceCommandHandler MoveHandler() =>
        new(_spaces, _targetAuthorizer.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    private async Task<Guid> CreateAsync(string name, Guid? parent = null, string? type = null, string? visibility = null, Guid? previous = null, Guid? next = null)
    {
        var result = await CreateHandler().Handle(
            new CreateSpaceCommand(TestWorkspaceId, name, visibility, null, type, parent, previous, next), CancellationToken.None);
        return result.Data;
    }

    private Space Get(Guid id) => _spaces.Spaces.Single(s => s.Id == id);

    public SpaceHierarchyHandlerTests()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
    }

    [Fact]
    public async Task Create_ADepartment_WithoutAVisibility_IsPrivate_AndAFolder_IsWorkspaceVisible()
    {
        var accounting = await CreateAsync("Accounting", type: "Department");
        var shared = await CreateAsync("Shared", type: "Folder");

        Get(accounting).Visibility.Should().Be(SpaceVisibility.Private, "department Spaces are Private by default (WSP-023)");
        Get(shared).Visibility.Should().Be(SpaceVisibility.Workspace);
        _spaces.StagedCreated.Should().Equal(accounting, shared);
        _spaces.LockCount.Should().Be(2, "each hierarchy write runs under the workspace hierarchy lock");
    }

    [Fact]
    public async Task Create_AppendsAfterTheLastSibling_OrPlacesBeforeTheFirst()
    {
        var first = await CreateAsync("First");
        var second = await CreateAsync("Second");
        var top = await CreateAsync("Top", next: first);

        var ordered = _spaces.Spaces.OrderBy(s => s.Position).Select(s => s.Id).ToList();
        ordered.Should().Equal(top, first, second);
    }

    [Fact]
    public async Task Create_WithAStaleNeighbor_IsAConflict()
    {
        var first = await CreateAsync("First");
        await CreateAsync("Second");

        var act = () => CreateAsync("Middle", previous: Guid.NewGuid(), next: first);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(SpacePlacementResolver.StalePlacementErrorCode);
    }

    [Fact]
    public async Task Create_UnderAParentThatDoesNotExistInTheWorkspace_IsNotFound()
    {
        var act = () => CreateAsync("Orphan", parent: Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        _spaces.Spaces.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_ToANewParent_AuthorizesTheDestination_AndRepointsTheClosure()
    {
        var it = await CreateAsync("IT");
        var finance = await CreateAsync("Finance");
        var security = await CreateAsync("Security", parent: it);

        await MoveHandler().Handle(new MoveSpaceCommand(TestWorkspaceId, security, finance, null, null, Get(security).Version), CancellationToken.None);

        Get(security).ParentSpaceId.Should().Be(finance);
        _targetAuthorizer.Verify(a => a.AuthorizePlacementAsync(TestWorkspaceId, finance, It.IsAny<CancellationToken>()), Times.Once);
        _spaces.StagedMoved.Should().Equal(security);
    }

    [Fact]
    public async Task Move_WithinTheSameParent_DoesNotReauthorizeTheDestination_NorRewriteTheClosure()
    {
        var a = await CreateAsync("A");
        var b = await CreateAsync("B");

        await MoveHandler().Handle(new MoveSpaceCommand(TestWorkspaceId, b, null, null, a, Get(b).Version), CancellationToken.None);

        _spaces.Spaces.OrderBy(s => s.Position).Select(s => s.Id).Should().Equal(b, a);
        _targetAuthorizer.VerifyNoOtherCalls();
        _spaces.StagedMoved.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_UnderItsOwnDescendant_IsRejectedByTheDomain_WithoutMutation()
    {
        var it = await CreateAsync("IT");
        var security = await CreateAsync("Security", parent: it);
        var version = Get(it).Version;

        var act = () => MoveHandler().Handle(new MoveSpaceCommand(TestWorkspaceId, it, security, null, null, version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Space_MoveCreatesCycle);
        Get(it).ParentSpaceId.Should().BeNull();
        Get(it).Version.Should().Be(version);
        _spaces.StagedMoved.Should().BeEmpty();
    }

    [Fact]
    public async Task Move_WhenTheDestinationIsNotAuthorized_FailsBeforeAnyMutation()
    {
        var it = await CreateAsync("IT");
        var finance = await CreateAsync("Finance");
        _targetAuthorizer
            .Setup(a => a.AuthorizePlacementAsync(TestWorkspaceId, finance, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("nope"));

        var act = () => MoveHandler().Handle(new MoveSpaceCommand(TestWorkspaceId, it, finance, null, null, Get(it).Version), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        Get(it).ParentSpaceId.Should().BeNull();
    }

    [Fact]
    public async Task AddMember_WithASubjectFromAnotherWorkspace_IsNotFound()
    {
        var it = await CreateAsync("IT");
        var outsider = Guid.NewGuid();
        var handler = new AddSpaceMemberCommandHandler(_spaces, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new AddSpaceMemberCommand(TestWorkspaceId, it, "User", outsider, "Member", Get(it).Version), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        Get(it).Members.Should().NotContain(m => m.SubjectId == outsider);
    }

    [Fact]
    public async Task AddMember_AnActiveTeamOfTheWorkspace_IsAdded()
    {
        var it = await CreateAsync("IT");
        var team = Guid.NewGuid();
        _spaces.ActiveTeams.Add((TestWorkspaceId, team));
        var handler = new AddSpaceMemberCommandHandler(_spaces, RequestContextMock.Object, DateTimeProviderMock.Object);

        await handler.Handle(new AddSpaceMemberCommand(TestWorkspaceId, it, "Team", team, "Viewer", Get(it).Version), CancellationToken.None);

        Get(it).Members.Should().Contain(m => m.SubjectType == SpaceMemberSubjectType.Team && m.SubjectId == team && m.Role == SpaceRole.Viewer);
    }

    [Fact]
    public async Task GetSpaceTree_ListsOnlyVisibleSpaces_AndNeverDisclosesAHiddenParent()
    {
        var it = await CreateAsync("IT");
        var security = await CreateAsync("Security", parent: it);
        var finance = await CreateAsync("Finance");
        var visibility = new FakeResourceVisibility();
        visibility.Spaces.AddRange([security, finance]);
        var handler = new GetSpaceTreeQueryHandler(_spaces, visibility, RequestContextMock.Object);

        var result = await handler.Handle(new GetSpaceTreeQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Select(n => n.Id).Should().BeEquivalentTo([security, finance]);
        result.Data!.Single(n => n.Id == security).ParentSpaceId.Should().BeNull("the hidden parent IT is not disclosed");
    }

    [Fact]
    public void PlacementResolver_PlacesBetweenAdjacentSiblings_AndRejectsNonAdjacentNeighbors()
    {
        var a = new SpaceSiblingEntry(Guid.NewGuid(), "A", FractionalIndex.Create("a0"));
        var b = new SpaceSiblingEntry(Guid.NewGuid(), "B", FractionalIndex.Create("a1"));
        var c = new SpaceSiblingEntry(Guid.NewGuid(), "C", FractionalIndex.Create("a2"));

        var between = SpacePlacementResolver.Place([a, b, c], a.Id, b.Id);
        between.CompareTo(a.Position).Should().BePositive();
        between.CompareTo(b.Position).Should().BeNegative();

        var stale = () => SpacePlacementResolver.Place([a, b, c], a.Id, c.Id);
        stale.Should().Throw<ConflictException>();
    }
}
