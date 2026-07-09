# Domain Code Review Checklist

Use this checklist for every Domain PR.

## Dependency boundary

- [ ] Domain does not reference Application, Infrastructure, API.
- [ ] Domain does not reference EF Core, MediatR, MassTransit, ASP.NET Core, Redis, HTTP, logging, options, DI.
- [ ] No repository, DbContext, cache, email, storage, message bus, or current-user service is used in Domain.

## Aggregate modeling

- [ ] New business object is placed in the correct bounded context.
- [ ] Aggregate root inherits `AggregateRoot` if it owns lifecycle/invariants.
- [ ] Entity/value object distinction is correct.
- [ ] Public setters are not exposed for business state.
- [ ] Mutable collections use private backing fields and read-only exposure.
- [ ] Factory method validates required state and sets audit.

## Invariants

- [ ] Business rules are enforced in Domain, not only in Application validators.
- [ ] Mutations call `EnsureNotDeleted()` when needed.
- [ ] Invalid transitions throw domain/business exceptions.
- [ ] No-op mutations return without version increment/event.
- [ ] Cross-aggregate rules receive required facts as values, not repositories.

## Audit/versioning

- [ ] Creation calls `SetAuditOnCreate`.
- [ ] Mutation calls `SetAuditOnUpdate`.
- [ ] Meaningful mutation calls `IncrementVersion` exactly once.
- [ ] SoftDelete/Restore increment version and raise events when business-significant.

## Domain events

- [ ] Events are named in past tense.
- [ ] Events are raised only after actual state change.
- [ ] Events contain account/workspace/actor metadata where required.
- [ ] Events do not contain infrastructure contracts or external payloads.
- [ ] Event version is explicit/stable.

## Scoping

- [ ] Workspace-scoped aggregates expose `WorkspaceId` and implement `IWorkspaceScoped`.
- [ ] Account-scoped aggregates expose `AccountId` and implement `IAccountScoped` when applicable.
- [ ] `ResourceRef` is used for polymorphic resource references where appropriate.
- [ ] Cross-context references use IDs/shared primitives, not navigation objects.

## SharedKernel

- [ ] New SharedKernel type is truly shared and stable.
- [ ] Context-specific logic did not leak into SharedKernel.
- [ ] Value objects are immutable and self-validating.

## Tests

- [ ] Domain tests cover create, invalid create, mutation, invalid mutation.
- [ ] Tests cover version increment and no-op behavior.
- [ ] Tests cover domain event emission and event metadata.
- [ ] Tests cover soft delete/restore when applicable.
- [ ] Value object tests cover equality and invalid values.

## Merge blockers

Block merge if any of these are true:

```txt
Domain calls Infrastructure/Application/API.
Application can set aggregate business state directly.
Aggregate mutation misses invariant guard.
Meaningful mutation misses version increment.
Tenant-scoped aggregate lacks account/workspace scope.
Domain event is raised from handler instead of aggregate.
Domain contains repository/DbContext/external service.
```
