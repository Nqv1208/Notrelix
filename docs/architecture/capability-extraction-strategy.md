---
document_id: SYS-CAPABILITY-EXTRACTION
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
  - backend
  - frontend
  - bounded-contexts
  - deployment
  - data-ownership
  - public-contracts
  - operations
evidence:
  - RULE.md
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/backend.slnx
  - backend/src/
  - backend/tests/Notrelix.Architecture.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/docs/architecture/architecture-change-policy.md
  - docker-compose.dev.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
review_on:
  - bounded-context-owner-change
  - service-extraction-proposal
  - backend-production-project-change
  - deployment-topology-change
  - database-ownership-change
  - cross-context-contract-change
  - operational-scaling-change
  - reliability-isolation-change
  - security-boundary-change
---

# Capability Extraction Strategy

> **Notrelix designs bounded contexts so they can be extracted later without designing the whole system as distributed services today.**
>
> Extraction is an operational architecture decision.
>
> It is not the mechanism used to discover business boundaries.

This document is the canonical system owner for:

- modular-monolith extraction strategy;
- service-extraction admission;
- readiness criteria;
- contract/data/runtime prerequisites;
- staged extraction;
- cutover and rollback/roll-forward;
- anti-premature-microservice rules;
- post-extraction ownership invariants.

It does not declare that any context must become a service today.

That requires current evidence and an accepted decision.

---

# 1. Current architecture position

The backend deployment default is:

```text
modular monolith
```

Bounded contexts remain explicit inside one backend deployment.

The target is:

```text
semantic modularity now
+
operational extraction only when justified
```

not:

```text
one service per bounded context
```

---

# 2. Why modular monolith is deliberate

One backend deployment currently avoids unnecessary:

- network failure;
- distributed tracing;
- deployment-version coordination;
- distributed consistency;
- service discovery;
- duplicated security plumbing;
- cross-service test complexity;
- developer-environment complexity.

Those costs are accepted only when extraction creates meaningful independent value.

---

# 3. SYS-EXT-001 — Bounded context is an extraction seam, not an extraction order

A bounded context should be semantically coherent enough to become independently deployable later.

It does **not** imply:

```text
Work Management
→ WorkManagement.Service now

Billing
→ Billing.Service now
```

Semantic boundary and deployment boundary are related but distinct.

---

# 4. SYS-EXT-002 — Extraction preserves product semantics

After extraction, these should remain stable:

```text
ubiquitous language
business owner
lifecycle
invariants
resource identity
public/context contract meaning
```

Extraction may change:

```text
transport
deployment
physical data store
operational ownership
failure model
scaling
```

If product meaning must be reinvented just to extract, the pre-extraction boundary is not mature.

---

# 5. Legitimate extraction motivations

Valid motivations may include independent need for:

```text
scaling
deployment cadence
reliability isolation
security/trust isolation
data residency/sovereignty
runtime/technology specialization
provider/network isolation
operational ownership
availability/SLO independence
cost isolation
```

The motivation should be operationally concrete.

---

# 6. Weak extraction motivations

These alone are insufficient:

```text
microservices look enterprise
folder is large
one table has many rows
team wants a service
bounded context exists
framework supports service templates
service count looks mature
```

---

# 7. SYS-EXT-003 — Extraction value must exceed recurring distributed cost

Service extraction introduces recurring cost in:

```text
networking
deployment
observability
security
data consistency
incident response
contract compatibility
testing
developer workflow
infrastructure
```

Extraction is justified when independent operational value exceeds these costs.

---

# 8. Extraction is consequential architecture change

Even with unchanged product semantics, extraction introduces:

- network latency/failure;
- partial availability;
- deployment skew;
- contract compatibility windows;
- data migration;
- operational ownership;
- new monitoring requirements.

Therefore it requires architecture governance.

---

# 9. ADR requirement

A service extraction requires a consequential system decision:

```text
SYS-ADR-*
```

The decision should state:

- motivation;
- alternatives;
- expected independent value;
- contract/data strategy;
- rollout;
- failure/recovery;
- ownership.

Do not begin with file movement.

---

# 10. Readiness dimensions

Evaluate:

