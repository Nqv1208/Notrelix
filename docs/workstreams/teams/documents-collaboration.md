---
document_id: WRK-TEAM-DOCUMENTS-COLLABORATION
document_type: workstream-team-spec
status: active
owner: documents-collaboration-team
applies_to:
  - documents
  - pages
  - page-hierarchy
  - blocks
  - block-ordering
  - comments
  - collaboration
  - document-realtime
  - collaboration-targets
evidence:
  - docs/product/documents.md
  - docs/product/collaboration.md
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
  - document-domain-change
  - collaboration-domain-change
  - page-hierarchy-change
  - block-ordering-change
  - comment-target-change
  - document-realtime-change
  - cross-context-retention-change
---

# Documents & Collaboration Workstream

## 1. Purpose

This workstream defines execution for the Documents and Collaboration bounded contexts.

The two contexts share one delivery team for execution efficiency, but they remain separate business ownership boundaries.

Documents owns:

- Page lifecycle;
- Page hierarchy;
- Block lifecycle;
- Block ordering/movement;
- document state;
- document-specific events.

Collaboration owns:

- Comment lifecycle;
- comment target semantics;
- collaboration-specific interactions;
- collaboration events/state.

This document exists so teams and coding agents can implement document/collaboration slices without inventing:

- a combined Documents+Collaboration domain;
- direct cross-context persistence;
- comments as embedded Document internals;
- document state inside realtime transport code;
- editor-local business invariants;
- target-deletion semantics from foreign keys;
- arbitrary realtime consistency rules.

Canonical product meaning remains in:

```text
docs/product/documents.md
docs/product/collaboration.md
```

## 2. Core context separation

Documents answers:

```text
What is a Page?
What is a Block?
How are Pages organized?
How are Blocks ordered and moved?
What document state exists?
```

Collaboration answers:

```text
What is a Comment?
Who can create/edit/delete it?
What resource does it target?
How does collaboration state behave?
```

Sharing a team MUST NOT lead to:

```text
Page aggregate owns comments
Comment repository owns Pages
Document service mutates comment persistence directly
```

unless an accepted architecture decision explicitly changes context ownership.

## 3. Non-ownership

Documents does NOT own:

- Workspace lifecycle;
- Governance policy;
- WorkManagement Board/BoardItem state;
- Comment persistence;
- generic realtime connection/recovery;
- Automation orchestration;
- Analytics reporting semantics.

Collaboration does NOT own:

- Page/Block lifecycle;
- WorkManagement resource lifecycle;
- target-resource persistence;
- Identity;
- Governance policy;
- generic notification delivery;
- generic realtime transport.

## 4. Capability decomposition

The team workstream is decomposed into:

```text
DCT-001 Page lifecycle
DCT-002 Page hierarchy
DCT-003 Page move/reparent
DCT-004 Block lifecycle
DCT-005 Block types/content contract
DCT-006 Block ordering
DCT-007 Document query/loading
DCT-008 Editor state reconciliation
DCT-009 Document permissions
DCT-010 Document events/activity
DCT-011 Document realtime
DCT-012 Recovery/conflict hardening

COL-001 Comment lifecycle
COL-002 Comment target contract
COL-003 Comment authorization
COL-004 Comment activity/events
COL-005 Collaboration realtime
COL-006 Target deletion/retention
COL-007 Collaboration query/state
COL-008 Collaboration hardening

DCX-01 Documents↔Collaboration handoff
DCX-02 WorkManagement collaboration targets
DCX-03 Automation handoff
DCX-04 Analytics handoff
```

These are delivery capabilities, not new bounded contexts or services.

## 5. Delivery waves

### DC Wave A — Documents core

```text
DCT-001 Page lifecycle
DCT-002 Page hierarchy
DCT-003 Page move/reparent
DCT-004 Block lifecycle
DCT-005 Block types/content
DCT-006 Block ordering
```

### DC Wave B — Documents application/frontend

```text
DCT-007 Query/loading
DCT-008 Editor reconciliation
DCT-009 Permissions
DCT-010 Events/activity
```

### DC Wave C — Collaboration core

```text
COL-001 Comment lifecycle
COL-002 Comment target contract
COL-003 Comment authorization
COL-004 Comment events/activity
```

