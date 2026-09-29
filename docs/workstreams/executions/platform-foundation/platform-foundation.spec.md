---
document_id: WRK-SPEC-PLATFORM-FOUNDATION
document_type: workstream-spec
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
  - docs/architecture/system-overview.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
  - frontend/docs/architecture/dependency-boundaries.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/docs/architecture/ui-and-design-system.md
  - frontend/docs/architecture/testing-and-quality-gates.md
  - frontend/docs/generated/package-boundaries.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.tests.md
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
  - production-runtime-owner-change
  - service-extraction-proposal
  - exact-candidate-sha-change
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# SPEC — Platform & Foundation

## 1. Purpose

Platform/Foundation is the reusable technical spine for backend, frontend, delivery and architecture enforcement. It has no business bounded context. This SPEC converts existing team guidance and current-source reality into normative, testable requirements without duplicating TAC/ADR authority.

## 2. Authority order

```text
product/team ownership docs
→ accepted architecture docs / ADRs
→ executable architecture/dependency gates
→ this Platform execution package
→ candidate implementation
→ exact-SHA certification evidence
```

`backend-team-architecture-closure` is reusable mechanism evidence where applicable; it is not replaced by this package.

### Baseline authority-path reconciliation

At baseline SHA `902bc9c5b39a003df3dce6673edc124984d2b398`, the historical path `docs/workstreams/capability-map.md` is absent while the active source-backed delivery authority is `docs/workstreams/capability-delivery-map.md`. Any upstream team/workstream document that still names the historical path is `DOC_STALE` evidence; this execution package MUST use `capability-delivery-map.md` and MUST NOT recreate the missing legacy file merely to satisfy a stale reference.

## 3. Baseline

```text
PR #160 head: 902bc9c5b39a003df3dce6673edc124984d2b398
top-level Platform/Foundation execution package did not previously exist
exact-head CodeQL run 36227690203: SUCCESS
exact-head Notrelix CI run 36227690334: SUCCESS
final Platform certification state: NOT_EVALUATED
```

Exact-head CI existence is preparation evidence only. It MUST be bound requirement-by-requirement in `platform-foundation.certification.md` before any D4/D5 or STABLE claim is made.

## 4. Capability lanes

- `PF-01` — Session and CSRF transport
- `PF-02` — Actor / Account / Workspace context propagation
- `PF-03` — Authorization enforcement pipeline
- `PF-04` — Persistence and tenant-isolation foundation
- `PF-05` — Migration and database initialization
- `PF-06` — Idempotency
- `PF-07` — Outbox, consumer deduplication and message identity
- `PF-08` — Ordered delivery and poison handling
- `PF-09` — Realtime transport and recovery
- `PF-10` — API, OpenAPI and generated contracts
- `PF-11` — Frontend query/server-state foundation
- `PF-12` — Frontend runtime and host composition
- `PF-13` — UI tokens and primitive foundation
- `PF-14` — Observability
- `PF-15` — Architecture and dependency enforcement
- `PF-16` — CI, container, security and packaging evidence

## 5. Preparation-time source posture

| Lane | Source posture | Current audit summary |
|---|---|---|
| PF-01 Session and CSRF transport | `IMPLEMENTED_UNCERTIFIED` | Backend `CsrfValidationMiddleware`/`CsrfProtector`, ADR-003/005, frontend `csrf.ts`, API/frontend CSRF tests. Historical naming mismatch is resolved in current source; do not reopen it without contrary candidate evidence. |
| PF-02 Actor / Account / Workspace context propagation | `GAP_CONFIRMED` | `HttpRequestContextMiddleware`, `CurrentRequestContext`, `ExecutionContextBehavior/Snapshot`, frontend query keys/realtime lifecycle. `accountQueryKey` still omits Account ID; an atomic account hard-reset/partition proof is not established. |
| PF-03 Authorization enforcement pipeline | `IMPLEMENTED_UNCERTIFIED` | `AccessControlBehavior`, `AccessPolicyEngine`, `PostgresAccessFactsProvider`, seven-behavior DI/architecture tests. Runtime mechanism exists; exact-candidate D5 proof is still required. |
| PF-04 Persistence and tenant-isolation foundation | `IMPLEMENTED_UNCERTIFIED` | `DataSessionBehavior`, `IRequestDataSession`, `EfRequestDataSession`, RLS policies/tests and expected-version support exist. RLS remains defense-in-depth, not authorization; worker scope remains consumer-sensitive. |
| PF-05 Migration and database initialization | `IMPLEMENTED_UNCERTIFIED` | EF migration discipline/init/CI mechanisms exist and the repository has governed development rebaseline history. Supported-upgrade proof is release-boundary specific. |
| PF-06 Idempotency | `IMPLEMENTED_UNCERTIFIED` | `IdempotencyBehavior`, HTTP/OpenAPI filters, partition/fingerprint/store implementation and API/Application/Integration tests exist. Consumer/business operation identity remains owner-specific. |
| PF-07 Outbox, consumer deduplication and message identity | `PARTIAL_GAP` | Durable consumer dedup **does exist** through `MessagingProcessedEvent`, `MessageDeduplicationStore` and `DeduplicationConsumeFilter` keyed by `(EventId, ConsumerName)`. The remaining confirmed gap is stale `Processing` claim recovery for the command-owned path after process interruption between committed claim and command settlement/cleanup. |
| PF-08 Ordered delivery and poison handling | `PARTIAL_GAP` | `ConsumerHost`, `OrderingEnforcer` and `PoisonDetector` are real mechanism/test evidence in `Notrelix.Platform`, but production broker wiring is owned by MassTransit/Infrastructure and no audited production consumer definition currently proves `OrderingRequired = true`. Production ordering/poison certification therefore requires explicit runtime-owner/equivalence proof. |
| PF-09 Realtime transport and recovery | `GAP_CONFIRMED` | Reconnect, heartbeat, dedup and gap detection exist. Current Workspace recovery uses non-canonical query-key shapes and the sequence tracker has no explicit post-recovery checkpoint reconciliation, so one gap can fail to converge deterministically. |
| PF-10 API, OpenAPI and generated contracts | `IMPLEMENTED_UNCERTIFIED` | Canonical OpenAPI export, tracked artifacts, operation filters, frontend codegen and convention tests exist. |
| PF-11 Frontend query/server-state foundation | `GAP_CONFIRMED` | Canonical `global/account/workspace` query-key helpers exist. Workspace keys include ID; Account keys currently do not. |
| PF-12 Frontend runtime and host composition | `IMPLEMENTED_UNCERTIFIED` | Web/mobile runtime packages and app composition exist; production-like host composition must be certified per consumed mechanism. |
| PF-13 UI tokens and primitive foundation | `GAP_CONFIRMED` | `@notrelix/ui-tokens` exports `./css -> ./src/css/index.css`, but the audited source path is absent. No current consumer of that export was found in the audit, which limits migration blast radius but does not remove the packaging defect. |
| PF-14 Observability | `IMPLEMENTED_UNCERTIFIED` | Application tracing, backend metrics/health, messaging diagnostics and frontend observability/redaction packages exist. |
| PF-15 Architecture and dependency enforcement | `IMPLEMENTED_UNCERTIFIED` | Backend `Architecture.Tests` and executable frontend architecture-manifest/generated-boundary checks are green at the baseline; mapping them to Platform requirements remains certification work. |
| PF-16 CI, container, security and packaging evidence | `IMPLEMENTED_UNCERTIFIED` | Exact-head CodeQL run `36227690203` and Notrelix CI run `36227690334` succeeded for the baseline SHA; selected backend/frontend/docs/container suites are green. This is available evidence, not yet a formal PF-16 D5 certification. |

