---
document_id: BE-AGENTS
document_type: agent-instructions
status: active
owner: backend-architecture
applies_to:
  - backend
  - backend-coding-agents
evidence:
  - ../AGENTS.md
  - ../RULE.md
  - ../CONTEXT-MAP.md
  - backend/backend.slnx
  - backend/docs/architecture/
  - backend/docs/operations/
  - backend/tests/AGENTS.md
review_on:
  - backend-execution-contract-change
  - backend-architecture-change
  - backend-project-topology-change
  - backend-test-topology-change
  - root-agent-contract-change
---

# Backend Agent Execution Contract

> **Start from owned product semantics and the complete use case. Never start from a controller, table, provider SDK, repository class, or convenient existing folder and then invent the product behavior around it.**
>
> A coding agent may choose ordinary implementation details only after semantic ownership, architecture boundaries, compatibility, security, data migration, and required proof are already determined by canonical authorities.

This file applies to any agent editing:

```text
backend/**
```

It extends root:

```text
../AGENTS.md
../RULE.md
```

It does not replace them.

---

# 1. Required reading order

Before editing backend production code:

```text
1. ../PRODUCT.md
2. ../RULE.md
3. ../AGENTS.md
4. ./AGENTS.md
5. owning product-context doc
6. relevant backend canonical architecture topic
7. related ADR only when rationale/decision matters
8. current source/tests/manifests as executable evidence
```

If editing tests, also read:

```text
tests/AGENTS.md
```

Do not assume there is a scoped `AGENTS.md` under each production project.

The current target intentionally has no:

```text
src/Notrelix.Domain/AGENTS.md
src/Notrelix.Application/AGENTS.md
src/Notrelix.Infrastructure/AGENTS.md
src/Notrelix.Platform/AGENTS.md
src/Notrelix.API/AGENTS.md
```

unless a future local instruction set is deliberately introduced.

---

# 2. Authority routing

Use the correct owner.

| Question | Canonical owner |
|---|---|
| Product meaning / lifecycle | `../docs/product/contexts/**` |
| System boundary / context relationship | `../docs/architecture/**` |
| Backend overall topology | `docs/architecture/backend-overview.md` |
| Domain behavior | `docs/architecture/domain-modeling.md` |
| Application orchestration/pipeline | `docs/architecture/application-model.md` |
| EF/PostgreSQL/RLS/providers/cache | `docs/architecture/infrastructure-and-data.md` |
| Messaging/delivery/runtime mechanisms | `docs/architecture/platform-and-messaging.md` |
| HTTP/OpenAPI/composition | `docs/architecture/api-and-contracts.md` |
| Security/tenant/authz | `docs/architecture/security-tenancy-authorization.md` |
| Backend tests/gates | `docs/architecture/testing-and-quality-gates.md` |
| Runtime config | `docs/operations/configuration-and-runtime.md` |
| Migrations/data change | `docs/operations/migrations-and-data-change.md` |
| Historical backend decision | `docs/decisions/**` |
| Repository delivery/migration/release | `../docs/delivery/**` |
| Repository quality/security/performance | `../docs/quality/**` |

Do not invent a local authority because the nearest file is convenient.

---

# 3. Source is evidence, not automatic precedent

Current source answers:

```text
what exists?
```

Canonical documents answer:

```text
what is intended?
```

When they disagree, classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not blindly propagate source debt.

---

# 4. Start from the use case

Before touching files, identify in order:

```text
1. bounded context
2. resource/aggregate/lifecycle owner
3. command/query/use-case intent
4. actor
5. Account/Workspace/resource scope
6. authorization policy/action
7. external/cross-aggregate facts
8. transaction/consistency boundary
9. persistence/RLS/migration impact
10. contract/event/realtime impact
11. idempotency/concurrency/retry/order
12. consumers and mixed versions
13. tests/gates
14. rollout/recovery if material
```

If any of 1–8 is materially unresolved, do not choose code structure yet.

---

# 5. Complete business transaction principle

A feature can cross all five projects.

Do not force arbitrary handoffs like:

```text
PR 1 Domain only
PR 2 Application only
PR 3 Infrastructure only
```

if the intermediate repository state is invalid or leaves semantics unproven.

Prefer:

```text
smallest complete, compatible, reviewable business change
```

unless migration/release compatibility explicitly requires stages.

---

# 6. Project topology

Current production projects are fixed by:

```text
backend.slnx
```

Do not create a new production project because:

- a new bounded context appears;
- a feature is large;
- a team wants independent ownership;
- folder count is growing.

A project-boundary change is consequential `C5` architecture work and normally requires architecture review/ADR.

---

# 7. Bounded context is not project boundary

Keep these independent:

```text
semantic context
implementation project/layer
deployment process
team
database
```

One product context can appear across:

```text
Domain
Application
Infrastructure
Platform
API
```

without becoming five separate owners.

---

# 8. Domain execution rules

When editing `src/Notrelix.Domain`:

## Do

- preserve pure business semantics;
- put invariant/state-transition logic with the owning model;
- use value objects/types when they encode real semantics;
- emit Domain events for committed local business facts when appropriate;
- pass external facts into Domain explicitly;
- make invalid transitions fail before partial mutation;
- define semantic no-op behavior intentionally;
- preserve concurrency/version semantics of the aggregate.

## Do not

- reference Infrastructure/API/Platform;
- inject `DbContext`;
- call provider SDK;
- fetch Redis;
- perform HTTP;
- read ambient current user;
- read environment configuration for business decisions;
- hide external facts behind static service locators;
- create `Common` abstractions merely to avoid context ownership.

Domain currently has no external package references; preserve purity unless an accepted architecture decision changes it.

---

# 9. Domain external-fact rule

If a Domain rule needs:

```text
actor
time
parent hierarchy
membership
quota
count
feature entitlement
provider fact
```

that the aggregate does not own, the caller supplies the fact.

Do not move DB/provider queries into Domain to make the method “self-contained”.

---

# 10. Aggregate consistency rule

Before updating several aggregates, answer:

```text
Which aggregate owns each invariant?
Must they commit atomically?
Can coordination be eventual?
Is compensation required?
Is one context being mutated from another?
```

Do not create a repository transaction across unrelated aggregates merely because it is technically possible.

---

# 11. Domain event rule

A Domain event describes an owned business fact.

It is **not automatically**:

```text
public integration event
realtime payload
audit record
notification
provider webhook
```

Map outward deliberately.

---

# 12. Application execution rules

When editing `src/Notrelix.Application`, Application owns the use case.

A command/query should make clear:

```text
intent
actor/scope
authorization
validation
external facts
load/query
Domain invocation
transaction
idempotency/concurrency
post-commit result
```

Do not turn a handler into a provider/persistence/business-rule monolith.

---

# 13. Application pipeline ownership

Before adding handler-local behavior, ask whether it belongs to the canonical pipeline.

Typical cross-cutting concerns include:

```text
validation
tenant/resource resolution
authorization
transaction
expected version
idempotency
post-commit orchestration
```

If the architecture says the pipeline owns it, do not reimplement it ad hoc in each handler.

---

# 14. Authorization rule

Protected work must be server-authorized before protected effects.

Do not trust:

```text
client role
client Workspace ID
hidden UI button
frontend entitlement state
route location
```

as permission.

Resolve the canonical resource/scope needed by the authorization policy.

---

# 15. Application EF exception

Current `Notrelix.Application.csproj` references:

```text
Microsoft.EntityFrameworkCore
```

under current approved exception:

```text
EX-BE-APP-EF-001
```

Interpretation:

```text
package currently exists
```

does **not** mean:

```text
new direct DbContext persistence is allowed
```

New handlers follow Application ports and Infrastructure implementations unless the governed architecture explicitly changes.

If the exception's required compatibility types can be removed, prefer eliminating the exception rather than extending it.

---

# 16. Direct persistence stop condition

Stop if the easiest implementation requires:

```csharp
_applicationDbContext.SomeSet...
```

inside a new Application handler and no canonical port/approved exception exists.

Do not copy neighboring direct EF usage as precedent without classifying it.

---

# 17. Transaction rule

The transaction should cover the complete local authoritative state change and any durable outbox/post-commit enrollment required for reliable delivery.

Do not:

```text
commit source state
→ then best-effort create required outbox record
```

if the architecture requires atomic enrollment.

