---
document_id: DEL-CROSS-TEAM-DEPENDENCIES
document_type: dependency-map
status: active
owner: delivery-governance
applies_to:
  - cross-team-delivery
  - bounded-context-dependencies
  - contract-handoffs
  - event-handoffs
  - frontend-backend-integration
  - parallel-feature-delivery
evidence:
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/capability-delivery-map.md
  - docs/delivery/team-ownership.md
review_on:
  - cross-context-contract-change
  - team-ownership-change
  - event-contract-change
  - authorization-boundary-change
  - data-ownership-change
  - service-extraction-decision
---

# Cross-Team Dependencies

## 1. Purpose

This document makes cross-team dependencies explicit so parallel feature development does not create hidden coupling.

It is used when:

- one bounded context consumes another context's contract;
- a feature requires both backend and frontend ownership;
- a domain event becomes an automation, collaboration, notification, analytics or integration input;
- a team requires a shared Platform/Foundation mechanism;
- a change crosses authorization, tenancy, persistence, realtime or billing boundaries;
- delivery order must be coordinated.

This document does not redefine the business model of any bounded context.

## 2. Dependency principles

### Producer owns meaning

The context that owns a business fact owns the canonical contract/event meaning for that fact.

Consumers may request change.

Consumers may not silently redefine the producer's semantics.

### Consumer owns consumption behavior

A consumer owns:

- how it reacts;
- its local state;
- its retry/business response;
- its user-facing behavior;
- its derived read model.

The producer does not absorb consumer-specific logic merely to avoid coordination.

### No cross-context private persistence access by default

A team must not solve a dependency by reading or writing another bounded context's private persistence model.

Preferred integration mechanisms are:

- application/API contract;
- explicit internal contract;
- domain/integration event;
- derived read model;
- approved composition layer.

### Shared mechanism does not imply shared business ownership

Platform can own:

- transport;
- retries;
- serialization mechanism;
- tenancy plumbing;
- authorization enforcement pipeline;
- realtime transport;
- observability.

Platform does not own:

- what a Workspace means;
- who may edit a Board;
- when an Automation should execute;
- whether a Subscription grants an entitlement.

## 3. Team identifiers used in this map

| Short name | Team |
|---|---|
| IA | Identity & Accounts |
| WG | Workspace & Governance |
| WM | Work Management |
| DC | Documents & Collaboration |
| AI | Automation & Integrations |
| BE | Billing & Entitlements |
| AR | Analytics & Reporting |
| PF | Platform & Foundation |

These identifiers are delivery shorthand only. They are not rule IDs, bounded-context IDs, package names, service names or ADR IDs.

## 4. Bounded-context producer map

| Bounded context | Primary team | Primary facts produced |
|---|---|---|
| Accounts | IA | account identity/lifecycle/context |
| Identity | IA | user identity, authentication/session/security facts |
| Workspaces | WG | workspace identity/lifecycle/membership/invitation |
| Governance | WG | role/permission/share policy facts |
| WorkManagement | WM | board/item/field/checklist/work-state facts |
| Documents | DC | page/block/document state |
| Collaboration | DC | comment/collaboration state |
| Automation | AI | rule/execution state |
| Integrations | AI | external connection/provider interaction state |
| Billing | BE | plan/subscription/entitlement/usage facts |
| Analytics / Reporting | AR | derived analytical/read-model facts |

## 5. High-level dependency graph

```text
Accounts ──────────────┐
Identity ──────────────┼────> Workspaces ─────> WorkManagement
                      │            │                  │
                      │            └────> Documents   │
                      │                               │
                      └────> Governance ──────────────┤
                                                     ├────> Collaboration
                                                     ├────> Automation
                                                     └────> Analytics

Billing ───────────────> entitlement consumers across product contexts

Integrations <─────────> Automation

Platform/Foundation ───> all contexts as mechanism provider

Source contexts ───────> Analytics / Reporting as downstream consumer
```

This diagram is directional ownership guidance, not a complete runtime call graph.

## 6. Dependency matrix

