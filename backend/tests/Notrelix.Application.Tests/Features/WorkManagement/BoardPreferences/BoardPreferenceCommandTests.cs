using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceFilters;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceSorts;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceGroup;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Queries.GetBoardPreference;
using DomainException = Notrelix.Domain.Common.Exceptions.DomainException;

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
        public async Task Handle_NoExistingPreference_WithFilters_CreatesAndAppliesFilters()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();
            var filters = new List<FilterRule> { FilterRule.Create(fieldId, FilterOperator.Equals, "test") };

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Filters: filters);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_NoExistingPreference_WithSorts_CreatesAndAppliesSorts()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();
            var sorts = new List<SortRule> { SortRule.Create(fieldId, SortDirection.Ascending) };

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Sorts: sorts);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_NoExistingPreference_WithGroup_CreatesAndAppliesGroup()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();
            var fieldId = Guid.CreateVersion7();
            var group = GroupRule.Create(fieldId);

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Group: group);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExistingPreference_UpdatesFilters()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var filters = new List<FilterRule> { FilterRule.Create(fieldId, FilterOperator.Contains, "value") };

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Filters: filters);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExistingPreference_UpdatesSorts()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var sorts = new List<SortRule> { SortRule.Create(fieldId, SortDirection.Descending) };

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Sorts: sorts);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExistingPreference_UpdatesGroup()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var group = GroupRule.Create(fieldId);

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Group: group);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExistingPreference_ClearsGroup()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplyGroup(GroupRule.Create(Guid.CreateVersion7()), TestNow);
            SetupBoardViewUserPreferences(preference);

            var command = new CreateOrUpdateBoardPreferenceCommand(board.Id, viewId, Group: null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── UpdateBoardPreferenceFilters ───────────────────────────

    public class UpdateBoardPreferenceFiltersTests : BoardPreferenceCommandTests
    {
        private readonly UpdateBoardPreferenceFiltersCommandHandler _handler;

        public UpdateBoardPreferenceFiltersTests()
        {
            _handler = new UpdateBoardPreferenceFiltersCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesFilters()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var filters = new List<FilterRule> { FilterRule.Create(fieldId, FilterOperator.Equals, "test") };

            var command = new UpdateBoardPreferenceFiltersCommand(board.Id, viewId, filters);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyFilters_ClearsFilters()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplyFilter(
                [FilterRule.Create(Guid.CreateVersion7(), FilterOperator.Equals, "old")],
                TestNow);
            SetupBoardViewUserPreferences(preference);

            var command = new UpdateBoardPreferenceFiltersCommand(board.Id, viewId, []);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PreferenceNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateBoardPreferenceFiltersCommand(
                Guid.CreateVersion7(), Guid.CreateVersion7(), []);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DuplicateFieldFilters_ThrowsDomainException()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var filters = new List<FilterRule>
            {
                FilterRule.Create(fieldId, FilterOperator.Equals, "a"),
                FilterRule.Create(fieldId, FilterOperator.Equals, "b")
            };

            var command = new UpdateBoardPreferenceFiltersCommand(board.Id, viewId, filters);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateBoardPreferenceSorts ─────────────────────────────

    public class UpdateBoardPreferenceSortsTests : BoardPreferenceCommandTests
    {
        private readonly UpdateBoardPreferenceSortsCommandHandler _handler;

        public UpdateBoardPreferenceSortsTests()
        {
            _handler = new UpdateBoardPreferenceSortsCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesSorts()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var sorts = new List<SortRule> { SortRule.Create(fieldId, SortDirection.Ascending) };

            var command = new UpdateBoardPreferenceSortsCommand(board.Id, viewId, sorts);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptySorts_ClearsSorts()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplySort(
                [SortRule.Create(Guid.CreateVersion7(), SortDirection.Ascending)],
                TestNow);
            SetupBoardViewUserPreferences(preference);

            var command = new UpdateBoardPreferenceSortsCommand(board.Id, viewId, []);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PreferenceNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateBoardPreferenceSortsCommand(
                Guid.CreateVersion7(), Guid.CreateVersion7(), []);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DuplicateFieldSorts_ThrowsDomainException()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var sorts = new List<SortRule>
            {
                SortRule.Create(fieldId, SortDirection.Ascending),
                SortRule.Create(fieldId, SortDirection.Descending)
            };

            var command = new UpdateBoardPreferenceSortsCommand(board.Id, viewId, sorts);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateBoardPreferenceGroup ─────────────────────────────

    public class UpdateBoardPreferenceGroupTests : BoardPreferenceCommandTests
    {
        private readonly UpdateBoardPreferenceGroupCommandHandler _handler;

        public UpdateBoardPreferenceGroupTests()
        {
            _handler = new UpdateBoardPreferenceGroupCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesGroup()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            SetupBoardViewUserPreferences(preference);

            var fieldId = Guid.CreateVersion7();
            var group = GroupRule.Create(fieldId);

            var command = new UpdateBoardPreferenceGroupCommand(board.Id, viewId, group);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ClearGroup_Succeeds()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var viewId = Guid.CreateVersion7();

            var preference = BoardViewUserPreference.Create(
                TestAccountId, TestWorkspaceId, board.Id, viewId, TestUserId, TestNow);
            preference.ApplyGroup(GroupRule.Create(Guid.CreateVersion7()), TestNow);
            SetupBoardViewUserPreferences(preference);

            var command = new UpdateBoardPreferenceGroupCommand(board.Id, viewId, null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PreferenceNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateBoardPreferenceGroupCommand(
                Guid.CreateVersion7(), Guid.CreateVersion7(), null);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
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
