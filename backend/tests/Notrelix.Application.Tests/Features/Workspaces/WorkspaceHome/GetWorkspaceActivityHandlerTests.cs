using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

namespace Notrelix.Application.Tests.Features.Workspaces.WorkspaceHome;

/// <summary>
/// The workspace activity read (UC-WSP-N03): a fact about a Board or Page the caller cannot read is left out and its actor is
/// never looked up, a fact that does not say which Board or Page it is about is never shown, the page is bounded and walks the
/// feed by cursor without a repeat or a gap, and an unreadable stretch is scanned only so far before a short page returns.
/// </summary>
public sealed class GetWorkspaceActivityHandlerTests : WorkspaceHandlerTestBase
{
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeFeed _feed = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly List<Guid> _lookedUp = [];
    private readonly HashSet<Guid> _readableBoards = [];
    private readonly HashSet<Guid> _readablePages = [];

    public GetWorkspaceActivityHandlerTests()
    {
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _readableBoards.ToList());
        _visibility.Setup(v => v.VisibleArchivedBoardIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _visibility.Setup(v => v.VisiblePageIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _readablePages.ToList());
        _visibility.Setup(v => v.VisibleTrashedPageIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
            {
                _lookedUp.AddRange(ids);
                return ids.Select(id => new ActorSnapshot(id, $"Person {id:N}", null)).ToList();
            });
    }

    private Task<Notrelix.Application.Common.Models.Result<Notrelix.Application.Common.Models.CursorPage<WorkspaceActivityEntryDto>>> Read(
        string? cursor = null, int? limit = null) =>
        new GetWorkspaceActivityQueryHandler(DbContextMock.Object, _feed, _visibility.Object, _actors.Object, RequestContextMock.Object)
            .Handle(new GetWorkspaceActivityQuery(TestWorkspaceId, cursor, limit), CancellationToken.None);

    private WorkspaceActivityFact Fact(int minutesAgo, WorkspaceActivityScopeKind kind, Guid? resource = null, Guid? actor = null)
    {
        var fact = new WorkspaceActivityFact(
            Guid.CreateVersion7(), "work.board-created", actor ?? Guid.CreateVersion7(), $"fact {minutesAgo}", "Board", resource ?? Guid.CreateVersion7(),
            null, new WorkspaceActivityScope(kind, resource), Start.AddMinutes(-minutesAgo));
        _feed.Facts.Add(fact);
        return fact;
    }

    [Fact]
    public async Task AFactAboutABoardTheCallerCannotRead_IsLeftOut_AndItsActorIsNeverLookedUp()
    {
        var readable = Guid.CreateVersion7();
        var hidden = Guid.CreateVersion7();
        _readableBoards.Add(readable);
        var shown = Fact(1, WorkspaceActivityScopeKind.Board, readable);
        var hiddenActor = Guid.CreateVersion7();
        Fact(2, WorkspaceActivityScopeKind.Board, hidden, hiddenActor);

        var page = (await Read()).Data!;

        page.Items.Select(i => i.Id).Should().Equal([shown.Id], "a Private Board's activity is not shown to a member who cannot read the Board");
        _lookedUp.Should().NotContain(hiddenActor, "a person named only by a hidden fact is not disclosed");
    }

    [Fact]
    public async Task AFactAboutAPageTheCallerCannotRead_IsLeftOut()
    {
        var readable = Guid.CreateVersion7();
        _readablePages.Add(readable);
        var shown = Fact(1, WorkspaceActivityScopeKind.Page, readable);
        Fact(2, WorkspaceActivityScopeKind.Page, Guid.CreateVersion7());

        (await Read()).Data!.Items.Select(i => i.Id).Should().Equal(shown.Id);
    }

    [Fact]
    public async Task AFactThatDoesNotSayWhichResourceItIsAbout_IsNeverShown_AWorkspaceFactAlwaysIs()
    {
        Fact(1, WorkspaceActivityScopeKind.Unresolved);
        var member = Fact(2, WorkspaceActivityScopeKind.Workspace);

        (await Read()).Data!.Items.Select(i => i.Id).Should().Equal(member.Id);
    }

    [Fact]
    public async Task ThePage_IsBoundedAndWalksTheFeedByCursor_WithoutARepeatOrAGap()
    {
        for (var i = 1; i <= 7; i++) Fact(i, WorkspaceActivityScopeKind.Workspace);
        var all = _feed.Facts.OrderByDescending(f => f.OccurredAt).Select(f => f.Id).ToList();

        var seen = new List<Guid>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = (await Read(cursor, 3)).Data!;
            page.Items.Length.Should().BeLessThanOrEqualTo(3);
            seen.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
            pages++;
        } while (cursor is not null && pages < 10);

