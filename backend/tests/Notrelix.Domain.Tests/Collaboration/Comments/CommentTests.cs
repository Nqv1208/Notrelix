using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Domain.Tests.Collaboration;

public class CommentTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var createdBy = Guid.NewGuid();

        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), target, "Test comment", createdBy, DateTimeOffset.UtcNow);

        comment.Content.Should().Be("Test comment");
        comment.Target.Should().Be(target);
        comment.CreatedBy.Should().Be(createdBy);
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => Comment.Create(Guid.NewGuid(), workspaceId, target, "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithParentId_ShouldSetParent()
    {
        var parentId = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Reply", Guid.NewGuid(), DateTimeOffset.UtcNow, parentId: parentId);

        comment.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WithAnchor_ShouldSetAnchor()
    {
        var anchor = CommentAnchor.Create("selector", 5);
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow, anchor: anchor);

        comment.Anchor.Should().Be(anchor);
    }

    [Fact]
    public void UpdateContent_ShouldUpdate_AndRaiseEvent()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Original", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.UpdateContent("Updated", Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.Content.Should().Be("Updated");
        comment.DomainEvents.Should().ContainSingle(e => e is CommentUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateContent_WhenSameContent_ShouldBeNoOp()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Same", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.UpdateContent("Same", Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateContent_WhenDeleted_ShouldThrow()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => comment.UpdateContent("New", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Resolve_ShouldUpdateStatus_AndRaiseEvent()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        var resolvedBy = Guid.NewGuid();
        comment.Resolve(resolvedBy, DateTimeOffset.UtcNow);

        comment.CommentStatus.Should().Be(CommentStatus.Resolved);
        comment.UpdatedBy.Should().Be(resolvedBy);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentResolvedDomainEvent);
    }

    [Fact]
    public void Resolve_WhenAlreadyResolved_ShouldBeNoOp()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WhenDeleted_ShouldThrow()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => comment.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldSetStatus_AndRaiseEvent()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.IsDeleted.Should().BeTrue();
        comment.CommentStatus.Should().Be(CommentStatus.SoftDeleted);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentSoftDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSetStatus_AndRaiseEvent()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.IsDeleted.Should().BeFalse();
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentRestoredDomainEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        comment.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        comment.DomainEvents.Should().BeEmpty();
    }
}