Source posture is not certification. Every lane begins `NOT_EVALUATED` for final candidate evidence.

## 6. Dependency-readiness targets

These targets are normative producer/consumer gates. `D4+` means the exact consumer-specific recovery/compatibility contract is proven at no less than D4; it is not a generic lane-wide D5 claim.

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

A consumer MUST remain dependency-blocked while a required target is unmet, even if unrelated Platform lanes are green.

## 7. Decision, escalation and service-extraction boundary

Platform may decide locally only private implementation shape, internal data structures, test fixtures and performance changes that preserve frozen contracts. The following MUST stop implementation and escalate to the appropriate Architecture/Security authority before code changes continue:

- a new cross-cutting framework;
- a new production project or service;
- an authorization-architecture change;
- a messaging delivery-guarantee change;
- a frontend architecture-manifest semantic change;
- a new global-state architecture;
- repository-wide dependency adoption;
- any security weakening.

Platform supports service extraction through transport, observability, messaging, deployment/security and migration mechanisms, but Platform MUST NOT choose bounded-context/service boundaries. A new production service/project boundary requires bounded-context ownership plus architecture approval/ADR before execution.

## 8. Canonical backend Platform rule crosswalk

`backend/docs/architecture/platform-and-messaging.md` remains canonical for `BE-PLT-001..063`. This SPEC does not copy those rules as a competing authority; it maps every rule to the Platform execution requirements that must carry it through PLAN/TESTS/CERTIFICATION. A future canonical rule addition or semantic change invalidates this map and triggers review.

