using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

using Notrelix.Domain.SharedKernel.Ordering;
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

        var command = new UpdateBoardFieldCommand(field.BoardId, field.Id, null, "{\"newSetting\":\"value\"}", 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RenamingASystemField_IsRefusedWithTheStableCodeAndChangesNothing()
    {
        var field = BoardField.Create(TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), "Due date", FieldType.Date, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow, isSystem: true);
        SetupBoardFields(field);
        var before = field.Version;

        var command = new UpdateBoardFieldCommand(field.BoardId, field.Id, "Deadline", null, before);

        var act = () => _handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Message.Should().Contain("system field");
        field.Name.Should().Be("Due date");
        field.Version.Should().Be(before);
    }

    [Fact]
    public async Task Handle_FieldNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardFieldCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, 1);

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

        var command = new UpdateBoardFieldCommand(boardId2, field.Id, null, null, 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullSettings_NoChanges()
    {
        var field = CreateBoardField();
        SetupBoardFields(field);

        var command = new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DefaultValue_SetsItAndBumpsTheVersion()
    {
        var field = CreateBoardField(type: FieldType.Number);
        SetupBoardFields(field);
        var before = field.Version;

        var result = await _handler.Handle(
            new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, before, "8"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        field.DefaultValue!.Data.Value.Should().NotBeNull();
        field.Version.Should().BeGreaterThan(before);
    }

    [Fact]
    public async Task Handle_SameDefault_ChangesNothing()
    {
        var field = CreateBoardField(type: FieldType.Number);
        SetupBoardFields(field);
        await _handler.Handle(new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, field.Version, "8"), CancellationToken.None);
        var version = field.Version;

        await _handler.Handle(new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, version, "8"), CancellationToken.None);

        field.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_InvalidDefault_IsRefusedAndNothingChanges()
    {
        var field = CreateBoardField(type: FieldType.Number);
        SetupBoardFields(field);
        var before = field.Version;

        var act = () => _handler.Handle(
            new UpdateBoardFieldCommand(field.BoardId, field.Id, null, null, before, "\"abc\""), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        field.DefaultValue.Should().BeNull();
        field.Version.Should().Be(before);
    }

    private BoardField CreateBoardField(Guid? id = null, Guid? boardId = null, FieldType type = FieldType.Text)
    {
        var field = Notrelix.Domain.WorkManagement.Fields.BoardField.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Field",
            type,
            FieldSettings.Empty(),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            field.GetType().GetProperty(nameof(BoardField.Id))!.SetValue(field, id.Value);
        return field;
    }
}
