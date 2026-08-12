---
document_id: DEL-CONTRACT-FIRST
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
  - api
  - events
  - realtime
  - generated-contracts
  - public-packages
  - integrations
evidence:
  - RULE.md
  - AGENTS.md
  - docs/governance/decision-and-exception-policy.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/change-classification.md
  - docs/quality/testing-strategy.md
  - backend/contracts/openapi/notrelix.v1.json
  - frontend/tooling/codegen/
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
review_on:
  - public-contract-change
  - versioning-policy-change
  - generated-contract-change
  - event-contract-change
  - realtime-contract-change
  - public-package-change
  - mobile-compatibility-policy-change
  - provider-contract-change
---

# Contract-First Delivery

> **Cross-boundary behavior starts with explicit semantics and an explicit contract before independently changing producer and consumer implementations.**
>
> Contract-first does not mean “write JSON first.” It means the producer meaning, consumer needs, compatibility, ownership, failure behavior, security scope, and rollout constraints are known before implementation choices create accidental coupling.

This document owns repository-wide contract-first delivery procedure.

`contract-boundaries.md` owns what a contract **is**.

`change-classification.md` owns how the change is **classified**.

`release-and-rollout.md` owns deployment/cohort mechanics.

`migration-policy.md` owns migration/backfill mechanics.

This file owns the **order in which cross-boundary change is designed and implemented**.

---

# 1. Applies to

Contract-first delivery applies to stable boundaries including:

```text
REST / HTTP / OpenAPI
integration/public events
realtime protocol
message/public envelope where consumer-visible
generated frontend/client contracts
public package exports
provider/webhook adapter contracts
persisted compatibility contracts when multiple versions coexist
```

It does not require a public contract for every private helper call.

---

# 2. DEL-CON-001 — Product semantics precede transport shape

Before defining an endpoint/event/generated type, answer:

```text
what product fact/action is represented?
which context owns it?
who consumes it?
what is authoritative?
what errors/outcomes exist?
what tenant/resource scope applies?
```

Transport syntax follows semantic ownership.

---

# 3. Contract-first versus schema-first

Schema-first can be useful for a known contract producer.

Contract-first is broader:

```text
semantic contract
→ compatibility
→ producer artifact
→ generated/consumer contract
→ implementation
```

A perfectly valid OpenAPI schema can still encode the wrong product ownership.

---

# 4. DEL-CON-002 — Internal implementation is not a contract

Do not couple consumers to:

```text
database table names
EF entity names
CLR class names
source folder paths
private TypeScript module paths
provider SDK objects
internal event class names alone
```

unless that identity is intentionally part of the public compatibility surface.

---

# 5. Logical identity

Stable contract identity should survive implementation refactors.

Examples:

```text
resource kind
operation name
event logical name
field/property meaning
public package export
provider capability name
```

---

# 6. DEL-CON-003 — Contract identity is logical, not accidental source identity

Renaming a CLR/TS class does not automatically rename:

- event identity;
- JSON field;
- OpenAPI operation;
- public package export;
- provider mapping key.

Changing the logical identity is separately classified.

---

# 7. Canonical sequence

For cross-boundary work, follow:

```text
1. confirm semantic owner and product behavior
2. classify change
3. inventory producer + consumers
4. define or revise contract
5. assess compatibility/mixed-version window
6. define producer support strategy
7. update contract source / generator source
8. implement producer
9. generate/publish checked artifacts
10. update consumers/adapters
11. run contract/integration/drift evidence
12. deploy using compatible sequence
13. remove old compatibility path only after proof
```

Not every local change needs every step.

---

# 8. DEL-CON-004 — Producer and consumer inventory is explicit

For each contract identify:

```text
producer
current consumers
generated consumers
mobile consumers
background consumers
external consumers if any
provider participants
stored backlog/replay consumers
```

Do not infer consumers from one folder.

---

# 9. Producer

The producer owns the contract source for the boundary.

Examples:

```text
backend API
→ OpenAPI source/spec

integration event producer
→ event schema/logical identity

frontend package
→ public export surface

provider adapter
→ provider-facing request/response mapping
```

---

# 10. Consumer

A consumer is anything that relies on contract meaning, not only direct code imports.

Consumers can include:

```text
web
mobile
background worker
automation
integration adapter
analytics projection
external API client
queued old message
generated SDK/client
test fixture
```

---

# 11. DEL-CON-005 — Consumers are classified by deployment independence

