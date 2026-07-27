using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Domain.Tests.Collaboration.Comments;

public class CommentReplyInvariantTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldNotAcceptParentId()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);

        var comment = Comment.Create(_accountId, _workspaceId, target, "Top-level", _actorId, _now);

        comment.ParentId.Should().BeNull();
    }

    [Fact]
    public void CreateReply_ShouldSetParentId()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target);

        var reply = Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        reply.ParentId.Should().Be(parentCommentId);
    }

    [Fact]
    public void CreateReply_ShouldThrow_WhenTargetMismatch()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var differentTarget = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, differentTarget);

        var act = () => Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        act.Should().Throw<BusinessRuleException>().WithMessage("*same target*");
    }

    [Fact]
    public void CreateReply_ShouldThrow_WhenWorkspaceMismatch()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        // Parent context has different workspace than the reply
        var otherWorkspaceId = Guid.NewGuid();
        var parentContext = ParentCommentContext.Create(_accountId, otherWorkspaceId, parentCommentId, target);

        var act = () => Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        act.Should().Throw<BusinessRuleException>().WithMessage("*same workspace*");
    }

    [Fact]
    public void CreateReply_ShouldThrow_WhenAccountMismatch()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target);
        var otherAccountId = Guid.NewGuid();

        var act = () => Comment.CreateReply(otherAccountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        act.Should().Throw<BusinessRuleException>().WithMessage("*same account*");
    }

    [Fact]
    public void CreateReply_ShouldThrow_WhenParentDeleted()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target, isDeleted: true);

        var act = () => Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        act.Should().Throw<BusinessRuleException>().WithMessage("*deleted*");
    }

    [Fact]
    public void CreateReply_ShouldTrimContent()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target);

        var reply = Comment.CreateReply(_accountId, _workspaceId, target, "  Reply  ", _actorId, _now, parentContext);

        reply.Content.Should().Be("Reply");
    }

    [Fact]
    public void CreateReply_ShouldSetStatusToActive()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target);

        var reply = Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        reply.CommentStatus.Should().Be(CommentStatus.Active);
    }

    [Fact]
    public void CreateReply_ShouldRaiseReplyEvent()
    {
        var parentCommentId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);
        var parentContext = ParentCommentContext.Create(_accountId, _workspaceId, parentCommentId, target);

        var reply = Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, parentContext);

        reply.DomainEvents.Should().ContainSingle(e => e is CommentReplyCreatedDomainEvent);
        var evt = (CommentReplyCreatedDomainEvent)reply.DomainEvents.First(e => e is CommentReplyCreatedDomainEvent);
        evt.ParentCommentId.Should().Be(parentCommentId);
    }

    [Fact]
    public void ParentCommentContext_ShouldRejectEmptyId()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);

        var act = () => ParentCommentContext.Create(_accountId, _workspaceId, Guid.Empty, target);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ParentCommentContext_ShouldRejectNullTarget()
    {
        var act = () => ParentCommentContext.Create(_accountId, _workspaceId, Guid.NewGuid(), null!);

        act.Should().Throw<BusinessRuleException>();
    }
}