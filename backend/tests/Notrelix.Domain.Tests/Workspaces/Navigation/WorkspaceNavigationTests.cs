using FluentAssertions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces.Navigation;

/// <summary>
/// WX-BE-001 / WX-BE-002 (WX-DEC-011): personal navigation state stores
/// Board/Doc references only, recents keep the newest 30 per member per
/// Workspace, and pins are capped at 50 per member per Workspace.
/// </summary>
public class WorkspaceNavigationTests
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    private static RecentResource Recent(DateTimeOffset openedAt, string kind = NavigationResourceKinds.Board) =>
        RecentResource.Create(AccountId, WorkspaceId, UserId, kind, Guid.CreateVersion7(), openedAt);

    [Theory]
    [InlineData(NavigationResourceKinds.Board)]
    [InlineData(NavigationResourceKinds.Page)]
    public void RecentResource_Create_ForSupportedKind_StoresReferenceAndScope(string kind)
    {
        var resourceId = Guid.CreateVersion7();

        var recent = RecentResource.Create(AccountId, WorkspaceId, UserId, kind, resourceId, Now);

        recent.AccountId.Should().Be(AccountId);
        recent.WorkspaceId.Should().Be(WorkspaceId);
        recent.UserId.Should().Be(UserId);
        recent.ResourceKind.Should().Be(kind);
        recent.ResourceId.Should().Be(resourceId);
        recent.LastOpenedAt.Should().Be(Now);
        recent.CreatedAt.Should().Be(Now);
        recent.DomainEvents.Should().BeEmpty("navigation state is personal and publishes no business fact");
    }

    [Theory]
    [InlineData("work-management.board-item")]
    [InlineData("documents.block")]
    [InlineData("workspaces.workspace")]
    [InlineData("")]
    [InlineData(null)]
    public void RecentResource_Create_ForUnsupportedKind_Throws(string? kind)
    {
        var act = () => RecentResource.Create(AccountId, WorkspaceId, UserId, kind!, Guid.CreateVersion7(), Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_UnsupportedResourceKind);
    }

    [Fact]
    public void RecentResource_Create_WithEmptyIdentifiers_Throws()
    {
        FluentActions.Invoking(() => RecentResource.Create(Guid.Empty, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now))
            .Should().Throw<BusinessRuleException>();
        FluentActions.Invoking(() => RecentResource.Create(AccountId, WorkspaceId, Guid.Empty, NavigationResourceKinds.Board, Guid.CreateVersion7(), Now))
            .Should().Throw<BusinessRuleException>();
        FluentActions.Invoking(() => RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.Empty, Now))
            .Should().Throw<BusinessRuleException>();
        FluentActions.Invoking(() => RecentResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), default))
            .Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RecordOpen_Newer_MovesLastOpenedForward()
    {
        var recent = Recent(Now);

        var changed = recent.RecordOpen(Now.AddMinutes(1));

        changed.Should().BeTrue();
        recent.LastOpenedAt.Should().Be(Now.AddMinutes(1));
        recent.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void RecordOpen_SameOrOlder_IsSemanticNoOp()
    {
        var recent = Recent(Now);

        recent.RecordOpen(Now).Should().BeFalse("a retried open is a no-op");
        recent.RecordOpen(Now.AddMinutes(-5)).Should().BeFalse("an out-of-order open never moves the entry back");
        recent.LastOpenedAt.Should().Be(Now);
    }

    [Fact]
    public void SelectRecentsToEvict_AtOrBelowLimit_EvictsNothing()
    {
        var entries = Enumerable.Range(0, WorkspaceNavigationRules.RecentRetentionLimit)
            .Select(i => Recent(Now.AddMinutes(i)))
            .ToList();

        WorkspaceNavigationRules.SelectRecentsToEvict(entries).Should().BeEmpty();
    }

    [Fact]
    public void SelectRecentsToEvict_OverLimit_EvictsOldestBeyondThirty()
    {
        var entries = Enumerable.Range(0, WorkspaceNavigationRules.RecentRetentionLimit + 3)
            .Select(i => Recent(Now.AddMinutes(i)))
            .ToList();

        var evicted = WorkspaceNavigationRules.SelectRecentsToEvict(entries);

        evicted.Should().HaveCount(3);
        evicted.Select(e => e.LastOpenedAt).Should().BeEquivalentTo(
            new[] { Now, Now.AddMinutes(1), Now.AddMinutes(2) },
            "the three oldest entries fall outside the 30-entry window");
    }

    [Fact]
    public void SelectRecentsToEvict_TiesAreBrokenDeterministically()
    {
        var entries = Enumerable.Range(0, WorkspaceNavigationRules.RecentRetentionLimit + 1)
            .Select(_ => Recent(Now))
            .ToList();

        var first = WorkspaceNavigationRules.SelectRecentsToEvict(entries);
        var second = WorkspaceNavigationRules.SelectRecentsToEvict(entries.AsEnumerable().Reverse());

        first.Should().ContainSingle();
        second.Should().ContainSingle().Which.Should().BeSameAs(first[0]);
    }

    [Fact]
    public void PinnedResource_Create_BelowCap_StoresReference()
    {
        var resourceId = Guid.CreateVersion7();

        var pin = PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Page, resourceId, WorkspaceNavigationRules.PinLimit - 1, Now);

        pin.UserId.Should().Be(UserId);
        pin.ResourceKind.Should().Be(NavigationResourceKinds.Page);
        pin.ResourceId.Should().Be(resourceId);
        pin.PinnedAt.Should().Be(Now);
        pin.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(WorkspaceNavigationRules.PinLimit)]
    [InlineData(WorkspaceNavigationRules.PinLimit + 1)]
    public void PinnedResource_Create_AtCap_ThrowsStableRuleCode(int currentPinCount)
    {
        var act = () => PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), currentPinCount, Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached);
    }

    [Fact]
    public void PinnedResource_Create_ForUnsupportedKind_Throws()
    {
        var act = () => PinnedResource.Create(AccountId, WorkspaceId, UserId, "collaboration.comment", Guid.CreateVersion7(), 0, Now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_UnsupportedResourceKind);
    }

    [Fact]
    public void NavigationEntries_AreWorkspaceScoped()
    {
        Recent(Now).Should().BeAssignableTo<IWorkspaceScoped>();
        PinnedResource.Create(AccountId, WorkspaceId, UserId, NavigationResourceKinds.Board, Guid.CreateVersion7(), 0, Now)
            .Should().BeAssignableTo<IWorkspaceScoped>();
    }
}
