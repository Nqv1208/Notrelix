---
document_id: ADR-001
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-application
  - application-pipeline
evidence:
  - backend/docs/architecture/application-model.md
  - backend/src/Notrelix.Application/DependencyInjection.cs
  - backend/src/Notrelix.Application/Common/Behaviors/
  - backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs
review_on:
  - decision-superseded
  - application-pipeline-zone-model-change
  - transaction-boundary-change
  - post-commit-boundary-change
---

# ADR-001: Pipeline Boundary Zones

## ID

`ADR-001`

## Status

Accepted

## Date

`2026-08-11`

Historical note:

```text
The original ADR did not contain an explicit Date section.
This date is recovered from the Git history entry that introduced/preserved
this ADR in the current documentation refoundation commit.
```

## Owners

Current stewardship:

- `backend-architecture`

Historical authorship/owner:

```text
Not recorded explicitly in the original ADR.
```

This normalization does not infer historical authorship from current ownership.

---

## Context

The original decision records that the MediatR pipeline in `Notrelix.Application` had evolved into a long ordered sequence of cross-cutting behaviors.

The key architectural problem was not simply the number of behaviors.

It was that several behaviors depend on state established by earlier boundaries:

```text
tenant bootstrap
resource scope
post-commit scope
DB/RLS scope
transaction
authorization
concurrency
feature/subscription gates
idempotency
post-commit work
authorized response cache
```

Without an explicit boundary model, reordering could change correctness.

The original ADR gives concrete examples:

```text
RLS applied before tenant context exists
post-commit side effects executing inside a failed transaction
cache behavior running before the authoritative transaction is safely complete
```

The accepted decision therefore formalized the pipeline as zones with explicit entry/exit semantics instead of treating registration order as a cosmetic list.

### Current evidence

The current Application composition still registers nineteen pipeline behaviors in an explicit outermost-to-innermost order.

The current source places:

```text
ExceptionMappingBehavior
ApplicationTracingBehavior
ValidationBehavior
RequestContractGuardBehavior
TokenValidationBehavior
TenantBootstrapBehavior
SystemOperationAuditBehavior
ResourceScopeBehavior
```

before the post-commit scope boundary.

It then registers:

```text
PostCommitScopeBehavior
PublicCacheBehavior
DbRequestScopeBehavior
AuthorizationBehavior
VerifiedEmailBehavior
ConcurrencyBehavior
SubscriptionGateBehavior
FeatureGateBehavior
IdempotencyBehavior
PostCommitEnqueueBehavior
AuthorizedCacheBehavior
```

in the current accepted order.

The exact concrete class list is **current evidence**.

The architectural decision is the boundary/order model.

---

## Decision

Notrelix Application uses an explicit pipeline-zone model.

The original ADR describes **six conceptual zones/boundaries**.

For clarity, the accepted model can be read as:

```text
OUTER / PRE-DB
        ↓
POST-COMMIT SCOPE BOUNDARY
        ↓
PUBLIC-CACHE PRE-DB POSITION
        ↓
DB / RLS / TRANSACTION BOUNDARY
        ↓
INNER TRANSACTIONAL ZONE
        ↓
POST-COMMIT ENQUEUE
        ↓
AUTHORIZED CACHE / FINAL RESPONSE CACHE POSITION
```

The original diagram groups these into the accepted zone model and identifies `PostCommitScopeBehavior` and `DbRequestScopeBehavior` as critical inflection points.

### 1. Outer zone

The outer zone runs before the transactional DB/RLS boundary.

The original ADR records responsibilities such as:

```text
exception mapping
tracing
validation
request-contract guarding
token validation
tenant bootstrap
system-operation audit setup
resource scope resolution
```

A behavior in this zone MUST NOT depend on state produced only by an inner transactional behavior.

### 2. Post-commit scope boundary

`PostCommitScopeBehavior` creates the scope that can collect deferred post-commit actions.

The boundary exists before the DB transaction so the handler/inner pipeline can enroll post-commit intent.

Creation of the scope does **not** mean provider/delivery side effects execute before commit.

### 3. Public-cache position

The original decision places public/shared cache read behavior before the DB request scope.

This is valid only for requests whose public-cache contract is safe.

Workspace/private authorized data is not made public cache data by this placement.

### 4. DB / RLS / transaction boundary

`DbRequestScopeBehavior` is the accepted boundary that owns entry into the database request scope.

Current architecture assigns this boundary responsibility for:

```text
opening/reusing the request DB connection as required
applying full RLS request context
starting the local transaction when required
saving/committing local work
```

Detailed mechanics are current architecture/source concerns.

The historical decision is that the DB/RLS/transaction boundary is centralized rather than scattered across handlers/behaviors.

### 5. Inner transactional zone

After the DB request scope is established, current inner behaviors can depend on the transaction/RLS/resource context.

The original decision places concerns such as:

```text
authorization
verified-email gate
concurrency
subscription gate
feature gate
idempotency
handler/business execution
```

inside this boundary.

### 6. Post-commit and authorized-cache positions

After successful transaction completion, post-commit work is enrolled/executed according to the Application/Platform contract.

Authorized response cache behavior runs only after the transaction has safely completed according to the accepted order.

The key rule is:

```text
protected durable side effect/cache success
must not be treated as committed
before the authoritative local transaction has succeeded.
```

