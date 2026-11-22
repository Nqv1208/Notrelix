using Notrelix.Application.Features.Collaboration.Mentions.DTOs;
using Notrelix.Application.Features.Collaboration.Mentions.Queries.GetMentionSuggestions;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// UC-COL-B05 / UC-DCT-E08: the @ suggestions list only active colleagues of the target's workspace who can
/// view the target (the same decision a notification delivery makes), never the caller, never a guest's roster.
/// </summary>
public sealed class GetMentionSuggestionsTests
{
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _page = Guid.CreateVersion7();
    private readonly Guid _item = Guid.CreateVersion7();
    private readonly Guid _board = Guid.CreateVersion7();

    private readonly Dictionary<Guid, string> _names = [];
    private readonly List<Guid> _colleagues = [];
    private readonly HashSet<Guid> _cannotView = [];
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IPageViewAccess> _pages = new();
    private readonly Mock<IWorkItemNotificationFacts> _items = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IExecutionContextReader> _context = new();
    private ExecutionContextSnapshot? _snapshot;
    private bool _itemExists = true;

    public GetMentionSuggestionsTests()
    {
        _snapshot = new ExecutionContextSnapshot(_me, _account, _workspace, null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");
        _context.SetupGet(c => c.Snapshot).Returns(() => _snapshot);
        _user.SetupGet(u => u.UserId).Returns(_me);
        _colleagues.Add(_me);
        _names[_me] = "Me Myself";

        _members.Setup(m => m.ListActiveColleagueIdsAsync(_workspace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _colleagues.ToList());
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Where(_names.ContainsKey).Select(id => new ActorSnapshot(id, _names[id], null)).ToList());
        _pages.Setup(p => p.CanViewAsync(_account, _workspace, _page, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, Guid _, Guid user, CancellationToken _) => !_cannotView.Contains(user));
        _items.Setup(i => i.FindAsync(_workspace, _item, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _itemExists ? new WorkItemNotificationFacts(_board, "t", false, null) : null);
        _visibility.Setup(v => v.VisibleBoardIdsAsync(_workspace, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid user, CancellationToken _) => _cannotView.Contains(user) ? [] : new List<Guid> { _board });
    }

    private Guid Person(string name, bool canView = true, bool colleague = true)
    {
        var id = Guid.CreateVersion7();
        _names[id] = name;
        if (colleague) _colleagues.Add(id);
        if (!canView) _cannotView.Add(id);
        return id;
    }

    private GetMentionSuggestionsQueryHandler Handler() =>
        new(_members.Object, _actors.Object, _pages.Object, _items.Object, _visibility.Object, _user.Object, _context.Object);

    private async Task<List<MentionSuggestionDto>> OnPage(string? query = null, int? limit = null) =>
        (await Handler().Handle(GetMentionSuggestionsQuery.ForPage(_page, query, limit), default)).Data!;

    private async Task<List<MentionSuggestionDto>> OnItem(string? query = null, int? limit = null) =>
        (await Handler().Handle(GetMentionSuggestionsQuery.ForBoardItem(_item, query, limit), default)).Data!;

    [Fact]
    public async Task Page_MemberWhoCannotViewIsNotSuggested_AndOneWhoCanIs()
    {
        var yes = Person("Ann Viewer");
        Person("Ann Blocked", canView: false);

        (await OnPage("ann")).Select(s => s.UserId).Should().Equal(yes);
    }

    [Fact]
    public async Task BoardItem_MemberWhoCannotSeeTheBoardIsNotSuggested_AndOneWhoCanIs()
    {
        var yes = Person("Ann Viewer");
        Person("Ann Blocked", canView: false);

        (await OnItem("ann")).Select(s => s.UserId).Should().Equal(yes);
    }

    [Fact]
    public async Task APersonOutsideTheWorkspace_IsNeverSuggested_EvenIfTheyCouldViewTheTarget()
    {
        var yes = Person("Ann Viewer");
        Person("Ann Outsider", colleague: false);

        (await OnPage("ann")).Select(s => s.UserId).Should().Equal(yes);
        (await OnItem("ann")).Select(s => s.UserId).Should().Equal(yes);
    }

    [Fact]
    public void CallerWithoutViewAccess_IsRefusedByThePipelineAsNotFound_AndAViewerIsAllowed()
    {
        var engine = new AccessPolicyEngine();
        var context = new ExecutionContextSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");
        AccessDecisionKind Decide(IRequirePermission request, AccessFacts facts) =>
            engine.Evaluate(RequestDescriptorValidator.Create(request.GetType()), context, facts, request).Kind;

        var page = GetMentionSuggestionsQuery.ForPage(_page, null, null);
        var item = GetMentionSuggestionsQuery.ForBoardItem(_item, null, null);
        page.Action.Should().Be(PermissionAction.ViewPage);
        item.Action.Should().Be(PermissionAction.ViewBoard);

        var noAccess = Facts(rank: 4) with
        {
            WorkspaceMemberRole = null,
            ResourceMemberRole = null,
            HasExplicitResourcePermission = false,
            ActiveResourcePermissionRank = null,
        };
        var pageViewer = Facts(rank: 1) with { ParentBoardId = null, IsBoardChild = false };
        Decide(page, noAccess with { ParentBoardId = null, IsBoardChild = false }).Should().Be(AccessDecisionKind.NotFound);
        Decide(item, noAccess).Should().Be(AccessDecisionKind.NotFound);
        Decide(page, pageViewer).Should().Be(AccessDecisionKind.Allowed);
        Decide(item, Facts(rank: 1)).Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public async Task EmptyOrBlankQuery_ReturnsTheFirstPeopleByName_ThenById()
    {
        Person("Charlie");
        Person("Alice");
        Person("Bob");

        (await OnPage()).Select(s => s.Name).Should().Equal("Alice", "Bob", "Charlie");
        (await OnPage("   ", 2)).Select(s => s.Name).Should().Equal("Alice", "Bob");
    }

    [Fact]
    public async Task SameNameSortsByUserId()
    {
        var a = Person("Twin");
        var b = Person("Twin");

        (await OnPage("twin")).Select(s => s.UserId).Should().Equal(new[] { a, b }.Order());
    }

    [Theory]
    [InlineData(null, 8)]
    [InlineData(0, 8)]
    [InlineData(-3, 8)]
    [InlineData(1, 1)]
    [InlineData(500, 20)]
    public async Task Limit_IsClamped_ToTheDefaultOrTheMax(int? limit, int expected)
    {
        for (var i = 0; i < 30; i++) Person($"Person {i:D2}");

        (await OnPage(null, limit)).Should().HaveCount(expected);
    }

    [Theory]
    [InlineData("nguyen", "Nguyễn Văn An")]
    [InlineData("NGUYỄN", "Nguyễn Văn An")]
    [InlineData("dang", "Đặng Thu Hà")]
    [InlineData("đặ", "Đặng Thu Hà")]
    [InlineData("van", "Nguyễn Văn An")]
    [InlineData("văn a", "Nguyễn Văn An")]
    [InlineData("nguyen van an", "Nguyễn Văn An")]
    public async Task Query_MatchesTheStartOfAnyWord_IgnoringCaseAndDiacritics(string query, string expected)
    {
        Person("Nguyễn Văn An");
        Person("Đặng Thu Hà");

        (await OnPage(query)).Select(s => s.Name).Should().Equal(expected);
    }

    [Theory]
    [InlineData("guyen")]
    [InlineData("yen")]
    [InlineData("hu")]
    [InlineData("an nguyen")]
    public async Task Query_InTheMiddleOfAWord_OrOutOfOrder_DoesNotMatch(string query)
    {
        Person("Nguyễn Văn An");
        Person("Đặng Thu Hà");

        (await OnPage(query)).Should().BeEmpty();
    }

    [Fact]
    public async Task TheCaller_IsExcluded()
    {
        Person("Me Other");

        (await OnPage("me")).Select(s => s.Name).Should().Equal("Me Other");
    }

    [Fact]
    public async Task AGuestCaller_GetsNothing_BecauseTheyAreNotAnActiveColleague()
    {
        _colleagues.Remove(_me);
        Person("Ann Viewer");

        (await OnPage()).Should().BeEmpty();
        (await OnItem()).Should().BeEmpty();
    }

    [Fact]
    public void TheDto_HasExactlyTheThreeFields()
    {
        typeof(MentionSuggestionDto).GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo("UserId", "Name", "AvatarUrl");
    }

    [Fact]
    public async Task ADeletedOrMissingItem_FailsClosedToAnEmptyList()
    {
        Person("Ann Viewer");
        _itemExists = false;

        (await OnItem()).Should().BeEmpty();
        _visibility.Verify(v => v.VisibleBoardIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoExecutionContext_FailsClosedToAnEmptyList()
    {
        Person("Ann Viewer");
        _snapshot = null;

        (await OnPage()).Should().BeEmpty();
    }

    [Fact]
    public async Task ViewChecksAreBounded_AndNeverReturnAnUnconfirmedPerson()
    {
        for (var i = 0; i < 300; i++) Person($"Blocked {i:D3}", canView: false);
        Person("Zed Viewer");

        // Zed sorts after 300 blocked people, past the bound of checks: left out rather than guessed.
        (await OnPage()).Should().BeEmpty();
        _pages.Verify(p => p.CanViewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(GetMentionSuggestionsQueryHandler.MaxViewChecks));
    }

    [Fact]
    public async Task ChecksStopOnceTheLimitIsFound()
    {
        for (var i = 0; i < 20; i++) Person($"Person {i:D2}");

        (await OnPage(null, 3)).Should().HaveCount(3);
        _pages.Verify(p => p.CanViewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public void QueryLongerThan64_IsRejectedByTheValidator()
    {
        var validator = new GetMentionSuggestionsQueryValidator();

        validator.Validate(GetMentionSuggestionsQuery.ForPage(_page, new string('a', 64), null)).IsValid.Should().BeTrue();
        validator.Validate(GetMentionSuggestionsQuery.ForPage(_page, new string('a', 65), null)).IsValid.Should().BeFalse();
    }

    private static AccessFacts Facts(int rank) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: "Member",
        ResourceExists: true,
        ResourceAudience: "Private",
        ResourceMemberRole: null,
        HasExplicitResourcePermission: true,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: rank,
        TargetPermissionRank: null)
    {
        ParentBoardId = PolicyBoardId,
        IsBoardChild = true,
    };

    private static readonly Guid PolicyBoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");
}
