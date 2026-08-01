# Domain Foundation

## Purpose

This document defines the stable architectural decisions governing the
Notrelix Domain layer. It replaces the previous Domain Freeze certification.

Stable does not mean immutable. It means broad foundation refactoring stops
and changes follow a controlled review procedure.

## Stable architecture decisions

- Domain has no production PackageReference.
- Domain has no production ProjectReference.
- Application uses public Domain contracts only.
- Domain internals are exposed only to `Notrelix.Domain.Tests`.
- Tenant-scoped state carries explicit tenant identity (`AccountId`, `WorkspaceId`).
- Aggregate root owns invariant mutation.
- Outer layers provide actor, time, and environment facts.
- Deletion is separated from business lifecycle where applicable.
- Domain Events are raised by Domain behavior after successful mutation.

## Aggregate behavior rules

Every production mutation follows:

1. Validate lifecycle (`EnsureNotDeleted`).
2. Validate actor and required IDs.
3. Validate business invariants.
4. Normalize input.
5. Detect semantic no-op.
6. Prepare audit timestamp/update.
7. Construct prospective children/value objects without attaching.
8. Mutate root/owned state.
9. Apply audit.
10. Increment Version exactly once.
11. Raise the approved event or follow the documented no-event contract.

A rejected mutation leaves all state unchanged (failure atomicity).
A semantic no-op does not change state, audit, Version, or events.

## Tenant identity and isolation

- Required tenant IDs are non-empty.
- Tenant IDs are immutable after creation.
- Nullable tenant IDs reject `Guid.Empty` when present.
- Cross-tenant references are rejected.
- Event base matches the business fact scope (Global / Account / Workspace).

## Actor/time/audit/version/event protocol

- Domain must not use ambient nondeterminism (`DateTime.Now`, `Random.Shared`,
  `Environment.*`, `CultureInfo.CurrentCulture`, `Thread.CurrentThread`).
- Application supplies time, actor, external facts, counts, and approved random input.
- Use ordinal or explicitly approved comparisons.
- Version increments exactly once per successful persistent mutation.

## Public Domain boundary

Domain contains: aggregate roots, owned entities, value objects, domain events,
domain exceptions/rule codes, pure domain rules, state transitions, and
tenant-scope contracts.

Domain does not contain: repositories, database queries, EF mapping, DTOs,
provider calls, search/index jobs, outbox persistence, runtime scheduling,
cache implementations.

## Friend assembly policy

Domain exposes internals to exactly one assembly:

```xml
<InternalsVisibleTo Include="Notrelix.Domain.Tests" />
```

No other assembly may be added without a separate architecture decision.

## Contract compatibility policy

Protected contracts:

- Domain Event logical names, versions, and payload shapes.
- Event-reachable enum numeric values.
- Rule-code string values.

These are verified by contract snapshot tests in `Domain.Tests/Contracts/`.

## Required CI gates

- `dotnet build backend.slnx -c Release`
- `dotnet test` for Domain.Tests, Architecture.Tests
- Contract snapshot comparison (never auto-update in CI)
- Vulnerability scan

## Domain contract change procedure

1. State the business reason.
2. Identify Application/Infrastructure/API callers.
3. Review persistence and migration impact.
4. Review Domain Event compatibility.
5. Review enum/rule-code compatibility.
6. Update bounded-context behavior tests.
7. Update contract snapshots only when real contracts change
   (`UPDATE_DOMAIN_CONTRACT_SNAPSHOTS=1` locally, never in CI).
8. Run Domain, Architecture, and backend solution gates.

No certification SHA update is required.

## What stable means

- Feature development may continue.
- Business bugs may be fixed.
- Public cross-boundary contracts change through review.
- CI protects behavior and actual contracts.
- Optional capabilities may evolve independently.

## What stable does not mean

- The Domain is frozen forever.
- No new aggregate can be added.
- No new event can be introduced.
- Every public method signature is immutable.

## Stop rule

Do not perform another global Domain governance refactor as a prerequisite
for feature development.

Reopen Domain foundation only for:

- **P0:** cross-tenant leakage, Domain-caused data corruption, security
  boundary breach, impossible core aggregate state, incorrect externally
  consumed event contract.
- **P1:** a core production use case cannot be implemented without changing
  an established invariant or cross-boundary contract.

## Related documents

- [Domain Capability Maturity](domain-capability-maturity.md)
- [Domain Lifecycle and Deletion](domain-lifecycle-and-deletion.md)
- [Deletion Policy Matrix](DELETION_POLICY_MATRIX.md)
- [Event Catalog](EVENT_CATALOG.md)
