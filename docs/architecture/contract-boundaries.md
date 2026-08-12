---
document_id: SYS-CONTRACT-BOUNDARIES
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
  - backend
  - frontend
  - public-contracts
  - integration-contracts
  - realtime-contracts
  - generated-contracts
evidence:
  - PRODUCT.md
  - RULE.md
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.API/
  - backend/src/Notrelix.Platform/
  - backend/tests/Notrelix.API.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/realtime.md
  - frontend/tooling/
  - artifacts/contracts/
review_on:
  - public-contract-change
  - api-versioning-change
  - integration-event-change
  - realtime-contract-change
  - generated-contract-producer-change
  - provider-webhook-contract-change
  - compatibility-policy-change
  - service-extraction-change
---

# Contract Boundaries

> **A contract is the intentionally stable boundary between independently changing producers and consumers.**
>
> Notrelix contracts preserve product meaning while allowing implementations, projects, packages, hosts, providers, and future deployment units to evolve independently.

This document is the canonical cross-stack owner for:

- contract classes;
- producer/consumer ownership;
- compatibility classification;
- versioning and deprecation;
- rollout ordering;
- generated-contract ownership;
- semantic-versus-structural compatibility;
- cross-boundary identity/scope requirements.

It does not own:

- exact HTTP endpoint implementation;
- exact OpenAPI generator configuration;
- Application use-case contracts internal to backend;
- exact Platform envelope/dedup implementation;
- frontend query/realtime reconciliation mechanics;
- provider SDK mechanics;
- database migration commands.

Those belong to the relevant project owners.

---

# 1. Contract thesis

Notrelix uses explicit contracts wherever independently changing components communicate.

The stability target is not:

```text
"never change JSON"
```

The target is:

```text
stable product meaning
+
explicit producer ownership
+
known consumer expectations
+
intentional compatibility
+
safe migration
```

A contract is complete only when both shape and meaning are understood.

---

# 2. Contract versus implementation

Implementation artifacts include:

- Domain aggregate;
- EF entity;
- database row;
- Application handler;
- React component;
- provider SDK object;
- internal message class;
- query cache object.

They are not automatically public/cross-boundary contracts.

A transport shape may initially resemble an implementation type.

Its compatibility lifecycle is still independent.

---

# 3. SYS-CON-001 — Producer and consumers are explicit

Every material cross-boundary contract MUST identify:

```text
producer
semantic owner
known consumer classes
contract identity
scope
compatibility expectations
evidence
```

A payload with no owner is not a stable contract.

---

# 4. SYS-CON-002 — Product semantics outrank transport representation

Changing transport representation MUST NOT silently change product meaning.

Equivalent product meaning may be represented through:

- REST JSON;
- generated TypeScript;
- realtime notification;
- integration event;
- provider translation.

The representations differ.

The product fact remains owned by its product context.

---

# 5. SYS-CON-003 — Implementation classes are not transport contracts

Do not expose internal types merely because they are convenient.

Forbidden defaults include:

```text
serialize Domain aggregate directly
serialize EF entity directly
use provider SDK DTO as Notrelix API contract
copy internal persistence enum into external contract without compatibility policy
```

Transport contracts require deliberate public meaning.

---

# 6. Contract classes

Notrelix recognizes several contract classes.

```text
1. REST / HTTP / OpenAPI
2. Realtime logical contract
3. Integration / public event
4. Message envelope/runtime contract
5. Generated frontend/client contract
6. Provider/webhook contract
7. Persisted compatibility contract
8. Public package/export contract
```

Some changes touch more than one class.

---

# 7. REST / HTTP / OpenAPI contracts

REST contracts include:

- route identity;
- HTTP method;
- request shape;
- response shape;
- error/problem shape;
- pagination;
- filtering/sorting;
- concurrency metadata;
- idempotency input where supported;
- authentication/authorization-related transport semantics;
- deprecation/version behavior.

