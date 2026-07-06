using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var board = Board.Create(accountId, workspaceId, createdBy, "My Board", "Description", DateTimeOffset.UtcNow);

        board.AccountId.Should().Be(accountId);
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
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        Action act = () => Board.Create(accountId, workspaceId, createdBy, "   ", null, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Rename_ShouldUpdateTitleAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Old Title", null, DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        board.Rename("New Title", updatedBy, DateTimeOffset.UtcNow);

        board.Title.Should().Be("New Title");
        board.UpdatedBy.Should().Be(updatedBy);

        board.DomainEvents.Should().ContainSingle(e => e is BoardRenamedDomainEvent);
    }

    [Fact]
    public void Rename_ShouldThrow_WhenTitleIsEmpty()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);

        Action act = () => board.Rename("   ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Rename_ShouldThrow_WhenBoardIsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.Rename("New Title", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename an archived board.");
    }

    [Fact]
    public void Archive_ShouldSetIsArchivedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        var archivedBy = Guid.NewGuid();
        board.Archive(archivedBy, DateTimeOffset.UtcNow);

        board.IsArchived.Should().BeTrue();

        board.DomainEvents.Should().ContainSingle(e => e is BoardArchivedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldBeNoOp_WhenAlreadyArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldClearIsArchivedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        var unarchivedBy = Guid.NewGuid();
        board.Unarchive(unarchivedBy, DateTimeOffset.UtcNow);

        board.IsArchived.Should().BeFalse();

        board.DomainEvents.Should().ContainSingle(e => e is BoardUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldBeNoOp_WhenNotArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.ClearDomainEvents();

        board.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }
}
