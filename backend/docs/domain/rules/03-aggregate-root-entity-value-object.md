# Aggregates, Entities, and Value Objects

## Aggregate root

An aggregate root is the consistency boundary. It owns invariants and raises domain events.

Rules:

```txt
Aggregate roots inherit AggregateRoot.
Aggregate roots expose identity and state through private setters.
Aggregate roots use static factory methods for creation.
Aggregate roots expose behavior methods for mutation.
Application must not directly set aggregate state.
Only aggregate roots should usually be loaded and saved by Application.
```

Example pattern:

```csharp
public class Board : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Title { get; private set; } = null!;

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
        Guard.MaxLength(title, 255);

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

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        Guard.MaxLength(title, 255);

        var normalized = title.Trim();
        if (Title == normalized) return;

        var oldTitle = Title;
        Title = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new BoardRenamedDomainEvent(...));
    }
}
```

## Entity

An entity has identity and lives inside an aggregate boundary.

Rules:

```txt
Entities inherit Entity, AuditableEntity, or SoftDeletableEntity.
Entities must not be manipulated outside aggregate invariants if they are part of an aggregate.
Collections must be private fields exposed as IReadOnlyCollection.
```

Example:

```csharp
private readonly List<FieldOption> _options = new();
public IReadOnlyCollection<FieldOption> Options => _options.AsReadOnly();
```

Never expose mutable collections:

```csharp
public List<FieldOption> Options { get; set; } // forbidden
```

## Value object

A value object is immutable and compared by value.

Rules:

```txt
Value objects should be sealed.
Value objects should have private constructors and static Create methods.
Value objects validate themselves.
Value objects must not have identity.
Value objects must not contain mutable collections unless protected/canonicalized.
```

Example:

```csharp
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email() { }
    private Email(string value) { Value = value; }

    public static Email Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        value = value.Trim().ToLowerInvariant();
        Guard.Assert(IsValid(value), "Invalid email.");
        return new Email(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

## Choosing aggregate vs entity vs value object

Use aggregate root when:

```txt
It owns a lifecycle.
It has independent identity.
It protects consistency rules.
It is loaded/modified by use cases directly.
It raises meaningful domain events.
```

Use entity when:

```txt
It has identity but belongs to another aggregate lifecycle.
It should not be modified independently.
```

Use value object when:

```txt
Its identity is its values.
It is immutable.
It validates a concept such as email, slug, money, date range, resource ref.
```

## Aggregate size rule

Do not make one aggregate own the whole system.

Bad:

```txt
Workspace aggregate owns boards, items, comments, documents, billing, automations.
```

Good:

```txt
Workspace owns workspace lifecycle.
Board owns board metadata and sequence rules.
BoardField owns field definition rules.
BoardItem owns item lifecycle rules.
Comment owns comment lifecycle rules.
```

Application coordinates multiple aggregates through separate repositories/DbContext interfaces and transaction pipeline.