| Canonical rule | Platform lane | Execution requirements | Disposition at preparation baseline |
|---|---|---|---|
| BE-PLT-001 | PF-01/PF-07/PF-08 | PFREQ001, PFREQ057 | Mapped |
| BE-PLT-002 | PF-07 | PFREQ052, PFREQ126 | Mapped |
| BE-PLT-003 | PF-07/PF-10 | PFREQ052, PFREQ076, PFREQ126 | Mapped |
| BE-PLT-004 | PF-07 | PFREQ057, PFREQ127 | Mapped |
| BE-PLT-005 | PF-07 | PFREQ052, PFREQ123 | Mapped |
| BE-PLT-006 | PF-07/PF-14 | PFREQ052, PFREQ100 | Mapped |
| BE-PLT-007 | PF-07 | PFREQ053, PFREQ126 | Mapped |
| BE-PLT-008 | PF-06/PF-07 | PFREQ046, PFREQ126 | Mapped |
| BE-PLT-009 | PF-07/PF-10 | PFREQ076, PFREQ126 | Mapped |
| BE-PLT-010 | PF-07/PF-10 | PFREQ076, PFREQ126 | Mapped |
| BE-PLT-011 | PF-07 | PFREQ051 | Mapped |
| BE-PLT-012 | PF-07/PF-08 | PFREQ051, PFREQ064, PFREQ126 | Mapped |
| BE-PLT-013 | PF-07 | PFREQ054, PFREQ056, PFREQ123 | Mapped — open crash-recovery debt |
| BE-PLT-014 | PF-07 | PFREQ056, PFREQ123 | Mapped |
| BE-PLT-015 | PF-07 | PFREQ053, PFREQ056 | Mapped |
| BE-PLT-016 | PF-06/PF-07 | PFREQ046, PFREQ056 | Mapped |
| BE-PLT-017 | PF-07/PF-08 | PFREQ056, PFREQ060, PFREQ124 | Mapped |
| BE-PLT-018 | PF-06/PF-07/PF-08 | PFREQ049, PFREQ104, PFREQ124 | Mapped |
| BE-PLT-019 | PF-06/PF-08 | PFREQ049, PFREQ125 | Mapped |
| BE-PLT-020 | PF-08/PF-14 | PFREQ104, PFREQ124 | Mapped |
| BE-PLT-021 | PF-08 | PFREQ062, PFREQ063 | Mapped |
| BE-PLT-022 | PF-07/PF-08 | PFREQ055, PFREQ124 | Mapped |
| BE-PLT-023 | PF-08 | PFREQ059, PFREQ125 | Mapped |
| BE-PLT-024 | PF-08 | PFREQ060 | Mapped |
| BE-PLT-025 | PF-08/PF-14 | PFREQ055, PFREQ061, PFREQ104 | Mapped |
| BE-PLT-026 | PF-07/PF-08 | PFREQ057, PFREQ127 | Mapped |
| BE-PLT-027 | PF-02/PF-04/PF-07 | PFREQ019, PFREQ036, PFREQ122 | Mapped |
| BE-PLT-028 | PF-01/PF-03/PF-14 | PFREQ010, PFREQ025, PFREQ121, PFREQ124 | Mapped — adapter-owned verification remains outside Platform |
| BE-PLT-029 | PF-06/PF-07/PF-14 | PFREQ044, PFREQ052, PFREQ100, PFREQ127 | Mapped |
| BE-PLT-030 | PF-07/PF-10 | PFREQ076, PFREQ126 | Mapped |
| BE-PLT-031 | PF-07/PF-10 | PFREQ076, PFREQ077, PFREQ126 | Mapped |
| BE-PLT-032 | PF-07/PF-08 | PFREQ055, PFREQ125, PFREQ126 | Mapped — replay is bounded/governed |
| BE-PLT-033 | PF-07/PF-08 | PFREQ055, PFREQ123 | Mapped |
| BE-PLT-034 | PF-07/PF-08 | PFREQ055, PFREQ124, PFREQ126 | Mapped |
| BE-PLT-035 | PF-02/PF-07/PF-08 | PFREQ122, PFREQ125 | Mapped |
| BE-PLT-036 | PF-07 | PFREQ057, PFREQ127 | Mapped — provider transport stays behind Platform contract |
| BE-PLT-037 | PF-08/PF-16 | PFREQ064, PFREQ120 | Mapped |
| BE-PLT-038 | PF-12/PF-14/PF-16 | PFREQ090, PFREQ105, PFREQ120 | Mapped |
| BE-PLT-039 | PF-06/PF-08 | PFREQ049, PFREQ123 | Mapped |
| BE-PLT-040 | PF-06/PF-07 | PFREQ044, PFREQ047, PFREQ123 | Mapped when scheduler capability is used |
| BE-PLT-041 | PF-07/PF-08 | PFREQ049, PFREQ125 | Mapped when durable delay is used |
| BE-PLT-042 | PF-14 | PFREQ100, PFREQ101, PFREQ103 | Mapped |
| BE-PLT-043 | PF-07/PF-14 | PFREQ055, PFREQ104, PFREQ105 | Mapped |
| BE-PLT-044 | PF-14 | PFREQ127 | Mapped |
| BE-PLT-045 | PF-07 | PFREQ053, PFREQ054, PFREQ056 | Mapped |
| BE-PLT-046 | PF-07 | PFREQ123, PFREQ126 | Mapped |
| BE-PLT-047 | PF-14 | PFREQ102, PFREQ121 | Mapped |
| BE-PLT-048 | PF-02/PF-03/PF-07 | PFREQ019, PFREQ028, PFREQ124 | Mapped |
| BE-PLT-049 | PF-07/PF-14 | PFREQ055, PFREQ106, PFREQ127 | Mapped |
| BE-PLT-050 | PF-07/PF-10 | PFREQ076, PFREQ126 | Mapped |
| BE-PLT-051 | PF-07 | PFREQ056, PFREQ126 | Mapped |
| BE-PLT-052 | PF-06/PF-07 | PFREQ044, PFREQ046, PFREQ052 | Mapped |
| BE-PLT-053 | PF-07/PF-08 | PFREQ055, PFREQ062, PFREQ126 | Mapped |
| BE-PLT-054 | PF-07/PF-08 | PFREQ057, PFREQ124 | Mapped |
| BE-PLT-055 | PF-07 | PFREQ051, PFREQ054, PFREQ055 | Mapped |
| BE-PLT-056 | PF-07/PF-08 | PFREQ124 | Mapped |
| BE-PLT-057 | PF-06/PF-08 | PFREQ049, PFREQ123, PFREQ125 | Mapped |
| BE-PLT-058 | PF-08 | PFREQ061, PFREQ125 | Mapped |
| BE-PLT-059 | PF-09 | PFREQ069, PFREQ070, PFREQ071, PFREQ127 | Mapped |
| BE-PLT-060 | PF-03/PF-07 | PFREQ024, PFREQ028, PFREQ057, PFREQ123 | Mapped |
| BE-PLT-061 | PF-06/PF-07 | PFREQ044, PFREQ047, PFREQ050, PFREQ123 | Mapped for financial consumers |
| BE-PLT-062 | PF-04/PF-07/PF-16 | PFREQ005, PFREQ030, PFREQ032, PFREQ054, PFREQ056, PFREQ120 | Mapped |
| BE-PLT-063 | PF-14/PF-16 | PFREQ005, PFREQ120, PFREQ124 | Mapped |

No `BE-PLT-*` rule may disappear from delivery because it lacks a dedicated PFREQ ID. PLAN/TESTS/CERTIFICATION MUST retain this crosswalk or a stricter one and must mark a rule `NOT_APPLICABLE` only with an explicit capability-specific rationale.

## 9. TAC `PF-FLOW-01..07` evidence-reuse crosswalk

Historical TAC evidence is reusable baseline evidence, never automatic current-candidate D5. Every reused flow requires exact-candidate rerun when its invalidation boundary intersects the candidate.

| TAC flow | Platform lanes / PFREQ | Reused source/test evidence | Preparation evidence state | Exact-candidate rerun | Invalidating changes |
|---|---|---|---|---|---|
| `PF-FLOW-01` request execution pipeline | PF-02/PF-03/PF-04; PFREQ017–019, PFREQ023–030 | MediatR behavior order + `ExecutionContext`; TAC `TAC-PF-FLOW-01` proof set | `REUSABLE_BASELINE_ONLY` | Yes | behavior order, context snapshot, data-session or authorization pipeline change |
| `PF-FLOW-02` DomainEvent → IntegrationEvent → outbox | PF-07; PFREQ051–055 | `DomainEventInterceptor`, integration-event mapping, durable outbox; TAC `TAC-PF-FLOW-02` proof set | `REUSABLE_BASELINE_ONLY` | Yes | event mapping, source transaction, outbox enrollment/state change |
| `PF-FLOW-03` broker delivery + tenant restoration + dedup | PF-02/PF-04/PF-07; PFREQ019, PFREQ032, PFREQ053, PFREQ056 | `TenantContextConsumeFilter`, `DeduplicationConsumeFilter`, persisted consumer claim; TAC `TAC-PF-FLOW-03` proof set | `REUSABLE_WITH_OPEN_GAP` | Yes | tenant restoration, consumer identity, claim lifecycle; command-owned stale-claim recovery is currently open |
| `PF-FLOW-04` delivery retry/failure | PF-07/PF-08/PF-14; PFREQ049, PFREQ055, PFREQ061–064, PFREQ104 | outbox dispatcher + production MassTransit retry/failure path; TAC `TAC-PF-FLOW-04` proof set | `REUSABLE_WITH_RUNTIME_OWNER_CHECK` | Yes | retry budget, terminal failure/dead-letter semantics, production runtime owner/wiring |
| `PF-FLOW-05` contract evolution/recovery capability | PF-07/PF-10; PFREQ076–078, PFREQ126 | `IntegrationEventCatalog`, `EventContractKey`, canonical event manifest and TAC evolution/recovery proof | `REUSABLE_WITH_OVERRIDE_REVIEW` | Yes | event version/schema/catalog, backlog compatibility, recovery capability |
| `PF-FLOW-06` background actor/security context | PF-02/PF-03/PF-04; PFREQ019, PFREQ028, PFREQ036 | `ExecutionContext` + system request markers; TAC `TAC-PF-FLOW-06` proof set | `REUSABLE_BASELINE_ONLY` | Yes | worker/system-context authority, RLS scope, authorization semantics |
| `PF-FLOW-07` scoped Integration Event tenant envelope | PF-02/PF-07; PFREQ017, PFREQ019, PFREQ052–053, PFREQ122 | scoped event envelope + `TenantContextConsumeFilter`; TAC `TAC-PF-FLOW-07` proof set | `REUSABLE_BASELINE_ONLY` | Yes | scope enum/envelope, tenant fallback, consumer restoration semantics |