A consumer that can lag deployment changes compatibility requirements.

Typical independently deployed consumers:

```text
backend
web
mobile
workers
external integrations
provider callbacks
```

One monorepo does not make them one deployment unit.

---

# 12. Same PR does not imply atomic release

A pull request can contain backend + web + mobile changes.

Production can still have:

```text
new backend + old mobile
new worker + old queued event
new web + old backend during deployment
```

---

# 13. DEL-CON-006 — Mixed-version behavior is designed explicitly

For every independently deployed consumer, define:

```text
old producer + old consumer
new producer + old consumer
old producer + new consumer if possible
new producer + new consumer
```

Relevant combinations must remain valid during rollout.

---

# 14. Compatibility classes

Contract change can be:

```text
additive compatible
behavioral compatible
breaking
```

Use `change-classification.md` for full classification.

---

# 15. DEL-CON-007 — Additive means consumer-compatible, not merely schema-additive

Adding:

- field;
- enum value;
- event;
- endpoint;
- status;

can still break a strict/old consumer.

Compatibility is proven against actual consumer behavior.

---

# 16. Enum evolution

New enum value requires old-consumer behavior.

Possible safe policy:

```text
unknown value fallback
versioned enum
consumer-before-producer rollout
```

Do not assume all generated clients tolerate unknown enum values.

---

# 17. Requiredness/nullability

Changing optional ↔ required is a contract change even if type name remains unchanged.

Defaults and missing-value behavior are part of compatibility.

---

# 18. DEL-CON-008 — Defaults are semantic contract

Changing default meaning can be C2/C3 even if the wire schema is unchanged.

Consumers/tests must reflect the new semantic default.

---

# 19. Error contract

Errors are part of the contract.

Consumer-relevant distinctions include:

```text
validation
authorization
not found
conflict
concurrency
pending
retryable failure
terminal failure
unknown outcome
```

---

# 20. DEL-CON-009 — Failure semantics are versioned with success semantics

A contract change is incomplete if success schema is updated but error/retry behavior is left implicit.

---

# 21. Timing/consistency

Contract can define:

```text
synchronous final result
accepted/pending
eventually consistent projection
retry-after
poll/realtime completion
```

These are observable semantics.

---

# 22. DEL-CON-010 — Completion semantics are contractual

Changing:

```text
200 final
→ 202 pending
```

is a semantic contract change even if the resource shape later looks the same.

---

# 23. Authorization/tenant scope

Contract definition includes:

```text
principal requirements
Account/Workspace/resource scope
permission action
guest/public behavior
```

where applicable.

---

# 24. DEL-CON-011 — Contract does not smuggle authorization through transport fields

A `workspaceId`, `role`, or `isAdmin` in request payload is input to validation, not proof of authority.

Server policy remains authoritative.

---

# 25. REST/API contract

For an HTTP boundary define:

```text
operation/resource meaning
method/path/version
request
response
status/error
authorization
idempotency
pagination
concurrency
compatibility
```

---

# 26. DEL-CON-012 — REST contract follows resource semantics, not database CRUD shape

Do not create endpoints solely because a table/entity exists.

Use product operation/resource meaning.

---

# 27. OpenAPI

OpenAPI is generated/committed contract evidence for the API producer.

Current backend CI exports the current API spec and compares it to committed contract.

Drift requires explicit regeneration/review.

---

# 28. DEL-CON-013 — OpenAPI drift is intentional or failing

If implementation changes the API:

```text
update intended source
→ regenerate/export
→ review diff
→ update consumers
```

Do not suppress drift because “code compiles”.

---

# 29. Generated frontend/client contract

Generated artifacts are downstream evidence.

They do not become an independent handwritten DTO authority.

---

# 30. DEL-CON-014 — Generated contracts change through producer source

Correct sequence:

```text
backend/public schema
→ generator
→ committed generated artifact
→ frontend consumer
```

Do not manually patch generated output to hide producer mismatch.

---

# 31. Generation determinism

Same producer source/config should produce same generated artifact.

Generation drift is a contract-quality failure.

---

# 32. Event contract

Public/integration event defines:

```text
logical event identity
version
producer
scope
fact meaning
required fields
compatibility
ordering expectation if any
replay/idempotency identity
```

---

# 33. DEL-CON-015 — Domain event is not automatically public event

Internal Domain event may be mapped into:

- integration event;
- realtime event;
- activity;
- analytics fact;

with different stable shape.

Do not expose aggregate internals by default.

---

