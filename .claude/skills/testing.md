---
skill: testing
description: Generate backend and frontend tests following Notrelix testing patterns
version: 1.0.0
---

# Testing Skill

Generate unit and integration tests for backend and frontend following established patterns.

## When to Use

- Adding tests for new features
- Need to follow established testing patterns
- Creating test data factories
- Setting up integration tests

## What This Skill Does

1. Generates backend unit tests (commands/queries)
2. Generates backend integration tests (API endpoints)
3. Generates frontend API contract tests
4. Creates test data factories
5. Follows existing test patterns and conventions

## Backend Testing

### Test Structure

```
backend/Notrelix.Tests/
├── Auth/                      # Auth handler tests
├── Workspaces/                # Workspace tests
├── Boards/                    # Board tests
├── Document/                  # Document tests
├── Data/                      # Test DB context factory
└── Api/                       # API integration tests
```

### Test Naming Conventions

- **Test Class:** `{FeatureName}Tests` or `{HandlerName}Tests`
- **Test Method:** `{MethodName}_{Scenario}_{ExpectedResult}`
- **Examples:**
  - `CreateCardCommandHandler_ValidInput_ReturnsCardDto`
  - `GetPageBlocksQueryHandler_PageNotFound_ThrowsNotFoundException`
  - `CardEndpoints_CreateCard_Returns201Created`

## Template: Command Handler Unit Test

```csharp
// File: backend/Notrelix.Tests/Boards/CreateCardCommandHandlerTests.cs
using FluentAssertions;
using Notrelix.Application.Features.Board.Commands;
using Notrelix.Application.Features.Board.DTOs;
using Notrelix.Domain.Entities.Board;
using Notrelix.Domain.Exceptions;
using Notrelix.Tests.Data;
using Xunit;

namespace Notrelix.Tests.Boards;

public class CreateCardCommandHandlerTests : IAsyncLifetime
{
    private readonly TestDbContextFactory _factory;
    private ApplicationDbContext _context = null!;
    private CreateCardCommandHandler _handler = null!;
    private ICurrentUser _currentUser = null!;

    public CreateCardCommandHandlerTests()
    {
        _factory = new TestDbContextFactory();
    }

    public async Task InitializeAsync()
    {
        _context = await _factory.CreateContextAsync();
        _currentUser = new TestCurrentUser(Guid.NewGuid());
        _handler = new CreateCardCommandHandler(_context, _currentUser);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ValidInput_ReturnsCardDto()
    {
        // Arrange
        var workspace = TestDataFactory.CreateWorkspace();
        var board = TestDataFactory.CreateBoard(workspace.Id);
        var list = TestDataFactory.CreateList(board.Id);
        
        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.Lists.Add(list);
        await _context.SaveChangesAsync();

        var command = new CreateCardCommand(
            ListId: list.Id,
            Title: "Test Card",
            Description: "Test Description"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Card");
        result.Description.Should().Be("Test Description");
        result.ListId.Should().Be(list.Id);
        result.Position.Should().BeGreaterThan(0);

        var cardInDb = await _context.Cards.FindAsync(result.Id);
        cardInDb.Should().NotBeNull();
        cardInDb!.CreatedBy.Should().Be(_currentUser.UserId);
    }

    [Fact]
    public async Task Handle_ListNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateCardCommand(
            ListId: Guid.NewGuid(),
            Title: "Test Card"
        );

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_NoPositionProvided_CalculatesPosition()
    {
        // Arrange
        var workspace = TestDataFactory.CreateWorkspace();
        var board = TestDataFactory.CreateBoard(workspace.Id);
        var list = TestDataFactory.CreateList(board.Id);
        var existingCard = TestDataFactory.CreateCard(list.Id, position: 5.0);
        
        _context.Workspaces.Add(workspace);
        _context.Boards.Add(board);
        _context.Lists.Add(list);
        _context.Cards.Add(existingCard);
        await _context.SaveChangesAsync();

        var command = new CreateCardCommand(
            ListId: list.Id,
            Title: "New Card"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Position.Should().BeGreaterThan(existingCard.Position);
    }
}
```

## Template: Query Handler Unit Test

