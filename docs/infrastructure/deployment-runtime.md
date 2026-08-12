---
document_id: INFRA-DEPLOYMENT-RUNTIME
document_type: infrastructure-standard
status: active
owner: infrastructure
applies_to:
  - deployment
  - runtime
  - processes
  - networking
  - persistence
  - cache
  - messaging
  - object-storage
  - external-providers
  - promotion
evidence:
  - docker-compose.yml
  - docker-compose.dev.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
  - infra/
  - docs/infrastructure/environment-model.md
  - docs/delivery/release-and-rollout.md
  - docs/operations/service-degradation.md
  - docs/operations/observability.md
  - docs/architecture/capability-extraction-strategy.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
review_on:
  - deployment-topology-change
  - process-boundary-change
  - persistence-service-change
  - cache-service-change
  - messaging-service-change
  - object-storage-change
  - network-boundary-change
  - scaling-model-change
  - service-extraction
---

# Deployment Runtime

> **Infrastructure turns repository artifacts into running processes and managed dependencies without redefining product or bounded-context ownership.**
>
> Deployment topology may evolve from a modular monolith to independently deployed capabilities. Semantic/data/contract boundaries must remain explicit before, during, and after that change.

This document is the canonical repository-level owner for runtime process roles, deployment topology, dependency roles, network exposure, runtime identity, state placement, scaling boundaries, health wiring, and infrastructure promotion invariants.

Container image/build details belong to `containerization-and-local-services.md`.

Release cohort/rollback decision policy belongs to `release-and-rollout.md`.

Backend Infrastructure/Platform docs own application-side persistence and messaging implementation.

---

# 1. Runtime principles

Canonical runtime model:

```text
immutable application artifacts
+
environment configuration/secrets
+
runtime identity
+
managed durable dependencies
+
network routes
+
health/observability
```

No runtime mechanism transfers product semantic ownership.

---

# 2. INFRA-RUN-001 — Deployment packaging does not redefine bounded contexts

Several contexts MAY share:

```text
one backend binary
one process
one database
one deployment
```

while retaining separate:

```text
semantic ownership
schema/table ownership
contracts
events
invariants
```

---

# 3. Modular monolith

Current architecture is a modular monolith.

This is a deployment choice optimized for current stage, not permission for cross-context internals.

---

# 4. INFRA-RUN-002 — Shared process does not imply shared business model

One memory space/database connection does not authorize:

- foreign table mutation;
- Domain reference bypass;
- generic event ownership;
- context lifecycle merging.

---

# 5. Future extraction

A capability can later extract to its own service/process when architecture strategy justifies it.

---

# 6. INFRA-RUN-003 — Extraction preserves semantic owner

Service extraction changes:

```text
network
deployment
data movement
operations
failure modes
```

not the product meaning of the context.

---

# 7. Process roles

Runtime MAY contain roles such as:

```text
API/backend request process
background consumer/worker
scheduler
migration job
frontend web host
marketing host
gateway/proxy
```

Exact role split may evolve.

---

# 8. INFRA-RUN-004 — Process role has explicit responsibility

Do not run unrelated privileged maintenance behavior inside every steady-state application process merely because the code is available.

---

# 9. API process

API/backend process owns:

```text
HTTP composition
Application invocation
authentication boundary
runtime composition
```

according to backend architecture.

Infrastructure provides the runtime resources/identity.

---

# 10. Background worker

A background worker performs durable async work under Platform contracts.

It still uses explicit tenant/resource scope and bounded runtime identity.

---

# 11. INFRA-RUN-005 — Worker is not globally trusted administrator

A background process MUST NOT gain unrestricted tenant/database/provider authority only because it is non-interactive.

---

# 12. Scheduler

Scheduler creates/claims logical due work according to Automation/Platform contracts.

---

# 13. INFRA-RUN-006 — Multiple scheduler instances cannot create uncontrolled duplicate logical occurrences

Use the approved claim/idempotency/leader semantics.

Scaling scheduler processes does not change one-occurrence product meaning.

---

# 14. Migration process

Migration/DDL/data-repair may require elevated DB/runtime permissions.

---

# 15. INFRA-RUN-007 — Migration role is separable from steady-state runtime

Where platform permits, use distinct:

```text
identity
job
lifecycle
permission
```

for schema/data changes.

---

# 16. Frontend hosts

Web, marketing, and mobile distribution are separate consumers/runtime surfaces.

They do not share server secrets.

---

