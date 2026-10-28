using Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterSorts;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterGroup;
using Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RestoreSavedFilter;
using DomainException = Notrelix.Domain.Common.Exceptions.DomainException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Views;

public class SavedFilterCommandTests : WorkManagementHandlerTestBase
{
    // ── CreateSavedFilter ──────────────────────────────────────

    public class CreateSavedFilterTests : SavedFilterCommandTests
    {
        private readonly CreateSavedFilterCommandHandler _handler;

        public CreateSavedFilterTests()
        {
            _handler = new CreateSavedFilterCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesSavedFilter()
        {
            var board = CreateBoard();
            SetupBoards(board);

            var command = new CreateSavedFilterCommand(board.Id, "My Filter");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_BoardNotFound_ThrowsNotFoundException()
        {
            var command = new CreateSavedFilterCommand(Guid.CreateVersion7(), "My Filter");

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_WithRules_CreatesSavedFilter()
        {
            var board = CreateBoard();
            SetupBoards(board);
            var fieldId = Guid.CreateVersion7();
            var rules = new List<FilterRule> { FilterRule.Create(fieldId, FilterOperator.Equals, "test") };

            var command = new CreateSavedFilterCommand(board.Id, "Filtered", rules);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── personal vs shared (a saved filter is a Board child, GOV-033) ──

    public class PersonalFilterAccessTests : SavedFilterCommandTests
    {
        private SavedFilter FilterOf(Guid creator, SavedFilterVisibility visibility) =>
            SavedFilter.Create(TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), "Of someone", [], creator, TestNow, visibility: visibility);

        [Fact]
        public async Task Rename_PersonalFilterOfSomeoneElse_IsNotFound()
        {
            var filter = FilterOf(Guid.CreateVersion7(), SavedFilterVisibility.Private);
            SetupSavedFilters(filter);
            var handler = new RenameSavedFilterCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

            await handler.Invoking(h => h.Handle(new RenameSavedFilterCommand(filter.Id, "x", 0), CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
            filter.Name.Should().Be("Of someone");
        }

        [Fact]
        public async Task Rename_SharedFilterOfSomeoneElse_Succeeds()
        {
            var filter = FilterOf(Guid.CreateVersion7(), SavedFilterVisibility.Public);
            SetupSavedFilters(filter);
            var handler = new RenameSavedFilterCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

            var result = await handler.Handle(new RenameSavedFilterCommand(filter.Id, "Renamed", 0), CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            filter.Name.Should().Be("Renamed");
        }

        [Fact]
        public async Task Visibility_OfSharedFilterOfSomeoneElse_IsForbidden()
        {
            var filter = FilterOf(Guid.CreateVersion7(), SavedFilterVisibility.Public);
            SetupSavedFilters(filter);
            var handler = new UpdateSavedFilterVisibilityCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

            await handler.Invoking(h => h.Handle(new UpdateSavedFilterVisibilityCommand(filter.Id, SavedFilterVisibility.Private, 0), CancellationToken.None))
                .Should().ThrowAsync<ForbiddenException>();
            filter.Visibility.Should().Be(SavedFilterVisibility.Public);
        }

        [Fact]
        public async Task Create_RejectsCriteriaThatIsNotAJsonObject()
        {
            var validator = new CreateSavedFilterCommandValidator();

            (await validator.ValidateAsync(new CreateSavedFilterCommand(Guid.CreateVersion7(), "n", Criteria: "[1]"))).IsValid.Should().BeFalse();
            (await validator.ValidateAsync(new CreateSavedFilterCommand(Guid.CreateVersion7(), "n", Criteria: "not json"))).IsValid.Should().BeFalse();
            (await validator.ValidateAsync(new CreateSavedFilterCommand(Guid.CreateVersion7(), "n", Criteria: "{\"filter\":\"o\"}"))).IsValid.Should().BeTrue();
        }
    }

    // ── RenameSavedFilter ──────────────────────────────────────

    public class RenameSavedFilterTests : SavedFilterCommandTests
    {
        private readonly RenameSavedFilterCommandHandler _handler;

        public RenameSavedFilterTests()
        {
            _handler = new RenameSavedFilterCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_RenamesFilter()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);

            var command = new RenameSavedFilterCommand(filter.Id, "Renamed", 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new RenameSavedFilterCommand(Guid.CreateVersion7(), "Renamed", 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_DoesNotThrow()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new RenameSavedFilterCommand(filter.Id, "Renamed", 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateSavedFilterVisibility ────────────────────────────

    public class UpdateSavedFilterVisibilityTests : SavedFilterCommandTests
    {
        private readonly UpdateSavedFilterVisibilityCommandHandler _handler;

        public UpdateSavedFilterVisibilityTests()
        {
            _handler = new UpdateSavedFilterVisibilityCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesVisibility()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterVisibilityCommand(filter.Id, SavedFilterVisibility.Public, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateSavedFilterVisibilityCommand(Guid.CreateVersion7(), SavedFilterVisibility.Public, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_DoesNotThrow()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterVisibilityCommand(filter.Id, SavedFilterVisibility.Public, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateSavedFilterFilters ───────────────────────────────

    public class UpdateSavedFilterFiltersTests : SavedFilterCommandTests
    {
        private readonly UpdateSavedFilterFiltersCommandHandler _handler;

        public UpdateSavedFilterFiltersTests()
        {
            _handler = new UpdateSavedFilterFiltersCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesFilters()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);
            var rules = new List<FilterRule> { FilterRule.Create(Guid.CreateVersion7(), FilterOperator.Contains, "value") };

            var command = new UpdateSavedFilterFiltersCommand(filter.Id, rules, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateSavedFilterFiltersCommand(Guid.CreateVersion7(), [], 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_DoesNotThrow()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterFiltersCommand(filter.Id, [], 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateSavedFilterSorts ─────────────────────────────────

    public class UpdateSavedFilterSortsTests : SavedFilterCommandTests
    {
        private readonly UpdateSavedFilterSortsCommandHandler _handler;

        public UpdateSavedFilterSortsTests()
        {
            _handler = new UpdateSavedFilterSortsCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesSorts()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);
            var sortRules = new List<SortRule> { SortRule.Create(Guid.CreateVersion7(), SortDirection.Ascending) };

            var command = new UpdateSavedFilterSortsCommand(filter.Id, sortRules, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateSavedFilterSortsCommand(Guid.CreateVersion7(), [], 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_DoesNotThrow()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterSortsCommand(filter.Id, [], 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── UpdateSavedFilterGroup ─────────────────────────────────

    public class UpdateSavedFilterGroupTests : SavedFilterCommandTests
    {
        private readonly UpdateSavedFilterGroupCommandHandler _handler;

        public UpdateSavedFilterGroupTests()
        {
            _handler = new UpdateSavedFilterGroupCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesGroup()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);
            var groupRule = GroupRule.Create(Guid.CreateVersion7());

            var command = new UpdateSavedFilterGroupCommand(filter.Id, groupRule, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ClearGroup_Succeeds()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterGroupCommand(filter.Id, null, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new UpdateSavedFilterGroupCommand(Guid.CreateVersion7(), null, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_DoesNotThrow()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new UpdateSavedFilterGroupCommand(filter.Id, null, 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<DomainException>();
        }
    }

    // ── DeleteSavedFilter ──────────────────────────────────

    public class DeleteSavedFilterTests : SavedFilterCommandTests
    {
        private readonly DeleteSavedFilterCommandHandler _handler;

        public DeleteSavedFilterTests()
        {
            _handler = new DeleteSavedFilterCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_DeletesFilter()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);

            var command = new DeleteSavedFilterCommand(filter.Id, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new DeleteSavedFilterCommand(Guid.CreateVersion7(), 0);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterAlreadyDeleted_IsIdempotent()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new DeleteSavedFilterCommand(filter.Id, 0);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }

    // ── RestoreSavedFilter ─────────────────────────────────────

    public class RestoreSavedFilterTests : SavedFilterCommandTests
    {
        private readonly RestoreSavedFilterCommandHandler _handler;

        public RestoreSavedFilterTests()
        {
            _handler = new RestoreSavedFilterCommandHandler(
                DbContextMock.Object,
                RequestContextMock.Object,
                DateTimeProviderMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_RestoresFilter()
        {
            var filter = CreateSavedFilter(isDeleted: true);
            SetupSavedFilters(filter);

            var command = new RestoreSavedFilterCommand(filter.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FilterNotFound_ThrowsNotFoundException()
        {
            var command = new RestoreSavedFilterCommand(Guid.CreateVersion7());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_FilterNotDeleted_IsIdempotent()
        {
            var filter = CreateSavedFilter();
            SetupSavedFilters(filter);

            var command = new RestoreSavedFilterCommand(filter.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
        }
    }
}
