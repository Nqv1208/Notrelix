# Domain Testing and Architecture Gates

Domain tests are the primary safety net for business rules. Architecture tests prevent Domain from leaking dependencies.

## Required Domain unit tests

Every aggregate must have tests for:

```txt
Create_ShouldSetRequiredState
Create_WithInvalidInput_ShouldThrow
Mutation_ShouldChangeState
Mutation_WithInvalidState_ShouldThrow
Mutation_NoOp_ShouldNotIncrementVersionOrRaiseEvent
Mutation_ShouldSetAudit
Mutation_ShouldIncrementVersion
Mutation_ShouldRaiseExpectedDomainEvent
SoftDelete_ShouldSetDeleteState
SoftDelete_ShouldRaiseEvent
Restore_ShouldClearDeleteState
Restore_ShouldRaiseEvent
```

## Core aggregate test priority

At minimum, keep strong tests for:

```txt
Workspace
WorkspaceMember
Board
BoardField
BoardItem
ShareLink
Subscription
Entitlement
OAuthAccount
Comment
Page
```

## Event tests

For every event-raising mutation, test:

```txt
exact event type
workspace/account scope metadata
aggregate id/resource id
actor user id
occurred at
old/new values where applicable
no event on no-op
```

## Version tests

For every meaningful mutation:

```txt
initial Version is 1
mutation increments Version by 1
no-op does not increment Version
SoftDelete increments Version
Restore increments Version
```

## Soft delete tests

For soft-deletable aggregates:

```txt
SoftDelete marks DeletedAt/DeletedBy
SoftDelete is idempotent if already deleted
Restore clears DeletedAt/DeletedBy/DeleteReason
Mutation after delete throws DomainException/BusinessRuleException
Protected/system entity rejects delete
```

## Value object tests

For every value object:

```txt
valid values create successfully
invalid values throw
normalization works
value equality works
ToString is stable if used in keys/logs
```

## Architecture gates

Architecture tests must enforce:

```txt
Domain project does not reference Application, Infrastructure, API.
Domain project does not reference EF Core, MediatR, MassTransit, ASP.NET Core.
Aggregate state is not publicly settable except allowed immutable/read-only properties.
Domain events are records/classes in Domain and do not reference integration bus contracts.
Value objects are immutable.
AggregateRoot mutations call IncrementVersion where required.
Workspace-scoped aggregates implement IWorkspaceScoped.
Account-scoped aggregates implement IAccountScoped.
```

## Suggested architecture tests

```txt
Domain_Should_Not_Reference_Outer_Layers
Domain_Should_Not_Reference_Infrastructure_Packages
AggregateRoot_Public_Setters_Should_Be_Private
Aggregate_Mutation_Methods_Should_Not_Call_DateTime_UtcNow
Domain_Should_Not_Contain_Repository_Or_DbContext_Types
DomainEvents_Should_Not_Depend_On_Messaging
WorkspaceScopedAggregates_Should_Have_WorkspaceId
AccountScopedAggregates_Should_Have_AccountId
```

## Test naming

Use behavior names:

```txt
Create_WithEmptyWorkspaceId_ShouldThrow
Rename_WhenArchived_ShouldThrow
Rename_WithSameTitle_ShouldNotRaiseEvent
SoftDelete_SystemField_ShouldThrow
AddOption_WithDuplicateName_ShouldThrow
```

Avoid vague names:

```txt
Test1
BoardTest
ShouldWork
```

## CI requirement

Domain tests and architecture tests must run on every PR. No feature PR that touches Domain should merge if Domain tests or architecture tests fail.
