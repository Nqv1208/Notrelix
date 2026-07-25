using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class DeleteBoardFieldTests : WorkManagementHandlerTestBase
{
    private readonly DeleteBoardFieldCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public DeleteBoardFieldTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new DeleteBoardFieldCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_FieldExists_DeletesField()
    {
        var field = CreateBoardField();
        SetupBoardFields(field);

        var command = new DeleteBoardFieldCommand(field.BoardId, field.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBoardFieldCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FieldBelongsToDifferentBoard_ThrowsNotFoundException()
    {
        var boardId1 = Guid.CreateVersion7();
        var boardId2 = Guid.CreateVersion7();
        var field = CreateBoardField(boardId: boardId1);
        SetupBoardFields(field);

        var command = new DeleteBoardFieldCommand(boardId2, field.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_IsIdempotent()
    {
        var field = CreateBoardField();
        field.SoftDelete(TestUserId, TestNow);
        SetupBoardFields(field);

        var command = new DeleteBoardFieldCommand(field.BoardId, field.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardField CreateBoardField(Guid? id = null, Guid? boardId = null)
    {
        var field = Notrelix.Domain.WorkManagement.Fields.BoardField.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Field",
            FieldType.Text,
            FieldSettings.Empty(),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            field.GetType().GetProperty(nameof(BoardField.Id))!.SetValue(field, id.Value);
        return field;
    }
}
