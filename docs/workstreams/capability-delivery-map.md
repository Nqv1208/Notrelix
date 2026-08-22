---
document_id: DEL-CAPABILITY-DELIVERY-MAP
document_type: delivery-map
status: active
owner: delivery-governance
applies_to:
  - bounded-context-delivery
  - backend-feature-delivery
  - frontend-feature-delivery
  - team-parallelization
  - future-service-extraction
evidence:
  - docs/product/README.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/capability-extraction-strategy.md
  - docs/delivery/team-ownership.md
  - backend/docs/architecture/backend-overview.md
  - frontend/docs/architecture/frontend-overview.md
review_on:
  - bounded-context-change
  - team-ownership-change
  - capability-boundary-change
  - service-extraction-decision
  - frontend-package-boundary-change
  - backend-production-project-change
---

# Capability Delivery Map

## 1. Purpose

This document converts the stable Notrelix foundation into an execution-ready delivery map for parallel backend and frontend teams.

It answers:

- which team owns each business capability;
- which bounded context owns the business semantics;
- which backend and frontend surfaces are expected to change;
- which dependencies must be resolved before implementation;
- what a team may decide locally;
- what requires a cross-team or architecture decision;
- what evidence is required before a capability is considered complete;
- which logical service boundary a capability belongs to if the modular monolith is extracted later.

This document is a **delivery authority**, not a replacement for product or architecture authority.

## 2. Authority relationship

Use the following precedence when a team encounters ambiguity:

1. product semantics: `docs/product/*`;
2. system boundaries and context relationships: `docs/architecture/*`;
3. backend implementation boundaries: `backend/docs/architecture/*`;
4. frontend implementation boundaries: `frontend/docs/architecture/*`;
5. durable architecture decisions: `docs/decisions/*`, `backend/docs/decisions/*`, `frontend/docs/decisions/*`;
6. delivery ownership and sequencing: this document plus `docs/delivery/team-ownership.md`;
7. implementation evidence: source, tests, generated inventories, CI.

A team MUST NOT use this delivery map to override a canonical product rule, architecture rule, ADR, contract, or executable dependency gate.

If this map disagrees with a higher authority, classify the mismatch before coding:

- `DOC_STALE`: update this map;
- `SOURCE_DEBT`: source violates the canonical architecture;
- `TRANSITION`: temporary migration state is explicitly approved;
- `CONTRACT_CHANGE`: producer/consumer contract must change;
- `UNRESOLVED`: stop the affected decision and escalate.

## 3. Organizational model

Notrelix should enter parallel delivery with **cross-functional capability teams**, while keeping bounded contexts intact.

A team is not a bounded context.

A bounded context is not automatically a deployable service.

A deployable service is not created merely because a team owns a context.

The current implementation remains a modular monolith and frontend monorepo until an explicit extraction decision is justified.

### 3.1 Recommended team topology

| Team | Primary business ownership | Supporting ownership |
|---|---|---|
| Identity & Accounts | Accounts, Identity | authentication/session contracts |
| Workspace & Governance | Workspaces, Governance | membership, invitations, policy/resource authorization |
| Work Management | WorkManagement | board/item/view behavior |
| Documents & Collaboration | Documents, Collaboration | pages, blocks, comments, collaborative resource behavior |
| Automation & Integrations | Automation, Integrations | rules, triggers, actions, external connectors |
| Billing & Entitlements | Billing | plans, subscription, entitlement and usage semantics |
| Analytics & Reporting | Analytics / Reporting | read models, reporting semantics, cross-context analytical consumption |
| Platform & Foundation | no business bounded context | tenancy infrastructure, messaging, idempotency, realtime transport, observability, frontend foundation/runtime/UI mechanisms |

### 3.2 Why several bounded contexts may share one team

Team grouping exists to reduce organizational overhead during feature delivery.

It MUST NOT collapse separate business models.

For example:

- Accounts and Identity may share a team, but their domain ownership remains separate.
- Workspaces and Governance may share a team, but workspace lifecycle is not the authorization model.
- Documents and Collaboration may share a team, but document state is not collaboration state.
- Automation and Integrations may share a team, but automation orchestration is not connector ownership.

If extraction occurs later, each bounded context is evaluated independently.

## 4. Logical service boundaries

The following names describe **future extraction candidates**, not projects to create now.

