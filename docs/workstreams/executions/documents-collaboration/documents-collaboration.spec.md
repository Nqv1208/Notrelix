---
document_id: WRK-SPEC-DOCUMENTS-COLLABORATION
document_type: workstream-spec
status: active
owner: documents-collaboration-team
audited_branch: develop
audited_sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
applies_to:
  - backend
  - documents
  - collaboration
  - pages
  - page-metadata
  - page-visibility
  - page-hierarchy
  - blocks
  - block-hierarchy
  - block-content
  - block-properties
  - block-ordering
  - resource-links
  - document-versions
  - document-snapshots
  - document-history
  - document-templates
  - document-search-boundary
  - document-import-export
  - document-file-references
  - comments
  - replies
  - comment-anchors
  - mentions
  - reactions
  - attachments
  - presence
  - read-state
  - watchers
  - notifications
  - activity
  - collaboration-targets
  - document-realtime
  - collaboration-realtime
  - resource-authorization
  - tenant-isolation
  - cross-context-events
  - migrations
  - security
  - reliability
evidence:
  - docs/product/documents.md
  - docs/product/collaboration.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
  - backend/src/Notrelix.Domain/Documents/
  - backend/src/Notrelix.Domain/Collaboration/
  - backend/src/Notrelix.Application/Features/Documents/
  - backend/src/Notrelix.Application/Features/Collaboration/
  - backend/src/Notrelix.API/Endpoints/Documents/
  - backend/src/Notrelix.API/Endpoints/Collaboration/
  - backend/tests/
review_on:
  - document-boundary-change
  - collaboration-boundary-change
  - page-lifecycle-change
  - page-visibility-change
  - page-hierarchy-change
  - block-contract-change
  - block-hierarchy-change
  - block-ordering-change
  - resource-link-change
  - document-versioning-change
  - document-template-change
  - comment-lifecycle-change
  - comment-target-change
  - mention-change
  - reaction-change
  - attachment-change
  - presence-change
  - read-state-change
  - watcher-change
  - notification-change
  - activity-change
  - authorization-contract-change
  - retention-change
  - event-contract-change
  - realtime-recovery-change
  - downstream-consumer-change
  - duplicate-page-change
  - notification-preference-change
  - notification-recipient-state-change
  - collaboration-search-change
  - integration-sync-contract-change
---

# SPEC — Documents & Collaboration

## 1. Purpose

This specification defines the complete target capability contract for the Documents & Collaboration workstream.

It is the master **WHAT** document.

It covers the canonical product scope owned by:

```text
Documents
+
Collaboration
```

rather than only the currently exposed Page/Block/Comment API surface.

The execution package derived from this SPEC must be able to decide, for every current source capability, whether it is:

```text
KEEP
HARDEN
REFACTOR
MIGRATE
RETIRE
BLOCKED
OUT_OF_RELEASE_SCOPE
```

without silently dropping a product-owned capability.

Implementation order belongs to:

```text
documents-collaboration.plan.md
```

Verification belongs to:

```text
documents-collaboration.tests.md
```

Candidate evidence belongs to:

```text
documents-collaboration.certification.md
```

A requirement is not complete merely because a class, DbSet, endpoint, migration, or unit test exists.

---

## 2. Source baseline

This version of the SPEC is reconciled against:

```text
branch: develop
SHA:    35702d0fa9fb01ed68b0667bab500030d60bd028
```

The baseline matters because the source already contains materially more product capability than the previous execution package represented.

Documents source contains first-class:

```text
Pages
Blocks
ResourceLinks
Templates
Versions
```

Collaboration source contains first-class:

```text
Attachments
Comments
Mentions
Presence
Reactions
ReadStates
Watchers
```

The Application/API surfaces are uneven.

Some Domain capabilities are richer than their Application/API coverage.

Some Application commands are placeholders or semantically suspicious.

This SPEC therefore treats the current repository as a brownfield system with incomplete vertical slices.

---

## 3. Authority chain

The authority order is:

```text
Product authority
→ System architecture / accepted ADRs
→ Backend architecture
→ Team ownership / roadmap
→ this SPEC
→ PLAN
→ TESTS
→ CERTIFICATION
→ source implementation
```

Source is evidence.

Source is not automatically product truth.

If source conflicts with higher authority:

```text
SOURCE_DEBT
```

If higher authorities conflict:

```text
BLOCKED-DECISION
```

The affected capability must stop rather than choosing the easiest implementation.

---

## 4. One team, two bounded contexts

Documents and Collaboration share a team because their user experience is tightly connected.

They do not share semantic ownership.

Documents owns authored durable knowledge.

Collaboration owns human interaction around product resources.

Correct model:

```text
Documents
  Page
  Block
  hierarchy
  typed content
  ResourceLink
  history/version/snapshot
  PageTemplate

Collaboration
  Comment/reply/anchor
  Mention
  Reaction
  Attachment
  Presence
  ReadState
  Watcher
  Notification/Activity semantics
```

Incorrect model:

```text
Page aggregate owns comments/reactions/watchers

Collaboration owns Page/BoardItem state

shared "Resource" repository mutates every bounded context

realtime channel becomes canonical state
```

Team boundary is not aggregate boundary.

---

## 5. Physical architecture constraint

This SPEC does not authorize new backend services or projects.

Current production composition remains conceptually:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Documents and Collaboration remain logical bounded contexts inside the current architecture unless a separate accepted service-extraction decision changes that topology.

---

# Canonical ownership

## 6. Documents owns

Documents owns:

```text
Page identity
Page lifecycle
Page title/metadata
Page visibility intent
Page hierarchy
Block identity
Block type
Block content
Block properties
Block parent/child hierarchy
Block sibling ordering
ResourceLink
DocumentVersion
DocumentSnapshot
document history/restore semantics
PageTemplate
document content events
document deletion/restore behavior
document search/index source facts
document import/export semantic mapping
file/media reference meaning inside document content
```

---

## 7. Documents does not own

Documents does not own:

```text
Account
Workspace lifecycle/membership
Governance policy
Board/BoardItem/Field state
Comment/reply/Mention/Reaction
Presence
ReadState
Watcher
Notification provider delivery
Activity transport
object-storage mechanics
search index as canonical truth
Analytics projection
Automation rule execution
realtime transport
```

---

## 8. Collaboration owns

Collaboration owns:

```text
Comment
reply/thread relationship
CommentAnchor
Comment status
Mention
Reaction
collaboration Attachment metadata/reference
PresenceSession
ResourceReadState
ResourceWatcher
user-facing Notification semantics
user-facing Activity semantics
collaboration target references
collaboration retention reaction
collaboration events
```

---

## 9. Collaboration does not own

Collaboration does not own:

```text
Page/Block content
Board/BoardItem state
Identity profile truth
Workspace membership truth
Governance permission policy
security Audit
object-storage provider mechanics
email/push provider delivery truth
realtime transport
Automation rule internals
Analytics projection truth
```

---


## 9A. Requirement namespace normalization

The repository currently contains an ID collision that must not leak into execution.

Canonical Product authority uses:

```text
docs/product/documents.md
DCT-001..031

docs/product/collaboration.md
COL-001..029
```

while the team workstream document independently uses:

```text
docs/workstreams/teams/documents-collaboration.md
DCT-001..012
COL-001..008
```

for different capability labels.

Therefore this execution package uses explicit aliases:

```text
PROD-DCT-001..031
→ docs/product/documents.md

PROD-COL-001..029
→ docs/product/collaboration.md

TEAM-DCT-001..012
→ docs/workstreams/teams/documents-collaboration.md

TEAM-COL-001..008
→ docs/workstreams/teams/documents-collaboration.md
```

Normative rule:

```text
bare DCT-* / COL-* identifiers are not sufficient traceability
when the source document is not explicit.
```

For requirement mapping in this SPEC, PLAN, TESTS, and CERTIFICATION:

```text
PROD-DCT-*
PROD-COL-*
```

are canonical product requirement identifiers.

`TEAM-DCT-*` / `TEAM-COL-*` are execution-capability aliases only and cannot override Product semantics.

If a future repository cleanup renames the team aliases, this execution package should adopt the canonical renamed IDs rather than reusing Product IDs.

---

# Canonical product requirement traceability

## 10. Documents product mapping

This SPEC must preserve every canonical Documents requirement:

| Product requirement | SPEC coverage |
|---|---|
| PROD-DCT-001 structured Page/Block model | DCREQ014–017 |
| PROD-DCT-002 Page belongs to one Workspace | DCREQ002 |
| PROD-DCT-003 archive != delete | DCREQ003–005 |
| PROD-DCT-004 visibility != permission | DCREQ113–116 |
| PROD-DCT-005 hierarchy tenant-safe/acyclic | DCREQ006–010 |
| PROD-DCT-006 Block belongs to one Page | DCREQ012 |
| PROD-DCT-007 content validates by Block Type | DCREQ014–017 |
| PROD-DCT-008 no arbitrary unvalidated JSON | DCREQ015 |
| PROD-DCT-009 Block-tree validation uses ancestry facts | DCREQ117–121 |
| PROD-DCT-010 deterministic server ordering | DCREQ018–022 |
| PROD-DCT-011 version/concurrency-aware editing | DCREQ027–030, DCREQ136 |
| PROD-DCT-012 realtime != collaborative-edit semantics | DCREQ031, DCREQ059–066 |
| PROD-DCT-013 Comment anchors do not own content | DCREQ147–149 |
| PROD-DCT-014 ResourceLink preserves ownership | DCREQ122–127 |
| PROD-DCT-015 sharing is non-transitive | DCREQ125 |
| PROD-DCT-016 history version != aggregate version | DCREQ128–134 |
| PROD-DCT-017 Snapshot != live document | DCREQ130 |
| PROD-DCT-018 Template is creation input | DCREQ135–139 |
| PROD-DCT-019 visibility does not authorize | DCREQ114–116 |
| PROD-DCT-020 Documents does not become WM storage | DCREQ067, DCREQ102 |
| PROD-DCT-021 search/index != truth | DCREQ140–141 |
| PROD-DCT-022 binary payload not Domain/event content | DCREQ144–146 |
| PROD-DCT-023 public events do not expose full tree | DCREQ193, DCREQ054, DCREQ077 |
| PROD-DCT-024 missed realtime cannot corrupt state | DCREQ064–066 |
| PROD-DCT-025 no-op edit avoids fake history | DCREQ137 |
| PROD-DCT-026 import/export are boundaries | DCREQ142–143 |
| PROD-DCT-027 cross-Workspace move is migration | DCREQ009 |
| PROD-DCT-028 Page deletion is lifecycle workflow | DCREQ004, DCREQ220, DCREQ189 |
| PROD-DCT-029 Block deletion defines child behavior | DCREQ121 |
| PROD-DCT-030 restore validates current constraints | DCREQ005, DCREQ133 |
| PROD-DCT-031 document history != Governance Audit | DCREQ134 |

