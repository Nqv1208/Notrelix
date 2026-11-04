using Notrelix.Application.Features.WorkManagement.BoardFields.Queries.PreviewFormula;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class PreviewFormulaTests : WorkManagementHandlerTestBase
{
    private readonly PreviewFormulaQueryHandler _handler;
    private readonly BoardItem _item;
    private readonly Guid _boardId;
    private readonly BoardField _hours;
    private readonly BoardItem _parent;

    public PreviewFormulaTests()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _handler = new PreviewFormulaQueryHandler(DbContextMock.Object, currentUser.Object, DateTimeProviderMock.Object);

        var board = CreateBoard();
        _boardId = board.Id;
        _item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(_item);
        _hours = BoardField.Create(TestAccountId, TestWorkspaceId, board.Id, "Giờ", FieldType.Number, FieldSettings.Empty(), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(_hours);
        _parent = CreateBoardItem(boardId: board.Id);
        var childA = BoardItem.CreateSubItem(_parent, "A", FractionalIndex.Create("a0"), TestUserId, TestNow);
        var childB = BoardItem.CreateSubItem(_parent, "B", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupBoardItems(_item, _parent, childA, childB);
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue>
        {
            BoardItemValue.Create(_item.Id, _hours.Id, FieldValue.Create(JsonValue.Create("6")!)),
            BoardItemValue.Create(childA.Id, _hours.Id, FieldValue.Create(JsonValue.Create("3")!)),
            BoardItemValue.Create(childB.Id, _hours.Id, FieldValue.Create(JsonValue.Create("4")!)),
        }));
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

    [Fact]
    public async Task ADraftFormula_NamesTheFieldsItReadsAndTheirValues()
    {
        var result = await _handler.Handle(new PreviewFormulaQuery(_boardId, _item.Id, "[Giờ] * 2"), CancellationToken.None);

        var input = result.Inputs.Should().ContainSingle().Subject;
        input.Name.Should().Be("Giờ");
        input.Value.Value!.Value.GetDecimal().Should().Be(6);
    }

    [Fact]
    public async Task ADraftRollup_SumsTheSubItemsAndNamesTheirValues()
    {
        var result = await _handler.Handle(new PreviewFormulaQuery(_boardId, _parent.Id, null, "sum", _hours.Id), CancellationToken.None);

        result.State.Should().Be("ok");
        result.Value!.Value.GetDecimal().Should().Be(7);
        result.SubItemCount.Should().Be(2);
        result.Inputs.Select(i => i.Value.Value!.Value.GetDecimal()).Should().Equal(3, 4);
    }

    [Fact]
    public async Task ADraftCountRollup_CountsSubItemsAndNamesNoValue_AndAnItemWithoutThemShowsZero()
    {
        var counted = await _handler.Handle(new PreviewFormulaQuery(_boardId, _parent.Id, null, "count"), CancellationToken.None);
        var none = await _handler.Handle(new PreviewFormulaQuery(_boardId, _item.Id, null, "count"), CancellationToken.None);

        counted.Value!.Value.GetDecimal().Should().Be(2);
        counted.Inputs.Should().BeEmpty();
        none.SubItemCount.Should().Be(0);
        none.Value!.Value.GetDecimal().Should().Be(0);
    }

    [Fact]
    public async Task ADraftRollupThatNeedsAFieldButHasNone_IsAnInvalidDefinition()
    {
        var result = await _handler.Handle(new PreviewFormulaQuery(_boardId, _parent.Id, null, "sum"), CancellationToken.None);

        result.State.Should().Be("error");
        result.ErrorCode.Should().Be("invalid-definition");
    }
}