```csharp
// File: backend/Notrelix.Tests/Document/GetPageBlocksQueryHandlerTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Document.Queries;
using Notrelix.Domain.Exceptions;
using Notrelix.Tests.Data;
using Xunit;

namespace Notrelix.Tests.Document;

public class GetPageBlocksQueryHandlerTests : IAsyncLifetime
{
    private readonly TestDbContextFactory _factory;
    private ApplicationDbContext _context = null!;
    private GetPageBlocksQueryHandler _handler = null!;

    public GetPageBlocksQueryHandlerTests()
    {
        _factory = new TestDbContextFactory();
    }

    public async Task InitializeAsync()
    {
        _context = await _factory.CreateContextAsync();
        _handler = new GetPageBlocksQueryHandler(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ValidPageId_ReturnsBlocksOrderedByPosition()
    {
        // Arrange
        var workspace = TestDataFactory.CreateWorkspace();
        var page = TestDataFactory.CreatePage(workspace.Id);
        var block1 = TestDataFactory.CreateBlock(page.Id, position: 2.0);
        var block2 = TestDataFactory.CreateBlock(page.Id, position: 1.0);
        var block3 = TestDataFactory.CreateBlock(page.Id, position: 3.0);
        
        _context.Workspaces.Add(workspace);
        _context.Pages.Add(page);
        _context.Blocks.AddRange(block1, block2, block3);
        await _context.SaveChangesAsync();

        var query = new GetPageBlocksQuery(page.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(b => b.Position);
        result.First().Id.Should().Be(block2.Id);
    }

    [Fact]
    public async Task Handle_PageNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetPageBlocksQuery(Guid.NewGuid());

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedBlocks_NotIncluded()
    {
        // Arrange
        var workspace = TestDataFactory.CreateWorkspace();
        var page = TestDataFactory.CreatePage(workspace.Id);
        var activeBlock = TestDataFactory.CreateBlock(page.Id);
        var deletedBlock = TestDataFactory.CreateBlock(page.Id, isDeleted: true);
        
        _context.Workspaces.Add(workspace);
        _context.Pages.Add(page);
        _context.Blocks.AddRange(activeBlock, deletedBlock);
        await _context.SaveChangesAsync();

        var query = new GetPageBlocksQuery(page.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(activeBlock.Id);
    }
}
```

## Template: API Integration Test

```csharp
// File: backend/Notrelix.Tests/Api/CardEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Notrelix.Application.Features.Board.Commands;
using Notrelix.Application.Features.Board.DTOs;
using Notrelix.Tests.Data;
using Xunit;

namespace Notrelix.Tests.Api;

public class CardEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CardEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCard_ValidInput_Returns201Created()
    {
        // Arrange
        var command = new CreateCardCommand(
            ListId: Guid.NewGuid(),
            Title: "Test Card",
            Description: "Test Description"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cards", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CardDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Test Card");
    }

    [Fact]
    public async Task CreateCard_InvalidInput_Returns400BadRequest()
    {
        // Arrange
        var command = new CreateCardCommand(
            ListId: Guid.NewGuid(),
            Title: "" // Invalid: empty title
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cards", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCard_ValidId_Returns200Ok()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/cards/{cardId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CardDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetCard_NotFound_Returns404NotFound()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/cards/{cardId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## Template: Test Data Factory

```csharp
// File: backend/Notrelix.Tests/Data/TestDataFactory.cs
using Notrelix.Domain.Entities.Board;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Entities.Workspace;

namespace Notrelix.Tests.Data;

