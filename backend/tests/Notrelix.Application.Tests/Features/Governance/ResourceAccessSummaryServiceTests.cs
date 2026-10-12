using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Board access summary composition over the canonical facts and engine:
/// the caller's level and source, de-duplicated people/guest counts, and name
/// disclosure limited to what the caller can already see. A caller without the
/// single read gets nothing (the handler maps it to NotFound).
/// </summary>
public sealed class ResourceAccessSummaryServiceTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid SpaceId = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid Caller = Guid.NewGuid();
    private static readonly Guid Grantor = Guid.NewGuid();
    private static readonly Guid Linh = Guid.NewGuid();
    private static readonly Guid Hoa = Guid.NewGuid();
    private static readonly Guid Quan = Guid.NewGuid();
    private static readonly Guid GuestUser = Guid.NewGuid();

    private readonly FakeFacts _facts = new();
    private readonly FakeBoards _boards = new();
    private readonly FakeEntries _entries = new();
    private readonly FakeResourceVisibility _visibility = new();
    private readonly Dictionary<Guid, string> _names = new()
    {
        [Caller] = "Minh Quân",
        [Grantor] = "Hà Anh",
        [Linh] = "Đỗ Khánh Linh",
        [Hoa] = "Phạm Hoa",
        [Quan] = "Lê Quang",
        [GuestUser] = "Tân Lộc",
    };

    [Fact]
    public async Task ExplicitCommenterGrant_IsCommenter_FromAnExplicitSource_WithTheGrantor()
    {
        _facts.Facts = Facts("Member", audience: "Private", explicitRank: 2);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("User", Caller, 2, Grantor, null)],
            [Reach(AccessReachSources.User, Caller)],
            null);

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Commenter);
        summary.MySource.Kind.Should().Be(AccessSourceKinds.Explicit);
        summary.MySource.GrantedBy.Should().Be(new AccessGrantor(Grantor, "Hà Anh"));
    }

    [Fact]
    public async Task WorkspaceOwner_IsManager_FromTheOwnerSource()
    {
        _facts.Facts = Facts("Owner", audience: "Private");

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Manager);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Owner, null));
    }

    [Fact]
    public async Task SpaceMember_IsEditor_FromTheSpace_AndTheVisibleSpaceIsNamed()
    {
        _facts.Facts = Facts("Member", spaceId: SpaceId, spaceRole: "Member");
        _visibility.Spaces.Add(SpaceId);
        _entries.Entries = new ResourceAccessEntries(
            [],
            [Reach(AccessReachSources.Space, Caller), Reach(AccessReachSources.Space, Hoa)],
            "Phòng Công nghệ");

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Editor);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Space, null));
        summary.Sources.Should().Equal(new AccessSourceCount(AccessSourceKinds.Space, SpaceId, "Phòng Công nghệ", 2));
        _entries.LastRequest!.SpaceId.Should().Be(SpaceId);
    }

    [Fact]
    public async Task SpaceTheCallerCannotSee_IsCountedWithoutItsName()
    {
        _facts.Facts = Facts("Member", spaceId: SpaceId, explicitRank: 1);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("User", Caller, 1, Grantor, null)],
            [Reach(AccessReachSources.User, Caller), Reach(AccessReachSources.Space, Hoa), Reach(AccessReachSources.Space, Quan)],
            "Phòng Kế toán");

        var summary = await SummarizeAsync();

        summary!.Sources.Should().Contain(new AccessSourceCount(AccessSourceKinds.Space, null, null, 2));
        summary.Sources.Should().NotContain(s => s.Name == "Phòng Kế toán");
    }

    [Fact]
    public async Task PrivateBoard_NeverCountsTheSpace()
    {
        _facts.Facts = Facts("Member", audience: "Private", spaceId: SpaceId, explicitRank: 3);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("User", Caller, 3, Grantor, null)], [Reach(AccessReachSources.User, Caller)], null);

        await SummarizeAsync();

        _entries.LastRequest!.SpaceId.Should().BeNull("Space roles never reach a Private Board (GOV-025 step 4)");
    }

    [Fact]
    public async Task GuestCaller_HasTheGuestSource_AndIsCountedAsAGuest()
    {
        _facts.Facts = Facts("Guest", explicitRank: 2);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("User", Caller, 2, Grantor, null)],
            [Reach(AccessReachSources.User, Caller, isGuest: true), Reach(AccessReachSources.Member, Linh)],
            null);
        _boards.Members.Add(Linh);

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Commenter);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Guest, new AccessGrantor(Grantor, "Hà Anh")));
        summary.PeopleCount.Should().Be(2);
        summary.GuestCount.Should().Be(1);
        summary.Sources.Should().Equal(
            new AccessSourceCount(AccessSourceKinds.User, Linh, "Đỗ Khánh Linh", 1),
            new AccessSourceCount(AccessSourceKinds.Guest, null, null, 1));
    }

    [Fact]
    public async Task CallerWithoutAccess_GetsNothing_AndNoEntriesAreRead()
    {
        _facts.Facts = Facts("Member", audience: "Private");

        var summary = await SummarizeAsync();

        summary.Should().BeNull();
        _entries.LastRequest.Should().BeNull("a denied caller must not trigger the people read");
        _boards.Calls.Should().Be(0);
    }

    [Fact]
    public async Task APersonReachableByTwoSources_IsCountedOnce_InTheFirstSource()
    {
        _facts.Facts = Facts("Admin", spaceId: SpaceId);
        _visibility.Spaces.Add(SpaceId);
        _boards.Members.AddRange([Linh, Hoa]);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("Team", TeamId, 3, Grantor, "Nhóm Kế toán tổng hợp")],
            [
                Reach(AccessReachSources.Space, Hoa),
                Reach(AccessReachSources.Space, Quan),
                Reach(AccessReachSources.Team, Hoa, TeamId),
                Reach(AccessReachSources.Team, Linh, TeamId),
                Reach(AccessReachSources.Member, Linh),
                Reach(AccessReachSources.Member, Hoa),
                Reach(AccessReachSources.Member, GuestUser, isGuest: true),
                Reach(AccessReachSources.Team, GuestUser, TeamId, isGuest: true),
            ],
            "Phòng Công nghệ");

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Manager);
        summary.MySource.Kind.Should().Be(AccessSourceKinds.WorkspaceAdmin);
        summary.PeopleCount.Should().Be(4);
        summary.GuestCount.Should().Be(1);
        summary.Sources.Should().Equal(
            new AccessSourceCount(AccessSourceKinds.Space, SpaceId, "Phòng Công nghệ", 2),
            new AccessSourceCount(AccessSourceKinds.Team, TeamId, "Nhóm Kế toán tổng hợp", 1),
            new AccessSourceCount(AccessSourceKinds.Guest, null, null, 1));
        summary.Sources.Sum(s => s.Count).Should().Be(summary.PeopleCount);
    }

    [Fact]
    public async Task NonManager_SeesBoardMembersByName_ButGrantsOnlyAsCounts()
    {
        _facts.Facts = Facts("Member", audience: "Private", boardRole: "Member");
        _boards.Members.Add(Caller);
        _entries.Entries = new ResourceAccessEntries(
            [
                new ResourceGrantEntry("Team", TeamId, 3, Grantor, "Nhóm Kế toán tổng hợp"),
                new ResourceGrantEntry("User", Quan, 1, Grantor, null),
            ],
            [
                Reach(AccessReachSources.Member, Caller),
                Reach(AccessReachSources.Team, Hoa, TeamId),
                Reach(AccessReachSources.User, Quan),
            ],
            null);

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Editor);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Explicit, null));
        summary.Sources.Should().Equal(
            new AccessSourceCount(AccessSourceKinds.Team, null, null, 1),
            new AccessSourceCount(AccessSourceKinds.User, Caller, "Minh Quân", 1),
            new AccessSourceCount(AccessSourceKinds.User, null, null, 1));
        summary.Sources.Should().NotContain(s => s.Name == "Nhóm Kế toán tổng hợp" || s.Id == Quan || s.Id == TeamId);
    }

    [Fact]
    public async Task Manager_SeesGrantedTeamsAndUsersByName()
    {
        _facts.Facts = Facts("Member", audience: "Private", explicitRank: 4);
        _entries.Entries = new ResourceAccessEntries(
            [
                new ResourceGrantEntry("User", Caller, 4, Grantor, null),
                new ResourceGrantEntry("Team", TeamId, 3, Grantor, "Nhóm Kế toán tổng hợp"),
                new ResourceGrantEntry("User", Quan, 1, Grantor, null),
            ],
            [
                Reach(AccessReachSources.User, Caller),
                Reach(AccessReachSources.Team, Hoa, TeamId),
                Reach(AccessReachSources.User, Quan),
            ],
            null);

        var summary = await SummarizeAsync();

        summary!.Sources.Should().Equal(
            new AccessSourceCount(AccessSourceKinds.Team, TeamId, "Nhóm Kế toán tổng hợp", 1),
            new AccessSourceCount(AccessSourceKinds.User, Quan, "Lê Quang", 1),
            new AccessSourceCount(AccessSourceKinds.User, Caller, "Minh Quân", 1));
    }

    [Fact]
    public async Task TeamGrant_IsTheTeamSource_WithItsGrantor()
    {
        _facts.Facts = Facts("Member", audience: "Private", explicitRank: 3);
        _entries.Entries = new ResourceAccessEntries(
            [new ResourceGrantEntry("Team", TeamId, 3, Grantor, "Nhóm Kế toán tổng hợp")],
            [Reach(AccessReachSources.Team, Caller, TeamId)],
            null);

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Editor);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Team, new AccessGrantor(Grantor, "Hà Anh")));
    }

    [Fact]
    public async Task PrivateBoardWithoutReadRules_HasACompleteCount_IncludingWorkspaceOwners()
    {
        var owner = Guid.NewGuid();
        _facts.Facts = Facts("Member", audience: "Private", boardRole: "Member");
        _boards.Members.Add(Caller);
        _entries.Entries = new ResourceAccessEntries(
            [],
            [Reach(AccessReachSources.Member, Caller), Reach(AccessReachSources.Owner, owner)],
            null);

        var summary = await SummarizeAsync();

        summary!.PeopleCountComplete.Should().BeTrue("a Private board is reached only by Owners, members and grants");
        summary.PeopleCount.Should().Be(2);
        summary.Sources.Should().Equal(
            new AccessSourceCount(AccessSourceKinds.User, Caller, "Minh Quân", 1),
            new AccessSourceCount(AccessSourceKinds.User, null, null, 1));
        _entries.LastRequest!.ReadAction.Should().Be("ViewBoard");
    }

    [Fact]
    public async Task PrivateBoardWithAnApplicableReadRule_IsNotComplete()
    {
        _facts.Facts = Facts("Member", audience: "Private", boardRole: "Member");
        _boards.Members.Add(Caller);
        _entries.Entries = new ResourceAccessEntries([], [Reach(AccessReachSources.Member, Caller)], null, HasReadRules: true);

        var summary = await SummarizeAsync();

        summary!.PeopleCountComplete.Should().BeFalse("a permission rule can add or remove readers that are not enumerated");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceVisibleBoard_IsNeverComplete(bool placed)
    {
        _facts.Facts = Facts("Member", spaceId: placed ? SpaceId : null, spaceRole: placed ? "Member" : null);

        var summary = await SummarizeAsync();

        summary!.PeopleCountComplete.Should().BeFalse("Space inheritance, open Spaces, Admins and the Workspace default are not enumerated");
    }

    [Fact]
    public async Task UnplacedWorkspaceBoard_IsEditor_FromTheWorkspaceDefault()
    {
        _facts.Facts = Facts("Member");

        var summary = await SummarizeAsync();

        summary!.MyLevel.Should().Be(AccessLevels.Editor);
        summary.MySource.Should().Be(new AccessSource(AccessSourceKinds.Workspace, null));
    }

    [Fact]
    public async Task FactsAreResolvedForAViewBoardReadOfTheBoard()
    {
        _facts.Facts = Facts("Owner");

        await SummarizeAsync();

        var probe = _facts.LastRequest.Should().BeAssignableTo<IRequirePermission>().Which;
        probe.Action.Should().Be(Notrelix.Domain.Governance.Permissions.PermissionAction.ViewBoard);
        probe.Resource.Should().Be(ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId));
        _facts.LastDescriptor!.Access.RequiresPermission.Should().BeTrue();
        _facts.LastDescriptor.Scope.Should().Be(ApplicationScopeKind.Resource);
    }

    private Task<BoardAccessSummary?> SummarizeAsync() =>
        CreateService().GetBoardAccessAsync(BoardId, CancellationToken.None);

    private ResourceAccessSummaryService CreateService()
    {
        var context = new Mock<IExecutionContextReader>();
        context.Setup(c => c.Snapshot).Returns(new ExecutionContextSnapshot(
            Caller, AccountId, WorkspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId),
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test"));

        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) => ids
                .Where(_names.ContainsKey)
                .Select(id => new ActorSnapshot(id, _names[id], null))
                .ToList());

        return new ResourceAccessSummaryService(
            context.Object,
            _facts,
            new AccessPolicyEngine(),
            _boards,
            _entries,
            _visibility,
            actors.Object);
    }

    private static AccessReachEntry Reach(string source, Guid userId, Guid? teamId = null, bool isGuest = false) =>
        new(source, teamId, userId, isGuest);

    private static AccessFacts Facts(
        string? workspaceRole,
        string audience = "Workspace",
        Guid? spaceId = null,
        string? spaceRole = null,
        string? boardRole = null,
        int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: workspaceRole,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: boardRole,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null)
    {
        ResourceSpaceId = spaceId,
        SpaceExists = spaceId is not null,
        SpaceRole = spaceRole,
    };

    private sealed class FakeFacts : IAccessFactsProvider
    {
        public AccessFacts? Facts { get; set; }
        public object? LastRequest { get; private set; }
        public RequestDescriptor? LastDescriptor { get; private set; }

        public Task<AccessFacts> ResolveAsync(
            RequestDescriptor descriptor, ExecutionContextSnapshot context, object request, CancellationToken cancellationToken)
        {
            LastDescriptor = descriptor;
            LastRequest = request;
            return Task.FromResult(Facts ?? throw new InvalidOperationException("facts not arranged"));
        }
    }

    private sealed class FakeBoards : IBoardAccessSubjectFacts
    {
        public List<Guid> Members { get; } = [];
        public int Calls { get; private set; }

        public Task<BoardAccessSubject?> FindAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<BoardAccessSubject?>(new BoardAccessSubject(boardId, 7, Members.ToArray()));
        }
    }

    private sealed class FakeEntries : IResourceAccessEntriesReader
    {
        public ResourceAccessEntries Entries { get; set; } = new([], [], null);
        public ResourceAccessEntriesRequest? LastRequest { get; private set; }

        public Task<ResourceAccessEntries> ReadAsync(ResourceAccessEntriesRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Entries);
        }
    }
}