```text
1. Semantic readiness
2. Contract readiness
3. Data readiness
4. Consistency readiness
5. Runtime readiness
6. Security/tenant readiness
7. Observability readiness
8. Testing readiness
9. Deployment readiness
10. Operational/team ownership readiness
```

A critical blocker in one dimension can stop extraction even if others are strong.

---

# 11. Semantic readiness

The capability needs:

- explicit product/context owner;
- stable vocabulary;
- clear lifecycle;
- explicit invariants;
- known resource/aggregate boundaries;
- controlled dependencies.

Source-folder presence is evidence, not ownership authority.

---

# 12. SYS-EXT-004 — No extraction with ambiguous fact ownership

If two contexts both appear to mutate/own the same fact:

```text
STOP
```

Resolve ownership before distributing the system.

Distributed deployment amplifies semantic ambiguity.

---

# 13. Contract readiness

Inventory:

```text
incoming synchronous calls
outgoing synchronous calls
integration/public events
realtime contracts
provider calls
background jobs
shared implementation types
database joins
public API routes
```

Every cross-boundary interaction must have intentional semantics.

---

# 14. Incoming operations

For each incoming operation define:

```text
request/response
auth/principal
tenant/resource scope
failure semantics
timeout
retry/idempotency
compatibility
```

An in-process method call becoming HTTP/RPC must not carry hidden assumptions.

---

# 15. Outgoing dependencies

Classify each dependency as:

```text
synchronous request
read projection
integration event
provider call
cache
shared technical primitive
```

Do not replace every old dependency with REST automatically.

Consistency requirements choose the interaction model.

---

# 16. SYS-EXT-005 — Extraction transport follows the semantic boundary

Preferred:

```text
existing context/Application contract
→ transport adapter
```

Not:

```text
move classes
→ expose every internal method as endpoint
```

A service exposes business use cases/facts, not implementation surface.

---

# 17. Mixed-version readiness

During extraction, old/new components may coexist.

Define compatibility among:

- old monolith caller/new service;
- new service/old consumers;
- old event backlog/new consumer;
- rollback path;
- public web/mobile/external clients where affected.

---

# 18. Data readiness

Inventory owned/related data:

```text
tables
owned rows
shared tables
foreign references
read joins
foreign writes
migrations
indexes
RLS
caches
search indexes
projections
```

Physical placement can still be shared.

Semantic ownership cannot be ambiguous.

---

# 19. SYS-EXT-006 — Data moves after ownership is clear

Do not decide service ownership from table convenience.

Correct order:

```text
business fact owner
→ logical data owner
→ physical migration
```

---

# 20. Shared-database extraction stages

Possible controlled stages:

```text
A. service boundary introduced; DB still shared
B. service becomes sole logical writer of owned schema/tables
C. consumers stop direct reads/writes
D. physical data moves if justified
```

Shared database may be transitional.

It must not preserve foreign writes indefinitely.

---

# 21. Shared-database risk

A shared DB after service extraction can preserve:

- transaction coupling;
- coordinated schema deploy;
- hidden joins;
- bypassed service contracts;
- RLS/session assumptions.

If retained, ownership and removal conditions must be explicit.

---

# 22. SYS-EXT-007 — No foreign direct writes after authority cutover

After service becomes authoritative owner:

```text
all external mutations
→ service-owned contract
```

No backdoor direct table writes without an approved temporary exception.

---

# 23. Read strategy

Consumers may use:

```text
synchronous query
local projection
cache
replicated read model
Analytics/reporting projection
```

Choose based on:

- freshness;
- latency;
- availability;
- query shape;
- security.

Avoid chatty N+1 distributed read graphs.

---

# 24. Cross-context joins

Old shared-DB joins must be classified:

```text
business-critical live read
reporting/read model
optimization
hidden ownership violation
```

Possible replacements:

- API composition;
- projection;
- replicated read model;
- Analytics/reporting.

---

# 25. Data migration patterns

Possible strategies:

```text
backfill + cutover
dual read
dual write with one authority
CDC/event replication
copy + incremental catch-up
```

Selection depends on data scale, uptime, and consistency needs.

---

# 26. Consistency readiness

Identify monolith assumptions that extraction changes:

- shared transaction;
- immediate read-after-write;
- local synchronous call;
- FK;
- lock;
- local version token.

