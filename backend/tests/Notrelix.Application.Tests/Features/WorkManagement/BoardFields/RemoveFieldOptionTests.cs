using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class RemoveFieldOptionTests : WorkManagementHandlerTestBase
{
    private readonly RemoveFieldOptionCommandHandler _handler;
    private readonly Board _board;
    private readonly BoardField _status;
    private readonly FieldOption _todo;
    private readonly FieldOption _doing;
    private readonly FieldOption _done;
    private readonly BoardItem _item;

    public RemoveFieldOptionTests()
    {
        _handler = new RemoveFieldOptionCommandHandler(DbContextMock.Object, CurrentUser(), DateTimeProviderMock.Object);
        _board = CreateBoard();
        _status = BoardField.Create(
            TestAccountId, TestWorkspaceId, _board.Id, "Status",
            FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        _status.AddOption("Todo", Color.Create("#111111"), FractionalIndex.Create("a0"), TestUserId, TestNow, WorkStatusCategory.Todo);
        _status.AddOption("Doing", Color.Create("#222222"), FractionalIndex.Create("a1"), TestUserId, TestNow, WorkStatusCategory.InProgress);
        _status.AddOption("Done", Color.Create("#333333"), FractionalIndex.Create("a2"), TestUserId, TestNow, WorkStatusCategory.Done);
        _todo = _status.Options.First(o => o.Name == "Todo");
        _doing = _status.Options.First(o => o.Name == "Doing");
        _done = _status.Options.First(o => o.Name == "Done");
        var group = BoardGroup.Create(TestAccountId, TestWorkspaceId, _board.Id, "Group", Color.Create("#444444"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _item = CreateBoardItem(boardId: _board.Id, groupId: group.Id);
        _item.UpdateFieldValue(_status, StatusValue(_doing), TestUserId, TestNow);

        SetupBoards(_board);
        SetupBoardGroups(group);
        SetupBoardFields(_status);
        SetupBoardItems(_item);
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(_item.FieldValues.ToList()));
    }

    private ICurrentUser CurrentUser()
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(c => c.UserId).Returns(TestUserId);
        return user.Object;
    }

    private static FieldValue StatusValue(FieldOption option) =>
        FieldValue.Create(JsonValue.Create($"\"{option.Id}\"")!);

    private RemoveFieldOptionCommand Remove(FieldOption option, FieldOption? replacement = null) =>
        new(_board.Id, _status.Id, option.Id, _status.Version, replacement?.Id);

    [Fact]
    public async Task Handle_StatusInUseWithoutReplacement_IsRefusedAndKeepsTheOption()
    {
        var result = await _handler.Handle(Remove(_doing), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Single().Code.Should().Be("work.field-option.replacement-required");
        _status.Options.Should().Contain(o => o.Id == _doing.Id);
    }

    [Fact]
    public async Task Handle_StatusInUse_MovesItemsToTheReplacement()
    {
        var result = await _handler.Handle(Remove(_doing, _todo), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _status.Options.Should().NotContain(o => o.Id == _doing.Id);
        _item.FieldValues.Single(v => v.FieldId == _status.Id).Value.Data.Value.Should().Contain(_todo.Id.ToString());
    }

    [Fact]
    public async Task Handle_ReplacementIsDone_CompletesTheItem()
    {
        await _handler.Handle(Remove(_doing, _done), CancellationToken.None);

        _item.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UnusedStatus_NeedsNoReplacement()
    {
        var review = _status.AddOption("Review", Color.Create("#555555"), FractionalIndex.Create("a3"), TestUserId, TestNow, WorkStatusCategory.Review);
        var result = await _handler.Handle(Remove(review), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _status.Options.Should().NotContain(o => o.Id == review.Id);
    }

    [Fact]
    public async Task Handle_ReplacementThatIsTheRemovedOption_IsInvalid()
    {
        var result = await _handler.Handle(Remove(_doing, _doing), CancellationToken.None);

        result.TypedErrors.Single().Code.Should().Be("work.field-option.replacement-invalid");
        _status.Options.Should().Contain(o => o.Id == _doing.Id);
    }

    [Fact]
    public async Task Handle_LastDoneOption_IsRefusedByTheField_AndItemsStay()
    {
        _item.UpdateFieldValue(_status, StatusValue(_done), TestUserId, TestNow);

        var act = () => _handler.Handle(Remove(_done, _todo), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        _item.FieldValues.Single(v => v.FieldId == _status.Id).Value.Data.Value.Should().Contain(_done.Id.ToString());
    }
}
