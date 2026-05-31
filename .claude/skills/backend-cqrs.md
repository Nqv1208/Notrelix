---
skill: backend-cqrs
description: Scaffold CQRS command or query with handler, DTO, and API endpoint for Notrelix backend
version: 1.0.0
---

# Backend CQRS Scaffolding

Generate a complete CQRS command or query following Notrelix conventions.

## When to Use

- Adding a new backend operation (create, update, delete, query)
- Need to generate command/query with handler, DTO, and API endpoint
- Want to follow established CQRS patterns automatically

## What This Skill Does

1. Generates Command or Query record
2. Creates Handler with MediatR
3. Generates DTO (if needed)
4. Adds API endpoint to appropriate domain
5. Follows all naming conventions from AGENTS.md
6. Includes validation setup (FluentValidation)

## Prerequisites

Before using this skill, you should know:
- Which domain the operation belongs to (Identity, Workspace, Document, Board, Calendar, Shared)
- Whether it's a Command (write) or Query (read)
- Input parameters and return type

## Usage

When the user asks to add a backend operation, use this skill to:

1. **Identify the domain** — Which of the 7 domains does this belong to?
2. **Determine type** — Is this a Command (write) or Query (read)?
3. **Generate files** — Create command/query, handler, DTO, endpoint

## File Locations

### Commands

```
backend/Notrelix.Application/Features/{Domain}/Commands/
  {Verb}{Noun}Command.cs          # Command record
  {Verb}{Noun}CommandHandler.cs   # Handler implementation
  {Verb}{Noun}CommandValidator.cs # FluentValidation rules
```

### Queries

```
backend/Notrelix.Application/Features/{Domain}/Queries/
  Get{Noun}Query.cs                # Query record
  Get{Noun}QueryHandler.cs         # Handler implementation
```

### DTOs

```
backend/Notrelix.Application/Features/{Domain}/DTOs/
  {Noun}Dto.cs                     # Data transfer object
```

### API Endpoints

```
backend/Notrelix.API/Endpoints/{Domain}/
  {Domain}Endpoints.cs             # Minimal API endpoints
```

## Naming Conventions

### Commands

- **Name:** `{Verb}{Noun}Command`
- **Examples:** `CreateCardCommand`, `UpdatePageCommand`, `LinkPageToCardCommand`
- **Verbs:** Create, Update, Delete, Link, Unlink, Move, Archive, Publish, Assign

### Queries

- **Name:** `Get{Noun}Query` or `Get{Noun}{Suffix}Query`
- **Examples:** `GetPageBlocksQuery`, `GetBoardsQuery`, `GetUserWorkspacesQuery`

### Handlers

- **Name:** `{CommandOrQueryName}Handler`
- **Examples:** `CreateCardCommandHandler`, `GetPageBlocksQueryHandler`

### DTOs

- **Name:** `{Noun}Dto`
- **Examples:** `CardDto`, `PageDto`, `WorkspaceDto`

## Template: Command

```csharp
// File: backend/Notrelix.Application/Features/Board/Commands/CreateCardCommand.cs
using MediatR;
using Notrelix.Application.Features.Board.DTOs;

namespace Notrelix.Application.Features.Board.Commands;

public record CreateCardCommand(
    Guid ListId,
    string Title,
    string? Description = null,
    double? Position = null
) : IRequest<CardDto>;
```

## Template: Command Handler