---

## 11. Collaboration product mapping

This SPEC must preserve every canonical Collaboration requirement:

| Product requirement | SPEC coverage |
|---|---|
| PROD-COL-001 target explicit/scoped | DCREQ040–042 |
| PROD-COL-002 Collaboration does not mutate target | DCREQ043 |
| PROD-COL-003 target existence/access outside pure Domain | DCREQ042, DCREQ044 |
| PROD-COL-004 reply remains same target/thread | DCREQ150–152 |
| PROD-COL-005 anchor is locator only | DCREQ147–149 |
| PROD-COL-006 Comment deletion policy explicit | DCREQ037, DCREQ048 |
| PROD-COL-007 Mention stable identity | DCREQ153–157 |
| PROD-COL-008 Mention delivery idempotent | DCREQ156 |
| PROD-COL-009 Reaction uniqueness deterministic | DCREQ158–161 |
| PROD-COL-010 Attachment metadata/object identity | DCREQ162–166 |
| PROD-COL-011 attachment download scoped/short-lived | DCREQ165 |
| PROD-COL-012 Presence ephemeral | DCREQ167–170 |
| PROD-COL-013 Presence does not authorize | DCREQ169 |
| PROD-COL-014 ReadState user+resource scoped | DCREQ171–174 |
| PROD-COL-015 unread derives from stable boundary | DCREQ173 |
| PROD-COL-016 Watcher explicit preference | DCREQ175–178 |
| PROD-COL-017 Notification explicit recipient | DCREQ179–184 |
| PROD-COL-018 provider delivery != Notification truth | DCREQ182 |
| PROD-COL-019 Activity != Audit | DCREQ185–188 |
| PROD-COL-020 Activity maps product facts | DCREQ186 |
| PROD-COL-021 new target kind is contract change | DCREQ041 |
| PROD-COL-022 Collaboration scope follows target | DCREQ047 |
| PROD-COL-023 historical Notification does not freeze access | DCREQ183 |
| PROD-COL-024 realtime replay-safe | DCREQ059–065, DCREQ190 |
| PROD-COL-025 optimistic state reconciles to authority | DCREQ219, DCREQ028–029 |
| PROD-COL-026 target deletion does not cascade history automatically | DCREQ189 |
| PROD-COL-027 history may outlive active Identity | DCREQ050 |
| PROD-COL-028 collaboration histories are purpose-specific | DCREQ188 |
| PROD-COL-029 collaboration content safe to render | DCREQ078 |

---

---


## 11A. Canonical Product semantics without dedicated Product IDs

The numbered `PROD-DCT-*` / `PROD-COL-*` requirements are not the complete Product documents.

The following canonical headings/checklists also define required semantics and must be dispositioned explicitly:

| Canonical Product semantic | SPEC coverage |
|---|---|
| Documents Page path is derived, not competing durable ownership | DCREQ201 |
| Documents duplicate creates new Page/Block identities | DCREQ202 |
| Documents external-provider formats translate into Page/Block semantics | DCREQ203 |
| Documents retention covers source/history/search/export/backups | DCREQ204 |
| reduced mobile editing cannot corrupt unsupported Block Types | DCREQ205 |
| offline editing, if released, requires identity/reconciliation/conflict/auth recheck | DCREQ206 |
| authored Block semantics must retain accessibility meaning | DCREQ207 |
| Collaboration Attachment upload lifecycle is explicit where product-visible | DCREQ208 |
| cursor/typing indicators are ephemeral signals | DCREQ209 |
| Notification delivery-channel state is separate from Notification read state | DCREQ210 |
| Notification preferences are explicit product state/policy inputs | DCREQ211 |
| Notification read/seen/archive/dismiss is Collaboration attention state | DCREQ212 |
| Workspace deletion/archive has explicit Collaboration retention/export/purge behavior | DCREQ213 |
| thread deletion preserves coherent reply display | DCREQ214 |
| Comment edit history, if supported, is Collaboration-owned history | DCREQ215 |
| public/guest Collaboration has explicit abuse-control disposition | DCREQ216 |
| Collaboration Comment/Activity search is derived and authorization-scoped | DCREQ217 |
| Collaboration provider sync maps through approved operations | DCREQ218 |
| optimistic Collaboration state reconciles to authoritative identity/state | DCREQ219 |
| Page deletion is an orchestrated lifecycle workflow, not ORM cascade | DCREQ220 |

These requirements do not force every optional product surface into every release.

PLAN must classify each as one of:

```text
RELEASED
OUT_OF_RELEASE_SCOPE
NOT_APPLICABLE
BLOCKED-DECISION
```

A capability may be outside the candidate release.

It may not disappear from full-workstream traceability.

---

# Current brownfield source facts

## 12. Source capability asymmetry

Current source is not vertically uniform.

Examples:

```text
Domain capability exists
+
persistence exists
+
Application/API missing or partial
```

This is true for multiple Documents and Collaboration capabilities.

Therefore:

```text
absence of endpoint
!=
absence of product capability
```

and:

```text
presence of Domain type
!=
release-ready feature
```

---

## 13. Known Documents source-debt facts

At audited `develop`:

```text
MovePageCommandHandler
→ throws NotImplementedException

PublishPageCommandHandler
→ throws NotImplementedException

SetPageDeadlineCommandHandler
→ throws NotImplementedException

GetPageHistoryQueryHandler
→ always returns an empty list after existence check

UpdatePageRequest
→ includes icon/cover fields
but UpdatePageEndpoint/Application only forwards Title
```

These are source facts.

They do not by themselves decide the target behavior.

PLAN must classify each as:

```text
IMPLEMENT
HARDEN
RETIRE
BLOCKED-DECISION
```

against this SPEC and product authority.

---

## 14. Known Documents authorization drift

Current source uses a mix of Documents-specific and WorkManagement actions.

Observed examples include:

```text
CreatePage       → CreatePage
ArchivePage      → ArchivePage
GetPageBlocks    → ViewPage
GetBreadcrumb    → ViewPage
GetPageHistory   → ViewPage

CreateBlock      → ManageBoard
UpdateBlock      → ManageBoard
DeleteBlock      → ManageBoard

UpdatePage       → ManageBoard
DeletePage       → ManageBoard
MovePage         → ManageBoard
PublishPage      → ManageBoard
SetPageDeadline  → ManageBoard

GetPage          → ViewBoard
```

This is not accepted as canonical merely because it compiles.

Every released request must be reconciled to the canonical resource/action model.

---

## 15. Known Collaboration source-debt facts

At audited `develop`:

```text
GetResourceActivityQueryHandler
→ returns empty data / total = 0
```

But Activity is not absent from the source.

Infrastructure already contains projection consumers such as:

```text
BoardCreatedActivityConsumer
CommentCreatedActivityConsumer
MentionCreatedActivityConsumer
WorkspaceMemberAddedActivityConsumer
```

which persist:

```text
WorkspaceActivityLogRecord
```

Therefore the current Activity debt is:

```text
existing projection
+
missing/unwired Collaboration resource-activity query surface
```

not permission to create a second Activity truth/store.

Current source also has durable Notification records:

```text
NotificationItemRecord
NotificationRecipientRecord
NotificationPreferenceRecord
NotificationCounterRecord
```

and `MentionCreatedNotificationConsumer` persists Notification + recipient state from a committed Mention fact with deduplication.

`NotificationRecipientRecord` already represents attention state such as:

```text
Unread
Seen
Read
Archived
Dismissed
```

while `NotificationPreferenceRecord` already represents:

```text
notification type
channel
enabled state
delivery mode
digest interval
quiet hours
timezone
```

Therefore the unresolved Notification question is not:

```text
does durable Notification state exist?
```

It is:

```text
how current Infrastructure persistence/runtime wiring satisfies
Collaboration-owned Notification product semantics,
what Application/API contract exposes it,
and which parts are release-scoped.
```

Other current Collaboration facts include:

```text
CreateComment
→ already creates Mention rows

Comment Domain
→ supports reply, anchor, resolve, reopen, delete, restore

Reaction
PresenceSession
ResourceReadState
ResourceWatcher
Attachment
→ exist in Domain/persistence with uneven Application/API coverage
```

---

## 16. Known Collaboration authorization drift

The current source uses target-specific and borrowed WorkManagement actions across more than Comment.

Observed examples include:

```text
CreateComment              → CreateComment

GetComments                → ViewBoard
UpdateComment              → UpdateItem
DeleteComment              → UpdateItem
ResolveComment             → UpdateItem

GetBoardItemAttachments    → ViewBoard
CreateBoardItemAttachment  → UpdateItem
DeleteAttachment           → UpdateItem on collaboration.attachment

GetResourceActivity        → ViewBoard on a generic ResourceKind
```

Some mappings may be accepted after policy reconciliation.

None are accepted merely because the request currently executes.

The release gate therefore requires one Collaboration request/resource/action matrix covering:

```text
Comment
Attachment
Activity
Mention
Reaction
Presence
ReadState
Watcher
Notification
```

as each surface becomes release-scoped.

The CreateComment policy matrix certifies only the CreateComment paths it actually exercises.

---

# Documents — Page core

## 17. DCREQ001 — Stable Page identity

A Page has one canonical stable ID.

The ID survives:

```text
rename
metadata change
visibility change
move
content edits
archive
restore
history creation
```

Title/path is never durable Page identity.

---

## 18. DCREQ002 — Page Account/Workspace containment

Every Page belongs to exactly one authoritative Workspace and Account scope.

Any parent relation, Block relation, query, history record, event, ResourceLink, Collaboration target, or realtime projection must preserve that scope.

Client-supplied scope is not authority.

---

## 19. DCREQ003 — Page lifecycle

Canonical Page lifecycle distinguishes at minimum the source/product-supported states:

```text
active
archived
deleted
restored where allowed
```

Archive and delete are not aliases.

Invalid transitions must fail without side effects.

---

## 20. DCREQ004 — Page archive/delete semantics

Archive:

- keeps durable identity;
- does not physically purge content;
- applies read/write restrictions according to product policy.

Delete:

- follows explicit lifecycle/retention semantics;
- does not rely on ORM cascade to define cross-context product behavior.

