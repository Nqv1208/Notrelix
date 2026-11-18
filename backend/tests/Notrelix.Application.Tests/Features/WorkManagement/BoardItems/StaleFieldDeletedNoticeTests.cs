using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// A write to a field deleted since the client loaded the board answers a precondition failure that names who
/// deleted it. A Workspace Guest reads the neutral "someone" copy instead of a name, as every other change notice
/// does (<c>ActorNotice</c>); a member still reads the name.
/// </summary>
public sealed class StaleFieldDeletedNoticeTests : WorkManagementHandlerTestBase
{
    private const string DeleterName = "Ada Lovelace";
    private readonly Guid _deleter = Guid.CreateVersion7();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkspaceAccessResolver> _access = new();
    private readonly BoardItem _item;

    public StaleFieldDeletedNoticeTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(TestUserId);
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ActorSnapshot(_deleter, DeleterName, null)]);

        var board = CreateBoard();
        _item = CreateBoardItem(boardId: board.Id);
        SetupBoards(board);
        SetupBoardItems(_item);

        var deleted = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Note",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        deleted.Delete(_deleter, TestNow);
        // The handler's own lookup no longer finds the field; the guard's unfiltered lookup finds the deleted row.
        DbContextMock.SetupSequence(c => c.BoardFields)
            .Returns(CreateAsyncDbSet(new List<BoardField>()))
            .Returns(CreateAsyncDbSet(new List<BoardField> { deleted }));
        FieldId = deleted.Id;
    }

    private Guid FieldId { get; }

    private void Viewer(bool guest) =>
        _access.Setup(a => a.ResolveAsync(TestWorkspaceId, TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, TestUserId, true, true, guest));

    private async Task<PreconditionFailedException> WriteAsync(bool withAccess = true) =>
        (await new UpdateBoardItemFieldValueCommandHandler(
                DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object,
                null, _actors.Object, RequestContextMock.Object, null, withAccess ? _access.Object : null)
            .Invoking(h => h.Handle(new UpdateBoardItemFieldValueCommand(_item.Id, FieldId, "late", _item.Version), CancellationToken.None))
            .Should().ThrowAsync<PreconditionFailedException>()).Which;

    private async Task<PreconditionFailedException> ClearAsync() =>
        (await new ClearFieldValueCommandHandler(
                DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object,
                null, _actors.Object, RequestContextMock.Object, _access.Object)
            .Invoking(h => h.Handle(new ClearFieldValueCommand(_item.Id, FieldId, _item.Version), CancellationToken.None))
            .Should().ThrowAsync<PreconditionFailedException>()).Which;

    [Fact]
    public async Task Write_ToADeletedField_NamesTheDeleter_ToAMember()
    {
        Viewer(guest: false);

        var error = await WriteAsync();

        error.Extensions["fieldName"].Should().Be("Note");
        error.Extensions["changedByName"].Should().Be(DeleterName);
    }

    [Fact]
    public async Task Write_ToADeletedField_NamesNoOne_ToAGuest()
    {
        Viewer(guest: true);

        var error = await WriteAsync();

        error.Extensions["fieldName"].Should().Be("Note");
        error.Extensions["changedByName"].Should().BeNull();
    }

    [Fact]
    public async Task Clear_OfADeletedField_NamesNoOne_ToAGuest()
    {
        Viewer(guest: true);

        (await ClearAsync()).Extensions["changedByName"].Should().BeNull();
    }

    [Fact]
    public async Task Write_ToADeletedField_NamesNoOne_WhenWorkspaceAccessIsUnknown()
    {
        var error = await WriteAsync(withAccess: false);

        error.Extensions["changedByName"].Should().BeNull("a notice never names anyone the access check could not clear");
    }
}