Exact endpoint conventions are backend-owned:

```text
backend/docs/architecture/api-and-contracts.md
```

---

# 8. Realtime contracts

Realtime contracts include:

- logical event identity;
- resource/tenant scope;
- payload semantics;
- version/sequence information where required;
- subscription identity;
- recovery expectation.

Realtime is not a second business truth.

The contract must represent committed/approved server facts that clients can reconcile.

---

# 9. Integration/public events

Integration events are durable cross-boundary facts intended for consumers outside the producing local Domain transaction.

They require stronger compatibility discipline than purely internal Domain events.

They SHOULD expose:

- stable logical identity;
- stable producer identity;
- semantic owner;
- relevant tenant/resource identity;
- payload meaning;
- event time/version semantics where needed.

Exact delivery is Platform-owned.

---

# 10. Domain events are not automatically public contracts

A Domain event represents a business fact within Domain/Application processing.

It may later produce:

- integration event;
- realtime event;
- audit/activity entry;
- post-commit work.

Do not assume the Domain event class itself is the durable external schema.

This allows Domain modeling to evolve without unnecessarily breaking consumers.

---

# 11. Message envelope contract

A message envelope may contain technical delivery metadata such as:

- message identity;
- producer identity;
- tenant scope;
- correlation/causation;
- contract/event name;
- version;
- ordering/sequence metadata;
- trace information.

Envelope mechanics are Platform-owned.

Cross-stack requirement:

> Envelope metadata must be sufficient for safe delivery, diagnosis, scoping, and compatibility without leaking business ownership into Platform.

---

# 12. Generated client contracts

Generated client artifacts are derived from a producer.

Typical conceptual flow:

```text
backend public contract / OpenAPI
        ↓
contract artifact
        ↓
frontend generator
        ↓
frontend types/client
```

The generated output is not manually authored authority.

---

# 13. Provider/webhook contracts

External providers have independently changing contracts.

Notrelix must separate:

```text
provider contract
from
Notrelix product contract
```

Provider DTO/status/event names require translation through Integrations/Infrastructure.

Do not propagate provider schema into every business context.

---

# 14. Persisted compatibility contracts

Persistence becomes a compatibility boundary during staged deployment or data migration.

Examples:

- old and new app versions sharing schema;
- event backlog containing old payloads;
- persisted polymorphic JSON;
- migration/backfill windows;
- durable integration cursor/state.

The database schema is not normally a public product API.

It can still be a deployment compatibility contract.

---

# 15. Public package/export contracts

Frontend/internal repository packages may have public exports consumed by multiple packages.

Changing those exports can be an architecture contract change even when no external user sees them.

Exact package boundary ownership remains frontend architecture.

---

# 16. SYS-CON-004 — Contract identity is logical

Contract identity MUST NOT depend only on:

- CLR class name;
- TypeScript interface name;
- namespace;
- file path.

Internal names can change.

Public logical identity should remain stable when semantics remain stable.

---

# 17. Naming stability

Do not rename a public logical event/contract merely to align with an internal refactor.

Example:

```text
internal class renamed
≠
public event identity must rename
```

A rename visible to consumers is itself a compatibility change.

---

# 18. Producer ownership

The producer owns:

- when the fact/response is valid;
- semantic meaning;
- authoritative data source;
- compatibility responsibility;
- deprecation/removal process.

Transport infrastructure does not own business meaning.

---

# 19. Consumer responsibility

Consumers own:

- tolerant/strict reading according to contract;
- version support;
- retry/idempotency behavior where applicable;
- migration before deprecation deadline;
- rejection/recovery behavior for unsupported versions.

Consumers must not infer undocumented semantics from incidental fields.

---

# 20. Semantic owner versus transport producer

Sometimes transport producer and semantic owner differ technically.

Example:

```text
Platform publishes envelope
Work Management owns business fact
```

Record both roles.

Do not conclude Platform owns Work Management semantics because it publishes bytes.

