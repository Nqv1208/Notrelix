using Notrelix.Application.Features.WorkManagement.BoardFields.Queries.PreviewFormula;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class PreviewFormulaTests : WorkManagementHandlerTestBase
{
    private readonly PreviewFormulaQueryHandler _handler;
    private readonly BoardItem _item;
    private readonly Guid _boardId;

    public PreviewFormulaTests()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _handler = new PreviewFormulaQueryHandler(DbContextMock.Object, currentUser.Object, DateTimeProviderMock.Object);

        var board = CreateBoard();
        _boardId = board.Id;
        _item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(_item);
        var hours = BoardField.Create(TestAccountId, TestWorkspaceId, board.Id, "Giờ", FieldType.Number, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(hours);
        var value = FieldValue.Create(JsonValue.Create("6")!);
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue> { BoardItemValue.Create(_item.Id, hours.Id, value) }));
    }

    [Fact]
    public async Task ADraftFormula_ShowsWhatItWouldComputeOnTheItem()
    {
        var result = await _handler.Handle(new PreviewFormulaQuery(_boardId, _item.Id, "[Giờ] * 2"), CancellationToken.None);

        result.State.Should().Be("ok");
        result.Value!.Value.GetDecimal().Should().Be(12);
    }

    [Fact]
    public async Task ADraftThatReadsAnUnknownField_IsAnErrorState_AndOneThatDoesNotParseIsAnInvalidDefinition()
    {
        var unknown = await _handler.Handle(new PreviewFormulaQuery(_boardId, _item.Id, "[Không có] + 1"), CancellationToken.None);
        var broken = await _handler.Handle(new PreviewFormulaQuery(_boardId, _item.Id, "[Giờ] *"), CancellationToken.None);

        unknown.State.Should().Be("error");
        unknown.ErrorCode.Should().Be("unknown-field");
        broken.State.Should().Be("error");
        broken.ErrorCode.Should().Be("invalid-definition");
    }

    [Fact]
    public async Task AnItemOfAnotherBoard_IsNotFound()
    {
        var act = () => _handler.Handle(new PreviewFormulaQuery(Guid.CreateVersion7(), _item.Id, "1"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
