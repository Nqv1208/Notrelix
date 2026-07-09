# Scoping, Audit, Soft Delete, and Versioning

These rules keep Domain safe for a multi-tenant SaaS system.

## Scoping interfaces

Use `IAccountScoped` for entities owned by an account.

```csharp
public interface IAccountScoped
{
    Guid AccountId { get; }
}
```

Use `IWorkspaceScoped` for entities owned by a workspace.

```csharp
public interface IWorkspaceScoped
{
    Guid WorkspaceId { get; }
}
```

If an aggregate is workspace-scoped and also has account boundary, it should normally expose both:

```csharp
public Guid AccountId { get; private set; }
public Guid WorkspaceId { get; private set; }
```

## Scope validation

Factories must validate scope ids:

```csharp
Guard.NotEmpty(accountId);
Guard.NotEmpty(workspaceId);
```

Do not create tenant-scoped aggregates with `Guid.Empty` scope ids.

## ResourceRef rule

Use `ResourceRef` to represent polymorphic resource references across contexts.

Rules:

```txt
ResourceId cannot be empty.
ResourceType must be explicit.
WorkspaceId is optional but must be checked when present.
Use ResourceRef instead of loose string resource type + Guid pairs where possible.
```

When a `ResourceRef` contains `WorkspaceId`, use `EnsureSameWorkspace` before applying workspace-scoped operations.

## Audit rule

`AuditableEntity` provides:

```txt
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
SetAuditOnCreate
SetAuditOnUpdate
```

Rules:

```txt
Factories call SetAuditOnCreate.
Meaningful mutations call SetAuditOnUpdate.
Domain receives actor/time from Application.
Domain does not read current user or current time.
```

## Soft delete rule

`SoftDeletableEntity` provides:

```txt
DeletedAt
DeletedBy
DeleteReason
RestoredAt
RestoredBy
IsDeleted
SoftDelete
Restore
EnsureNotDeleted
```

Rules:

```txt
Mutation methods call EnsureNotDeleted.
Aggregates override SoftDelete/Restore when deletion/restoration is business-significant.
SoftDelete should update audit, increment version, and raise deleted event.
Restore should update audit, increment version, and raise restored event.
Protected/system entities must reject deletion in Domain.
```

Example:

```csharp
public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
{
    if (IsDeleted) return;
    if (IsSystem) throw new BusinessRuleException("Cannot delete a system field.");

    base.SoftDelete(deletedBy, deletedAt, reason);
    SetAuditOnUpdate(deletedBy, deletedAt);
    IncrementVersion();
    AddDomainEvent(new BoardFieldDeletedDomainEvent(...));
}
```

## Versioning rule

`AggregateRoot` has `Version`, initialized to `1`, and `IncrementVersion()`.

Rules:

```txt
New aggregate starts at Version = 1.
Every meaningful mutation increments version exactly once.
No-op mutation does not increment version.
SoftDelete and Restore increment version.
Version is used by Application/Infrastructure for optimistic concurrency.
Domain must not manually set Version from outside.
```

## Common mistake: missing version increment

Bad:

```csharp
public void UpdatePosition(FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
{
    EnsureNotDeleted();
    Position = position;
    SetAuditOnUpdate(updatedBy, updatedAt);
    // Missing IncrementVersion()
}
```

Good:

```csharp
public void UpdatePosition(FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
{
    EnsureNotDeleted();
    Guard.NotNull(position);
    if (Position.Value == position.Value) return;

    Position = position;
    SetAuditOnUpdate(updatedBy, updatedAt);
    IncrementVersion();
    AddDomainEvent(new BoardFieldPositionUpdatedDomainEvent(...)); // if the change matters externally
}
```

## Data sensitivity rule

If the domain object has sensitivity/classification concepts, state transitions must be explicit methods and must raise events if audit/projection/security needs to react.

Example:

```csharp
public void UpdateClassification(DataClassification classification, bool isSensitive, Guid updatedBy, DateTimeOffset updatedAt)
```

Do not let Application set sensitivity flags directly.