| Bounded context | Logical service candidate | Current deployment rule |
|---|---|---|
| Accounts | Accounts capability/service | remain inside current backend projects |
| Identity | Identity capability/service | remain inside current backend projects |
| Workspaces | Workspaces capability/service | remain inside current backend projects |
| Governance | Governance capability/service | remain inside current backend projects |
| WorkManagement | Work Management capability/service | remain inside current backend projects |
| Documents | Documents capability/service | remain inside current backend projects |
| Collaboration | Collaboration capability/service | remain inside current backend projects |
| Automation | Automation capability/service | remain inside current backend projects |
| Integrations | Integrations capability/service | remain inside current backend projects |
| Billing | Billing capability/service | remain inside current backend projects |
| Analytics / Reporting | Analytics capability/service | remain inside current backend projects |

Do not create per-context backend projects by default.

The backend production structure remains:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

A context team changes the relevant modules inside those projects.

## 5. Common backend delivery shape

When a capability requires all backend layers, use this ownership model:

```text
Domain
  business state
  invariants
  aggregate behavior
  domain events
        ↓
Application
  command/query/use-case orchestration
  validation
  authorization declaration
  interfaces/contracts
        ↓
Infrastructure
  persistence/provider implementations
  external technical adapters
        ↓
Platform
  generic delivery/messaging/runtime mechanisms
        ↓
API
  transport
  endpoint mapping
  request/response composition
```

A feature team MUST NOT move business rules into Infrastructure, Platform, or API for convenience.

A feature team MUST NOT bypass the Application pipeline for authorization, validation, idempotency, or other foundation mechanisms already owned by the platform.

## 6. Common frontend delivery shape

Frontend work should follow the existing package architecture rather than growing application-local feature islands.

Expected direction:

```text
apps/*
  composition
  routes
  host initialization
        ↓
features/*
  user-facing feature flows
        ↓
product/*
  product-specific models/state/adapters
        ↓
foundation/*
  product-agnostic contracts/query/platform/realtime/observability
        ↓
runtimes/*
  host-specific runtime construction

ui/*
  tokens/primitives/icons
```

The executable architecture manifest remains the source of truth for exact allowed package imports.

Apps compose. They do not become the default owner of reusable business logic.

## 7. Capability completion contract

A capability is not complete merely because an endpoint or screen works.

For each delivered slice, the owning team must account for the following dimensions when they apply:

### Product

- canonical product semantics identified;
- success and failure behavior defined;
- ownership boundary identified;
- authorization meaning identified;
- destructive/irreversible behavior explicitly handled.

### Backend

- Domain model/invariant;
- Application command/query;
- validation;
- authorization resource/action;
- persistence behavior;
- API contract;
- idempotency where applicable;
- domain/integration events where applicable;
- audit/activity implications where applicable.

### Frontend

- generated contract consumption where available;
- query keys and cache ownership;
- mutations and invalidation;
- realtime handling where applicable;
- loading/empty/error/permission states;
- accessibility;
- responsive/mobile implications;
- analytics/observability hooks where required.

### Verification

- unit/domain tests;
- application tests;
- architecture gates;
- integration tests when persistence or cross-boundary behavior changes;
- API/OpenAPI drift check where contract changes;
- frontend architecture/type/lint/format/tests;
- critical E2E for user-critical flows;
- documentation update when governed behavior changes.

## 8. Delivery states

Every capability slice should move through the following states:

| State | Meaning |
|---|---|
| `DEFINED` | semantic owner and scope are known |
| `CONTRACTED` | API/event/authz/data boundaries are defined |
| `IMPLEMENTING` | owning team is changing source |
| `INTEGRATING` | producer/consumer work is connected |
| `HARDENING` | failure, concurrency, security and recovery behavior are verified |
| `DONE` | all required evidence and CI gates pass |
| `BLOCKED` | external dependency prevents safe continuation |

Do not mark a slice `DONE` while required downstream or upstream contract work remains implicit.

## 9. Team: Identity & Accounts

### 9.1 Accounts bounded context

Primary responsibility:

- account/tenant lifecycle semantics;
- account-scoped identity association where owned by Accounts;
- account-level lifecycle state;
- account-level boundaries consumed by billing, workspaces and governance.

Initial delivery slices:

1. account creation/lifecycle;
2. account read/update;
3. account membership or account-user association only where canonical Accounts semantics own it;
4. account transition behavior;
5. account-scoped authorization resources;
6. account-scoped frontend state isolation.

Backend surfaces:

```text
Notrelix.Domain/Accounts
Notrelix.Application/Features/Accounts
Notrelix.Infrastructure/...Accounts...
Notrelix.API/Endpoints/Accounts
```

Exact folders may follow current source structure; teams MUST preserve layer and bounded-context ownership even when a local folder name differs.

Frontend surfaces should primarily use the existing account feature package and foundation/query mechanisms.

Critical dependencies:

- Identity for authenticated user identity;
- Governance for policy evaluation where account-level operations are protected;
- Billing for subscription/entitlement association;
- Platform for tenant context propagation.

Special readiness requirement:

Account-scoped frontend cache isolation must be proven before expanding flows that can switch between accounts without a hard application reset.

### 9.2 Identity bounded context

Primary responsibility:

- user identity;
- authentication;
- session lifecycle;
- OAuth identity linkage;
- MFA/security settings;
- API-token identity semantics where owned by Identity.

Initial delivery slices:

1. authentication/session bootstrap;
2. login/logout/session expiration;
3. OAuth start/callback/linking;
4. MFA enrollment/challenge/recovery;
5. security settings;
6. API token lifecycle;
7. identity profile read/update where canonical Identity semantics own it.

Critical dependencies:

- Accounts for account context after identity resolution;
- Governance for protected identity administration;
- API/security platform for CSRF/session transport;
- frontend runtime/api client for session-expiration handling.

Special readiness requirement:

Browser session and CSRF naming/transport contracts must be reconciled before auth flows are expanded around a mismatched client/server contract.

## 10. Team: Workspace & Governance

### 10.1 Workspaces bounded context

Primary responsibility:

- workspace lifecycle;
- workspace identity/slug;
- membership;
- invitation;
- workspace-level settings and state.

Initial delivery slices:

1. create workspace;
2. resolve/read workspace;
3. update workspace;
4. membership lifecycle;
5. invitation lifecycle;
6. workspace switch/select;
7. workspace-scoped resource discovery.

Critical dependencies:

- Accounts for tenancy/account ownership;
- Identity for actor/member identity;
- Governance for authorization;
- Platform for tenant/workspace context propagation.

### 10.2 Governance bounded context

Primary responsibility:

- roles;
- custom roles;
- resource permissions;
- share-link governance where owned here;
- policy/resource authorization semantics.

Initial delivery slices:

1. role model;
2. custom-role lifecycle;
3. permission assignment;
4. effective-permission evaluation;
5. resource authorization contract;
6. share-link policy/lifecycle;
7. governance administration UI.

Critical dependencies:

- Accounts/Identity for subject identity;
- Workspaces for workspace scope;
- all resource-owning contexts for resource kind/identity;
- Application authorization pipeline for enforcement.

Governance owns policy meaning.

Resource-owning contexts own resource existence and business state.

Neither side may silently take ownership of the other.

## 11. Team: Work Management

Bounded context: WorkManagement.

Primary responsibility:

- board/work container behavior;
- board items;
- fields and values;
- grouping/order semantics;
- checklists;
- work views over the same underlying work data.

Initial capability sequence:

1. board lifecycle;
2. board membership/access integration;
3. board-item lifecycle;
4. field definition;
5. field values;
6. ordering/grouping;
7. checklist behavior;
8. table view;
9. kanban view;
10. calendar view;
11. timeline view;
12. dashboard projections;
13. form-based creation/input;
14. activity/realtime integration.

View rule:

Table, Kanban, Calendar, Timeline, Dashboard and Form are not independent business models.

They are views/projections/interactions over shared WorkManagement state.

Do not introduce a separate aggregate model per view.

Critical dependencies:

- Workspaces for containment;
- Governance for resource authorization;
- Identity for actor semantics;
- Collaboration for comments/collaborative interactions attached to work resources;
- Automation for triggers/actions against work changes;
- Analytics for read-only reporting;
- Platform realtime and messaging mechanisms.

Frontend primary ownership should remain in the dedicated Work Management product package family plus feature/app composition.

## 12. Team: Documents & Collaboration

### 12.1 Documents bounded context

Primary responsibility:

- page lifecycle;
- page hierarchy;
- block lifecycle;
- block ordering/movement;
- document-specific invariants.

Initial delivery slices:

1. page create/read/update/delete;
2. page hierarchy/move;
3. block create/update/delete;
4. block ordering/move;
5. document loading/state;
6. document editor composition;
7. recovery/conflict behavior where supported;
8. document realtime integration.

Critical dependencies:

- Workspaces for containment;
- Governance for access;
- Identity for actor semantics;
- Collaboration for comments and collaborative interaction;
- Platform realtime mechanisms.

### 12.2 Collaboration bounded context

Primary responsibility:

- comment lifecycle;
- collaborative interactions that are not owned by the underlying resource context;
- collaboration-specific events/state.

Initial delivery slices:

1. comments on supported resources;
2. comment edit/delete;
3. collaboration feed/activity integration where canonical;
4. realtime collaboration delivery;
5. user-facing collaboration states;
6. notification handoff where a notification capability consumes collaboration events.