Physical purge is separate retention work.

---

## 21. DCREQ005 — Historical Page attribution and restore

Historical references may outlive active Page visibility/lifecycle according to retention policy.

Restore must validate current:

```text
Workspace
parent
authorization
schema
retention
```

Historical state is not automatically valid under present constraints.

---

# Page metadata and visibility

## 22. DCREQ113 — Page metadata contract

Page metadata that is product-supported must have one coherent contract across:

```text
Domain
Application
API
OpenAPI/generated client
persistence
```

Current source includes fields such as:

```text
Title
Icon
CoverImage
Visibility
```

An API field must not be accepted and silently discarded.

Unsupported metadata must be removed from the public contract or explicitly implemented.

---

## 23. DCREQ114 — Page visibility intent

Canonical visibility includes current supported values such as:

```text
Private
Workspace
Public
```

Visibility expresses document access intent/default posture.

It does not itself grant final authorization.

---

## 24. DCREQ115 — Visibility is an authorization input, not an authorization engine

A Page marked:

```text
Public
```

must still obey applicable:

```text
Governance
share-link
resource restrictions
embedded-resource authorization
retention/security policy
```

No handler may short-circuit Governance solely because of PageVisibility.

---

## 25. DCREQ116 — Visibility change is a governed mutation

Changing visibility is a distinct protected action with:

```text
server authorization
tenant scope
event/history implications
cache/realtime invalidation
```

if the product exposes visibility mutation.

If it is not release-scoped, source/API placeholders must not imply that it is already supported.

---

# Page hierarchy

## 26. DCREQ006 — Explicit Page hierarchy

Page parent/child relation is explicit.

Hierarchy storage may vary, but semantics must preserve:

```text
same Workspace
valid parent
acyclic structure
lifecycle constraints
authorization
```

---

## 27. DCREQ007 — Page cycle prevention

A Page cannot be:

- its own parent;
- a descendant of itself;
- moved under a descendant through concurrent state.

Cycle prevention must be authoritative server behavior.

Frontend tree validation is not sufficient.

---

## 28. DCREQ008 — Page move/reparent contract

Move/reparent is a dedicated semantic operation.

It must validate:

```text
source
destination
scope
source authorization
destination authorization
cycle
lifecycle
concurrency
```

and preserve Page identity.

---

## 29. DCREQ009 — Cross-Workspace Page movement is migration

Ordinary Page reparent must not change Workspace/Account.

Cross-Workspace movement affects:

```text
authorization
Blocks
links
comments
history
search
Automation
Integrations
Analytics
retention
```

and therefore is a migration/transfer workflow, not a simple parent update.

---

## 30. DCREQ010 — Page hierarchy concurrency

Conflicting Page moves must produce:

```text
one valid committed hierarchy
or deterministic conflict
```

They must never commit a cycle or broken parent relation.

---

# Blocks — identity, type, content

## 31. DCREQ011 — Stable Block identity

A Block has one stable ID independent of:

```text
position
parent
editor node identity
frontend key
```

Moving or editing a Block does not implicitly replace identity.

---

## 32. DCREQ012 — Block belongs to exactly one Page

A Block belongs to one authoritative:

```text
Account
Workspace
Page
```

Ordinary Block parent/move operations must stay inside the same Page.

A cross-Page content transfer, if product-supported, must be a dedicated copy/migration/recreate workflow with explicit identity/history semantics.

It is not an ordinary `Move`.

---

## 33. DCREQ013 — Block lifecycle

Create/update/move/delete/restore behavior must be explicit.

Deleted/archived parent Page state must prevent invalid Block mutation.

---

## 34. DCREQ014 — Stable Block Type identity

A Block Type is a semantic persisted discriminator.

It must not be derived from:

```text
frontend component name
DOM element
CLR concrete class name
```

---

## 35. DCREQ015 — Block content/properties contract

Each released Block Type defines as applicable:

```text
content schema
properties schema
normalization
validation
empty/null behavior
size limits
renderer/editor compatibility
migration/evolution
```

Arbitrary unvalidated JSON is forbidden.

---

## 36. DCREQ016 — Persisted content compatibility

Changing a persisted Block content/properties shape requires explicit compatibility handling.

Candidate code must:

```text
read old valid content
or
migrate it deterministically
```

---

## 37. DCREQ017 — Unknown Block type/version behavior

Unknown type/version must not silently coerce to another semantic type.

Behavior must be explicit:

```text
safe unsupported representation
read-only fallback
migration requirement
or deterministic rejection
```

without data loss.

---

# Block hierarchy

## 38. DCREQ117 — Block parent relation is first-class

A Block may have an optional parent according to supported content-tree semantics.

The parent relation is part of Documents Domain semantics, not merely frontend nesting.

---

## 39. DCREQ118 — Block parent must share scope

A parent Block must belong to the same:

```text
Account
Workspace
Page
```

as the child.

Supplying a raw parent ID is insufficient.

Authoritative ancestry facts must be validated.

---

## 40. DCREQ119 — Block tree is acyclic

A Block cannot become:

```text
its own parent
ancestor under descendant
member of a cyclic ancestry chain
```

Cycle prevention belongs to authoritative server behavior.

---

## 41. DCREQ120 — Block move changes parent and position coherently

A Block move that changes parent/order is one semantic mutation.

The committed result must never expose:

```text
new parent + old invalid order
or
new order + stale/invalid parent
```

---

## 42. DCREQ121 — Block deletion defines child behavior

Deleting a parent Block must have one explicit policy such as:

```text
delete subtree
promote/reparent children
reject until children handled
```

The policy must not be defined accidentally by persistence cascade.

---

# Block ordering

## 43. DCREQ018 — Canonical server-authoritative Block ordering

Sibling order is durable business state.

The canonical order must be deterministic across:

```text
server
database
API
frontend rendering
realtime recovery
```

---

## 44. DCREQ019 — Approved ordering primitive

Current source uses a fractional ordering primitive.

The implementation must preserve:

```text
valid sortable keys
dense insertion
boundary/prefix correctness
deterministic comparison
```

and must not replace it with naive floating midpoint arithmetic.

---

## 45. DCREQ020 — Ordering operations

Released operations may include:

```text
insert first
append last
insert between siblings
move before/after
move under another parent in same Page
```

They must preserve Block tree invariants.

Ordinary cross-Page move is forbidden by DCREQ012.

---

## 46. DCREQ021 — Ordering concurrency

Concurrent insert/move operations must not produce ambiguous canonical ordering.

Allowed outcomes:

```text
valid deterministic order
or deterministic conflict/retry
```

---

## 47. DCREQ022 — Ordering persistence roundtrip

Semantic ordering must survive:

```text
commit
fresh DbContext
reload
query
serialization
```

An in-memory-only ordering proof is insufficient.

---

# Document editing, concurrency and semantic no-op

## 48. DCREQ027 — Server state is authoritative persisted truth

Editor-local state may own:

```text
selection
cursor
focus
drag
composition
temporary optimistic state
```

It must not become a second long-lived canonical document store.

---

## 49. DCREQ028 — Mutation reconciliation

Every released optimistic mutation must define:

```text
optimistic change
server request
success reconciliation
server rejection rollback
competing update behavior
final canonical query/recovery
```

---

## 50. DCREQ029 — Temporary client identity

If temporary IDs are used, mapping to server identity must be deterministic and duplicate-safe.

If not used, this requirement is `NOT_APPLICABLE` with evidence.

---

## 51. DCREQ030 — Stale mutation behavior

A stale client mutation must not silently overwrite newer authoritative state when concurrency protection is required.

The contract may use:

```text
expected version
conditional update
conflict/retry
approved merge protocol
```

but must be explicit.

---

## 52. DCREQ031 — Collaborative editing boundary

Realtime connectivity does not imply CRDT/OT semantics.

True simultaneous collaborative editing requires explicit architecture for:

```text
operation identity
merge
conflict
offline/reconnect
checkpoint
history
authorization revocation
```

No implementation may invent CRDT/OT merely to satisfy a realtime requirement.

---

## 53. DCREQ136 — Concurrency version and document-history version are distinct

The system must not conflate:

```text
aggregate optimistic-concurrency version
document user/history version
snapshot/checkpoint identity
realtime sequence
```

Each has a distinct purpose.

---

## 54. DCREQ137 — Semantic no-op does not create false history

An effective no-op such as identical:

```text
title
content
properties
parent/position
```

should avoid unnecessary:

```text
aggregate version increment
Domain event
DocumentVersion/history
Activity
```

where current Domain/product semantics define no-op behavior.

---

# Document query/loading

## 55. DCREQ023 — Explicit document query contract

Queries must define:

```text
scope
lifecycle visibility
authorization
DTO/result shape
loading boundaries
error semantics
```

EF entities are not public query contracts.

---

## 56. DCREQ024 — Bounded document loading

Large hierarchy/Page queries must avoid unbounded recursive/N+1 behavior.

The implementation must be measured against representative product scale.

---

## 57. DCREQ025 — Query authorization consistency

A resource denied by direct get must not leak through:

```text
tree
list
search
breadcrumb
history
recent/derived query
```

unless a canonical policy explicitly differs.

---

## 58. DCREQ026 — No cross-context query shortcut

Documents queries must not solve performance by reading foreign private persistence.

Derived search/index projections remain non-authoritative.

---

# Resource Links

## 59. DCREQ122 — ResourceLink has explicit source and target identity

A ResourceLink is a Documents-owned relationship containing stable resource references.

It does not own the foreign resource.

---

## 60. DCREQ123 — ResourceLink scope is tenant-safe

Source/target relationship must obey current cross-Workspace policy.

Cross-Workspace links are forbidden by default unless an explicit product contract authorizes them.

---

## 61. DCREQ124 — ResourceLink cannot self-reference where prohibited

The Domain must reject invalid self-link semantics according to current product rules.

---

## 62. DCREQ125 — Sharing/access is non-transitive across ResourceLinks

A public/shared Page that links/embeds a private foreign resource does not make that resource public.

Rendering/opening the target re-evaluates target authorization.

---

## 63. DCREQ126 — ResourceLink deletion deletes the relationship only

Deleting/restoring a ResourceLink must not mutate/delete the target resource.

---

## 64. DCREQ127 — Target lifecycle behavior is explicit

When a linked target is archived/deleted:

```text
tombstone
unavailable link
hidden embed
historical reference
cleanup
```

must be explicitly defined.

Persistence cascade is not product policy.

---

# Document Versions, Snapshots and history