### DC Wave D — realtime/recovery

```text
DCT-011 Document realtime
DCT-012 Recovery/conflict
COL-005 Collaboration realtime
COL-006 Target deletion/retention
COL-007 Collaboration query/state
COL-008 Collaboration hardening
```

### DC Wave E — cross-context integration

```text
DCX-01 Documents↔Collaboration
DCX-02 WorkManagement target support
DCX-03 Automation
DCX-04 Analytics
```

Cross-context contracts may be designed earlier, but target/context ownership MUST be stable before implementation hardens them.

# Documents core

## 6. Page lifecycle (DCT-001)

### Responsibilities

Define:

- create;
- read;
- update;
- archive/delete according to product semantics;
- stable Page identity;
- Workspace/account scope;
- owner/actor attribution where applicable;
- resource/action mapping;
- events.

### Required invariants

A Page must:

- remain in the correct tenant/workspace scope;
- reject invalid lifecycle transitions;
- not silently become a container for another context's state;
- not acquire comment/business-collaboration semantics merely because comments appear on the page.

### Dependencies

Required:

```text
Account/actor identity → D5
Workspace containment → D5
Governance resource/action → D5
Platform authz enforcement → D5
```

## 7. Page hierarchy (DCT-002)

### Hierarchy semantics

Define:

- root Pages;
- parent Page;
- child Pages;
- maximum/allowed nesting if product-defined;
- move/reparent;
- ordering among siblings;
- cycle prevention;
- visibility/inheritance if any;
- deletion/archive effects.

### Required invariant

A Page hierarchy MUST NOT contain cycles.

### Implementation rule

Do not treat tree traversal convenience or nested-set/path storage choice as the product model.

Storage strategy belongs to Infrastructure.

Hierarchy semantics belong to Documents.

## 8. Page move / reparent (DCT-003)

A move operation must define:

```text
source Page
old parent
new parent
new sibling/order position
authorization on source
authorization on destination
cycle validation
tenant/workspace validation
event production
frontend reconciliation
```

### Cross-scope restriction

A Page MUST NOT move across an account/workspace boundary unless canonical product semantics explicitly support it and all dependent ownership/migration behavior is defined.

### Concurrency

Concurrent reparent/move requires a defined behavior.

If source does not define the concurrency model:

```text
UNRESOLVED
→ do not invent UI-only conflict behavior
```

## 9. Block lifecycle (DCT-004)

Define:

- create;
- update;
- delete;
- move;
- parent Page;
- block identity;
- block type;
- content payload;
- ordering;
- actor/audit behavior.

A Block should not become an arbitrary serialized JSON dumping ground that hides domain invariants.

## 10. Block type/content contract (DCT-005)

### Separation

Block implementation must distinguish:

```text
block type identity
domain-valid content
API serialization
frontend renderer
frontend editor
```

A renderer component name MUST NOT define canonical block type.

### Adding a block type

Before adding a new type define:

- stable type ID;
- content schema;
- validation;
- serialization;
- null/empty semantics;
- migration compatibility;
- renderer;
- editor;
- export/import behavior if relevant;
- event payload impact.

### Compatibility

Changing persisted block content schema requires migration/compatibility handling.

Do not allow old persisted content to become unreadable because a frontend component changed shape.

## 11. Block ordering (DCT-006)

### Criticality

Block ordering is a core document invariant.

It affects:

- editor rendering;
- drag/drop;
- copy/move;
- realtime convergence;
- persistence consistency.

### Canonical ordering

Use the canonical hardened ordering/fractional-indexing mechanism where Documents currently shares that ordering primitive.

Do not reintroduce a naive midpoint algorithm.

### Required operations

- insert first;
- append last;
- insert between;
- move within Page;
- move between Pages if permitted;
- repeated dense insert;
- deterministic ordering;
- batch generation if supported.

### Required tests

- boundary insertion;
- dense repeated insertion;
- malformed keys;
- duplicate/concurrent keys;
- persistence roundtrip;
- move across Page;
- backend/frontend order agreement.

# Documents application/frontend

## 12. Document query/loading (DCT-007)

### Query contract

Define:

