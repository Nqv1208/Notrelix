---
document_id: TEMPLATE-ARCHITECTURE-CHANGE
document_type: template
status: active
owner: documentation-governance
applies_to:
  - repository
  - system-architecture
  - backend-architecture
  - frontend-architecture
  - cross-boundary-changes
evidence:
  - docs/governance/decision-and-exception-policy.md
  - docs/governance/topic-authority-map.md
  - docs/decisions/README.md
  - docs/templates/adr-template.md
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
  - docs/delivery/release-and-rollout.md
  - docs/delivery/definition-of-done.md
review_on:
  - architecture-change-process-change
  - adr-policy-change
  - delivery-classification-change
  - migration-policy-change
  - documentation-template-change
---

# Architecture Change Template

> **Use this template when a change materially modifies an architectural boundary, ownership model, dependency rule, platform mechanism, contract strategy, persistence model, runtime topology, or other consequential structural property.**
>
> This artifact analyzes and executes a change against the current canonical architecture. It does not itself become permanent architecture authority, and it does not replace an ADR when an ADR is required.

This template consolidates durable planning knowledge that previously appeared in separate Backend vertical-slice and Frontend capability planning templates while preserving the distinction between:

```text
current canonical architecture
architecture decision
change execution
temporary exception
```

Use the canonical owner for each.

---

# 1. When to use this template

Use when the change is materially one or more of:

```text
C5 architecture boundary/dependency
C4 data ownership/persistence architecture
C6 security/tenant foundation
C7 runtime/infrastructure architecture
C3 breaking architectural contract
```

or when a normal feature causes a consequential durable architecture choice under `decision-and-exception-policy.md`.

Examples:

- change bounded-context ownership;
- introduce/remove a cross-context contract;
- change backend layer dependency;
- change frontend package/layer graph;
- introduce a durable platform mechanism;
- move semantic data authority;
- change event/realtime/versioning architecture;
- extract a capability into a service;
- change persistence/broker/cache/provider architecture;
- change authentication/session/authorization foundation.

---

# 2. When not to use it

Do not use this template merely because a change is large.

Do not use it for:

```text
routine feature implementation
private refactor
ordinary endpoint
normal UI composition
small bug fix
temporary exception
incident timeline
migration progress tracker
```

Use:

```text
feature-spec-template.md
```

for product behavior, and:

```text
decision-and-exception-policy.md
```

for temporary exceptions.

---

# 3. Architecture change versus ADR

This document answers:

```text
What architecture is changing?
What is the current evidence?
What will the repository need to change?
How will compatibility/migration/proof work?
```

An ADR answers:

```text
Why did we choose this consequential architecture over alternatives?
```

A material architecture change can therefore require both:

```text
ADR
+
architecture change artifact
```

The ADR remains historical decision evidence.

The canonical architecture docs become the new current authority after the change is accepted/completed.

---

# 4. Architecture change versus exception

An exception means:

```text
existing rule remains correct
but
temporary scoped violation is allowed
```

An architecture change means:

```text
the intended architecture itself changes
```

Never use this template to avoid the expiry/removal requirements of an exception.

---

# 5. Completion philosophy

Delete sections that are genuinely not applicable **only after stating why** where the omission could hide material risk.

Do not fill sections with speculative detail.

Mark unresolved material choices as:

```text
BLOCKING DECISION
```

and identify the canonical owner/ADR required to resolve them.

A coding agent MUST NOT invent those decisions.

---

# 6. Copy from here

