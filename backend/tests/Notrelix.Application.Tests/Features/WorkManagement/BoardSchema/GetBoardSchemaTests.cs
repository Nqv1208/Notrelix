using Notrelix.Application.Features.WorkManagement.BoardSchema.Queries.GetBoardSchema;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardSchema;

public class GetBoardSchemaTests : WorkManagementHandlerTestBase
{
    private readonly GetBoardSchemaQueryHandler _handler;

    public GetBoardSchemaTests()
    {
        _handler = new GetBoardSchemaQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSchema()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var query = new GetBoardSchemaQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(board.Id);
        result.Title.Should().Be("Test Board");
        result.Fields.Should().BeEmpty();
        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var query = new GetBoardSchemaQuery(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BoardWithFields_ReturnsFields()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Status, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoardFields(field);

        var query = new GetBoardSchemaQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Fields.Should().HaveCount(1);
        result.Fields.First().Name.Should().Be("Status");
    }

    [Fact]
    public async Task Handle_BoardWithGroups_ReturnsGroups()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "To Do",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoardGroups(group);

        var query = new GetBoardSchemaQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Groups.Should().HaveCount(1);
        result.Groups.First().Title.Should().Be("To Do");
    }

    [Fact]
    public async Task Handle_BoardWithFieldsAndGroups_ReturnsBoth()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Priority",
            FieldType.Select, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), TestUserId, TestNow);
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "In Progress",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoardFields(field);
        SetupBoardGroups(group);

        var query = new GetBoardSchemaQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Fields.Should().HaveCount(1);
        result.Groups.Should().HaveCount(1);
    }
}
