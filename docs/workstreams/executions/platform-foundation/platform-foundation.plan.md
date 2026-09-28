---
document_id: WRK-PLAN-PLATFORM-FOUNDATION
document_type: workstream-plan
status: active
owner: platform-foundation-team
applies_to:
  - backend-platform
  - backend-cross-cutting-foundation
  - frontend-foundation
  - frontend-runtime
  - frontend-ui-foundation
  - architecture-tooling
  - generated-contracts
  - ci-quality-gates
evidence:
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/docs/architecture/ui-and-design-system.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on:
  - shared-runtime-contract-change
  - authorization-enforcement-change
  - tenancy-context-change
  - messaging-delivery-change
  - idempotency-change
  - realtime-foundation-change
  - query-foundation-change
  - ui-foundation-change
  - architecture-gate-change
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# PLAN — Platform & Foundation

## 1. Purpose

This PLAN executes the 128 requirements in `platform-foundation.spec.md` without creating a second architecture authority and without authorizing a broad Platform rewrite.

Execution order is brownfield-first:

```text
freeze exact candidate
→ inventory canonical owner + production source
→ classify RETAIN / HARDEN / GAP / DEPENDENCY_BLOCKED / NOT_APPLICABLE
→ make the minimum lane-scoped change
→ execute focused + production-graph + consumer evidence
→ hand off exact readiness at exact candidate SHA
```

Existing ADR/TAC/source mechanisms are retained when they satisfy the contract. Source existence and historical green CI are baseline evidence only; D4/D5 requires exact-candidate proof.

## 2. Canonical waves

The canonical team wave semantics are:

```text
Wave 0 — security / tenancy / frontend state isolation
  PF-01 Session/CSRF
  PF-02 Context propagation
  PF-03 Authorization enforcement
  PF-11 Query/server-state isolation

Wave 1 — async + realtime reliability
  PF-06 Idempotency
  PF-07 Messaging delivery/dedup
  PF-08 Ordering/poison
  PF-09 Realtime recovery

Wave 2 — developer throughput / shared frontend
  PF-10 Generated API contracts
  PF-12 Runtime composition
  PF-13 UI foundation
  PF-14 Observability

Wave 3 — continuous governance
  PF-15 Architecture/dependency gates
  PF-16 CI/container/security evidence
```

`PF-04` persistence/RLS and `PF-05` migration/init are support tracks. They run alongside whichever wave/consumer changes persistence or schema; they are not reclassified as standalone Wave-1 product capabilities.

## 3. Exact readiness targets

| Platform contract | Consumers | Required target |
|---|---|---|
| session/CSRF | Identity + protected browser features | `D5` |
| actor/account/workspace context | all tenant-scoped teams | `D5` |
| authorization enforcement | all protected teams | `D5` |
| idempotency | governed command consumers | `D4–D5` by consumer |
| messaging delivery | async consumers | `D5` |
| realtime recovery | Work Management / Documents / Collaboration | `D4+` per consumer |
| account state isolation | frontend tenant-scoped teams | `D5` |
| generated API contract flow | frontend teams | `D5` |
| UI export evidence | UI consumers | `D4` |
| architecture/CI gates | all teams | `D5` |

A consumer remains dependency-blocked while its exact required target is unmet.

## 4. Preparation source posture and execution disposition

| Lane | Posture | Execution disposition |
|---|---|---|
| PF-01 Session and CSRF transport | `IMPLEMENTED_UNCERTIFIED` | Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate. |
| PF-02 Actor / Account / Workspace context propagation | `GAP_CONFIRMED` | Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable. |
| PF-03 Authorization enforcement pipeline | `IMPLEMENTED_UNCERTIFIED` | Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths. |
| PF-04 Persistence and tenant-isolation foundation | `IMPLEMENTED_UNCERTIFIED` | Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority. |
| PF-05 Migration and database initialization | `IMPLEMENTED_UNCERTIFIED` | For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure. |
| PF-06 Idempotency | `IMPLEMENTED_UNCERTIFIED` | Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer. |
| PF-07 Outbox, consumer deduplication and message identity | `PARTIAL_GAP` | Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome. |
| PF-08 Ordered delivery and poison handling | `PARTIAL_GAP` | For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment. |
| PF-09 Realtime transport and recovery | `GAP_CONFIRMED` | Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes. |
| PF-10 API, OpenAPI and generated contracts | `IMPLEMENTED_UNCERTIFIED` | Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift. |
| PF-11 Frontend query/server-state foundation | `GAP_CONFIRMED` | Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache. |
| PF-12 Frontend runtime and host composition | `IMPLEMENTED_UNCERTIFIED` | Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation. |
| PF-13 UI tokens and primitive foundation | `GAP_CONFIRMED` | Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review. |
| PF-14 Observability | `IMPLEMENTED_UNCERTIFIED` | Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation. |
| PF-15 Architecture and dependency enforcement | `IMPLEMENTED_UNCERTIFIED` | Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries. |
| PF-16 CI, container, security and packaging evidence | `IMPLEMENTED_UNCERTIFIED` | Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics. |

## 5. Decision and escalation gate

Platform may decide locally only private implementation shape, internal data structures, test fixtures and performance changes that preserve frozen contracts.

Implementation MUST stop and escalate before code changes when a proposal introduces:

- a new cross-cutting framework;
- a new production project or service;
- an authorization architecture change;
- a messaging delivery-guarantee change;
- a frontend architecture-manifest semantic change;
- a new global-state architecture;
- repository-wide dependency adoption;
- security weakening.

Platform can provide extraction mechanisms, but it does not select service boundaries. Bounded-context ownership plus Architecture approval/ADR is required before any new service/project extraction.

## 6. TAC flow evidence reuse plan

Historical TAC proof is a reusable baseline, never automatic exact-candidate D5.

| TAC flow | Platform work units | Reused mechanism | Preparation state | Exact candidate rule |
|---|---|---|---|---|
| `PF-FLOW-01` request execution pipeline | `PF-02-*`, `PF-03-*`, `PF-04-*` | MediatR order + ExecutionContext/DataSession/AccessControl | `REUSABLE_BASELINE_ONLY` | rerun when behavior order/context/data-session/auth changes |
| `PF-FLOW-02` DomainEvent → IntegrationEvent → outbox | `PF-07-*` | DomainEventInterceptor + mappers + durable outbox | `REUSABLE_BASELINE_ONLY` | rerun on mapping/source transaction/outbox state change |
| `PF-FLOW-03` tenant restoration + dedup | `PF-02-*`, `PF-04-*`, `PF-07-*` | TenantContextConsumeFilter + durable dedup | `REUSABLE_WITH_OPEN_GAP` | rerun; command-owned stale Processing recovery must close |
| `PF-FLOW-04` retry/failure | `PF-07-*`, `PF-08-*`, `PF-14-*` | OutboxDispatcher + MassTransit retry/failure | `REUSABLE_WITH_RUNTIME_OWNER_CHECK` | rerun and bind actual production runtime owner |
| `PF-FLOW-05` contract evolution/recovery | `PF-07-*`, `PF-10-*` | IntegrationEventCatalog/EventContractKey/manifest | `REUSABLE_WITH_OVERRIDE_REVIEW` | rerun on schema/version/backlog/recovery change |
| `PF-FLOW-06` background actor/security context | `PF-02-*`, `PF-03-*`, `PF-04-*` | ExecutionContext + system markers | `REUSABLE_BASELINE_ONLY` | rerun on worker/System/RLS/auth semantics |
| `PF-FLOW-07` scoped tenant envelope | `PF-02-*`, `PF-07-*` | scoped envelope + TenantContextConsumeFilter | `REUSABLE_BASELINE_ONLY` | rerun on scope/envelope/fallback/restoration semantics |

## 7. Production runtime-owner matrix

| Capability | Reference mechanism | Production runtime owner | Required PLAN closure |
|---|---|---|---|
| transactional outbox | Application/Infrastructure outbox contracts | source transaction + Infrastructure dispatcher | exact atomic enrollment + reclaim/failure proof |
| consumer dedup | `MessagingProcessedEvent` + `MessageDeduplicationStore` + `DeduplicationConsumeFilter` | Infrastructure/MassTransit | close command-owned stale `Processing` crash recovery |
| ordering | Platform `OrderingEnforcer` / `ConsumerHost` | actual Infrastructure/MassTransit path per ordered consumer | prove wiring/equivalence or mark consumer `NOT_APPLICABLE` |
| poison/failure containment | Platform `PoisonDetector` + durable outbox failure mechanics | Infrastructure/MassTransit + durable failure state | production consumer/message scoped proof |
| realtime recovery | RealtimeClient + web recovery policy | consuming frontend runtime | canonical key recovery + checkpoint convergence |

Mechanism tests cannot be substituted for production-runtime proof.

## 8. Canonical BE-PLT execution routing