## 10. Runtime-owner truth table for reliability mechanisms

Mechanism tests and production runtime proof are distinct evidence classes. A lane may be certified only from the runtime that actually executes production traffic.

| Capability | Reference/mechanism implementation | Production runtime owner | Preparation disposition | Required closure |
|---|---|---|---|---|
| transactional outbox | Application/Infrastructure outbox contracts and persistence | production source transaction + Infrastructure dispatcher | implemented, uncertified | exact transaction/enrollment/dispatcher proof |
| consumer dedup | `MessagingProcessedEvent`, `MessageDeduplicationStore`, `DeduplicationConsumeFilter` | Infrastructure/MassTransit consumer pipeline | partial gap | prove crash recovery for command-owned committed `Processing` claim |
| ordering | `Notrelix.Platform` `OrderingEnforcer` / `ConsumerHost` | production MassTransit/Infrastructure path for consumers that request ordering | partial gap | prove actual DI/wiring/equivalent semantics; isolated Platform unit proof is insufficient |
| poison/failure containment | `Notrelix.Platform` `PoisonDetector` plus outbox failure mechanics | production MassTransit/Infrastructure + durable outbox failure state | partial/consumer-specific | prove consumer/message identity, retry exhaustion and recovery on production graph |
| realtime gap recovery | frontend realtime client + app recovery policy | consuming web/runtime application | confirmed gap | canonical cache-key recovery + sequence-checkpoint convergence + failure/reconnect behavior |

# Global requirements

## PFREQ001 — Platform has no business bounded context

Platform/Foundation owns reusable technical mechanisms only and MUST NOT become semantic owner of business contexts. Platform mechanisms may support service extraction, but they MUST NOT select service boundaries or create a new production service/project without bounded-context ownership and architecture approval.

## PFREQ002 — Architecture authority precedence

Accepted architecture docs/ADRs and executable architecture gates remain structural authority; this execution package orchestrates delivery and does not redefine those boundaries. Any new cross-cutting framework, production project/service, authorization architecture, delivery guarantee, architecture-manifest semantic, global-state model, repository-wide dependency, or security weakening is an escalation decision and MUST NOT be decided unilaterally by the Platform implementation agent.

## PFREQ003 — TAC evidence reuse without duplicate authority

`backend-team-architecture-closure` evidence may satisfy Platform mechanisms when it proves the same contract, but this package MUST reclassify that evidence by Platform lane and MUST NOT invent a second contradictory mechanism. Reuse MUST record source/test evidence, evidence state, exact-candidate rerun requirement and invalidating changes; historical `VERIFIED` evidence MUST NOT be promoted directly to current D5.

## PFREQ004 — Brownfield-first execution

Every lane MUST inventory candidate-SHA source/tests first and classify `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE` before adding abstractions.

## PFREQ005 — Exact-candidate evidence discipline

Source existence, test existence and historical green CI are not certification. Final evidence MUST bind to the accepted candidate SHA.

## PFREQ006 — Consumer-specific readiness

Platform capabilities may reach D4/D5 independently. A consumer may proceed only when its exact required Platform contracts meet the readiness matrix in §6. Readiness is a dependency contract, not a lane-status label: unmet consumer-specific D4/D5 targets remain blocking even when neighboring lanes are certified.

## PFREQ007 — No broad cleanup PR

Unrelated Platform lanes MUST NOT be bundled into one unbounded foundation rewrite; execution follows lane-scoped work units and explicit dependency gates.

## PFREQ008 — No silent compatibility break

Shared mechanism changes MUST classify current contract, target contract, affected teams, breaking/additive status, migration strategy, feature-team action, Platform action, verification, required readiness, rollback/forward-fix and invalidation conditions before release. Persisted/backlogged messages, dedup/replay state, generated artifacts and old consumers count as compatibility surfaces.

# PF-01 — Session and CSRF transport

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ009 — Canonical browser CSRF contract

Protected browser mutations MUST use the single ADR-approved CSRF bootstrap/echo contract. Current source uses the HttpOnly `csrf_token` cookie plus bootstrap response body and `X-CSRF-Token` request header; legacy `XSRF-TOKEN` / `X-XSRF-TOKEN` conventions are forbidden.

## PFREQ010 — Unsafe-request classification

CSRF applicability MUST be decided by the shared API/Infrastructure classifier, not by feature-local endpoint code. Signature-authenticated provider callbacks may be exempt only through explicit endpoint metadata.

## PFREQ011 — Token secrecy and storage

The frontend MUST keep the CSRF token in instance-scoped memory from the bootstrap response. It MUST NOT read the API cookie or persist the token to localStorage/sessionStorage.

## PFREQ012 — Valid and invalid pair behavior

A valid browser session plus valid CSRF pair MUST proceed; missing or invalid CSRF MUST fail before the protected mutation.

## PFREQ013 — Session-expiry behavior

Session expiry and CSRF refresh/bootstrap behavior MUST remain typed and observable; the client MUST NOT hide session failure behind blind retry.

## PFREQ014 — Cross-origin production policy

Production cookie/CORS/credential settings MUST support the approved browser topology without weakening SameSite/Secure rules or broadening origins.

## PFREQ015 — CSRF contract compatibility evidence

Any CSRF cookie/header/bootstrap change MUST update backend/API tests, frontend contract tests and generated/public contract evidence atomically.

# PF-02 — Actor / Account / Workspace context propagation

