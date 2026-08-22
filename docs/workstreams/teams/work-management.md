---
document_id: WRK-TEAM-WORK-MANAGEMENT
document_type: workstream-team-spec
status: active
owner: work-management-team
applies_to:
  - work-management
  - boards
  - board-items
  - board-fields
  - field-values
  - ordering
  - checklists
  - table-view
  - kanban-view
  - calendar-view
  - timeline-view
  - dashboard-view
  - form-view
evidence:
  - docs/product/work-management.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - frontend/docs/architecture/dependency-boundaries.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/docs/generated/package-boundaries.md
review_on:
  - work-management-domain-change
  - work-management-contract-change
  - work-management-event-change
  - work-view-model-change
  - ordering-contract-change
  - resource-authorization-change
---

# Work Management Workstream

## 1. Purpose

This workstream defines execution for the WorkManagement bounded context.

WorkManagement is the central transactional product context for Boards, BoardItems, BoardFields, values, ordering, checklists and the user-facing views over shared work state.

Its purpose is to let teams and coding agents implement WorkManagement slices without inventing:

- a second work-item model;
- a separate domain per view;
- view-specific authorization;
- direct cross-context persistence;
- a WorkManagement-specific platform stack;
- Automation-specific source events;
- frontend state outside the approved package graph.

Canonical product semantics remain in:

```text
docs/product/work-management.md
```

This file owns current execution decomposition, not product meaning.

## 2. Core ownership

WorkManagement owns semantics for:

```text
Board
BoardItem
BoardField
field/value behavior
grouping and ordering behavior
checklist behavior
WorkManagement-owned view configuration
WorkManagement domain/integration facts
```

Exact aggregate/entity/value-object boundaries follow canonical Domain source and backend domain-modeling authority.

Do not infer aggregate-root status from table names.

## 3. Explicit non-ownership

WorkManagement does NOT own:

- Account lifecycle;
- Identity;
- Workspace lifecycle;
- membership/invitation;
- Governance policy;
- Comment persistence;
- Page/Block semantics;
- Automation rule/execution;
- provider integrations;
- Billing entitlement calculation;
- cross-context Analytics semantics;
- generic messaging;
- generic realtime connection/recovery;
- generic frontend query/runtime architecture.

## 4. Foundational product invariant: one work model, many views

The following surfaces are views/interactions over shared WorkManagement state:

```text
Table
Kanban
Calendar
Timeline
Dashboard
Form
```

They MUST NOT become independent transactional models.

A view may own:

- projection;
- layout;
- persisted view configuration when product-defined;
- filters/sort/grouping presentation;
- view-specific interaction;
- local transient UI state.

A view MUST NOT own a duplicate of:

- BoardItem identity;
- field values;
- item lifecycle;
- authorization semantics;
- Workspace containment.

## 5. Capability decomposition

The context is decomposed into:

```text
WM-001 Board lifecycle
WM-002 BoardItem lifecycle
WM-003 BoardField definitions
WM-004 Field values
WM-005 Groups/grouping
WM-006 Ordering
WM-007 Checklists
WM-008 Query/filter/sort foundation
WM-009 Table view
WM-010 Kanban view
WM-011 Calendar view
WM-012 Timeline view
WM-013 Dashboard projection
WM-014 Form input
WM-015 Activity/event production
WM-016 Realtime synchronization
WM-017 Automation handoff
WM-018 Collaboration handoff
WM-019 Billing/entitlement integration
WM-020 Performance and hardening
```

These are delivery capabilities, not new bounded contexts or backend projects.

## 6. Delivery waves

### WM Wave A — canonical transactional core

```text
WM-001 Board
WM-002 BoardItem
WM-003 BoardField
WM-004 Field values
WM-005 Grouping
WM-006 Ordering
WM-007 Checklists
```

Complex views SHOULD NOT define behavior before this core is stable.

### WM Wave B — shared query and primary interaction views

```text
WM-008 Query/filter/sort
WM-009 Table
WM-010 Kanban
```

These should establish reusable query/mutation contracts.

### WM Wave C — temporal/derived/input views

```text
WM-011 Calendar
WM-012 Timeline
WM-013 Dashboard
WM-014 Form
```

### WM Wave D — cross-context integration and hardening

```text
WM-015 Events/activity
WM-016 Realtime
WM-017 Automation
WM-018 Collaboration
WM-019 Entitlements
WM-020 Performance/hardening
```