# 34. Event facts

Events describe committed facts.

They are not hidden commands such as:

```text
PleaseUpdateBoard
```

disguised as past tense.

---

# 35. DEL-CON-016 — Public event change considers stored backlog and replay

Removing/renaming/changing event fields requires reviewing:

```text
queued messages
dead-letter/replay
old consumers
analytics/backfill
mobile/realtime if shared
```

---

# 36. Realtime contract

Realtime is freshness/convergence contract, not source truth.

Define:

```text
logical event identity
resource/Workspace scope
version/revision/sequence
duplicate behavior
out-of-order behavior
gap/reconnect behavior
authorization
```

---

# 37. DEL-CON-017 — Realtime payload can differ from integration event

Realtime should carry what client convergence needs.

Do not force one universal event shape across:

```text
Domain
integration
realtime
activity
analytics
```

---

# 38. Realtime compatibility

Old clients may receive new events or payload fields.

Unknown-event/value behavior must be explicit.

---

# 39. Public package contract

Frontend/backend packages with intended consumers expose stable public entry points.

Internal folders are not public API.

---

# 40. DEL-CON-018 — Consumer imports public exports, not internal paths

Do not use:

```text
@package/src/internal/...
deep relative path
private implementation folder
```

to bypass package ownership.

Frontend dependency tooling should enforce relevant boundaries.

---

# 41. Package export change

Removing/renaming public export is a contract change.

Moving private implementation behind the same export may be local.

---

# 42. Provider contract

External providers are independently evolving systems.

Provider-facing contract includes:

```text
request/response schema
authentication/signature
rate limit
idempotency/correlation
version
webhook behavior
unknown outcome
```

---

# 43. DEL-CON-019 — Provider schema is translated, not propagated into product domains

Provider changes are isolated through Integrations/Billing/other adapter owners.

Do not regenerate core Domain types directly from provider SDK.

---

# 44. Webhook contract

Inbound webhook must define:

```text
raw verification needs
provider delivery/event identity
schema/version
trusted Connection mapping
tenant resolution
duplicate/replay behavior
```

---

# 45. DEL-CON-020 — Verified provider transport still passes product validation

Signature proves provider authenticity under the provider protocol.

It does not prove Notrelix business validity or authorization.

---

# 46. Persisted compatibility contract

Some persisted values behave like contracts because old/new code must read them concurrently.

Examples:

```text
discriminator
status
event name
feature code
metric key
JSON config version
provider mapping key
```

---

# 47. DEL-CON-021 — Persisted discriminator/key change is classified as contract + data change

Do not call it a local enum rename when old rows remain.

Use migration/dual-reader compatibility as needed.

---

# 48. Consumer-first versus producer-first

For additive changes:

```text
producer-first
```

is often safe if old consumers ignore/tolerate the addition.

For removals/breaks:

```text
consumer-first / dual support
```

is usually required.

Exact sequence follows compatibility.

---

# 49. DEL-CON-022 — Rollout order follows compatibility, not team ownership

The backend team does not automatically deploy first.

The frontend/mobile team does not automatically deploy last.

Choose the order that keeps every deployed stage valid.

---

# 50. Parallel version contract

For breaking evolution, a producer may support:

```text
v1 + v2
old + new field
old + new event
old + new public export adapter
```

temporarily.

---

# 51. DEL-CON-023 — Compatibility path is temporary and observable

A dual contract declares:

```text
owner
consumers remaining
removal condition
telemetry/evidence
```

It is not permanent duplicate architecture.

---

# 52. Deprecation

Deprecation communicates planned removal.

For independently deployed consumers, include a realistic migration window based on product support policy.

---

# 53. Mobile

Mobile can lag server deployment materially.

Contract design must accommodate supported client-version policy.

---

# 54. DEL-CON-024 — Backend remains compatible with supported mobile floor

Do not remove/change contract solely because latest mobile source is updated.

Release policy determines when old mobile can be dropped.

---

# 55. Web

Web may deploy faster but can still overlap backend deployment.

Use compatibility during rolling deployments unless infrastructure proves atomicity.

---

# 56. Workers

Background workers can be independently deployed and can consume old backlog.

Treat them as consumers, not backend implementation detail.

---

# 57. DEL-CON-025 — Worker compatibility includes in-flight messages

A new worker must understand messages that can still exist when it starts.

An old worker must not receive newly incompatible messages during rollout.

---

# 58. External consumers