Each must be intentionally replaced or preserved.

---

# 27. SYS-EXT-008 — Cross-service atomicity is not assumed

Default cross-service model:

```text
local transaction
+
durable event
+
idempotent consumer
+
process manager/compensation when required
```

Do not recreate a monolith transaction as a fragile chain of network calls.

---

# 28. Invariant review

For every current cross-context transaction ask:

```text
Is this truly one invariant?
Can ownership move to make it local?
Can temporary inconsistency be tolerated?
Does process-manager workflow fit?
Does this coupling mean extraction is wrong?
```

Strong coupling may be evidence not to extract yet.

---

# 29. Runtime readiness

An extracted service needs:

```text
host/process
config
health
ingress/routing
outbound policy
deployment manifest
shutdown/drain behavior
background worker lifecycle
```

Business code without runtime ownership is not an extracted service.

---

# 30. Runtime dependencies

Identify ownership of:

```text
database
cache
messaging
provider credentials
object storage
scheduled jobs
```

Do not clone all monolith infrastructure by default.

---

# 31. Messaging readiness

If local interaction becomes async, define:

```text
message identity
consumer identity
tenant scope
ordering
idempotency
retry
dead-letter
replay
compatibility
```

Platform can provide mechanism.

Product/context owns fact semantics.

---

# 32. Realtime readiness

When state owner moves, define one logical realtime publication path.

Possible:

```text
service → logical realtime fact
```

or:

```text
service integration fact
→ realtime gateway/mapper
```

Do not allow monolith and new service to publish duplicate authoritative realtime facts after cutover.

---

# 33. API routing readiness

If public routes move:

- keep public semantics stable where possible;
- gateway routes internally;
- auth context propagates;
- Application/resource authorization remains authoritative;
- OpenAPI producer ownership updates.

Internal topology change should not force public break.

---

# 34. SYS-EXT-009 — Public API stability is independent of service topology

Clients should not need to know whether implementation is:

```text
monolith
service
gateway-composed
```

unless the product/API contract intentionally changes.

---

# 35. Security readiness

Extraction introduces a new trust boundary.

Review:

```text
service/workload identity
original user identity
tenant/resource scope
authorization
RLS
secrets
network policy
audit
provider credentials
administrative access
```

---

# 36. SYS-EXT-010 — Tenant context survives extraction

Tenant/account/workspace/resource scope must propagate through:

```text
gateway
service call
message
background worker
database session
cache
search
realtime
```

Network separation must never weaken tenant isolation.

---

# 37. Service-to-service identity

Internal calls should establish:

- workload/service identity;
- original principal when required;
- tenant/resource scope;
- correlation.

Internal network location is not authentication by itself.

---

# 38. Authorization after extraction

Possible models:

```text
service evaluates policy with trusted facts
service queries centralized policy capability
service consumes versioned authorization projection
```

Whatever model is chosen, do not move all security enforcement to gateway only.

---

# 39. RLS after extraction

If data moves, tenant isolation mechanics must be preserved/adapted.

Production-role integration tests should prove the new persistence graph.

Extraction is not a reason to remove defense in depth.

---

# 40. Secrets isolation

An extracted service should receive only the secrets it needs.

Do not copy a monolith-wide secret bundle to every service.

---

# 41. Observability readiness

Before traffic cutover, the service needs:

```text
logs
metrics
traces
correlation
dependency health
latency/error evidence
queue lag if relevant
```

Cross-service debugging without correlation is an operational regression.

---

# 42. SYS-EXT-011 — Operational evidence exists before cutover

Operators must be able to answer:

```text
Is service healthy?
Is traffic reaching it?
Are errors/latency acceptable?
Is data consistent?
Are queues/backlogs healthy?
Can one request be traced end-to-end?
```

Do not cut over blind.

---

# 43. Reliability readiness

If extraction is motivated by reliability isolation, define:

- availability target;
- degradation behavior;
- dependency fallback;
- failure blast radius.

A new service that becomes mandatory to every request may reduce overall availability.

---

# 44. Blast-radius test

Ask:

> If this service fails, what still works?

If the answer is “almost nothing”, the extraction creates a central dependency.