Integration mechanisms can be prepared earlier, but unfinished downstream consumers MUST NOT dictate unfinished core-domain structure.

# Board lifecycle (WM-001)

## 7. Responsibilities

Board delivery includes:

- create;
- read;
- update;
- archive/delete where product semantics permit;
- workspace containment;
- account/workspace scoping;
- stable identity;
- Board-level configuration owned by WorkManagement;
- resource/action mapping.

## 8. Board invariants

A Board must:

- belong to exactly the allowed Workspace/account scope;
- preserve stable identity;
- reject invalid lifecycle transitions;
- never silently move across tenant boundaries;
- not embed Governance role/permission semantics;
- remain internally valid if optional views/configurations do not exist.

## 9. Board dependencies

Required upstream readiness:

```text
Account/actor identity → D5
Workspace containment → D5
Governance resource/action contract → D5
Platform authorization enforcement → D5
```

## 10. Board tests

Required cases:

- valid create;
- invalid/missing workspace;
- wrong account/workspace;
- unauthorized create;
- authorized update;
- forbidden update;
- lifecycle transition;
- cross-tenant isolation;
- persistence roundtrip;
- API contract.

# BoardItem lifecycle (WM-002)

## 11. Responsibilities

Implement:

- create;
- read;
- update;
- move;
- archive/delete where product semantics permit;
- Board/group containment;
- stable identity;
- actor/activity behavior;
- field/value association.

## 12. Mutation semantics

For every mutation define:

```text
target resource
authorization action
validation
idempotency requirement
concurrency behavior
persistence transaction
event production
frontend update/invalidation
realtime interaction
error contract
```

A successful HTTP response alone does not define correct mutation semantics.

## 13. Concurrency

Concurrency-sensitive operations include:

- item edits;
- item movement;
- field edits;
- ordering;
- bulk actions.

The team must follow existing canonical concurrency semantics.

If current authority does not define a required conflict policy:

```text
UNRESOLVED
→ stop the affected slice
→ do not invent last-write-wins inside frontend
```

## 14. Idempotency

Retry-prone create/move/bulk operations may require idempotency according to backend policy.

WorkManagement owns business operation meaning.

Platform owns the technical idempotency mechanism.

Tests should distinguish:

- same key/same operation;
- same key/different operation;
- duplicate after success;
- duplicate while in progress.

# BoardField definitions (WM-003)

## 15. Separation of concerns

Field implementation MUST distinguish:

```text
definition
value
validation
serialization
query semantics
presentation/editor
```

Frontend renderer names are not canonical field-type identifiers.

## 16. New field-type checklist

Before adding a field type define:

- canonical type ID;
- Domain value constraints;
- API serialization;
- null/empty semantics;
- equality/comparison;
- sort/filter semantics;
- frontend renderer;
- frontend editor;
- migration behavior;
- generated contract impact;
- compatibility with existing saved data.

## 17. Field deletion/change

Changing/deleting a field may affect stored values and views.

Define:

- retained value behavior;
- migration/backfill;
- view/filter references;
- form references;
- event impact;
- API compatibility.

Do not let DB cascade choose product semantics.

# Field values (WM-004)

## 18. Value contract

For every type define:

- valid;
- invalid;
- null;
- empty;
- serialization;
- normalization;
- comparison;
- update semantics.

Validation authority is backend/domain/application according to architecture.

Frontend validation improves UX but is not canonical enforcement.

## 19. Bulk field updates

Bulk operations must define:

- all-or-nothing vs partial success;
- authorization per target;
- validation;
- idempotency;
- concurrency;
- event/activity behavior;
- frontend error mapping.

Do not implement "bulk" merely by firing uncoordinated single-item requests unless that is the explicit product contract.

# Groups and grouping (WM-005)

## 20. Ownership question

For each grouping concept determine whether it is:

- persistent WorkManagement domain state;
- saved view configuration;
- derived query projection.

Do not create three independent group identities for Table/Kanban/query if product semantics define one shared concept.

## 21. Group lifecycle

If groups are persistent, define:

- create;
- rename;
- reorder;
- delete;
- item movement into/out of group;
- default/unassigned group semantics;
- lifecycle interaction with items.

# Ordering (WM-006)

## 22. Criticality

Ordering is a shared invariant used by multiple views.

Weak ordering causes:

- duplicates;
- unstable rendering;
- incorrect drag/drop;
- divergent clients;
- dense-key exhaustion;
- hard-to-repair persistence.

