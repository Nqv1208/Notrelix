# Domain Services, Policies, and Specifications

Most business rules should live on aggregates or value objects. Use domain services/policies/specifications only when the rule does not naturally belong to a single aggregate.

## Aggregate method first

Before creating a service, ask:

```txt
Can the aggregate that owns this state enforce the rule itself?
Can this be a value object validation?
Can Application load required facts and call an aggregate method?
```

If yes, do not create a domain service.

## Domain service rule

A domain service is allowed when:

```txt
The operation is pure domain logic.
The rule spans multiple aggregates in the same bounded context.
The service does not call infrastructure or Application services.
The service does not perform IO.
```

Allowed:

```csharp
public sealed class BoardItemMovePolicy
{
    public void EnsureCanMove(BoardItem item, Guid targetGroupBoardId)
    {
        if (item.BoardId != targetGroupBoardId)
            throw new BusinessRuleException("Target group belongs to another board.");
    }
}
```

Not allowed:

```csharp
public sealed class BoardItemMoveService
{
    private readonly IBoardRepository _repo; // forbidden in Domain
}
```

## Domain policy rule

Use a policy when a rule is named, reusable, and pure.

Examples:

```txt
WorkspaceOwnerRemovalPolicy
ShareLinkExpirationPolicy
FieldTypeCompatibilityPolicy
SubscriptionCancellationPolicy
```

Policies should receive facts as method parameters, not fetch them.

## Specification rule

Use specifications for pure predicates that are reused and have business names.

Example:

```csharp
public sealed class ActiveShareLinkSpecification
{
    public bool IsSatisfiedBy(ShareLink link, DateTimeOffset now)
        => link.Status == ShareLinkStatus.Active && !link.IsExpired(now);
}
```

Specifications must not query databases.

## Anti-patterns

Do not create:

```txt
DomainManager
DomainHelper
DomainUtil
Service classes that just wrap aggregate methods
Services that inject repositories/DbContext
Services that publish events/messages
Services that do authorization or permission checks using Application services
```

## Permission vs Domain rule

Permission evaluation is not a Domain aggregate invariant by default. It is an Application/Governance policy concern. Domain may model roles/permissions as entities, but it must not call `IPermissionService` or decide current-user authorization.

Example:

```txt
Domain may say: a public share link must expire.
Application/Security says: current user may create a share link for this board.
```

## Entitlement vs Domain rule

Billing entitlement checks are usually Application pipeline concerns. Domain may model plans, subscriptions, entitlements, invoices, and usage, but feature gating belongs in Application unless it is an invariant of the aggregate itself.

## Factory service rule

If creating an aggregate requires complex pure logic, use a named factory in Domain. If creating it requires database checks, external calls, current user, clock, or permissions, Application orchestrates and passes the facts into the factory.
