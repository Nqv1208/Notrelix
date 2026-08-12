---
document_id: TEMPLATE-FEATURE-SPEC
document_type: template
status: active
owner: documentation-governance
applies_to:
  - product-features
  - backend-features
  - frontend-features
  - cross-stack-features
evidence:
  - docs/product/README.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/definition-of-done.md
  - docs/quality/engineering-quality-standard.md
review_on:
  - feature-spec-process-change
  - product-model-change
  - acceptance-policy-change
  - contract-first-policy-change
  - documentation-template-change
---

# Feature Specification Template

> **A feature specification defines the product behavior that must become true: problem, outcome, semantic owner, scope, state transitions, authorization, contracts, user-visible states, failure semantics, and acceptance proof.**
>
> It is not a substitute for canonical product context docs, an ADR, a migration plan, or a task-by-task implementation plan.

Use this template for a feature/capability whose behavior must be explicit enough that Backend, Frontend, QA, and coding agents do not invent product semantics independently.

The template intentionally preserves the strongest knowledge from the legacy Product Feature Specification while integrating cross-stack details only where they affect the feature contract. The legacy template explicitly required problem/outcome, semantic owner, authorization/scope, invariants/state transitions, cross-context facts, API/realtime/frontend state, data/migration, failures/operations, and acceptance proof. citeturn570799view3

---

# 1. When to use this template

Use for:

- new user-visible capability;
- material change to an existing product workflow;
- cross-stack feature with Backend + Frontend contract;
- lifecycle/state change;
- permission/share/entitlement-sensitive feature;
- feature with realtime/async/provider behavior;
- feature whose acceptance would otherwise be ambiguous.

---

# 2. When a feature spec is unnecessary

A small change MAY be sufficiently defined by:

```text
canonical product docs
issue/bug reproduction
existing contract
acceptance test
```

if no material semantic choice remains.

Do not create a feature spec merely to increase documentation count.

---

# 3. Feature spec versus canonical product docs

Feature spec is change-scoped.

Canonical context docs remain durable authority.

If the feature introduces a durable new product rule:

```text
feature spec
→ implementation/change evidence

canonical product/context doc
→ durable rule after acceptance
```

Do not leave important product semantics only in an old completed spec.

---

# 4. Feature spec versus architecture change

Feature spec answers:

```text
What must the product do?
```

Architecture change answers:

```text
What consequential architecture must change to support it?
```

A feature can require both.

---

# 5. Feature spec versus ADR

Do not resolve a consequential architecture choice casually inside a feature spec.

Instead:

```text
Feature requirement
→ architecture decision identified
→ ADR if required
→ architecture change artifact if material
```

The feature spec can reference the resolved ADR.

---

# 6. Feature spec versus implementation plan

This template may identify required surfaces and constraints.

It should not prescribe hundreds of line-level coding steps unless those details are necessary to remove a material semantic ambiguity.

Implementation tasks/plans should be derived after the feature contract is stable.

---

# 7. Writing rule

Use Notrelix domain language.

Prefer:

```text
Workspace member
Board Item
Page Block
Automation Execution
Integration Connection
Entitlement
```

over generic:

```text
record
object
thing
entity
service
```

when the product vocabulary is known.

---

# 8. Copy from here