## 65. DCREQ128 — DocumentVersion is user/history state

DocumentVersion represents document history/recovery semantics.

It is not interchangeable with aggregate optimistic concurrency counters.

---

## 66. DCREQ129 — Version creation policy is explicit

The system must define which mutations create a user-visible/history version.

It must not create permanent history for every internal write by accident.

---

## 67. DCREQ130 — DocumentSnapshot is immutable historical representation

A snapshot may support:

```text
history
restore
checkpoint
migration
```

but is not a second live mutable document.

---

## 68. DCREQ131 — Snapshot schema/version is explicit

Historical snapshots must identify the schema/version needed to interpret them safely across Block-type evolution.

---

## 69. DCREQ132 — History query is real product behavior

A Page history endpoint must return real canonical history or be removed/redefined.

A constant empty result is a stub, not a completed capability.

---

## 70. DCREQ133 — Restore creates a new current mutation

Restoring historical content must:

```text
authorize
validate current constraints
apply restored state
preserve the fact that newer history existed
create appropriate new current/history fact
```

It must not rewrite history as if intervening changes never existed.

---

## 71. DCREQ134 — Document history is not Governance Audit

History and Audit may share source facts but differ in:

```text
purpose
retention
integrity
visibility
user interaction
```

Documents must not treat Audit as its history store or vice versa.

---

# Templates

## 72. DCREQ135 — PageTemplate is Documents-owned creation input

Template instantiation creates ordinary Page/Block identities.

The template is not hidden live authority over previously created Pages.

---

## 73. DCREQ138 — Template lifecycle is independent

Template states such as:

```text
Draft
Published
Archived
```

are separate from Page lifecycle.

---

## 74. DCREQ139 — Template content obeys normal Block contracts

Template Page/Block snapshot/content must obey current Block-type validation and migration compatibility.

Template change must not silently mutate existing Pages unless a linked-template feature is explicitly designed.

---

# Search, import/export and files

## 75. DCREQ140 — Search/index is derived state

Search may index Page/Block content.

It is not authoritative document truth.

Index lag/failure cannot decide document mutation semantics.

---

## 76. DCREQ141 — Search remains authorization-filtered

Search results must respect current tenant/resource authorization and permission revocation.

A stale search index must not expose inaccessible resource content.

---

## 77. DCREQ142 — Import maps into validated Documents semantics

External HTML/Markdown/provider formats must map into canonical Page/Block semantics.

Untrusted raw content must be sanitized/validated.

Provider schema does not become Documents schema automatically.

---

## 78. DCREQ143 — Export is a boundary representation

HTML/Markdown/PDF/etc. are derived representations.

Any round-trip guarantee must be explicit per format.

---

## 79. DCREQ144 — Binary payload is not Domain content

Large binary content must not be stored in:

```text
Domain events
logs
arbitrary Block JSON
```

Documents may store stable file/object identity and safe metadata/reference.

---

## 80. DCREQ145 — File/media reference ownership

Documents owns the meaning of a file/media reference in Block content.

Infrastructure owns object-storage/provider mechanics.

---

## 81. DCREQ146 — Download/access capability remains authorized

Any generated/signed media access must be:

```text
resource-scoped
time-bounded where applicable
permission-aware
```

and must not turn an object key/URL into authorization proof.

---

# Documents authorization

## 82. DCREQ032 — Documents owns resource/action meaning

Documents defines product actions for resources such as:

```text
Page
Block where independently addressable
ResourceLink
DocumentVersion/history
PageTemplate
```

Governance evaluates policy.

---

## 83. DCREQ033 — Central authorization enforcement

Protected requests use the canonical Application authorization mechanism.

No handler-local role matrix may become competing policy authority.

Business ownership invariants may remain local when semantically distinct.

---

## 84. DCREQ034 — Destination authorization on structural move

A Page/Block structural move must validate destination capability as well as source mutation authority.

Access to the source alone is not enough.

---

## 85. DCREQ035 — Consistent existence-disclosure behavior

Denied/not-found behavior must follow canonical API/security policy across direct and aggregate queries.

---

## 86. DCREQ191 — Documents request/action matrix must be reconciled

Every release-scoped Documents request must map to the canonical:

```text
resource kind
resource ID
PermissionAction
```

Observed `ManageBoard`/`ViewBoard` reuse on Documents resources must be either:

```text
replaced
or explicitly justified by accepted policy semantics
```

before D5.

---

# Collaboration target contract

## 87. DCREQ040 — Explicit target reference

Every collaboration object targets an explicit stable resource reference.

The target reference identifies at least:

```text
resource kind
resource ID
Workspace scope where needed
```

and must not serialize CLR type/table identity.

---

## 88. DCREQ041 — Supported target registry is explicit

Supported target kinds are a contract.

Adding a new target kind is a cross-context contract change requiring:

```text
owner
scope
existence fact
lifecycle fact
authorization semantics
retention semantics
tests
```

Generic runtime acceptance does not certify arbitrary target kinds.

---

## 89. DCREQ042 — Target-owner fact boundary

Target existence/scope/lifecycle/authorization facts come from:

```text
target owner public/application contract
+
Governance
```

not direct foreign DbContext/table access.

---

## 90. DCREQ043 — Collaboration lifecycle never mutates target implicitly

Creating/editing/deleting:

```text
Comment
Mention
Reaction
Attachment
Watcher
ReadState
```

must not mutate target business state unless a separate explicit target-context operation is invoked.

---

## 91. DCREQ044 — Target access is prerequisite, not whole Collaboration policy

A target may be accessible while a collaboration action is still forbidden.

Authorization may require:

```text
target access
+
collaboration action
+
author/moderator semantics
```

---

## 92. DCREQ046 — Target access is revalidated

Previously granted target access does not freeze permission forever.

Subsequent query/mutation/recovery must respect current permission.

---

## 93. DCREQ047 — Collaboration scope follows target

A Collaboration object cannot target another Workspace/tenant through a forged ResourceRef.

Target scope is authoritative.

---

# Comments, replies, anchors and status

## 94. DCREQ036 — Stable Comment identity

Comment ID survives:

```text
content edit
resolve/reopen
profile change
target display change
```

according to lifecycle.

---

## 95. DCREQ037 — Comment lifecycle

Released Comment lifecycle includes current supported semantics:

```text
create
edit
resolve
reopen
delete
restore
```

where exposed/approved.

The Application/API surface must either implement or explicitly classify each Domain capability.

---

## 96. DCREQ038 — Stable author attribution

Comment stores stable author/actor identity.

Mutable profile fields are projections, not identity.

---

## 97. DCREQ039 — Comment content validation

Comment content must have server-side:

```text
non-empty
size
markup/structure safety
```

according to product contract.

---

## 98. DCREQ045 — Edit/delete/resolve semantics are explicit

Authorization must distinguish where applicable:

```text
author edit/delete
moderator/admin action
resolve/reopen
target access
```

`CreatedBy` alone must not silently replace Governance policy.

---

## 99. DCREQ147 — CommentAnchor is locator metadata

An anchor locates:

```text
Page
Block
selector
range
```

or other supported target fragment.

It does not own document content.

---

## 100. DCREQ148 — Anchor staleness is explicit

Document edits/moves/deletes may make an anchor stale.

The system must define:

```text
retain stale anchor
re-resolve
hide
tombstone
```

where release-scoped.

Deleting/editing a Comment must never mutate anchored document content.

---

## 101. DCREQ149 — Anchor scope is target-safe

An anchor must not redirect a Comment to another target/tenant.

Anchor data is subordinate to the canonical target ResourceRef.

---

## 102. DCREQ150 — Reply remains on same target

A reply must reference a parent Comment in the same:

```text
Account
Workspace
target resource
```

---

## 103. DCREQ151 — Reply to deleted/invalid parent follows explicit policy

Current source forbids replying to deleted parent.

The release contract must preserve or deliberately change that rule with product authority.

---

## 104. DCREQ152 — Thread identity is not arbitrary nesting

Reply/thread behavior must define:

```text
parent semantics
depth if bounded
target consistency
deletion behavior
query ordering
```

without importing foreign target hierarchy.

---

# Mentions

## 105. DCREQ153 — Mention is first-class Collaboration state

A Mention has stable:

```text
source ResourceRef
MentionType
MentionedId
Workspace scope
created actor/time
```

according to current source/product semantics.

---

## 106. DCREQ154 — Mention identity/scope is validated

Mentioned identity must be valid for the supported MentionType and safe for target/Workspace scope.

Client-provided user IDs are not automatically valid recipients.

---

## 107. DCREQ155 — Mention lifecycle follows Comment edit semantics

When Comment content changes, the system must define:

```text
new mention
unchanged mention
removed mention
```

behavior.

A Comment edit must not repeatedly recreate/re-notify unchanged Mentions unless policy says so.

---

## 108. DCREQ156 — Mention delivery is idempotent

Notification/attention delivery caused by the same committed Mention fact must not duplicate durable recipient effects under retry.

---

## 109. DCREQ157 — Mention privacy respects current access

Mentioning a user does not grant target access.

A historical Mention/Notification must not expose content after permission revocation.

---

# Reactions

## 110. DCREQ158 — Reaction is user + target + emoji collaboration state

Reaction identity/uniqueness must be deterministic according to product semantics.

---

## 111. DCREQ159 — Reaction duplicate/retry behavior

Retrying the same logical reaction must not create duplicate counts/state.

---

## 112. DCREQ160 — Reaction removal is explicit

Removing a Reaction mutates Collaboration state only.

It does not mutate the target aggregate.

---

## 113. DCREQ161 — Reaction count is derived from durable reaction state

A cached/projected count may exist, but must be recoverable from authoritative Reaction state or an explicitly authoritative projection contract.

---

# Collaboration attachments

## 114. DCREQ162 — Attachment stores metadata/reference, not arbitrary binary Domain data

Attachment owns safe metadata and object/reference identity.

Raw binary belongs to storage/provider infrastructure.

---

## 115. DCREQ163 — Attachment target is explicit

Attachment must target an approved Collaboration resource through ResourceRef or a specific canonical relation.

The current BoardItem-centric API does not by itself define all possible target support.

---

## 116. DCREQ164 — Attachment authorization follows target and collaboration policy

Create/list/delete/download must verify:

```text
target access
collaboration attachment action
tenant scope
```

as applicable.

---

## 117. DCREQ165 — Attachment download capability is scoped

Signed/direct download capability must be:

```text
short-lived where appropriate
resource-scoped
permission-aware
```