Critical dependencies:

- Identity;
- Governance;
- WorkManagement/Documents as resource producers;
- Platform realtime/messaging;
- notification/activity supporting capabilities.

Collaboration MUST reference resources through contracts/identifiers. It must not directly own or mutate WorkManagement/Documents persistence.

## 13. Team: Automation & Integrations

### 13.1 Automation bounded context

Primary responsibility:

- automation rule definition;
- triggers;
- conditions where modeled;
- actions;
- enable/disable lifecycle;
- execution orchestration semantics;
- execution history/status where owned by Automation.

Initial delivery slices:

1. rule create/read/update/delete;
2. trigger definition;
3. action definition;
4. rule enable/disable;
5. event-to-trigger matching;
6. execution request;
7. execution result/status;
8. retry/failure semantics;
9. automation authoring UI.

Critical dependencies:

- producer contexts emitting durable events;
- Platform delivery/messaging;
- Integrations for external actions;
- Governance for action permission;
- Workspaces for scope.

### 13.2 Integrations bounded context

Primary responsibility:

- external connector lifecycle;
- connection/configuration semantics;
- credential reference/ownership according to security architecture;
- inbound/outbound integration contract;
- webhook/provider adaptation where owned by Integrations.

Initial delivery slices:

1. connector catalog/availability;
2. connection lifecycle;
3. configuration;
4. authorization/credential flow;
5. outbound operation;
6. inbound webhook/event;
7. connection health/error state;
8. integration management UI.

Critical dependencies:

- Identity for user-authorized flows;
- Workspaces for installation scope;
- Governance for admin permission;
- Automation for connector-backed actions;
- Platform for external delivery/retry/observability.

Provider-specific code must not leak into unrelated bounded contexts.

## 14. Team: Billing & Entitlements

Bounded context: Billing.

Primary responsibility:

- plan;
- subscription;
- entitlement;
- usage/billing semantics;
- payment/invoice methods where canonical Billing owns them.

Initial delivery slices:

1. plan/catalog read;
2. subscription lifecycle;
3. entitlement evaluation;
4. usage recording/aggregation contract;
5. billing administration;
6. payment-method lifecycle where implemented;
7. invoice/payment state where implemented;
8. frontend upgrade/entitlement states.

Critical dependencies:

- Accounts for billable tenant/account;
- Identity for billing administrator actor;
- Governance for billing administration permission;
- feature contexts as entitlement consumers;
- Integrations/external provider adapters where payment provider integration exists.

Feature teams may consume entitlements.

They MUST NOT duplicate billing rules inside their own domain models.

## 15. Team: Analytics & Reporting

Bounded context: Analytics / Reporting.

Primary responsibility:

- analytical read models;
- reporting semantics;
- derived metrics;
- query/report composition;
- dashboard/reporting projections that are analytical rather than transactional WorkManagement views.

Initial delivery slices:

1. event/read-model ingestion;
2. workspace/account metric model;
3. work-management reporting;
4. automation/integration reporting;
5. billing/usage reporting where authorized;
6. report/query API;
7. dashboard/report frontend;
8. export only when explicitly specified.

Critical dependencies:

- source contexts as event/contract producers;
- Platform for delivery/observability;
- Governance for report visibility;
- frontend query/data visualization foundation.

Analytics is downstream by default.

It MUST NOT become a transaction coordinator for source bounded contexts.

It MUST NOT read another context's private tables merely because reporting is easier that way unless an explicit architecture decision authorizes the read model strategy.

## 16. Team: Platform & Foundation

Platform/Foundation has no business bounded context.

Its purpose is to provide stable mechanisms that feature teams depend on.

Backend ownership includes mechanisms such as:

- tenant/runtime context infrastructure;
- Application pipeline mechanisms;
- authorization enforcement mechanism, not business policy;
- idempotency infrastructure;
- outbox/inbox/delivery infrastructure;
- messaging/runtime delivery;
- realtime transport/infrastructure;
- persistence foundations;
- observability;
- API composition foundations;
- test/architecture gates.

Frontend ownership includes:

- contracts/codegen foundation;
- query foundation;
- runtime construction;
- realtime transport foundation;
- observability foundation;
- UI tokens/primitives/icons;
- cross-host infrastructure;
- architecture/dependency tooling.

Platform MUST NOT absorb business semantics merely because multiple contexts use a mechanism.

## 17. Platform debt lanes during parallel delivery

The following previously identified foundation debts should be handled as explicit lanes rather than silently assigned to feature teams.

### Auth/session contract lane

Affected delivery:

- Identity authentication;
- browser session behavior;
- protected mutations.