```markdown
---
document_id: <FEATURE-ID-or-descriptive-id>
document_type: feature-spec
status: draft
owner: <product-context-logical-owner>
applies_to:
  - <context/capability>
evidence:
  - <canonical product/context doc>
  - <existing source/contract evidence>
review_on:
  - implementation-complete
  - behavior-changed
---

# <Feature name>

## 1. Summary

### Problem

<What user/business problem exists?>

### Outcome

<What observable result should exist after this feature?>

### Semantic owner

- Bounded context: `<context>`
- Capability/resource: `<resource/lifecycle>`
- Canonical product doc: `<path>`

### Primary actors

- ...

### Product scope

`<global / Account / Workspace / resource / user-private>`

### Primary change class

- `<C0..C8 as applicable>`

### Required related artifacts

- ADR: `<none/reference>`
- Architecture change: `<none/reference>`
- Migration plan: `<none/reference>`

---

## 2. User / business problem

Describe the current problem from product perspective.

### Current behavior

- ...

### Why current behavior is insufficient

- ...

### Who is affected

- ...

### User/business value

- ...

### Evidence

- user/workflow evidence:
- product constraint:
- existing implementation gap:

Do not manufacture analytics/customer evidence that does not exist.

---

## 3. Success outcome

Define observable post-feature behavior.

Good:

```text
A Workspace member with `board.item.update` can change an Item's Status field.
The mutation is reflected in all open Board views after server confirmation/realtime convergence.
```

Weak:

```text
Implement status editing.
```

### Success criteria

1. ...
2. ...
3. ...

---

## 4. Non-goals

Explicitly exclude adjacent work.

- ...
- ...

### Future possibilities, not part of this spec

- ...

Do not allow future ideas to become implementation requirements accidentally.

---

## 5. Ubiquitous language

Define only new/ambiguous feature terms.

| Term | Meaning | Owner |
|---|---|---|
| ... | ... | ... |

Do not redefine terms already owned by canonical context docs.

---

## 6. Semantic ownership

### Authoritative context

`<context>`

### Authoritative resource / aggregate / lifecycle

- ...

### Facts owned here

- ...

### Facts consumed from other contexts

| Fact | Owning context | How obtained | Freshness/consistency |
|---|---|---|---|
| ... | ... | sync contract/event/projection | ... |

### Facts explicitly not owned here

- ...

This section prevents a feature from absorbing foreign semantics.

---

## 7. Actors and access

### Actors

| Actor | Relationship | Intended capability |
|---|---|---|
| owner/admin | ... | ... |
| member | ... | ... |
| guest | ... | ... |
| public/share user | ... | ... |
| service principal | ... | ... |

Remove actors that truly do not apply.

### Authentication

- required?:
- anonymous/public behavior:

### Authorization

Define resource/action semantics.

| Operation | Resource | Required permission/policy | Denied behavior |
|---|---|---|---|
| ... | ... | ... | ... |

### Entitlement

If commercially gated:

- FeatureCode/entitlement:
- quota/limit:
- not-entitled behavior:

Remember:

```text
Entitlement
≠
Authorization
```

### Scope

- Account:
- Workspace:
- resource:
- private/user scope:

### Revocation

What happens if permission/membership/share/entitlement changes while the feature is open/in progress?

---

## 8. Preconditions

List conditions required before the operation starts.

- ...
- ...

Distinguish:

```text
invalid input
unauthorized
not entitled
not found
conflict
invalid lifecycle
```

---

## 9. State model

### Relevant states

```text
<ASCII state model or table>
```

### State transitions

| From | Operation | To | Preconditions | Side effects |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |

### Terminal states

- ...

### Archived/deleted behavior

- ...

### Restore behavior

- ...

Delete lifecycle sections only if the feature genuinely has no lifecycle consequence.

---

## 10. Invariants

List product invariants.

Examples:

```text
A Workspace must retain at least one owner.
An Item value must match the owning Board Field type.
A revoked share link cannot grant future access.
```

### Invariants introduced

- ...

### Existing invariants exercised

- ...

### Cross-context invariants

State which owner enforces each one.

---

## 11. Operation semantics

For each main operation define:

### `<Operation name>`

#### Intent

...

#### Input facts

...

#### Preconditions

...

#### Authorization

...

#### Success

...

#### No-op

When is the request semantically a no-op?

Does it:

```text
change version?
emit event?
write history?
invalidate cache?
```

#### Rejection

- validation:
- lifecycle:
- permission:
- entitlement:
- concurrency:

#### Concurrency

- expected version/ETag:
- stale conflict behavior:
- merge behavior if any:

#### Events / downstream facts

- ...

Repeat per materially different operation.

---

## 12. User-visible behavior

### Loading

- ...

### Empty

- ...

### Success

- ...

### Validation error

- ...

### Authorization denied

- ...

### Not entitled / quota reached

- ...

### Read-only

- ...

### Concurrency conflict

- ...

### Pending

- ...

### Provider unknown outcome

- ...

### Offline / realtime disconnected

- ...

### Destructive confirmation

- ...

Remove only states proven irrelevant.

---

## 13. Web / mobile / marketing applicability

| Host | Applies? | Required behavior |
|---|---:|---|
| web | yes/no | ... |
| mobile | yes/no | ... |
| marketing | yes/no | ... |

### Parity

Define what must be semantically equivalent across hosts.

### Host-specific behavior

List legitimate platform differences.

Do not invent mobile behavior by copying web DOM interactions.

---

## 14. Accessibility contract

For user-facing feature describe relevant:

```text
keyboard
focus
accessible name/role/state
error association
drag alternative
screen reader
zoom/reflow
reduced motion
touch/native semantics
```

### Critical accessibility acceptance

- ...

---

## 15. API contract

If an API change is required:

### Operations

| Operation | Method/path concept | Request | Result | Error categories |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |

The exact OpenAPI producer remains Backend API.

### Idempotency

- required?:
- operation identity:
- retry semantics:

### Concurrency

- ...

### Pagination/filter/sort

- ...

### Compatibility

- additive:
- breaking:
- old consumer behavior:

Do not define endpoint shapes from database CRUD convenience.

---

## 16. Generated client / frontend contract

- producer:
- codegen:
- generated types:
- consumer mapping:
- compatibility:

Generated DTOs are not a second handwritten authority.

---

## 17. Realtime contract

If realtime is needed:

### Why realtime exists

`<freshness/convergence reason>`

### Event consequence

- ...

### Resource scope

- ...

### Revision/version

- ...

### Duplicate

- ...

### Out-of-order

- ...

### Gap/reconnect

- ...

### Permission revocation

- ...

### Authoritative reconciliation

- ...

Realtime does not become durable source truth.

---

## 18. Cross-context facts and side effects

| Producer/owner | Fact/action | Consumer | Sync/async | Failure behavior |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |

### Synchronous facts

- ...

### Committed events

- ...

### External provider effects

- ...

### Consistency choice

Explain why the feature needs:

```text
strong local transaction
eventual consistency
pending external action
derived projection
```

Do not create distributed transaction assumptions silently.

---

## 19. Data semantics

### Durable data introduced/changed

- ...

### Authoritative owner

- ...

### Derived data

- ...

### History/audit/activity

Clarify which history is:

```text
business history
Governance Audit
user Activity
version history
execution history
```

Do not merge them generically.

### Retention/deletion

- ...

### Privacy/sensitivity

- ...

---

## 20. Schema / migration impact

If none:

```text
No persistence migration required because ...
```

Otherwise:

### Schema/index/RLS

- ...

### Existing data

- ...

### Backfill

- ...

### Mixed-version compatibility

- ...

### Completion proof

- ...

### Destructive cleanup

- ...

Detailed execution belongs to `migration-plan-template.md`.

---

## 21. Failure semantics

Define user/product meaning for:

```text
validation
authorization
not entitled
concurrency
dependency unavailable
provider timeout
provider rejected
unknown external outcome
background failure
realtime disconnect
```

### Retryable

- ...

### Terminal

- ...

### Reconciliation required

- ...

Do not collapse all failures into “Something went wrong”.

---

## 22. Idempotency and duplicate behavior

For retryable create/update/external effects:

- logical operation identity:
- duplicate same request:
- duplicate different request:
- retention:
- external provider idempotency:
- message consumer dedup:

If not needed, explain why.

---

## 23. Ordering

If order matters:

- ordering key:
- deterministic order representation:
- concurrent move/update:
- retry:
- compaction/rebalance if any:

For Work Management/Document reorder, describe product outcome, not algorithm implementation unless the algorithm is itself a product/architecture constraint.

---

## 24. Performance / scale expectations

State product/workload expectations, not invented SLO numbers.

### Expected cardinalities

- ...

### Potentially large collections

- ...

### Required pagination/windowing

- ...

### Hot filter/sort/report behavior

- ...

### Frontend density/virtualization concerns

- ...

### Provider/rate-limit concerns

- ...

If a numerical requirement is real, cite its approved owner/evidence.

---

## 25. Security / privacy

### Sensitive data

- ...

### Public/share behavior

- ...

### Provider/webhook/file boundary

- ...

### Abuse/rate-limit concerns

- ...

### Audit requirements

- ...

### Security negative cases

- ...

---

## 26. Observability / operations

Feature-level observable states:

- success/failure:
- pending:
- backlog:
- provider:
- migration:
- freshness:

### New operational failure modes

- ...

### Degradation

What reduced mode remains safe?

- ...

### Recovery

What state requires reconciliation?

- ...

### Runbook impact

- ...

Do not invent SLO thresholds here.

---

## 27. Notifications / activity / collaboration

If the feature emits user-facing collaboration artifacts:

### Notification

- recipient:
- trigger:
- current permission behavior:
- deep link/resource:

### Activity

- product-visible event:
- actor:
- target:

### Audit

- separate security/governance evidence if required:

Do not treat Notification, Activity, and Audit as one generic event stream.

---

## 28. Automation impact

Can this feature:

```text
trigger Automation?
be targeted by Automation?
be queried by Conditions?
perform provider Actions?
```

Define stable product facts/actions if applicable.

Do not expose internal implementation events automatically.

---

## 29. Analytics impact

Can this feature be measured/reportable?

Define source facts and ownership.

Do not make Analytics own source business state.

If there is no approved metric definition, do not invent one merely because the feature exists.

---

## 30. Billing impact

Does it affect:

```text
entitlement
quota
usage
Subscription
invoice/payment
```

If yes, identify Billing contract.

Do not count product behavior as billable usage unless Billing defines the metric.

---

## 31. Acceptance scenarios

Use independently checkable scenarios.

### Scenario F-01 — `<name>`

**Given**

...

**When**

...

**Then**

...

**And**

...

### Scenario F-02 — `<name>`

...

Include:

```text
happy path
permission denial
invalid state
concurrency
no-op
failure/retry
tenant isolation
host/realtime behavior
```

as relevant.

---

## 32. Acceptance proof matrix

| Requirement / invariant | Primary proof | Cross-boundary proof |
|---|---|---|
| F-01 | ... | ... |
| F-02 | ... | ... |
| permission | ... | ... |
| tenant | ... | ... |
| migration | ... | ... |
| realtime | ... | ... |
| accessibility | ... | ... |

Do not use E2E for every invariant when a cheaper reliable seam exists.

---

## 33. Definition of Done additions

List only feature-specific completion conditions beyond the repository DoD.

- ...

Repository-wide DoD remains:

```text
docs/delivery/definition-of-done.md
```

---

## 34. Resolved product decisions

These decisions are part of the spec and MUST NOT be reopened by the implementing agent.

1. ...
2. ...
3. ...

---

## 35. Blocking unresolved questions

If none:

```text
None.
```

Otherwise:

| Question | Owner | Why blocking | Required resolution |
|---|---|---|---|
| ... | ... | ... | product decision / ADR / contract |

Do not leave material questions as:

```text
TBD
```

and then tell a coding agent to proceed.

---

## 36. Out of scope

- ...
- ...

---

## 37. Related canonical documents

### Product

- ...

### Architecture

- ...

### Quality

- ...

### Delivery

- ...

### ADR

- ...

---

## 38. Implementation handoff

The implementation agent receives:

```text
this feature contract
+
canonical docs
+
resolved ADRs/architecture change
+
current source evidence
```

The agent is authorized to choose only normal implementation details already constrained by those authorities.

The agent is NOT authorized to invent:

- new bounded context;
- new permission semantics;
- destructive migration policy;
- public compatibility strategy;
- provider unknown-outcome semantics;
- new architecture exception.

---

## 39. Final feature acceptance

The feature is accepted when:

```text
[ ] problem/outcome satisfied
[ ] semantic owner unchanged/correct
[ ] required actors/scope authorized
[ ] invariants and state transitions proven
[ ] all defined failure states behave correctly
[ ] contracts/generated consumers synchronized
[ ] realtime converges if applicable
[ ] data/migration completed if applicable
[ ] tenant/security negative cases pass
[ ] accessibility proof passes
[ ] performance/scalability obligations pass
[ ] operations/degradation/recovery are sufficient
[ ] feature-specific acceptance scenarios pass
[ ] repository Definition of Done passes
[ ] durable new product semantics rehomed to canonical docs
```
```

