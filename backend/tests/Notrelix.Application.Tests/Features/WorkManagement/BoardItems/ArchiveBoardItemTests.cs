using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.SharedKernel.Ordering;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class ArchiveBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ArchiveBoardItemCommandHandler _handler;

    public ArchiveBoardItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new ArchiveBoardItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ArchivesTheItemBumpsItsVersionAndRaisesOneEvent()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var before = item.Version;

        var result = await _handler.Handle(new ArchiveBoardItemCommand(item.Id, before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        item.IsArchived.Should().BeTrue();
        item.Version.Should().Be(before + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemArchivedDomainEvent);
    }

    [Fact]
    public async Task Handle_AlreadyArchived_IsANoOpWithoutVersionBumpOrEvent()
    {
        var item = CreateBoardItem();
        item.Archive(TestUserId, TestNow);
        SetupBoardItems(item);
        ((IHasDomainEvents)item).ClearDomainEvents();
        var before = item.Version;

        var result = await _handler.Handle(new ArchiveBoardItemCommand(item.Id, before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        item.Version.Should().Be(before);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ParentWithSubItems_ArchivesEverySubItemWithIt()
    {
        var parent = CreateBoardItem();
        var one = BoardItem.CreateSubItem(parent, "One", FractionalIndex.Create("a0"), TestUserId, TestNow);
        var two = BoardItem.CreateSubItem(parent, "Two", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardItems(parent, one, two);

        await _handler.Handle(new ArchiveBoardItemCommand(parent.Id, parent.Version), CancellationToken.None);

        new[] { parent, one, two }.Should().OnlyContain(i => i.IsArchived);
        one.ArchivedWithParent.Should().BeTrue();
        two.ArchivedWithParent.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new ArchiveBoardItemCommand(Guid.CreateVersion7(), 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedItem_IsRefusedAndLeavesItUnarchived()
    {
        var item = CreateBoardItem();
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        var act = () => _handler.Handle(new ArchiveBoardItemCommand(item.Id, item.Version), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        item.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ArchivedItem_RefusesADescriptionEditWithAReason()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        await _handler.Handle(new ArchiveBoardItemCommand(item.Id, item.Version), CancellationToken.None);

        var act = () => item.SetDescription("New text", TestUserId, TestNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Validator_RejectsAMissingExpectedVersion()
    {
        new ArchiveBoardItemCommandValidator()
            .Validate(new ArchiveBoardItemCommand(Guid.CreateVersion7(), 0))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Command_RequiresEditorAndAboveToUpdateTheItem()
    {
        new ArchiveBoardItemCommand(Guid.CreateVersion7(), 1).Action.Should().Be(PermissionAction.UpdateItem);
    }
}