---

# 21. Scope in contracts

Cross-boundary contracts involving protected resources MUST carry or resolve sufficient scope.

Scope may include:

- Account;
- Workspace;
- resource;
- user/principal relation;
- provider connection.

The exact representation depends on the boundary.

Scope cannot be omitted when safe consumption requires it.

---

# 22. SYS-CON-005 — Tenant scope is contract semantics

A payload that can be interpreted only with tenant/workspace context must define how that scope is established.

Do not make consumers infer tenant from:

- cached global state;
- previous request;
- current UI workspace;
- unverified resource ID alone.

---

# 23. Resource identity

Resource identity must be stable enough for:

- authorization;
- client reconciliation;
- idempotency;
- event correlation;
- projections;
- audit.

Do not use presentation labels/names as resource identity.

---

# 24. Contract compatibility dimensions

Compatibility is multidimensional.

Evaluate:

```text
shape
meaning
identity
scope
ordering
lifecycle
error semantics
requiredness
default behavior
retry/idempotency
security
timing/freshness
```

A shape-compatible change can still be behaviorally breaking.

---

# 25. Compatibility classes

Use these system-level classes:

```text
Additive Compatible
Behavior-Compatible
Breaking
Parallel/New Version
Internal/Private
```

Detailed migration actions depend on contract class.

---

# 26. Additive Compatible

Definition:

> Existing consumers continue to behave correctly without semantic reinterpretation.

Examples may include:

- optional response field;
- new event field ignored by tolerant consumers;
- new enum value only if consumer contract explicitly supports unknown values.

“Additive” is not determined only by JSON syntax.

---

# 27. Behavior-Compatible

Definition:

> Shape may remain the same and semantics become more precise without invalidating valid existing consumer behavior.

This category requires care.

A “clarification” that changes previously valid behavior is breaking.

---

# 28. Breaking

Definition:

> An existing supported consumer may fail, reject, mis-authorize, corrupt state, or behave semantically incorrectly.

Breaking examples include:

- remove/rename required field;
- add required request input without compatible default;
- change enum meaning;
- change tenant scope;
- change ordering guarantees;
- change deletion/lifecycle meaning;
- change idempotency identity;
- change permission interpretation;
- reuse old event name for new fact.

---

# 29. Parallel / New Version

Use when incompatible old/new identities must coexist during migration.

Parallel versions must define:

- old/new identity;
- producer support window;
- consumer migration;
- rollout order;
- removal condition.

Do not create new version only because internal code was refactored.

---

# 30. Internal / Private

A private contract is still a contract when independently changing internal components rely on it.

Its migration may be easier because all consumers live in one repository/deployment.

Private does not mean “change without impact analysis”.

---

# 31. SYS-CON-006 — Semantic break counts as breaking

Keeping identical fields while changing meaning can be breaking.

Examples:

```text
status field now means something else
workspace scope changes
field null now means deleted instead of unknown
sequence no longer monotonic
permission result interpretation changes
```

Compatibility is behavioral, not merely structural.

---

# 32. Request compatibility

Requests are usually stricter than responses.

Adding a required request field is breaking unless:

- producer can supply a safe default;
- old behavior remains valid;
- compatibility is explicit.

Making validation stricter may also be breaking.

---

# 33. Response compatibility

Adding optional response data is often compatible if old consumers ignore unknown fields.

But semantics matter.

Example:

```text
new field changes which old field should be trusted
```

may be behavior-breaking even though shape is additive.

---

# 34. Enum compatibility

Enum evolution must define unknown-value behavior.

Possible strategies:

- versioned contract;
- tolerant string/unknown representation;
- generated union with fallback;
- deliberate break.

Do not assume every generated consumer tolerates new enum members.

---

# 35. Nullability compatibility

Changes involving:

```text
required ↔ optional
null ↔ absent
empty ↔ unknown
```

can be breaking.

Document semantic meaning, not only type annotation.

---