Legend:

- `P`: producer dependency — row team consumes facts/contracts from column team;
- `M`: mechanism dependency — row team consumes shared mechanism from Platform;
- `C`: coordinated peer dependency;
- `-`: no standing dependency expected.

| Consumer \ Provider | IA | WG | WM | DC | AI | BE | AR | PF |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| IA | - | C | - | - | - | P | - | M |
| WG | P | - | - | - | - | P | - | M |
| WM | P | P | - | C | C | P | - | M |
| DC | P | P | P | - | C | P | - | M |
| AI | P | P | P | P | - | P | - | M |
| BE | P | C | - | - | C | - | - | M |
| AR | P | P | P | P | P | P | - | M |
| PF | C | C | C | C | C | C | C | - |

The matrix does not authorize synchronous calls merely because a dependency exists.

Integration style is selected from the canonical contract/event/data architecture.

## 7. Accounts dependencies

### Accounts → Identity

Use when an account operation requires actor identity.

Identity owns authentication.

Accounts owns account lifecycle.

Do not put account business state in Identity merely because the authenticated principal carries an account claim/context.

### Accounts → Billing

Billing may attach subscription/entitlement facts to an account identity.

Accounts must not duplicate plan/entitlement rules.

Billing must not control account domain lifecycle unless a canonical product rule explicitly defines a lifecycle dependency.

### Accounts → Platform

Platform provides tenant/account context propagation mechanisms.

Accounts owns the meaning of the account context.

## 8. Identity dependencies

### Identity → Accounts

After authentication, user flows may require an account context.

The contract must distinguish:

- identity established;
- account selected/resolved;
- workspace selected/resolved.

Do not collapse all three into a single opaque "current user context" model if doing so hides ownership.

### Identity → Governance

Identity administration operations may require Governance policy.

Governance policy must be enforced through the approved authorization pipeline.

### Identity → Platform/API security

Browser session transport, CSRF, authentication middleware and security headers are shared technical concerns.

Identity owns the authentication/session semantics.

Platform/API owns the mechanism.

Any mismatch between frontend and backend session/CSRF contract blocks affected auth slices until reconciled.

## 9. Workspaces dependencies

### Workspaces → Accounts

Every workspace must be scoped according to canonical tenancy/account semantics.

The Workspace team may not create an alternative tenant root.

### Workspaces → Identity

Membership and invitations reference identities/subjects through explicit contracts/identifiers.

Workspace state must not embed private Identity persistence models.

### Workspaces → Governance

Governance evaluates administrative/resource permissions.

Workspaces owns:

- workspace existence;
- membership/invitation business state.

Governance owns:

- role/policy/permission meaning.

## 10. Governance dependencies

### Governance → resource-owning contexts

Governance requires stable resource identity/kind semantics.

A resource context owns:

- resource ID;
- resource lifecycle;
- resource containment.

Governance owns:

- permission semantics;
- role semantics;
- policy evaluation.

If a new resource kind needs authorization:

1. resource team defines resource identity and required actions;
2. Governance team defines/extends policy support;
3. Application/API enforcement uses the central pipeline;
4. integration tests prove allowed and denied behavior.

### Governance → Identity

Governance references subjects/actors.

It must not become a second identity store.

## 11. Work Management dependencies

### WorkManagement → Workspaces

Boards/work resources are workspace-contained according to product semantics.

Workspace deletion/archive behavior affecting work resources requires an explicit lifecycle contract; do not implement cascade behavior by database convenience alone.

### WorkManagement → Governance

All protected operations require consistent resource/action representation.

The Work Management team owns business action names/meaning for its resources.

Governance owns how policy grants/denies them.

### WorkManagement ↔ Collaboration

WorkManagement produces resource identity and lifecycle facts.

Collaboration owns comment/collaboration state.

When a BoardItem is removed, comment cleanup/retention behavior must be an explicit contract, not a hidden foreign-key cascade across context ownership.

### WorkManagement → Automation

WorkManagement is an event producer for relevant automation triggers.

