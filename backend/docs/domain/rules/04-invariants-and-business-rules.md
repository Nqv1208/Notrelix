# Invariants and Business Rules

Domain is responsible for business invariants. If a rule must always be true regardless of API, UI, importer, background job, or integration source, it belongs in Domain.

## What is an invariant?

An invariant is a rule that must never be broken.

Examples:

```txt
A board title cannot be empty.
A system field cannot be deleted.
A public share link must have an expiration date.
A deleted aggregate cannot be modified.
A board field option name must be unique within the field.
A board item cannot move to a group from another board.
The last workspace owner cannot be removed.
```

## Domain guard pattern

Use `Guard` and domain exceptions in factories and mutation methods.

Good:

```csharp
Guard.NotEmpty(workspaceId);
Guard.NotNullOrWhiteSpace(title);
Guard.MaxLength(title, 255);
if (IsArchived) throw new BusinessRuleException("Cannot rename an archived board.");
```

Bad:

```csharp
// Validation only in Application validator
// Domain accepts invalid state if called from another path.
```

Application validation is for request shape and user-friendly errors. Domain validation is for business truth.

## Mutation method template

Use this template for meaningful mutations:

```csharp
public void ChangeSomething(..., Guid actorUserId, DateTimeOffset now)
{
    EnsureNotDeleted();

    // Validate input
    Guard.NotEmpty(actorUserId);
    Guard.NotNull(...);

    // Validate business rules
    if (SomeForbiddenState)
        throw new BusinessRuleException("...");

    // Normalize
    var normalized = ...;

    // No-op return
    if (CurrentValue == normalized)
        return;

    // State change
    var oldValue = CurrentValue;
    CurrentValue = normalized;

    // Audit/version/event
    SetAuditOnUpdate(actorUserId, now);
    IncrementVersion();
    AddDomainEvent(new SomethingChangedDomainEvent(...));
}
```

## No-op rule

If the requested mutation does not change state, return without version increment or event.

Example:

```csharp
if (Title == normalizedTitle) return;
```

Do not raise events for no-op changes.

## Normalization rule

Normalize inside Domain when normalization is part of business truth.

Examples:

```txt
Trim user-provided titles/names.
Normalize email to lowercase in Email value object.
Validate and normalize slug values.
```

Do not normalize purely presentation-specific data in Domain.

## Cross-aggregate rule

Domain must not query another aggregate. If a rule needs information from another aggregate, Application loads the needed facts and passes them into Domain as values.

Bad:

```csharp
public void MoveToGroup(Guid groupId)
{
    var group = _db.Groups.Find(groupId); // forbidden
}
```

Good:

```csharp
public void MoveToGroup(Guid groupId, Guid groupBoardId, Guid actorUserId, DateTimeOffset now)
{
    if (groupBoardId != BoardId)
        throw new BusinessRuleException("Group does not belong to the same board.");
    ...
}
```

## Application validator vs Domain invariant

Application validator checks request format:

```txt
Required fields
Max length for input contracts
Enum range
Basic date shape
```

Domain checks business truth:

```txt
Archived board cannot mutate.
System field cannot be deleted.
Public share link must expire.
Permission/resource identity cannot mismatch.
```

If the rule must hold for every caller, duplicate it in Domain even if Application already validates it.

## Error type rule

Use business/domain exceptions for domain rule failures:

```txt
BusinessRuleException
DomainException
WorkspaceMismatchException
```

Do not return Application `Result<T>` from Domain. Domain either returns values or throws domain exceptions.

## Actor and time rule

Every mutation that changes state should usually receive:

```csharp
Guid actorUserId
DateTimeOffset now
```

If mutation is system-driven, pass `Guid? actorUserId` only when the domain concept allows system actions. Do not read current user or current time from inside Domain.