# 36. Error-contract compatibility

Errors are part of public behavior.

Stable aspects may include:

- status class;
- problem/error code;
- retryability meaning;
- field-validation structure;
- conflict/not-found/forbidden distinction.

Do not expose raw internal exception class names as stable consumer API.

---

# 37. Pagination/filter/sort compatibility

Query semantics are contracts.

Changing:

- default ordering;
- cursor identity;
- filter interpretation;
- null sorting;
- page stability;
- permission filtering

can break clients even when endpoint shape stays constant.

---

# 38. Concurrency compatibility

If a contract supports expected-version/concurrency semantics:

- version identity;
- conflict outcome;
- retry/read-latest behavior

are part of the contract.

Do not silently turn fail-closed concurrency into last-write-wins.

---

# 39. Idempotency compatibility

If a request supports idempotency:

- key scope;
- operation identity;
- replay behavior;
- conflict semantics;
- retention assumptions where consumer-visible

are contract concerns.

Exact server storage mechanics are backend-owned.

---

# 40. Event compatibility

Integration event compatibility must consider:

- backlog;
- retry;
- replay;
- old consumers;
- independently deployed consumers;
- persisted event payloads;
- logical identity.

Removing a field immediately after producer deploy may break queued historical messages.

---

# 41. Event replay

If replay is supported or operationally possible, ask:

```text
Can current consumer read historical contract versions?
Does replay repeat side effects safely?
Does projection rebuild understand old facts?
```

Do not design only for live delivery.

---

# 42. Realtime compatibility

Realtime consumers may remain connected across deploy transitions.

Consider:

- mixed client/server versions;
- reconnect after upgrade;
- cached old state;
- new event fields/types;
- sequence continuity.

When uncertain, refetch from authoritative query state.

---

# 43. Provider compatibility

Provider contracts can break externally.

Integrations/Infrastructure must define:

- provider version/API identity;
- webhook version;
- tolerant parsing;
- unknown fields/events;
- provider deprecation window;
- Notrelix translation.

Do not expose provider breakage directly as a product semantic change unless product behavior truly changes.

---

# 44. Persisted polymorphism

Persisted polymorphic JSON/config requires:

- discriminator identity;
- schema/version strategy;
- backward reader behavior;
- migration path;
- unknown type behavior.

Do not persist arbitrary implementation type names as long-term schema unless explicitly governed.

---

# 45. SYS-CON-007 — Generated output changes through producer

Generated contracts MUST be changed through:

```text
producer
→ generator
→ generated artifact
```

Never:

```text
edit generated output
→ pretend producer changed
```

Drift checks must fail on manual divergence.

---

# 46. Contract producer inventory

For a material contract change, identify the producer artifact.

Examples:

```text
REST
→ backend API/OpenAPI producer

frontend generated client
→ codegen producer

frontend package dependency/export
→ package/architecture manifest

integration event
→ backend public event contract source

provider webhook
→ provider adapter/integration contract
```

Do not guess producer location from generated output.

---

# 47. Consumer inventory

Breaking/semantic contract changes require known consumer inventory.

Consumer classes may include:

```text
web
mobile
marketing/public client
external integration client
background consumer
Automation
Analytics projection
provider
persisted backlog
migration tool
test fixture
```

“Same repository” does not eliminate consumer migration.

---

# 48. SYS-CON-008 — Compatibility is evaluated per consumer class

A change may be:

```text
compatible for web
breaking for mobile
irrelevant to marketing
breaking for old event consumer
```

Therefore “backward compatible” without identifying consumers is incomplete.

---

# 49. Rollout order

For independently changing producer/consumer, define safe deployment order.

Typical additive flow:

```text
producer supports old + new
→ consumer migrates
→ old support removed
```

The exact direction can differ.

Rollout order must be explicit.

---

# 50. SYS-CON-009 — Mixed-version window is designed

If producer and consumer can run different versions concurrently, define:

- which combinations are safe;
- for how long;
- what degrades;
- what must not happen.

Do not assume atomic deploy across separately deployed clients/services.

---

# 51. Expand-contract migration

Common contract migration:

```text
expand
→ support old + new
→ migrate consumers/data
→ switch authority/usage
→ remove old
```

Use for:

- API;
- events;
- persistence;
- generated contracts;
- provider mappings

where appropriate.

---

# 52. Deprecation

Deprecation must define:

- deprecated identity/behavior;
- replacement;
- affected consumers;
- support window/trigger;
- telemetry/evidence if needed;
- removal condition.

Do not mark something deprecated with no migration path.

---

# 53. Removal condition

A contract may be removed when:

- all required consumers migrated;
- backlog/replay implications handled;
- old persisted data migrated/read-compatible;
- generated clients updated;
- routes/event names no longer used;
- tests/gates no longer require compatibility.

“New code exists” is not removal proof.

---

# 54. SYS-CON-010 — Removal follows consumer proof

Do not remove old contract support based only on producer confidence.

Require evidence that supported consumers no longer depend on it.

---

# 55. Contract and data migration

Contract migration may require data migration.

Examples:

- rename semantic identity persisted in records;
- enum lifecycle change;
- payload schema migration;
- provider mapping identity change.

Route to:

```text
docs/delivery/change-impact-and-migration.md
backend/docs/operations/migrations-and-data-change.md
```

---

# 56. Contract and authorization

Security semantics are part of contract behavior.

Changes to:

- required scope;
- action permission;
- public/share access;
- tenant resolution

can break consumers even when the DTO is identical.

---

# 57. SYS-CON-011 — Security cannot be weakened for compatibility

Backward compatibility does not justify preserving insecure behavior indefinitely.

Security fixes may intentionally break behavior.

They still require:

- explicit impact analysis;
- migration where possible;
- communication;
- tests;
- decision/exception handling if architecture changes.

---

# 58. Contract and tenant scope

Moving an endpoint/event from:

```text
workspace-scoped
→ account-scoped
```

is a semantic contract change.

It may affect:

- authorization;
- cache keys;
- event routing;
- client subscriptions;
- analytics;
- provider mappings.

Treat scope as part of contract identity.

---

# 59. Contract and product ownership

Moving a fact between bounded contexts is usually a major contract migration.

Even if JSON stays the same, change in semantic owner can affect:

- lifecycle;
- authorization;
- events;
- data;
- consistency;
- service extraction.

Use system ADR + migration policy.

---

# 60. Contract and realtime

REST and realtime representations of the same business fact must remain coherent.

Realtime may be:

- notification;
- delta;
- invalidation hint.

It must not invent a conflicting business interpretation.

---

# 61. Contract and frontend cache

Frontend cache identity must reflect contract scope/identity sufficiently to avoid:

- tenant collision;
- resource collision;
- stale old-version response overwriting new state.

Detailed behavior remains frontend-owned.

---

# 62. Contract and service extraction

Before extracting a capability:

- public/context contracts must be explicit;
- internal direct calls that become network calls must be inventoried;
- compatibility and failure semantics must be defined;
- data ownership must be clear.

Extraction should change transport/deployment more than semantics.

---

# 63. Contract and internal synchronous calls

Inside the modular monolith, Application-to-Application/context calls may be in-process.

If they represent a context boundary, design them as intentional contracts rather than direct persistence access.

This supports later extraction.

---

# 64. Contract and events

An integration event should answer:

```text
What fact happened?
Who owns it?
What stable identity exists?
What scope applies?
What can consumers rely on?
What must they not infer?
```

Avoid “entity changed” events with ambiguous semantics when consumers need stable product facts.

---

# 65. Event granularity

Too coarse:

```text
SomethingChanged
```

can force consumers to re-query unnecessarily or infer meaning.

Too fine:

```text
every internal field mutation as public event
```

couples consumers to implementation.