public static class TestDataFactory
{
    public static Workspace CreateWorkspace(
        string? name = null,
        bool isPersonal = false)
    {
        return new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name ?? "Test Workspace",
            Slug = $"test-workspace-{Guid.NewGuid():N}",
            IsPersonal = isPersonal,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Board CreateBoard(
        Guid workspaceId,
        string? title = null,
        double position = 1.0)
    {
        return new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = title ?? "Test Board",
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List CreateList(
        Guid boardId,
        string? title = null,
        double position = 1.0)
    {
        return new List
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = title ?? "Test List",
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Card CreateCard(
        Guid listId,
        string? title = null,
        double position = 1.0,
        bool isDeleted = false)
    {
        return new Card
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            Title = title ?? "Test Card",
            Position = position,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Page CreatePage(
        Guid workspaceId,
        string? title = null,
        Guid? parentPageId = null)
    {
        return new Page
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ParentPageId = parentPageId,
            Title = title ?? "Test Page",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Block CreateBlock(
        Guid pageId,
        string? content = null,
        double position = 1.0,
        bool isDeleted = false)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            PageId = pageId,
            Type = "paragraph",
            Content = content ?? "Test content",
            Position = position,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

## Template: Test Current User

```csharp
// File: backend/Notrelix.Tests/Data/TestCurrentUser.cs
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.Tests.Data;

public class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid? userId = null)
    {
        UserId = userId;
    }

    public Guid? UserId { get; }
}
```

## Frontend Testing

### Test Structure

```
frontend/features/
└── {feature}/
    └── __tests__/
        ├── api-contracts.test.ts    # API contract tests
        └── hooks.test.ts            # Hook tests (future)
```

## Template: Frontend API Contract Test

```typescript
// File: frontend/features/boards/api/__tests__/boards-api.test.ts
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { boardsApi } from '../boards-api';
import { apiClient } from '@/lib/api/api-client';

vi.mock('@/lib/api/api-client');

describe('boardsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getBoards', () => {
    it('should fetch boards for a workspace', async () => {
      // Arrange
      const workspaceId = 'workspace-123';
      const mockBoards = [
        { id: 'board-1', title: 'Board 1', workspaceId },
        { id: 'board-2', title: 'Board 2', workspaceId },
      ];

      vi.mocked(apiClient.get).mockResolvedValue({
        data: { data: mockBoards },
      });

      // Act
      const result = await boardsApi.getBoards(workspaceId);

      // Assert
      expect(apiClient.get).toHaveBeenCalledWith(
        `/api/v1/workspaces/${workspaceId}/boards`
      );
      expect(result).toEqual(mockBoards);
    });

    it('should handle API errors', async () => {
      // Arrange
      const workspaceId = 'workspace-123';
      const error = new Error('Network error');

      vi.mocked(apiClient.get).mockRejectedValue(error);

      // Act & Assert
      await expect(boardsApi.getBoards(workspaceId)).rejects.toThrow('Network error');
    });
  });

  describe('createBoard', () => {
    it('should create a new board', async () => {
      // Arrange
      const workspaceId = 'workspace-123';
      const createDto = {
        title: 'New Board',
        description: 'Test board',
      };
      const mockBoard = {
        id: 'board-1',
        ...createDto,
        workspaceId,
      };

      vi.mocked(apiClient.post).mockResolvedValue({
        data: { data: mockBoard },
      });

      // Act
      const result = await boardsApi.createBoard(workspaceId, createDto);

      // Assert
      expect(apiClient.post).toHaveBeenCalledWith(
        `/api/v1/workspaces/${workspaceId}/boards`,
        createDto
      );
      expect(result).toEqual(mockBoard);
    });
  });
});
```

## Important Rules

### DO

- ✅ Use FluentAssertions for readable assertions
- ✅ Follow AAA pattern (Arrange, Act, Assert)
- ✅ Test happy path and error cases
- ✅ Use test data factories for consistent test data
- ✅ Clean up resources in DisposeAsync
- ✅ Use descriptive test names
- ✅ Test one thing per test method
- ✅ Mock external dependencies
- ✅ Test edge cases (null, empty, boundary values)

### DON'T

- ❌ Don't test framework code (EF Core, ASP.NET)
- ❌ Don't use real database in unit tests
- ❌ Don't test private methods directly
- ❌ Don't have multiple assertions for different concerns
- ❌ Don't use magic numbers (use constants)
- ❌ Don't skip cleanup (memory leaks in tests)
- ❌ Don't test implementation details

## Test Categories

### Unit Tests

Test individual components in isolation:
- Command/Query handlers
- Domain logic
- Validators
- Utilities

### Integration Tests

Test components working together:
- API endpoints
- Database operations
- External service integrations

### Contract Tests

Test API contracts match expectations:
- Request/response shapes
- Status codes
- Error handling

## Running Tests

### Backend

```bash
cd backend

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~CreateCardCommandHandlerTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend

```bash
cd frontend

# Run tests (when configured)
bun test

# Run with coverage
bun test --coverage

# Run in watch mode
bun test --watch
```

## Checklist

When creating tests, ensure:

- [ ] Test class name follows convention
- [ ] Test method names are descriptive
- [ ] AAA pattern followed (Arrange, Act, Assert)
- [ ] Happy path tested
- [ ] Error cases tested
- [ ] Edge cases tested
- [ ] Test data factories used
- [ ] Resources cleaned up properly
- [ ] No hardcoded values (use constants)
- [ ] FluentAssertions used for assertions
- [ ] Tests are independent (no shared state)
- [ ] Tests run quickly (< 1 second each)

## Examples

### Example 1: Command Handler Test

**User Request:** "Add tests for LinkPageToCardCommand"

**Generated File:** `backend/Notrelix.Tests/Boards/LinkPageToCardCommandHandlerTests.cs`

**Test Cases:**
- Valid input returns success
- Card not found throws NotFoundException
- Page not found throws NotFoundException
- Already linked page updates successfully

### Example 2: Query Handler Test

**User Request:** "Add tests for GetWorkspaceBoardsQuery"

**Generated File:** `backend/Notrelix.Tests/Boards/GetWorkspaceBoardsQueryHandlerTests.cs`

**Test Cases:**
- Returns boards ordered by position
- Filters deleted boards
- Returns empty list for workspace with no boards
- Workspace not found throws NotFoundException

## Related Skills

- `backend-cqrs` — Create commands/queries to test
- `frontend-feature` — Create API clients to test

## References

- [AGENTS.md](../../AGENTS.md) — Testing guidelines
- [xUnit Docs](https://xunit.net/)
- [FluentAssertions Docs](https://fluentassertions.com/)
- [Vitest Docs](https://vitest.dev/)
