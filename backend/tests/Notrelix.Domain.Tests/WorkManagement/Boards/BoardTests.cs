using FluentAssertions;
using Notrelix.Domain.WorkManagement;
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
        ((IHasDomainEvents)board).ClearDomainEvents();

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
        ((IHasDomainEvents)board).ClearDomainEvents();

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
        ((IHasDomainEvents)board).ClearDomainEvents();

        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldClearIsArchivedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var unarchivedBy = Guid.NewGuid();
        board.Unarchive(unarchivedBy, DateTimeOffset.UtcNow);

        board.IsArchived.Should().BeFalse();

        board.DomainEvents.Should().ContainSingle(e => e is BoardUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldBeNoOp_WhenNotArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        board.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ShouldSetIsDeletedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var deletedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        board.Delete(deletedBy, now);

        board.IsDeleted.Should().BeTrue();
        board.DeletedAt.Should().Be(now);
        board.DeletedBy.Should().Be(deletedBy);

        board.DomainEvents.Should().ContainSingle(e => e is BoardDeletedDomainEvent);
    }

    [Fact]
    public void Delete_ShouldBeNoOp_WhenAlreadyDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        board.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldClearIsDeletedAndRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var restoredBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        board.Restore(restoredBy, now);

        board.IsDeleted.Should().BeFalse();

        board.DomainEvents.Should().ContainSingle(e => e is BoardRestoredDomainEvent);
    }

    [Fact]
    public void Restore_ShouldBeNoOp_WhenNotDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        board.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_ShouldThrow_WhenBoardIsDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.Rename("New Title", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Archive_ShouldThrow_WhenBoardIsDeleted()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDescription_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        board.UpdateDescription("New description", updatedBy, DateTimeOffset.UtcNow);

        board.Description.Should().Be("New description");

        board.DomainEvents.Should().ContainSingle(e => e is BoardDescriptionUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateDescription_ShouldThrow_WhenBoardIsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.UpdateDescription("New desc", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot update description of an archived board.");
    }

    [Fact]
    public void ChangeVisibility_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        board.ChangeVisibility(BoardVisibility.Private, updatedBy, DateTimeOffset.UtcNow);

        board.Visibility.Should().Be(BoardVisibility.Private);

        board.DomainEvents.Should().ContainSingle(e => e is BoardVisibilityChangedDomainEvent);
    }

    [Fact]
    public void ChangeVisibility_ToPublicLink_IsRefusedAndChangesNothing()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        var before = board.Visibility;
        ((IHasDomainEvents)board).ClearDomainEvents();

        var act = () => board.ChangeVisibility(BoardVisibility.PublicLink, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        board.Visibility.Should().Be(before);
        board.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetDefaultGroup_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var groupId = Guid.NewGuid();
        board.SetDefaultGroup(groupId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.DefaultItemGroupId.Should().Be(groupId);

        board.DomainEvents.Should().ContainSingle(e => e is BoardDefaultGroupSetDomainEvent);
    }

    [Fact]
    public void GenerateNextItemIdentity_ShouldIncrementSequence()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow, itemKeyPrefix: "TASK");
        ((IHasDomainEvents)board).ClearDomainEvents();

        var (seq1, key1) = board.GenerateNextItemIdentity(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var (seq2, key2) = board.GenerateNextItemIdentity(Guid.NewGuid(), DateTimeOffset.UtcNow);

        seq1.Should().Be(1);
        key1.Should().Be("TASK-1");
        seq2.Should().Be(2);
        key2.Should().Be("TASK-2");
    }

    [Fact]
    public void GenerateNextItemIdentity_ShouldThrow_WhenBoardIsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow);
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => board.GenerateNextItemIdentity(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot generate item identity for an archived board.");
    }

    private static Board BoardWithPrefix(string? prefix = "WEB") =>
        Board.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Valid Title", null, DateTimeOffset.UtcNow, itemKeyPrefix: prefix);

    [Fact]
    public void ChangeItemKeyPrefix_ChangesThePrefix_AndRaisesTheEvent()
    {
        var board = BoardWithPrefix();
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;

        board.ChangeItemKeyPrefix("OPS2", false, Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.ItemKeyPrefix.Should().Be("OPS2");
        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardItemKeyPrefixChangedDomainEvent);
    }

    [Fact]
    public void ChangeItemKeyPrefix_IsANoOp_WhenUnchanged()
    {
        var board = BoardWithPrefix("WEB");
        var version = board.Version;

        board.ChangeItemKeyPrefix("WEB", true, Guid.NewGuid(), DateTimeOffset.UtcNow);

        board.Version.Should().Be(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCDEFG")]
    [InlineData("web")]
    [InlineData("WE-B")]
    [InlineData("WÉB")]
    public void ChangeItemKeyPrefix_RejectsAnInvalidPrefix_AndLeavesTheBoardUnchanged(string prefix)
    {
        var board = BoardWithPrefix("WEB");
        var version = board.Version;

        var act = () => board.ChangeItemKeyPrefix(prefix, false, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Board_InvalidItemKeyPrefix);
        board.ItemKeyPrefix.Should().Be("WEB");
        board.Version.Should().Be(version);
    }

    [Fact]
    public void ChangeItemKeyPrefix_RejectsAPrefixAnotherBoardUses()
    {
        var board = BoardWithPrefix("WEB");

        var act = () => board.ChangeItemKeyPrefix("OPS", true, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Board_ItemKeyPrefixInUse);
        board.ItemKeyPrefix.Should().Be("WEB");
    }

    [Fact]
    public void ChangeItemKeyPrefix_Throws_WhenBoardIsArchived()
    {
        var board = BoardWithPrefix("WEB");
        board.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => board.ChangeItemKeyPrefix("OPS", false, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Board_CannotChangeItemKeyPrefixArchived);
    }
}
