using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var board = Board.Create(workspaceId, createdBy, "My Board", "Description", DateTimeOffset.UtcNow);

        board.WorkspaceId.Should().Be(workspaceId);
        board.Title.Should().Be("My Board");
        board.Description.Should().Be("Description");
        board.Visibility.Should().Be(BoardVisibility.Workspace);
        board.IsArchived.Should().BeFalse();
        board.CreatedBy.Should().Be(createdBy);

        board.DomainEvents.Should().ContainSingle(e => e is BoardCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldThrow_WhenTitleIsEmpty()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        Action act = () => Board.Create(workspaceId, createdBy, "   ", null, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Rename_ShouldUpdateTitleAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Old Title", null, DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        board.Rename("New Title", updatedBy, DateTimeOffset.UtcNow);

        board.Title.Should().Be("New Title");
        board.UpdatedBy.Should().Be(updatedBy);

        board.DomainEvents.Should().ContainSingle(e => e is BoardRenamedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldSetIsArchivedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Board", null, DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        var archivedBy = Guid.NewGuid();
        board.Archive(archivedBy, DateTimeOffset.UtcNow);

        board.IsArchived.Should().BeTrue();
        board.UpdatedBy.Should().Be(archivedBy);
        board.DomainEvents.Should().ContainSingle(e => e is BoardArchivedDomainEvent);
    }

    [Fact]
    public void Rename_ShouldThrow_WhenBoardIsDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Board", null, DateTimeOffset.UtcNow);
        board.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.Rename("New Title", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