Preparation posture: `GAP_CONFIRMED`.

## PFREQ016 — Distinct context identities

Authenticated actor, Account and Workspace MUST remain distinct runtime concepts. No opaque mutable global context may collapse their ownership.

## PFREQ017 — Trusted HTTP context resolution

Account/Workspace context MUST be derived through approved request middleware/context contracts and MUST reject spoofed or inconsistent scope.

## PFREQ018 — ExecutionContext immutability

Application handlers MUST consume the request snapshot/readers rather than mutating ambient request state during execution.

## PFREQ019 — Background scope reconstruction

Background work MUST reconstruct explicit trusted tenant/security context; lack of HTTP does not imply global or System authority.

## PFREQ020 — Account transition state isolation

Frontend Account transitions MUST prevent cached Account-A data, permission state, optimistic state or realtime subscriptions from leaking into Account B.

## PFREQ021 — Workspace query partitioning

Workspace-scoped server-state keys MUST include a non-empty Workspace ID and remain compatible with workspace changes.

## PFREQ022 — Account query-key closure

Because `accountQueryKey(resource, ...)` currently omits `accountId`, Platform MUST either add Account identity to the key or prove an atomic hard reset/partition mechanism before declaring Account server-state isolation D5.

# PF-03 — Authorization enforcement pipeline

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ023 — Single enforcement pipeline

Protected Application requests MUST flow through the canonical seven-behavior pipeline with `AccessControlBehavior` as the business-action enforcement point.

## PFREQ024 — Policy ownership separation

Governance owns permission policy; resource contexts own resource/business-action semantics; Platform/Application owns enforcement mechanics. Platform MUST NOT implement a second policy engine.

## PFREQ025 — Facts before policy

Authorization MUST evaluate trusted server-side facts through the approved facts/evaluator seams. Client claims and route presence alone are not authorization facts.

## PFREQ026 — DataSession ordering

`DataSessionBehavior` MUST precede `AccessControlBehavior` where the facts provider requires the active connection/transaction.

## PFREQ027 — Unauthorized side-effect exclusion

Denied or malformed protected requests MUST NOT execute protected handler side effects.

## PFREQ028 — System-internal exception

The explicit `ISystemInternalRequest` contract may use the frozen trusted System path; ordinary background/user requests MUST NOT be able to opt into it.

## PFREQ029 — Architecture guard

Architecture tests MUST prevent reintroduction of legacy parallel `AuthorizationBehavior` / `PermissionService` / handler-local RBAC authority.

# PF-04 — Persistence and tenant-isolation foundation

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ030 — Request data-session boundary

Write requests MUST use the approved request data-session/transaction mechanism and must not create ad-hoc nested transaction authority.

## PFREQ031 — Owner-local persistence

Shared persistence helpers MUST preserve bounded-context ownership; Platform MUST NOT expose a generic cross-context repository or universal business DbContext.

## PFREQ032 — RLS session context

Tenant-scoped persistence MUST set and clear the PostgreSQL request/session context according to the approved transaction-local mechanism.

## PFREQ033 — RLS defense-in-depth

RLS proves persistence isolation and MUST NOT be documented as a replacement for Application authorization.

## PFREQ034 — Cross-context private-table rule

Foreign private-table reads remain exceptional and explicitly classified; a shared database is not permission to create hidden business coupling.

## PFREQ035 — Concurrency/version support

Shared persistence mechanisms MUST preserve optimistic-concurrency/expected-version semantics required by product aggregates.

## PFREQ036 — Worker persistence scope

Tenant worker paths MUST have explicit Platform-approved scope semantics before they are treated as tenant-safe; broad worker bypass is not a default capability.

# PF-05 — Migration and database initialization

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ037 — Migration authority

EF migration history and model snapshot are canonical schema evolution evidence; generated/model drift MUST be detected rather than suppressed.

## PFREQ038 — Clean database proof

Schema-affecting delivery MUST prove clean database creation/startup for the supported environment.

## PFREQ039 — Supported upgrade proof

Where an upgrade boundary is claimed, the supported previous state MUST migrate successfully with data/security semantics preserved.

## PFREQ040 — Pending model changes

`PendingModelChangesWarning` or equivalent model drift MUST block certification unless resolved by an intentional governed rebaseline.

## PFREQ041 — RLS deployment order

Relational migration and RLS policy deployment ordering MUST be explicit and reproducible.

## PFREQ042 — Initialization failure visibility

Database/runtime initialization failure MUST be observable and MUST NOT degrade into a partially initialized service that reports healthy.

## PFREQ043 — Migration discipline

History rewrites are allowed only inside the documented development rebaseline policy; outside that window migrations are append-only or require explicit architecture approval.

# PF-06 — Idempotency

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ044 — Explicit operation identity

Each idempotent request MUST declare a stable operation identity; raw client keys are insufficient as the sole namespace.

## PFREQ045 — Tenant partitioning

Idempotency state MUST be partitioned by the required Account/tenant scope so identical raw keys in different tenants cannot collide.

## PFREQ046 — Payload fingerprint

A repeated key with a materially different request payload MUST fail deterministically rather than replaying an unrelated result.

## PFREQ047 — Concurrent duplicate coordination

Concurrent duplicates MUST coordinate through the canonical store/behavior and MUST NOT execute the business mutation twice.

## PFREQ048 — Success replay

A completed duplicate MUST return the approved persisted/reconstructed result semantics without re-running the mutation.

## PFREQ049 — Failure/retry semantics

Failed/incomplete idempotency state MUST have explicit retry/recovery semantics and cannot be treated as successful completion.

## PFREQ050 — Producer ownership

Business teams own which operation is idempotent and the logical operation identity; Platform owns the reusable execution/store mechanism.

# PF-07 — Outbox, consumer deduplication and message identity

Preparation posture: `PARTIAL_GAP`.

## PFREQ051 — Transactional outbox

Published integration/realtime intents that require atomicity MUST be persisted with the producing business transaction through the approved outbox mechanism.

## PFREQ052 — Stable message identity

Every delivered envelope MUST carry non-empty message identity suitable for retry, deduplication and poison tracking.

## PFREQ053 — Consumer identity

Consumer/dedup state MUST include enough consumer identity to prevent unrelated consumers from sharing completion state accidentally.

## PFREQ054 — Outbox claiming and reclaim