This table operationalizes the SPEC crosswalk into PLAN and stable TEST/CERT references. `PLAN` lists the work units that own the mapped PFREQs; duplicate units are intentionally collapsed.
| Canonical rule | PFREQ | PLAN | TESTS | CERT |
|---|---|---|---|---|
| `BE-PLT-001` | PFREQ001, PFREQ057 | `PF-INV-001`, `PF-07-COMPAT-001` | `PF-TST-GLOBAL-001`, `PF-TST-MSG-057` | `PF-CERT-CSRF-001`, `PF-CERT-MSG-001`, `PF-CERT-ORDER-001` |
| `BE-PLT-002` | PFREQ052, PFREQ126 | `PF-07-INV-001`, `PF-GOV-002` | `PF-TST-MSG-052`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-003` | PFREQ052, PFREQ076, PFREQ126 | `PF-07-INV-001`, `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-052`, `PF-TST-API-076`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-004` | PFREQ057, PFREQ127 | `PF-07-COMPAT-001`, `PF-GOV-002` | `PF-TST-MSG-057`, `PF-TST-CROSS-127` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-005` | PFREQ052, PFREQ123 | `PF-07-INV-001`, `PF-GOV-002` | `PF-TST-MSG-052`, `PF-TST-CROSS-123` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-006` | PFREQ052, PFREQ100 | `PF-07-INV-001`, `PF-14-INV-001`, `PF-GATE-003` | `PF-TST-MSG-052`, `PF-TST-OBS-100` | `PF-CERT-MSG-001`, `PF-CERT-OBS-001` |
| `BE-PLT-007` | PFREQ053, PFREQ126 | `PF-07-CORE-001`, `PF-GOV-002` | `PF-TST-MSG-053`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-008` | PFREQ046, PFREQ126 | `PF-06-CORE-001`, `PF-GOV-002` | `PF-TST-IDEM-046`, `PF-TST-CROSS-126` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-009` | PFREQ076, PFREQ126 | `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-API-076`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-010` | PFREQ076, PFREQ126 | `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-API-076`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-011` | PFREQ051 | `PF-07-INV-001`, `PF-GATE-002` | `PF-TST-MSG-051` | `PF-CERT-MSG-001` |
| `BE-PLT-012` | PFREQ051, PFREQ064, PFREQ126 | `PF-07-INV-001`, `PF-GATE-002`, `PF-08-COMPAT-001`, `PF-GOV-002` | `PF-TST-MSG-051`, `PF-TST-ORDER-064`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-013` | PFREQ054, PFREQ056, PFREQ123 | `PF-07-CORE-001`, `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-054`, `PF-TST-MSG-056`, `PF-TST-CROSS-123` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-014` | PFREQ056, PFREQ123 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-056`, `PF-TST-CROSS-123` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-015` | PFREQ053, PFREQ056 | `PF-07-CORE-001`, `PF-07-SEC-001` | `PF-TST-MSG-053`, `PF-TST-MSG-056` | `PF-CERT-MSG-001` |
| `BE-PLT-016` | PFREQ046, PFREQ056 | `PF-06-CORE-001`, `PF-07-SEC-001` | `PF-TST-IDEM-046`, `PF-TST-MSG-056` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001` |
| `BE-PLT-017` | PFREQ056, PFREQ060, PFREQ124 | `PF-07-SEC-001`, `PF-08-CORE-001`, `PF-GOV-002` | `PF-TST-MSG-056`, `PF-TST-ORDER-060`, `PF-TST-CROSS-124` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-018` | PFREQ049, PFREQ104, PFREQ124 | `PF-06-SEC-001`, `PF-14-SEC-001`, `PF-GOV-002` | `PF-TST-IDEM-049`, `PF-TST-OBS-104`, `PF-TST-CROSS-124` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-019` | PFREQ049, PFREQ125 | `PF-06-SEC-001`, `PF-GOV-002` | `PF-TST-IDEM-049`, `PF-TST-CROSS-125` | `PF-CERT-IDEM-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-020` | PFREQ104, PFREQ124 | `PF-14-SEC-001`, `PF-GOV-002` | `PF-TST-OBS-104`, `PF-TST-CROSS-124` | `PF-CERT-ORDER-001`, `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-021` | PFREQ062, PFREQ063 | `PF-08-SEC-001` | `PF-TST-ORDER-062`, `PF-TST-ORDER-063` | `PF-CERT-ORDER-001` |
| `BE-PLT-022` | PFREQ055, PFREQ124 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-CROSS-124` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-023` | PFREQ059, PFREQ125 | `PF-08-INV-001`, `PF-GOV-002` | `PF-TST-ORDER-059`, `PF-TST-CROSS-125` | `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-024` | PFREQ060 | `PF-08-CORE-001` | `PF-TST-ORDER-060` | `PF-CERT-ORDER-001` |
| `BE-PLT-025` | PFREQ055, PFREQ061, PFREQ104 | `PF-07-SEC-001`, `PF-08-CORE-001`, `PF-14-SEC-001` | `PF-TST-MSG-055`, `PF-TST-ORDER-061`, `PF-TST-OBS-104` | `PF-CERT-ORDER-001`, `PF-CERT-OBS-001` |
| `BE-PLT-026` | PFREQ057, PFREQ127 | `PF-07-COMPAT-001`, `PF-GOV-002` | `PF-TST-MSG-057`, `PF-TST-CROSS-127` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-027` | PFREQ019, PFREQ036, PFREQ122 | `PF-02-CORE-001`, `PF-04-COMPAT-001`, `PF-GOV-002` | `PF-TST-CTX-019`, `PF-TST-DATA-036`, `PF-TST-CROSS-122` | `PF-CERT-CTX-001`, `PF-CERT-DATA-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-028` | PFREQ010, PFREQ025, PFREQ121, PFREQ124 | `PF-01-INV-001`, `PF-03-CORE-001`, `PF-GOV-002` | `PF-TST-CSRF-010`, `PF-TST-AUTHZ-025`, `PF-TST-CROSS-121`, `PF-TST-CROSS-124` | `PF-CERT-CSRF-001`, `PF-CERT-AUTHZ-001`, `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-029` | PFREQ044, PFREQ052, PFREQ100, PFREQ127 | `PF-06-INV-001`, `PF-GATE-002`, `PF-07-INV-001`, `PF-14-INV-001`, `PF-GATE-003`, `PF-GOV-002` | `PF-TST-IDEM-044`, `PF-TST-MSG-052`, `PF-TST-OBS-100`, `PF-TST-CROSS-127` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001`, `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-030` | PFREQ076, PFREQ126 | `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-API-076`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-031` | PFREQ076, PFREQ077, PFREQ126 | `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-API-076`, `PF-TST-API-077`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-032` | PFREQ055, PFREQ125, PFREQ126 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-CROSS-125`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-033` | PFREQ055, PFREQ123 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-CROSS-123` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-034` | PFREQ055, PFREQ124, PFREQ126 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-CROSS-124`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-035` | PFREQ122, PFREQ125 | `PF-GOV-002` | `PF-TST-CROSS-122`, `PF-TST-CROSS-125` | `PF-CERT-CTX-001`, `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-036` | PFREQ057, PFREQ127 | `PF-07-COMPAT-001`, `PF-GOV-002` | `PF-TST-MSG-057`, `PF-TST-CROSS-127` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-037` | PFREQ064, PFREQ120 | `PF-08-COMPAT-001`, `PF-GATE-002`, `PF-16-COMPAT-001`, `PF-GATE-004` | `PF-TST-ORDER-064`, `PF-TST-CI-120` | `PF-CERT-ORDER-001`, `PF-CERT-CI-001` |
| `BE-PLT-038` | PFREQ090, PFREQ105, PFREQ120 | `PF-12-SEC-001`, `PF-14-SEC-001`, `PF-16-COMPAT-001`, `PF-GATE-004` | `PF-TST-RUNTIME-090`, `PF-TST-OBS-105`, `PF-TST-CI-120` | `PF-CERT-RUNTIME-001`, `PF-CERT-OBS-001`, `PF-CERT-CI-001` |
| `BE-PLT-039` | PFREQ049, PFREQ123 | `PF-06-SEC-001`, `PF-GOV-002` | `PF-TST-IDEM-049`, `PF-TST-CROSS-123` | `PF-CERT-IDEM-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-040` | PFREQ044, PFREQ047, PFREQ123 | `PF-06-INV-001`, `PF-GATE-002`, `PF-06-CORE-001`, `PF-GOV-002` | `PF-TST-IDEM-044`, `PF-TST-IDEM-047`, `PF-TST-CROSS-123` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-041` | PFREQ049, PFREQ125 | `PF-06-SEC-001`, `PF-GOV-002` | `PF-TST-IDEM-049`, `PF-TST-CROSS-125` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-042` | PFREQ100, PFREQ101, PFREQ103 | `PF-14-INV-001`, `PF-GATE-003`, `PF-14-CORE-001` | `PF-TST-OBS-100`, `PF-TST-OBS-101`, `PF-TST-OBS-103` | `PF-CERT-OBS-001` |
| `BE-PLT-043` | PFREQ055, PFREQ104, PFREQ105 | `PF-07-SEC-001`, `PF-14-SEC-001` | `PF-TST-MSG-055`, `PF-TST-OBS-104`, `PF-TST-OBS-105` | `PF-CERT-MSG-001`, `PF-CERT-OBS-001` |
| `BE-PLT-044` | PFREQ127 | `PF-GOV-002` | `PF-TST-CROSS-127` | `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-045` | PFREQ053, PFREQ054, PFREQ056 | `PF-07-CORE-001`, `PF-07-SEC-001` | `PF-TST-MSG-053`, `PF-TST-MSG-054`, `PF-TST-MSG-056` | `PF-CERT-MSG-001` |
| `BE-PLT-046` | PFREQ123, PFREQ126 | `PF-GOV-002` | `PF-TST-CROSS-123`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-047` | PFREQ102, PFREQ121 | `PF-14-CORE-001`, `PF-GOV-002` | `PF-TST-OBS-102`, `PF-TST-CROSS-121` | `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-048` | PFREQ019, PFREQ028, PFREQ124 | `PF-02-CORE-001`, `PF-03-SEC-001`, `PF-GOV-002` | `PF-TST-CTX-019`, `PF-TST-AUTHZ-028`, `PF-TST-CROSS-124` | `PF-CERT-CTX-001`, `PF-CERT-AUTHZ-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-049` | PFREQ055, PFREQ106, PFREQ127 | `PF-07-SEC-001`, `PF-14-COMPAT-001`, `PF-GATE-003`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-OBS-106`, `PF-TST-CROSS-127` | `PF-CERT-MSG-001`, `PF-CERT-OBS-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-050` | PFREQ076, PFREQ126 | `PF-10-SEC-001`, `PF-GOV-002` | `PF-TST-API-076`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-API-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-051` | PFREQ056, PFREQ126 | `PF-07-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-056`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-052` | PFREQ044, PFREQ046, PFREQ052 | `PF-06-INV-001`, `PF-GATE-002`, `PF-06-CORE-001`, `PF-07-INV-001` | `PF-TST-IDEM-044`, `PF-TST-IDEM-046`, `PF-TST-MSG-052` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001` |
| `BE-PLT-053` | PFREQ055, PFREQ062, PFREQ126 | `PF-07-SEC-001`, `PF-08-SEC-001`, `PF-GOV-002` | `PF-TST-MSG-055`, `PF-TST-ORDER-062`, `PF-TST-CROSS-126` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-054` | PFREQ057, PFREQ124 | `PF-07-COMPAT-001`, `PF-GOV-002` | `PF-TST-MSG-057`, `PF-TST-CROSS-124` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-055` | PFREQ051, PFREQ054, PFREQ055 | `PF-07-INV-001`, `PF-GATE-002`, `PF-07-CORE-001`, `PF-07-SEC-001` | `PF-TST-MSG-051`, `PF-TST-MSG-054`, `PF-TST-MSG-055` | `PF-CERT-MSG-001` |
| `BE-PLT-056` | PFREQ124 | `PF-GOV-002` | `PF-TST-CROSS-124` | `PF-CERT-MSG-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-057` | PFREQ049, PFREQ123, PFREQ125 | `PF-06-SEC-001`, `PF-GOV-002` | `PF-TST-IDEM-049`, `PF-TST-CROSS-123`, `PF-TST-CROSS-125` | `PF-CERT-IDEM-001`, `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-058` | PFREQ061, PFREQ125 | `PF-08-CORE-001`, `PF-GOV-002` | `PF-TST-ORDER-061`, `PF-TST-CROSS-125` | `PF-CERT-ORDER-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-059` | PFREQ069, PFREQ070, PFREQ071, PFREQ127 | `PF-09-SEC-001`, `PF-09-COMPAT-001`, `PF-GATE-002`, `PF-GOV-002` | `PF-TST-RT-069`, `PF-TST-RT-070`, `PF-TST-RT-071`, `PF-TST-CROSS-127` | `PF-CERT-RT-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-060` | PFREQ024, PFREQ028, PFREQ057, PFREQ123 | `PF-03-INV-001`, `PF-03-SEC-001`, `PF-07-COMPAT-001`, `PF-GOV-002` | `PF-TST-AUTHZ-024`, `PF-TST-AUTHZ-028`, `PF-TST-MSG-057`, `PF-TST-CROSS-123` | `PF-CERT-AUTHZ-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-061` | PFREQ044, PFREQ047, PFREQ050, PFREQ123 | `PF-06-INV-001`, `PF-GATE-002`, `PF-06-CORE-001`, `PF-06-COMPAT-001`, `PF-GOV-002` | `PF-TST-IDEM-044`, `PF-TST-IDEM-047`, `PF-TST-IDEM-050`, `PF-TST-CROSS-123` | `PF-CERT-IDEM-001`, `PF-CERT-MSG-001`, `PF-CERT-GLOBAL-001` |
| `BE-PLT-062` | PFREQ005, PFREQ030, PFREQ032, PFREQ054, PFREQ056, PFREQ120 | `PF-INV-002`, `PF-04-INV-001`, `PF-04-CORE-001`, `PF-07-CORE-001`, `PF-07-SEC-001`, `PF-16-COMPAT-001`, `PF-GATE-004` | `PF-TST-GLOBAL-005`, `PF-TST-DATA-030`, `PF-TST-DATA-032`, `PF-TST-MSG-054`, `PF-TST-MSG-056`, `PF-TST-CI-120` | `PF-CERT-DATA-001`, `PF-CERT-MSG-001`, `PF-CERT-CI-001` |
| `BE-PLT-063` | PFREQ005, PFREQ120, PFREQ124 | `PF-INV-002`, `PF-16-COMPAT-001`, `PF-GATE-004`, `PF-GOV-002` | `PF-TST-GLOBAL-005`, `PF-TST-CI-120`, `PF-TST-CROSS-124` | `PF-CERT-OBS-001`, `PF-CERT-CI-001`, `PF-CERT-GLOBAL-001` |

## 9. Lane source map

The paths below are the first inspection set for each lane. A coding agent must search direct callers/consumers before editing; this table is not permission to change every listed file.
| Lane | Current source/evidence paths | Current → target |
|---|---|---|
| PF-01 | `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`<br>`backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`<br>`backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs`<br>`frontend/packages/foundation/contracts/src/client/csrf.ts`<br>`frontend/packages/foundation/contracts/src/client/api-client.ts`<br>`frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts` | Backend and frontend already agree on HttpOnly `csrf_token` + bootstrap response body + `X-CSRF-Token`. Historical XSRF naming drift is not a current source gap.<br>**Target:** Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate. |
| PF-02 | `backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs`<br>`backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs`<br>`backend/src/Notrelix.Application/Common/Context/ICurrentRequestContext.cs`<br>`backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`<br>`backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs`<br>`frontend/packages/foundation/query/src/query-key-scope.ts`<br>`frontend/apps/web/src/providers/realtime-lifecycle.tsx` | Actor/Account/Workspace request context exists and is snapshotted into Application execution. The frontend account state boundary is incomplete because account-scoped query identity does not encode Account ID and no atomic transition reset is proven.<br>**Target:** Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable. |
| PF-03 | `backend/src/Notrelix.Application/DependencyInjection.cs`<br>`backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs`<br>`backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`<br>`backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`<br>`backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineRuntimeOrderTests.cs`<br>`backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`<br>`backend/tests/Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs` | The frozen seven-behavior pipeline is registered, DataSession precedes AccessControl, and representative production-graph authorization tests exist.<br>**Target:** Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths. |
| PF-04 | `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`<br>`backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs`<br>`backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`<br>`backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`<br>`backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`<br>`backend/tests/Notrelix.Integration.Tests/Data/ExpectedVersionConcurrencyIntegrationTests.cs` | Request transaction/data-session, RLS session context and expected-version support exist. RLS is defense-in-depth; worker scope remains path-specific.<br>**Target:** Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority. |
| PF-05 | `backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs`<br>`backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs`<br>`backend/src/Notrelix.Infrastructure/Data/Migrations/`<br>`backend/docs/operations/migrations-and-data-change.md`<br>`backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`<br>`backend/tests/Notrelix.Integration.Tests/Data/SeedDataInitialiserTests.cs`<br>`.github/workflows/backend-ci.yml` | EF migration/init discipline and governed rebaseline history exist. Supported-upgrade evidence depends on the claimed release boundary.<br>**Target:** For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure. |
| PF-06 | `backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs`<br>`backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs`<br>`backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs`<br>`backend/src/Notrelix.Application/Common/Idempotency/IIdempotencyStore.cs`<br>`backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs`<br>`backend/src/Notrelix.Infrastructure/DependencyInjection/OperationsRegistration.cs`<br>`backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs`<br>`backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs` | Operation identity, tenant partition, deterministic request fingerprint, persisted store and replay behavior are implemented.<br>**Target:** Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer. |
| PF-07 | `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`<br>`backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`<br>`backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs`<br>`backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`<br>`backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`<br>`backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs`<br>`backend/src/Notrelix.Infrastructure/Data/Configurations/Messaging/MessagingProcessedEventConfiguration.cs`<br>`backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`<br>`backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterFullIntegrationTests.cs`<br>`backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs` | Transactional outbox and durable `(EventId, ConsumerName)` consumer claims exist. Default dedup path is transactionally safe; the command-owned path commits `Processing` before command execution and only removes it on a normal exception. Process death can strand the unique Processing row.<br>**Target:** Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome. |
| PF-08 | `backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs`<br>`backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs`<br>`backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs`<br>`backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistry.cs`<br>`backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs`<br>`backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`<br>`backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`<br>`backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/OrderingEnforcerTests.cs`<br>`backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/PoisonDetectorTests.cs`<br>`backend/tests/Notrelix.Platform.Tests/Messaging/Consumers/ConsumerHostDeliveryContractTests.cs` | Ordering/poison reference mechanisms are implemented and unit-tested in `Notrelix.Platform`, but production RabbitMQ/MassTransit wiring is owned by Infrastructure. At the audited candidate no production `ConsumerDefinition` proves `OrderingRequired = true`.<br>**Target:** For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment. |
| PF-09 | `frontend/packages/foundation/realtime/src/transport/realtime-client.ts`<br>`frontend/apps/web/src/realtime/workspace-recovery-policy.ts`<br>`frontend/apps/web/src/providers/realtime-lifecycle.tsx`<br>`frontend/packages/foundation/query/src/query-key-scope.ts`<br>`frontend/packages/features/workspace/src/query/keys.ts`<br>`frontend/packages/features/notifications/src/query/keys.ts`<br>`frontend/docs/architecture/realtime.md` | RealtimeClient detects gaps, but `handleWorkspaceRecovery()` invalidates non-canonical key shapes and there is no explicit successful-recovery API that reconciles the sequence checkpoint. One gap can therefore trigger repeated recovery.<br>**Target:** Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes. |
| PF-10 | `backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs`<br>`backend/contracts/openapi/notrelix.v1.json`<br>`frontend/tooling/codegen/openapi/generate-openapi.mjs`<br>`frontend/packages/foundation/contracts/src/generated/rest/schema.ts`<br>`backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs`<br>`.github/workflows/backend-ci.yml` | Backend has canonical deterministic OpenAPI export and tracked contract; frontend generates REST types from that artifact.<br>**Target:** Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift. |
| PF-11 | `frontend/packages/foundation/query/src/query-key-scope.ts`<br>`frontend/packages/foundation/query/src/index.ts`<br>`frontend/packages/features/account/src/query/keys.ts`<br>`frontend/packages/features/workspace/src/query/keys.ts`<br>`frontend/packages/features/notifications/src/query/keys.ts`<br>`frontend/tooling/generators/create-feature/index.mjs`<br>`frontend/docs/architecture/state-query-mutations.md` | Three query roots exist and workspace keys encode Workspace ID. Account root does not encode Account ID, so Account A/B data can collide unless an unproven hard reset occurs.<br>**Target:** Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache. |
| PF-12 | `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`<br>`frontend/packages/runtimes/web/src/runtime/session-event-bus.ts`<br>`frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts`<br>`frontend/packages/runtimes/web/src/index.ts`<br>`frontend/packages/runtimes/mobile/`<br>`frontend/apps/web/src/providers/app-providers.tsx`<br>`frontend/apps/web/src/composition/application-services.ts`<br>`frontend/apps/web/src/main.tsx` | Web/mobile runtime packages and app composition roots exist; web runtime owns session/realtime/browser adapter composition.<br>**Target:** Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation. |
| PF-13 | `frontend/packages/ui/tokens/package.json`<br>`frontend/packages/ui/tokens/src/index.ts`<br>`frontend/packages/ui/tokens/src/colors.ts`<br>`frontend/packages/ui/tokens/src/semantic.ts`<br>`frontend/packages/ui/tokens/src/themes/`<br>`frontend/packages/ui/web/src/index.ts`<br>`frontend/packages/ui/web/src/components/`<br>`frontend/packages/ui/web/src/verification/ui-web-critical-surfaces.tsx`<br>`frontend/packages/ui/web/verification/ui-evidence.manifest.json` | Token TypeScript source is present, but package metadata advertises `./css -> ./src/css/index.css` and `src/css/` does not exist at the audited candidate. Audit found no current consumer of the CSS subpath.<br>**Target:** Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review. |
| PF-14 | `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`<br>`backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs`<br>`backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`<br>`backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs`<br>`frontend/packages/foundation/observability/src/init.ts`<br>`frontend/packages/foundation/observability/src/tracing.ts`<br>`frontend/packages/foundation/observability/src/telemetry/redaction.ts`<br>`frontend/packages/foundation/observability/src/telemetry/telemetry.ts` | Tracing, metrics, health and frontend telemetry/redaction mechanisms exist.<br>**Target:** Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation. |
| PF-15 | `backend/tests/Notrelix.Architecture.Tests/`<br>`backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`<br>`backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`<br>`frontend/tooling/dependency-rules/src/architecture-manifest.ts`<br>`frontend/tooling/dependency-rules/src/check-package-manifests.ts`<br>`frontend/tooling/dependency-rules/src/generate-architecture-docs.ts`<br>`frontend/docs/generated/package-boundaries.md` | Backend architecture tests and frontend closed-world dependency manifest/generation gates exist and are substantial.<br>**Target:** Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries. |
| PF-16 | `.github/workflows/backend-ci.yml`<br>`.github/workflows/container-ci.yml`<br>`backend/tests/ci-proofs.json`<br>`tools/deliveryctl/architecture.py`<br>`scripts/ci/validate-infra.py`<br>`docs/delivery/ci-cd-architecture.md` | Baseline exact-head CI evidence at `902bc9c5b39a003df3dce6673edc124984d2b398` is green (`CodeQL` run `36227690203`, `Notrelix CI` run `36227690334`); container workflow includes runtime non-root/health proof and Trivy/SBOM controls. These historical exact-head runs establish preparation posture only; final D5 still requires rerun/evidence bound to the accepted certification candidate.<br>**Target:** Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics. |

# 10. Work units

Every work unit must finish with the following evidence envelope:

```text
Work unit:
Candidate SHA:
Source posture before:
Source posture after:
Files inspected:
Files changed:
Tests changed/added:
Focused tests discovered/executed/passed/failed/skipped:
Production/runtime evidence:
Affected consumers:
Breaking/additive:
Migration/compatibility:
Blocking debt:
Invalidating changes:
Reviewer:
```

For any changed shared mechanism, also emit the full handoff in §13.

## PF-INV-001 — Candidate inventory and authority freeze

### Requirement coverage

```text
PFREQ001, PFREQ002, PFREQ003, PFREQ004
```

### Stable verification references

```text
PF-TST-GLOBAL-001, PF-TST-GLOBAL-002, PF-TST-GLOBAL-003, PF-TST-GLOBAL-004
```

### Source / authority to inspect

- `docs/workstreams/teams/platform-foundation.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `frontend/docs/architecture/state-query-mutations.md`
- `frontend/docs/architecture/realtime.md`
- `frontend/docs/architecture/ui-and-design-system.md`
- `docs/workstreams/executions/backend-team-architecture-closure/`

### Current behavior

The four-file package is orchestration authority below canonical architecture/ADR/TAC sources; preparation baseline is PR #160 head.

### Target behavior

Freeze exact candidate, authority order, lane owner, source posture and applicable BE-PLT/PF-FLOW evidence before implementation.

### Required actions