```markdown
---
document_id: <CHANGE-ID-or-descriptive-id>
document_type: architecture-change
status: draft
owner: <logical-owner>
applies_to:
  - <scope>
evidence:
  - <current canonical architecture path>
  - <source/manifests/tests>
review_on:
  - implementation-complete
  - decision-changed
---

# <Architecture Change Title>

## 1. Executive summary

### Current state

<One concise description of the current architecture property.>

### Target state

<One concise description of the intended architecture property.>

### Why this change exists

<Concrete problem/constraint.>

### Primary change class(es)

- `<C3/C4/C5/C6/C7>`

### Risk modifiers

- `<MOBILE_LAG / ASYNC_BACKLOG / CROSS_TENANT / ...>`

### Required decision records

- `<None, or SYS-ADR/ADR/FE-ADR proposal/reference>`

### Completion statement

This change is complete when:

- <objective repository/production condition>
- <old-path removal condition>
- <exact evidence condition>

---

## 2. Canonical authorities

### Product / business owner

- Context: `<bounded context>`
- Canonical doc: `<path>`
- Owned semantics affected:
  - ...

### System architecture owner

- Canonical docs:
  - `<path>`

### Backend architecture owner

- Applicable: `<yes/no + reason>`
- Canonical docs:
  - `<path>`

### Frontend architecture owner

- Applicable: `<yes/no + reason>`
- Canonical docs:
  - `<path>`

### Quality / security / delivery owners

- `<path>`

### Source is evidence, not automatic precedent

Current source locations that conflict with canonical target:

- `<path>` — `<SOURCE_DEBT / TRANSITION / other classification>`

Do not copy accidental current coupling into the target design.

---

## 3. Problem and constraints

### Problem

<What is currently unsafe, ambiguous, coupled, unscalable, incompatible, or structurally wrong?>

### Observable impact

- ...
- ...

### Durable constraints

- semantic ownership:
- tenant/security:
- compatibility:
- deployment:
- data:
- mobile:
- background/backlog:
- provider:
- performance:
- operations:

### Non-goals

- ...
- ...

### Must not change

List architecture/product properties that remain deliberately unchanged.

- ...
- ...

---

## 4. Current architecture evidence

### Current dependency / data / control flow

```text
<ASCII flow>
```

### Current source locations

| Surface | Path | Current role | Evidence / debt |
|---|---|---|---|
| Domain | `...` | ... | ... |
| Application | `...` | ... | ... |
| Infrastructure | `...` | ... | ... |
| Platform | `...` | ... | ... |
| API | `...` | ... | ... |
| Frontend | `...` | ... | ... |
| Contracts | `...` | ... | ... |
| CI/tests | `...` | ... | ... |

### Current consumers

- backend:
- web:
- mobile:
- workers:
- events/replay:
- providers/external:
- generated consumers:

### Current persistence / durable identities

- tables/columns:
- persisted discriminators/status:
- public event names:
- cache/config keys:
- provider mappings:
- generated contract identities:

### Current failure modes

- ...

---

## 5. Target architecture

### Target ownership

State one authoritative owner for every changed semantic fact.

| Fact / capability | Current owner | Target owner | Authority during transition |
|---|---|---|---|
| ... | ... | ... | ... |

### Target dependency graph

```text
<ASCII dependency graph>
```

### Allowed dependencies

- ...

### Forbidden dependencies

- ...

### Target data flow

```text
<request/event/data flow>
```

### Target source of truth

- ...

### Derived state

- ...

### Cross-context boundary

- producer:
- consumer:
- command/query/event:
- consistency:
- authorization:
- failure semantics:

---

## 6. Architecture invariants

List the exact invariants the implementation MUST satisfy.

Use stable identifiers if the invariant belongs in a canonical rule document.

Examples:

```text
- Domain remains free of Infrastructure dependencies.
- Foreign context state is mutated only through the target owner contract.
- Realtime remains freshness, not source truth.
- Cache cannot become authorization authority.
```

### New durable invariants

- ...

### Existing invariants preserved

- ...

### Existing invariant changed

If an existing rule is actually changing:

- Rule ID:
- Current rule:
- Proposed replacement:
- Canonical owner:
- ADR required:
- Migration consequence:

Do not simply violate it in implementation.

---

## 7. ADR decision gate

### Is a new ADR required?

`<yes/no>`

### Reason

<Reference the decision threshold.>

### Scope

`<SYS-ADR / ADR / FE-ADR>`

### Decision that must be resolved

<One precise question.>

### Alternatives that must be considered

1. ...
2. ...
3. ...

### Blocking status

If the ADR is required but not accepted:

```text
BLOCKING DECISION
```

A coding agent must stop before normalizing the undecided architecture.

---

## 8. Backend impact

Delete only if truly not applicable.

### Domain

- aggregate/entity/value objects:
- invariants:
- external facts supplied:
- lifecycle:
- no-op:
- concurrency/version:
- Domain events:
- dependencies removed/added:
- exact mutation tests:

### Application

- commands/queries:
- request contracts/markers:
- validation:
- authorization/resource:
- handler orchestration:
- ports:
- transaction:
- idempotency:
- cache:
- realtime consequence:
- external context interaction:

### Infrastructure

- persistence/query:
- mapping:
- constraints/indexes:
- RLS:
- migrations:
- provider adapters:
- cache:
- search:
- storage:

### Platform

- outbox:
- integration event:
- consumer:
- ordering:
- dedup/idempotency:
- retry/backoff:
- poison/dead-letter:
- scheduler:
- tenant context:

### API

- routes/operations:
- request/response:
- errors:
- idempotency:
- concurrency:
- OpenAPI:
- compatibility:

### Backend architecture tests

- dependency rules:
- context isolation:
- security/authorization:
- persistence/RLS:
- Platform:
- API:
- integration:

---

## 9. Frontend impact

Delete only if truly not applicable.

### Semantic owner / hosts

- product owner:
- web:
- mobile:
- marketing:

### Generated contract

- REST types:
- realtime types:
- codegen source:
- compatibility blocker:

### Package placement

- existing package:
- new package required?:
- why existing packages are insufficient:
- layer:
- allowed dependencies:
- public exports:
- forbidden deep imports:

### Server state

- query owner:
- canonical query keys:
- mutation:
- optimistic behavior:
- rollback:
- invalidation:
- stale/error categories:
- Workspace/account transition:

### Realtime

- event:
- patch/invalidate:
- duplicate:
- out-of-order:
- reconnect/gap:
- permission revocation:
- scope switch:

### UX states

- loading:
- empty:
- success:
- validation:
- denied:
- read-only:
- conflict:
- offline/degraded:
- destructive confirmation:
- pending/unknown:

### Accessibility

- keyboard:
- focus:
- screen reader:
- drag alternative:
- mobile semantics:
- reduced motion/zoom:

### Host composition

- route:
- shell:
- web:
- mobile:
- marketing:
- native-safe dependencies:

### Frontend proof

- package tests:
- architecture/dependency:
- codegen:
- web:
- integration:
- mobile:
- UI/accessibility:
- E2E:

---

## 10. Contract impact

### Contract inventory

| Contract | Producer | Consumers | Current version/shape | Target | Compatibility |
|---|---|---|---|---|---|
| REST | ... | ... | ... | ... | ... |
| event | ... | ... | ... | ... | ... |
| realtime | ... | ... | ... | ... | ... |
| package export | ... | ... | ... | ... | ... |

### Mixed-version matrix

| Producer | Consumer | Must work? | Behavior |
|---|---|---:|---|
| old | old | yes | ... |
| new | old | ... | ... |
| old | new | ... | ... |
| new | new | yes | ... |

### Mobile lag

- supported old clients:
- compatibility window:
- removal floor:

### Async backlog

- old queued formats:
- replay/DLQ:
- removal condition:

### Generated artifacts

- producer:
- generator:
- generated paths:
- drift gates:

---

## 11. Data / persistence impact

### Current schema/data

- ...

### Target schema/data

- ...

### Existing production data

- cardinality:
- legacy values:
- invalid rows:
- tenant distribution:

### Migration class

- expand:
- compatible reader/writer:
- backfill:
- switch:
- verify:
- contract:

### Authority by phase

| Phase | Read authority | Write authority | Compatibility representation |
|---|---|---|---|
| expand | ... | ... | ... |
| backfill | ... | ... | ... |
| cutover | ... | ... | ... |
| cleanup | ... | ... | ... |

### RLS / tenant

- ...

### Index / constraint

- ...

### Backfill

- stable traversal key:
- batch:
- idempotency:
- checkpoint:
- concurrency:
- failure handling:
- completion proof:

### Destructive contraction

- old readers stopped:
- old writers stopped:
- backlog handled:
- evidence permitting removal:

---

## 12. Security and tenancy

### Trust boundaries changed

- ...

### Authentication

- ...

### Authorization

- resource/action:
- server enforcement:
- cache/realtime invalidation:

### Tenant isolation

- API/Application:
- DB/RLS:
- cache:
- messages:
- background:
- realtime:
- analytics/search:

### Secrets / provider scope

- ...

### Negative/adversarial proof

- wrong tenant:
- insufficient permission:
- revoked authority:
- malformed/replay:
- fail-closed configuration:

---

## 13. Reliability and async behavior

### Transaction boundary

- ...

### Commit-before-side-effect rule

- ...

### Logical operation identity

- ...

### Idempotency

- ...

### Ordering

- ...

### Retry/backoff

- ...

### Poison/dead-letter

- ...

### Unknown external outcome

- ...

### Recovery/reconciliation

- ...

---

## 14. Performance and scalability

### Expected cardinality

- ...

### Hot query/render path

- ...

### Index/projection strategy

- ...

### Fan-out

- ...

### Cache

- reason:
- scope:
- invalidation:
- failure fallback:

### Backpressure / noisy-neighbor

- ...

### Mobile/bundle/render impact

- ...

### Required evidence

- query plan:
- benchmark:
- load:
- bundle/profile:
- not applicable reason:

Do not invent universal latency numbers.

---

## 15. Operations impact

### Observability

- semantic identifiers:
- logs:
- metrics:
- backlog/freshness:
- release/cohort:
- provider:
- migration:

### Degradation

- dependency:
- safe reduced mode:
- unsafe shortcut:
- recovery criterion:

### Incident/runbook

- new failure mode:
- containment:
- recovery:
- verification:

### Data recovery

- backup/restore impact:
- replay:
- provider/object reconciliation:

---

## 16. Infrastructure impact

### Environment/config

- new keys:
- secrets:
- build/startup/runtime binding:
- compatibility:

### Runtime processes

- new/changed process:
- identity/privilege:
- network:
- scaling:

### Dependency

- authority class:
- reason:
- failure model:
- recovery:

### Containers/build

- Dockerfile:
- build context:
- base image:
- runtime user:
- health:
- packaging smoke:

---

## 17. Change map

List exact implementation surfaces once the target design is resolved.

| Path / artifact | Change | Owner | Why |
|---|---|---|---|
| `...` | add/modify/delete/generate | ... | ... |

### Explicitly unchanged

List high-risk neighboring artifacts that readers might otherwise assume should change.

| Path / surface | Why no change |
|---|---|
| `...` | ... |

This prevents scope drift and helps agents avoid inventing structural work.

---

## 18. Proof matrix

| Property | Primary proof | Higher-level proof | Required gate |
|---|---|---|---|
| Domain invariant | ... | ... | ... |
| architecture dependency | ... | ... | ... |
| authorization | ... | ... | ... |
| RLS | ... | ... | ... |
| contract | ... | ... | ... |
| async reliability | ... | ... | ... |
| frontend state | ... | ... | ... |
| accessibility | ... | ... | ... |
| performance | ... | ... | ... |
| migration | ... | ... | ... |
| runtime | ... | ... | ... |

### Non-zero execution requirements

- ...

### Bug/regression reproduction

- ...

### Exact CI completion

- workflow/checks:
- exact SHA requirement:

---

## 19. Delivery stages

### Stage 0 — prerequisites

- ...

### Stage 1 — compatible expansion

- ...

### Stage 2 — implementation/consumer migration

- ...

### Stage 3 — data/backfill/cutover

- ...

### Stage 4 — rollout/observation

- ...

### Stage 5 — contraction/cleanup

- ...

Delete empty stages if genuinely not required.

Every retained stage must be valid independently in production.

---

## 20. Rollback / forward recovery

### Binary rollback

- safe?:
- reason:

### Schema rollback

- safe?:
- reason:

### Data rollback

- safe?:
- reason:

### Messages/events

- reversible?:
- reconciliation:

### External provider effects

- reversible/compensatable/unknown:

### Forward-recovery path

- ...

Do not write only “rollback if problems occur”.

---

## 21. Documentation updates

### Current canonical architecture

- ...

### Product/context

- ...

### Backend/frontend architecture

- ...

### ADR

- ...

### Generated documentation

- ...

### Temporary artifacts to retire after migration

- ...

---

## 22. Resolved implementation decisions

List decisions that the implementing agent MUST NOT reopen.

1. ...
2. ...
3. ...

---

## 23. Blocking unresolved decisions

If none:

```text
None.
```

Otherwise:

| Decision | Owner | Why blocking | Resolution artifact |
|---|---|---|---|
| ... | ... | ... | ADR/product decision/etc. |

The implementing agent MUST NOT choose these on its own.

---

## 24. Stop conditions

Stop implementation if any occurs:

- semantic owner is unresolved;
- target architecture conflicts with canonical docs without approved decision;
- required ADR is not accepted;
- cross-context write requires direct foreign persistence access;
- breaking contract has no mixed-version plan;
- data migration has no target authority/backfill proof;
- security/tenant boundary is unresolved;
- old mobile/backlog/provider consumers are unknown;
- generated contract source is ambiguous;
- rollout assumes impossible atomic deployment;
- requested exception is being disguised as permanent architecture;
- source paths are being chosen before target ownership is resolved.

---

## 25. Completion criteria

The architecture change is complete only when:

```text
[ ] required ADR accepted
[ ] canonical architecture updated
[ ] target dependency/ownership implemented
[ ] source debt/transition classified
[ ] contracts/generated consumers synchronized
[ ] migration/backfill/cutover completed as required
[ ] security/tenant negative proof
[ ] architecture/integration tests
[ ] performance/operations obligations
[ ] rollout/recovery evidence
[ ] old path removed when criteria satisfied
[ ] temporary transition docs retired/re-homed
[ ] exact required CI green
```

---

## 26. Evidence report

### Verified

- ...

### Not applicable

- `<item>` — `<reason>`

### Pending rollout phase

- ...

### Exact revision

`<SHA>`

### Remaining transition

- `<None, or explicit owned transition>`
```

