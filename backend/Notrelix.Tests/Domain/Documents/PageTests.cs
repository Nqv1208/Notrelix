using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class PageTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        
        var page = Page.Create(workspaceId, "My Page", createdBy, DateTimeOffset.UtcNow);

        page.Title.Should().Be("My Page");
        page.WorkspaceId.Should().Be(workspaceId);
        page.CreatedBy.Should().Be(createdBy);
        page.DomainEvents.Should().ContainSingle(e => e is PageCreatedEvent);
    }

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var page = Page.Create(Guid.NewGuid(), "Old Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.Rename("New Title", Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.Title.Should().Be("New Title");
        page.DomainEvents.Should().ContainSingle(e => e is PageRenamedEvent);
    }

    [Fact]
    public void Rename_WhenArchived_ShouldThrow()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => page.Rename("New Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Move_ShouldSucceed_AndRaiseEvent()
    {
        var page = Page.Create(Guid.NewGuid(), "Child", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();
        var newParentId = Guid.NewGuid();

        page.Move(newParentId, Guid.NewGuid(), DateTimeOffset.UtcNow, _ => null);

        page.ParentId.Should().Be(newParentId);
        page.DomainEvents.Should().ContainSingle(e => e is PageMovedEvent);
    }

    [Fact]
    public void Move_ToSameParent_ShouldBeNoOp()
    {
        var page = Page.Create(Guid.NewGuid(), "Child", Guid.NewGuid(), DateTimeOffset.UtcNow, parentId: Guid.NewGuid());
        var currentParent = page.ParentId;
        page.ClearDomainEvents();

        page.Move(currentParent, Guid.NewGuid(), DateTimeOffset.UtcNow, _ => null);

        page.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Move_WhenArchived_ShouldThrow()
    {
        var page = Page.Create(Guid.NewGuid(), "Child", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => page.Move(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, _ => null);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Move_ShouldThrow_WhenCreatingCycle()
    {
        var pageId = Guid.NewGuid();
        var page = Page.Create(Guid.NewGuid(), "Child", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var parentId = Guid.NewGuid();
        
        Func<Guid, Guid?> getParentId = (id) => 
        {
            if (id == parentId) return page.Id;
            return null;
        };
        
        Action act = () => Notrelix.Domain.Documents.Rules.PageTreeRules.EnsureNoCycle(page.Id, parentId, getParentId);

        act.Should().Throw<BusinessRuleException>().WithMessage("Page move would create a cycle in the page tree.");
    }

    [Fact]
    public void Archive_ShouldSetStatus_AndRaiseEvent()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.Status.Should().Be(PageStatus.Archived);
        page.DomainEvents.Should().ContainSingle(e => e is PageArchivedEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_ShouldSetStatus_AndRaiseEvent()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.IsDeleted.Should().BeTrue();
        page.Status.Should().Be(PageStatus.SoftDeleted);
        page.DomainEvents.Should().ContainSingle(e => e is PageSoftDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSetStatus_AndRaiseEvent()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.IsDeleted.Should().BeFalse();
        page.Status.Should().Be(PageStatus.Active);
        page.DomainEvents.Should().ContainSingle(e => e is PageRestoredEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var page = Page.Create(Guid.NewGuid(), "Title", Guid.NewGuid(), DateTimeOffset.UtcNow);
        page.ClearDomainEvents();

        page.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        page.DomainEvents.Should().BeEmpty();
    }
}