Required outcome:

- frontend/backend CSRF contract agrees on transport naming and behavior;
- tests prove the contract.

### Account state-isolation lane

Affected delivery:

- account switching;
- account-scoped frontend queries/mutations.

Required outcome:

- prove hard reset on account transition, or;
- change account-scoped query identity to make isolation explicit.

### Realtime recovery lane

Affected delivery:

- WorkManagement realtime;
- Documents realtime;
- Collaboration realtime;
- Automation status updates.

Required outcome:

- define and verify continuation after gap/reconnect;
- duplicate/out-of-order handling remains safe;
- invalidation alone must not be assumed equivalent to sequence recovery without evidence.

### UI token/export lane

Affected delivery:

- shared web/mobile UI work.

Required outcome:

- verify declared UI token exports resolve to real source/build output;
- architecture docs and package exports agree.

These lanes do not block unrelated CRUD or read-only slices.

They block only capability slices that depend on the unresolved behavior.

## 18. Capability start checklist

Before starting a capability, the owning team should be able to answer all of the following without guessing:

- Which bounded context owns this behavior?
- Which canonical product document defines it?
- Which aggregate/entity/value object owns the invariant?
- Which actor/resource/action represents authorization?
- Which API contract is produced or changed?
- Is the operation idempotent?
- Which data store/context owns persistence?
- Are domain/integration events required?
- Is another context a producer or consumer?
- Is realtime required?
- Which frontend package should own the reusable behavior?
- What server-state query identity is required?
- What cache invalidation or event update occurs?
- What tests prove the behavior?
- Is an ADR required?
- Is a migration required?
- Is any foundation debt blocking this slice?

If a material answer is unknown, the slice remains `DEFINED` or `BLOCKED`; the coding agent must not invent the missing architecture.

## 19. Cross-team change protocol

A single feature may require several teams, but ownership must remain explicit.

Example:

```text
WorkManagement event
        ↓
Automation trigger
        ↓
Integration action
```

The Work Management team owns the source event semantics.

The Automation team owns trigger interpretation and execution orchestration.

The Integrations team owns provider-specific external execution.

Platform owns reliable event transport.

Do not create a shared "automation-workmanagement-integration" domain model to avoid coordination.

## 20. Pull-request decomposition

Prefer PRs that preserve one primary owner.

Recommended decomposition:

```text
PR A — producer contract/domain behavior
PR B — consumer support
PR C — frontend integration
PR D — cross-context E2E/integration evidence
```

A single atomic PR is acceptable when splitting it would leave the repository in an invalid state, but each changed authority and owner must still be explicit.

## 21. Future service extraction readiness

A bounded context becomes a serious extraction candidate only when there is evidence such as:

- independent scaling requirement;
- independent deployment cadence;
- materially different availability requirement;
- clear data ownership already enforced;
- stable contracts/events;
- limited synchronous coupling;
- operational ownership exists;
- extraction reduces rather than increases total system complexity.

Team size alone is not sufficient evidence.

Repository size alone is not sufficient evidence.

"Microservices are more scalable" is not sufficient evidence.

Until an extraction ADR is accepted, teams implement within the current modular-monolith production projects.

## 22. Delivery wave recommendation

### Wave A — identity, workspace and platform prerequisites

Prioritize:

- auth/session contract reconciliation;
- account isolation proof;
- core Accounts/Identity flows;
- Workspace lifecycle;
- Governance authorization foundations.

This establishes the user/tenant/workspace/resource security frame used by later teams.

### Wave B — primary collaborative product

Parallelize:

- WorkManagement;
- Documents;
- Collaboration.

Platform concurrently completes realtime recovery semantics required by realtime-heavy slices.

### Wave C — automation, integrations and monetization

Parallelize:

- Automation;
- Integrations;
- Billing/Entitlements.

These consume more mature resource, governance and event contracts.

### Wave D — analytics/reporting hardening

Analytics can begin read-model infrastructure earlier, but production reporting should consume stable source contracts/events rather than forcing upstream models to change for reporting convenience.

## 23. Exit criteria for Phase 2 ownership decomposition

Phase 2 is complete when:

- every bounded context has one accountable team;
- every capability slice has one primary producer owner;
- cross-team dependencies are registered rather than implicit;
- each team knows its backend and frontend ownership surfaces;
- Platform/Foundation responsibilities are separated from business semantics;
- service extraction candidates are named without creating premature projects/services;
- unresolved platform debts have owners and affected slices;
- `docs/delivery/team-ownership.md` is synchronized with this map;
- delivery can proceed without a coding agent inventing context ownership or dependency direction.
