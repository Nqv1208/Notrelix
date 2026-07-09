# Domain Folder Structure and Boundaries

## Canonical Domain structure

Use bounded context folders at the root of `Notrelix.Domain`:

```txt
Notrelix.Domain/
  Common/
  SharedKernel/
  Accounts/
  Identity/
  Workspaces/
  WorkManagement/
  Documents/
  Collaboration/
  Governance/
  Automation/
  Integrations/
  Billing/
  Analytics/
```

Each bounded context should use subfolders by aggregate/module, not technical layer first.

Example:

```txt
WorkManagement/
  Boards/
    Board.cs
    BoardVisibility.cs
    Events/
      BoardCreatedDomainEvent.cs
      BoardRenamedDomainEvent.cs
  Fields/
    BoardField.cs
    FieldOption.cs
    Events/
  Items/
    BoardItem.cs
    Events/
```

## What belongs in Domain

Domain may contain:

```txt
Aggregate roots
Entities
Value objects
Enums
Domain events
Domain exceptions
Domain policies/specifications
Pure domain services
Shared kernel primitives
Tenant/account/workspace scoping marker interfaces
Guard helpers
```

## What does not belong in Domain

Domain must not contain:

```txt
DbContext
EF configurations
Migrations
Repository implementations
HTTP endpoints
DTOs for API contracts
MediatR commands/queries/handlers
Validators for request models
Authorization services
Permission evaluators
Cache services
Email services
Storage clients
Message bus clients
Background jobs
Options/configuration classes
Logging
```

## Common vs SharedKernel

### `Common/`

Use `Common` for base building blocks and framework-free domain primitives:

```txt
AggregateRoot
Entity
AuditableEntity
SoftDeletableEntity
DomainEvent
IDomainEvent
IDurableDomainEvent
ILocalDomainEvent
IWorkspaceScoped
IAccountScoped
Guard
Domain exceptions
```

### `SharedKernel/`

Use `SharedKernel` for shared business concepts that are used across multiple bounded contexts:

```txt
Email
Money
Slug
ResourceRef
ResourceType
FractionalIndex
Color
DateRange
Url
SecretRef
SyncHash
JsonValue
```

Do not put context-specific business rules in `SharedKernel`.

## Events folder rule

Domain events should live near the aggregate that raises them:

```txt
WorkManagement/Boards/Events/BoardCreatedDomainEvent.cs
Governance/ShareLinks/Events/ShareLinkCreatedEvent.cs
Billing/Subscriptions/Events/SubscriptionCancelledDomainEvent.cs
```

If an event is cross-context but still a domain event, keep it in the source bounded context. Integration event mapping belongs outside Domain.

## Namespace rule

Namespace must match folder ownership.

Good:

```csharp
namespace Notrelix.Domain.WorkManagement.Boards;
namespace Notrelix.Domain.WorkManagement.Boards.Events;
namespace Notrelix.Domain.SharedKernel;
```

Bad:

```csharp
namespace Notrelix.Domain.Common.WorkManagement;
namespace Notrelix.Application.WorkManagement;
```

## Cross-context references

A bounded context should not depend deeply on another context's internal aggregate. Prefer IDs or shared primitives.

Good:

```csharp
public Guid WorkspaceId { get; private set; }
public ResourceRef Resource { get; private set; }
```

Risky:

```csharp
public Workspace Workspace { get; private set; }
public Board Board { get; private set; }
```

Application loads and coordinates aggregates. Domain should not form large object graphs across bounded contexts.