# 17. INFRA-RUN-008 — Frontend runtime is untrusted client/public-delivery surface

Never expose:

- DB credentials;
- JWT signing secrets;
- provider client secrets;
- private infrastructure endpoints

through client bundle/config.

---

# 18. Gateway/proxy

Gateway/reverse proxy can own:

```text
TLS termination
routing
static host routing
forwarding headers
compression
edge limits
```

according to topology.

It does not replace application authentication/authorization.

---

# 19. INFRA-RUN-009 — Network trust does not replace resource authorization

A request arriving from an internal network/gateway is still subject to application security contract.

---

# 20. Runtime artifact

Application artifact should be reproducible and traceable to source revision.

Deployment injects environment config separately.

---

# 21. INFRA-RUN-010 — Runtime artifact is immutable for one release identity

Do not mutate application code/generated contract inside a running container/server after promotion as ordinary deployment practice.

---

# 22. Artifact identity

Logs/deployments SHOULD expose:

```text
source SHA
build/release identity
host/app version
```

to correlate incidents and rollouts.

---

# 23. INFRA-RUN-011 — Mutable `latest` is not the only production identity

A human/operator must be able to identify the exact artifact currently serving workload.

---

# 24. State classes

Runtime state is classified:

```text
authoritative durable
derived durable
ephemeral/cache
local temporary
external provider state
```

---

# 25. INFRA-RUN-012 — Container/process filesystem is not authoritative product storage

Unless explicitly designed as durable mounted storage, local filesystem/process memory is ephemeral.

Business data belongs in the owning durable mechanism.

---

# 26. PostgreSQL

Current architecture treats PostgreSQL as authoritative relational persistence and RLS defense-in-depth.

---

# 27. INFRA-RUN-013 — PostgreSQL runtime preserves Application transaction and tenant contracts

Infrastructure config for:

```text
pool
timeout
connection
migration
RLS
```

must not alter business atomicity or tenant isolation.

---

# 28. Connection pools

Pool size/timeouts follow workload/resource capacity.

Do not invent universal values in canonical docs.

---

# 29. INFRA-RUN-014 — Connection capacity is bounded and observable

Pool exhaustion, lock pressure, and query latency require operational signals and backpressure.

---

# 30. Database network

Production DB SHOULD be reachable only by required runtime/admin/recovery paths according to infrastructure capability.

---

# 31. INFRA-RUN-015 — Public database exposure is not production convenience

Direct Internet exposure requires explicit security architecture, not default port publishing.

---

# 32. Redis/cache

Redis/cache is scoped acceleration/ephemeral derived state unless a specific accepted architecture says otherwise.

---

# 33. INFRA-RUN-016 — Cache cannot become authorization or product truth

Loss/eviction should lead to:

```text
miss
recompute/refetch
safe degradation
```

not permission grant or semantic data loss.

---

# 34. Redis durability

Redis persistence settings may support operational recovery, but cache remains derived unless separately classified.

Do not infer product durability from AOF being enabled.

---

# 35. Messaging broker

Broker transports durable async work according to Platform.

Current local/base evidence includes optional RabbitMQ, but the canonical dependency role is provider-neutral enough to permit change through architecture review.

---

# 36. INFRA-RUN-017 — Broker semantics must satisfy Platform delivery contract

Required properties include as applicable:

```text
message identity
consumer identity
at-least-once handling
ack after successful processing
retry/backoff
poison/dead-letter
ordering support
```

---

# 37. Broker substitution

Changing broker is architectural if transactionality/order/delivery/security semantics differ.

---

# 38. INFRA-RUN-018 — Similar API does not prove dependency equivalence

A new broker/cache/database/provider must be evaluated by protected semantics, not SDK convenience.

---

# 39. Outbox

The DB-backed outbox decouples source commit from broker availability.

Infrastructure provides dispatcher/broker runtime.

---

# 40. INFRA-RUN-019 — Broker outage must not erase committed outbox work

Capacity/backlog management and service degradation follow Operations policy.

---

# 41. Object storage

Object storage owns binary/object durability mechanism.

Product contexts own metadata/lifecycle references.

---

# 42. INFRA-RUN-020 — Object storage identity is referenced, not embedded as business data

Domain/events SHOULD carry safe metadata/reference, not large binary payloads.

---

# 43. Object access

Upload/download access uses scoped authorization, signed URL/token or equivalent mechanism where needed.

---

# 44. INFRA-RUN-021 — Storage URL is not permanent permission