If Notrelix exposes external APIs/webhooks later, they require explicit version/deprecation policy.

Do not assume same-repo migration.

---

# 59. Contract test

Contract tests prove:

- serialization;
- required/optional fields;
- stable identity;
- error semantics;
- compatibility.

They complement behavior tests.

---

# 60. DEL-CON-026 — Contract test asserts public meaning, not private implementation

Avoid snapshots of huge internal objects.

Prefer intentional public examples and compatibility assertions.

---

# 61. Consumer test

Consumer test should prove the consumer handles the producer contract/version it claims to support.

Generated compile alone may not prove semantic handling of new statuses/errors.

---

# 62. DEL-CON-027 — Critical consumer semantics are tested explicitly

Examples:

```text
unknown enum fallback
pending outcome
authorization denial
realtime gap
new optional field
deprecated old path
```

---

# 63. Contract fixtures

Fixtures should be:

- versioned when needed;
- minimal;
- representative;
- obviously synthetic;
- safe.

Do not use current production payload dumps as unchecked test fixtures.

---

# 64. Consumer-driven feedback

Consumer needs can shape contract design.

The producer owner still decides product semantic ownership.

---

# 65. DEL-CON-028 — Consumer convenience does not move source ownership

A frontend/report consumer requesting a flattened shape can receive a read contract.

That does not make frontend/Analytics owner of the business fact.

---

# 66. Over-fetching versus stable shape

A convenient giant response can create:

- coupling;
- payload growth;
- security leakage;
- compatibility burden.

Prefer bounded purpose-driven contracts.

---

# 67. DEL-CON-029 — Contract includes only data needed for its purpose

Do not serialize entire aggregate graph “for future use”.

Add fields deliberately.

---

# 68. Query contracts

List/search endpoints define:

- pagination;
- sort;
- filter;
- authorization;
- projection;
- consistency/freshness.

These are contract semantics.

---

# 69. DEL-CON-030 — List/detail shapes may differ intentionally

A list contract should not be forced to return full detail object when not needed.

---

# 70. Concurrency contract

Mutation contract should expose/consume expected version/ETag/etc. when stale writes need protection.

---

# 71. DEL-CON-031 — Concurrency failure is a first-class contract outcome

Do not silently convert stale-write conflict into generic validation or success.

---

# 72. Idempotency contract

For retried mutation, define:

```text
logical operation identity
key scope
same-key/same-request behavior
same-key/different-request behavior
retention
```

---

# 73. DEL-CON-032 — Idempotency semantics are shared producer-consumer contract

Client/worker must know whether a retry is safe and how result replay/conflict behaves.

---

# 74. Pagination compatibility

Changing default/max page size can change consumer behavior/performance.

Classify intentional changes.

---

# 75. Time/date contract

Define:

```text
date-only
instant
local datetime
timezone
duration
```

where relevant.

Do not encode ambiguous strings and leave interpretation to each consumer.

---

# 76. DEL-CON-033 — Time meaning is semantic, not formatting

ISO formatting alone does not answer whether value is date-only or instant.

---

# 77. Monetary contract

Billing money contracts define currency/precision/amount semantics.

Do not expose floating-point ambiguous monetary values.

---

# 78. File/binary contract

Large file transfer should use dedicated upload/download/object contracts.

Do not embed arbitrary binary in normal JSON/events.

---

# 79. DEL-CON-034 — Large payload transfer uses purpose-built boundary

This preserves performance, security, retry, and authorization semantics.

---

# 80. Cross-context command

One context should not invoke another context's internal Domain model directly.

Use Application/public capability contracts.

---

# 81. DEL-CON-035 — Cross-context contract preserves target-context invariants

Caller supplies intent/facts.

Target owner validates current state and authorization.

---

# 82. Synchronous versus asynchronous contract

Use sync when caller requires immediate authoritative result and operation fits the consistency boundary.

Use async when durable side effect/process can continue independently.

---

# 83. DEL-CON-036 — Async is not used to hide unclear ownership

Before adding a queue/event, define:

```text
source fact
consumer owner
delivery semantics
user-visible outcome
retry
idempotency
```

---

# 84. Contract documentation

The canonical contract owner should explain:

- meaning;
- compatibility;
- lifecycle;
- consumer implications.

Generated schema explains exact machine shape.

Do not duplicate generated field inventory manually when generation can own it.

---

# 85. DEL-CON-037 — Generated facts are generated; authored docs explain semantics

OpenAPI/package map/schema output belongs to generated/executable evidence.