Choose product-relevant facts.

---

# 66. Event naming

Names should reflect stable business fact identity.

Do not tie public logical name to current CLR namespace/class formatting.

Version when semantic identity changes incompatibly.

---

# 67. Event payload minimization

Expose enough data for safe consumption.

Do not expose entire aggregate snapshots by default.

Consider:

- security;
- payload size;
- future compatibility;
- source ownership;
- consumer need.

Consumers needing richer state may query the source owner.

---

# 68. Sensitive data

Contracts must minimize sensitive data.

Especially review:

- event payloads;
- logs;
- client contracts;
- public API;
- provider webhooks;
- analytics exports.

Do not add sensitive fields “for convenience”.

---

# 69. Contract observability

Operational evidence should identify:

- contract identity/version;
- producer;
- consumer where relevant;
- correlation/message/request ID;
- tenant/resource scope where safe;
- compatibility failure.

Do not log sensitive payloads by default.

---

# 70. Failure semantics

Each contract must define relevant failures.

Examples:

```text
validation rejection
unauthorized/forbidden
not found
conflict
unsupported version
duplicate/idempotent replay
retryable provider failure
terminal provider failure
gap/recovery
```

Consumer behavior depends on semantic classification.

---

# 71. Unknown fields

For evolvable object contracts, consumers SHOULD tolerate unknown additive fields unless strict validation is intentionally part of the contract.

Do not assume this for every serializer/client.

Test actual generated/runtime behavior.

---

# 72. Unknown event types

Unknown event behavior must be explicit.

Possible choices:

- ignore safely;
- dead-letter;
- fail subscription and refetch;
- version-negotiated rejection.

Choose according to consumer safety.

---

# 73. Unknown enum values

Do not accidentally turn new enum member into client crash.

If evolution is expected, design tolerant representation.

If every value has strict exhaustive meaning, versioning may be safer.

---

# 74. Contract negotiation

Notrelix does not require generic runtime contract negotiation everywhere.

Use the simplest compatibility mechanism that satisfies the boundary:

- additive compatibility;
- explicit version;
- endpoint version;
- event version;
- provider API version;
- generated client update.

Avoid framework-heavy negotiation without product need.

---

# 75. Version identifiers

Version identifiers must represent meaningful compatibility identity.

Avoid version increments for internal refactors.

A version is not a release number unless the contract explicitly uses release versioning.

---

# 76. REST versioning

Backend API owns exact implementation.

System rules:

- incompatible public behavior requires explicit version/migration;
- prefer additive response evolution where safe;
- requests require stricter compatibility analysis;
- deprecation/removal must be consumer-aware.

---

# 77. Event versioning

Use explicit strategy when payload/semantics break.

Possible strategies:

- new logical event name/version;
- version field with parallel readers;
- translation adapter.

Do not mutate historical backlog meaning in place.

---

# 78. Realtime versioning

Realtime versioning may reuse underlying event version or define a client-specific logical contract.

Do not expose backend internal event class blindly.

Client upgrade cadence matters, especially mobile.

---

# 79. Mobile compatibility

Mobile clients can remain deployed long after backend/web rollout.

Therefore public mobile-consumed contracts need realistic backward-compatibility windows.

Do not assume same-day consumer migration.

---

# 80. Web compatibility

Web often updates faster, but mixed sessions/tabs/caches can still exist.

Design safe transition rather than relying on instant refresh.

---

# 81. External-client compatibility

If external consumers exist, they require stronger deprecation evidence and communication than first-party code.

Do not assume repository-wide search finds all consumers.

---

# 82. Provider webhook evolution

Provider webhooks may deliver historical/old versions.

Consumer parsing should identify:

- provider event identity;
- version;
- connection/account mapping;
- dedup identity;
- unsupported behavior.

Translate to Notrelix-owned facts before cross-context use.

---

# 83. SYS-CON-012 — Provider contracts never become Notrelix ubiquitous language by default

