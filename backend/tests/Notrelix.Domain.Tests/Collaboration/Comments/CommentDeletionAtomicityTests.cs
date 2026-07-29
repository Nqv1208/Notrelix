using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Tests.Freeze;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration.Comments;

public class CommentDeletionAtomicityTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private ResourceRef Target => ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);

[CoversMutation(typeof(Comment), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldTransitionStatus()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.SoftDelete(_actorId, _now);
        comment.CommentStatus.Should().Be(CommentStatus.SoftDeleted);
        comment.IsDeleted.Should().BeTrue();
    }

[CoversMutation(typeof(Comment), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldRaiseEvent()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.SoftDelete(_actorId, _now);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentSoftDeletedDomainEvent);
    }

[CoversMutation(typeof(Comment), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        var before = comment.Version;
        comment.SoftDelete(_actorId, _now);
        comment.Version.Should().Be(before + 1);
    }

[CoversMutation(typeof(Comment), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.SoftDelete(_actorId, _now);
        var before = comment.Version;
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.SoftDelete(_actorId, _now);
        comment.Version.Should().Be(before);
        comment.DomainEvents.Should().BeEmpty();
    }

[CoversMutation(typeof(Comment), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_AfterSoftDelete_ShouldRevertStatus()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.SoftDelete(_actorId, _now);
        comment.Restore(_actorId, _now);
        comment.CommentStatus.Should().Be(CommentStatus.Active);
        comment.IsDeleted.Should().BeFalse();
    }

[CoversMutation(typeof(Comment), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldRaiseEvent()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)comment).ClearDomainEvents();
        comment.Restore(_actorId, _now);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentRestoredDomainEvent);
    }

[CoversMutation(typeof(Comment), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var comment = Comment.Create(_accountId, _workspaceId, Target, "Content", _actorId, _now);
        comment.SoftDelete(_actorId, _now);
        var before = comment.Version;
        comment.Restore(_actorId, _now);
        comment.Version.Should().Be(before + 1);
    }

[CoversMutation(typeof(Comment), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
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