Authored docs explain why/how consumers use it.

---

# 86. Breaking contract plan

A breaking change plan must state:

```text
old contract
new contract
producer
consumers
mixed-version combinations
migration/adapter/version
deploy order
backlog/mobile considerations
removal proof
```

---

# 87. Contract-first PR shape

A material change may be split into PRs/stages:

```text
A: compatible contract expansion
B: producer implementation
C: consumer migration
D: switch
E: old contract removal
```

when this reduces risk.

One giant PR is not preferred automatically.

---

# 88. DEL-CON-038 — PR boundaries follow safe review/deployment units

Split when it improves:

- compatibility;
- reviewability;
- rollback;
- ownership.

Do not split so finely that intermediate source cannot compile/test.

---

# 89. Contract drift

Drift means implementation/generated/consumer meaning no longer matches intended contract.

Drift is classified and fixed at the owner.

---

# 90. DEL-CON-039 — Drift is not normalized by handwritten adapter hacks

If producer contract is wrong, fix producer/contract source.

If consumer is stale, migrate consumer.

Avoid accumulating invisible compatibility code with no removal owner.

---

# 91. Contract deletion

Before removal prove:

```text
no supported consumer requires it
no backlog/replay requires it
no generated artifact exports it
no provider depends on it
no migration is incomplete
```

---

# 92. DEL-CON-040 — Old contract removal is a separate classified change

Removal is not an automatic cleanup step hidden inside the first introduction of replacement.

---

# 93. Contract-first checklist

```text
[ ] semantic owner
[ ] product fact/action
[ ] change class/modifiers
[ ] producer
[ ] consumers
[ ] deployment independence
[ ] old/new compatibility
[ ] auth/tenant scope
[ ] success/error/timing
[ ] idempotency/concurrency
[ ] generated producer/consumer
[ ] mobile/backlog/provider
[ ] evidence
[ ] removal condition
```

---

# 94. REST checklist

```text
[ ] resource/action semantics
[ ] path/version
[ ] request/response
[ ] errors
[ ] auth/tenant
[ ] idempotency
[ ] concurrency
[ ] pagination
[ ] OpenAPI
[ ] generated client
[ ] mixed-version rollout
```

---

# 95. Event checklist

```text
[ ] logical identity
[ ] producer owner
[ ] committed fact
[ ] version/schema
[ ] tenant/resource scope
[ ] duplicate/replay
[ ] ordering
[ ] consumers
[ ] backlog
[ ] compatibility
```

---

# 96. Realtime checklist

```text
[ ] logical identity
[ ] subscription scope
[ ] authorization
[ ] payload/revision
[ ] duplicate/out-of-order
[ ] gap/reconnect
[ ] old client behavior
[ ] query reconciliation
```

---

# 97. Generated-contract checklist

```text
[ ] producer source changed
[ ] generator deterministic
[ ] generated diff reviewed
[ ] committed artifact updated
[ ] consumer compiles
[ ] semantic consumer tests
[ ] drift gate green
```

---

# 98. Stop conditions

Stop rather than implement if:

- transport shape is being defined before semantic owner/meaning;
- producer/consumer inventory is missing;
- same PR is used as proof of atomic deployment;
- mobile/background backlog compatibility is ignored;
- internal CLR/DB/folder name is treated as public contract unintentionally;
- generated file is patched directly;
- old consumer behavior for new enum/status is unknown;
- error/timing/idempotency behavior is unspecified;
- provider webhook schema is propagated directly into product Domain;
- compatibility path has no removal condition;
- old contract is removed before migration evidence.

---

# 99. Related canonical owners

```text
docs/architecture/contract-boundaries.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/delivery/change-classification.md
docs/delivery/definition-of-done.md
docs/delivery/release-and-rollout.md
docs/delivery/migration-policy.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
```

---

# 100. Final contract-first rule

For every cross-boundary change, answer:

```text
What product meaning is crossing the boundary?
Who owns it?
Who produces it?
Who consumes it?
Which consumers deploy independently?
What old/new combinations must coexist?
What is the success/error/timing/auth/idempotency contract?
Which artifact is the machine-readable producer?
How are generated consumers updated?
What evidence proves compatibility?
What objective evidence permits removing the old contract?
```

The target is:

> **cross-boundary delivery in which semantics and compatibility are intentional before code diverges, producer artifacts remain authoritative, consumers can migrate safely across mixed versions, and compatibility paths disappear only after proof.**
