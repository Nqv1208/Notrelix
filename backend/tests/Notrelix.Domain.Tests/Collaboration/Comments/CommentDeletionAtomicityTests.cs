using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Collaboration.Comments;

public class CommentDeletionAtomicityTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private ResourceRef Target => ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);

    [CoversMutation(typeof(Comment), nameof(Comment.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldTransitionStatus()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.Delete(_actorId, _now);
        comment.IsDeleted.Should().BeTrue();
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldRaiseEvent()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.Delete(_actorId, _now);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentDeletedDomainEvent);
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        var before = comment.Version;
        comment.Delete(_actorId, _now);
        comment.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.Delete(_actorId, _now);
        var before = comment.Version;
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.Delete(_actorId, _now);
        comment.Version.Should().Be(before);
        comment.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Restore_AfterDelete_ShouldRevertStatus()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.Delete(_actorId, _now);
        comment.Restore(_actorId, _now);
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.IsDeleted.Should().BeFalse();
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldRaiseEvent()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.Delete(_actorId, _now);
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.Restore(_actorId, _now);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentRestoredDomainEvent);
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.Delete(_actorId, _now);
        var before = comment.Version;
        comment.Restore(_actorId, _now);
        comment.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(Comment), nameof(Comment.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        var before = comment.Version;
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.Restore(_actorId, _now);
        comment.Version.Should().Be(before);
        comment.DomainEvents.Should().BeEmpty();
    }
}