Concurrent dispatchers MUST claim/reclaim durable work without duplicate committed delivery state or permanent starvation. Any durable `Processing`/claimed state MUST have an explicit lease, stale-claim reclamation or equivalent recovery owner so a process crash cannot make eligible work permanently invisible.

## PFREQ055 — Delivery, dead-letter and recovery state visibility

Retry/attempt/terminal delivery state MUST be observable through approved diagnostics/health mechanisms using semantic identities. Retry-exhausted/dead-letter state is recovery input, not deletion. Replay/re-drive MUST be bounded, tenant/consumer-scoped, checkpointed after successful units, governed when forced, and observable for backlog age/freshness without dumping sensitive payloads.

## PFREQ056 — Durable consumer claim/dedup discipline

Current production source has a durable consumer dedup/claim mechanism through `MessagingProcessedEvent`, `MessageDeduplicationStore` and `DeduplicationConsumeFilter`, keyed by `(EventId, ConsumerName)`. Certification MUST prove duplicate, conflicting-identity, concurrent-delivery, rollback and retry semantics per logical consumer. The command-owned path MUST additionally prove process-crash recovery after a durable `Processing` claim commits but before command settlement/cleanup; a stale claim MUST NOT permanently suppress redelivery or the intended business effect.

## PFREQ057 — Event meaning and transport ownership

Platform owns reusable envelope/delivery/runtime mechanics only; producer and consumer business contexts retain event meaning and reaction semantics. Provider-specific broker/persistence wiring remains Infrastructure-owned behind Platform contracts. Generic hosts/diagnostics MUST NOT acquire product policy or become permission to mutate target-context state directly.

# PF-08 — Ordered delivery and poison handling

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ058 — Ordering requires real sequence and production adoption

An ordering-enabled consumer MUST reject an envelope without an explicit sequence; Platform MUST NOT synthesize one. A production ordering claim also requires proof that the actual production runtime owner wires or implements the required ordering semantics; `Notrelix.Platform.Tests` alone cannot certify MassTransit/Infrastructure delivery.

## PFREQ059 — Partition ordering

Ordering MUST be scoped by the approved partition identity and MUST allow unrelated partitions to make progress independently.

## PFREQ060 — Commit after handler success

Ordering state MUST advance only after successful handler completion; failed handling MUST permit correct retry of the same sequence.

## PFREQ061 — Backpressure is observable

Concurrency-slot timeout/backpressure MUST fail observably for transport retry rather than dropping the message.

## PFREQ062 — Poison identity

Poison tracking MUST be keyed by real message identity, not only event-name string. Current source uses `(eventName, messageId)`.

## PFREQ063 — Poison does not poison siblings

A terminally failing message MUST NOT make another message of the same event type poison by association.

## PFREQ064 — Restart/durability and runtime-owner classification

Process-local ordering/poison state MUST be documented honestly. Durable delivery/dead-letter guarantees belong to the production transport/store evidence, not to in-memory counters or isolated mechanism tests. For each consumer requiring ordering/poison guarantees, certification MUST identify the production runtime owner, DI/wiring, durable state owner and exact runtime proof; otherwise the capability remains `PARTIAL_GAP` or explicitly `NOT_APPLICABLE` for that consumer.

# PF-09 — Realtime transport and recovery

Preparation posture: `GAP_CONFIRMED`.

## PFREQ065 — Connection lifecycle

Realtime foundation MUST expose deterministic connect/reconnect/offline/close state and bounded reconnect/heartbeat behavior.

## PFREQ066 — Subscription scoping

Subscriptions MUST carry explicit Workspace/tenant scope and MUST be rebuilt safely after reconnect/context change.

## PFREQ067 — Message validation and dedup

Malformed envelopes MUST be rejected/observed, and duplicate event IDs MUST be boundedly deduplicated.

## PFREQ068 — Gap detection and recovery entry

Ordered envelopes MUST detect sequence gaps per subscription/workspace rather than silently delivering an inconsistent stream. Gap detection MUST enter an explicit recovery state that defines whether later envelopes are suspended, dropped, buffered or otherwise controlled until convergence.

## PFREQ069 — Consumer recovery contract

Each realtime-critical consumer MUST define what happens after a detected gap: authoritative reload, replay, checkpoint/rebase or another explicit mechanism. Recovery MUST target canonical query/cache identities rather than ad-hoc key shapes and MUST define deterministic behavior for recovery failure and retry.

## PFREQ070 — Invalidation is not universal recovery

The current web Workspace recovery policy invalidates key shapes that do not fully match canonical query identities. Platform MUST NOT certify generic ordered-stream recovery from invalidation alone. The owning recovery policy MUST prove that the authoritative state actually refreshed/reconciled is the state represented by canonical query keys.

## PFREQ071 — Post-recovery convergence guarantee

WorkManagement/Documents/Collaboration realtime consumers MUST prove their final consistency guarantee after duplicate, out-of-order, gap and reconnect scenarios before their recovery dependency is D4+/D5. Successful authoritative recovery MUST reconcile/reset the sequence checkpoint so the next valid sequence is delivered normally; failed recovery, duplicate-after-recovery, out-of-order-after-recovery and disconnect/reconnect-during-recovery MUST have deterministic outcomes.

# PF-10 — API, OpenAPI and generated contracts

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ072 — OpenAPI producer authority

Backend API is the canonical transport/OpenAPI producer; Domain/Application semantics MUST NOT be inferred from generated clients.

## PFREQ073 — Deterministic export

A fresh canonical OpenAPI export MUST be byte/semantic compatible with the tracked artifact for the same source candidate.

## PFREQ074 — Security metadata

OpenAPI MUST accurately expose authentication/authorization/idempotency transport requirements without leaking internal policy implementation.

## PFREQ075 — Generated frontend contracts

Frontend generated contracts MUST derive from canonical artifacts and MUST NOT be hand-maintained copies of server DTOs where generated coverage exists.

## PFREQ076 — Breaking-change review

Breaking producer changes MUST be classified, reviewed with affected consumers and migrated compatibly.

## PFREQ077 — Realtime contract generation

Realtime envelope/generated contract artifacts MUST be versioned and drift-checked alongside OpenAPI where applicable.

## PFREQ078 — Consumer compile gate

Contract changes MUST run codegen/typecheck/consumer tests before being declared compatible.

# PF-11 — Frontend query/server-state foundation

Preparation posture: `GAP_CONFIRMED`.

## PFREQ079 — Canonical query-key roots

Server-state keys MUST use the approved `global`, `account` or `workspace` roots and reject malformed workspace keys.

