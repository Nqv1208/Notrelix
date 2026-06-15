using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class CommentTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var createdBy = Guid.NewGuid();
        
        var comment = Comment.Create(Guid.NewGuid(), target, "Test comment", createdBy, DateTimeOffset.UtcNow);

        comment.Content.Should().Be("Test comment");
        comment.Target.Should().Be(target);
        comment.CreatedBy.Should().Be(createdBy);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentCreatedEvent);
    }

    [Fact]
    public void Resolve_ShouldUpdateStatus_AndRaiseEvent()
    {
        var comment = Comment.Create(Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), "Content", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.ClearDomainEvents();

        var resolvedBy = Guid.NewGuid();
        comment.Resolve(resolvedBy, DateTimeOffset.UtcNow);

        comment.CommentStatus.Should().Be(CommentStatus.Resolved);
        comment.UpdatedBy.Should().Be(resolvedBy);
        comment.DomainEvents.Should().ContainSingle(e => e is CommentResolvedEvent);
    }
}