---

## Decision invariants

The historical decision implies the following durable constraints.

### No backward dependency

A behavior in an earlier/outer zone MUST NOT depend on state created only by a later/inner zone.

### One DB request boundary

DB/RLS/transaction ownership remains centralized at the accepted boundary rather than distributed across arbitrary handlers.

### Authorization after required scope exists

Authorization runs only after the resource/DB context needed by the authorization contract is available.

### Post-commit after commit

Work classified as post-commit MUST NOT escape as a committed consequence of a transaction that later rolls back.

### Authorized cache after authoritative success

Private/authorized response caching MUST NOT record a successful result before the authoritative transaction completes.

### New behavior placement is architectural

A new global pipeline behavior must identify:

```text
its prerequisite state
the state it creates
its zone
its failure semantics
its ordering dependency
```

It is not inserted at an arbitrary registration position.

---

## Alternatives Considered

### Alternative A — No explicit zone model

This alternative is recoverable from the original ADR context as the condition being rejected:

```text
keep one long pipeline registration list
rely on maintainers to infer ordering dependencies
```

**Benefits**

- less explicit architecture documentation;
- no additional conceptual zone vocabulary.

**Costs / risks**

- order becomes fragile implicit knowledge;
- new behaviors can be inserted at incorrect positions;
- transaction/RLS/post-commit/cache semantics can be violated by refactor;
- failures can appear only at runtime.

**Why not chosen**

The original ADR explicitly states that the prior implicit ordering was fragile and that the zone model exists to make these dependencies explicit and enforceable.

### Other alternatives

```text
Not recorded in the original ADR.
```

This normalized record deliberately does not invent additional options such as a different mediator, decorator model, or custom pipeline engine because those alternatives were not documented in the historical ADR.

---

## Consequences

### Positive

The original ADR records these benefits:

- architecture tests can enforce order;
- new behaviors have an explicit placement model;
- the transaction/RLS/post-commit boundaries become reviewable;
- accidental reordering can be detected before production;
- cache/post-commit behavior can depend on successful transaction completion intentionally.

### Negative / trade-offs

The original ADR did not include a dedicated negative-consequences subsection.

The following historical fact is explicit from the decision itself:

```text
pipeline behavior ordering is now architecture,
not free implementation ordering.
```

Any additional trade-off not recorded in the original decision is not fabricated here.

### New obligations

The accepted decision creates durable obligations:

```text
Application composition
→ preserve the zone/order contract

new pipeline behavior
→ declare/justify its zone

architecture/runtime tests
→ reject invalid ordering

canonical Application architecture
→ document current meaning of each boundary
```

---

## Compatibility / Migration

The original ADR did not record a separate migration plan.

The accepted decision primarily normalized an existing Application pipeline into explicit zone boundaries and required enforcement.

Current compatibility implications are:

```text
request contracts/handlers should not depend on accidental old ordering
new pipeline behavior must enter at a compatible zone
moving an existing behavior across a boundary is an architecture change
removing/replacing a boundary requires reassessment of downstream dependencies
```

No persisted database migration or public API version migration is inherently required by the ADR itself.

A future change to the pipeline architecture can require:

```text
new ADR
canonical Application update
architecture/runtime test changes
security/RLS/idempotency/post-commit review
```

depending on the boundary affected.

---

## Evidence

### Canonical current architecture

- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/infrastructure-and-data.md`

### Source / manifests

- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/PostCommitScopeBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/DbRequestScopeBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/PostCommitEnqueueBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/AuthorizedCacheBehavior.cs`

### Tests / gates

- `backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`
  - `PipelineBehaviorOrder_ShouldHaveCorrectOrder`
  - `OnlyDbRequestScopeBehaviorCanCallRlsApply`
  - `OnlyDbRequestScopeBehaviorCanBeginTransaction`
  - `NoSeparatePostCommitBehaviorsExist`
- Application pipeline/runtime behavior tests as present in the current test tree.

### Contracts / migrations / generated evidence

No persisted-schema migration or generated public contract is owned directly by this decision.

Pipeline marker/request contracts under:

- `backend/src/Notrelix.Application/Common/Requests/`

are implementation evidence where relevant.

---

## Supersedes

`None`

The original ADR does not record a prior ADR that it superseded.

---

## Superseded By

`None`

Current registry status remains:

```text
Accepted
```

No newer backend ADR is currently recorded as superseding ADR-001.

---

## Notes

### Historical normalization note

This file has been normalized to the current ADR schema while preserving the accepted historical decision.

The normalization intentionally:

```text
adds front matter
adds ID/Date/Owners sections
adds explicit supersession fields
refreshes current evidence
separates current architecture from historical decision
marks unrecorded historical alternatives/ownership honestly
```

It does **not** change the accepted pipeline-zone architecture.

### Decision-change trigger

A new/superseding ADR should be considered if Notrelix materially changes:

```text
the pipeline zone model
the centralized DB/RLS/transaction boundary
the post-commit boundary
the relationship between authorization and DB/resource scope
the final authorized-cache ordering contract
```

Routine changes such as:

```text
adding one behavior inside an existing accepted zone
renaming a behavior
moving source files without changing semantics
refreshing evidence paths
```

do not automatically require a new ADR.