- Record candidate SHA and changed-file inventory.
- Diff canonical architecture/ADR/TAC authorities since the preparation baseline.
- Invalidate any lane map whose governing authority changed.

### Stop condition

Stop if two authorities conflict; resolve architecture ownership before code.

## PF-INV-002 — Consumer dependency inventory

### Requirement coverage

```text
PFREQ005, PFREQ006, PFREQ008
```

### Stable verification references

```text
PF-TST-GLOBAL-005, PF-TST-GLOBAL-006, PF-TST-GLOBAL-008
```

### Source / authority to inspect

- `docs/workstreams/cross-team-dependencies.md`
- `docs/workstreams/capability-delivery-map.md`
- `docs/workstreams/teams/platform-foundation.md`

### Current behavior

Consumers require different Platform readiness; blanket Platform-complete is forbidden. At the preparation baseline, some upstream workstream documents still reference the absent historical path `docs/workstreams/capability-map.md`; the active delivery authority is `docs/workstreams/capability-delivery-map.md`, so the former is classified `DOC_STALE` rather than recreated.

### Target behavior

Create a consumer → capability → required D-level matrix and block consumers whose required target is unmet.

### Required actions

- Map session/CSRF, tenant context, authz, idempotency, messaging, realtime, account isolation, generated contracts, UI exports and CI/architecture gates to actual workstreams.
- Record invalidating changes and consumer rerun set.

### Stop condition

Stop any consumer handoff that relies on a generic lane status instead of its exact dependency contract.

## PF-GOV-001 — Execution slicing

### Requirement coverage

```text
PFREQ007, PFREQ128
```

### Stable verification references

```text
PF-TST-GLOBAL-007, PF-TST-CROSS-128
```

### Source / authority to inspect

- `docs/workstreams/teams/platform-foundation.md`
- `docs/delivery/change-classification.md`
- `docs/architecture/capability-extraction-strategy.md`

### Current behavior

Platform work spans many cross-cutting areas and can easily become an unbounded cleanup PR.

### Target behavior

Keep lane-scoped slices and explicit wave/gate handoffs; service extraction remains bounded-context + Architecture authority.

### Required actions

- Reject unrelated cleanup from lane PRs.
- Escalate new framework/project/service/global-state/auth architecture/delivery guarantee/security weakening before implementation.

### Stop condition

Stop on any escalation-class change until approval/ADR exists.

## PF-GOV-002 — Cross-cutting invariant register

### Requirement coverage

```text
PFREQ121, PFREQ122, PFREQ123, PFREQ124, PFREQ125, PFREQ126, PFREQ127
```

### Stable verification references

```text
PF-TST-CROSS-121, PF-TST-CROSS-122, PF-TST-CROSS-123, PF-TST-CROSS-124, PF-TST-CROSS-125, PF-TST-CROSS-126, PF-TST-CROSS-127
```

### Source / authority to inspect

- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/operations/observability.md`
- `docs/delivery/migration-policy.md`

### Current behavior

Secret safety, tenant isolation, retry/idempotency safety, failure transparency, bounded performance, compatibility and source-of-truth rules cut across lanes.

### Target behavior

Attach cross-cutting invariants to every changed lane and require deliberate failure proof.

### Required actions

- For every changed work unit, record PFREQ121–127 applicability.
- Require explicit `NOT_APPLICABLE` rationale rather than silent omission.

### Stop condition

Stop if a lane fix would weaken a cross-cutting invariant.

## PF-01-INV-001 — Session and CSRF transport candidate inventory

### Requirement coverage

```text
PFREQ009, PFREQ010
```

### Stable verification references

```text
PF-TST-CSRF-009, PF-TST-CSRF-010
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
- `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs`
- `frontend/packages/foundation/contracts/src/client/csrf.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`
- `frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts`

### Current behavior

Backend and frontend already agree on HttpOnly `csrf_token` + bootstrap response body + `X-CSRF-Token`. Historical XSRF naming drift is not a current source gap.

### Target behavior

Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ009` — Canonical browser CSRF contract
- `PFREQ010` — Unsafe-request classification

### Minimum allowed change

Do not rename the cookie/header or redesign session transport unless candidate evidence shows a real defect. Harden only the concrete failing contract/test.

### Non-goals

No feature-local CSRF implementation; no localStorage/sessionStorage token persistence; no second legacy XSRF convention.

### Compatibility / migration impact

Any cookie/header/bootstrap or credential change is a backend + frontend contract change and must update API/frontend tests atomically.

### Verification and exit evidence

- Execute and record `PF-TST-CSRF-009, PF-TST-CSRF-010` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if closing PF-01 requires weakening SameSite/Secure/origin policy or creating a second CSRF protocol.

## PF-01-CORE-001 — Session and CSRF transport mechanism closure

### Requirement coverage

```text
PFREQ011, PFREQ012
```

### Stable verification references

```text
PF-TST-CSRF-011, PF-TST-CSRF-012
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
- `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs`
- `frontend/packages/foundation/contracts/src/client/csrf.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`
- `frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts`

### Current behavior

Backend and frontend already agree on HttpOnly `csrf_token` + bootstrap response body + `X-CSRF-Token`. Historical XSRF naming drift is not a current source gap.

### Target behavior

Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ011` — Token secrecy and storage
- `PFREQ012` — Valid and invalid pair behavior

### Minimum allowed change

Do not rename the cookie/header or redesign session transport unless candidate evidence shows a real defect. Harden only the concrete failing contract/test.

### Non-goals

No feature-local CSRF implementation; no localStorage/sessionStorage token persistence; no second legacy XSRF convention.

### Compatibility / migration impact

Any cookie/header/bootstrap or credential change is a backend + frontend contract change and must update API/frontend tests atomically.

### Verification and exit evidence

- Execute and record `PF-TST-CSRF-011, PF-TST-CSRF-012` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if closing PF-01 requires weakening SameSite/Secure/origin policy or creating a second CSRF protocol.

## PF-01-SEC-001 — Session and CSRF transport security/isolation/failure closure

### Requirement coverage

```text
PFREQ013, PFREQ014
```

### Stable verification references

```text
PF-TST-CSRF-013, PF-TST-CSRF-014
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
- `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs`
- `frontend/packages/foundation/contracts/src/client/csrf.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`
- `frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts`

### Current behavior

Backend and frontend already agree on HttpOnly `csrf_token` + bootstrap response body + `X-CSRF-Token`. Historical XSRF naming drift is not a current source gap.

### Target behavior

Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ013` — Session-expiry behavior
- `PFREQ014` — Cross-origin production policy

### Minimum allowed change

Do not rename the cookie/header or redesign session transport unless candidate evidence shows a real defect. Harden only the concrete failing contract/test.

### Non-goals

No feature-local CSRF implementation; no localStorage/sessionStorage token persistence; no second legacy XSRF convention.

### Compatibility / migration impact

Any cookie/header/bootstrap or credential change is a backend + frontend contract change and must update API/frontend tests atomically.

### Verification and exit evidence

- Execute and record `PF-TST-CSRF-013, PF-TST-CSRF-014` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if closing PF-01 requires weakening SameSite/Secure/origin policy or creating a second CSRF protocol.

## PF-01-COMPAT-001 — Session and CSRF transport compatibility and consumer handoff

### Requirement coverage

```text
PFREQ015
```

### Stable verification references

```text
PF-TST-CSRF-015
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs`
- `backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs`
- `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs`
- `frontend/packages/foundation/contracts/src/client/csrf.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`
- `frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts`

### Current behavior

Backend and frontend already agree on HttpOnly `csrf_token` + bootstrap response body + `X-CSRF-Token`. Historical XSRF naming drift is not a current source gap.

### Target behavior

Retain one browser CSRF protocol, prove unsafe-request classification, session-expiry behavior and approved cross-origin credentials on the exact candidate.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ015` — CSRF contract compatibility evidence

### Minimum allowed change

Do not rename the cookie/header or redesign session transport unless candidate evidence shows a real defect. Harden only the concrete failing contract/test.

### Non-goals

No feature-local CSRF implementation; no localStorage/sessionStorage token persistence; no second legacy XSRF convention.

### Compatibility / migration impact

Any cookie/header/bootstrap or credential change is a backend + frontend contract change and must update API/frontend tests atomically.

### Verification and exit evidence

- Execute and record `PF-TST-CSRF-015` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if closing PF-01 requires weakening SameSite/Secure/origin policy or creating a second CSRF protocol.

## PF-02-INV-001 — Actor / Account / Workspace context propagation candidate inventory

### Requirement coverage

```text
PFREQ016, PFREQ017
```

### Stable verification references

```text
PF-TST-CTX-016, PF-TST-CTX-017
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs`
- `backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Context/ICurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`

### Current behavior

Actor/Account/Workspace request context exists and is snapshotted into Application execution. The frontend account state boundary is incomplete because account-scoped query identity does not encode Account ID and no atomic transition reset is proven.

### Target behavior

Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ016` — Distinct context identities
- `PFREQ017` — Trusted HTTP context resolution

### Minimum allowed change

Prefer adding explicit Account identity to account-scoped keys and migrating call sites; an alternative hard-reset design is acceptable only if it is atomic and fully proven before activation of the next account.

### Non-goals

No mutable global tenant singleton, no client-supplied tenant trust, no implicit System authority for background work.

### Compatibility / migration impact

Changing account key shape invalidates caches, query helpers, generators and all account-scoped feature key factories; migration must be repository-wide and atomic.

### Verification and exit evidence

- Execute and record `PF-TST-CTX-016, PF-TST-CTX-017` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if Account ownership/transition semantics are ambiguous or if a proposed fix relies on eventual cache clearing after Account B becomes active.

## PF-02-CORE-001 — Actor / Account / Workspace context propagation mechanism closure

### Requirement coverage

```text
PFREQ018, PFREQ019
```

### Stable verification references

```text
PF-TST-CTX-018, PF-TST-CTX-019
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs`
- `backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Context/ICurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`

### Current behavior

Actor/Account/Workspace request context exists and is snapshotted into Application execution. The frontend account state boundary is incomplete because account-scoped query identity does not encode Account ID and no atomic transition reset is proven.

### Target behavior

Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ018` — ExecutionContext immutability
- `PFREQ019` — Background scope reconstruction

### Minimum allowed change

Prefer adding explicit Account identity to account-scoped keys and migrating call sites; an alternative hard-reset design is acceptable only if it is atomic and fully proven before activation of the next account.

### Non-goals

No mutable global tenant singleton, no client-supplied tenant trust, no implicit System authority for background work.

### Compatibility / migration impact

Changing account key shape invalidates caches, query helpers, generators and all account-scoped feature key factories; migration must be repository-wide and atomic.

### Verification and exit evidence

- Execute and record `PF-TST-CTX-018, PF-TST-CTX-019` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if Account ownership/transition semantics are ambiguous or if a proposed fix relies on eventual cache clearing after Account B becomes active.

## PF-02-SEC-001 — Actor / Account / Workspace context propagation security/isolation/failure closure

### Requirement coverage

```text
PFREQ020, PFREQ021
```

### Stable verification references

```text
PF-TST-CTX-020, PF-TST-CTX-021
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs`
- `backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Context/ICurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`

### Current behavior

Actor/Account/Workspace request context exists and is snapshotted into Application execution. The frontend account state boundary is incomplete because account-scoped query identity does not encode Account ID and no atomic transition reset is proven.

### Target behavior

Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ020` — Account transition state isolation
- `PFREQ021` — Workspace query partitioning

### Minimum allowed change

Prefer adding explicit Account identity to account-scoped keys and migrating call sites; an alternative hard-reset design is acceptable only if it is atomic and fully proven before activation of the next account.

### Non-goals

No mutable global tenant singleton, no client-supplied tenant trust, no implicit System authority for background work.

### Compatibility / migration impact

Changing account key shape invalidates caches, query helpers, generators and all account-scoped feature key factories; migration must be repository-wide and atomic.

### Verification and exit evidence

- Execute and record `PF-TST-CTX-020, PF-TST-CTX-021` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if Account ownership/transition semantics are ambiguous or if a proposed fix relies on eventual cache clearing after Account B becomes active.

## PF-02-COMPAT-001 — Actor / Account / Workspace context propagation compatibility and consumer handoff

### Requirement coverage

```text
PFREQ022
```

### Stable verification references

```text
PF-TST-CTX-022
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs`
- `backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Context/ICurrentRequestContext.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`

### Current behavior

Actor/Account/Workspace request context exists and is snapshotted into Application execution. The frontend account state boundary is incomplete because account-scoped query identity does not encode Account ID and no atomic transition reset is proven.

### Target behavior

Preserve distinct Actor/Account/Workspace identities in HTTP and workers and make Account A → B → A client-state isolation mechanically provable.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.
- Choose and freeze the Account isolation strategy before editing call sites. Preferred source-level closure: change `accountQueryKey(resource, ...segments)` to require `accountId` and return `['account', accountId, resource, ...]`.
- Update `frontend/packages/features/account/src/query/keys.ts`, `workspace/src/query/keys.ts`, `notifications/src/query/keys.ts` and every repository call site returned by search.
- Update `frontend/tooling/generators/create-feature/index.mjs` so new features cannot recreate the old unpartitioned shape.
- Add migration-time A → B → A proof; no Account B activation may occur while Account A cache/optimistic/permission/realtime state is still addressable through the active Account scope.

Covered requirement intent:
- `PFREQ022` — Account query-key closure

### Minimum allowed change

Prefer adding explicit Account identity to account-scoped keys and migrating call sites; an alternative hard-reset design is acceptable only if it is atomic and fully proven before activation of the next account.

### Non-goals

No mutable global tenant singleton, no client-supplied tenant trust, no implicit System authority for background work.

### Compatibility / migration impact

Changing account key shape invalidates caches, query helpers, generators and all account-scoped feature key factories; migration must be repository-wide and atomic.

### Verification and exit evidence

- Execute and record `PF-TST-CTX-022` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if Account ownership/transition semantics are ambiguous or if a proposed fix relies on eventual cache clearing after Account B becomes active.

## PF-03-INV-001 — Authorization enforcement pipeline candidate inventory

### Requirement coverage

```text
PFREQ023, PFREQ024
```

### Stable verification references

```text
PF-TST-AUTHZ-023, PF-TST-AUTHZ-024
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineRuntimeOrderTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`

### Current behavior

The frozen seven-behavior pipeline is registered, DataSession precedes AccessControl, and representative production-graph authorization tests exist.

### Target behavior

Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ023` — Single enforcement pipeline
- `PFREQ024` — Policy ownership separation

### Minimum allowed change

Fix only a demonstrated ordering/facts/guard defect; do not move permission policy into Platform.

### Non-goals

No second AuthorizationBehavior/PermissionService authority; no handler-local RBAC as canonical enforcement.

### Compatibility / migration impact

Pipeline ordering or request descriptor/facts changes affect all protected bounded contexts and require architecture + representative integration reruns.

### Verification and exit evidence

- Execute and record `PF-TST-AUTHZ-023, PF-TST-AUTHZ-024` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on any authorization-architecture redesign or security weakening; escalate before implementation.

## PF-03-CORE-001 — Authorization enforcement pipeline mechanism closure

### Requirement coverage

```text
PFREQ025, PFREQ026
```

### Stable verification references

```text
PF-TST-AUTHZ-025, PF-TST-AUTHZ-026
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineRuntimeOrderTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`

### Current behavior

The frozen seven-behavior pipeline is registered, DataSession precedes AccessControl, and representative production-graph authorization tests exist.

### Target behavior

Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ025` — Facts before policy
- `PFREQ026` — DataSession ordering

### Minimum allowed change

Fix only a demonstrated ordering/facts/guard defect; do not move permission policy into Platform.

### Non-goals

No second AuthorizationBehavior/PermissionService authority; no handler-local RBAC as canonical enforcement.

### Compatibility / migration impact

Pipeline ordering or request descriptor/facts changes affect all protected bounded contexts and require architecture + representative integration reruns.

### Verification and exit evidence

- Execute and record `PF-TST-AUTHZ-025, PF-TST-AUTHZ-026` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on any authorization-architecture redesign or security weakening; escalate before implementation.

## PF-03-SEC-001 — Authorization enforcement pipeline security/isolation/failure closure

### Requirement coverage

```text
PFREQ027, PFREQ028
```

### Stable verification references

```text
PF-TST-AUTHZ-027, PF-TST-AUTHZ-028
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineRuntimeOrderTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`

### Current behavior

The frozen seven-behavior pipeline is registered, DataSession precedes AccessControl, and representative production-graph authorization tests exist.

### Target behavior

Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ027` — Unauthorized side-effect exclusion
- `PFREQ028` — System-internal exception

### Minimum allowed change

Fix only a demonstrated ordering/facts/guard defect; do not move permission policy into Platform.

### Non-goals

No second AuthorizationBehavior/PermissionService authority; no handler-local RBAC as canonical enforcement.

### Compatibility / migration impact

Pipeline ordering or request descriptor/facts changes affect all protected bounded contexts and require architecture + representative integration reruns.

### Verification and exit evidence

