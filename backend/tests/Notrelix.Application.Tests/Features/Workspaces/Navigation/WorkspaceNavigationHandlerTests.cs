using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.UnpinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Tests.Features.Workspaces.Navigation;

/// <summary>
/// WX-BE-001 / WX-BE-002 handler tests: resource scope validation through
/// IResourceLocator, per-member keying, the recents upsert and trim, the pin
/// cap, idempotent pin/unpin and lock-before-read ordering.
/// </summary>
public class WorkspaceNavigationHandlerTests
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryWorkspaceNavigationStore _store = new();
    private readonly Mock<IResourceLocator> _locator = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private DateTimeOffset _now = Now;

    public WorkspaceNavigationHandlerTests()
    {
        _requestContext.Setup(c => c.UserId).Returns(UserId);
        _requestContext.Setup(c => c.RequireAccountId()).Returns(AccountId);
        _clock.Setup(c => c.UtcNow).Returns(() => _now);
    }

    private void CallerCanSee(IEnumerable<Guid> boards, IEnumerable<Guid> pages)
    {
        _visibility.Setup(v => v.VisibleBoardIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boards.ToList());
        _visibility.Setup(v => v.VisiblePageIdsAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages.ToList());
    }

    private void ResourceExistsIn(Guid resourceId, Guid accountId, Guid workspaceId, string kind = NavigationResourceKinds.Board)
    {
        _locator
            .Setup(l => l.LocateAsync(
                It.Is<ResourceRef>(r => r.ResourceId == resourceId && r.Kind.Value == kind),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLocation(ResourceKind.Create(kind), resourceId, accountId, workspaceId));
    }

    private Guid ExistingBoard()
    {
        var id = Guid.CreateVersion7();
        ResourceExistsIn(id, AccountId, WorkspaceId);
        return id;
    }

    private RecordRecentResourceCommandHandler RecordHandler() => new(_store, _locator.Object, _requestContext.Object, _clock.Object);
    private PinResourceCommandHandler PinHandler() => new(_store, _locator.Object, _requestContext.Object, _clock.Object);
    private UnpinResourceCommandHandler UnpinHandler() => new(_store, _requestContext.Object);

    // ── Recents ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordRecent_NewResource_AddsEntryForCurrentMember()
    {
        var boardId = ExistingBoard();

        var result = await RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        result.Succeeded.Should().BeTrue();
        var entry = _store.Recents.Should().ContainSingle().Subject;
        entry.UserId.Should().Be(UserId);
        entry.AccountId.Should().Be(AccountId);
        entry.WorkspaceId.Should().Be(WorkspaceId);
        entry.ResourceKind.Should().Be(NavigationResourceKinds.Board);
        entry.ResourceId.Should().Be(boardId);
        entry.LastOpenedAt.Should().Be(Now);
        _store.ReadBeforeLock.Should().BeFalse("the member navigation lock is taken before the recents are read");
    }

    [Fact]
    public async Task RecordRecent_SameResourceAgain_UpdatesLastOpenedWithoutDuplicate()
    {
        var boardId = ExistingBoard();
        await RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        _now = Now.AddMinutes(5);
        await RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        _store.Recents.Should().ContainSingle().Which.LastOpenedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public async Task RecordRecent_ThirtyFirstResource_EvictsTheOldestEntry()
    {
        var first = ExistingBoard();
        await RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, first), default);

        for (var i = 1; i <= WorkspaceNavigationRules.RecentRetentionLimit; i++)
        {
            _now = Now.AddMinutes(i);
            await RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, ExistingBoard()), default);
        }

        _store.Recents.Should().HaveCount(WorkspaceNavigationRules.RecentRetentionLimit);
        _store.Recents.Should().NotContain(r => r.ResourceId == first, "the oldest entry falls out of the 30-entry window");
    }

    [Fact]
    public async Task RecordRecent_ResourceInAnotherWorkspace_IsNotFoundAndChangesNothing()
    {
        var foreignBoard = Guid.CreateVersion7();
        ResourceExistsIn(foreignBoard, AccountId, Guid.CreateVersion7());

        var act = () => RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, foreignBoard), default);

        await act.Should().ThrowAsync<NotFoundException>();
        _store.Recents.Should().BeEmpty();
        _store.Locks.Should().BeEmpty("validation happens before any navigation state is touched");
    }

    [Fact]
    public async Task RecordRecent_ResourceInAnotherAccount_IsNotFound()
    {
        var foreignBoard = Guid.CreateVersion7();
        ResourceExistsIn(foreignBoard, Guid.CreateVersion7(), WorkspaceId);

        var act = () => RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Board, foreignBoard), default);

        await act.Should().ThrowAsync<NotFoundException>();
        _store.Recents.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordRecent_UnknownResource_IsNotFound()
    {
        var act = () => RecordHandler().Handle(new RecordRecentResourceCommand(WorkspaceId, NavigationResourceKinds.Page, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRecents_ReturnsOnlyCurrentMemberEntries_NewestFirst()
    {
        var older = RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now);
        var newer = RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Page, Guid.CreateVersion7(), Now.AddHours(1));
        var otherMember = RecentResource.Create(AccountId, WorkspaceId, Guid.CreateVersion7(), NavigationResourceKinds.Board, Guid.CreateVersion7(), Now.AddHours(2));
        var otherWorkspace = RecentResource.Create(AccountId, Guid.CreateVersion7(), UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now.AddHours(3));
        _store.Recents.AddRange([older, newer, otherMember, otherWorkspace]);
        CallerCanSee([older.ResourceId], [newer.ResourceId]);

        var result = await new GetRecentResourcesQueryHandler(_store, _requestContext.Object, _visibility.Object)
            .Handle(new GetRecentResourcesQuery(WorkspaceId), default);

        result.Data!.Select(d => d.ResourceId).Should().Equal(newer.ResourceId, older.ResourceId);
        result.Data![0].Kind.Should().Be(NavigationResourceKinds.Page);
        result.Data![0].LastOpenedAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public async Task GetRecents_OmitsReferencesTheCallerCanNoLongerSee()
    {
        var visibleBoard = RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now);
        var hiddenBoard = RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now.AddMinutes(1));
        var hiddenPage = RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Page, Guid.CreateVersion7(), Now.AddMinutes(2));
        _store.Recents.AddRange([visibleBoard, hiddenBoard, hiddenPage]);
        CallerCanSee([visibleBoard.ResourceId], []);

        var result = await new GetRecentResourcesQueryHandler(_store, _requestContext.Object, _visibility.Object)
            .Handle(new GetRecentResourcesQuery(WorkspaceId), default);

        result.Data!.Select(d => d.ResourceId).Should().Equal(visibleBoard.ResourceId);
    }

    [Fact]
    public async Task GetPins_OmitsReferencesTheCallerCanNoLongerSee()
    {
        var visiblePage = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Page, Guid.CreateVersion7(), 0, Now);
        var hiddenBoard = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), 1, Now.AddMinutes(1));
        _store.Pins.AddRange([visiblePage, hiddenBoard]);
        CallerCanSee([], [visiblePage.ResourceId]);

        var result = await new GetPinnedResourcesQueryHandler(_store, _requestContext.Object, _visibility.Object)
            .Handle(new GetPinnedResourcesQuery(WorkspaceId), default);

        result.Data!.Select(d => d.ResourceId).Should().Equal(visiblePage.ResourceId);
    }

    [Theory]
    [InlineData("work-management.board-item")]
    [InlineData("documents.block")]
    [InlineData("")]
    public void RecordRecentValidator_RejectsUnsupportedKinds(string kind)
    {
        new RecordRecentResourceCommandValidator()
            .Validate(new RecordRecentResourceCommand(WorkspaceId, kind, Guid.CreateVersion7()))
            .IsValid.Should().BeFalse();
    }

    // ── Pins ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pin_NewResource_AddsPin()
    {
        var boardId = ExistingBoard();

        var result = await PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        result.Succeeded.Should().BeTrue();
        var pin = _store.Pins.Should().ContainSingle().Subject;
        pin.UserId.Should().Be(UserId);
        pin.PinnedAt.Should().Be(Now);
        _store.ReadBeforeLock.Should().BeFalse("the pin count is read under the member navigation lock");
    }

    [Fact]
    public async Task Pin_AlreadyPinned_IsNoOpAndKeepsOriginalPinTime()
    {
        var boardId = ExistingBoard();
        await PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        _now = Now.AddDays(1);
        var result = await PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        result.Succeeded.Should().BeTrue();
        _store.Pins.Should().ContainSingle().Which.PinnedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Pin_OverTheCap_IsRejectedWithStableRuleCode()
    {
        for (var i = 0; i < WorkspaceNavigationRules.PinLimit; i++)
            _store.Pins.Add(PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), i, Now));

        var act = () => PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, ExistingBoard()), default);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached);
        _store.Pins.Should().HaveCount(WorkspaceNavigationRules.PinLimit);
    }

    [Fact]
    public async Task Pin_CapCountsOnlyTheCurrentMember()
    {
        for (var i = 0; i < WorkspaceNavigationRules.PinLimit; i++)
            _store.Pins.Add(PinnedResource.Create(AccountId, WorkspaceId, Guid.CreateVersion7(), NavigationResourceKinds.Board, Guid.CreateVersion7(), 0, Now));

        var result = await PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, ExistingBoard()), default);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Pin_ResourceInAnotherWorkspace_IsNotFound()
    {
        var foreignPage = Guid.CreateVersion7();
        ResourceExistsIn(foreignPage, AccountId, Guid.CreateVersion7(), NavigationResourceKinds.Page);

        var act = () => PinHandler().Handle(new PinResourceCommand(WorkspaceId, NavigationResourceKinds.Page, foreignPage), default);

        await act.Should().ThrowAsync<NotFoundException>();
        _store.Pins.Should().BeEmpty();
    }

    [Fact]
    public async Task Unpin_Pinned_RemovesOnlyTheCurrentMembersPin()
    {
        var boardId = ExistingBoard();
        var mine = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, boardId, 0, Now);
        var theirs = PinnedResource.Create(AccountId, WorkspaceId, Guid.CreateVersion7(), NavigationResourceKinds.Board, boardId, 0, Now);
        _store.Pins.AddRange([mine, theirs]);

        var result = await UnpinHandler().Handle(new UnpinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, boardId), default);

        result.Succeeded.Should().BeTrue();
        _store.Pins.Should().ContainSingle().Which.Should().BeSameAs(theirs);
    }

    [Fact]
    public async Task Unpin_NotPinned_IsNoOpSuccess()
    {
        var result = await UnpinHandler().Handle(new UnpinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, Guid.CreateVersion7()), default);

        result.Succeeded.Should().BeTrue();
        _store.Pins.Should().BeEmpty();
    }

    [Fact]
    public async Task Unpin_DoesNotRequireTheResourceToStillExist()
    {
        var deletedBoard = Guid.CreateVersion7();
        _store.Pins.Add(PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, deletedBoard, 0, Now));

        var result = await UnpinHandler().Handle(new UnpinResourceCommand(WorkspaceId, NavigationResourceKinds.Board, deletedBoard), default);

        result.Succeeded.Should().BeTrue();
        _store.Pins.Should().BeEmpty("a stale pin must be removable so it stops counting against the cap");
        _locator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPins_OrderedByPinTimeNewestFirst_ForCurrentMemberOnly()
    {
        var first = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), 0, Now);
        var second = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Page, Guid.CreateVersion7(), 1, Now.AddMinutes(1));
        var other = PinnedResource.Create(AccountId, WorkspaceId, Guid.CreateVersion7(), NavigationResourceKinds.Board, Guid.CreateVersion7(), 0, Now.AddMinutes(2));
        _store.Pins.AddRange([first, second, other]);
        CallerCanSee([first.ResourceId], [second.ResourceId]);

        var result = await new GetPinnedResourcesQueryHandler(_store, _requestContext.Object, _visibility.Object)
            .Handle(new GetPinnedResourcesQuery(WorkspaceId), default);

        result.Data!.Select(d => d.ResourceId).Should().Equal(second.ResourceId, first.ResourceId);
        result.Data![0].PinnedAt.Should().Be(Now.AddMinutes(1));
    }

    [Theory]
    [InlineData("work-management.board-item")]
    [InlineData("workspaces.workspace")]
    public void PinValidators_RejectUnsupportedKinds(string kind)
    {
        new PinResourceCommandValidator().Validate(new PinResourceCommand(WorkspaceId, kind, Guid.CreateVersion7())).IsValid.Should().BeFalse();
        new UnpinResourceCommandValidator().Validate(new UnpinResourceCommand(WorkspaceId, kind, Guid.CreateVersion7())).IsValid.Should().BeFalse();
    }
}
