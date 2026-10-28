using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.WorkManagement;
using DomainRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class DeleteBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly DeleteBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _boardId = Guid.CreateVersion7();

    public DeleteBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new DeleteBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object,
            new WorkPlacementResolver(new NoLock()));
    }

    [Fact]
    public async Task Handle_EmptyGroup_DeletesWithoutATarget()
    {
        var group = CreateBoardGroup();
        var other = CreateBoardGroup();
        SetupBoardGroups(group, other);

        var result = await _handler.Handle(new DeleteBoardGroupCommand(group.Id, group.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        group.IsDeleted.Should().BeTrue();
        other.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBoardGroupCommand(Guid.CreateVersion7(), 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_IsIdempotent()
    {
        var group = CreateBoardGroup();
        group.Delete(TestUserId, TestNow);
        SetupBoardGroups(group);

        var result = await _handler.Handle(new DeleteBoardGroupCommand(group.Id, group.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TheLastGroupOfABoard_IsRefused()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        await _handler.Invoking(h => h.Handle(new DeleteBoardGroupCommand(group.Id, group.Version), CancellationToken.None))
            .Should().ThrowAsync<DomainRuleException>()
            .Where(e => e.RuleCode == WorkManagementRuleCodes.WorkManagement_Group_CannotDeleteLast);

        group.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GroupWithItemsAndNoTarget_IsRefusedAndNothingChanges()
    {
        var group = CreateBoardGroup();
        var other = CreateBoardGroup();
        var item = CreateBoardItem(boardId: _boardId, groupId: group.Id);
        SetupBoardGroups(group, other);
        SetupBoardItems(item);

        var result = await _handler.Handle(new DeleteBoardGroupCommand(group.Id, group.Version), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Single().Code.Should().Be(WorkManagementRuleCodes.WorkManagement_Group_ItemsNeedTargetGroup);
        group.IsDeleted.Should().BeFalse();
        item.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task Handle_GroupWithItemsAndATarget_MovesItemsAfterTheTargetsOwnInOrder()
    {
        var group = CreateBoardGroup();
        var target = CreateBoardGroup();
        var ownItem = CreateBoardItem(boardId: _boardId, groupId: target.Id);
        var first = CreateBoardItem(boardId: _boardId, groupId: group.Id, position: "a0");
        var second = CreateBoardItem(boardId: _boardId, groupId: group.Id, position: "a1");
        var archived = CreateBoardItem(boardId: _boardId, groupId: group.Id, position: "a2");
        archived.Archive(TestUserId, TestNow);
        SetupBoardGroups(group, target);
        SetupBoardItems(ownItem, first, second, archived);

        var result = await _handler.Handle(new DeleteBoardGroupCommand(group.Id, group.Version, target.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        group.IsDeleted.Should().BeTrue();
        new[] { first, second, archived }.Should().OnlyContain(i => i.GroupId == target.Id);
        var order = new[] { ownItem, first, second, archived }.Select(i => i.Position).ToList();
        order.Should().BeInAscendingOrder();
        order.Distinct().Should().HaveCount(4);
    }

    [Fact]
    public async Task Handle_ATargetOnAnotherBoard_IsRefused()
    {
        var group = CreateBoardGroup();
        var other = CreateBoardGroup();
        var foreign = CreateBoardGroup(boardId: Guid.CreateVersion7());
        var item = CreateBoardItem(boardId: _boardId, groupId: group.Id);
        SetupBoardGroups(group, other, foreign);
        SetupBoardItems(item);

        var result = await _handler.Handle(new DeleteBoardGroupCommand(group.Id, group.Version, foreign.Id), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Single().Code.Should().Be(WorkManagementRuleCodes.WorkManagement_Group_InvalidTargetGroup);
        item.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task Handle_TheGroupItselfAsTarget_IsRefused()
    {
        var group = CreateBoardGroup();
        var other = CreateBoardGroup();
        var item = CreateBoardItem(boardId: _boardId, groupId: group.Id);
        SetupBoardGroups(group, other);
        SetupBoardItems(item);

        await _handler.Invoking(h => h.Handle(new DeleteBoardGroupCommand(group.Id, group.Version, group.Id), CancellationToken.None))
            .Should().ThrowAsync<DomainRuleException>()
            .Where(e => e.RuleCode == WorkManagementRuleCodes.WorkManagement_Group_InvalidTargetGroup);
    }

    private BoardGroup CreateBoardGroup(Guid? boardId = null)
    {
        return Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? _boardId,
            "Test Group",
            Color.Create("#808080"),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
    }

    private sealed class NoLock : IWorkOrderingScopeLock
    {
        public Task AcquireAsync(WorkOrderingScope scope, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