---

# 9. Required product depth

A spec should be as deep as the feature risk.

A simple read-only preference feature does not need a page of message ordering.

A cross-tenant share/integration/automation feature may need every section.

Depth follows:

```text
semantic risk
security
durability
cross-boundary consumers
failure modes
migration
```

not template symmetry.

---

# 10. State-transition quality

Do not define only:

```text
request
→ success
```

For mutable lifecycle behavior, specify:

```text
precondition
success transition
no-op
rejection
concurrency
event/history consequence
deletion/archive interaction
```

This was a core durable requirement in the legacy Feature Specification template. citeturn570799view3

---

# 11. Authorization quality

Every protected feature should identify:

```text
principal
resource
action
tenant scope
guest/public behavior
entitlement if any
```

Avoid ambiguous:

```text
admins can do this
```

if the canonical product uses resource/action policy.

---

# 12. Cross-context quality

A feature can touch many contexts without merging their ownership.

Example:

```text
Automation detects Work Management fact
→ owns Rule/Execution

Work Management
→ owns Item mutation

Governance
→ authorizes target action

Integrations
→ owns provider Connection

Billing
→ may gate entitlement
```

The spec names each owner and contract.

---

# 13. Backend handoff quality

The feature spec should provide enough semantic information that a Backend implementation plan can derive:

```text
Domain invariant
Application operation
authorization resource/action
transaction
persistence
event/async consequence
API contract
```

It should not dictate an arbitrary repository pattern or new project unless architecture has decided that.

---

# 14. Frontend handoff quality

The spec should provide enough user/product behavior that Frontend can derive:

```text
generated contract usage
server-state owner
query/mutation behavior
realtime convergence
loading/error/denied/read-only/conflict
accessibility
web/mobile host behavior
```

The legacy Frontend capability template correctly treated these as required capability planning concerns. citeturn570799view0

---

# 15. Acceptance criteria quality

Good acceptance criteria are:

```text
observable
independently checkable
semantic
permission-aware
failure-aware
```

Bad:

```text
code is clean
API works
UI looks good
```

Those are not feature contracts.

---

# 16. No invented implementation

Do not put speculative:

```text
create Redis cache
add RabbitMQ event
create new package
add microservice
use optimistic update
```

into the spec unless the product contract or accepted architecture actually requires it.

Feature spec constrains outcomes.

Architecture docs constrain structural choices.

Implementation chooses ordinary details within those boundaries.

---

# 17. Product-to-canonical lifecycle

After implementation:

```text
temporary spec-specific semantics
→ may remain historical evidence

durable product invariant
→ canonical context/product doc

architecture decision
→ ADR + architecture doc

runtime recovery
→ operations

migration tracker
→ retire after completion
```

This prevents completed feature specs from becoming a second permanent product handbook.

---

# 18. Feature-spec quality test

A feature spec is ready for implementation when a coding agent can answer:

```text
What user/business outcome must exist?
Which context owns every important fact?
Who can perform each operation and on which resource?
What states/transitions/no-ops/rejections exist?
What happens under concurrency/failure/retry?
Which cross-context contracts are required?
What do web/mobile users see in every important state?
What persistence/migration consequence exists?
What acceptance scenarios prove success?
Which decisions are already fixed?
Which unresolved choices block implementation?
```

If the agent must invent those answers, the feature spec is incomplete.