Object URL/key alone is not authorization.

---

## 118. DCREQ166 — Attachment deletion/retention is explicit

Deleting target/comment/attachment must follow product retention.

Provider-object deletion is not silently equivalent to collaboration-history deletion.

---

# Presence

## 119. DCREQ167 — Presence is ephemeral

PresenceSession is collaboration awareness state.

Loss, delay, reconnect, and duplicate connection updates may be tolerated without corrupting durable product state.

---

## 120. DCREQ168 — Presence is scoped

Presence must be scoped to relevant:

```text
User
Account
Workspace
resource/channel
```

where applicable.

---

## 121. DCREQ169 — Presence never authorizes access

Joining/updating presence requires normal server authorization.

Existing PresenceSession is never permission proof.

---

## 122. DCREQ170 — Presence cleanup/reconnect semantics are bounded

Ghost presence is tolerated only within defined expiry/heartbeat/reconnect behavior.

Presence recovery must not mutate durable Page/Comment truth.

---

# Read state

## 123. DCREQ171 — ReadState belongs to user + resource

ReadState must be keyed/scoped by:

```text
User
target resource
tenant/Workspace
```

according to current source.

---

## 124. DCREQ172 — ReadState is monotonic where semantics require

A stale/reordered update must not regress a later accepted read boundary.

---

## 125. DCREQ173 — Unread count derives from a stable read boundary

UnreadCount must not become unrecoverable sole truth.

It must be derivable/reconcilable from stable read/message boundaries or an explicitly authoritative projection.

---

## 126. DCREQ174 — Mark-read requires current target access

Historical target access does not authorize future ReadState mutation after permission revocation.

---

# Watchers

## 127. DCREQ175 — Watch state is explicit user-resource preference

Opening a resource, being a Workspace member, or having one old Mention does not automatically equal permanent watch state unless an explicit rule says so.

---

## 128. DCREQ176 — Automatic watching rules are explicit

If comment/assignment/mention automatically creates Watcher state:

```text
trigger
reason
dedup
unwatch interaction
```

must be defined.

---

## 129. DCREQ177 — Watch/unwatch is idempotent

Retries must not create duplicate watchers or resurrect an explicit unwatch unintentionally.

---

## 130. DCREQ178 — Watcher does not grant access

Watcher state never replaces target authorization.

Permission revocation overrides watch-driven delivery/access.

---

# Notifications

## 131. DCREQ179 — User-facing Notification is Collaboration semantics

Until architecture authority explicitly changes ownership, product Notification semantics belong to Collaboration even if no dedicated Domain folder currently exists.

Transport/provider implementations may live elsewhere.

---

## 132. DCREQ180 — Notification has explicit recipient

Every durable user-facing Notification has a recipient identity and tenant/resource scope sufficient for privacy enforcement.

---

## 133. DCREQ181 — Notification source is a committed product fact

Notification must be caused by approved committed facts such as:

```text
Mention
Comment
Watcher-relevant change
```

not raw transport attempts.

---

## 134. DCREQ182 — Provider delivery result is not Notification truth

Email/push/provider success/failure is delivery state.

It does not define whether the product Notification exists/read/dismissed.

---

## 135. DCREQ183 — Historical Notification does not freeze permission

A user who loses target access must not regain content visibility through an old Notification.

Deep-link rendering re-evaluates access.

---

## 136. DCREQ184 — Notification fan-out/idempotency is recipient-safe

Each recipient gets independent durable attention state.

Retry must not duplicate one logical Notification for the same recipient/event unless product semantics explicitly allow it.

---

# Activity

## 137. DCREQ185 — User-facing Activity is Collaboration semantics

Activity is a user-visible projection of product facts.

It is not Governance Audit.

---

## 138. DCREQ186 — Activity maps business facts, not transport attempts

Activity may consume:

```text
Page facts
Comment facts
Mention facts
WorkManagement facts
```

but must not represent broker/websocket/provider attempts as product activity.

---

## 139. DCREQ187 — Activity query must be real or explicitly unsupported

A constant empty Activity response is a stub.

The release must either:

```text
implement Activity projection/query
or remove/defer the endpoint contract explicitly
```

---

## 140. DCREQ188 — Collaboration histories are purpose-specific

Do not collapse:

```text
Comment edit history
Notification history
Activity feed
ReadState
Governance Audit
```

into one generic history model without product authority.

---

# Collaboration query contract

## 141. DCREQ051 — Comment query contract

Comment query defines:

```text
target
tenant
authorization
pagination
ordering
deleted visibility
thread/reply shape
author projection
status
```

---

## 142. DCREQ052 — No per-comment authorization fan-out where unnecessary

For a single target thread, target-level authorization should not become one permission lookup per Comment unless product semantics genuinely require per-Comment policy.

---

## 143. DCREQ053 — Stable Comment ordering

Comment/thread ordering must be deterministic under equal/near-equal timestamps and pagination.

---

## 144. DCREQ192 — Collaboration request/action matrix must be reconciled

Every release-scoped Collaboration request must map to canonical resource/action semantics.

Observed use of:

```text
ViewBoard
UpdateItem
```

for generic Comment operations must be either corrected or explicitly justified per target type.

No D5 claim while this mapping is ambiguous.

---

# Target deletion and retention

## 145. DCREQ048 — Target deletion does not automatically delete Comment history

Comment outcome on target archive/delete must be explicit.

Possible policies include:

```text
retain
hide
tombstone
explicit cleanup
```

---

## 146. DCREQ049 — Retained target references preserve integrity

If retained after target deletion, Collaboration must preserve stable target identity/scope without exposing private deleted content.

---

## 147. DCREQ050 — Identity deletion/privacy interaction

Historical collaboration may outlive active Identity.

The system must separate:

```text
stable actor reference
display profile
privacy/anonymization
content retention
Audit
```

---

## 148. DCREQ189 — Target deletion policy applies across all Collaboration state

Target deletion/archival must explicitly classify effects on:

```text
Comments
Replies
Anchors
Mentions
Reactions
Attachments
Watchers
ReadState
Notifications
Activity
Presence
```

No global FK cascade may silently decide the product outcome.

---

# Events

## 149. DCREQ054 — Documents integration-event ownership

Documents emits only producer-owned facts.

Potential current facts include:

```text
Page created/renamed/moved/archived/deleted/restored
Block created/changed/moved/deleted/restored
ResourceLink changes
DocumentVersion created/restored
Template lifecycle
```

Exact released events come from source/manifest authority.

---

## 150. DCREQ055 — Collaboration integration-event ownership

Collaboration emits producer-owned facts for released capability such as:

```text
Comment
Mention
Reaction
Attachment
Watcher
Presence where appropriate
Notification/Activity projection input
```

Ephemeral presence transport should not be turned into durable integration events without need.

---

## 151. DCREQ056 — Event tenant/resource identity

Public integration events include enough stable identity to route/authorize consumers without reading producer private storage.

---

## 152. DCREQ057 — Event version compatibility

Public event contract uses the canonical name/version registry.

Breaking shape change requires version/migration compatibility.

---

## 153. DCREQ058 — Integration events are committed facts

Externally deliverable producer events represent committed state.

Transaction failure must not publish a false completed business fact.

---

## 154. DCREQ193 — Public document events do not expose full document trees by default

Do not publish entire:

```text
Page tree
Block tree
snapshot
Comment thread
```

for ordinary lifecycle changes.

Events should carry producer facts, not bulk private state.

---

# Cross-context handoffs

## 155. DCREQ067 — Documents ↔ Collaboration handoff

Documents exposes stable resource facts.

Collaboration owns discussion/attention state.

Neither context writes the other's private persistence.

---

## 156. DCREQ068 — WorkManagement target handoff

For Board/BoardItem targets:

```text
WorkManagement owns target
Collaboration owns collaboration state
```

Integration uses stable public contracts.

---

## 157. DCREQ069 — Automation handoff

Direction:

```text
Documents/Collaboration fact
→ Automation trigger
```

Automation internals are not invoked directly from producer handlers.

---

## 158. DCREQ070 — Analytics handoff

Analytics consumes approved facts/projections and owns derived reporting state.

No producer business decision depends on Analytics private data.

---

## 159. DCREQ071 — Notification/Activity handoff

Product facts may feed user-facing Notification/Activity.

Delivery failure must not redefine the producer fact.

Security Audit remains separate.

---

# Realtime and recovery

## 160. DCREQ059 — Realtime is delivery/projection, not source of truth

Durable Page/Block/Comment/etc. truth remains in authoritative server state.

---

## 161. DCREQ060 — Document realtime contract

Realtime document updates must identify enough resource/version information for the client to reconcile safely.

---

## 162. DCREQ061 — Collaboration realtime contract

Realtime may deliver:

```text
Comment
Reaction
Presence
Notification
ReadState-related projection
```

where released.

Each type must define durable-vs-ephemeral recovery behavior.

---

## 163. DCREQ062 — Duplicate delivery is safe

Repeated delivery of the same logical event must not duplicate durable state or counts.

---

## 164. DCREQ063 — Out-of-order delivery is safe

Older delivery after newer state must not regress authoritative client-visible state.

---

## 165. DCREQ064 — Missed-event gap recovery

A client missing durable changes must have a recovery path such as:

```text
authoritative query reload
checkpoint/snapshot
sequence recovery
approved collaborative resync
```

---

## 166. DCREQ065 — Final convergence

After duplicate/out-of-order/disconnect/reconnect, released durable realtime surfaces must converge to authoritative server state.

---

## 167. DCREQ066 — Platform recovery dependency

Generic transport/reconnect/gap mechanisms belong to Platform.

Documents/Collaboration own resource-specific final-state semantics.

If Platform recovery is insufficient, realtime certification is blocked without blocking unrelated non-realtime core.

---

## 168. DCREQ190 — Replay safety applies to all released Collaboration projections

Duplicate/reordered realtime must not corrupt:

```text
Comment set
Reaction count/state
Notification set/read state
ReadState/unread state
Watcher state
Presence view
```

according to each capability's durability model.

---

# Tenant isolation and security

## 169. DCREQ072 — Account/Workspace isolation

Every Documents/Collaboration path preserves authoritative tenant scope.

---

## 170. DCREQ073 — Client scope is not authority

Client-provided:

```text
AccountId
WorkspaceId
ResourceRef
PageId
BoardItemId
```

must not grant access by possession.

---

## 171. DCREQ074 — Persistence/RLS defense

Where RLS is part of architecture, it must be enforced using real application DB role/session context.