## PFREQ080 — Workspace identity in keys

Workspace-scoped keys MUST include Workspace ID so concurrent/transitioned workspaces cannot alias.

## PFREQ081 — Account isolation

Account-scoped query identity/reset MUST prevent Account A cache from being observable as Account B.

## PFREQ082 — Optimistic update ownership

Generic optimistic-command infrastructure may coordinate cache mechanics; business teams own mutation meaning, rollback and invalidation semantics.

## PFREQ083 — No duplicate server truth

Frontend query/cache state is derived state and MUST NOT become an independent source of business truth.

## PFREQ084 — Account-switch proof

Execution evidence MUST exercise A→B→A account transition with cached resources, permissions and realtime state.

## PFREQ085 — Generator compliance

Feature generators/templates MUST emit query keys and tests consistent with the final Account/Workspace isolation contract.

# PF-12 — Frontend runtime and host composition

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ086 — Runtime composition root

Environment/service/runtime construction MUST occur in approved runtime/host composition roots rather than feature packages.

## PFREQ087 — Host-specific adapters

Web/mobile/marketing host framework concerns MUST remain in their host/runtime packages; reusable foundation packages cannot assume one host.

## PFREQ088 — Typed session-expired plumbing

Generic auth/runtime infrastructure MUST emit typed session-expired state/events while the app host owns navigation policy.

## PFREQ089 — Realtime factory ownership

Web/mobile-specific WebSocket factories belong to runtimes while protocol/connection state remains foundation-owned.

## PFREQ090 — Environment validation

Required runtime configuration MUST fail fast and observably when invalid or absent.

## PFREQ091 — No route hardcoding in foundation

Generic foundation code MUST NOT own product route strings when routing authority belongs to app hosts.

## PFREQ092 — Composition tests

Each supported host MUST have production-like composition evidence for the shared services it consumes.

# PF-13 — UI tokens and primitive foundation

Preparation posture: `GAP_CONFIRMED`.

## PFREQ093 — Token vs product component boundary

Shared UI MUST distinguish generic design token/primitive from product-semantic component and host-specific composition.

## PFREQ094 — CSS export validity

`@notrelix/ui-tokens` declares `./css -> ./src/css/index.css`, but the audited source path is absent. This export MUST either be backed by real source/build output or removed/migrated intentionally.

## PFREQ095 — Theme/token stability

Primitive/semantic tokens and themes MUST be versioned/changed with consumer impact review rather than duplicated inside feature packages.

## PFREQ096 — Accessibility foundation

Shared primitives MUST preserve keyboard, focus, reduced-motion and semantic accessibility expectations required by the quality standard.

## PFREQ097 — Responsive foundation

Shared layout/primitives MUST support approved viewport classes without embedding WorkManagement/Documents-specific product behavior.

## PFREQ098 — Evidence manifests

Critical UI foundation surfaces MUST participate in the repository's owner-local visual/a11y evidence mechanism where applicable.

## PFREQ099 — No business semantics in primitives

Platform UI primitives MUST NOT encode roles, entitlements, Board/Page state or other bounded-context business rules.

# PF-14 — Observability

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ100 — Correlation propagation

HTTP/Application/messaging/realtime paths MUST preserve safe correlation/operation identity across the supported chain.

## PFREQ101 — Trace and metric mechanics

Platform owns reusable tracing/metrics/logging mechanics; business teams own domain-specific metric meaning.

## PFREQ102 — Secret redaction

Observability MUST redact credentials, authorization headers, session/CSRF/provider secrets and other reusable sensitive material.

## PFREQ103 — Tenant-safe context

Account/Workspace identifiers may be attached only in safe bounded form; telemetry MUST NOT leak another tenant's data or create unsafe high-cardinality labels.

## PFREQ104 — Retry/failure signals

Async delivery and idempotency/realtime mechanisms MUST expose bounded retry/failure/terminal state signals.

## PFREQ105 — Health checks

Database, Redis, messaging and outbox health checks MUST reflect meaningful readiness/dependency state rather than only process liveness.

## PFREQ106 — Failure observability

Initialization, pipeline and background failures MUST be discoverable through logs/metrics/traces without swallowing the actual failure.

# PF-15 — Architecture and dependency enforcement

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ107 — Domain purity gates

Executable architecture tests MUST prevent framework/Infrastructure/API leakage into Domain.

## PFREQ108 — Layer dependency gates

Backend project/layer dependencies MUST remain within accepted architecture and ADR boundaries.

## PFREQ109 — Authorization ownership gates

Architecture tests MUST forbid feature-local/legacy authorization authority and protect the canonical pipeline.

## PFREQ110 — Bounded-context ownership gates

Cross-context private persistence or semantic ownership violations MUST be caught where the repository has declared enforceable boundaries.

## PFREQ111 — Frontend dependency manifest

`frontend/tooling/dependency-rules/src/architecture-manifest.ts` remains executable frontend dependency authority; generated docs are evidence, not competing authority.

## PFREQ112 — Generated drift gates

Generated project/package/contract inventories MUST be drift-checked rather than manually edited as canonical authority.

## PFREQ113 — Gate-change governance

A failing architecture gate MUST be fixed in implementation or changed through explicit architecture approval; tests are not weakened merely for convenience.

# PF-16 — CI, container, security and packaging evidence

Preparation posture: `IMPLEMENTED_UNCERTIFIED`.

## PFREQ114 — Suite non-zero execution

Critical CI jobs MUST prove the intended suites executed non-zero tests; a skipped/empty suite cannot be interpreted as success.

## PFREQ115 — Dependency-aware final gate

Aggregate gates MUST distinguish success, failure and skipped prerequisite jobs rather than converting a skipped required proof into green.

## PFREQ116 — Container runtime proof

Container validation MUST run the built image as non-root and probe a real health/HTTP route. Current container workflow does this for backend/web/marketing.

## PFREQ117 — Security/SBOM evidence

Selected images MUST pass the governed vulnerability scan and produce the required SBOM/provenance evidence.

## PFREQ118 — Exact-source publication

Trusted publication MUST publish the exact validated image bytes for the recorded source SHA rather than rebuilding unverified bytes.

## PFREQ119 — Docs/generated governance

CI MUST fail on governed documentation/generated-artifact drift and MUST NOT silently rewrite canonical artifacts.

## PFREQ120 — Exact-candidate certification