That may be justified for critical trust services, but it must be deliberate.

---

# 45. Testing readiness

Before extraction, existing boundaries should already have tests for:

- invariants;
- use-case contracts;
- cross-context contracts;
- event compatibility;
- tenant isolation;
- idempotency/retry.

After extraction add:

- network failure;
- service contract;
- deployment graph;
- data cutover;
- mixed-version tests.

---

# 46. Architecture tests

Architecture tests should enforce:

```text
no old direct dependency
no foreign direct persistence access
contract-only dependencies
service/project/package boundary
approved shared abstractions
```

Do not rely on diagrams as enforcement.

---

# 47. Integration tests

Production-like flows should cover:

```text
caller
→ gateway/service
→ transaction
→ outbox/event
→ consumer
→ downstream state
```

Use real infrastructure where mocks cannot prove correctness.

---

# 48. Contract tests

Separately deployable units require stronger compatibility evidence.

Use appropriate:

- OpenAPI drift;
- serialization/version tests;
- generated clients;
- consumer compatibility.

The policy requires proof, not a fashionable framework.

---

# 49. Migration tests

Data migration evidence should cover:

- real/representative existing data;
- backfill;
- live writes during migration;
- cutover;
- uniqueness/FK/RLS;
- rollback/roll-forward;
- idempotency.

Empty-database success is insufficient.

---

# 50. Deployment readiness

Define:

```text
build artifact
config
secrets
DB migration
routing
health
rollout
recovery
observability
```

Service code without repeatable deployment is incomplete.

---

# 51. Operational ownership

Someone must own:

- releases;
- incidents/on-call;
- runtime cost;
- upgrades;
- security;
- migrations;
- compatibility.

An independent service without operational ownership is organizational debt.

---

# 52. SYS-EXT-012 — Team topology does not redefine product semantics

A team may own a service.

Team changes do not automatically change bounded-context meaning.

Organization supports semantic ownership; it does not create it.

---

# 53. Readiness scores

A score can summarize readiness.

It cannot override blockers.

Example:

```text
90/100 readiness
+
ambiguous data ownership
=
not ready
```

Critical constraints are fail-closed.

---

# 54. Hard blockers

Extraction must stop when:

- semantic ownership ambiguous;
- foreign direct writes unresolved;
- data owner unclear;
- cross-boundary atomic invariant unresolved;
- tenant/service authorization model missing;
- incoming/outgoing contracts unknown;
- cutover/recovery strategy absent;
- no operator owner;
- no observability;
- critical behavior lacks executable proof.

---

# 55. Preparation debt

Some non-blocking debt may remain during early preparation:

- dashboard polish;
- optional optimization;
- non-critical dev tooling.

But traffic cutover must satisfy operational minimums.

---

# 56. Extraction phases

Recommended responsibility sequence:

```text
0 Decision / baseline
1 Boundary hardening
2 Contract isolation
3 Data-ownership enforcement
4 Service host/runtime
5 Shadow/dual path where useful
6 Data migration/catch-up
7 Traffic cutover
8 Old-path removal
9 Certification
```

Not every extraction uses every technique.

---

# 57. Phase 0 — Decision / baseline

Produce:

```text
accepted SYS-ADR
owner
motivation
current dependency/data baseline
success criteria
migration plan
recovery strategy
```

Do not start by creating a service folder.

---

# 58. Phase 1 — Boundary hardening

Inside the monolith first:

- remove foreign repositories;
- route mutations through Application contracts;
- separate internal/public events;
- tighten architecture rules;
- add tests.

---

# 59. SYS-EXT-013 — Make the monolith modular before making it distributed

Preferred:

```text
modular in-process boundary
→ tested contract
→ transport/runtime extraction
```

Forbidden:

```text
tangled code
→ copy into service
→ hope network creates modularity
```

---

# 60. Phase 2 — Contract isolation

Confirm/create:

- inbound business operations;
- outbound dependencies;
- public/integration events;
- API/realtime contracts;
- versioning.

Consumers stop depending on implementation types.

---

# 61. Phase 3 — Data ownership enforcement

Before physical move:

- stop foreign writes;
- identify read joins;
- define projections;
- isolate migration ownership;
- add checks.

Logical ownership comes first.

---