- Page fetch;
- hierarchy fetch;
- Block fetch;
- pagination/windowing if large;
- account/workspace scope;
- authorization behavior;
- not-found vs forbidden semantics;
- loading order.

### Avoid N+1 ownership leakage

Performance optimizations must preserve Documents data ownership.

Do not solve document loading by reading Collaboration/WorkManagement private tables.

## 13. Editor state reconciliation (DCT-008)

### Core rule

Frontend editor state is not the canonical Documents domain.

The editor may own:

- selection;
- cursor;
- composition state;
- unsaved local input;
- drag state;
- transient optimistic state.

Server/domain state owns persisted Page/Block truth.

### Mutation reconciliation

For every editor mutation define:

- optimistic behavior;
- server request;
- temporary identity if needed;
- rollback;
- server rejection;
- realtime competing update;
- final query/state reconciliation.

### Stop condition

If editor correctness requires maintaining a second long-lived canonical document store independent from server state:

```text
STOP
→ review frontend state architecture
```

## 14. Document authorization (DCT-009)

Resource examples:

```text
Page
Block where independently addressable
document/share resource where product-defined
```

Documents owns resource/action meaning.

Governance owns policy.

Platform/Application owns enforcement.

Frontend permission state is UX only.

### Required actions

Classify operations such as:

- read;
- create;
- update;
- move;
- delete/archive;
- share;
- comment target access where applicable.

## 15. Document events and activity (DCT-010)

Potential business facts:

- Page created;
- Page moved;
- Page changed;
- Page archived/deleted;
- Block created;
- Block changed;
- Block moved;
- Block deleted.

Exact contracts follow canonical source/ADR authority.

### Event rule

Documents events should not embed:

- Collaboration internal state;
- Automation rule state;
- Analytics projection fields.

Consumers adapt to producer-owned facts.

### Activity vs audit

Keep separate:

```text
user-visible document activity
security/compliance audit
```

They may consume similar facts but have different retention/access requirements.

# Documents realtime/recovery

## 16. Document realtime (DCT-011)

### Required user outcome

Define what should happen when:

- Client A edits Block;
- Client B is viewing same Page;
- a Block moves;
- Page hierarchy changes;
- Client B disconnects;
- several changes occur;
- Client B reconnects.

### Platform dependency

Platform owns:

- transport;
- reconnect;
- gap detection;
- generic recovery mechanism.

Documents owns:

- acceptable final Page/Block state;
- rebase/reload behavior;
- conflict semantics.

### Required scenarios

```text
two clients same Page
A changes Block
B converges

A moves Block
B converges to same order

duplicate event
→ no duplicate Block

out-of-order event
→ defined behavior

disconnect
→ missed changes
→ reconnect
→ gap recovery
→ canonical final document
```

## 17. Recovery and conflict hardening (DCT-012)

### Recovery classes

Different document operations may require different recovery semantics:

```text
simple metadata change
→ query reload may be sufficient

ordered Block move
→ order convergence required

concurrent content edit
→ conflict/rebase semantics required if collaborative editing is supported
```

Do not assume one generic invalidation policy is correct for all.

### Unsupported collaborative editing

If true multi-cursor/co-edit conflict resolution is not part of current product architecture, do not invent CRDT/OT infrastructure merely because Documents may eventually need collaboration.

Introduce such architecture only through explicit design/ADR.

# Collaboration core

## 18. Comment lifecycle (COL-001)

Define:

- create;
- read/list;
- edit;
- delete;
- author/actor;
- created/updated time;
- target resource;
- soft-delete/retention behavior according to canonical domain policy.

### Required invariants

A Comment must:

- target a supported resource;
- preserve tenant/account scope;
- not mutate target resource state;
- enforce author/admin permissions as product semantics require.

## 19. Comment target contract (COL-002)

### Ownership split

Target context owns:

- target resource ID;
- target lifecycle;
- target containment;
- target business authorization meaning.

Collaboration owns:

- Comment;
- target reference contract;
- comment lifecycle.

### Supported targets

Targets may include Documents or WorkManagement resources only where canonical product semantics support them.

Do not add arbitrary generic target strings to avoid defining resource contracts.

### Target identity

A target reference should make explicit:

```text
resource kind
resource ID
account/workspace scope as required
```

rather than serializing a private EF entity or CLR type name.

## 20. Comment authorization (COL-003)

Authorization may require:

```text
can actor access target?
can actor comment?
can actor edit own comment?
can actor delete own comment?
can moderator/admin delete?
```

Backend enforcement is authoritative.

Frontend hidden/disabled controls are UX only.

### Cross-context policy

Collaboration may need target access information.

It SHOULD NOT read target private tables directly.

Use approved resource authorization contracts.

## 21. Comment activity/events (COL-004)

Potential facts:

- Comment created;
- Comment edited;
- Comment deleted.

Consumers may include:

- activity feed;
- notification capability;
- Analytics;
- Automation if product-defined.

Producer event remains Collaboration-owned.

Do not put notification-template or Automation-action fields into Comment events.

# Collaboration realtime and lifecycle

## 22. Collaboration realtime (COL-005)

Realtime comments should define:

- subscription scope;
- target identity;
- duplicate handling;
- ordering;
- reconnect;
- gap recovery;
- pagination/list reconciliation.

Comment streams may tolerate reload recovery differently from ordered Block editing, but that assumption must be tested.

## 23. Target deletion / retention (COL-006)

This is a cross-context lifecycle contract and MUST be explicit.

When a target resource is deleted/archived, possible comment outcomes include:

```text
retain against tombstoned target
delete
detach to historical reference
hide but retain
```

The choice belongs to product/architecture policy.

### Forbidden shortcut

Do not let a foreign-key cascade silently define comment retention.

### Historical attribution

If a user leaves the workspace, historical comments should preserve attribution according to canonical Identity/Collaboration semantics.

Do not erase history merely because membership ended.

## 24. Collaboration query/state (COL-007)

Frontend state should define:

- target query key;
- pagination;
- optimistic comment create;
- edit/delete update;
- realtime update;
- account/workspace transition;
- permission changes.

Target identity must participate sufficiently to avoid cache collision across resources/accounts.

## 25. Collaboration hardening (COL-008)

Verify:

- duplicate comment submit;
- retry/idempotency where required;
- delete/edit races;
- stale target;
- permission revoked;
- target archived;
- cross-tenant target spoofing;
- reconnect;
- large thread pagination.

# Cross-context handoffs

## 26. DCX-01 — Documents ↔ Collaboration

Correct dependency:

```text
Documents
→ exposes Page/Block resource identity and lifecycle

Collaboration
→ owns Comment state
```

Do not merge persistence.

### Integration tests

At minimum:

```text
authorized Page
→ create comment succeeds

forbidden Page
→ create comment denied

Page removed
→ defined comment-retention behavior

workspace/account mismatch
→ target denied
```

## 27. DCX-02 — WorkManagement collaboration targets

For comments on Board/BoardItem:

WorkManagement owns target resource semantics.

Collaboration owns Comment.

The handoff must use stable resource contracts.

No Collaboration dependency on WorkManagement EF entities/repositories.

## 28. DCX-03 — Automation handoff

Documents/Collaboration may emit facts consumed by Automation.

Correct direction:

```text
Documents/Collaboration fact
→ Automation trigger
```

Incorrect:

```text
Document handler
→ directly execute Automation rule internals
```

## 29. DCX-04 — Analytics handoff

Analytics may consume:

- Page/Block lifecycle facts;
- Comment/activity facts;

for derived reporting.

Analytics MUST NOT read private Documents/Collaboration tables without explicit architectural approval.

# Backend layer responsibilities

## 30. Domain

Documents Domain owns Page/Block invariants.

Collaboration Domain owns Comment invariants.

No Domain dependency on Infrastructure/API.

No generic realtime/query concerns in Domain.

## 31. Application

Application owns:

- commands/queries;
- validation orchestration;
- authorization declarations;
- use-case transaction flow;
- cross-context contract interfaces.

## 32. Infrastructure

Infrastructure owns:

- persistence;
- hierarchy/order storage implementation;
- mappings;
- technical adapters.

Infrastructure MUST NOT define product hierarchy/retention semantics.

## 33. API

API owns:

- endpoint transport;
- request/response;
- OpenAPI;
- status/error mapping.