Platform STABLE/D5 claims MUST record exact source SHA, CI run/job evidence and required runtime/security/container proofs. For baseline `902bc9c5b39a003df3dce6673edc124984d2b398`, CodeQL run `36227690203` and Notrelix CI run `36227690334` are available successful exact-head evidence, but remain `NOT_EVALUATED` for Platform certification until bound to the applicable PFREQ/PFAC records.

# Cross-cutting requirements

## PFREQ121 — Cross-lane secret safety

No Platform mechanism may expose reusable credentials/secrets in logs, generated artifacts, client persistence, messages or diagnostics outside the intended transport boundary.

## PFREQ122 — Cross-lane tenant isolation

Account/Workspace isolation MUST be preserved across HTTP, Application, persistence, messaging, realtime, frontend cache and observability.

## PFREQ123 — Cross-lane idempotent/retry safety

Retries across HTTP, outbox, consumers and realtime recovery MUST not create duplicate business side effects.

## PFREQ124 — Cross-lane failure transparency

Platform failures MUST fail closed or fail observably according to the contract; silent success is forbidden.

## PFREQ125 — Cross-lane performance bounds

Shared mechanisms MUST use bounded memory/retry/query/queue behavior and MUST not impose unbounded scans or high-cardinality telemetry by default.

## PFREQ126 — Cross-lane compatibility and migration

Changes to keys, envelopes, operation identities, generated contracts, RLS/session variables, UI exports, delivery state or CI evidence formats require explicit compatibility/migration review. Old consumers, queued/outbox/dead-letter/replay bytes, persisted dedup/order/checkpoint state and supported upgrade baselines are deployed compatibility surfaces and MUST be inventoried before the change is accepted.

## PFREQ127 — Cross-lane source-of-truth rule

Caches, projections, generated clients, telemetry and delivery records are supporting technical state and MUST NOT become competing business sources of truth.

## PFREQ128 — Platform exit/handoff contract

Platform certification MUST publish capability-by-capability readiness, blocking debt, consumers affected and invalidation rules; there is no single blanket `Platform done` claim that hides lane gaps. Every shared-mechanism handoff MUST record: `Platform capability`, `Current contract`, `Target contract`, `Affected teams`, `Breaking/additive`, `Migration strategy`, `Feature-team action`, `Platform action`, `Verification`, `Required readiness`, `Rollback/forward-fix`, plus candidate SHA and exact evidence references.

# Acceptance criteria

| ID | Acceptance criterion |
|---|---|
| PFAC001 | No business semantic ownership moves into Platform/Foundation. |
| PFAC002 | Browser session/CSRF contract is one interoperable backend/frontend protocol. |
| PFAC003 | Actor/Account/Workspace isolation is preserved across request, worker and frontend state. |
| PFAC004 | One canonical authorization enforcement pipeline remains production authority. |
| PFAC005 | Request data-session/RLS/migration mechanisms preserve tenant and schema correctness. |
| PFAC006 | Idempotency prevents duplicate governed mutations across concurrency/retry. |
| PFAC007 | Outbox/message identity/durable consumer dedup are atomic, observable and crash-recoverable; a stale committed claim cannot permanently suppress required work. |
| PFAC008 | Ordered delivery advances only after handler success; poison identity is message-specific; the certified guarantee is proven on the actual production runtime owner. |
| PFAC009 | Realtime consumers have explicit gap recovery that targets canonical state and converges the sequence checkpoint, not merely connection retry/query invalidation. |
| PFAC010 | OpenAPI/realtime/generated contracts are deterministic and consumer-verified. |
| PFAC011 | Frontend query/cache identity prevents Account/Workspace cross-scope leakage. |
| PFAC012 | Runtime/host composition keeps framework concerns out of reusable feature/foundation code. |
| PFAC013 | UI token/primitive exports are real, accessible and free of product business semantics. |
| PFAC014 | Observability is correlated, secret-safe and operationally meaningful. |
| PFAC015 | Backend/frontend architecture rules are executable and cannot be weakened ad hoc. |
| PFAC016 | CI evidence is bound to the exact candidate and proves non-zero tests, runtime containers, security/SBOM and exact-source publication; green workflow existence alone is not certification. |
| PFAC017 | Cross-lane secret/tenant/retry/failure/performance invariants remain intact. |
| PFAC018 | Certification publishes per-capability readiness/debt and the full cross-team handoff contract; no blanket Platform-complete shortcut. |

# Stop conditions

- STOP if a proposed shared abstraction contains business-context-specific policy that belongs to a bounded context.
- STOP and escalate if implementation proposes a new cross-cutting framework, production project/service, authorization architecture, delivery guarantee, global-state model, repository-wide dependency or any security weakening.
- STOP if Platform mechanism work is being used to select a service boundary without bounded-context ownership and architecture approval/ADR.
- STOP if a feature asks Platform to bypass authorization, tenant isolation, CSRF, RLS, idempotency or architecture gates for convenience.
- STOP if source/test existence or historical TAC evidence is being converted directly into current `VERIFIED`, `STABLE` or D5 without exact-candidate execution and invalidation review.
- STOP if a shared-contract change lacks the full handoff: current/target contract, affected teams, compatibility/migration, feature-team action, Platform action, readiness, verification and rollback/forward-fix.
- STOP if any applicable `BE-PLT-001..063` rule or `PF-FLOW-01..07` flow is neither mapped nor explicitly `NOT_APPLICABLE` with rationale.
- STOP if the command-owned consumer path can retain a committed stale `Processing` claim that permanently suppresses redelivery after process interruption.
- STOP if production ordering/poison guarantees are inferred solely from `Notrelix.Platform` mechanism/unit tests without production MassTransit/Infrastructure runtime-owner proof.
- STOP if account query isolation is declared D5 while `accountQueryKey` still aliases tenants and no atomic reset proof exists.
- STOP if realtime recovery uses non-canonical query keys or cannot reconcile its sequence checkpoint after successful recovery.
- STOP if realtime recovery is declared D5 from query invalidation alone for a consumer that requires ordered/replay consistency.
- STOP if `@notrelix/ui-tokens/css` remains exported without a real source/build artifact.
- STOP if a schema/event/routing/consumer change ignores old queue/outbox/dead-letter/replay bytes or persisted dedup/order state.
- STOP if CI reports required skipped/zero-execution evidence as success or if successful exact-head runs are treated as certification without PFREQ/PFAC evidence binding.