# 62. Phase 4 — Service runtime

Add:

```text
host
DI/composition
health
config
secrets
telemetry
DB
messaging
deployment artifact
```

Keep product semantics stable.

---

# 63. Phase 5 — Shadow/dual path

Optional:

- shadow reads;
- mirror no-side-effect requests;
- compare results;
- dual-read;
- event mirror.

Never dual-execute irreversible product/provider effects without proven idempotency.

---

# 64. Shadow reads

Use to compare:

- result correctness;
- latency;
- data parity.

Do not leak protected data into comparison logs.

---

# 65. Dual write

If necessary, define one authority per phase.

Example:

```text
Phase A:
old authoritative
new mirrored

Phase B:
new authoritative
old compatibility mirror
```

Never “both authoritative”.

---

# 66. Phase 6 — Data migration

Typical:

```text
initial backfill
→ incremental catch-up
→ parity verification
→ write cutover
→ read cutover
```

Tenant-safe verification is mandatory.

---

# 67. Data parity

Compare business meaning, not only row count.

Check:

- missing IDs;
- lifecycle;
- versions;
- tenant scope;
- uniqueness;
- projections.

---

# 68. Phase 7 — Traffic cutover

Risk-based approaches:

```text
internal tenants
cohort
percentage
feature flag
endpoint route
```

Observe:

- business success;
- errors;
- latency;
- queue lag;
- data consistency.

---

# 69. SYS-EXT-014 — Cutover remains recoverable until old-path removal

Before removal, define whether recovery is:

```text
rollback
or
roll-forward
```

A routing toggle is not real rollback if data cannot be reconciled.

---

# 70. Roll-forward

Data migrations often make roll-forward safer.

Define the point after which rollback is no longer safe.

Do not imply infinite reversibility.

---

# 71. Phase 8 — Remove old path

Remove:

- old direct calls;
- old foreign writes;
- duplicate jobs;
- compatibility adapters;
- dual write;
- migration flags;
- exceptions;
- old route.

Extraction is incomplete while backdoors remain.

---

# 72. Phase 9 — Certification

Verify:

```text
no foreign access
contracts stable
data consistent
security green
observability green
deployment repeatable
tests/CI green
old path gone
docs updated
```

Certification is point-in-time evidence, not permanent architecture status.

---

# 73. New project versus service

A new backend project does not automatically mean a new service.

Project boundary:

```text
source/technical responsibility
```

Service boundary:

```text
independent runtime/deployment
```

Do not conflate them.

---

# 74. Internal service layering

An extracted service may use layered architecture internally.

Do not require every service to clone the monolith’s exact five-project topology.

Structure according to service complexity while preserving semantic/contract boundaries.

---

# 75. SYS-EXT-015 — Do not clone monolith structure by symmetry

Avoid mandatory:

```text
Service.Domain
Service.Application
Service.Infrastructure
Service.Platform
Service.API
```

for every service.

Only create responsibility boundaries that the service actually needs.

---

# 76. Shared Platform relationship

Messaging/delivery Platform is technical infrastructure.

It may remain shared across extracted business services.

Platform is not a business bounded context.

---

# 77. Frontend impact

Extraction should normally remain transparent to frontend when public contracts stay stable.

Frontend changes only when extraction changes product-visible:

- latency;
- async/pending state;
- contract version;
- realtime source;
- rollout behavior.

Do not expose internal service names into UI architecture by default.

---

# 78. Mobile impact

Mobile clients may lag deployments.

Keep gateway/public contracts backward-compatible through the required window.

---

# 79. Realtime producer movement

When producer moves:

- keep logical event identity if semantics unchanged;
- prevent monolith + service duplicate publication;
- preserve/redefine sequence continuity intentionally;
- keep subscription security.

---

# 80. Background jobs

Jobs whose business semantics belong to the context should move with the owner.

Generic scheduler/runtime mechanisms may remain shared.

Never run both old/new job after cutover.

---

# 81. Scheduled Automation

Moving Automation runtime must preserve stable execution identity to prevent duplicate scheduled executions.

---

# 82. Provider-heavy capabilities

Provider isolation can justify extraction because of:

- rate limit;
- credential boundary;
- external failure;
- specialized workers.

