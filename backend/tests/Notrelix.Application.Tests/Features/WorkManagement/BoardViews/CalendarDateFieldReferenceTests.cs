using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;
using ViewMode = Notrelix.Domain.WorkManagement.ViewMode;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

/// <summary>
/// UC-WM-H01 and UC-WM-K01: a Calendar view places items by a Date field (product section 52). Every board has
/// the item's own start and due days (<c>span</c>, <c>due</c>); a field named instead must be an active Date
/// field of the same board, or the write is refused and nothing is saved.
/// </summary>
public class CalendarDateFieldReferenceTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateBoardViewCommandHandler _create;
    private readonly UpdateBoardViewConfigCommandHandler _update;
    private readonly SaveBoardViewCommandHandler _save;

    public CalendarDateFieldReferenceTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _create = new CreateBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
        _update = new UpdateBoardViewConfigCommandHandler(DbContextMock.Object, _currentUserMock.Object, DateTimeProviderMock.Object);
        _save = new SaveBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);
    }

    private BoardField Field(Guid boardId, string name, FieldType type) =>
        BoardField.Create(TestAccountId, TestWorkspaceId, boardId, name, type, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow);

    private static string Basis(object value) => $"{{\"calendarDateField\":\"{value}\"}}";

    private BoardView CalendarView(Guid boardId, string config) =>
        BoardView.Create(TestAccountId, TestWorkspaceId, boardId, "Lịch", ViewType.Calendar,
            BoardViewConfig.Create(JsonValue.Create(config)), TestUserId, TestNow);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"calendarDateField\":\"span\"}")]
    [InlineData("{\"calendarDateField\":\"due\"}")]
    public async Task Create_OnABoardWithNoDateField_UsesTheItemsOwnDates(string config)
    {
        var board = CreateBoard();
        SetupBoards(board);
        SetupBoardFields(Field(board.Id, "Ghi chú", FieldType.Text));

        var result = await _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", config), CancellationToken.None);

        result.Type.Should().Be("Calendar");
        result.Config.Should().Be(config);
    }

    [Fact]
    public async Task Create_WithADateField_IsSaved()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var handover = Field(board.Id, "Ngày bàn giao", FieldType.Date);
        SetupBoardFields(handover);

        var result = await _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", Basis(handover.Id)), CancellationToken.None);

        result.Config.Should().Be(Basis(handover.Id));
    }

    [Theory]
    [InlineData(FieldType.Text)]
    [InlineData(FieldType.LongText)]
    [InlineData(FieldType.Number)]
    [InlineData(FieldType.Checkbox)]
    public async Task Create_WithAFieldThatIsNotADate_IsRefusedAndAddsNoView(FieldType type)
    {
        var board = CreateBoard();
        SetupBoards(board);
        var notADate = Field(board.Id, "Hạn (chữ)", type);
        SetupBoardFields(notADate);

        var act = () => _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", Basis(notADate.Id)), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_View_PresentationConfigInvalid);
        DbContextMock.Object.BoardViews.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithADateFieldOfAnotherBoard_IsRefused()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var foreign = Field(Guid.CreateVersion7(), "Ngày khác", FieldType.Date);
        SetupBoardFields(foreign);

        var act = () => _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", Basis(foreign.Id)), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_View_PresentationConfigInvalid);
    }

    [Fact]
    public async Task Create_WithADeletedDateField_IsRefused()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var gone = Field(board.Id, "Ngày cũ", FieldType.Date);
        gone.Delete(TestUserId, TestNow);
        SetupBoardFields(gone);

        var act = () => _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", Basis(gone.Id)), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Create_WithTextThatIsNotAFieldId_IsRefused()
    {
        var board = CreateBoard();
        SetupBoards(board);
        SetupBoardFields();

        var act = () => _create.Handle(new CreateBoardViewCommand(board.Id, "Lịch", "Calendar", Basis("hạn chót")), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateConfig_ToATextField_IsRefusedAndKeepsTheView()
    {
        var board = CreateBoard();
        var view = CalendarView(board.Id, Basis("due"));
        SetupBoardViews(view);
        var text = Field(board.Id, "Ghi chú", FieldType.Text);
        SetupBoardFields(text);

        var act = () => _update.Handle(new UpdateBoardViewConfigCommand(board.Id, view.Id, Basis(text.Id), ExpectedVersion: 1), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        view.Config.Data.Value.Should().Be(Basis("due"));
        view.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateConfig_KeepingAFieldDeletedSinceItWasChosen_StillSavesOtherChanges()
    {
        var board = CreateBoard();
        var gone = Field(board.Id, "Ngày cũ", FieldType.Date);
        var view = CalendarView(board.Id, Basis(gone.Id));
        gone.Delete(TestUserId, TestNow);
        SetupBoardViews(view);
        SetupBoardFields(gone);
        var next = $"{{\"calendarDateField\":\"{gone.Id}\",\"filter\":{{\"conditions\":[]}}}}";

        var result = await _update.Handle(new UpdateBoardViewConfigCommand(board.Id, view.Id, next, ExpectedVersion: 1), CancellationToken.None);

        result.Config.Should().Be(next);
    }

    [Fact]
    public async Task SaveSharedView_NamingATextField_IsRefused()
    {
        var board = CreateBoard();
        SetupBoards(board);
        SetupBoardViews();
        var text = Field(board.Id, "Ghi chú", FieldType.Text);
        SetupBoardFields(text);

        var act = () => _save.Handle(new SaveBoardViewCommand(board.Id, ViewMode.Calendar, Basis(text.Id)), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        DbContextMock.Object.BoardViews.ToList().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSharedView_ToADateField_IsSaved()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var view = CalendarView(board.Id, "{}");
        SetupBoardViews(view);
        var handover = Field(board.Id, "Ngày bàn giao", FieldType.Date);
        SetupBoardFields(handover);

        var result = await _save.Handle(new SaveBoardViewCommand(board.Id, ViewMode.Calendar, Basis(handover.Id), ExpectedVersion: view.Version), CancellationToken.None);

        view.Config.Data.Value.Should().Be(Basis(handover.Id));
    }
}