```csharp
// File: backend/Notrelix.Application/Features/Board/Commands/CreateCardCommandHandler.cs
using MediatR;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Board.DTOs;
using Notrelix.Domain.Entities.Board;
using Notrelix.Domain.Exceptions;

namespace Notrelix.Application.Features.Board.Commands;

public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, CardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CardDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        // Validate list exists
        var list = await _context.Lists
            .FirstOrDefaultAsync(l => l.Id == request.ListId && !l.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"List with id '{request.ListId}' not found");

        // Calculate position if not provided
        var position = request.Position;
        if (position == null)
        {
            var lastCard = await _context.Cards
                .Where(c => c.ListId == request.ListId && !c.IsDeleted)
                .OrderByDescending(c => c.Position)
                .FirstOrDefaultAsync(cancellationToken);
            
            position = lastCard != null ? lastCard.Position + 1.0 : 1.0;
        }

        // Create card
        var card = new Card
        {
            ListId = request.ListId,
            Title = request.Title,
            Description = request.Description,
            Position = position.Value,
            CreatedBy = _currentUser.UserId
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return new CardDto(
            card.Id,
            card.Title,
            card.Description,
            card.Position,
            card.ListId,
            card.LinkedPageId,
            card.CreatedAt,
            card.UpdatedAt
        );
    }
}
```

## Template: Command Validator

```csharp
// File: backend/Notrelix.Application/Features/Board/Commands/CreateCardCommandValidator.cs
using FluentValidation;

namespace Notrelix.Application.Features.Board.Commands;

public class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty()
            .WithMessage("List ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(10000)
            .When(x => x.Description != null)
            .WithMessage("Description must not exceed 10000 characters");

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Position.HasValue)
            .WithMessage("Position must be non-negative");
    }
}
```

## Template: Query

```csharp
// File: backend/Notrelix.Application/Features/Document/Queries/GetPageBlocksQuery.cs
using MediatR;
using Notrelix.Application.Features.Document.DTOs;

namespace Notrelix.Application.Features.Document.Queries;

public record GetPageBlocksQuery(Guid PageId) : IRequest<IEnumerable<BlockDto>>;
```

## Template: Query Handler

```csharp
// File: backend/Notrelix.Application/Features/Document/Queries/GetPageBlocksQueryHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Document.DTOs;
using Notrelix.Domain.Exceptions;

namespace Notrelix.Application.Features.Document.Queries;

public class GetPageBlocksQueryHandler : IRequestHandler<GetPageBlocksQuery, IEnumerable<BlockDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPageBlocksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BlockDto>> Handle(GetPageBlocksQuery request, CancellationToken cancellationToken)
    {
        // Validate page exists
        var pageExists = await _context.Pages
            .AnyAsync(p => p.Id == request.PageId && !p.IsDeleted, cancellationToken);

        if (!pageExists)
            throw new NotFoundException($"Page with id '{request.PageId}' not found");

        // Get blocks ordered by position
        var blocks = await _context.Blocks
            .Where(b => b.PageId == request.PageId && !b.IsDeleted)
            .OrderBy(b => b.Position)
            .Select(b => new BlockDto(
                b.Id,
                b.PageId,
                b.Type,
                b.Content,
                b.Properties,
                b.Position,
                b.ParentBlockId,
                b.CreatedAt,
                b.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return blocks;
    }
}
```

## Template: API Endpoint

```csharp
// File: backend/Notrelix.API/Endpoints/Board/CardEndpoints.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Board.Commands;
using Notrelix.Application.Features.Board.Queries;

namespace Notrelix.API.Endpoints.Board;

public static class CardEndpoints
{
    public static void MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cards")
            .WithTags("Cards")
            .RequireAuthorization();

        // POST /api/v1/cards
        group.MapPost("/", async (
            [FromBody] CreateCardCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/v1/cards/{result.Id}", result.ToApiResponse());
        })
        .WithName("CreateCard")
        .Produces<CardDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/v1/cards/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCardQuery(id), cancellationToken);
            return Results.Ok(result.ToApiResponse());
        })
        .WithName("GetCard")
        .Produces<CardDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
```

## Important Rules

### DO

- ✅ Use `record` for commands and queries (immutable)
- ✅ Use `IRequest<TResponse>` from MediatR
- ✅ Include `CancellationToken` in all async methods
- ✅ Validate entity exists before operations
- ✅ Use `FirstOrDefaultAsync` + null check or throw `NotFoundException`
- ✅ Filter by `!IsDeleted` for soft-deleted entities
- ✅ Set `CreatedBy` from `ICurrentUser` for commands
- ✅ Use fractional indexing for position fields (double)
- ✅ Return DTOs, never domain entities