- Execute and record `PF-TST-AUTHZ-027, PF-TST-AUTHZ-028` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on any authorization-architecture redesign or security weakening; escalate before implementation.

## PF-03-COMPAT-001 — Authorization enforcement pipeline compatibility and consumer handoff

### Requirement coverage

```text
PFREQ029
```

### Stable verification references

```text
PF-TST-AUTHZ-029
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.Application/DependencyInjection.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs`
- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineRuntimeOrderTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`

### Current behavior

The frozen seven-behavior pipeline is registered, DataSession precedes AccessControl, and representative production-graph authorization tests exist.

### Target behavior

Retain one enforcement pipeline, server-side facts and side-effect exclusion for denied requests; exact-candidate proof must cover ordinary and System-internal paths.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ029` — Architecture guard

### Minimum allowed change

Fix only a demonstrated ordering/facts/guard defect; do not move permission policy into Platform.

### Non-goals

No second AuthorizationBehavior/PermissionService authority; no handler-local RBAC as canonical enforcement.

### Compatibility / migration impact

Pipeline ordering or request descriptor/facts changes affect all protected bounded contexts and require architecture + representative integration reruns.

### Verification and exit evidence

- Execute and record `PF-TST-AUTHZ-029` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on any authorization-architecture redesign or security weakening; escalate before implementation.

## PF-04-INV-001 — Persistence and tenant-isolation foundation candidate inventory

### Requirement coverage

```text
PFREQ030, PFREQ031
```

### Stable verification references

```text
PF-TST-DATA-030, PF-TST-DATA-031
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D5 where tenant persistence is required
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/ExpectedVersionConcurrencyIntegrationTests.cs`

### Current behavior

Request transaction/data-session, RLS session context and expected-version support exist. RLS is defense-in-depth; worker scope remains path-specific.

### Target behavior

Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ030` — Request data-session boundary
- `PFREQ031` — Owner-local persistence

### Minimum allowed change

Retain owner-local repositories/DbContexts and existing DataSession boundary; harden only missing worker/RLS/concurrency evidence.

### Non-goals

No universal business DbContext/repository exposed to Application; no RLS-as-authorization claim; no broad worker bypass.

### Compatibility / migration impact

RLS/session-variable or transaction-semantics changes require migration/runtime evidence and consumer-specific worker revalidation.

### Verification and exit evidence

- Execute and record `PF-TST-DATA-030, PF-TST-DATA-031` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a shared persistence helper requires cross-context private-table ownership or unrestricted System scope.

## PF-04-CORE-001 — Persistence and tenant-isolation foundation mechanism closure

### Requirement coverage

```text
PFREQ032, PFREQ033
```

### Stable verification references

```text
PF-TST-DATA-032, PF-TST-DATA-033
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D5 where tenant persistence is required
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/ExpectedVersionConcurrencyIntegrationTests.cs`

### Current behavior

Request transaction/data-session, RLS session context and expected-version support exist. RLS is defense-in-depth; worker scope remains path-specific.

### Target behavior

Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ032` — RLS session context
- `PFREQ033` — RLS defense-in-depth

### Minimum allowed change

Retain owner-local repositories/DbContexts and existing DataSession boundary; harden only missing worker/RLS/concurrency evidence.

### Non-goals

No universal business DbContext/repository exposed to Application; no RLS-as-authorization claim; no broad worker bypass.

### Compatibility / migration impact

RLS/session-variable or transaction-semantics changes require migration/runtime evidence and consumer-specific worker revalidation.

### Verification and exit evidence

- Execute and record `PF-TST-DATA-032, PF-TST-DATA-033` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a shared persistence helper requires cross-context private-table ownership or unrestricted System scope.

## PF-04-SEC-001 — Persistence and tenant-isolation foundation security/isolation/failure closure

### Requirement coverage

```text
PFREQ034, PFREQ035
```

### Stable verification references

```text
PF-TST-DATA-034, PF-TST-DATA-035
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D5 where tenant persistence is required
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/ExpectedVersionConcurrencyIntegrationTests.cs`

### Current behavior

Request transaction/data-session, RLS session context and expected-version support exist. RLS is defense-in-depth; worker scope remains path-specific.

### Target behavior

Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ034` — Cross-context private-table rule
- `PFREQ035` — Concurrency/version support

### Minimum allowed change

Retain owner-local repositories/DbContexts and existing DataSession boundary; harden only missing worker/RLS/concurrency evidence.

### Non-goals

No universal business DbContext/repository exposed to Application; no RLS-as-authorization claim; no broad worker bypass.

### Compatibility / migration impact

RLS/session-variable or transaction-semantics changes require migration/runtime evidence and consumer-specific worker revalidation.

### Verification and exit evidence

- Execute and record `PF-TST-DATA-034, PF-TST-DATA-035` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a shared persistence helper requires cross-context private-table ownership or unrestricted System scope.

## PF-04-COMPAT-001 — Persistence and tenant-isolation foundation compatibility and consumer handoff

### Requirement coverage

```text
PFREQ036
```

### Stable verification references

```text
PF-TST-DATA-036
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D5 where tenant persistence is required
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/ExpectedVersionConcurrencyIntegrationTests.cs`

### Current behavior

Request transaction/data-session, RLS session context and expected-version support exist. RLS is defense-in-depth; worker scope remains path-specific.

### Target behavior

Prove request and worker persistence execute under explicit transaction/RLS scope without creating a generic cross-context persistence authority.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ036` — Worker persistence scope

### Minimum allowed change

Retain owner-local repositories/DbContexts and existing DataSession boundary; harden only missing worker/RLS/concurrency evidence.

### Non-goals

No universal business DbContext/repository exposed to Application; no RLS-as-authorization claim; no broad worker bypass.

### Compatibility / migration impact

RLS/session-variable or transaction-semantics changes require migration/runtime evidence and consumer-specific worker revalidation.

### Verification and exit evidence

- Execute and record `PF-TST-DATA-036` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a shared persistence helper requires cross-context private-table ownership or unrestricted System scope.

## PF-05-INV-001 — Migration and database initialization candidate inventory

### Requirement coverage

```text
PFREQ037, PFREQ038
```

### Stable verification references

```text
PF-TST-MIG-037, PF-TST-MIG-038
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5 for schema-affecting release boundary
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- `backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs`
- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/docs/operations/migrations-and-data-change.md`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/SeedDataInitialiserTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

EF migration/init discipline and governed rebaseline history exist. Supported-upgrade evidence depends on the claimed release boundary.

### Target behavior

For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ037` — Migration authority
- `PFREQ038` — Clean database proof

### Minimum allowed change

Add/fix migration or initialization evidence only for an intentional model change; never suppress source/schema divergence to obtain green startup.

### Non-goals

No ad-hoc schema mutation at runtime, no hand-edited migration history to hide drift, no blanket upgrade claim without a supported predecessor.

### Compatibility / migration impact

Schema, RLS policy and generated migration changes require explicit forward/rollback or governed rebaseline disposition.

### Verification and exit evidence

- Execute and record `PF-TST-MIG-037, PF-TST-MIG-038` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification on unresolved model drift or `PendingModelChangesWarning` in production startup semantics.

## PF-05-CORE-001 — Migration and database initialization mechanism closure

### Requirement coverage

```text
PFREQ039, PFREQ040
```

### Stable verification references

```text
PF-TST-MIG-039, PF-TST-MIG-040
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5 for schema-affecting release boundary
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- `backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs`
- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/docs/operations/migrations-and-data-change.md`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/SeedDataInitialiserTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

EF migration/init discipline and governed rebaseline history exist. Supported-upgrade evidence depends on the claimed release boundary.

### Target behavior

For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ039` — Supported upgrade proof
- `PFREQ040` — Pending model changes

### Minimum allowed change

Add/fix migration or initialization evidence only for an intentional model change; never suppress source/schema divergence to obtain green startup.

### Non-goals

No ad-hoc schema mutation at runtime, no hand-edited migration history to hide drift, no blanket upgrade claim without a supported predecessor.

### Compatibility / migration impact

Schema, RLS policy and generated migration changes require explicit forward/rollback or governed rebaseline disposition.

### Verification and exit evidence

- Execute and record `PF-TST-MIG-039, PF-TST-MIG-040` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification on unresolved model drift or `PendingModelChangesWarning` in production startup semantics.

## PF-05-SEC-001 — Migration and database initialization security/isolation/failure closure

### Requirement coverage

```text
PFREQ041, PFREQ042
```

### Stable verification references

```text
PF-TST-MIG-041, PF-TST-MIG-042
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5 for schema-affecting release boundary
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- `backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs`
- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/docs/operations/migrations-and-data-change.md`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/SeedDataInitialiserTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

EF migration/init discipline and governed rebaseline history exist. Supported-upgrade evidence depends on the claimed release boundary.

### Target behavior

For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ041` — RLS deployment order
- `PFREQ042` — Initialization failure visibility

### Minimum allowed change

Add/fix migration or initialization evidence only for an intentional model change; never suppress source/schema divergence to obtain green startup.

### Non-goals

No ad-hoc schema mutation at runtime, no hand-edited migration history to hide drift, no blanket upgrade claim without a supported predecessor.

### Compatibility / migration impact

Schema, RLS policy and generated migration changes require explicit forward/rollback or governed rebaseline disposition.

### Verification and exit evidence

- Execute and record `PF-TST-MIG-041, PF-TST-MIG-042` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification on unresolved model drift or `PendingModelChangesWarning` in production startup semantics.

## PF-05-COMPAT-001 — Migration and database initialization compatibility and consumer handoff

### Requirement coverage

```text
PFREQ043
```

### Stable verification references

```text
PF-TST-MIG-043
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5 for schema-affecting release boundary
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- `backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs`
- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/docs/operations/migrations-and-data-change.md`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/SeedDataInitialiserTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

EF migration/init discipline and governed rebaseline history exist. Supported-upgrade evidence depends on the claimed release boundary.

### Target behavior

For each schema-affecting candidate, prove clean DB creation, supported upgrade/rebaseline, RLS deployment order, seed/init behavior and visible failure.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ043` — Migration discipline

### Minimum allowed change

Add/fix migration or initialization evidence only for an intentional model change; never suppress source/schema divergence to obtain green startup.

### Non-goals

No ad-hoc schema mutation at runtime, no hand-edited migration history to hide drift, no blanket upgrade claim without a supported predecessor.

### Compatibility / migration impact

Schema, RLS policy and generated migration changes require explicit forward/rollback or governed rebaseline disposition.

### Verification and exit evidence

- Execute and record `PF-TST-MIG-043` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification on unresolved model drift or `PendingModelChangesWarning` in production startup semantics.

## PF-06-INV-001 — Idempotency candidate inventory

### Requirement coverage

```text
PFREQ044, PFREQ045
```

### Stable verification references

```text
PF-TST-IDEM-044, PF-TST-IDEM-045
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4–D5 by governed command consumer
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/OperationsRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs`

### Current behavior

Operation identity, tenant partition, deterministic request fingerprint, persisted store and replay behavior are implemented.

### Target behavior

Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ044` — Explicit operation identity
- `PFREQ045` — Tenant partitioning

### Minimum allowed change

Retain generic mechanism; product teams still define the business operation identity and whether a command is idempotent.

### Non-goals

No global request hash as business identity; no implicit idempotency for non-idempotent effects; no transport retry treated as a new operation.

### Compatibility / migration impact

Fingerprint, partition or completion-state changes can invalidate historical records and require explicit reconciliation/retention review.

### Verification and exit evidence

- Execute and record `PF-TST-IDEM-044, PF-TST-IDEM-045` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a consumer cannot define stable operation identity or if retries can multiply external/financial effects without a total attempt model.

## PF-06-CORE-001 — Idempotency mechanism closure

### Requirement coverage

```text
PFREQ046, PFREQ047
```

### Stable verification references

```text
PF-TST-IDEM-046, PF-TST-IDEM-047
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4–D5 by governed command consumer
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/OperationsRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs`

### Current behavior

Operation identity, tenant partition, deterministic request fingerprint, persisted store and replay behavior are implemented.

### Target behavior

Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ046` — Payload fingerprint
- `PFREQ047` — Concurrent duplicate coordination

### Minimum allowed change

Retain generic mechanism; product teams still define the business operation identity and whether a command is idempotent.

### Non-goals

No global request hash as business identity; no implicit idempotency for non-idempotent effects; no transport retry treated as a new operation.

### Compatibility / migration impact

Fingerprint, partition or completion-state changes can invalidate historical records and require explicit reconciliation/retention review.

### Verification and exit evidence

- Execute and record `PF-TST-IDEM-046, PF-TST-IDEM-047` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a consumer cannot define stable operation identity or if retries can multiply external/financial effects without a total attempt model.

## PF-06-SEC-001 — Idempotency security/isolation/failure closure

### Requirement coverage

```text
PFREQ048, PFREQ049
```

### Stable verification references

```text
PF-TST-IDEM-048, PF-TST-IDEM-049
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4–D5 by governed command consumer
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/OperationsRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs`

### Current behavior

Operation identity, tenant partition, deterministic request fingerprint, persisted store and replay behavior are implemented.

### Target behavior

Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ048` — Success replay
- `PFREQ049` — Failure/retry semantics

### Minimum allowed change

Retain generic mechanism; product teams still define the business operation identity and whether a command is idempotent.

### Non-goals

No global request hash as business identity; no implicit idempotency for non-idempotent effects; no transport retry treated as a new operation.

### Compatibility / migration impact

Fingerprint, partition or completion-state changes can invalidate historical records and require explicit reconciliation/retention review.

### Verification and exit evidence

- Execute and record `PF-TST-IDEM-048, PF-TST-IDEM-049` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a consumer cannot define stable operation identity or if retries can multiply external/financial effects without a total attempt model.

## PF-06-COMPAT-001 — Idempotency compatibility and consumer handoff

### Requirement coverage

```text
PFREQ050
```

### Stable verification references

```text
PF-TST-IDEM-050
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4–D5 by governed command consumer
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs`
- `backend/src/Notrelix.Application/Common/Idempotency/IIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/OperationsRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs`
- `backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs`

### Current behavior

Operation identity, tenant partition, deterministic request fingerprint, persisted store and replay behavior are implemented.

### Target behavior

Prove same operation/same payload replay, conflicting payload rejection, concurrent duplicate coordination and safe failure/retry semantics per producer.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ050` — Producer ownership

### Minimum allowed change

Retain generic mechanism; product teams still define the business operation identity and whether a command is idempotent.

### Non-goals

No global request hash as business identity; no implicit idempotency for non-idempotent effects; no transport retry treated as a new operation.

### Compatibility / migration impact

Fingerprint, partition or completion-state changes can invalidate historical records and require explicit reconciliation/retention review.

### Verification and exit evidence

- Execute and record `PF-TST-IDEM-050` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if a consumer cannot define stable operation identity or if retries can multiply external/financial effects without a total attempt model.

## PF-07-INV-001 — Outbox, consumer deduplication and message identity candidate inventory

### Requirement coverage

```text
PFREQ051, PFREQ052
```

### Stable verification references

```text
PF-TST-MSG-051, PF-TST-MSG-052
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 for async consumers
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`
- `backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Messaging/MessagingProcessedEventConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterFullIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs`

### Current behavior

Transactional outbox and durable `(EventId, ConsumerName)` consumer claims exist. Default dedup path is transactionally safe; the command-owned path commits `Processing` before command execution and only removes it on a normal exception. Process death can strand the unique Processing row.