Product Integrations semantics still remain independent from transport mechanics.

---

# 83. Billing extraction

Billing can become a candidate when commercial/provider/compliance independence justifies it.

But availability strategy for entitlement checks must be explicit.

Do not create a synchronous hard dependency in every request without fail behavior.

---

# 84. Identity extraction

Identity is a critical trust service.

Review carefully:

- session/token authority;
- revocation;
- availability;
- service identity;
- emergency/degraded behavior.

It is not ordinary CRUD extraction.

---

# 85. Governance extraction

A central policy service can become a system-wide dependency.

Review:

```text
latency
availability
policy cache/version
fail-open/closed
tenant isolation
```

Do not make every product action depend on a fragile remote call casually.

---

# 86. Work Management extraction

Before extraction ensure:

- Board/Field/Item/View ownership clear;
- Documents/Collaboration use stable refs/contracts;
- Automation consumes stable facts;
- dynamic storage/query model owned;
- realtime ownership clear;
- cross-service reads are not chatty.

---

# 87. Documents extraction

Before extraction ensure:

- Page/Block ownership;
- Work Management relation contract;
- Collaboration target model;
- attachment/storage boundary;
- search/index;
- editor/realtime consistency.

---

# 88. Collaboration extraction

Collaboration references many target contexts.

Avoid requiring live synchronous fetch from every target for every comment/activity operation.

Use stable resource identity, authorization contract, and projections where justified.

---

# 89. Analytics extraction

Analytics is naturally projection-oriented and may benefit from workload isolation.

Ensure:

- source events/contracts stable;
- freshness explicit;
- rebuild/backfill possible;
- Analytics never becomes source mutation owner.

---

# 90. Search extraction

A dedicated Search service may exist operationally while Search remains a technical/supporting capability.

Deployment unit classification does not imply business bounded-context classification.

---

# 91. SYS-EXT-016 — Technical service does not imply business context

Examples:

```text
Search service
Realtime gateway
Notification delivery service
Media service
```

can be technical deployables without becoming product bounded contexts.

---

# 92. Documentation after extraction

Update affected canonical owners:

```text
system-overview
contract-boundaries
data-ownership-and-consistency
events/realtime boundary
deployment-runtime
operations
backend/frontend project docs
topic-authority-map if documentation owner changes
```

Update bounded-context map only if **semantic ownership** changes.

---

# 93. Shared-library debt

Shared libraries containing business semantics from several contexts can block extraction.

Before extraction:

```text
classify each type
→ move semantics to owner
→ retain only genuinely shared stable primitives
```

Do not deploy a “shared domain service” as workaround.

---

# 94. Shared persistence library

Do not let consumers share service persistence implementation after ownership cutover.

Shared model/migration packages can exist only as bounded transition with removal plan.

---

# 95. Gateway responsibility

Gateway may own:

```text
routing
TLS
auth integration
rate limiting
version routing
```

It must not become a giant cross-context business orchestration layer.

---

# 96. Service discovery

Use the simplest mechanism supported by deployment platform.

Logical service identity matters more than discovery product choice.

---

# 97. Network retry

Retrying a mutation can duplicate business effect.

Use stable operation identity/idempotency when retry is permitted.

Do not apply generic retry blindly to every mutation.

---

# 98. Timeout

Every synchronous remote dependency needs bounded timeout.

Timeout may mean unknown outcome, not guaranteed failure.

---

# 99. Degradation / circuit behavior

If dependency fails, define product fallback:

```text
reject
stale projection
queue for later
disable optional feature
```

A circuit-breaker library without product semantics is incomplete.

---

# 100. Availability composition

More synchronous dependencies generally reduce overall success probability.

Avoid chatty mandatory service chains.

Extraction can improve or worsen reliability depending on dependency shape.

---

# 101. Event-driven extraction

Events reduce synchronous coupling but introduce:

- lag;
- duplicate;
- order;
- replay;
- backlog;
- operations.

Use when eventual consistency fits the product.

---

# 102. Contract-first extraction

Before service host:

```text
inbound use cases
outbound dependencies
auth context
data ownership
failure semantics
public/integration events
```

must be known.

Transport comes second.

---

# 103. Database-per-service

