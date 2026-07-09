# Bounded Contexts and Shared Kernel

Notrelix is a modular SaaS system. Domain must preserve bounded context ownership.

## Bounded contexts

Current major Domain contexts include:

```txt
Accounts
Identity
Workspaces
WorkManagement
Documents
Collaboration
Governance
Automation
Integrations
Billing
Analytics
```

Each context owns its language, aggregate lifecycles, events, and invariants.

## Context ownership rule

An aggregate belongs to exactly one bounded context. Do not place one aggregate under a context just because another context uses it.

Examples:

```txt
Board belongs to WorkManagement.
ShareLink belongs to Governance.
Subscription belongs to Billing.
OAuthAccount belongs to Identity.
Comment belongs to Collaboration.
Page belongs to Documents.
```

## Cross-context coupling rule

Domain contexts should not reference each other's internal aggregates directly. Use IDs or `ResourceRef`.

Good:

```csharp
public Guid BoardId { get; private set; }
public ResourceRef Resource { get; private set; }
```

Bad:

```csharp
public Board Board { get; private set; }
public Workspace Workspace { get; private set; }
```

Application coordinates cross-context use cases.

## SharedKernel admission rule

Only promote a concept to `SharedKernel` if:

```txt
It is used by multiple bounded contexts.
It has stable semantics across those contexts.
It is small and context-neutral.
It does not depend on a particular aggregate lifecycle.
```

Good SharedKernel candidates:

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
Color
JsonValue
```

Bad SharedKernel candidates:

```txt
Board permission algorithm
Subscription cancellation policy
Workspace onboarding workflow
Automation trigger state machine
Document block editing rule
```

## ResourceType rule

`ResourceType` is a global enum used for security, governance, audit, and polymorphic references. Adding a new resource type is an architecture-level change.

When adding a `ResourceType`, also update:

```txt
Application permission/resource scope resolver if needed
Infrastructure mapping/version reader if needed
Architecture/security matrix if needed
Domain tests for resources using it
```

## Context event rule

Domain events live in the source context that owns the fact.

Example:

```txt
BoardCreatedDomainEvent belongs to WorkManagement/Boards.
ShareLinkCreatedEvent belongs to Governance/ShareLinks.
SubscriptionChangedDomainEvent belongs to Billing/Subscriptions.
```

Mapping a domain event to an integration event belongs outside Domain.

## Context service rule

If a rule belongs to one aggregate, put it on that aggregate.

If a rule coordinates multiple aggregates inside one bounded context and has no infrastructure dependency, use a domain service or policy inside that bounded context.

If a rule coordinates multiple bounded contexts, Application orchestrates it.

## Naming rule

Use ubiquitous language from the context.

Examples:

```txt
Board, BoardField, BoardItem, BoardView in WorkManagement.
ShareLink, ResourcePermission, CustomRole in Governance.
Subscription, Entitlement, Invoice in Billing.
OAuthAccount, UserSession in Identity.
```

Do not use generic names like `Manager`, `Processor`, `Helper`, `Util` in Domain.