### Target behavior

Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ051` — Transactional outbox
- `PFREQ052` — Stable message identity

### Minimum allowed change

Do not replace the durable inbox/dedup system. Extend the command-owned claim lifecycle with a bounded stale/recovery rule, preferably reusing `ClaimedAt` and store-level compare/update/delete semantics, then add crash-boundary integration proof.

### Non-goals

No delete-all dedup recovery, no new EventId on retry, no global message-only completion bit, no product semantics in the filter.

### Compatibility / migration impact

Claim-state/retention semantics affect dedup history and all command-owned consumers. Existing `Succeeded` records must remain valid; stale `Processing` reclamation must not steal live work.

### Verification and exit evidence

- Execute and record `PF-TST-MSG-051, PF-TST-MSG-052` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if stale threshold/ownership cannot distinguish interrupted work from an active command, or if recovery requires clearing durable history globally.

## PF-07-CORE-001 — Outbox, consumer deduplication and message identity mechanism closure

### Requirement coverage

```text
PFREQ053, PFREQ054
```

### Stable verification references

```text
PF-TST-MSG-053, PF-TST-MSG-054
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 for async consumers
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`
- `backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Messaging/MessagingProcessedEventConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterFullIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs`

### Current behavior

Transactional outbox and durable `(EventId, ConsumerName)` consumer claims exist. Default dedup path is transactionally safe; the command-owned path commits `Processing` before command execution and only removes it on a normal exception. Process death can strand the unique Processing row.

### Target behavior

Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.
- Retain `MessagingProcessedEvent` and unique `(EventId, ConsumerName)` durable identity; the issue is not absence of a durable inbox.
- Extend `IMessageDeduplicationStore` / `MessageDeduplicationStore` with an explicit stale command-owned claim decision that uses persisted `ClaimedAt` and an owner-safe compare/update/delete operation.
- Change `DeduplicationConsumeFilter.CommandOwnedSendAsync` so a redelivery encountering `Processing` can distinguish live claim, stale/interrupted claim and `Succeeded`; a stale claim must become recoverable without executing two concurrent business effects.
- Do not reuse the N8n provider-effect protocol blindly: its ADR-008 lifecycle has different out-of-transaction effect semantics. Reuse only generic claim primitives that preserve command-owned DataSession behavior.

Covered requirement intent:
- `PFREQ053` — Consumer identity
- `PFREQ054` — Outbox claiming and reclaim

### Minimum allowed change

Do not replace the durable inbox/dedup system. Extend the command-owned claim lifecycle with a bounded stale/recovery rule, preferably reusing `ClaimedAt` and store-level compare/update/delete semantics, then add crash-boundary integration proof.

### Non-goals

No delete-all dedup recovery, no new EventId on retry, no global message-only completion bit, no product semantics in the filter.

### Compatibility / migration impact

Claim-state/retention semantics affect dedup history and all command-owned consumers. Existing `Succeeded` records must remain valid; stale `Processing` reclamation must not steal live work.

### Verification and exit evidence

- Execute and record `PF-TST-MSG-053, PF-TST-MSG-054` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if stale threshold/ownership cannot distinguish interrupted work from an active command, or if recovery requires clearing durable history globally.

## PF-07-SEC-001 — Outbox, consumer deduplication and message identity security/isolation/failure closure

### Requirement coverage

```text
PFREQ055, PFREQ056
```

### Stable verification references

```text
PF-TST-MSG-055, PF-TST-MSG-056
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 for async consumers
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`
- `backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Messaging/MessagingProcessedEventConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterFullIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs`

### Current behavior

Transactional outbox and durable `(EventId, ConsumerName)` consumer claims exist. Default dedup path is transactionally safe; the command-owned path commits `Processing` before command execution and only removes it on a normal exception. Process death can strand the unique Processing row.

### Target behavior

Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.
- Add a production-graph interruption test: persist `Processing`, simulate process loss before `next.Send` settlement/cleanup, redeliver after the governed stale window, and assert the effect is not permanently suppressed.
- Prove exactly one final business outcome and one terminal `Succeeded` claim after recovery; also prove a non-stale live claim is not stolen.
- Re-run command-owned cleanup regression proving failed MediatR tracked entities are detached before claim cleanup so rollback state cannot be re-committed.

Covered requirement intent:
- `PFREQ055` — Delivery, dead-letter and recovery state visibility
- `PFREQ056` — Durable consumer claim/dedup discipline

### Minimum allowed change

Do not replace the durable inbox/dedup system. Extend the command-owned claim lifecycle with a bounded stale/recovery rule, preferably reusing `ClaimedAt` and store-level compare/update/delete semantics, then add crash-boundary integration proof.

### Non-goals

No delete-all dedup recovery, no new EventId on retry, no global message-only completion bit, no product semantics in the filter.

### Compatibility / migration impact

Claim-state/retention semantics affect dedup history and all command-owned consumers. Existing `Succeeded` records must remain valid; stale `Processing` reclamation must not steal live work.

### Verification and exit evidence

- Execute and record `PF-TST-MSG-055, PF-TST-MSG-056` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if stale threshold/ownership cannot distinguish interrupted work from an active command, or if recovery requires clearing durable history globally.

## PF-07-COMPAT-001 — Outbox, consumer deduplication and message identity compatibility and consumer handoff

### Requirement coverage

```text
PFREQ057
```

### Stable verification references

```text
PF-TST-MSG-057
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 for async consumers
```

### Source to inspect

- `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs`
- `backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Messaging/MessagingProcessedEventConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterFullIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs`

### Current behavior

Transactional outbox and durable `(EventId, ConsumerName)` consumer claims exist. Default dedup path is transactionally safe; the command-owned path commits `Processing` before command execution and only removes it on a normal exception. Process death can strand the unique Processing row.

### Target behavior

Keep durable dedup, but add/prove lease/stale-claim recovery for the command-owned path so crash after claim commit cannot permanently suppress redelivery; preserve exactly one final business outcome.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ057` — Event meaning and transport ownership

### Minimum allowed change

Do not replace the durable inbox/dedup system. Extend the command-owned claim lifecycle with a bounded stale/recovery rule, preferably reusing `ClaimedAt` and store-level compare/update/delete semantics, then add crash-boundary integration proof.

### Non-goals

No delete-all dedup recovery, no new EventId on retry, no global message-only completion bit, no product semantics in the filter.

### Compatibility / migration impact

Claim-state/retention semantics affect dedup history and all command-owned consumers. Existing `Succeeded` records must remain valid; stale `Processing` reclamation must not steal live work.

### Verification and exit evidence

- Execute and record `PF-TST-MSG-057` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if stale threshold/ownership cannot distinguish interrupted work from an active command, or if recovery requires clearing durable history globally.

## PF-08-INV-001 — Ordered delivery and poison handling candidate inventory

### Requirement coverage

```text
PFREQ058, PFREQ059
```

### Stable verification references

```text
PF-TST-ORDER-058, PF-TST-ORDER-059
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 where async consumer requires the capability; otherwise explicit NOT_APPLICABLE
```

### Source to inspect

- `backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistry.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/OrderingEnforcerTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/PoisonDetectorTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Consumers/ConsumerHostDeliveryContractTests.cs`

### Current behavior

Ordering/poison reference mechanisms are implemented and unit-tested in `Notrelix.Platform`, but production RabbitMQ/MassTransit wiring is owned by Infrastructure. At the audited candidate no production `ConsumerDefinition` proves `OrderingRequired = true`.

### Target behavior

For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.
- Build a runtime-owner matrix per production consumer: `ConsumerDefinition.EndpointName`, whether ordering is required, actual MassTransit endpoint/wiring, retry owner, poison/dead-letter owner and durable state.
- Record `Notrelix.Platform` `ConsumerHost`/`OrderingEnforcer`/`PoisonDetector` as reference mechanism evidence only unless production DI actually uses them.
- Explicitly list every production consumer with `OrderingRequired=true`; at the preparation baseline this list is empty, so do not infer production ordering coverage.

Covered requirement intent:
- `PFREQ058` — Ordering requires real sequence and production adoption
- `PFREQ059` — Partition ordering

### Minimum allowed change

Choose one truthful disposition per consumer: wire the Platform mechanism, implement/certify equivalent MassTransit/Infrastructure semantics, or mark ordering NOT_APPLICABLE. Do not add ordering globally.

### Non-goals

No certification from isolated Platform unit tests alone; no global ordering; no process-local poison state presented as durable production DLQ.

### Compatibility / migration impact

Changing retry/order/poison behavior changes delivery guarantees and requires Architecture review plus backlog/consumer migration analysis.

### Verification and exit evidence

- Execute and record `PF-TST-ORDER-058, PF-TST-ORDER-059` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop before any delivery-guarantee change without Architecture approval; stop certification if the production runtime owner is unresolved.

## PF-08-CORE-001 — Ordered delivery and poison handling mechanism closure

### Requirement coverage

```text
PFREQ060, PFREQ061
```

### Stable verification references

```text
PF-TST-ORDER-060, PF-TST-ORDER-061
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 where async consumer requires the capability; otherwise explicit NOT_APPLICABLE
```

### Source to inspect

- `backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistry.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/OrderingEnforcerTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/PoisonDetectorTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Consumers/ConsumerHostDeliveryContractTests.cs`

### Current behavior

Ordering/poison reference mechanisms are implemented and unit-tested in `Notrelix.Platform`, but production RabbitMQ/MassTransit wiring is owned by Infrastructure. At the audited candidate no production `ConsumerDefinition` proves `OrderingRequired = true`.

### Target behavior

For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.
- For each consumer that truly needs order, choose one disposition: A) production runtime adopts the Platform mechanism; B) Infrastructure/MassTransit implements equivalent semantics; or C) ordering is `NOT_APPLICABLE`.
- If B is chosen, implement ordering at the smallest business-required partition and persist/coordinate any cursor needed for restart safety; handler success must precede sequence advancement.
- Bind poison/retry exhaustion to the production durable failure owner (outbox terminal `Failed` state and/or broker endpoint semantics) with consumer/message identity; do not introduce a second competing broker runtime.

Covered requirement intent:
- `PFREQ060` — Commit after handler success
- `PFREQ061` — Backpressure is observable

### Minimum allowed change

Choose one truthful disposition per consumer: wire the Platform mechanism, implement/certify equivalent MassTransit/Infrastructure semantics, or mark ordering NOT_APPLICABLE. Do not add ordering globally.

### Non-goals

No certification from isolated Platform unit tests alone; no global ordering; no process-local poison state presented as durable production DLQ.

### Compatibility / migration impact

Changing retry/order/poison behavior changes delivery guarantees and requires Architecture review plus backlog/consumer migration analysis.

### Verification and exit evidence

- Execute and record `PF-TST-ORDER-060, PF-TST-ORDER-061` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop before any delivery-guarantee change without Architecture approval; stop certification if the production runtime owner is unresolved.

## PF-08-SEC-001 — Ordered delivery and poison handling security/isolation/failure closure

### Requirement coverage

```text
PFREQ062, PFREQ063
```

### Stable verification references

```text
PF-TST-ORDER-062, PF-TST-ORDER-063
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 where async consumer requires the capability; otherwise explicit NOT_APPLICABLE
```

### Source to inspect

- `backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistry.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/OrderingEnforcerTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/PoisonDetectorTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Consumers/ConsumerHostDeliveryContractTests.cs`

### Current behavior

Ordering/poison reference mechanisms are implemented and unit-tested in `Notrelix.Platform`, but production RabbitMQ/MassTransit wiring is owned by Infrastructure. At the audited candidate no production `ConsumerDefinition` proves `OrderingRequired = true`.

### Target behavior

For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.
- Prove same-partition in-order, gap, handler failure, retry, poison/retry exhaustion, sibling-partition progress and restart behavior on the actual production runtime path.
- Prove retry budget does not multiply with provider retries into uncontrolled attempts and that required delivery never degrades to fake success.

Covered requirement intent:
- `PFREQ062` — Poison identity
- `PFREQ063` — Poison does not poison siblings

### Minimum allowed change

Choose one truthful disposition per consumer: wire the Platform mechanism, implement/certify equivalent MassTransit/Infrastructure semantics, or mark ordering NOT_APPLICABLE. Do not add ordering globally.

### Non-goals

No certification from isolated Platform unit tests alone; no global ordering; no process-local poison state presented as durable production DLQ.

### Compatibility / migration impact

Changing retry/order/poison behavior changes delivery guarantees and requires Architecture review plus backlog/consumer migration analysis.

### Verification and exit evidence

- Execute and record `PF-TST-ORDER-062, PF-TST-ORDER-063` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop before any delivery-guarantee change without Architecture approval; stop certification if the production runtime owner is unresolved.

## PF-08-COMPAT-001 — Ordered delivery and poison handling compatibility and consumer handoff

### Requirement coverage

```text
PFREQ064
```

### Stable verification references

```text
PF-TST-ORDER-064
```

### Lane posture / target

```text
Preparation posture: PARTIAL_GAP
Required readiness: D5 where async consumer requires the capability; otherwise explicit NOT_APPLICABLE
```

### Source to inspect

- `backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs`
- `backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistry.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs`
- `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/OrderingEnforcerTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Reliability/PoisonDetectorTests.cs`
- `backend/tests/Notrelix.Platform.Tests/Messaging/Consumers/ConsumerHostDeliveryContractTests.cs`

### Current behavior

Ordering/poison reference mechanisms are implemented and unit-tested in `Notrelix.Platform`, but production RabbitMQ/MassTransit wiring is owned by Infrastructure. At the audited candidate no production `ConsumerDefinition` proves `OrderingRequired = true`.

### Target behavior

For every consumer that claims ordering/poison semantics, bind the requirement to the real production runtime owner and prove equivalent ordering, commit-after-success, partition independence and consumer-scoped failure containment.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ064` — Restart/durability and runtime-owner classification

### Minimum allowed change

Choose one truthful disposition per consumer: wire the Platform mechanism, implement/certify equivalent MassTransit/Infrastructure semantics, or mark ordering NOT_APPLICABLE. Do not add ordering globally.

### Non-goals

No certification from isolated Platform unit tests alone; no global ordering; no process-local poison state presented as durable production DLQ.

### Compatibility / migration impact

Changing retry/order/poison behavior changes delivery guarantees and requires Architecture review plus backlog/consumer migration analysis.

### Verification and exit evidence

- Execute and record `PF-TST-ORDER-064` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop before any delivery-guarantee change without Architecture approval; stop certification if the production runtime owner is unresolved.

## PF-09-INV-001 — Realtime transport and recovery candidate inventory

### Requirement coverage

```text
PFREQ065, PFREQ066
```

### Stable verification references

```text
PF-TST-RT-065, PF-TST-RT-066
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4+ per Work Management / Documents / Collaboration consumer
```

### Source to inspect

- `frontend/packages/foundation/realtime/src/transport/realtime-client.ts`
- `frontend/apps/web/src/realtime/workspace-recovery-policy.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/docs/architecture/realtime.md`

### Current behavior

RealtimeClient detects gaps, but `handleWorkspaceRecovery()` invalidates non-canonical key shapes and there is no explicit successful-recovery API that reconciles the sequence checkpoint. One gap can therefore trigger repeated recovery.

### Target behavior

Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ065` — Connection lifecycle
- `PFREQ066` — Subscription scoping

### Minimum allowed change

Use canonical query key factories in recovery and add an explicit client/runtime recovery completion operation. Keep recovery owner-specific; do not invent generic conflict semantics.

### Non-goals

No claim that reconnect or invalidation alone equals ordered recovery; no business conflict policy in foundation realtime transport.

### Compatibility / migration impact

Sequence/recovery API changes affect web runtime and realtime-heavy consumers. Recovery key changes must align with PF-11 account/workspace key migration.

### Verification and exit evidence

- Execute and record `PF-TST-RT-065, PF-TST-RT-066` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the owning feature cannot define authoritative recovery or if sequence advancement could skip an un-reconciled business gap.

## PF-09-CORE-001 — Realtime transport and recovery mechanism closure

### Requirement coverage

```text
PFREQ067, PFREQ068
```

### Stable verification references

```text
PF-TST-RT-067, PF-TST-RT-068
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4+ per Work Management / Documents / Collaboration consumer
```

### Source to inspect

- `frontend/packages/foundation/realtime/src/transport/realtime-client.ts`
- `frontend/apps/web/src/realtime/workspace-recovery-policy.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/docs/architecture/realtime.md`

### Current behavior

RealtimeClient detects gaps, but `handleWorkspaceRecovery()` invalidates non-canonical key shapes and there is no explicit successful-recovery API that reconciles the sequence checkpoint. One gap can therefore trigger repeated recovery.

### Target behavior

Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.
- Replace hard-coded recovery key arrays in `workspace-recovery-policy.ts` with canonical key factories from PF-11. The recovery policy must invalidate/refetch the exact cache identities that own authoritative Workspace/member/ability/notification state.
- Extend the realtime transport/runtime contract with explicit recovery lifecycle state. After authoritative recovery succeeds, reconcile/reset/advance the sequence checkpoint to the recovered position before normal event processing resumes.
- Define the later-envelope policy while recovery is active (drop-with-authoritative-refetch, bounded queue, or suspension). The chosen policy must be deterministic and bounded.

Covered requirement intent:
- `PFREQ067` — Message validation and dedup
- `PFREQ068` — Gap detection and recovery entry

### Minimum allowed change

Use canonical query key factories in recovery and add an explicit client/runtime recovery completion operation. Keep recovery owner-specific; do not invent generic conflict semantics.

### Non-goals

No claim that reconnect or invalidation alone equals ordered recovery; no business conflict policy in foundation realtime transport.

### Compatibility / migration impact

Sequence/recovery API changes affect web runtime and realtime-heavy consumers. Recovery key changes must align with PF-11 account/workspace key migration.

### Verification and exit evidence

- Execute and record `PF-TST-RT-067, PF-TST-RT-068` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the owning feature cannot define authoritative recovery or if sequence advancement could skip an un-reconciled business gap.

## PF-09-SEC-001 — Realtime transport and recovery security/isolation/failure closure

### Requirement coverage

```text
PFREQ069, PFREQ070
```

### Stable verification references

```text
PF-TST-RT-069, PF-TST-RT-070
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4+ per Work Management / Documents / Collaboration consumer
```

### Source to inspect

- `frontend/packages/foundation/realtime/src/transport/realtime-client.ts`
- `frontend/apps/web/src/realtime/workspace-recovery-policy.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/docs/architecture/realtime.md`

### Current behavior

RealtimeClient detects gaps, but `handleWorkspaceRecovery()` invalidates non-canonical key shapes and there is no explicit successful-recovery API that reconciles the sequence checkpoint. One gap can therefore trigger repeated recovery.