Application filtering does not replace RLS evidence, and owner/admin bypass is not valid RLS proof.

---

## 172. DCREQ075 — Background/realtime authority is explicit

Jobs/consumers/realtime subscriptions require explicit:

```text
Account
Workspace
resource scope
```

and least-privilege authority.

No null/default global fallback.

---

## 173. DCREQ076 — Permission revocation propagates

Permission change must invalidate applicable:

```text
queries
Comment access
attachment download
watch-driven content visibility
realtime subscriptions/recovery
search projections
```

within the accepted contract.

---

## 174. DCREQ077 — Sensitive content handling

Document/Comment content may contain sensitive information.

Ordinary:

```text
logs
metrics
ProblemDetails
message diagnostics
```

must not expose raw content unnecessarily.

---

## 175. DCREQ078 — Safe rich-content rendering

If HTML/Markdown/rich text is supported, allowed structure/escaping/sanitization must be explicit.

Editor-produced content is not inherently trusted.

---

# Persistence and transaction ownership

## 176. DCREQ079 — Documents owns Documents persistence

Foreign contexts do not directly mutate Documents private tables/entities.

---

## 177. DCREQ080 — Collaboration owns Collaboration persistence

Foreign contexts do not directly mutate Collaboration private tables/entities.

---

## 178. DCREQ081 — Local semantic mutation is atomic

Same-context operations that must be coherent commit atomically.

Examples:

```text
Page move + hierarchy state
Block parent + order change
Comment + same-context Mention creation where semantically one operation
```

---

## 179. DCREQ082 — Cross-context atomicity is explicit

Cross-context effects use:

```text
events
approved orchestration
public contracts
eventual consistency
```

according to architecture.

Same process/database is not permission for shared transactional ownership.

---

# Migration and compatibility

## 180. DCREQ083 — Schema migration discipline

Schema changes require:

```text
migration
clean DB proof
supported upgrade or documented fresh-baseline policy
pending-model check
runtime startup proof
```

---

## 181. DCREQ084 — Hierarchy migration

Page/Block hierarchy changes preserve:

```text
identity
scope
acyclicity
parent semantics
```

or explicitly repair invalid legacy data.

---

## 182. DCREQ085 — Ordering migration

Order representation changes preserve visible semantic order.

Duplicate/malformed legacy keys need explicit repair policy.

---

## 183. DCREQ086 — Content/schema migration

Block content/properties evolution must keep historical valid data readable or migrate deterministically.

---

## 184. DCREQ087 — Collaboration target migration

Target representation changes preserve:

```text
resource kind
resource ID
tenant scope
historical Collaboration references
```

---

## 185. DCREQ194 — Snapshot/template migration compatibility

Changes to Block/Page content schema must review compatibility for:

```text
DocumentSnapshot
DocumentVersion
PageTemplate
```

not only live Blocks.

---

# Failure semantics

## 186. DCREQ088 — Stable failure classes

Released API behavior distinguishes canonical:

```text
validation
authentication
authorization
not found/concealed
conflict
unsupported content/target
dependency failure
```

---

## 187. DCREQ089 — Persistence failure is rollback-safe

Failure must not cause:

```text
false success
partial durable state
stale tracked mutation later flushed
false integration event
```

---

## 188. DCREQ090 — Cross-context dependency failure fails closed

Target/resource authorization/fact dependency failure must not become permissive fallback.

---

## 189. DCREQ091 — Messaging failure does not undo committed producer fact

Post-commit delivery failure is observable/retryable/poison-handled according to Platform contract.

---

## 190. DCREQ092 — Realtime failure does not undo committed producer fact

Realtime publication failure does not roll back valid business state.

Recovery uses authoritative state.

---

# Observability and performance

## 191. DCREQ093 — Safe operational correlation

Use safe stable IDs such as:

```text
PageId
BlockId
CommentId
TargetRef
MessageId
CorrelationId
```

without logging raw content by default.

---

## 192. DCREQ094 — Failure observability

Operational diagnostics distinguish important categories:

```text
authorization
tenant mismatch
hierarchy conflict
ordering conflict
content/version failure
target failure
persistence failure
messaging failure
recovery failure
```

---

## 193. DCREQ095 — Realtime recovery observability

Released recovery paths should expose:

```text
gap detected
recovery started
recovery succeeded/failed
duplicate suppressed
stale ignored
```

without unsafe high-cardinality content labels.

---

## 194. DCREQ096 — Page hierarchy query performance

Hierarchy traversal must remain bounded/measured for representative product scale.

---

## 195. DCREQ097 — Block loading performance

Large Page loading must avoid obvious:

```text
N+1
unbounded recursive materialization
unnecessary full-content loading
```

---

## 196. DCREQ098 — Collaboration query performance

Comment/Activity/Notification queries must be paged/bounded and avoid avoidable per-item authorization/actor lookups.

---

## 197. DCREQ099 — Mutation hot paths

High-frequency Block/comment/reaction/read-state operations must not perform whole-collection rewrites unless measured constraints justify it.

---

# Reliability

## 198. DCREQ110 — Idempotent retry where required

Each retry-prone mutation is explicitly classified:

```text
idempotency required
natural uniqueness sufficient
idempotency not required
```

Do not apply generic idempotency mechanically.

---

## 199. DCREQ111 — Consumer deduplication

Durable consumers with side effects must deduplicate by logical message identity where Platform contract requires it.

---

## 200. DCREQ112 — Poison isolation

A terminally failing message must not indefinitely block unrelated valid messages according to Platform poison/DLQ policy.

---

# Architectural non-goals

## 201. DCREQ100 — No implicit CRDT/OT

This workstream does not authorize a new CRDT/OT/offline-first subsystem.

Such architecture requires explicit design/ADR.

---

## 202. DCREQ101 — No generic CMS ownership abstraction

Documents remains the semantic owner of Page/Block knowledge state.

Do not replace it with a generic untyped CMS model.

---

## 203. DCREQ102 — No arbitrary polymorphic foreign-entity repository

`ResourceRef` is a semantic reference, not permission to build:

```text
generic table lookup
generic foreign DbContext
reflection-based aggregate mutation
```

---

## 204. DCREQ103 — No combined DocumentsCollaboration aggregate

Screen composition does not justify merging:

```text
Page
Comment
Reaction
Watcher
```

into one persistence/aggregate boundary.

---

# Brownfield source requirements

## 205. DCREQ104 — Brownfield inventory is mandatory

Before implementation, PLAN must inventory current:

```text
Domain
Application
Infrastructure
API
tests
migrations
events/consumers
cross-context adapters
```

and classify each relevant area.

---

## 206. DCREQ105 — Stub source is not capability proof

Examples such as:

```text
NotImplementedException
constant empty query
stub/no-op consumer
unmapped command
```

must be explicitly classified.

No certification from source presence.

---

## 207. DCREQ106 — Existing tests require semantic review

A green historical test must be checked for:

```text
current requirement
production DI
real DB where required
negative path
committed state
message identity
```

before reuse as certification evidence.

---

## 208. DCREQ195 — Known source debt must appear in PLAN

At minimum PLAN must disposition:

```text
MovePage NotImplemented
PublishPage NotImplemented
SetPageDeadline NotImplemented
GetPageHistory empty
UpdatePage icon/cover contract drift
PageVisibility incomplete vertical slice
Documents permission-action mismatches
ResourceLinks vertical-slice gap
Versions/Snapshots vertical-slice gap
Templates vertical-slice gap

Activity empty query
Mention lifecycle incompleteness
Reactions vertical-slice gap
Presence vertical-slice gap
ReadState vertical-slice gap
Watchers vertical-slice gap
Notification ownership/source gap
Comment reply/anchor/status partial exposure
Comment permission-action mismatches
Attachment target-scope ambiguity
```

A known item may be:

```text
IMPLEMENT
HARDEN
RETIRE
OUT_OF_RELEASE_SCOPE
BLOCKED-DECISION
```

but not omitted.

---

## 209. DCREQ196 — Placeholder commands require semantic decision before implementation

`PublishPage` and `SetPageDeadline` currently exist in Application source while corresponding canonical Documents product semantics are not established by the inspected authority.

Therefore PLAN must not implement them merely because code files exist.

They require:

```text
product match
or RETIRE
or BLOCKED-DECISION
```

---

## 210. DCREQ197 — API fields may not be silently ignored

If a public request accepts a field, the candidate must:

```text
apply it
validate/reject it
or remove it through compatible API change
```

Silent discard is forbidden for release-scoped contracts.

This directly applies to current Page update icon/cover request drift.

---

## 211. DCREQ198 — Empty product query stubs cannot certify

A handler returning a hard-coded empty successful result for a product capability such as:

```text
Page history
Activity
```

must be treated as incomplete unless product explicitly defines empty-only behavior.

---

## 212. DCREQ199 — Domain-rich/Application-thin capability must be classified

A Domain/persistence capability with no complete Application/API surface must be classified as:

```text
release capability
internal foundation
future/backlog
legacy to retire
```

before full-scope certification.

This applies to multiple Documents/Collaboration areas on current source.

---

## 213. DCREQ200 — Full workstream scope follows product authority, not team summary alone

The narrower `docs/workstreams/teams/documents-collaboration.md` capability list does not erase product-owned capabilities defined by:

```text
docs/product/documents.md
docs/product/collaboration.md
```

The execution package must either implement/certify or explicitly scope/disposition every canonical owned capability.

Canonical scope includes both:

```text
numbered PROD-DCT-* / PROD-COL-* requirements
+
normative unnumbered Product semantics captured by DCREQ201..220
```

---


# Canonical unnumbered Product semantics closure

## 213A. DCREQ201 — Page path is derived state

A Page path is derived from current:

```text
Page hierarchy
+
Page names/titles
```

It is not a second durable identity or competing ownership source.

Path changes caused by rename/reparent must not create a new Page identity.

---

## 213B. DCREQ202 — Duplicate Page creates new identities

If Page duplication is release-scoped:

```text
source Page
→ new Page ID

source Blocks
→ new Block IDs
```

Duplication must explicitly define handling for:

```text
ResourceLinks
attachments/file references
history
comments/anchors
templates
```

No duplicated Page may accidentally retain source aggregate identity.

If duplication is not released, PLAN must mark it `OUT_OF_RELEASE_SCOPE`.

---

## 213C. DCREQ203 — Documents provider integration is a translation boundary

External provider/import schemas must be translated through approved Documents operations/contracts.

Provider-specific schema does not become canonical:

```text
Page schema
Block schema
BlockType schema
```