        seen.Should().Equal(all, "newest first, every fact once");
        pages.Should().Be(3);
    }

    [Fact]
    public async Task AnUnreadableStretch_IsScannedOnlySoFar_ThenAShortPageReturnsACursorThatContinues()
    {
        var readable = Guid.CreateVersion7();
        _readableBoards.Add(readable);
        var hidden = Guid.CreateVersion7();
        for (var i = 1; i <= 600; i++) Fact(i, WorkspaceActivityScopeKind.Board, hidden);
        var older = Fact(700, WorkspaceActivityScopeKind.Board, readable);

        var first = (await Read()).Data!;
        first.Items.Should().BeEmpty("500 facts were scanned and none could be read");
        first.NextCursor.Should().NotBeNull("the scan stopped, the feed did not end");
        _feed.Reads.Should().Be(5, "the scan is bounded");

        var second = (await Read(first.NextCursor)).Data!;
        second.Items.Select(i => i.Id).Should().Equal(older.Id);
        second.NextCursor.Should().BeNull();
    }

    [Fact]
    public void TheLimit_IsBoundedAndABadCursorIsRefused()
    {
        var validator = new GetWorkspaceActivityQueryValidator();

        validator.Validate(new GetWorkspaceActivityQuery(TestWorkspaceId, null, GetWorkspaceActivityQuery.MaxLimit)).IsValid.Should().BeTrue();
        validator.Validate(new GetWorkspaceActivityQuery(TestWorkspaceId, null, GetWorkspaceActivityQuery.MaxLimit + 1)).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspaceActivityQuery(TestWorkspaceId, null, 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspaceActivityQuery(TestWorkspaceId, "not-a-cursor")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ANeverSeenWorkspace_IsNotFound()
    {
        var read = () => new GetWorkspaceActivityQueryHandler(
                DbContextMock.Object, _feed, _visibility.Object, _actors.Object, RequestContextMock.Object)
            .Handle(new GetWorkspaceActivityQuery(Guid.CreateVersion7()), CancellationToken.None);

        await read.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void TheScope_NamesTheBoardOrPageAFactIsAbout_AndNothingElse()
    {
        var board = Guid.CreateVersion7();
        var item = Guid.CreateVersion7();
        var page = Guid.CreateVersion7();

        WorkspaceActivityScopes.Of("work", "Board", board, null, null, null, null)
            .Should().Be(new WorkspaceActivityScope(WorkspaceActivityScopeKind.Board, board));
        WorkspaceActivityScopes.Of("work", "BoardItem", item, "Board", board, ItemActivityTypes.ResourceKind, item)
            .Should().Be(new WorkspaceActivityScope(WorkspaceActivityScopeKind.Board, board));
        WorkspaceActivityScopes.Of("documents", "Page", page, null, null, PageActivityTypes.ResourceKind, page)
            .Should().Be(new WorkspaceActivityScope(WorkspaceActivityScopeKind.Page, page));
        WorkspaceActivityScopes.Of("collaboration", "Comment", Guid.CreateVersion7(), "documents.page", page, null, null)
            .Should().Be(new WorkspaceActivityScope(WorkspaceActivityScopeKind.Page, page));
        WorkspaceActivityScopes.Of("workspace", "WorkspaceMember", Guid.CreateVersion7(), null, null, null, null)
            .Kind.Should().Be(WorkspaceActivityScopeKind.Workspace);

        // An Item row without its Board, and a comment or mention on an Item, cannot be tied to a Board: never shown.
        WorkspaceActivityScopes.Of("work", "BoardItem", item, null, null, ItemActivityTypes.ResourceKind, item)
            .Kind.Should().Be(WorkspaceActivityScopeKind.Unresolved);
        WorkspaceActivityScopes.Of("collaboration", "Comment", Guid.CreateVersion7(), "work-management.board-item", item, null, null)
            .Kind.Should().Be(WorkspaceActivityScopeKind.Unresolved);
        WorkspaceActivityScopes.Of("collaboration", "Mention", Guid.CreateVersion7(), "work-management.board-item", item, null, null)
            .Kind.Should().Be(WorkspaceActivityScopeKind.Unresolved);
    }

    private sealed class FakeFeed : IWorkspaceActivityFeed
    {
        public List<WorkspaceActivityFact> Facts { get; } = [];
        public int Reads { get; private set; }

        public Task<IReadOnlyList<WorkspaceActivityFact>> ListAsync(
            Guid workspaceId, WorkspaceActivityPosition? after, int take, CancellationToken cancellationToken)
        {
            Reads++;
            IReadOnlyList<WorkspaceActivityFact> rows = Facts
                .OrderByDescending(f => f.OccurredAt).ThenByDescending(f => f.Id)
                .Where(f => after is not { } p
                    || f.OccurredAt < p.OccurredAt || (f.OccurredAt == p.OccurredAt && f.Id.CompareTo(p.Id) < 0))
                .Take(take).ToList();
            return Task.FromResult(rows);
        }
    }
}