Likewise, do not perform irreversible external provider work inside the DB transaction merely to “keep everything atomic”.

---

# 18. Concurrency rule

For versioned mutable resources, define stale-write behavior.

Do not:

- overwrite newer state silently;
- report conflict after partial mutation;
- retry a stale semantic mutation blindly.

Concurrency behavior is product/application semantics, not an EF exception detail.

---

# 19. Idempotency rule

For retryable create/update/external-effect operations, identify the logical operation identity.

Prove:

```text
same key + same request
same key + conflicting request
retry after timeout
retry after partial infrastructure failure
retention/expiration
```

as applicable.

Do not add random GUID generation inside a retry path and call it idempotent.

---

# 20. Infrastructure execution rules

When editing `src/Notrelix.Infrastructure`:

Infrastructure implements mechanisms for the inward contracts.

Keep:

```text
EF mapping
DB queries
provider SDK
Redis
storage
search
JWT/Identity mechanics
email/provider integration
RLS/migrations
```

outside Domain.

Do not move business lifecycle decisions into mapping/configuration classes simply because the database can enforce them.

---

# 21. Persistence mapping rule

Map the Domain/Application semantics to PostgreSQL deliberately.

Review:

```text
column type
nullability
constraint
index
tenant predicate
concurrency
JSON/converter version
delete/archive behavior
```

Database constraints strengthen invariants but do not replace the Domain/Application owner.

---

# 22. RLS rule

RLS is defense-in-depth for tenant isolation.

Do not treat it as a substitute for Application authorization.

When adding/changing tenant-owned tables:

```text
identify tenant path
define policy
verify session context
test allowed tenant
test denied foreign tenant
review index/selectivity
```

Do not generate RLS policy solely because a column happens to be named `workspace_id`.

---

# 23. Migration rule

Any persisted meaning change considers **existing real data**, not only a clean DB.

Use:

```text
expand
→ compatible code
→ backfill
→ cutover
→ verify
→ contract
```

when old/new readers or writers can coexist.

Do not suppress pending model changes.

Do not rewrite already-applied production migration history to hide a correction.

---

# 24. Legacy data rule

If existing rows contain an unexpected value:

```text
normalize by approved semantic mapping
quarantine/report
block migration
explicit sentinel
```

Do not guess a business value solely to satisfy a new constraint.

---

# 25. Cache rule

Cache is scoped acceleration/derived state.

Cache key and invalidation must preserve:

```text
Account/Workspace/resource
user/permission dimension where required
version/freshness
```

Do not make cache the only authority for:

```text
permission
membership
entitlement
resource visibility
```

---

# 26. Provider rule

For external providers, define:

```text
timeout
cancellation
retry class
rate limit
correlation/idempotency
unknown outcome
reconciliation
secret handling
```

Do not treat timeout as proof that an external write failed.

---

# 27. Platform execution rules

When editing `src/Notrelix.Platform`, remember:

```text
Platform owns reusable delivery mechanics
Product context owns the business event/effect meaning
```

Platform must remain reusable without importing context-specific policy into generic consumer/runtime infrastructure.

---

# 28. Message identity

Every durable message/consumer path needs stable logical identity appropriate to:

```text
dedup
poison detection
ordering
retry
replay
observability
```

Do not use a transient CLR object reference/process instance as identity.

---

# 29. Consumer success/ack rule

Advance:

```text
ack
ordering cursor
completion marker
```

only after the business handler/effect satisfies the approved success contract.

Do not mark ordered work complete before handler success.

---

# 30. Retry rule

Retry only failures that can become successful.

Use bounded retry/backoff.

Do not hot-loop:

```text
validation failure
contract/schema mismatch
revoked authorization
poison message
```

as if they were transient network failures.

---

# 31. Poison rule

Poison handling is scoped to:

```text
logical message
+
logical consumer
```

unless a broader invariant explicitly requires more.

Do not poison all consumers because one consumer cannot process a message.

---

# 32. Ordering rule

Order only as broadly as the business invariant requires.

Possible scopes:

```text
resource
aggregate
Workspace
provider connection
```

Global ordering is expensive and should not be introduced for convenience.

If one ordered message fails, later advancement must respect the owning invariant.

---

# 33. Background tenant rule