### Target behavior

Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.
- Add scenarios for one gap → successful authoritative recovery → next sequence normal; failed recovery → deterministic retry; duplicate after recovery; out-of-order after recovery; workspace/subscription isolation; disconnect/reconnect during recovery.
- Prove recovery never applies Workspace A events/query invalidation/checkpoint to Workspace B.

Covered requirement intent:
- `PFREQ069` — Consumer recovery contract
- `PFREQ070` — Invalidation is not universal recovery

### Minimum allowed change

Use canonical query key factories in recovery and add an explicit client/runtime recovery completion operation. Keep recovery owner-specific; do not invent generic conflict semantics.

### Non-goals

No claim that reconnect or invalidation alone equals ordered recovery; no business conflict policy in foundation realtime transport.

### Compatibility / migration impact

Sequence/recovery API changes affect web runtime and realtime-heavy consumers. Recovery key changes must align with PF-11 account/workspace key migration.

### Verification and exit evidence

- Execute and record `PF-TST-RT-069, PF-TST-RT-070` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the owning feature cannot define authoritative recovery or if sequence advancement could skip an un-reconciled business gap.

## PF-09-COMPAT-001 — Realtime transport and recovery compatibility and consumer handoff

### Requirement coverage

```text
PFREQ071
```

### Stable verification references

```text
PF-TST-RT-071
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4+ per Work Management / Documents / Collaboration consumer
```

### Source to inspect

- `frontend/packages/foundation/realtime/src/transport/realtime-client.ts`
- `frontend/apps/web/src/realtime/workspace-recovery-policy.ts`
- `frontend/apps/web/src/providers/realtime-lifecycle.tsx`
- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/docs/architecture/realtime.md`

### Current behavior

RealtimeClient detects gaps, but `handleWorkspaceRecovery()` invalidates non-canonical key shapes and there is no explicit successful-recovery API that reconciles the sequence checkpoint. One gap can therefore trigger repeated recovery.

### Target behavior

Define one recovery state machine: gap detected → later-envelope policy → authoritative owner recovery → canonical cache convergence → sequence checkpoint reconcile/reset → normal delivery resumes.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ071` — Post-recovery convergence guarantee

### Minimum allowed change

Use canonical query key factories in recovery and add an explicit client/runtime recovery completion operation. Keep recovery owner-specific; do not invent generic conflict semantics.

### Non-goals

No claim that reconnect or invalidation alone equals ordered recovery; no business conflict policy in foundation realtime transport.

### Compatibility / migration impact

Sequence/recovery API changes affect web runtime and realtime-heavy consumers. Recovery key changes must align with PF-11 account/workspace key migration.

### Verification and exit evidence

- Execute and record `PF-TST-RT-071` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the owning feature cannot define authoritative recovery or if sequence advancement could skip an un-reconciled business gap.

## PF-10-INV-001 — API, OpenAPI and generated contracts candidate inventory

### Requirement coverage

```text
PFREQ072, PFREQ073
```

### Stable verification references

```text
PF-TST-API-072, PF-TST-API-073
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs`
- `backend/contracts/openapi/notrelix.v1.json`
- `frontend/tooling/codegen/openapi/generate-openapi.mjs`
- `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`
- `backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

Backend has canonical deterministic OpenAPI export and tracked contract; frontend generates REST types from that artifact.

### Target behavior

Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ072` — OpenAPI producer authority
- `PFREQ073` — Deterministic export

### Minimum allowed change

Change producer endpoint/metadata first, regenerate via canonical commands, review semantic diff, then regenerate frontend; never hand-edit generated authority.

### Non-goals

No handwritten duplicate client model as canonical wire contract; no generated artifact treated as producer authority.

### Compatibility / migration impact

Breaking REST/realtime contract changes require consumer rollout/compatibility classification before merge.

### Verification and exit evidence

- Execute and record `PF-TST-API-072, PF-TST-API-073` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if generated output/source ownership is unclear or if a breaking change has no migration/consumer plan.

## PF-10-CORE-001 — API, OpenAPI and generated contracts mechanism closure

### Requirement coverage

```text
PFREQ074, PFREQ075
```

### Stable verification references

```text
PF-TST-API-074, PF-TST-API-075
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs`
- `backend/contracts/openapi/notrelix.v1.json`
- `frontend/tooling/codegen/openapi/generate-openapi.mjs`
- `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`
- `backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

Backend has canonical deterministic OpenAPI export and tracked contract; frontend generates REST types from that artifact.

### Target behavior

Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ074` — Security metadata
- `PFREQ075` — Generated frontend contracts

### Minimum allowed change

Change producer endpoint/metadata first, regenerate via canonical commands, review semantic diff, then regenerate frontend; never hand-edit generated authority.

### Non-goals

No handwritten duplicate client model as canonical wire contract; no generated artifact treated as producer authority.

### Compatibility / migration impact

Breaking REST/realtime contract changes require consumer rollout/compatibility classification before merge.

### Verification and exit evidence

- Execute and record `PF-TST-API-074, PF-TST-API-075` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if generated output/source ownership is unclear or if a breaking change has no migration/consumer plan.

## PF-10-SEC-001 — API, OpenAPI and generated contracts security/isolation/failure closure

### Requirement coverage

```text
PFREQ076, PFREQ077
```

### Stable verification references

```text
PF-TST-API-076, PF-TST-API-077
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs`
- `backend/contracts/openapi/notrelix.v1.json`
- `frontend/tooling/codegen/openapi/generate-openapi.mjs`
- `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`
- `backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

Backend has canonical deterministic OpenAPI export and tracked contract; frontend generates REST types from that artifact.

### Target behavior

Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ076` — Breaking-change review
- `PFREQ077` — Realtime contract generation

### Minimum allowed change

Change producer endpoint/metadata first, regenerate via canonical commands, review semantic diff, then regenerate frontend; never hand-edit generated authority.

### Non-goals

No handwritten duplicate client model as canonical wire contract; no generated artifact treated as producer authority.

### Compatibility / migration impact

Breaking REST/realtime contract changes require consumer rollout/compatibility classification before merge.

### Verification and exit evidence

- Execute and record `PF-TST-API-076, PF-TST-API-077` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if generated output/source ownership is unclear or if a breaking change has no migration/consumer plan.

## PF-10-COMPAT-001 — API, OpenAPI and generated contracts compatibility and consumer handoff

### Requirement coverage

```text
PFREQ078
```

### Stable verification references

```text
PF-TST-API-078
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs`
- `backend/contracts/openapi/notrelix.v1.json`
- `frontend/tooling/codegen/openapi/generate-openapi.mjs`
- `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`
- `backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs`
- `.github/workflows/backend-ci.yml`

### Current behavior

Backend has canonical deterministic OpenAPI export and tracked contract; frontend generates REST types from that artifact.

### Target behavior

Keep producer authority in API source, drift-check the tracked artifact, preserve security metadata, and make frontend compile/codegen fail on incompatible contract drift.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ078` — Consumer compile gate

### Minimum allowed change

Change producer endpoint/metadata first, regenerate via canonical commands, review semantic diff, then regenerate frontend; never hand-edit generated authority.

### Non-goals

No handwritten duplicate client model as canonical wire contract; no generated artifact treated as producer authority.

### Compatibility / migration impact

Breaking REST/realtime contract changes require consumer rollout/compatibility classification before merge.

### Verification and exit evidence

- Execute and record `PF-TST-API-078` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if generated output/source ownership is unclear or if a breaking change has no migration/consumer plan.

## PF-11-INV-001 — Frontend query/server-state foundation candidate inventory

### Requirement coverage

```text
PFREQ079, PFREQ080
```

### Stable verification references

```text
PF-TST-QUERY-079, PF-TST-QUERY-080
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/foundation/query/src/index.ts`
- `frontend/packages/features/account/src/query/keys.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/tooling/generators/create-feature/index.mjs`
- `frontend/docs/architecture/state-query-mutations.md`

### Current behavior

Three query roots exist and workspace keys encode Workspace ID. Account root does not encode Account ID, so Account A/B data can collide unless an unproven hard reset occurs.

### Target behavior

Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ079` — Canonical query-key roots
- `PFREQ080` — Workspace identity in keys

### Minimum allowed change

Change `accountQueryKey` signature/tuple to carry accountId and migrate all factories/generator, unless PF-02 delivers a stronger atomic reset contract with equivalent proof.

### Non-goals

No new global server-state store; no feature-specific ad-hoc account prefix; no duplicate server truth in client stores.

### Compatibility / migration impact

This is a deliberate cache-key breaking change inside the client. All account-scoped feature keys and generator templates must move in one candidate.

### Verification and exit evidence

- Execute and record `PF-TST-QUERY-079, PF-TST-QUERY-080` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop D5 if any account-scoped query can be constructed without non-empty account identity after the migration.

## PF-11-CORE-001 — Frontend query/server-state foundation mechanism closure

### Requirement coverage

```text
PFREQ081, PFREQ082
```

### Stable verification references

```text
PF-TST-QUERY-081, PF-TST-QUERY-082
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/foundation/query/src/index.ts`
- `frontend/packages/features/account/src/query/keys.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/tooling/generators/create-feature/index.mjs`
- `frontend/docs/architecture/state-query-mutations.md`

### Current behavior

Three query roots exist and workspace keys encode Workspace ID. Account root does not encode Account ID, so Account A/B data can collide unless an unproven hard reset occurs.

### Target behavior

Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.
- Change the canonical account key helper or prove the alternative atomic-reset contract. Do not leave the root-only `['account', resource, ...]` shape while claiming D5.
- Migrate every account-scoped feature key and generator in the same change; reject empty Account IDs in assertion/tooling.

Covered requirement intent:
- `PFREQ081` — Account isolation
- `PFREQ082` — Optimistic update ownership

### Minimum allowed change

Change `accountQueryKey` signature/tuple to carry accountId and migrate all factories/generator, unless PF-02 delivers a stronger atomic reset contract with equivalent proof.

### Non-goals

No new global server-state store; no feature-specific ad-hoc account prefix; no duplicate server truth in client stores.

### Compatibility / migration impact

This is a deliberate cache-key breaking change inside the client. All account-scoped feature keys and generator templates must move in one candidate.

### Verification and exit evidence

- Execute and record `PF-TST-QUERY-081, PF-TST-QUERY-082` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop D5 if any account-scoped query can be constructed without non-empty account identity after the migration.

## PF-11-SEC-001 — Frontend query/server-state foundation security/isolation/failure closure

### Requirement coverage

```text
PFREQ083, PFREQ084
```

### Stable verification references

```text
PF-TST-QUERY-083, PF-TST-QUERY-084
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/foundation/query/src/index.ts`
- `frontend/packages/features/account/src/query/keys.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/tooling/generators/create-feature/index.mjs`
- `frontend/docs/architecture/state-query-mutations.md`

### Current behavior

Three query roots exist and workspace keys encode Workspace ID. Account root does not encode Account ID, so Account A/B data can collide unless an unproven hard reset occurs.

### Target behavior

Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.
- Exercise Account A → B → A with overlapping resource names. Assert no A data, optimistic patch, permissions or stale subscription state is visible under B and returning to A resolves the intended A partition only.

Covered requirement intent:
- `PFREQ083` — No duplicate server truth
- `PFREQ084` — Account-switch proof

### Minimum allowed change

Change `accountQueryKey` signature/tuple to carry accountId and migrate all factories/generator, unless PF-02 delivers a stronger atomic reset contract with equivalent proof.

### Non-goals

No new global server-state store; no feature-specific ad-hoc account prefix; no duplicate server truth in client stores.

### Compatibility / migration impact

This is a deliberate cache-key breaking change inside the client. All account-scoped feature keys and generator templates must move in one candidate.

### Verification and exit evidence

- Execute and record `PF-TST-QUERY-083, PF-TST-QUERY-084` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop D5 if any account-scoped query can be constructed without non-empty account identity after the migration.

## PF-11-COMPAT-001 — Frontend query/server-state foundation compatibility and consumer handoff

### Requirement coverage

```text
PFREQ085
```

### Stable verification references

```text
PF-TST-QUERY-085
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D5
```

### Source to inspect

- `frontend/packages/foundation/query/src/query-key-scope.ts`
- `frontend/packages/foundation/query/src/index.ts`
- `frontend/packages/features/account/src/query/keys.ts`
- `frontend/packages/features/workspace/src/query/keys.ts`
- `frontend/packages/features/notifications/src/query/keys.ts`
- `frontend/tooling/generators/create-feature/index.mjs`
- `frontend/docs/architecture/state-query-mutations.md`

### Current behavior

Three query roots exist and workspace keys encode Workspace ID. Account root does not encode Account ID, so Account A/B data can collide unless an unproven hard reset occurs.

### Target behavior

Make all account-scoped server state partitioned by active Account identity and prove A → B → A isolation including optimistic state, permissions and realtime-related cache.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ085` — Generator compliance

### Minimum allowed change

Change `accountQueryKey` signature/tuple to carry accountId and migrate all factories/generator, unless PF-02 delivers a stronger atomic reset contract with equivalent proof.

### Non-goals

No new global server-state store; no feature-specific ad-hoc account prefix; no duplicate server truth in client stores.

### Compatibility / migration impact

This is a deliberate cache-key breaking change inside the client. All account-scoped feature keys and generator templates must move in one candidate.

### Verification and exit evidence

- Execute and record `PF-TST-QUERY-085` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop D5 if any account-scoped query can be constructed without non-empty account identity after the migration.

## PF-12-INV-001 — Frontend runtime and host composition candidate inventory

### Requirement coverage

```text
PFREQ086, PFREQ087
```

### Stable verification references

```text
PF-TST-RUNTIME-086, PF-TST-RUNTIME-087
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D4/D5
```

### Source to inspect

- `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`
- `frontend/packages/runtimes/web/src/runtime/session-event-bus.ts`
- `frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts`
- `frontend/packages/runtimes/web/src/index.ts`
- `frontend/packages/runtimes/mobile/`
- `frontend/apps/web/src/providers/app-providers.tsx`
- `frontend/apps/web/src/composition/application-services.ts`
- `frontend/apps/web/src/main.tsx`

### Current behavior

Web/mobile runtime packages and app composition roots exist; web runtime owns session/realtime/browser adapter composition.

### Target behavior

Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ086` — Runtime composition root
- `PFREQ087` — Host-specific adapters

### Minimum allowed change

Retain current package graph and add only missing host composition/negative-path evidence.

### Non-goals

No route hardcoding in foundation runtime; no browser API dependency in portable/core packages; no new global-state architecture.

### Compatibility / migration impact

Runtime public export/factory changes affect every host and must be compiled/tested against web/mobile consumers.

### Verification and exit evidence

- Execute and record `PF-TST-RUNTIME-086, PF-TST-RUNTIME-087` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new global state/framework or a new production host/service boundary without Architecture approval.

## PF-12-CORE-001 — Frontend runtime and host composition mechanism closure

### Requirement coverage

```text
PFREQ088, PFREQ089
```

### Stable verification references

```text
PF-TST-RUNTIME-088, PF-TST-RUNTIME-089
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D4/D5
```

### Source to inspect

- `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`
- `frontend/packages/runtimes/web/src/runtime/session-event-bus.ts`
- `frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts`
- `frontend/packages/runtimes/web/src/index.ts`
- `frontend/packages/runtimes/mobile/`
- `frontend/apps/web/src/providers/app-providers.tsx`
- `frontend/apps/web/src/composition/application-services.ts`
- `frontend/apps/web/src/main.tsx`

### Current behavior

Web/mobile runtime packages and app composition roots exist; web runtime owns session/realtime/browser adapter composition.

### Target behavior

Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ088` — Typed session-expired plumbing
- `PFREQ089` — Realtime factory ownership

### Minimum allowed change

Retain current package graph and add only missing host composition/negative-path evidence.

### Non-goals

No route hardcoding in foundation runtime; no browser API dependency in portable/core packages; no new global-state architecture.

### Compatibility / migration impact

Runtime public export/factory changes affect every host and must be compiled/tested against web/mobile consumers.

### Verification and exit evidence

- Execute and record `PF-TST-RUNTIME-088, PF-TST-RUNTIME-089` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new global state/framework or a new production host/service boundary without Architecture approval.

## PF-12-SEC-001 — Frontend runtime and host composition security/isolation/failure closure

### Requirement coverage

```text
PFREQ090, PFREQ091
```

### Stable verification references

```text
PF-TST-RUNTIME-090, PF-TST-RUNTIME-091
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D4/D5
```

### Source to inspect

- `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`
- `frontend/packages/runtimes/web/src/runtime/session-event-bus.ts`
- `frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts`
- `frontend/packages/runtimes/web/src/index.ts`
- `frontend/packages/runtimes/mobile/`
- `frontend/apps/web/src/providers/app-providers.tsx`
- `frontend/apps/web/src/composition/application-services.ts`
- `frontend/apps/web/src/main.tsx`

### Current behavior

Web/mobile runtime packages and app composition roots exist; web runtime owns session/realtime/browser adapter composition.

### Target behavior

Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ090` — Environment validation
- `PFREQ091` — No route hardcoding in foundation

