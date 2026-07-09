# Domain Layer Rules

These rules are mandatory for all Domain code.

## 1. Domain is pure

Domain must not reference:

```txt
Notrelix.Application
Notrelix.Infrastructure
Notrelix.API
Microsoft.EntityFrameworkCore
MediatR
MassTransit
ASP.NET Core
Redis
Npgsql
HttpClient
System.Text.Json for infrastructure payload concerns
ILogger
IOptions
IServiceProvider
DateTimeProvider / clock abstractions
```

Allowed dependencies:

```txt
System.*
Notrelix.Domain.Common
Notrelix.Domain.SharedKernel
Other Domain bounded contexts only when explicitly accepted by the context ownership rule
```

The Domain project must stay independent. If a business rule needs data from another aggregate or context, Application orchestrates loading data and calls Domain methods. Domain must not query repositories or DbContext.

## 2. Aggregates protect invariants

Never mutate aggregate state from Application by setting properties directly.

Bad:

```csharp
board.Title = request.Title;
board.Visibility = request.Visibility;
```

Good:

```csharp
board.Rename(request.Title, actorUserId, now);
board.ChangeVisibility(request.Visibility, actorUserId, now);
```

Every meaningful mutation must go through a named method on the aggregate root or entity that owns the invariant.

## 3. Constructors are not business factories

Use private/protected parameterless constructors for ORM materialization. Use static factory methods for business creation.

Good:

```csharp
private Board() : base() { }

public static Board Create(
    Guid accountId,
    Guid workspaceId,
    Guid createdBy,
    string title,
    DateTimeOffset createdAt)
{
    Guard.NotEmpty(accountId);
    Guard.NotEmpty(workspaceId);
    Guard.NotEmpty(createdBy);
    Guard.NotNullOrWhiteSpace(title);

    var board = new Board
    {
        AccountId = accountId,
        WorkspaceId = workspaceId,
        Title = title.Trim()
    };

    board.SetAuditOnCreate(createdBy, createdAt);
    board.AddDomainEvent(new BoardCreatedDomainEvent(...));
    return board;
}
```

## 4. Public setters are forbidden for mutable business state

Business state should use `private set` or private backing fields.

Allowed:

```csharp
public string Title { get; private set; }
private readonly List<FieldOption> _options = new();
public IReadOnlyCollection<FieldOption> Options => _options.AsReadOnly();
```

Not allowed:

```csharp
public string Title { get; set; }
public List<FieldOption> Options { get; set; }
```

## 5. Domain does not perform IO

Domain must not:

```txt
send email
publish messages
write database
call external APIs
read files
write logs
use cache
call current user services
call authorization services
call tenant context services
```

Domain raises domain events. Infrastructure/Application handles side effects after commit.

## 6. Time and actor are passed in

Domain must not call `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, or current-user services. Application passes `actorUserId` and `now` into domain methods.

Bad:

```csharp
UpdatedAt = DateTimeOffset.UtcNow;
```

Good:

```csharp
public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
```

## 7. Domain exceptions are business exceptions

Use Domain exceptions for business rule violations:

```txt
BusinessRuleException
DomainException
WorkspaceMismatchException
```

Do not throw infrastructure exceptions from Domain.

## 8. Aggregate mutations must update version and audit

Every meaningful aggregate mutation must normally do all of these:

```txt
EnsureNotDeleted()
Validate inputs and invariants
Change state
SetAuditOnUpdate(actorUserId, now)
IncrementVersion()
Raise a domain event if the change matters to other parts of the system
```

No-op methods may return early without version increment or event if nothing changed.

## 9. Soft-delete must be explicit

If an aggregate can be deleted/restored, override `SoftDelete` and `Restore` to:

```txt
call base behavior
set audit/update timestamps
increment version
raise deleted/restored domain event
respect aggregate-specific restrictions
```

System or protected aggregates/entities must reject delete in Domain.

## 10. Domain events describe facts, not commands

Good event names:

```txt
BoardCreatedDomainEvent
BoardRenamedDomainEvent
ShareLinkDisabledEvent
BoardFieldRestoredDomainEvent
```

Bad event names:

```txt
CreateBoardEvent
RenameBoardCommandEvent
DoShareLinkDisableEvent
```

Events represent things that already happened.

## 11. SharedKernel must stay small

Only put concepts in `SharedKernel` if they are truly shared, stable, and context-neutral.

Allowed examples:

```txt
Email
Money
Slug
ResourceRef
ResourceType
FractionalIndex
DateRange
Url
SecretRef
```

Not allowed:

```txt
Board-specific policy
Billing subscription lifecycle rule
Workspace membership permission algorithm
Automation trigger execution rule
```

## 12. Domain tests are required for invariants

Every new aggregate or significant mutation must have Domain tests for:

```txt
valid creation
invalid creation
valid mutation
invalid mutation
version increment
audit update
relevant domain event
soft delete/restore if applicable
```