Do not move Page/Block/Comment rules into endpoints.

# Frontend responsibilities

## 34. Product/feature boundary

Conceptually:

```text
product/documents
→ reusable document product logic

product/collaboration
→ reusable collaboration product logic

features/*
→ user-facing workflows

foundation/*
→ generic API/query/realtime

apps/*
→ route/host composition
```

Exact edges remain architecture-manifest-owned.

## 35. Editor local state

Local editor state may contain:

- selection;
- cursor;
- focus;
- drag;
- temporary composition;
- optimistic temporary state.

It SHOULD NOT duplicate durable Page/Block business state indefinitely.

## 36. Query-key scope

Queries must distinguish:

- account;
- workspace;
- Page;
- target resource;
- comment thread/list.

If account transition relies on hard reset rather than explicit account IDs, that dependency must be tested.

# Data ownership and consistency

## 37. Documents persistence

Documents owns Page/Block persistence.

Collaboration cannot mutate it.

WorkManagement cannot mutate it.

Analytics cannot use it as an implicit shared read store.

## 38. Collaboration persistence

Collaboration owns Comment/collaboration persistence.

Documents/WorkManagement cannot directly write comment tables.

## 39. Cross-context consistency

Use explicit:

- contracts;
- events;
- read models;
- approved orchestration.

Do not treat same-process access as permission to bypass ownership.

# Migration and compatibility

## 40. Page hierarchy migration

Hierarchy schema changes must address:

- existing parent relationships;
- cycles/invalid legacy data;
- root representation;
- index/path rebuild if used;
- rollback;
- authorization scope.

## 41. Block content migration

Block type/content schema changes must define:

- old format;
- new format;
- conversion;
- invalid content;
- rollback/forward-fix;
- frontend compatibility.

## 42. Ordering migration

Changing order representation requires:

- deterministic conversion;
- preservation of visible order;
- duplicate detection;
- repair path;
- frontend compatibility.

## 43. Comment-target migration

Changing target representation requires:

- mapping old kind/ID;
- unsupported/removed targets;
- retention semantics;
- authorization compatibility;
- replay/event compatibility.

# Performance and scalability

## 44. Documents performance

Measure:

- Page hierarchy query latency;
- large Page Block load;
- editor mutation latency;
- order persistence;
- payload size;
- frontend render/memory;
- realtime fanout.

## 45. Collaboration performance

Measure:

- comment list latency;
- pagination;
- target authorization cost;
- realtime fanout;
- large threads.

## 46. Optimization rule

Do not introduce:

- duplicate canonical document stores;
- cross-context caches;
- global comment joins;
- denormalized shared tables;

without measured need, ownership and recovery strategy.

# Security

## 47. Tenant isolation

Every Page/Block/Comment path must preserve account/workspace isolation.

Client-supplied resource IDs cannot be trusted without authorization/context validation.

## 48. Content safety

If Documents/Comments support rich content, treat:

- rendering;
- HTML/markdown sanitization;
- links/embeds;
- attachments if present;

as security-sensitive.

Do not assume editor-generated content is inherently safe.

## 49. Share/public access

If share links/public document/comment behavior exists, explicitly define:

- anonymous access;
- permission scope;
- expiration;
- revocation;
- write ability;
- indexing/search implications;
- rate/abuse protection.

Do not let public share bypass tenant/security rules accidentally.

# Testing

## 50. Documents Domain tests

Cover:

- Page lifecycle;
- hierarchy;
- cycle prevention;
- reparent;
- Block lifecycle;
- ordering;
- invalid moves;
- content validation.

## 51. Collaboration Domain tests

Cover:

- Comment lifecycle;
- target contract;
- author/moderator rules;
- invalid target state;
- retention state where modeled.

## 52. Application tests

Cover:

- commands/queries;
- authorization;
- validation;
- missing account/workspace;
- stale target;
- cross-context contract failure.

## 53. Infrastructure tests

Cover:

- Page/Block mappings;
- hierarchy storage;
- ordering persistence;
- Comment persistence;
- target indices;
- migrations;
- tenant/workspace scoping.

## 54. API tests

Cover:

- contracts/OpenAPI;
- not found vs forbidden;
- invalid target;
- hierarchy move;
- Block mutation;
- comment mutation;
- pagination.

## 55. Frontend tests

Documents:

- Page loading;
- hierarchy navigation;
- Block editing;
- move/order;
- optimistic rollback;
- permission state;
- reconnect.

Collaboration:

- comment list;
- optimistic create;
- edit/delete;
- target switching;
- pagination;
- permission state;
- realtime.

## 56. Critical E2E

Documents:

```text
create Page
→ create Blocks
→ reorder Blocks
→ move Page
→ second client converges
→ reconnect
→ canonical state preserved
```

Collaboration:

```text
open authorized resource
→ create Comment
→ second client receives Comment
→ edit/delete
→ permission revoked
→ next mutation denied
```

Cross-context:

```text
BoardItem target
→ comment created through Collaboration
→ WorkManagement remains unchanged except its own state
```

# Delivery governance

## 57. Readiness matrix

| Capability | Upstream readiness |
|---|---|
| Page lifecycle | Workspace D5, authz D5 |
| Page hierarchy | Page model D5 |
| Block lifecycle | Page model D5 |
| Block ordering | Block model D4+ |
| Editor reconciliation | API/query contract D4+ |
| Document realtime | Platform recovery D4+ |
| Comment lifecycle | Identity/authz D5 |
| Comment target | target resource contract D4+ |
| Comment realtime | Platform recovery D4+ |
| WorkManagement target | WM resource contract D4+ |
| Automation handoff | source event D4+ |
| Analytics handoff | source event D4+ |

## 58. Safe parallelization

After Documents core contracts stabilize:

```text
hierarchy UI
Block editor
comment core
comment UI
activity/event projection
```

can proceed in parallel.

Realtime should not harden until recovery semantics exist.

## 59. Unsafe parallelization

Do not allow separate teams to independently invent:

- Block type IDs;
- ordering;
- comment target representation;
- permission logic;
- realtime recovery;
- Page hierarchy semantics.

## 60. Team-local decisions

May decide locally:

- private aggregate helpers;
- editor component decomposition;
- private query helpers;
- local rendering composition;
- test fixtures;
- measured performance improvements.

Must escalate:

- new collaborative editing architecture;
- CRDT/OT introduction;
- new target-resource family;
- cross-context persistence;
- public-share security model change;
- new global frontend editor state architecture;
- breaking Block content schema;
- service extraction.

## 61. Stop conditions

Stop and escalate when:

- Page hierarchy semantics are unclear;
- Block schema change lacks migration;
- ordering needs incompatible semantics;
- comment target cannot be represented cleanly;
- target deletion retention is undefined;
- realtime correctness depends on unresolved recovery;
- Collaboration requires direct target persistence;
- Documents attempts to own Comments;
- public share requires security weakening;
- true concurrent editing requires new architecture.

## 62. Capability Definition of Done

A Documents/Collaboration slice is `DONE` only when:

- canonical product owner is identified;
- context ownership is preserved;
- authorization is server-enforced;
- persistence ownership is private;
- API contract is explicit;
- frontend package placement is valid;
- query/realtime identity is safe;
- hierarchy/order/target invariants are covered;
- migrations are handled;
- cross-context consumers/producers are compatible;
- tests/CI pass;
- no architecture decision is hidden inside implementation.

## 63. Workstream exit criteria

The team can support broad parallel delivery when:

- Page/Block core is stable;
- hierarchy/order are hardened;
- editor state reconciles with server canonical state;
- Comment target contract is stable;
- WorkManagement target integration is explicit;
- authorization is D5;
- realtime recovery is D4+ for document/comment consumers;
- target deletion/retention is defined;
- Comments remain separate from target persistence.

## 64. Service extraction readiness

Documents and Collaboration remain separate future extraction candidates.

Before Documents extraction prove:

- private Page/Block data;
- stable Workspace/Governance contracts;
- stable realtime semantics;
- stable Collaboration target contract;
- no hidden DB coupling.

Before Collaboration extraction prove:

- stable generic target contract;
- target authorization contract;
- independent Comment data;
- realtime/notification integrations;
- retention behavior;
- no direct target DB access.

Team co-location is not a reason to merge the services later.
