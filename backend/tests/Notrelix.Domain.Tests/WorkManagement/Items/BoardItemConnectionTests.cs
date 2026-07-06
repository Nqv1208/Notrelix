using FluentAssertions;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemConnectionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var relationId = Guid.NewGuid();
        var sourceBoardId = Guid.NewGuid();
        var sourceItemId = Guid.NewGuid();
        var targetBoardId = Guid.NewGuid();
        var targetItemId = Guid.NewGuid();

        var connection = BoardItemConnection.Create(Guid.NewGuid(), workspaceId, relationId, sourceBoardId, sourceItemId, targetBoardId, targetItemId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        connection.WorkspaceId.Should().Be(workspaceId);
        connection.RelationId.Should().Be(relationId);
        connection.SourceBoardId.Should().Be(sourceBoardId);
        connection.SourceItemId.Should().Be(sourceItemId);
        connection.TargetBoardId.Should().Be(targetBoardId);
        connection.TargetItemId.Should().Be(targetItemId);
        connection.SyncStatus.Should().Be(BoardItemSyncStatus.InSync);
    }

    [Fact]
    public void Create_WithCustomSyncStatus_ShouldSetStatus()
    {
        var connection = BoardItemConnection.Create(Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            syncStatus: BoardItemSyncStatus.Pending);

        connection.SyncStatus.Should().Be(BoardItemSyncStatus.Pending);
    }

    [Fact]
    public void Create_WhenSourceEqualsTarget_ShouldThrow()
    {
        var itemId = Guid.NewGuid();

        var act = () => BoardItemConnection.Create(Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), itemId,
            Guid.NewGuid(), itemId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*connect an item to itself*");
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => BoardItemConnection.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyRelationId_ShouldThrow()
    {
        var act = () => BoardItemConnection.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptySourceBoardId_ShouldThrow()
    {
        var act = () => BoardItemConnection.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateSyncStatus_ShouldChangeStatus()
    {
        var connection = CreateConnection();

        connection.UpdateSyncStatus(BoardItemSyncStatus.Pending, DateTimeOffset.UtcNow);

        connection.SyncStatus.Should().Be(BoardItemSyncStatus.Pending);
        connection.UpdatedAt.Should().NotBeNull();
    }

    private static BoardItemConnection CreateConnection()
    {
        return BoardItemConnection.Create(Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