Background work is not tenantless.

A consumer/job must reconstruct or carry explicit tenant/resource scope before authorized/persisted effects.

Do not give a worker global data visibility and rely on message correctness alone.

---

# 34. API execution rules

When editing `src/Notrelix.API`:

API owns transport/composition.

Keep endpoint logic focused on:

```text
binding
authentication integration
request construction
Application invocation
HTTP/result mapping
OpenAPI
```

Do not move Domain rules or persistence logic into endpoints.

---

# 35. Endpoint resource rule

A protected endpoint must identify the canonical resource/action needed by Application authorization.

Do not authorize through:

```text
route-name string
UI role
controller folder
HTTP verb alone
```

---

# 36. Public error rule

Public errors should be stable semantic categories.

Do not leak:

```text
EF exception
PostgreSQL detail
stack trace
provider response secrets
internal class names
```

to clients.

---

# 37. OpenAPI rule

If public REST shape changes:

```text
change producer intentionally
review compatibility
regenerate/check artifacts
run API/OpenAPI drift proof
update consumers
```

Do not patch generated frontend contracts by hand.

---

# 38. API versioning rule

Version only when the contract requires it.

Do not create a new API version as an easy way to avoid designing additive compatibility.

Conversely, do not force breaking semantics into the same version merely to avoid a migration.

---

# 39. Cross-context execution rule

Before coordinating two contexts, write the ownership statement:

```text
Context A owns X.
Context B needs Y.
B obtains Y through <contract/event/query>.
B never mutates A's storage directly.
```

If this cannot be stated clearly, the architecture is unresolved.

---

# 40. Cross-context synchronous call

Use synchronous coordination when the caller genuinely needs the result to complete the current use case.

Keep the source owner authoritative.

Avoid hidden circular context dependencies.

---

# 41. Cross-context async fact

Use committed events when eventual propagation is appropriate.

Public/integration event identity is a contract.

Do not expose internal Domain CLR names directly by default.

---

# 42. Security stop conditions

Stop implementation if it requires:

```text
trusting client tenant scope as authority
bypassing Application authorization
disabling RLS to make query work
using stale permission cache as allow
logging secret/private payload
global worker privilege with no scope
temporary public endpoint with no auth design
```

A smaller secure unavailable behavior is preferable to a broad insecure shortcut.

---

# 43. Contract-first rule

For a cross-boundary contract, establish before independent implementation:

```text
producer semantic owner
consumer need
shape/version
authorization/tenant scope
error behavior
idempotency/concurrency where relevant
rollout order
old/new compatibility
```

Then implement/generate consumers.

---

# 44. Existing consumer inventory

Do not assume “same repository” means atomic deployment.

Inventory as applicable:

```text
web
mobile
old loaded browser bundle
background worker
message backlog
DLQ/replay
provider webhook
external integration
```

---

# 45. Mobile/backlog rule

Backend compatibility often outlives one deployment because:

```text
mobile clients lag
old browser bundles remain loaded
old queued messages remain
replay archives remain
```

Do not remove old contract merely because the new backend/frontend code merged together.

---

# 46. Testing rule

Start at the cheapest seam that proves the property.

Then run broader evidence required by the change class.

Do not default every invariant to Integration/E2E.

Do not mock away the property under test.

---

# 47. Test routing

Use:

```text
Domain.Tests
→ pure business behavior

Application.Tests
→ use case/pipeline/auth/result

Infrastructure.Tests
→ mapping/provider/persistence mechanism

Platform.Tests
→ delivery/reliability mechanics

API.Tests
→ transport/public contract

Integration.Tests
→ real production graph / PostgreSQL / RLS / cross-layer

Architecture.Tests
→ structural dependency/placement rules
```

Also obey:

```text
tests/AGENTS.md
```

---

# 48. Regression rule

For a bug:

```text
reproduce at the failed contract seam
→ fix
→ preserve regression
→ add higher-level proof only if it contributes a different property
```

Do not write a broad mock-heavy test that merely follows implementation calls.

---

# 49. PostgreSQL-realistic proof

SQLite/InMemory may help tests that do not depend on PostgreSQL semantics.

They cannot prove:

```text
RLS
PostgreSQL locking
Npgsql conversion
provider-specific SQL/index
real migration behavior
```

Use PostgreSQL/Testcontainers/integration evidence for those properties.

---

# 50. Required non-zero work

A required filtered test command that selects zero intended tests is a failure.

Do not treat:

```text
exit code 0
```

as sufficient if nothing relevant executed.

---

# 51. Architecture tests

When a canonical MUST is structurally automatable, prefer executable architecture gates.

Examples:

```text
project dependency
forbidden outer-layer reference
pipeline-owned authorization
context isolation
public API invocation boundary
```

Do not weaken/delete a valid architecture test to land a shortcut.

---

# 52. Documentation update rule

Update the canonical owner when a durable rule changes.

Do not write the same rule into:

```text
README
AGENTS
architecture doc
feature plan
PR
```

as five independent authorities.

README routes.

AGENTS constrains execution.

Architecture/Product own durable semantics.

---

# 53. Generated documentation

Do not hand-edit:

```text
docs/generated/**
```

or generated project/contract maps.

Change the producer and regenerate.

---

# 54. Legacy documentation rule

Do not use retired:

```text
docs/engineering/**
freeze plans
audit reports
roadmaps
old rule packs
backend/RULE.md
backend/PROMPT.md
```

as current authority after migration.

If unique knowledge exists there, migrate it to the correct canonical owner before deleting it.

---

# 55. Architecture decision trigger

A consequential durable choice may require:

```text
ADR-*
```

Examples:

```text
project/dependency boundary
new persistence technology
pipeline foundation
RLS foundation
security/CSRF/rate-limit architecture
major messaging mechanism
```

Routine implementation following current architecture does not need an ADR.

---

# 56. Exception rule

If an existing canonical rule remains correct but must be violated temporarily, use the governed exception process.

An exception must identify at least:

```text
exact rule
scope
reason
risk
compensating controls
owner
expiry/removal condition
validation
```

Do not create:

```text
TODO
PR note
comment
disabled test
```

and call it an exception.

---

# 57. Existing EF exception preservation

Until removed, preserve:

```text
EX-BE-APP-EF-001
```

as an exception, not precedent.

Do not expand its scope without explicit governance.

---

# 58. Change classification before coding

Classify material changes under:

```text
../docs/delivery/change-classification.md
```

The class determines required proof.

Examples:

```text
C1 additive API
C3 breaking contract
C4 schema/data
C5 architecture
C6 security/tenant
C7 runtime/config
C8 destructive/financial
```

Modifiers such as:

```text
MOBILE_LAG
ASYNC_BACKLOG
CROSS_TENANT
DATA_BACKFILL
ROLLBACK_UNSAFE
PROVIDER_EXTERNAL
```

are cumulative.

---

# 59. Architecture-change artifact

For a material structural change, use:

```text
../docs/templates/architecture-change-template.md
```

Do not create an architecture-change artifact for a routine feature merely because it is large.

---

# 60. Feature specification

If product behavior is not sufficiently defined by existing canonical product docs, use:

```text
../docs/templates/feature-spec-template.md
```

The agent must not invent:

```text
permission semantics
lifecycle
entitlement behavior
public share behavior
unknown provider outcome
```

to fill a feature gap.

---

# 61. Migration plan

For material durable transition, use:

```text
../docs/templates/migration-plan-template.md
```

The plan must define one semantic authority in every phase.

“Keep old and new in sync” without precedence is dual truth and is not acceptable.

---

# 62. Rollout rule

Every deployed stage must be valid independently.

Do not rely on impossible atomic simultaneous deployment of:

```text
DB
API
worker
web
mobile
provider
```

unless the environment truly guarantees it and evidence proves it.

---

# 63. Rollback rule

Never write only:

```text
rollback if it fails
```

Assess separately:

```text
binary
schema
data
messages/events
provider side effects
mobile
config/secrets
```

Forward recovery is first-class when old state cannot safely be restored.

---

# 64. Runtime/config rule

Runtime configuration is typed, fail-safe, and environment-specific without redefining business semantics.

Do not:

```text
if Production then enforce authorization
```

or bake production secrets into source/container/client bundle.

---

# 65. Container rule

If backend container changes:

```text
build context
toolchain
restore
final stage
runtime user
health
secret exclusion
startup
artifact identity
```

must remain correct.

A successful Docker build does not prove backend semantic correctness.

---

# 66. Performance rule

Before adding optimization:

```text
identify workload/cardinality
measure or explain bottleneck
preserve ownership/security
```

Do not introduce cache, denormalization, global batching, or eventual behavior solely because it “scales better” in theory.

---

# 67. Noisy-neighbor rule

For tenant-shared background/provider work, consider:

```text
per-tenant concurrency
backpressure
queue fairness
provider rate limit
DB capacity
```

Do not let one high-volume tenant consume all shared recovery/worker capacity if the mechanism supports fair bounds.

---

# 68. Recovery rule

For data/provider/message incidents, preserve:

```text
evidence
tenant scope
outbox
dedup
ordering
provider reality
```

Do not delete/clear state simply to make health green.

Recovery correctness is owned by repository Operations.

---

# 69. Definition of Done

Before claiming Done, verify all applicable:

```text
semantic owner
architecture owner
security/tenant
contract
existing-data migration
async reliability
tests
generated artifacts
docs/ADR/exception
rollout
recovery
exact CI revision
```

Use:

```text
../docs/delivery/definition-of-done.md
```

---

# 70. Completion report

When reporting backend implementation completion, include:

```text
Owning context:
Use case:
Aggregate/resource:
Authorization:
Tenant scope:
Persistence/migration:
Contracts:
Events/realtime:
Idempotency/concurrency:
Tests executed:
Broader gates:
Documentation/ADR:
Rollout/recovery:
Exact revision:
Remaining transition:
```

If a field is not applicable, say why.

Do not omit a material dimension silently.

---

# 71. Evidence honesty

Allowed:

```text
Verified:
- Domain tests ...
- Integration test ...

Not applicable:
- migration — no persisted shape changed

Pending:
- production rollout ...
```

Not allowed:

```text
All good.
Everything tested.
Production safe.
```

without matching evidence.

---

# 72. Stop conditions

Stop and resolve the owner/decision rather than inventing code if:

- bounded-context owner is unclear;
- aggregate/consistency owner is unclear;
- permission/resource semantics are undefined;
- cross-context mutation requires foreign persistence access;
- new handler appears to require direct EF under no approved boundary;
- public contract change has unknown consumers;
- schema change has no existing-data plan;
- dual write has no semantic authority;
- RLS/tenant scope is unresolved;
- provider write can time out with no reconciliation semantics;
- message ordering scope/success condition is unresolved;
- old mobile/backlog compatibility is unknown;
- architecture test must be weakened to make the design compile;
- required ADR is not accepted;
- a temporary exception has no removal condition;
- source/doc conflict has not been classified.

---

# 73. Review-before-code checklist

Before editing:

```text
[ ] owning product context read
[ ] relevant backend architecture doc read
[ ] use-case semantics known
[ ] resource/action authorization known
[ ] tenant scope known
[ ] transaction/consistency known
[ ] external facts identified
[ ] contract consumers inventoried
[ ] migration impact classified
[ ] async/retry/idempotency identified
[ ] test proof mapped
[ ] ADR/exception need resolved
```

---

# 74. Review-before-merge checklist

Before merge:

```text
[ ] no architecture shortcut
[ ] no foreign semantic ownership
[ ] auth/tenant negative proof
[ ] migration existing-data proof when required
[ ] contract/codegen synchronized
[ ] async failure/retry proof when required
[ ] focused tests executed
[ ] broader required gates executed
[ ] zero-test false green excluded
[ ] docs/ADR/exception updated
[ ] rollout/recovery valid
[ ] exact SHA evidence
```

---

# 75. Final backend-agent rule

A backend coding agent is authorized to implement only when it can state:

```text
This context owns the behavior.
This aggregate/resource owns consistency.
This actor may perform this action on this scope.
These external facts are supplied explicitly.
This transaction owns the local durable change.
These contracts/events expose the result.
These old/new consumers remain compatible.
This migration preserves existing data.
This retry/idempotency/ordering behavior is defined.
These tests/gates prove each protected property.
```

If any of those statements requires an architectural or product guess, stop normal implementation and route the unresolved decision to its canonical owner.