Download access can be revoked/expired according to product security lifecycle.

---

# 45. External providers

Provider adapters own network/protocol mechanics for:

```text
email
OAuth
calendar
payment
automation/integration providers
```

according to context.

---

# 46. INFRA-RUN-022 — Provider call has bounded runtime policy

Define:

```text
timeout
cancellation
rate limit
retry class
idempotency/correlation
unknown outcome
```

through owning adapter/runtime configuration.

---

# 47. Provider credentials

Credentials are scoped per environment/runtime role/provider account.

---

# 48. INFRA-RUN-023 — Provider admin credential is not default application credential

Use least privilege available from provider.

---

# 49. Networking

Network segmentation expresses exposure and connectivity needs.

It does not create semantic trust.

---

# 50. INFRA-RUN-024 — Network path follows least connectivity

Processes/services SHOULD connect only to required networks/dependencies.

Current Compose uses separate frontend/backend/data network concepts; future orchestrators may implement equivalent isolation differently.

---

# 51. Internal DNS/service discovery

Service names/endpoints are infrastructure configuration.

Product code should depend on adapter/config contracts, not hardcoded container names.

---

# 52. INFRA-RUN-025 — Service discovery is replaceable

Moving from Compose DNS to another orchestrator should not require Domain/Application changes.

---

# 53. Public ingress

Only intended public hosts/endpoints should be exposed.

Data stores/brokers/admin tools are private unless explicit operational requirement exists.

---

# 54. INFRA-RUN-026 — Admin tooling is not production public surface by default

Tools such as DB admin consoles require restricted access and independent hardening.

---

# 55. Runtime identity

Each process receives identity/credentials required for:

```text
DB
cache
broker
storage
provider
telemetry
```

---

# 56. INFRA-RUN-027 — Runtime permissions follow process responsibility

Examples:

```text
API
worker
migration
backup
deployment
```

may have distinct capabilities.

---

# 57. Filesystem

Production containers/processes SHOULD be read-only/minimal where practical, with explicit writable temp/state mounts only where needed.

Current production Compose already applies read-only filesystems to selected services.

---

# 58. INFRA-RUN-028 — Writable filesystem is intentional

Do not assume container root filesystem is durable or safe secret/data storage.

---

# 59. Process privilege

Run non-root / drop capabilities / no-new-privileges where platform/application permits.

Exact mechanism is orchestrator-specific.

---

# 60. INFRA-RUN-029 — Privilege hardening cannot break required runtime semantics silently

If a capability is added back, document why the process needs it.

---

# 61. Health

Runtime exposes liveness/readiness according to process responsibility.

---

# 62. INFRA-RUN-030 — Readiness means safe to receive intended workload

Readiness should account for required:

```text
startup/config
schema
critical dependency
internal initialization
```

without treating every optional provider as globally critical.

---

# 63. Liveness

Liveness detects stuck process, not dependency outage by blindly restarting every instance.

---

# 64. INFRA-RUN-031 — Liveness avoids dependency restart loops

If DB/provider is down, restarting healthy application processes continuously may amplify outage.

---

# 65. Capability health

Optional degraded dependencies SHOULD be reflected in operational capability health rather than global process death when architecture permits.

---

# 66. Scaling

Horizontal/vertical scaling is infrastructure mechanism constrained by application semantics.

---

# 67. INFRA-RUN-032 — Scale-out preserves idempotency and coordination semantics

Adding instances must not duplicate:

- schedules;
- provider side effects;
- migrations;
- one-owner coordination;
- ordered work

beyond approved semantics.

---

# 68. Stateless request processes

API/web hosts SHOULD minimize process-local state required for correctness so instances can scale/restart.

---

# 69. INFRA-RUN-033 — Process-local cache/session is not sole durable authority

Restarting an instance cannot lose authoritative user/business state.

---

# 70. Worker scaling

Worker concurrency is bounded by:

```text
DB
provider rate limits
message ordering
tenant fairness
memory/CPU
```

---

# 71. INFRA-RUN-034 — More workers are not automatically more throughput

Scaling can worsen lock/rate-limit/retry saturation.

Operations/performance evidence guides capacity changes.

---

# 72. Resource limits

CPU/memory reservations/limits MAY be declared per environment.

Canonical docs do not freeze exact numbers without operational evidence.

---

# 73. INFRA-RUN-035 — Resource limits are operational configuration, not product architecture

Change them through measured capacity/release operations.

---

# 74. Startup

Startup performs only bounded initialization required before readiness.