## 23. Canonical ordering mechanism

Previous hardening established that naive midpoint algorithms are unsafe for cases such as:

- prefix relationships;
- invalid alphabets;
- duplicate key generation.

Use the canonical hardened fractional-indexing mechanism.

Do not reintroduce retired midpoint logic merely because it is simpler.

## 24. Required operations

Ordering must support:

- insert before first;
- append after last;
- insert between neighbors;
- move within group;
- move between groups;
- repeated insertion between same neighbors;
- deterministic comparison;
- batch key generation where current implementation supports it.

## 25. Ordering tests

Required:

- boundary positions;
- adjacent positions;
- dense repeated insertion;
- invalid key;
- duplicate/concurrent request;
- persistence roundtrip;
- cross-group movement;
- sorting consistency backend/frontend.

# Checklists (WM-007)

## 26. Checklist boundary

Define:

- checklist ownership;
- checklist-item identity;
- lifecycle;
- ordering;
- completion;
- parent BoardItem relationship;
- actor/activity behavior.

Checklist items are not automatically Collaboration comments or independent WorkManagement items.

## 27. Parent lifecycle

When parent BoardItem is archived/deleted, checklist behavior must follow product semantics.

Do not infer destructive behavior from FK configuration.

# Query/filter/sort foundation (WM-008)

## 28. Purpose

All major views consume shared WorkManagement query semantics.

Before implementing independent view parsers, define common semantics for:

- filter;
- sort;
- grouping;
- search;
- pagination/windowing;
- selected fields;
- board/workspace scope.

## 29. Server/client boundary

Current architecture should decide where canonical filtering/sorting occurs.

Table and Kanban MUST NOT interpret the same persisted filter differently because each has a private parser.

## 30. Persisted query/view configuration

If filters/sorts/groupings are saved or shared, they become a versioned contract.

Define:

- serialized schema;
- field references;
- migration;
- invalid field behavior;
- compatibility.

# Table view (WM-009)

## 31. Role

Table is a dense editor/projection over canonical WorkManagement data.

It may own:

- column display;
- local selection;
- virtualization;
- editing interaction;
- local layout state.

It MUST NOT own a second BoardItem/value store.

## 32. Table delivery

Define:

- visible columns;
- resize/reorder if product-defined;
- inline edit;
- sorting/filtering;
- row selection;
- bulk actions;
- pagination/virtualization;
- loading/empty/error/permission states.

## 33. Performance

Large-board Table delivery should measure:

- initial payload;
- render time;
- query count;
- edit latency;
- virtualization behavior;
- memory usage.

Optimize from evidence rather than introducing denormalized duplicate stores prematurely.

# Kanban view (WM-010)

## 34. Projection

Kanban projects canonical state using a grouping dimension.

Drag/drop must resolve to canonical:

- group/field mutation;
- order mutation;
- authorization;
- event production;
- realtime update.

## 35. Forbidden Kanban fork

Do not maintain a Kanban-only copy of item group/order if product semantics define shared state.

If a view-specific order is desired, that is a product/contract decision and must be modeled explicitly rather than hidden in frontend local storage.

# Calendar view (WM-011)

## 36. Temporal mapping

Calendar must explicitly define:

- which field(s) drive date;
- date vs datetime;
- timezone;
- all-day behavior;
- missing date;
- invalid value;
- drag/reschedule semantics.

The Calendar component must not become the timezone authority.

## 37. Mutation

Dragging an item in Calendar must map to the same canonical field mutation API used elsewhere.

No Calendar-only date store.

# Timeline view (WM-012)

## 38. Temporal range

Define:

- start;
- end/duration;
- timezone;
- invalid range;
- resize;
- drag;
- missing endpoints;
- display overlap.

Task dependency semantics are added only if product authority defines dependencies.

A timeline library supporting dependency arrows is not sufficient reason to add a dependency domain.

# Dashboard (WM-013)

## 39. Ownership boundary

Distinguish:

```text
WorkManagement Dashboard
```

from:

```text
Analytics / Reporting
```

A board dashboard showing WorkManagement projections can remain WorkManagement-owned.

Cross-context historical/analytical metrics belong to Analytics.

## 40. Dashboard data

Do not create ad hoc private cross-context queries from WorkManagement to populate dashboard widgets.

If the widget is analytical, consume Analytics.

# Form (WM-014)

## 41. Form purpose

Form is an input surface into canonical WorkManagement mutations.

Define:

- target Board;
- exposed fields;
- required fields;
- validation;
- authenticated vs anonymous;
- authorization;
- public abuse/rate controls where relevant;
- idempotent submission;
- post-submit behavior.

## 42. Submission model

Form submission should create/update canonical BoardItem/field state.

Do not create a separate transactional `FormSubmission` model unless product semantics explicitly require it.

# Activity and event production (WM-015)

## 43. Producer ownership

WorkManagement owns business facts such as:

- Board created/changed;
- BoardItem created/changed/moved/deleted;
- BoardField changed;
- checklist changed.

Exact event names/payloads follow canonical source and ADRs.

## 44. Event design

Events should expose stable:

- resource identity;
- account/workspace scope;
- actor where appropriate;
- business fact.

Do not expose private aggregate internals solely for consumers.

Do not add Automation-specific orchestration fields to source events.

## 45. Activity vs audit

Do not conflate:

```text
user-visible activity feed
```

with:

```text
security/compliance audit
```

They may consume similar facts but have different retention/security semantics.

# Realtime synchronization (WM-016)

## 46. Required user outcome

Define expected convergence when:

- another user edits an item;
- another user moves an item;
- field definition changes;
- Board changes;
- network disconnects;
- client reconnects after missed events.

## 47. Ownership split

Platform owns connection/recovery mechanism.

WorkManagement owns correct post-recovery WorkManagement state.

## 48. Required scenarios

```text
client A and B on same board
A updates item
B converges

duplicate event
→ no duplicate state

out-of-order event
→ defined behavior

disconnect
→ several changes happen
→ reconnect
→ gap recovery
→ final canonical state correct
```

## 49. Optimistic updates

If used, define:

- temporary optimistic state;
- rollback;
- server rejection;
- competing event;
- query reconciliation;
- ordering rollback.

Optimistic client state is not a second source of truth.

## 50. Current dependency blocker

Realtime-heavy WorkManagement hardening depends on Platform gap-recovery semantics reaching at least:

```text
D4 VERIFIED
```

Query invalidation alone is not accepted as proof unless the business state can be proven correct after every gap scenario.

# Automation handoff (WM-017)

## 51. Contract direction

```text
WorkManagement
→ publishes business fact

Automation
→ interprets trigger
```

WorkManagement MUST NOT execute automation orchestration inside aggregates/handlers as a shortcut.

## 52. Event consumer compatibility

Before changing a WorkManagement event used by Automation:

- identify consumers;
- classify additive/breaking;
- verify replay/idempotency impact;
- update integration tests;
- define rollout if non-atomic.

# Collaboration handoff (WM-018)

## 53. Ownership split

WorkManagement owns target resource identity/lifecycle.

Collaboration owns comments.

## 54. Deletion/retention

Board/BoardItem deletion must explicitly define comment behavior:

```text
retain/tombstone
delete
detach
```

The DB relationship MUST NOT silently choose product behavior.

## 55. Access

Comment access may depend on the target's WorkManagement authorization.

Keep policy composition explicit and server-enforced.

# Billing and entitlement integration (WM-019)

## 56. Contract

WorkManagement consumes entitlement outcomes.

It does not calculate plan-tier rules.

Correct:

```text
Billing
→ entitlement decision
→ WorkManagement application enforcement
```

Avoid scattered:

```text
if plan == "Pro"
```

inside WorkManagement unless canonical Billing/product design explicitly exposes that concept.

## 57. Limits

If Billing provides limits such as:

- item count;
- premium views;
- automation availability;
- advanced field types;

define whether enforcement occurs:

- before command;
- during validation;
- through policy/entitlement behavior.

Frontend gating is UX only.

# Backend execution

## 58. Domain responsibilities

Domain owns:

- invariants;
- aggregate/entity/value behavior;
- domain events.

No Domain → Infrastructure/API dependency.

Do not encode frontend view state in Domain unless it is actual persistent product semantics.

## 59. Application responsibilities

Application owns:

- commands;
- queries;
- validation orchestration;
- authorization declarations;
- use-case transaction flow;
- interfaces;
- entitlement checks where architecture places them.

## 60. Infrastructure responsibilities

Infrastructure owns:

- persistence;
- EF mappings;
- query implementation where architecture permits;
- technical adapters.

It MUST NOT become the owner of WorkManagement business rules.

## 61. API responsibilities

API owns:

- endpoints;
- transport;
- request/response adaptation;
- OpenAPI;
- HTTP status/error mapping.