### DON'T

- ❌ Don't use classes for commands/queries (use records)
- ❌ Don't return domain entities from handlers (use DTOs)
- ❌ Don't forget to check `!IsDeleted` in queries
- ❌ Don't use `int` for position fields (use `double`)
- ❌ Don't call external services in handlers (use domain events)
- ❌ Don't forget validation for commands
- ❌ Don't expose internal IDs in error messages to unauthorized users

## Common Patterns

### Checking Permissions

```csharp
// Check workspace membership
var isMember = await _context.WorkspaceMembers
    .AnyAsync(wm => 
        wm.WorkspaceId == workspaceId && 
        wm.UserId == _currentUser.UserId && 
        !wm.IsDeleted, 
        cancellationToken);

if (!isMember)
    throw new ForbiddenException("You don't have access to this workspace");
```

### Calculating Position (Fractional Indexing)

```csharp
// Insert at end
var lastItem = await _context.Items
    .Where(i => i.ContainerId == containerId && !i.IsDeleted)
    .OrderByDescending(i => i.Position)
    .FirstOrDefaultAsync(cancellationToken);

var position = lastItem != null ? lastItem.Position + 1.0 : 1.0;

// Insert between items
var position = (beforeItem.Position + afterItem.Position) / 2.0;
```

### Eager Loading Related Entities

```csharp
var card = await _context.Cards
    .Include(c => c.Labels)
    .Include(c => c.Members)
    .Include(c => c.Checklists)
        .ThenInclude(cl => cl.Items)
    .FirstOrDefaultAsync(c => c.Id == cardId && !c.IsDeleted, cancellationToken);
```

## Checklist

When generating CQRS code, ensure:

- [ ] Command/Query uses `record` type
- [ ] Handler implements `IRequestHandler<TRequest, TResponse>`
- [ ] Validator created for commands (FluentValidation)
- [ ] Entity existence validated before operations
- [ ] Soft delete filter applied (`!IsDeleted`)
- [ ] Position calculated correctly for ordered entities
- [ ] DTOs used for return types (not domain entities)
- [ ] API endpoint added to appropriate domain endpoints file
- [ ] Endpoint uses proper HTTP verb (POST/GET/PUT/PATCH/DELETE)
- [ ] Authorization required (`.RequireAuthorization()`)
- [ ] Proper status codes returned (201 for create, 200 for get, etc.)

## Examples

### Example 1: Create Command

**User Request:** "Add a command to link a page to a card"

**Generated Files:**

1. `backend/Notrelix.Application/Features/Board/Commands/LinkPageToCardCommand.cs`
2. `backend/Notrelix.Application/Features/Board/Commands/LinkPageToCardCommandHandler.cs`
3. `backend/Notrelix.Application/Features/Board/Commands/LinkPageToCardCommandValidator.cs`
4. Update `backend/Notrelix.API/Endpoints/Board/CardEndpoints.cs`

### Example 2: Query

**User Request:** "Add a query to get all boards in a workspace"

**Generated Files:**

1. `backend/Notrelix.Application/Features/Board/Queries/GetWorkspaceBoardsQuery.cs`
2. `backend/Notrelix.Application/Features/Board/Queries/GetWorkspaceBoardsQueryHandler.cs`
3. Update `backend/Notrelix.API/Endpoints/Board/BoardEndpoints.cs`

## Related Skills

- `database-migration` — Create EF Core migration after adding entities
- `testing` — Generate tests for commands/queries

## References

- [AGENTS.md](../../AGENTS.md) — Section 3: Backend Rules
- [notrelix-backend-structure.md](../../notrelix-backend-structure.md) — Detailed architecture
