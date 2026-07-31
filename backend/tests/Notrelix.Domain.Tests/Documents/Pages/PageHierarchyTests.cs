using FluentAssertions;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Documents.Pages;

public class PageHierarchyTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_RootPage_ShouldHaveNullParent()
    {
        var page = Page.Create(_accountId, _workspaceId, "Root", _actorId, _now);
        page.ParentId.Should().BeNull();
    }

    [Fact]
    public void Create_ChildPage_ShouldSetParent()
    {
        var parentId = Guid.NewGuid();
        var page = Page.Create(_accountId, _workspaceId, "Child", _actorId, _now, parentId);
        page.ParentId.Should().Be(parentId);
    }

    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.Valid, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [Fact]
    public void Move_ShouldChangeParent()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        var newParent = Guid.NewGuid();
        page.Move(newParent, _actorId, _now, _ => null);
        page.ParentId.Should().Be(newParent);
    }

    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.NoOp, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [Fact]
    public void Move_NoOp_ShouldNotIncrementVersion()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        var before = page.Version;
        page.Move(null, _actorId, _now, _ => null);
        page.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.Valid, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.Version, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [Fact]
    public void Move_ShouldIncrementVersion()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        var before = page.Version;
        page.Move(Guid.NewGuid(), _actorId, _now, _ => null);
        page.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.Invalid, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [Fact]
    public void Move_ToCycle_ShouldThrow()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        var act = () => page.Move(page.Id, _actorId, _now, id => id == page.Id ? page.Id : null);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Page), nameof(Page.Move), MutationScenario.Invalid, typeof(Guid?), typeof(Guid), typeof(DateTimeOffset), typeof(System.Func<System.Guid,System.Guid?>))]
    [Fact]
    public void Move_Archived_ShouldThrow()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        page.Archive(_actorId, _now);
        var act = () => page.Move(Guid.NewGuid(), _actorId, _now, _ => null);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Page), nameof(Page.Rename), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_Archived_ShouldThrow()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        page.Archive(_actorId, _now);
        var act = () => page.Rename("New", _actorId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Page), nameof(Page.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetStatus()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        page.Delete(_actorId, _now);
    }

    [CoversMutation(typeof(Page), nameof(Page.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldSetStatusActive()
    {
        var page = Page.Create(_accountId, _workspaceId, "Page", _actorId, _now);
        page.Delete(_actorId, _now);
        page.Restore(_actorId, _now);
        page.Status.Should().Be(PageStatus.Active);
    }
}