Do not move item/field/order rules to endpoints.

# Frontend execution

## 62. Package ownership model

Conceptually:

```text
product/work-management
→ reusable WorkManagement product state/model/adapters

features/*
→ user-facing feature workflows

foundation/*
→ generic API/query/realtime

apps/*
→ routes/composition/runtime
```

Exact allowed dependencies remain governed by the architecture manifest.

## 63. Query-key scope

Every WorkManagement query identity must contain sufficient resource/scope identity to prevent collisions across:

- accounts;
- workspaces;
- boards;
- resources.

If a global account/workspace reset is relied on instead, that mechanism must be explicit and tested.

Route identity is not cache identity.

## 64. Mutation integration

Each frontend mutation defines:

- request contract;
- optimistic behavior;
- invalidation/cache update;
- server rejection;
- permission failure;
- realtime interaction;
- stale state handling.

Do not duplicate mutation rules separately for Table, Kanban, Calendar and Timeline.

## 65. Permission UX

Frontend may use capability/permission data to:

- hide;
- disable;
- explain.

Backend always enforces.

A disabled control is not authorization.

# Cross-context and data consistency

## 66. WorkManagement data ownership

WorkManagement owns its transactional persistence.

Other contexts MUST NOT:

- write WorkManagement tables;
- depend on WorkManagement EF entity types;
- use private repositories as integration contracts.

Use explicit API/application contract, event or approved read model.

## 67. Workspace lifecycle

If Workspace archive/delete affects WorkManagement:

- product behavior must be defined;
- WorkManagement participates;
- migration/cascade is explicit;
- background cleanup is observable.

Relational cascade is not an architecture decision.

## 68. Identity attribution

Historical WorkManagement records may reference actors whose membership later changes.

Define whether historical actor attribution is immutable, resolved dynamically or tombstoned according to product/audit semantics.

Do not delete historical business meaning because a member leaves.

# Migration and compatibility

## 69. Schema-change checklist

For each schema-affecting capability:

- source model change;
- migration;
- existing-data compatibility;
- backfill;
- indexes;
- rollback/forward-fix;
- API/event impact;
- frontend saved-state impact;
- tenant isolation.

## 70. Field-type migration

Field-type changes are high risk.

Define:

- old value conversion;
- invalid existing values;
- downgrade/rollback;
- view/filter references;
- event payload compatibility;
- import/export implications if present.

## 71. Persisted view configuration

If view filters/sorts/layout are persisted, any schema change affecting referenced fields requires compatibility handling.

Do not leave stale persisted configs to crash clients.

# Performance and scalability

## 72. Performance evidence

Measure:

- board load latency;
- item-query latency;
- mutation latency;
- payload size;
- large-board rendering;
- DB query count;
- index usage;
- realtime fanout;
- frontend memory;
- virtualization.

## 73. Optimization rule

Do not introduce:

- duplicate stores;
- cross-context cache;
- denormalized read table;
- global event fanout;

without measured need, ownership and recovery strategy.

## 74. Large-board behavior

Define explicit product/technical behavior for large boards:

- pagination/windowing;
- server-side filter/sort;
- virtualization;
- realtime subscription scope;
- bulk mutation limits.

Do not assume the small-board implementation scales unchanged.

# Testing and evidence

## 75. Domain tests

Cover:

- Board invariants;
- BoardItem lifecycle;
- BoardField behavior;
- field values;
- grouping;
- ordering;
- checklists;
- invalid transitions.

## 76. Application tests

Cover:

- command/query orchestration;
- validation;
- authorization declaration;
- missing actor/account/workspace;
- entitlement behavior where applicable;
- idempotency;
- concurrency.

## 77. Infrastructure tests

Cover:

- mappings;
- constraints/indexes;
- persistence roundtrip;
- account/workspace isolation;
- ordering persistence;
- migration compatibility.

## 78. API tests

Cover:

- OpenAPI;
- validation/error mapping;
- unauthorized vs forbidden;
- pagination;
- filter/sort contract;
- idempotency requirements where exposed.

## 79. Frontend tests

Cover:

- query/mutation;
- account/workspace transition;
- permission states;
- optimistic rollback;
- view consistency;
- accessibility;
- realtime convergence;
- saved view config compatibility.

## 80. Critical E2E

At minimum a canonical cross-view scenario should prove:

```text
create Board
→ create BoardItem
→ define/edit fields
→ observe in Table
→ move/change grouping in Kanban
→ observe same state in Table
→ change date
→ observe Calendar
→ change range
→ observe Timeline
→ reconnect second client
→ converge to canonical state
```

Additional E2E should cover:

```text
permission denied
account/workspace switch
session expiration
entitlement denied
realtime reconnect
```

# Delivery governance

## 81. Capability readiness matrix

| Capability | Required upstream readiness |
|---|---|
| Board | Workspace D5, authz D5 |
| BoardItem | Board D5 |
| Field definitions | Board D5 |
| Field values | field contract D4+ |
| Grouping | item/field model D4+ |
| Ordering | group/item model D4+ |
| Table | shared query/state D4+ |
| Kanban | grouping/order D4+ |
| Calendar | date semantics D4+ |
| Timeline | temporal semantics D4+ |
| Dashboard | query/projection D4+ |
| Form | mutation/validation/authz D4+ |
| Realtime | Platform recovery D4+ |
| Automation | source events D4+ |
| Collaboration | target contract D4+ |
| Entitlement | Billing contract D4+ |

## 82. Parallelization after core stabilization

Safe to parallelize after WM Wave A and shared query contracts stabilize:

```text
Table
Kanban
Calendar
Timeline
Dashboard
Form
```

provided all consume the same canonical mutation/query semantics.

## 83. Unsafe parallelization patterns

Do not allow teams to independently create:

- item models per view;
- field serialization per view;
- ordering algorithms per view;
- permission logic per view;
- realtime models per view;
- API DTOs competing with generated contracts.

## 84. Cross-team handoff template

For a dependency record:

```text
Capability:
Producer team:
Consumer team:
Business owner:
Contract/event/resource:
Current readiness:
Required readiness:
Breaking/additive:
Consumer action:
Producer action:
Verification:
Blocking:
```

# Decision authority

## 85. Team-local decisions

May decide locally:

- private Domain helper decomposition;
- private handler/query structure;
- UI component composition;
- view-local transient state;
- test fixtures;
- measured performance optimization preserving contracts.

## 86. Decisions requiring escalation

Escalate:

- new bounded context;
- new production project/service;
- separate transactional model per view;
- direct cross-context persistence;
- new authorization architecture;
- new global frontend state architecture;
- breaking API/event contract;
- new field serialization with cross-version impact;
- new incompatible ordering algorithm;
- service extraction.

## 87. Stop conditions

Stop the affected slice when:

- Board/Workspace ownership is ambiguous;
- Governance cannot represent resource/action cleanly;
- field changes require migration but compatibility is undefined;
- ordering needs incompatible semantics;
- a view requires duplicate canonical state;
- realtime correctness depends on unresolved recovery;
- Automation requests orchestration inside WorkManagement;
- Collaboration requires private persistence;
- Billing integration requires plan logic in WorkManagement;
- a service split is proposed to solve team coordination.

# Completion criteria

## 88. Capability Definition of Done

A capability slice is `DONE` only when:

- canonical product authority is identified;
- Domain/Application ownership is correct;
- authorization is centrally enforced;
- persistence ownership is preserved;
- API contract is explicit;
- frontend placement passes architecture rules;
- query/mutation identity is safe;
- realtime behavior is defined where applicable;
- producer event ownership is preserved;
- downstream consumers are compatible;
- migrations are complete;
- relevant tests pass;
- architecture gates remain green;
- no undocumented architecture choice was invented by the coding agent.

## 89. WorkManagement foundation exit criteria

Broad parallel feature delivery is safe when:

- Board/BoardItem/BoardField core is stable;
- field-value contract is stable;
- ordering is hardened;
- shared filter/sort/query semantics exist;
- Table and Kanban prove one canonical state;
- authorization resource/action semantics are D5;
- account/workspace server-state isolation is proven;
- realtime recovery is D4+ for work synchronization;
- Automation and Collaboration handoffs are explicit;
- new views can be built without duplicating business state.

## 90. Service extraction readiness

WorkManagement is a likely future extraction candidate because of product centrality and possible scale.

That alone is not sufficient.

Before extraction prove:

- private WorkManagement data ownership;
- stable Workspace/Governance contracts;
- safe actor/account context crossing;
- stable Automation/Analytics events;
- stable Collaboration target contract;
- no direct cross-context DB dependency;
- known synchronous dependencies;
- observability;
- migration strategy;
- independent deployment/scaling provides measurable value.

Until then, continue inside the existing modular monolith.