Provider vocabulary is external.

Product contexts use Notrelix vocabulary.

Integrations owns translation.

---

# 84. Contract tests

Use tests appropriate to boundary.

Examples:

```text
API
→ endpoint/API tests + OpenAPI drift

generated client
→ codegen drift/typecheck/consumer tests

event
→ serialization/consumer compatibility + integration

realtime
→ protocol/reconciliation tests

provider
→ adapter/webhook contract tests
```

---

# 85. Consumer-driven evidence

Where valuable, contract tests may exercise actual consumers.

Do not overfit to a heavy consumer-driven-contract framework if simpler repo-owned proof is sufficient.

The requirement is evidence, not a specific tool.

---

# 86. Golden/snapshot contracts

Snapshots can detect shape drift.

They do not prove semantic compatibility.

Use them with explicit compatibility review.

---

# 87. OpenAPI as evidence

OpenAPI is exact public HTTP contract evidence where the API producer emits it.

It is not the sole product semantic owner.

Canonical product/API docs explain meaning.

Generated OpenAPI proves shape.

---

# 88. Generated TypeScript as evidence

Generated frontend types prove current producer-to-client shape.

They do not prove:

- authorization;
- lifecycle meaning;
- error semantics beyond represented schema;
- realtime convergence.

Do not confuse type safety with semantic safety.

---

# 89. Contract metadata

Where a durable runtime contract requires metadata, prefer explicit fields/envelope over hidden process-local assumptions.

Examples:

- tenant;
- correlation;
- event identity;
- expected version;
- idempotency key.

---

# 90. Contract registry

Notrelix does not require one giant manually maintained catalog of every endpoint/event.

Exact inventories should be generated or source-derived.

Canonical docs own contract classes and policies.

Generated artifacts own exact shape inventories.

---

# 91. Public contract change workflow

For a material change:

```text
1. identify semantic owner
2. identify producer
3. inventory consumers
4. classify compatibility
5. choose migration/version strategy
6. define mixed-version window
7. update producer
8. regenerate artifacts
9. migrate consumers
10. run contract/gate evidence
11. remove old support only after proof
12. update canonical docs/ADR if semantics changed
```

---

# 92. Change checklist

```text
[ ] producer known
[ ] semantic owner known
[ ] consumer classes known
[ ] scope/security impact reviewed
[ ] shape compatibility classified
[ ] semantic compatibility classified
[ ] old/new rollout order defined
[ ] backlog/replay considered
[ ] persistence compatibility considered
[ ] mobile/external client lag considered
[ ] codegen artifacts regenerated
[ ] tests/gates identified
[ ] deprecation/removal condition defined
```

---

# 93. Stop conditions

Stop rather than guess if:

- public event has no semantic owner;
- consumer inventory is materially unknown for a breaking change;
- old mobile/external clients may exist but compatibility plan is absent;
- tenant scope changes without authorization analysis;
- generated producer is unknown;
- provider and product semantics conflict;
- event backlog compatibility is unclear;
- new meaning reuses old logical identity ambiguously.

Use decision/migration governance.

---

# 94. Related canonical owners

```text
docs/architecture/system-overview.md
docs/architecture/bounded-context-map.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md

backend/docs/architecture/api-and-contracts.md
backend/docs/architecture/platform-and-messaging.md

frontend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md

docs/delivery/change-impact-and-migration.md
```

---

# 95. Final contract rule

A contract boundary is healthy when future implementation can change while consumers continue to understand the same stable meaning.

For every cross-boundary contract, Notrelix should be able to answer:

```text
Who owns the fact?
Who produces it?
Who consumes it?
What identity/scope does it carry?
What behavior is guaranteed?
What is explicitly not guaranteed?
What counts as breaking?
How can old/new versions coexist?
How is migration proven?
What source/generated evidence represents the current contract?
```

If those answers are missing, the boundary is not yet stable enough to treat as a durable contract.
