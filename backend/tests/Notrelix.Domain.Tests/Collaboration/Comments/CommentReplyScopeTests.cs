using FluentAssertions;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Domain.Tests.Collaboration.Comments;

public class CommentReplyScopeTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private ResourceRef Target => ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), _workspaceId);

    [Fact]
    public void ParentCommentContext_MissingParent_ShouldThrow()
    {
        var act = () => ParentCommentContext.Create(_accountId, _workspaceId, Guid.Empty, Target);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ParentCommentContext_NullTarget_ShouldThrow()
    {
        var act = () => ParentCommentContext.Create(_accountId, _workspaceId, Guid.NewGuid(), null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateReply_WithNullParentContext_ShouldThrow()
    {
        var act = () => Comment.CreateReply(_accountId, _workspaceId, Target, "Reply", _actorId, _now, null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateReply_ShouldRaiseReplyEvent()
    {
        var parentId = Guid.NewGuid();
        var target = Target;
        var ctx = ParentCommentContext.Create(_accountId, _workspaceId, parentId, target);
        var reply = Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, ctx);
        reply.DomainEvents.Should().ContainSingle(e => e is CommentReplyCreatedDomainEvent);
        var evt = (CommentReplyCreatedDomainEvent)reply.DomainEvents
            .First(e => e is CommentReplyCreatedDomainEvent);
        evt.ParentCommentId.Should().Be(parentId);
        evt.Target.Should().Be(target);
    }

    [Fact]
    public void CreateReply_DeletedParentContext_ShouldThrow()
    {
        var parentId = Guid.NewGuid();
        var target = Target;
        var ctx = ParentCommentContext.Create(_accountId, _workspaceId, parentId, target, isDeleted: true);
        var act = () => Comment.CreateReply(_accountId, _workspaceId, target, "Reply", _actorId, _now, ctx);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Comment_CannotReplyToDeleted);
    }
}