Avoid large non-idempotent business jobs.

---

# 75. INFRA-RUN-036 — Long data backfill is not ordinary process startup

Use migration/job workflow for long/recoverable data movement.

---

# 76. Seed

Development/staging seed behavior is operational bootstrap.

Production seeding is disabled unless a separately governed bootstrap process is required.

---

# 77. INFRA-RUN-037 — Runtime restart cannot reset production data

Seed/reset flags in lower environments MUST NOT leak into production defaults.

---

# 78. Migrations

Migration execution sequence belongs to Delivery + backend Infrastructure.

Infrastructure runtime ensures appropriate connectivity/identity/job mechanism.

---

# 79. INFRA-RUN-038 — Migration is coordinated with rolling runtime

Do not run schema contraction while old binaries still serve/consume old representation.

---

# 80. Promotion

Infrastructure promotes the artifact/config through environments according to release policy.

---

# 81. INFRA-RUN-039 — Production promotion preserves exact evidence identity

Deployment record correlates:

```text
artifact SHA/version
database migration level
configuration/flag state
```

where tooling supports it.

---

# 82. Current Compose caveat

Current staging/production Compose files use `build:` during `up --build`.

This is current executable packaging evidence, not proof that build-on-production is the desired final immutable-promotion architecture.

---

# 83. INFRA-RUN-040 — Build-on-target is not normalized as canonical release strategy

The target remains:

```text
tested source/artifact provenance
→ immutable/reproducible release identity
→ promoted deployment
```

A future CI/CD pipeline may replace current Compose build-on-host mechanics.

---

# 84. Rolling deployment

Old/new process versions may coexist.

Runtime topology must support mixed-version compatibility defined by Delivery.

---

# 85. INFRA-RUN-041 — Shared dependency stays compatible through overlap window

Review:

```text
DB schema
cache format/key
message/event
config
provider mapping
```

before rollout.

---

# 86. Gateway deployment

Gateway config changes can affect:

- routing;
- cache;
- TLS;
- forwarded headers;
- request limits.

They are infrastructure/runtime changes.

---

# 87. INFRA-RUN-042 — Gateway cannot bypass API security semantics

Do not trust arbitrary forwarded identity headers without accepted authentication architecture.

---

# 88. Frontend deployment

Web/marketing artifacts are independently versioned consumer deployments.

Old browser bundles may remain loaded.

---

# 89. INFRA-RUN-043 — Static asset deployment accounts for immutable/cacheable assets

Avoid overwriting versioned asset identity in a way that makes old HTML reference incompatible new chunks.

Exact frontend host strategy belongs to frontend/infrastructure implementation.

---

# 90. Mobile deployment

Mobile artifacts are distributed independently of server runtime.

Infrastructure/release must preserve supported server compatibility.

---

# 91. Logs

Runtime routes logs to operational collection without making container disk a durable log archive by assumption.

---

# 92. INFRA-RUN-044 — Local log rotation is not production observability architecture

Current Compose json-file rotation is useful evidence for local/self-hosted topology; production collection/retention requires operational implementation.

---

# 93. Telemetry

Runtime provides exporter/collector endpoints/config as approved.

Telemetry loss should follow Operations degradation semantics.

---

# 94. INFRA-RUN-045 — Telemetry path does not block critical request indefinitely

Observability is important but is not source-transaction authority.

---

# 95. Backup/recovery

Infrastructure supplies backup/restore mechanisms for durable stores.

Operations owns recovery correctness and verification.

---

# 96. INFRA-RUN-046 — Backup mechanism does not replace recovery runbook

A snapshot resource alone is not a complete recovery system.

---

# 97. Dependency lifecycle

Adding/removing dependency requires:

```text
owner
purpose
authority class
network
credentials
health
capacity
failure/degradation
backup/recovery if durable
tests
```

---

# 98. INFRA-RUN-047 — New dependency has declared authority class

Classify:

```text
authoritative durable
derived durable
cache/ephemeral
delivery transport
external provider
development tool
```

before architecture normalizes it.

---

# 99. Dependency substitution

Changing PostgreSQL/Redis/broker/object provider can materially change semantics.

---

# 100. INFRA-RUN-048 — Provider replacement evaluates semantics, not product logo/API similarity

Review:

```text
transactionality
consistency
ordering
durability
security
tenant isolation
failure model
recovery
```

---

# 101. Local development topology

Current development Compose exposes dependency ports and mounts source for live reload.

This is development-specific.

