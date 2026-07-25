using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class UpdateBoardFieldTests : WorkManagementHandlerTestBase
{
    private readonly UpdateBoardFieldCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public UpdateBoardFieldTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UpdateBoardFieldCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithSettings_UpdatesSettings()
    {
        var field = CreateBoardField();
        SetupBoardFields(field);

        var command = new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, "{\"newSetting\":\"value\"}");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardFieldCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, null);

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

        var command = new UpdateBoardFieldCommand(boardId2, field.Id, null, null, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullSettings_NoChanges()
    {
        var field = CreateBoardField();
        SetupBoardFields(field);

        var command = new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, null);

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