without an explicit product/schema change.

---

## 213D. DCREQ204 — Documents retention includes derived and historical copies

Retention/privacy disposition must account for release-scoped:

```text
current Page/Block content
DocumentVersion/DocumentSnapshot
search/index projections
exports
backups/restore media
file/object references
```

Deleting current content does not prove historical/derived copies were handled.

Cross-system purge remains an explicit workflow.

---

## 213E. DCREQ205 — Reduced mobile editor capability cannot corrupt unsupported content

If a mobile/reduced editor is released, unsupported Block Types or properties must be:

```text
preserved
rendered read-only
or rejected safely
```

They must not be silently rewritten or dropped merely because a client lacks editing support.

Backend contracts must remain forward-compatible with such clients.

---

## 213F. DCREQ206 — Offline editing is conditional and protocol-owned

Offline editing is not implied by optimistic UI or realtime.

If released, it requires explicit:

```text
operation identity
reconciliation
conflict handling
authorization recheck
deleted/moved target handling
retry/idempotency
```

Without an accepted protocol:

```text
offline mutation = OUT_OF_RELEASE_SCOPE
```

and no CRDT/OT mechanism is inferred.

---

## 213G. DCREQ207 — Block semantics preserve accessibility meaning

Canonical Block types should preserve semantic information required by clients for accessible rendering, such as applicable:

```text
heading level
list semantics
todo state
code semantics/language metadata
image alternative text
focus/navigation-safe structure
```

Backend contracts must not collapse typed semantic content into opaque presentation-only HTML.

Frontend accessibility implementation remains frontend-owned.

---

## 213H. DCREQ208 — Attachment upload lifecycle is explicit where product-visible

If upload lifecycle is visible to product behavior, accepted states must be explicit, for example:

```text
pending
uploaded
failed
removed
```

A signed upload URL or object-provider response is not durable Attachment truth.

The product must not present an Attachment as durably available before accepted metadata/object state is committed.

---

## 213I. DCREQ209 — Cursor/typing state is ephemeral

Cursor/typing indicators, if released, are ephemeral collaboration-awareness signals.

They are not:

```text
Comment history
Presence authorization
Activity/Audit history
durable collaboration truth
```

Loss/reconnect may rebuild or discard them according to ephemeral-state policy.

---

## 213J. DCREQ210 — Notification delivery-channel state is separate

Notification product state must remain distinct from channel/provider delivery attempts.

Examples of delivery state:

```text
email attempt
push attempt
websocket delivery
provider retry
```

must not overwrite recipient read/dismiss truth.

---

## 213K. DCREQ211 — Notification preferences are explicit product inputs

If Notification preferences are release-scoped, they must define:

```text
user
optional Workspace/Account scope
notification type
channel
enabled/disabled state
delivery mode
digest interval where applicable
quiet hours/timezone where applicable
```

Preferences cannot weaken mandatory security delivery when higher policy requires delivery.

Current persisted preference records are evidence, not by themselves a complete Application/API contract.

---

## 213L. DCREQ212 — Notification attention lifecycle is recipient-owned state

If durable Notification UI is release-scoped, recipient attention state must define applicable operations such as:

```text
seen
read
archive
dismiss
```

These operations:

```text
do not mutate the source resource
do not grant source-resource access
do not change provider-delivery truth
```

Permission is rechecked when following/deep-linking to protected source resources.

---

## 213M. DCREQ213 — Workspace deletion has explicit Collaboration retention workflow

Workspace archive/delete must explicitly define, for released Collaboration state:

```text
retain
hide
export
anonymize
purge
or delayed purge
```

for applicable:

```text
Comments
Mentions
Reactions
Attachments
ReadState
Watchers
Notifications
Activity
Presence
```

This behavior cannot be derived from database cascade alone.

---

## 213N. DCREQ214 — Thread deletion preserves coherent reply semantics

Deleting/tombstoning a parent Comment must define child/reply behavior.

Allowed behavior must be explicit, for example:

```text
retain tombstoned parent
retain replies with historical parent marker
delete subtree under explicit policy
```

No dangling reply may silently become a different target/thread.

---

## 213O. DCREQ215 — Comment edit history is Collaboration-owned when supported

If Comment edit history is released, it is purpose-specific Collaboration history.

It is distinct from:

```text
Documents history
Activity
Notification history
Governance Audit
```

If edit history is not released:

```text
NOT_APPLICABLE / OUT_OF_RELEASE_SCOPE
```

must be recorded rather than inventing a generic event-log implementation.

---

## 213P. DCREQ216 — Collaboration abuse controls are explicit where exposure requires them

For public/guest/high-abuse surfaces, release scope must classify applicable:

```text
rate limiting
content limits
moderation
spam/abuse controls
attachment constraints
```

Absence of public/guest exposure may make this `NOT_APPLICABLE`.

Security policy, not convenience, decides the classification.

---

## 213Q. DCREQ217 — Collaboration search is derived and authorization-scoped

If Comment/Activity search is released:

```text
search index/result
!=
Collaboration truth
```

Every returned result must respect current:

```text
tenant scope
target access
resource lifecycle
permission revocation
```

Index lag/failure cannot become mutation authority.

---

## 213R. DCREQ218 — Collaboration provider integration uses approved operations

External comment/message/provider synchronization must map:

```text
external identity
external content
external resource reference
```

into approved Collaboration operations/contracts.

Provider payloads must not bypass:

```text
target validation
authorization
content safety
idempotency
tenant scope
```

Provider schema does not become canonical Collaboration schema.

---

## 213S. DCREQ219 — Optimistic Collaboration state reconciles to authority

Optimistic client state for released:

```text
Comment
Reaction
ReadState
Attachment
Notification attention
```

must reconcile to authoritative server identity/state.

Temporary/client identities must not become durable cross-context identifiers.

Rejected or stale mutations must converge safely.

This requirement is Collaboration-specific and is not satisfied solely by Documents editor reconciliation requirements.

---

## 213T. DCREQ220 — Page deletion is an orchestrated lifecycle workflow

Page deletion must explicitly coordinate Documents-owned effects such as applicable:

```text
Block lifecycle
ResourceLink lifecycle
history/version retention
search/index removal
file/reference retention
document events
```

and emit/hand off facts needed for cross-context reactions such as Collaboration.

ORM cascade is an implementation detail and cannot define product deletion semantics.

`DCREQ189` separately governs Collaboration's reaction to target deletion.

---


# Authorization/security closure

## 214. DCREQ107 — Authorization bypass inventory

All release-scoped protected handlers/endpoints must be inventoried.

Any direct role/permission checks are classified:

```text
canonical authorization
business invariant
legacy compatibility
unsafe bypass
```

No wildcard exception.

---

## 215. DCREQ108 — Cross-tenant negative matrix

At minimum verify representative:

```text
Page read/move
Block create/move
ResourceLink
history
Comment create/list/edit/delete
Mention
Reaction
Attachment
Watcher/ReadState
realtime subscription/recovery
```

for tenant A vs tenant B when capability is released.

---

## 216. DCREQ109 — Target existence disclosure

Forbidden vs missing behavior must be consistent with security policy across:

```text
Page
Block
Comment
target lookup
search/list
attachment
history
```

---

# Source-specific capability constraints

## 217. Page command surface

Current Page Application source includes:

```text
CreatePage
UpdatePage
ArchivePage
DeletePage
MovePage
PublishPage
SetPageDeadline
```

This SPEC accepts only product-backed semantics.

Source file existence does not make `PublishPage` or `SetPageDeadline` canonical.

---

## 218. Page query surface

Current source includes:

```text
GetPage
GetPageBreadcrumb
GetPageHistory
GetPageTree
GetWorkspacePages
SearchPages
```

Each release-scoped query must obey:

```text
authorization parity
tenant scope
bounded loading
real product semantics
```

---

## 219. Block command surface

Current source includes:

```text
CreateBlock
UpdateBlock
DeleteBlock
ReorderBlocks
BatchUpdateBlocks
```

PLAN must determine which current operations represent:

```text
content update
properties update
parent move
order move
batch concurrency
```

and ensure Domain invariants remain authoritative.

---

## 220. Collaboration Comment surface

Current source/API includes at least:

```text
Create
Update
Delete
Resolve
GetComments
```

Domain additionally supports:

```text
CreateReply
Reopen
Restore
Anchor
```

The vertical-slice gap must be dispositioned explicitly.

---

## 221. Attachment surface

Current API/Application is BoardItem-centric while Domain target identity is generic.

The release must explicitly define supported Attachment target kinds.

Do not infer Page/Comment attachment support merely from generic Domain structure.

---

# P4B readiness

## 222. P4B entry gate

Broad P4B implementation requires stable upstream:

```text
Actor/Account D5
Workspace D5
Governance/resource authorization D5
```

Specific target integrations additionally require their target-owner public contracts.

---

## 223. Documents core D4 gate

At minimum release-scoped Documents core requires executable proof for:

```text
Page identity/lifecycle
Page metadata/visibility where exposed
Page hierarchy/concurrency
Block identity/content
Block hierarchy
ordering
authorization
tenant isolation
persistence roundtrip
```

---

## 224. Documents full D4 gate

Full release-scoped Documents adds applicable:

```text
ResourceLinks
Versions/Snapshots/history
Templates
search boundary
import/export/file references
events
migration compatibility
```

---

## 225. Documents D5 gate

Documents D5 additionally requires:

```text
stable public/API/event contracts
no unresolved critical ownership/security debt
no placeholder/stub in certified capability
downstream consumers can depend without private persistence
exact-SHA evidence
```

---

## 226. Collaboration core D4 gate

At minimum:

```text
Comment/reply/anchor/status
target contract
authorization
tenant isolation
retention
query
```

for released scope.

---

## 227. Collaboration full D4 gate

Full release-scoped Collaboration additionally requires applicable:

```text
Mention
Reaction
Attachment
Presence
ReadState
Watcher
Notification
Activity
realtime
```

---

## 228. Collaboration D5 gate

Collaboration D5 requires:

```text
stable released target contracts
stable attention/collaboration semantics
no foreign private persistence
retention/privacy explicit
no certified capability represented only by Domain/persistence stub
exact-SHA evidence
```

---

## 229. Realtime D4+ gate

A released realtime surface requires proof for:

```text
duplicate
out-of-order
gap
reconnect
permission revoke
authoritative recovery
final convergence
```

for the state type being claimed.

---

## 230. Target-specific D5 gate

Each released target kind is certified independently.

Examples:

```text
Page
Block if supported
Board
BoardItem
```

