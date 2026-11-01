using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Queries.GetBoardPreference;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardPreferences;

public class BoardPreferenceCommandTests : WorkManagementHandlerTestBase
{
    // ── CreateOrUpdateBoardPreference ──────────────────────────

    public class CreateOrUpdateBoardPreferenceTests : BoardPreferenceCommandTests
    {
        private readonly CreateOrUpdateBoardPreferenceCommandHandler _handler;

        public CreateOrUpdateBoardPreferenceTests()
        {
            _handler = new CreateOrUpdateBoardPreferenceCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_NoExistingPreference_CreatesNewPreference()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExistingPreference_AppliesCriteriaAndColumns()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Criteria: "{}", Columns: "[]");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── GetBoardPreference ─────────────────────────────────────

    public class GetBoardPreferenceTests : BoardPreferenceCommandTests
    {
        private readonly GetBoardPreferenceQueryHandler _handler;

        public GetBoardPreferenceTests()
        {
            _handler = new GetBoardPreferenceQueryHandler(
                DbContextMock.Object,
                RequestContextMock.Object);
        }

        [Fact]
        public async Task Handle_PreferenceExists_ReturnsDto()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var query = new GetBoardPreferenceQuery(board.Id, viewId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.BoardId.Should().Be(board.Id);
            result.Data.ViewId.Should().Be(viewId);
        }

        [Fact]
        public async Task Handle_PreferenceNotFound_ReturnsEmptyDto()
        {
            var boardId = Guid.CreateVersion7();
            var viewId = Guid.CreateVersion7();

            var query = new GetBoardPreferenceQuery(boardId, viewId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(Guid.Empty);
            result.Data.BoardId.Should().Be(boardId);
            result.Data.ViewId.Should().Be(viewId);
            result.Data.FilterRules.Should().BeEmpty();
            result.Data.SortRules.Should().BeEmpty();
            result.Data.GroupRule.Should().BeNull();
        }

        [Fact]
        public async Task Handle_PreferenceWithFilters_ReturnsFilters()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplyFilter(
                [FilterRule.Create(fieldId, FilterOperator.Equals, "test")],
                TestNow);
            SetupBoardViewUserPreferences(preference);

            var query = new GetBoardPreferenceQuery(board.Id, viewId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.FilterRules.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_PreferenceWithSorts_ReturnsSorts()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplySort(
                [SortRule.Create(fieldId, SortDirection.Ascending)],
                TestNow);
            SetupBoardViewUserPreferences(preference);

            var query = new GetBoardPreferenceQuery(board.Id, viewId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.SortRules.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_PreferenceWithGroup_ReturnsGroup()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplyGroup(GroupRule.Create(fieldId), TestNow);
            SetupBoardViewUserPreferences(preference);

            var query = new GetBoardPreferenceQuery(board.Id, viewId);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.GroupRule.Should().NotBeNull();
            result.Data.GroupRule.FieldId.Should().Be(fieldId);
        }
    }

}