The producer event should describe a WorkManagement fact.

It should not contain Automation-specific orchestration logic.

### WorkManagement → Billing

Entitlement checks may gate product capabilities.

WorkManagement consumes entitlement outcomes.

It does not implement plan-tier logic internally.

## 12. Documents dependencies

### Documents → Workspaces

Documents/pages are scoped according to workspace ownership.

### Documents → Governance

Document/page/block operations use explicit resource authorization.

### Documents ↔ Collaboration

Documents produces document resource facts.

Collaboration owns comments/collaborative interaction.

Do not allow comment persistence requirements to dictate the internal Document aggregate model.

### Documents → Platform realtime

Realtime transport is Platform-owned.

Document version/order/conflict semantics remain Documents-owned.

## 13. Collaboration dependencies

### Collaboration → Identity

Comments and collaborative events reference actors.

### Collaboration → resource contexts

A collaboration target may be a resource owned by WorkManagement or Documents.

Collaboration must use a supported target contract.

It may not directly update the target resource's private persistence state.

### Collaboration → Platform realtime

Platform owns connection/reconnect/transport mechanisms.

Collaboration owns user-visible collaboration semantics.

If gap recovery is unresolved, realtime-critical delivery remains blocked even if simple query invalidation appears to refresh the screen.

## 14. Automation dependencies

### Automation → source contexts

Automation consumes events/facts from WorkManagement, Documents, Collaboration or other approved producers.

Producer teams own event meaning and compatibility.

Automation owns trigger interpretation.

### Automation → Governance

Automation actions execute under an explicit actor/system authorization model.

"Background execution" is not permission to bypass authorization.

### Automation ↔ Integrations

Automation owns orchestration.

Integrations owns provider-specific operation.

A connector action contract may be consumed by Automation, but Automation must not embed provider SDK/client behavior.

### Automation → Platform messaging

Reliable delivery/retry mechanism is Platform-owned.

Automation owns business retry/terminal-state semantics where those semantics differ from transport retry.

## 15. Integrations dependencies

### Integrations → Workspaces

Connection installation/configuration is scoped to the correct tenant/workspace boundary.

### Integrations → Identity

User-authorized OAuth/provider flows depend on authenticated actor identity.

### Integrations → Governance

Installing/removing/changing an integration requires explicit administrative permission.

### Integrations → Platform

Platform may own:

- secret/credential mechanism;
- HTTP client/runtime mechanisms;
- retry transport;
- observability.

Integrations owns provider contract and connection state.

## 16. Billing dependencies

### Billing → Accounts

The billable subject is represented through the Accounts contract.

Billing must not create a parallel tenant/account identity.

### Product contexts → Billing

WorkManagement, Documents, Automation, Integrations and other product capabilities may consume entitlement decisions.

Preferred flow:

```text
Billing entitlement fact/service
        ↓
Application/policy decision
        ↓
feature behavior
```

Avoid:

```text
feature package/domain
        ↓
hard-coded plan names
```

### Billing → Governance

Billing administration operations are protected by Governance.

## 17. Analytics dependencies

Analytics is intentionally downstream from transactional bounded contexts.

Preferred input forms:

- durable integration events;
- approved read-model feeds;
- explicit reporting contracts.

Analytics must not force transactional contexts to expose private persistence models.

Analytics derived state is rebuildable or independently owned according to the approved read-model strategy.

### Analytics → Governance

Reports must honor visibility and tenant/resource boundaries.

Analytical convenience does not justify cross-tenant reads.

## 18. Platform dependency contract

Every business team depends on Platform/Foundation, but dependency direction must remain disciplined.

### Platform may provide

Backend:

- pipeline behavior;
- tenancy context mechanism;
- authorization enforcement infrastructure;
- persistence infrastructure;
- messaging/delivery;
- idempotency;
- realtime transport;
- observability;
- test/architecture infrastructure.

Frontend:

- API client foundation;
- contracts/codegen;
- query foundation;
- realtime foundation;
- runtime construction;
- observability;
- UI primitives/tokens;
- architecture tooling.