Database-per-service is not a slogan that must be achieved on day one.

Long-term independent service should control authoritative persistence.

Physical separation may be staged.

Invariant:

```text
only owner writes owned data
```

---

# 104. Schema ownership during transition

If DB remains shared:

- table/schema owner explicit;
- foreign writes forbidden;
- foreign reads tracked;
- migration ownership explicit;
- removal path documented.

---

# 105. Data replication

Replica/projection is derived.

Define:

```text
source
replication
lag
delete
rebuild
```

Never let replica become silently writable.

---

# 106. Stable IDs

IDs should survive physical database/service movement.

Avoid external consumers depending on local DB implementation identity.

---

# 107. Correlation identity

Cross-service workflow uses stable operation/correlation identity.

Local DB transaction ID is not a cross-service correlation contract.

---

# 108. Deployment sequence

Typical pattern:

```text
deploy compatible service
→ shadow/small traffic
→ migrate callers/data
→ full cutover
→ remove old
```

Exact sequencing belongs to the extraction plan.

---

# 109. Backward compatibility

Retain old support until:

- callers migrated;
- backlog handled;
- mobile/external compatibility covered;
- recovery boundary intentionally crossed.

---

# 110. Recovery decision

Ask:

```text
Can routing move back?
Can old code read new data?
Can new writes be reconciled?
Have irreversible effects happened?
Can messages replay safely?
```

If not, plan roll-forward instead of pretending rollback is safe.

---

# 111. Feature flags

Flags can support cutover.

Every flag needs:

- owner;
- scope;
- default;
- cleanup condition.

No permanent split architecture.

---

# 112. Architecture exceptions

Temporary migration violations require explicit exception.

Examples:

- shared DB foreign read;
- compatibility adapter;
- dual write;
- old call path.

No implicit migration exception.

---

# 113. Extraction metrics

Useful evidence:

```text
traffic
latency
CPU/memory
queue lag
deployment frequency
incident count
scaling asymmetry
data volume
provider errors
release blocking
```

Metrics support, but do not replace, semantic architecture analysis.

---

# 114. Cost model

Evaluate:

- compute;
- network;
- observability;
- CI/build;
- storage duplication;
- on-call;
- developer environment;
- deployment automation.

Distributed architecture has permanent operating cost.

---

# 115. Developer experience

Define:

- local start;
- dependencies;
- test mode;
- contract generation;
- seed data;
- integration environment.

Routine development should not require undocumented manual choreography.

---

# 116. Repository topology

Independent service may remain in the same monorepo.

Repository boundary is a separate decision from deployment boundary.

---

# 117. CI topology

Service should have independent:

- build;
- tests;
- contracts;
- artifact/container;
- deployment evidence.

Repository-wide quality/docs governance still applies.

---

# 118. Release-independence test

A service is meaningfully independent when it can deploy without lockstep release of unrelated components within declared compatibility windows.

If every deploy requires coordinated changes everywhere, extraction value is weak.

---

# 119. Failure-isolation test

Ask:

> If this service is down, what remains usable?

Use the answer to evaluate true isolation.

---

# 120. Scaling-independence test

Ask whether capability has materially distinct workload.

Potential examples:

- Analytics;
- realtime fan-out;
- provider sync;
- collaborative documents.

No distinct profile means scaling is weak justification.

---

# 121. Security-isolation test

Extraction can improve security if it actually reduces:

- credentials;
- data access;
- network trust;
- operator access.

If every service shares all secrets/DB privileges, isolation is superficial.

---

# 122. Technology-specialization test

Use another runtime/database only when requirements justify it.

Operational diversity has cost.

---

# 123. Data-sovereignty test

Customer/regulatory placement requirements may justify extraction even at modest scale.

Treat as security/compliance architecture, not optimization.

---

# 124. Provider-isolation test

Provider-heavy workers can be independently deployable for rate-limit/error isolation while remaining part of the Integrations semantic context.

---

# 125. Service extraction readiness checklist