### Minimum allowed change

Retain current package graph and add only missing host composition/negative-path evidence.

### Non-goals

No route hardcoding in foundation runtime; no browser API dependency in portable/core packages; no new global-state architecture.

### Compatibility / migration impact

Runtime public export/factory changes affect every host and must be compiled/tested against web/mobile consumers.

### Verification and exit evidence

- Execute and record `PF-TST-RUNTIME-090, PF-TST-RUNTIME-091` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new global state/framework or a new production host/service boundary without Architecture approval.

## PF-12-COMPAT-001 — Frontend runtime and host composition compatibility and consumer handoff

### Requirement coverage

```text
PFREQ092
```

### Stable verification references

```text
PF-TST-RUNTIME-092
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: consumer-specific D4/D5
```

### Source to inspect

- `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`
- `frontend/packages/runtimes/web/src/runtime/session-event-bus.ts`
- `frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts`
- `frontend/packages/runtimes/web/src/index.ts`
- `frontend/packages/runtimes/mobile/`
- `frontend/apps/web/src/providers/app-providers.tsx`
- `frontend/apps/web/src/composition/application-services.ts`
- `frontend/apps/web/src/main.tsx`

### Current behavior

Web/mobile runtime packages and app composition roots exist; web runtime owns session/realtime/browser adapter composition.

### Target behavior

Keep host-specific IO at runtime/composition boundaries, typed session-expired plumbing and validated environment/config without feature-route semantics leaking into foundation.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ092` — Composition tests

### Minimum allowed change

Retain current package graph and add only missing host composition/negative-path evidence.

### Non-goals

No route hardcoding in foundation runtime; no browser API dependency in portable/core packages; no new global-state architecture.

### Compatibility / migration impact

Runtime public export/factory changes affect every host and must be compiled/tested against web/mobile consumers.

### Verification and exit evidence

- Execute and record `PF-TST-RUNTIME-092` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new global state/framework or a new production host/service boundary without Architecture approval.

## PF-13-INV-001 — UI tokens and primitive foundation candidate inventory

### Requirement coverage

```text
PFREQ093, PFREQ094
```

### Stable verification references

```text
PF-TST-UI-093, PF-TST-UI-094
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4
```

### Source to inspect

- `frontend/packages/ui/tokens/package.json`
- `frontend/packages/ui/tokens/src/index.ts`
- `frontend/packages/ui/tokens/src/colors.ts`
- `frontend/packages/ui/tokens/src/semantic.ts`
- `frontend/packages/ui/tokens/src/themes/`
- `frontend/packages/ui/web/src/index.ts`
- `frontend/packages/ui/web/src/components/`
- `frontend/packages/ui/web/src/verification/ui-web-critical-surfaces.tsx`
- `frontend/packages/ui/web/verification/ui-evidence.manifest.json`

### Current behavior

Token TypeScript source is present, but package metadata advertises `./css -> ./src/css/index.css` and `src/css/` does not exist at the audited candidate. Audit found no current consumer of the CSS subpath.

### Target behavior

Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ093` — Token vs product component boundary
- `PFREQ094` — CSS export validity

### Minimum allowed change

Because no consumer was found, prefer the smallest contract-correct change: source-back the export if CSS is part of intended public API; otherwise remove it and prove zero consumers. Do not invent a parallel design-system layer.

### Non-goals

No product/business semantics in primitives; no broad visual rewrite; no silent dead export.

### Compatibility / migration impact

Even an unused public export is a package contract. Search repo consumers, update package/export tests, generated boundary evidence and any downstream imports atomically.

### Verification and exit evidence

- Execute and record `PF-TST-UI-093, PF-TST-UI-094` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the intended ownership of CSS token generation is unresolved; do not fabricate a file merely to satisfy a path.

## PF-13-CORE-001 — UI tokens and primitive foundation mechanism closure

### Requirement coverage

```text
PFREQ095, PFREQ096
```

### Stable verification references

```text
PF-TST-UI-095, PF-TST-UI-096
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4
```

### Source to inspect

- `frontend/packages/ui/tokens/package.json`
- `frontend/packages/ui/tokens/src/index.ts`
- `frontend/packages/ui/tokens/src/colors.ts`
- `frontend/packages/ui/tokens/src/semantic.ts`
- `frontend/packages/ui/tokens/src/themes/`
- `frontend/packages/ui/web/src/index.ts`
- `frontend/packages/ui/web/src/components/`
- `frontend/packages/ui/web/src/verification/ui-web-critical-surfaces.tsx`
- `frontend/packages/ui/web/verification/ui-evidence.manifest.json`

### Current behavior

Token TypeScript source is present, but package metadata advertises `./css -> ./src/css/index.css` and `src/css/` does not exist at the audited candidate. Audit found no current consumer of the CSS subpath.

### Target behavior

Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.
- Resolve `package.json` export `./css -> ./src/css/index.css` truthfully. Either create a governed CSS artifact sourced from tokens and verify it, or remove the export after proving no supported consumer.
- If creating CSS, define source/generation ownership and drift test; do not hand-author an unrelated stylesheet only to satisfy package resolution.

Covered requirement intent:
- `PFREQ095` — Theme/token stability
- `PFREQ096` — Accessibility foundation

### Minimum allowed change

Because no consumer was found, prefer the smallest contract-correct change: source-back the export if CSS is part of intended public API; otherwise remove it and prove zero consumers. Do not invent a parallel design-system layer.

### Non-goals

No product/business semantics in primitives; no broad visual rewrite; no silent dead export.

### Compatibility / migration impact

Even an unused public export is a package contract. Search repo consumers, update package/export tests, generated boundary evidence and any downstream imports atomically.

### Verification and exit evidence

- Execute and record `PF-TST-UI-095, PF-TST-UI-096` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the intended ownership of CSS token generation is unresolved; do not fabricate a file merely to satisfy a path.

## PF-13-SEC-001 — UI tokens and primitive foundation security/isolation/failure closure

### Requirement coverage

```text
PFREQ097, PFREQ098
```

### Stable verification references

```text
PF-TST-UI-097, PF-TST-UI-098
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4
```

### Source to inspect

- `frontend/packages/ui/tokens/package.json`
- `frontend/packages/ui/tokens/src/index.ts`
- `frontend/packages/ui/tokens/src/colors.ts`
- `frontend/packages/ui/tokens/src/semantic.ts`
- `frontend/packages/ui/tokens/src/themes/`
- `frontend/packages/ui/web/src/index.ts`
- `frontend/packages/ui/web/src/components/`
- `frontend/packages/ui/web/src/verification/ui-web-critical-surfaces.tsx`
- `frontend/packages/ui/web/verification/ui-evidence.manifest.json`

### Current behavior

Token TypeScript source is present, but package metadata advertises `./css -> ./src/css/index.css` and `src/css/` does not exist at the audited candidate. Audit found no current consumer of the CSS subpath.

### Target behavior

Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ097` — Responsive foundation
- `PFREQ098` — Evidence manifests

### Minimum allowed change

Because no consumer was found, prefer the smallest contract-correct change: source-back the export if CSS is part of intended public API; otherwise remove it and prove zero consumers. Do not invent a parallel design-system layer.

### Non-goals

No product/business semantics in primitives; no broad visual rewrite; no silent dead export.

### Compatibility / migration impact

Even an unused public export is a package contract. Search repo consumers, update package/export tests, generated boundary evidence and any downstream imports atomically.

### Verification and exit evidence

- Execute and record `PF-TST-UI-097, PF-TST-UI-098` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the intended ownership of CSS token generation is unresolved; do not fabricate a file merely to satisfy a path.

## PF-13-COMPAT-001 — UI tokens and primitive foundation compatibility and consumer handoff

### Requirement coverage

```text
PFREQ099
```

### Stable verification references

```text
PF-TST-UI-099
```

### Lane posture / target

```text
Preparation posture: GAP_CONFIRMED
Required readiness: D4
```

### Source to inspect

- `frontend/packages/ui/tokens/package.json`
- `frontend/packages/ui/tokens/src/index.ts`
- `frontend/packages/ui/tokens/src/colors.ts`
- `frontend/packages/ui/tokens/src/semantic.ts`
- `frontend/packages/ui/tokens/src/themes/`
- `frontend/packages/ui/web/src/index.ts`
- `frontend/packages/ui/web/src/components/`
- `frontend/packages/ui/web/src/verification/ui-web-critical-surfaces.tsx`
- `frontend/packages/ui/web/verification/ui-evidence.manifest.json`

### Current behavior

Token TypeScript source is present, but package metadata advertises `./css -> ./src/css/index.css` and `src/css/` does not exist at the audited candidate. Audit found no current consumer of the CSS subpath.

### Target behavior

Make every public export source-backed. Either create the governed CSS token artifact and its generation/verification path or remove the unused export with explicit compatibility review.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.
- Search all repo imports for `@notrelix/ui-tokens/css`, update any consumer, package manifest and generated boundary evidence, and prove package resolution from a clean install/build.

Covered requirement intent:
- `PFREQ099` — No business semantics in primitives

### Minimum allowed change

Because no consumer was found, prefer the smallest contract-correct change: source-back the export if CSS is part of intended public API; otherwise remove it and prove zero consumers. Do not invent a parallel design-system layer.

### Non-goals

No product/business semantics in primitives; no broad visual rewrite; no silent dead export.

### Compatibility / migration impact

Even an unused public export is a package contract. Search repo consumers, update package/export tests, generated boundary evidence and any downstream imports atomically.

### Verification and exit evidence

- Execute and record `PF-TST-UI-099` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include consuming-host/frontend integration evidence, not only isolated helper tests.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if the intended ownership of CSS token generation is unresolved; do not fabricate a file merely to satisfy a path.

## PF-14-INV-001 — Observability candidate inventory

### Requirement coverage

```text
PFREQ100, PFREQ101
```

### Stable verification references

```text
PF-TST-OBS-100, PF-TST-OBS-101
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4/D5 by consuming runtime
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs`
- `frontend/packages/foundation/observability/src/init.ts`
- `frontend/packages/foundation/observability/src/tracing.ts`
- `frontend/packages/foundation/observability/src/telemetry/redaction.ts`
- `frontend/packages/foundation/observability/src/telemetry/telemetry.ts`

### Current behavior

Tracing, metrics, health and frontend telemetry/redaction mechanisms exist.

### Target behavior

Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ100` — Correlation propagation
- `PFREQ101` — Trace and metric mechanics

### Minimum allowed change

Add dimensions/health only when actionable and bounded; preserve metadata-first diagnostics.

### Non-goals

No raw payload/credential logging; no unbounded tenant/resource cardinality; no health=green when a required dependency is unavailable.

### Compatibility / migration impact

Telemetry schema/dashboard changes are operational contracts; document renamed dimensions and update tests/runbooks where relied upon.

### Verification and exit evidence

- Execute and record `PF-TST-OBS-100, PF-TST-OBS-101` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if diagnosis requires dumping sensitive payload or if a proposed metric creates unbounded cardinality.

## PF-14-CORE-001 — Observability mechanism closure

### Requirement coverage

```text
PFREQ102, PFREQ103
```

### Stable verification references

```text
PF-TST-OBS-102, PF-TST-OBS-103
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4/D5 by consuming runtime
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs`
- `frontend/packages/foundation/observability/src/init.ts`
- `frontend/packages/foundation/observability/src/tracing.ts`
- `frontend/packages/foundation/observability/src/telemetry/redaction.ts`
- `frontend/packages/foundation/observability/src/telemetry/telemetry.ts`

### Current behavior

Tracing, metrics, health and frontend telemetry/redaction mechanisms exist.

### Target behavior

Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ102` — Secret redaction
- `PFREQ103` — Tenant-safe context

### Minimum allowed change

Add dimensions/health only when actionable and bounded; preserve metadata-first diagnostics.

### Non-goals

No raw payload/credential logging; no unbounded tenant/resource cardinality; no health=green when a required dependency is unavailable.

### Compatibility / migration impact

Telemetry schema/dashboard changes are operational contracts; document renamed dimensions and update tests/runbooks where relied upon.

### Verification and exit evidence

- Execute and record `PF-TST-OBS-102, PF-TST-OBS-103` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if diagnosis requires dumping sensitive payload or if a proposed metric creates unbounded cardinality.

## PF-14-SEC-001 — Observability security/isolation/failure closure

### Requirement coverage

```text
PFREQ104, PFREQ105
```

### Stable verification references

```text
PF-TST-OBS-104, PF-TST-OBS-105
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4/D5 by consuming runtime
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs`
- `frontend/packages/foundation/observability/src/init.ts`
- `frontend/packages/foundation/observability/src/tracing.ts`
- `frontend/packages/foundation/observability/src/telemetry/redaction.ts`
- `frontend/packages/foundation/observability/src/telemetry/telemetry.ts`

### Current behavior

Tracing, metrics, health and frontend telemetry/redaction mechanisms exist.

### Target behavior

Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ104` — Retry/failure signals
- `PFREQ105` — Health checks

### Minimum allowed change

Add dimensions/health only when actionable and bounded; preserve metadata-first diagnostics.

### Non-goals

No raw payload/credential logging; no unbounded tenant/resource cardinality; no health=green when a required dependency is unavailable.

### Compatibility / migration impact

Telemetry schema/dashboard changes are operational contracts; document renamed dimensions and update tests/runbooks where relied upon.

### Verification and exit evidence

- Execute and record `PF-TST-OBS-104, PF-TST-OBS-105` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if diagnosis requires dumping sensitive payload or if a proposed metric creates unbounded cardinality.

## PF-14-COMPAT-001 — Observability compatibility and consumer handoff

### Requirement coverage

```text
PFREQ106
```

### Stable verification references

```text
PF-TST-OBS-106
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D4/D5 by consuming runtime
```

### Source to inspect

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs`
- `frontend/packages/foundation/observability/src/init.ts`
- `frontend/packages/foundation/observability/src/tracing.ts`
- `frontend/packages/foundation/observability/src/telemetry/redaction.ts`
- `frontend/packages/foundation/observability/src/telemetry/telemetry.ts`

### Current behavior

Tracing, metrics, health and frontend telemetry/redaction mechanisms exist.

### Target behavior

Prove correlation propagation, safe bounded dimensions, secret redaction, tenant-safe context, actionable retry/failure/backlog signals and honest health/degradation.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ106` — Failure observability

### Minimum allowed change

Add dimensions/health only when actionable and bounded; preserve metadata-first diagnostics.

### Non-goals

No raw payload/credential logging; no unbounded tenant/resource cardinality; no health=green when a required dependency is unavailable.

### Compatibility / migration impact

Telemetry schema/dashboard changes are operational contracts; document renamed dimensions and update tests/runbooks where relied upon.

### Verification and exit evidence

- Execute and record `PF-TST-OBS-106` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop if diagnosis requires dumping sensitive payload or if a proposed metric creates unbounded cardinality.

## PF-15-INV-001 — Architecture and dependency enforcement candidate inventory

### Requirement coverage

```text
PFREQ107, PFREQ108
```

### Stable verification references

```text
PF-TST-ARCH-107, PF-TST-ARCH-108
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/tests/Notrelix.Architecture.Tests/`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`
- `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- `frontend/tooling/dependency-rules/src/check-package-manifests.ts`
- `frontend/tooling/dependency-rules/src/generate-architecture-docs.ts`
- `frontend/docs/generated/package-boundaries.md`

### Current behavior

Backend architecture tests and frontend closed-world dependency manifest/generation gates exist and are substantial.

### Target behavior

Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ107` — Domain purity gates
- `PFREQ108` — Layer dependency gates

### Minimum allowed change

Tighten or add a gate only for a declared architecture invariant; fix implementation before weakening a failing rule.

### Non-goals

No test weakening to make candidate green; no generated doc as competing architecture authority.

### Compatibility / migration impact

Manifest semantic changes or new project/package edges affect repository architecture and require Architecture review.

### Verification and exit evidence

- Execute and record `PF-TST-ARCH-107, PF-TST-ARCH-108` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new project/service/framework or architecture-manifest semantic change until approved.

## PF-15-CORE-001 — Architecture and dependency enforcement mechanism closure

### Requirement coverage

```text
PFREQ109, PFREQ110
```

### Stable verification references

```text
PF-TST-ARCH-109, PF-TST-ARCH-110
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/tests/Notrelix.Architecture.Tests/`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`
- `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- `frontend/tooling/dependency-rules/src/check-package-manifests.ts`
- `frontend/tooling/dependency-rules/src/generate-architecture-docs.ts`
- `frontend/docs/generated/package-boundaries.md`

### Current behavior

Backend architecture tests and frontend closed-world dependency manifest/generation gates exist and are substantial.

### Target behavior

Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ109` — Authorization ownership gates
- `PFREQ110` — Bounded-context ownership gates

### Minimum allowed change

Tighten or add a gate only for a declared architecture invariant; fix implementation before weakening a failing rule.

### Non-goals

No test weakening to make candidate green; no generated doc as competing architecture authority.

### Compatibility / migration impact

Manifest semantic changes or new project/package edges affect repository architecture and require Architecture review.

### Verification and exit evidence