One stable target does not certify another.

---

# Evidence families

## 231. Required evidence families

Certification must be able to collect:

```text
DC-EVID-01 source inventory/disposition
DC-EVID-02 Page lifecycle/metadata/visibility
DC-EVID-03 Page hierarchy/concurrency
DC-EVID-04 Block type/content/properties
DC-EVID-05 Block hierarchy
DC-EVID-06 ordering
DC-EVID-07 ResourceLinks
DC-EVID-08 Versions/Snapshots/history
DC-EVID-09 Templates
DC-EVID-10 Documents auth/tenant
DC-EVID-11 Comment/reply/anchor/status
DC-EVID-12 targets
DC-EVID-13 Mentions
DC-EVID-14 Reactions
DC-EVID-15 Attachments
DC-EVID-16 Presence
DC-EVID-17 ReadState/Watchers
DC-EVID-18 Notification/Activity
DC-EVID-19 Collaboration auth/tenant/retention
DC-EVID-20 API/query contracts
DC-EVID-21 events/messaging
DC-EVID-22 realtime/recovery
DC-EVID-23 migrations/compatibility
DC-EVID-24 reliability
DC-EVID-25 security/privacy
DC-EVID-26 performance/observability
DC-EVID-27 cross-team integration
DC-EVID-28 exact-SHA CI
```

---

## 232. Candidate exact-SHA rule

Final evidence refers to the exact source candidate SHA.

No combined certification from mismatched:

```text
source
migration
generated OpenAPI
event manifest
local working tree
remote CI
```

---

## 233. No prefilled success

SPEC/PLAN/TESTS must not contain future:

```text
PASS
VERIFIED
STABLE
CERTIFIED
```

claims.

Those belong only to CERTIFICATION after execution.

---

# Stop conditions

## 234. DCSTOP001 — Context ownership ambiguity

Stop when state ownership between Documents/Collaboration/foreign context is unclear.

---

## 235. DCSTOP002 — Foreign private persistence required

Stop if normal implementation requires direct foreign private:

```text
DbContext
table
EF entity
repository
```

instead of an approved contract.

---

## 236. DCSTOP003 — Cross-Workspace transfer semantics unresolved

Stop if implementation attempts to change Workspace/Account as ordinary Page/Block move.

---

## 237. DCSTOP004 — Page hierarchy concurrency unresolved

Stop release of Page movement if concurrent operations can commit an invalid hierarchy.

---

## 238. DCSTOP005 — Block hierarchy/order cannot preserve invariant

Stop if parent/order mechanism can create:

```text
cycle
cross-Page parent
ambiguous order
```

without deterministic conflict/repair.

---

## 239. DCSTOP006 — Block content/history compatibility unresolved

Stop if new writer makes valid:

```text
live Block
snapshot
version
template
```

content unreadable.

---

## 240. DCSTOP007 — Authorization bypass required

Stop if a feature only works by disabling/bypassing canonical authorization or tenant isolation.

---

## 241. DCSTOP008 — Collaboration target semantics unresolved

Stop a target kind when owner/scope/lifecycle/auth/retention are undefined.

---

## 242. DCSTOP009 — Realtime recovery undefined

Stop realtime certification when missed-event recovery cannot restore authoritative state.

---

## 243. DCSTOP010 — CRDT/OT required but unauthorized

Stop if true collaborative editing requires new merge architecture not approved by product/ADR.

---

## 244. DCSTOP011 — Retention/privacy unresolved

Stop destructive lifecycle changes when retention/privacy outcome is undefined.

---

## 245. DCSTOP012 — Migration cannot preserve integrity

Stop migration when legacy data cannot map deterministically to:

```text
tenant
target
hierarchy
order
content version
```

without approved repair policy.

---

## 246. DCSTOP013 — Critical suite executes zero work

A green process that selected zero relevant tests is not evidence.

---

## 247. DCSTOP014 — Source placeholder semantics unresolved

Do not implement a placeholder merely because it exists.

This applies especially to current:

```text
PublishPage
SetPageDeadline
```

until product semantics are confirmed.

---

## 248. DCSTOP015 — Notification/Activity ownership implementation unresolved

If source architecture cannot express canonical user-facing Notification/Activity ownership without violating context boundaries, stop and resolve ownership rather than moving them silently to another context.

---

## 248A. DCSTOP016 — Block content becomes schema-less

Stop if a released Block Type accepts arbitrary unvalidated JSON such that typed semantic compatibility cannot be proven.

---

## 248B. DCSTOP017 — Page sharing leaks linked/private target access

Stop if Page visibility/sharing can expose a ResourceLink/embed target without that target's current authorization.

---

## 248C. DCSTOP018 — History version and concurrency version are conflated

Stop if one counter/record is being used interchangeably as:

```text
aggregate optimistic-concurrency token
and
user-facing document history identity
```

without an explicit accepted model.

---

## 248D. DCSTOP019 — Template mutation rewrites existing Pages implicitly

Stop if changing a PageTemplate silently mutates already-instantiated Pages without an explicit migration/product workflow.

---

## 248E. DCSTOP020 — Mention identity is not stable

Stop Mention release if the mentioned identity cannot be resolved to an authoritative supported principal/user identity and scope.

---

## 248F. DCSTOP021 — Reaction retry can duplicate logical reaction/count

Stop Reaction release if duplicate/retried concurrent requests can create multiple logical reactions or irreconcilable counts.

---

## 248G. DCSTOP022 — Unread count becomes unrecoverable truth

Stop ReadState/Notification release if an increment/decrement counter is the only truth and cannot reconcile from a stable read/message boundary.

---

## 248H. DCSTOP023 — Notification semantics collapse into delivery transport

Stop if:

```text
recipient is absent
provider delivery becomes Notification truth
provider retry creates duplicate logical Notification
read/dismiss is conflated with provider delivery
```

---

## 248I. DCSTOP024 — Activity is used as Governance Audit

Stop if user-facing Activity is used as security/governance evidence or if Audit retention/integrity semantics are silently transferred into Activity.

---

## 248J. DCSTOP025 — Workspace deletion Collaboration policy is unresolved

Stop destructive Workspace lifecycle integration when Collaboration retention/export/anonymization/purge behavior is undefined.

---

## 248K. DCSTOP026 — Required public/guest abuse policy is unresolved

Stop release of public/guest Collaboration surfaces when applicable abuse/rate/content controls are not classified.

---


# Definition of Done

## 249. Documents Core Done

Documents core is DONE only when:

```text
Page
Page metadata/visibility where released
Page hierarchy
Block
Block hierarchy
Block content/properties
ordering
query/API
authorization
tenant isolation
```

satisfy release requirements with executable evidence.

---

## 250. Documents Full Scope Done

Full release-scoped Documents additionally requires applicable:

```text
ResourceLinks
Versions/Snapshots/history/restore
Templates
search boundary
import/export
file/media references
Page path semantics
Page duplication
Integrations translation boundary
retention across historical/derived copies
mobile/offline compatibility classification
accessibility-preserving content contract
events
realtime
migration/security/performance
```

with no omitted product-owned capability hidden behind absent API.

Capabilities not included in the candidate release must still have an explicit release disposition.

---

## 251. Collaboration Core Done

Collaboration core is DONE only when release-scoped:

```text
Comment
reply/thread
anchor
status
target contract
authorization
retention
query
tenant isolation
```

are verified.

---

## 252. Collaboration Full Scope Done

Full release-scoped Collaboration additionally requires applicable:

```text
Mention
Reaction
Attachment + upload lifecycle
Presence + cursor/typing classification
ReadState
Watcher
Notification recipient/channel/preferences/read-dismiss lifecycle
Activity
Collaboration search
Workspace/thread/edit-history retention behavior
abuse-control classification
Integrations translation boundary
optimistic reconciliation
realtime
```

with explicit dispositions for every canonical product-owned area.

---

## 253. Realtime Done

Realtime is DONE only for the specific resource/state types whose:

```text
duplicate
reorder
gap
recovery
revocation
convergence
```

are proven.

---

## 254. Full workstream Done

`DOCUMENTS & COLLABORATION FULL SCOPE CERTIFIED` requires:

```text
Documents release scope D5
+
Collaboration release scope D5
+
required target adapters D5
+
authorization/tenant isolation D5
+
required events/handoffs stable
+
released realtime D4+
+
migration/reliability/security/performance closure
+
exact candidate SHA CI
```

---

# Extraction readiness

## 255. Documents extraction readiness

Future extraction requires proof of:

```text
private Page/Block/Link/History/Template data
stable Workspace/Governance contracts
stable Collaboration target contract
stable event contract
no hidden DB coupling
```

---

## 256. Collaboration extraction readiness

Future extraction requires:

```text
independent collaboration data
stable target contracts
stable recipient/notification semantics
realtime/delivery boundaries
retention
no target private DB access
```

---

## 257. Team boundary is not service boundary

A single team owning both contexts is not justification to merge them into one future service.

Extraction decisions remain architecture decisions.

---

# Final invariants

## 258. Documents invariant

At any point, the system must be able to answer:

```text
Which Workspace owns this Page?
Which Page owns this Block?
Which parent owns this Block position?
Which Block Type schema validates this content?
Which history/snapshot represents this point in time?
Which ResourceLink is only a relationship rather than foreign ownership?
Which authorization protects this operation?
```

without guessing from UI state or foreign persistence.

---

## 259. Collaboration invariant

At any point, the system must be able to answer:

```text
What resource is being collaborated on?
Who owns that resource?
What tenant scope does it belong to?
Who authored/received/reacted/watched/read?
Is the state durable attention, durable discussion, or ephemeral presence?
What current permission allows this view/mutation?
What happens if the target or actor disappears?
```

without making Collaboration the owner of the target.

---

## 260. Cross-context invariant

Cross-context integration must preserve:

```text
one owner per business truth
explicit contract
minimum required facts
stable identity
failure isolation
no private persistence coupling
```

---

## 261. Final specification rule

Before implementation or certification, ask:

```text
Have all canonical Documents product capabilities been either
implemented/certified or explicitly dispositioned?

Have all canonical Collaboration product capabilities been either
implemented/certified or explicitly dispositioned?

Does every current source placeholder/stub have a deliberate outcome?

Can a downstream consumer use public contracts without learning private persistence?

Can a permission revocation, retry, migration, reconnect, or target deletion
occur without corrupting or leaking authoritative state?
```

If the answer is no for a release-required capability:

```text
the workstream is not complete
```

This rule overrides any generic "DONE" label.