```text
[ ] explicit context/product owner
[ ] stable vocabulary/lifecycle/invariants
[ ] no ambiguous authoritative fact
[ ] inbound contracts inventoried
[ ] outbound dependencies inventoried
[ ] foreign writes eliminated/planned
[ ] data ownership mapped
[ ] cross-context atomicity resolved
[ ] async identity/idempotency/order defined
[ ] tenant/auth propagation defined
[ ] public API compatibility defined
[ ] realtime ownership defined
[ ] runtime host/deployment defined
[ ] observability
[ ] tests/gates
[ ] migration
[ ] recovery
[ ] operational owner
[ ] measurable motivation
[ ] accepted SYS-ADR
```

---

# 126. Data extraction checklist

```text
[ ] authoritative source
[ ] target store
[ ] schema
[ ] backfill
[ ] live-write coexistence
[ ] idempotency
[ ] tenant partition
[ ] RLS/security
[ ] integrity/uniqueness
[ ] parity verification
[ ] read cutover
[ ] write cutover
[ ] old data removal/retention
[ ] recovery boundary
```

---

# 127. Contract extraction checklist

```text
[ ] inbound operations
[ ] outbound operations
[ ] producer/consumer
[ ] auth/tenant
[ ] idempotency
[ ] timeout/retry
[ ] compatibility/version
[ ] mixed-version window
[ ] backlog
[ ] mobile/external impact
[ ] generated artifacts
[ ] contract tests
```

---

# 128. Runtime extraction checklist

```text
[ ] service identity
[ ] configuration
[ ] secrets
[ ] health
[ ] logs/metrics/traces
[ ] ingress
[ ] database
[ ] cache
[ ] messaging
[ ] providers
[ ] shutdown/drain
[ ] deployment
[ ] scaling
[ ] alerts
[ ] incident/runbook
```

---

# 129. Security extraction checklist

```text
[ ] workload identity
[ ] original principal propagation if needed
[ ] tenant/resource scope
[ ] authorization
[ ] RLS
[ ] secret isolation
[ ] network trust
[ ] audit
[ ] administrative access
[ ] threat review
```

---

# 130. Cutover checklist

```text
[ ] service healthy
[ ] data parity
[ ] contract compatibility
[ ] traffic control
[ ] observability
[ ] alerts
[ ] rollback/roll-forward
[ ] no duplicate publisher/job
[ ] one authoritative writer
[ ] frontend/mobile compatibility
[ ] on-call ready
```

---

# 131. Removal checklist

```text
[ ] old direct calls removed
[ ] old DB writes removed
[ ] compatibility adapters removed
[ ] flags removed
[ ] dual write removed
[ ] temporary projection normalized
[ ] exceptions resolved
[ ] old routes/jobs disabled
[ ] obsolete secrets removed
[ ] docs updated
[ ] architecture tests prevent regression
```

---

# 132. Stop conditions

Stop extraction rather than guess when:

- context ownership disputed;
- data ownership ambiguous;
- service still needs direct writes into another owner;
- same-transaction invariant has no distributed design;
- service-to-service auth/tenant propagation undefined;
- contract compatibility unknown;
- data cutover lacks verification;
- recovery is impossible and not explicitly accepted;
- no operator owner;
- observability absent;
- reason is only architectural aesthetics;
- critical request path becomes a chatty remote dependency without reliability analysis.

---

# 133. Related canonical owners

```text
docs/architecture/system-overview.md
docs/architecture/bounded-context-map.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md

docs/delivery/change-classification.md
docs/delivery/change-impact-and-migration.md
docs/delivery/release-rollout-and-recovery.md

docs/infrastructure/deployment-runtime.md
docs/operations/observability.md
docs/operations/incident-readiness.md

backend/docs/architecture/backend-overview.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md

frontend/docs/architecture/architecture-change-policy.md
```

---

# 134. Final extraction rule

Before moving deployment boundaries, Notrelix must be able to answer:

```text
What capability is being extracted?
Who owns its facts?
Why does extraction create measurable value now?
Which contracts cross the boundary?
Which data moves?
Which data stays projected?
Which invariants remain local?
Which workflows become eventual?
How are identity/tenant/auth propagated?
How are retry/idempotency/order handled?
How do we deploy, observe, cut over, and recover?
How do we prove old backdoors are gone?
```

Extraction succeeds when:

> **business semantics remain stable, hidden couplings are removed, operational independence is real, and the network boundary does not become a substitute for architectural discipline.**