- Execute and record `PF-TST-ARCH-109, PF-TST-ARCH-110` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new project/service/framework or architecture-manifest semantic change until approved.

## PF-15-SEC-001 — Architecture and dependency enforcement security/isolation/failure closure

### Requirement coverage

```text
PFREQ111, PFREQ112
```

### Stable verification references

```text
PF-TST-ARCH-111, PF-TST-ARCH-112
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/tests/Notrelix.Architecture.Tests/`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`
- `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- `frontend/tooling/dependency-rules/src/check-package-manifests.ts`
- `frontend/tooling/dependency-rules/src/generate-architecture-docs.ts`
- `frontend/docs/generated/package-boundaries.md`

### Current behavior

Backend architecture tests and frontend closed-world dependency manifest/generation gates exist and are substantial.

### Target behavior

Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ111` — Frontend dependency manifest
- `PFREQ112` — Generated drift gates

### Minimum allowed change

Tighten or add a gate only for a declared architecture invariant; fix implementation before weakening a failing rule.

### Non-goals

No test weakening to make candidate green; no generated doc as competing architecture authority.

### Compatibility / migration impact

Manifest semantic changes or new project/package edges affect repository architecture and require Architecture review.

### Verification and exit evidence

- Execute and record `PF-TST-ARCH-111, PF-TST-ARCH-112` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new project/service/framework or architecture-manifest semantic change until approved.

## PF-15-COMPAT-001 — Architecture and dependency enforcement compatibility and consumer handoff

### Requirement coverage

```text
PFREQ113
```

### Stable verification references

```text
PF-TST-ARCH-113
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `backend/tests/Notrelix.Architecture.Tests/`
- `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs`
- `frontend/tooling/dependency-rules/src/architecture-manifest.ts`
- `frontend/tooling/dependency-rules/src/check-package-manifests.ts`
- `frontend/tooling/dependency-rules/src/generate-architecture-docs.ts`
- `frontend/docs/generated/package-boundaries.md`

### Current behavior

Backend architecture tests and frontend closed-world dependency manifest/generation gates exist and are substantial.

### Target behavior

Map Platform invariants to executable gates, keep generated inventories drift-checked and require explicit architecture approval to change semantic boundaries.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.

Covered requirement intent:
- `PFREQ113` — Gate-change governance

### Minimum allowed change

Tighten or add a gate only for a declared architecture invariant; fix implementation before weakening a failing rule.

### Non-goals

No test weakening to make candidate green; no generated doc as competing architecture authority.

### Compatibility / migration impact

Manifest semantic changes or new project/package edges affect repository architecture and require Architecture review.

### Verification and exit evidence

- Execute and record `PF-TST-ARCH-113` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop on new project/service/framework or architecture-manifest semantic change until approved.

## PF-16-INV-001 — CI, container, security and packaging evidence candidate inventory

### Requirement coverage

```text
PFREQ114, PFREQ115
```

### Stable verification references

```text
PF-TST-CI-114, PF-TST-CI-115
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`
- `tools/deliveryctl/architecture.py`
- `scripts/ci/validate-infra.py`
- `docs/delivery/ci-cd-architecture.md`

### Current behavior

Baseline exact-head CI evidence at `902bc9c5b39a003df3dce6673edc124984d2b398` is green (`CodeQL` run `36227690203`, `Notrelix CI` run `36227690334`); container workflow includes runtime non-root/health proof and Trivy/SBOM controls. These historical exact-head runs establish preparation posture only; final D5 still requires rerun/evidence bound to the accepted certification candidate.

### Target behavior

Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics.

### Required actions

- Inventory the listed production source, tests, DI/composition and current consumers at the exact candidate SHA. Classify each relevant mechanism `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE`; do not create new abstractions before this classification.

Covered requirement intent:
- `PFREQ114` — Suite non-zero execution
- `PFREQ115` — Dependency-aware final gate

### Minimum allowed change

Retain current green gates and close only evidence/provenance gaps. Never convert skipped prerequisite proof into green.

### Non-goals

No certification from historical runs alone; no rebuild of unverified bytes after validation for trusted publication.

### Compatibility / migration impact

Workflow/evidence-format changes affect repository-wide release gates and must preserve exact-SHA traceability.

### Verification and exit evidence

- Execute and record `PF-TST-CI-114, PF-TST-CI-115` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification if any required suite is zero/skipped, container proof is synthetic, or published bytes are not the validated artifact.

## PF-16-CORE-001 — CI, container, security and packaging evidence mechanism closure

### Requirement coverage

```text
PFREQ116, PFREQ117
```

### Stable verification references

```text
PF-TST-CI-116, PF-TST-CI-117
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`
- `tools/deliveryctl/architecture.py`
- `scripts/ci/validate-infra.py`
- `docs/delivery/ci-cd-architecture.md`

### Current behavior

Baseline exact-head CI evidence at `902bc9c5b39a003df3dce6673edc124984d2b398` is green (`CodeQL` run `36227690203`, `Notrelix CI` run `36227690334`); container workflow includes runtime non-root/health proof and Trivy/SBOM controls. These historical exact-head runs establish preparation posture only; final D5 still requires rerun/evidence bound to the accepted certification candidate.

### Target behavior

Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics.

### Required actions

- Implement or harden only the source-level mechanism needed to satisfy the covered requirements. Preserve already-correct contracts and keep changes inside the declared Platform ownership boundary.

Covered requirement intent:
- `PFREQ116` — Container runtime proof
- `PFREQ117` — Security/SBOM evidence

### Minimum allowed change

Retain current green gates and close only evidence/provenance gaps. Never convert skipped prerequisite proof into green.

### Non-goals

No certification from historical runs alone; no rebuild of unverified bytes after validation for trusted publication.

### Compatibility / migration impact

Workflow/evidence-format changes affect repository-wide release gates and must preserve exact-SHA traceability.

### Verification and exit evidence

- Execute and record `PF-TST-CI-116, PF-TST-CI-117` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification if any required suite is zero/skipped, container proof is synthetic, or published bytes are not the validated artifact.

## PF-16-SEC-001 — CI, container, security and packaging evidence security/isolation/failure closure

### Requirement coverage

```text
PFREQ118, PFREQ119
```

### Stable verification references

```text
PF-TST-CI-118, PF-TST-CI-119
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`
- `tools/deliveryctl/architecture.py`
- `scripts/ci/validate-infra.py`
- `docs/delivery/ci-cd-architecture.md`

### Current behavior

Baseline exact-head CI evidence at `902bc9c5b39a003df3dce6673edc124984d2b398` is green (`CodeQL` run `36227690203`, `Notrelix CI` run `36227690334`); container workflow includes runtime non-root/health proof and Trivy/SBOM controls. These historical exact-head runs establish preparation posture only; final D5 still requires rerun/evidence bound to the accepted certification candidate.

### Target behavior

Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics.

### Required actions

- Close negative, failure, tenant/security, retry/restart and bounded-resource behavior on the production graph. Unit-only mechanism evidence is insufficient where the contract crosses process/database/browser/broker boundaries.

Covered requirement intent:
- `PFREQ118` — Exact-source publication
- `PFREQ119` — Docs/generated governance

### Minimum allowed change

Retain current green gates and close only evidence/provenance gaps. Never convert skipped prerequisite proof into green.

### Non-goals

No certification from historical runs alone; no rebuild of unverified bytes after validation for trusted publication.

### Compatibility / migration impact

Workflow/evidence-format changes affect repository-wide release gates and must preserve exact-SHA traceability.

### Verification and exit evidence

- Execute and record `PF-TST-CI-118, PF-TST-CI-119` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification if any required suite is zero/skipped, container proof is synthetic, or published bytes are not the validated artifact.

## PF-16-COMPAT-001 — CI, container, security and packaging evidence compatibility and consumer handoff

### Requirement coverage

```text
PFREQ120
```

### Stable verification references

```text
PF-TST-CI-120
```

### Lane posture / target

```text
Preparation posture: IMPLEMENTED_UNCERTIFIED
Required readiness: D5
```

### Source to inspect

- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`
- `tools/deliveryctl/architecture.py`
- `scripts/ci/validate-infra.py`
- `docs/delivery/ci-cd-architecture.md`

### Current behavior

Baseline exact-head CI evidence at `902bc9c5b39a003df3dce6673edc124984d2b398` is green (`CodeQL` run `36227690203`, `Notrelix CI` run `36227690334`); container workflow includes runtime non-root/health proof and Trivy/SBOM controls. These historical exact-head runs establish preparation posture only; final D5 still requires rerun/evidence bound to the accepted certification candidate.

### Target behavior

Final candidate must execute required non-zero suites, dependency-aware aggregate gates, real container runtime health/non-root proof, security/SBOM evidence and exact-source publication semantics.

### Required actions

- Run compatibility/consumer evidence, update generated/migration/package artifacts when affected, and publish the full cross-team handoff. No silent consumer break is permitted.
- Bind final evidence to the exact candidate SHA. Historical baseline runs may be cited as baseline posture only.
- Record workflow run/job IDs, discovered/executed/passed/failed/skipped counts, container image digest, SBOM/security result and publication provenance.

Covered requirement intent:
- `PFREQ120` — Exact-candidate certification

### Minimum allowed change

Retain current green gates and close only evidence/provenance gaps. Never convert skipped prerequisite proof into green.

### Non-goals

No certification from historical runs alone; no rebuild of unverified bytes after validation for trusted publication.

### Compatibility / migration impact

Workflow/evidence-format changes affect repository-wide release gates and must preserve exact-SHA traceability.

### Verification and exit evidence

- Execute and record `PF-TST-CI-120` from the canonical executable scenarios in `platform-foundation.tests.md`.
- Include production-graph/integration evidence; unit tests alone cannot close this work unit.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts.
- Record affected consumers and any carried debt with owner + invalidation rule.

### Stop condition

Stop certification if any required suite is zero/skipped, container proof is synthetic, or published bytes are not the validated artifact.

## PF-GATE-001 — Wave 0 security/tenancy gate

### Requirement coverage

```text
PFREQ009, PFREQ015, PFREQ016, PFREQ022, PFREQ023, PFREQ029, PFREQ079, PFREQ085
```

### Stable verification references

```text
PF-TST-CSRF-009, PF-TST-CSRF-015, PF-TST-CTX-016, PF-TST-CTX-022, PF-TST-AUTHZ-023, PF-TST-AUTHZ-029, PF-TST-QUERY-079, PF-TST-QUERY-085
```

### Current gate state

Wave 0 includes PF-01, PF-02, PF-03 and PF-11. PF-02/PF-11 Account isolation is the known blocker.

### Target gate state

Protected browser and tenant-scoped frontend consumers meet D5 before being unblocked.

### Required actions

- Require PF-01 D5, PF-02 D5, PF-03 D5 and PF-11 D5 for critical consumers.
- Do not accept root-name-only account isolation.

### Stop condition

Block the gate while Account A/B isolation remains unproven.

## PF-GATE-002 — Wave 1 async/realtime gate

### Requirement coverage

```text
PFREQ044, PFREQ050, PFREQ051, PFREQ064, PFREQ065, PFREQ071
```

### Stable verification references

```text
PF-TST-IDEM-044, PF-TST-IDEM-050, PF-TST-MSG-051, PF-TST-ORDER-064, PF-TST-RT-065, PF-TST-RT-071
```

### Current gate state

Canonical Wave 1 is PF-06/PF-07/PF-08/PF-09; PF-04/PF-05 run alongside consumers that need persistence/schema work.

### Target gate state

Async consumers receive D5 delivery where required and realtime-heavy consumers receive D4+ owner-specific recovery.

### Required actions

- PF-07 stale claim crash recovery must close before generic messaging D5.
- PF-08 requires production runtime-owner truth, not Platform unit tests.
- PF-09 may be consumer-specific, but a realtime-critical consumer cannot bypass its recovery target.

### Stop condition

Block any consumer whose required delivery/recovery contract has no production proof.

## PF-GATE-003 — Wave 2 frontend/developer-throughput gate

### Requirement coverage

```text
PFREQ072, PFREQ099, PFREQ100, PFREQ106
```

### Stable verification references

```text
PF-TST-API-072, PF-TST-UI-099, PF-TST-OBS-100, PF-TST-OBS-106
```

### Current gate state

PF-10/PF-12/PF-13/PF-14 can progress in parallel after their contracts are stable; PF-13 CSS export is a concrete gap.

### Target gate state

Generated contracts/runtime/UI/observability are source-backed and consumer-compatible.

### Required actions

- Require generated API flow D5 for frontend consumers.
- Require UI export evidence at least D4 before UI handoff.
- Re-run runtime/observability composition evidence.

### Stop condition

Block PF-13 D4 while `@notrelix/ui-tokens/css` remains a dead export.

## PF-GATE-004 — Wave 3 architecture/CI gate

### Requirement coverage

```text
PFREQ107, PFREQ120, PFREQ128
```

### Stable verification references

```text
PF-TST-ARCH-107, PF-TST-CI-120, PF-TST-CROSS-128
```

### Current gate state

PF-15/PF-16 are continuous gates and must be rerun on the final candidate.

### Target gate state

Architecture and CI/security/container evidence reaches D5 at the exact candidate SHA.

### Required actions

- Run architecture/dependency gates and verify non-zero suites.
- Bind container/security/SBOM/publication evidence to exact candidate bytes.
- Publish per-lane readiness rather than `Platform complete`.

### Stop condition

Block final certification on skipped required proof, architecture drift or non-exact publication evidence.

# 11. Mandatory execution order and parallelism

1. Execute `PF-INV-001`, `PF-INV-002`, `PF-GOV-001`, `PF-GOV-002` first.
2. Wave 0:
   - PF-01 and PF-03 may be retained/certified in parallel after inventory.
   - PF-02 and PF-11 MUST be coordinated because Account key isolation is one shared client-state problem.
   - `PF-GATE-001` does not pass until the Account transition/key contract reaches D5.
3. Persistence support:
   - PF-04/PF-05 run only when a changed wave touches transaction/RLS/schema/init semantics.
   - They can run in parallel with their owning consumer change but must finish before that consumer certifies.
4. Wave 1:
   - PF-06 can certify independently per governed command consumer.
   - PF-07 stale command-owned claim recovery is a direct blocker for messaging D5.
   - PF-08 starts with the runtime-owner inventory before any ordering code is added.
   - PF-09 coordinates with PF-11 because canonical recovery keys depend on canonical query partitioning.
5. Wave 2:
   - PF-10, PF-12 and PF-14 can proceed in parallel if no shared contract changes conflict.
   - PF-13 must resolve the CSS export contract before UI D4.
6. PF-15/PF-16 run continuously and MUST be rerun after all implementation changes on the final candidate.
7. Execute the canonical `platform-foundation.tests.md` scenarios referenced by each work unit and bind their exact-candidate evidence into `platform-foundation.certification.md`; do not create replacement test/certification authorities.

# 12. Global STOP rules

Stop and escalate rather than improvising when any of the following becomes true:

- business aggregate/policy is being moved into Platform;
- a second authorization pipeline or policy engine is proposed;
- a generic cross-context repository/DbContext authority is proposed;
- CSRF requires a second browser protocol or weaker security policy;
- Account isolation relies only on the `account` root name;
- background work has no explicit trusted tenant/System semantics;
- command-owned durable `Processing` claims can survive indefinitely without recovery;
- production ordering is claimed only from `Notrelix.Platform.Tests`;
- retry/provider retry creates an uncontrolled amplification budget;
- realtime gap recovery has no authoritative owner or no post-recovery sequence convergence;
- a generated/exported path is declared but not source-backed;
- a failing architecture gate is weakened merely to obtain green;
- a required CI suite is skipped/zero yet the aggregate gate is green;
- service extraction/new project/framework/global state/security weakening is attempted without required approval.

# 13. Cross-team handoff schema

Every shared mechanism change MUST publish the canonical team handoff fields plus exact-candidate evidence:

```text
Platform capability:
Current contract:
Target contract:
Affected teams:
Breaking/additive:
Migration strategy:
Feature-team action:
Platform action:
Verification:
Required readiness:
Rollback/forward-fix:

Candidate SHA:
Source posture before:
Source posture after:
Certification:
Blocking debt:
Invalidating changes:
Exact tests/CI/runtime evidence:
```

Consumers MUST NOT reverse-engineer the new contract from source.

# 14. Final PLAN exit contract

This PLAN is complete for implementation handoff only when:

```text
72 / 72 stable work-unit IDs remain unique
128 / 128 PFREQ are covered by at least one work unit
128 / 128 stable PF-TST IDs resolve from covered PFREQ
BE-PLT-001..063 are routed to PFREQ → PLAN → PF-TST → target CERT
PF-FLOW-01..07 have explicit reuse + invalidation/rerun treatment
all source-confirmed gaps have exact source path + current/target + minimum change + non-goal + compatibility + stop condition
canonical wave semantics are preserved
full handoff schema is present
no fifth Platform execution file is introduced
```

Final delivery remains capability-specific. The handoff MUST NOT claim a blanket `Platform complete` state while any consumer-critical capability remains below its required readiness.
