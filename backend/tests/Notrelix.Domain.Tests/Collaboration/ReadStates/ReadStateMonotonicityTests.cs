using FluentAssertions;
using Notrelix.Domain.Collaboration.ReadStates;

namespace Notrelix.Domain.Tests.Collaboration.ReadStates;

public class ReadStateMonotonicityTests
{
    [Fact]
    public void MarkAsRead_ShouldUpdateLastReadAt()
    {
        var state = ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);
        var readAt = DateTimeOffset.UtcNow.AddMinutes(5);

        state.MarkAsRead(readAt);

        state.LastReadAt.Should().Be(readAt);
        state.UnreadCount.Should().Be(0);
    }

    [Fact]
    public void MarkAsRead_ShouldSetLastCommentId()
    {
        var state = ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);
        var commentId = Guid.NewGuid();

        state.MarkAsRead(DateTimeOffset.UtcNow, commentId);

        state.LastReadCommentId.Should().Be(commentId);
    }

    [Fact]
    public void IncrementUnread_ShouldIncreaseCount()
    {
        var state = ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);

        state.IncrementUnread(DateTimeOffset.UtcNow);
        state.IncrementUnread(DateTimeOffset.UtcNow);

        state.UnreadCount.Should().Be(2);
    }

    [Fact]
    public void MarkAsRead_ShouldResetUnreadCount()
    {
        var state = ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);
        state.IncrementUnread(DateTimeOffset.UtcNow);
        state.IncrementUnread(DateTimeOffset.UtcNow);

        state.MarkAsRead(DateTimeOffset.UtcNow);

        state.UnreadCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldSetInitialUnreadToZero()
    {
        var state = ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);

        state.UnreadCount.Should().Be(0);
        state.LastReadAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => ResourceReadState.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ResourceReadState.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => ResourceReadState.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid()), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