---

# 102. INFRA-RUN-049 — Development topology is not production topology authority

Do not copy:

- writable source mounts;
- SDK images;
- admin ports;
- dev JWT defaults;
- seed/reset;

into production merely for parity.

---

# 103. Production topology

Current production Compose evidence includes:

```text
PostgreSQL
Redis
optional RabbitMQ
backend
web
marketing
nginx
```

with selected read-only filesystems/capability drops and required production configuration.

This is current evidence, not a permanent resource-count/cloud-provider declaration.

---

# 104. INFRA-RUN-050 — Exact cloud/provider resources come from executable infrastructure

Canonical docs describe durable invariants.

IaC/deployment automation owns exact:

```text
region
instance count
resource class
network IDs
provider product
autoscaling values
```

once those exist.

---

# 105. Infrastructure drift

Drift occurs when deployed runtime/resource differs unintentionally from versioned desired state.

---

# 106. INFRA-RUN-051 — Runtime drift is detectable and reconciled

Emergency manual fixes are recorded then:

- encoded in desired state;
- or reverted.

---

# 107. Runtime inventory

Operators should be able to identify:

```text
deployed components
artifact versions
dependency endpoints/classes
migration level
feature/config state
```

without private knowledge.

---

# 108. INFRA-RUN-052 — Runtime inventory is machine-evidenced where possible

Avoid maintaining a giant handwritten resource inventory that immediately drifts from IaC/orchestrator.

---

# 109. Runtime security checklist

```text
[ ] explicit process role
[ ] least-privilege identity
[ ] secrets external to artifact
[ ] network exposure minimal
[ ] filesystem writable only as needed
[ ] public client config safe
[ ] admin/migration identity separated where possible
[ ] health/readiness
[ ] logs/telemetry safe
```

---

# 110. Dependency checklist

```text
[ ] authority class
[ ] owner
[ ] protocol semantics
[ ] network
[ ] credential
[ ] timeout/cancellation
[ ] retry/idempotency
[ ] capacity/backpressure
[ ] degradation
[ ] observability
[ ] recovery/backup if durable
```

---

# 111. Scaling checklist

```text
[ ] coordination key
[ ] idempotency
[ ] ordering
[ ] schedule duplication
[ ] DB pool/lock
[ ] provider rate limit
[ ] tenant fairness
[ ] resource limits
[ ] backlog/catch-up
```

---

# 112. Deployment checklist

```text
[ ] exact artifact/revision
[ ] config/secrets
[ ] migration compatibility
[ ] old/new instance compatibility
[ ] network/routes
[ ] readiness/liveness
[ ] observability
[ ] dependency capacity
[ ] rollout/rollback/forward recovery
```

---

# 113. Stop conditions

Stop rather than normalize if:

- deployment packaging is being used to merge bounded-context semantics;
- worker receives global admin credentials without necessity;
- runtime filesystem/process memory becomes business durability;
- cache becomes permission/source truth;
- broker ack occurs before approved durable processing;
- object URL becomes permanent authorization;
- provider call has no timeout/idempotency/unknown-outcome model;
- internal network is used as authorization bypass;
- scale-out can duplicate scheduled/provider effects;
- long backfill runs in every process startup;
- production seed/reset behavior is enabled casually;
- build-on-production is treated as immutable-promotion proof;
- exact cloud/resource numbers are authored in canonical docs while executable IaC disagrees.

---

# 114. Related canonical owners

```text
docs/infrastructure/environment-model.md
docs/infrastructure/containerization-and-local-services.md
docs/delivery/release-and-rollout.md
docs/delivery/migration-policy.md
docs/operations/observability.md
docs/operations/service-degradation.md
docs/operations/recovery-and-data-safety.md
docs/architecture/capability-extraction-strategy.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
```

---

# 115. Final runtime rule

For every runtime/dependency/topology change, answer:

```text
Which process/capability needs this resource?
What authority class does it have?
Which semantic owner remains authoritative?
What runtime identity and network access are required?
Where is durable state stored?
What happens on restart/scale-out?
How are timeout/retry/idempotency/order preserved?
How does it degrade and recover?
How is exact release/runtime identity observed?
Can this topology change later without rewriting product semantics?
```

The target is:

> **a replaceable deployment/runtime layer that gives each process the minimum resources and privileges it needs, keeps durable/derived/external state roles explicit, scales without duplicating logical work, and preserves modular-monolith boundaries so future extraction changes topology rather than product meaning.**