---

# 7. Backend-specific guidance

The legacy Backend vertical-slice template correctly required explicit treatment of:

```text
Domain
Application
Infrastructure
API
Async consequences
Proof matrix
Files to change
```

Those sections are retained above, but only when the architecture change affects them.

Do not create Backend changes simply to populate every layer.

---

# 8. Frontend-specific guidance

The legacy Frontend capability template correctly required explicit treatment of:

```text
semantic owner / hosts
generated contracts
package placement/public API
server state
realtime
UX states
accessibility
proof
```

Those concerns are retained above.

Do not create new packages because the template has a package section. Explain why an existing package cannot own the capability before adding one.

---

# 9. Architecture-exception boundary

If the current architecture rule remains correct but cannot be followed temporarily, this template is the wrong artifact.

Use the governed exception process with:

```text
exact rule
exact scope
reason
risk
compensating controls
owner
expiry/removal condition
validation
```

Do not modify the target architecture merely to legitimize temporary debt.

---

# 10. Source-debt handling

A good architecture change explicitly distinguishes:

```text
CURRENT SOURCE
CANONICAL TARGET
MIGRATION TRANSITION
```

Example:

```text
Current:
Frontend feature deep-imports internal package path.

Canonical target:
Feature consumes public package export.

Transition:
Add public export → migrate consumer → architecture gate → remove deep import.
```

Do not describe the deep import as intended architecture because it exists.

---

# 11. New package/project/service admission

Creating a structural unit is consequential.

Require:

```text
semantic/mechanism owner
public responsibility
allowed dependencies
consumers
why current unit cannot own it
test/gate
operational impact
future extraction value
```

Avoid symmetry-driven decomposition.

---

# 12. Architecture-change quality test

A complete artifact lets a coding agent answer without invention:

```text
What is wrong today?
Which owner decides the target?
What exact architecture must exist?
What must not exist?
Which source/artifacts change?
Which consumers coexist?
How does existing data migrate?
Which tests prove each property?
How is rollout/recovery safe?
Which old paths are removed?
Which decisions are already resolved?
Which unresolved choices are blockers?
```

If those questions still require architecture invention, the artifact is not ready for execution.