### Platform may not provide

Platform must not become the owner of:

- workspace lifecycle;
- board rules;
- document rules;
- billing entitlements;
- automation trigger semantics;
- integration provider meaning.

If a Platform abstraction needs business-specific branching, ownership should be re-evaluated.

## 19. Cross-team contract types

Each dependency should be classified before implementation.

### API contract

Use when a caller requires synchronous request/response behavior.

Producer owns the endpoint/contract semantics.

### Integration event

Use when the producer announces a completed business fact and consumers react independently.

The producer must not know all consumers.

### Command/request to another capability

Use only when the target capability explicitly owns the requested behavior.

Do not disguise shared-domain orchestration as an event if immediate success/failure is required.

### Read model

Use for query/reporting needs where direct source persistence access would violate ownership.

### Frontend package contract

Use public package exports and architecture-manifest-approved imports.

Do not deep import package source.

## 20. Contract-change handshake

A producer contract change follows this sequence:

1. producer team identifies semantic change;
2. consumers are enumerated;
3. compatibility strategy is selected;
4. contract/ADR requirement is evaluated;
5. producer tests are updated;
6. consumer updates are prepared;
7. generated API/package evidence is regenerated;
8. integration/E2E evidence is run;
9. rollout/migration order is documented when versions cannot change atomically.

A consumer MUST NOT merge an assumption about a producer's future contract before the producer change is accepted unless an explicit transitional compatibility mechanism exists.

## 21. Event-change handshake

For event changes:

1. event owner remains the source bounded context;
2. event represents a completed business fact;
3. additive vs breaking change is classified;
4. consumer inventory is checked;
5. replay/idempotency implications are checked;
6. delivery ordering and poison-message behavior are checked where relevant;
7. analytics/automation/integration consumers are explicitly considered;
8. integration tests prove producer and critical consumers.

Do not create an event whose only purpose is to expose an internal implementation detail to one consumer.

## 22. Authorization handoff

When a feature adds a protected operation, at least three ownership roles may be involved:

```text
Resource context
  owns resource/action meaning
        ↓
Governance
  owns policy/permission semantics
        ↓
Application/API foundation
  owns enforcement mechanism
```

A team must not bypass the central mechanism by adding handler-local permission logic unless the architecture explicitly authorizes that pattern.

## 23. Frontend/backend handoff

Backend and frontend teams should not treat OpenAPI/contract types as prose agreements.

For a contract-affecting capability:

1. backend owns contract definition;
2. OpenAPI/generated contract evidence is updated;
3. frontend consumes generated contract where supported;
4. frontend query/mutation semantics are implemented;
5. error/permission/session states are tested;
6. critical flow E2E is added when appropriate.

Handwritten duplicate DTOs should not become a second contract authority.

## 24. Realtime handoff

Realtime-critical capabilities involve two owners:

- business context owns the event/state semantics;
- Platform owns transport/reconnect/gap mechanism.

The delivery checklist must specify:

- event identity;
- resource/account/workspace scope;
- duplicate handling;
- out-of-order handling;
- reconnect behavior;
- gap detection;
- recovery/rebase/invalidation strategy;
- stale-state behavior.

"Invalidate the query" is not automatically sufficient proof of ordered stream recovery.

## 25. Data migration handoff

A cross-context data migration must identify:

- source data owner;
- target data owner;
- migration authority;
- compatibility window;
- rollback or forward-fix behavior;
- event/outbox implications;
- tenant isolation;
- verification query/test.

No team should migrate another context's state without the owning team's explicit participation.

## 26. Shared UI handoff

Product teams may request new reusable primitives.

The UI/Foundation owner determines whether the requirement is:

- generic token;
- generic primitive;
- product-specific component;
- host/runtime-specific component.

A product-specific interaction should not be generalized merely because two screens currently look similar.

Conversely, a foundational primitive should not be duplicated across features to avoid coordinating with the UI owner.

## 27. Dependency readiness levels

Each cross-team dependency should be tagged with one of these levels in execution plans/issues:

| Level | Meaning |
|---|---|
| `D0 UNKNOWN` | dependency is suspected but not defined |
| `D1 IDENTIFIED` | producer and consumer are known |
| `D2 CONTRACTED` | contract/event/resource semantics are defined |
| `D3 IMPLEMENTED` | producer capability exists |
| `D4 VERIFIED` | producer/consumer integration evidence passes |
| `D5 STABLE` | contract can support parallel downstream delivery without active redesign |

A consumer should not begin irreversible implementation against a `D0` or `D1` dependency.

## 28. Known dependency blockers/debts

### CSRF/session contract

Owners:

- IA for authentication/session semantics;
- PF/API/frontend foundation for transport mechanism.

Affected:

- protected browser mutations;
- login/session refresh/security flows.

### Account-scoped frontend cache isolation

Owners:

- IA for account-transition semantics;
- PF/frontend query foundation for cache mechanism.

Affected:

- account switch;
- account-scoped queries;
- cross-account state safety.

### Realtime gap recovery

Owners:

- PF for recovery mechanism;
- WM/DC/AI for business-specific state continuation expectations.

Affected:

- realtime-heavy work management;
- documents;
- collaboration;
- automation status.

### UI token export evidence

Owner:

- PF/frontend UI foundation.

Affected:

- teams consuming the export.

These items must be tracked as dependencies, not rediscovered independently by each feature team.

## 29. Parallelization map

### Safe to parallelize after common tenancy/authz contracts are stable

- WorkManagement domain slices;
- Documents domain slices;
- Billing domain slices;
- Automation rule authoring;
- Integration connector modeling;
- Analytics read-model foundation.

### Requires explicit coordination

- WorkManagement + Collaboration target behavior;
- Documents + Collaboration target behavior;
- WorkManagement/Documents + Automation event contracts;
- Automation + Integrations external actions;
- Billing + feature entitlement gating;
- Identity + API/frontend session/CSRF;
- all realtime business teams + Platform recovery behavior.

## 30. Cross-team issue template

Every cross-team dependency issue should state:

```text
Producer team:
Consumer team:
Owning bounded context:
Dependency type:
Canonical semantic owner:
Contract/event/resource:
Current readiness level:
Required readiness level:
Breaking or additive:
Migration/compatibility:
Tests/evidence:
Blocking capability:
Decision/ADR required:
Owner for next action:
```

Do not create a generic "blocked by backend/frontend" issue without naming the actual producer and required contract.

## 31. Escalation conditions

Escalate before implementation when:

- two bounded contexts both appear to own the same business fact;
- a team proposes direct access to another context's private persistence;
- a shared abstraction requires product-specific branching;
- a contract cannot be changed atomically and no compatibility strategy exists;
- an event requires synchronous acknowledgement to preserve a core invariant;
- authorization ownership is unclear;
- tenancy boundary is unclear;
- a coding agent would need to choose a new service/project/package boundary;
- a proposed implementation contradicts an accepted ADR;
- service extraction is proposed.

## 32. Service-extraction dependency test

Before extracting a bounded context into a deployable service, prove:

- owned data can be isolated;
- inbound contracts are explicit;
- outbound events/contracts are explicit;
- consumers do not depend on private persistence;
- synchronous dependencies are understood;
- distributed failure modes are acceptable;
- observability and operations ownership exist;
- migration order is feasible;
- authorization/tenancy context crosses the boundary safely.

If these are not true, extraction should not be used as a way to discover the boundary.

The modular monolith should expose the boundary first.

## 33. Phase 2 exit criteria

Cross-team dependency decomposition is complete when:

- each dependency has a producer;
- each dependency has a consumer;
- dependency type is explicit;
- data ownership is explicit;
- authorization ownership is explicit;
- event ownership is explicit;
- Platform mechanism ownership is separate from business semantics;
- known foundation debts are assigned rather than hidden;
- teams can sequence work without inventing cross-context access patterns;
- service extraction remains a deliberate later decision rather than an organizational shortcut.
