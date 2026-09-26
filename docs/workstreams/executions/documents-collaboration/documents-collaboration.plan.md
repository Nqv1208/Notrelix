---
document_id: WRK-PLAN-DOCUMENTS-COLLABORATION
document_type: workstream-plan
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
  - block-ordering
  - resource-links
  - document-versions
  - document-snapshots
  - document-history
  - document-templates
  - document-search
  - import-export
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
  - authorization
  - tenant-isolation
  - events
  - messaging
  - realtime
  - migrations
  - reliability
  - security
  - performance
  - p4b
evidence:
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/product/documents.md
  - docs/product/collaboration.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
review_on:
  - documents-collaboration-spec-change
  - source-debt-ledger-change
  - release-scope-change
  - page-contract-change
  - page-visibility-change
  - page-hierarchy-change
  - block-contract-change
  - block-hierarchy-change
  - block-ordering-change
  - resource-link-change
  - version-history-change
  - template-change
  - comment-contract-change
  - mention-change
  - reaction-change
  - attachment-change
  - presence-change
  - read-state-change
  - watcher-change
  - notification-change
  - activity-change
  - target-contract-change
  - authorization-contract-change
  - realtime-recovery-change
  - migration-change
  - p4b-gate-change
---

# PLAN — Documents & Collaboration

## 1. Purpose

This PLAN converts the full canonical `documents-collaboration.spec.md` into an implementation sequence for the current brownfield backend.

It is the master **HOW / ORDER** document.

It answers:

```text
What source must be inventoried first?
Which current code is canonical?
Which source is incomplete/stale?
Which product capabilities are release-scoped?
Which capabilities are internal foundations only?
Which source must be retired rather than implemented?
Which decisions block execution?
Which vertical slices must be completed?
Which requirements can run in parallel?
Which migrations/contracts/events are affected?
Which exact evidence is handed to TESTS/CERTIFICATION?
```

This PLAN MUST NOT invent semantics that the SPEC leaves unresolved.

---

## 2. Execution baseline

The initial audited source baseline is:

```text
branch: develop
SHA:    35702d0fa9fb01ed68b0667bab500030d60bd028
```

This SHA is the source baseline for this execution artifact.

Actual implementation/certification must recapture the exact candidate SHA before work and before final evidence.

---

## 3. Brownfield execution model

The workstream is not greenfield.

Execution follows:

```text
authority
→ exact source inventory
→ source-debt classification
→ release-scope classification
→ preserve valid implementation
→ harden/complete missing vertical slices
→ migration/compatibility
→ executable verification
→ certification
```

Never:

```text
SPEC
→ rewrite everything into an idealized design
```

Each source area receives one disposition:

```text
KEEP
HARDEN
REFACTOR
MIGRATE
RETIRE
BLOCKED-DECISION
BLOCKED-UPSTREAM
OUT_OF_RELEASE_SCOPE
```

---

## 4. Mandatory preservation rule

Existing correct source is an asset.

A source change is justified only by:

```text
a violated SPEC requirement
a missing release-scoped vertical slice
a source-debt ledger entry
a failing verification gate
a migration/compatibility need
```

Do not rewrite because another pattern appears cleaner.

---

## 5. Canonical scope rule

The execution scope comes from:

```text
docs/product/documents.md
docs/product/collaboration.md
```

not solely from the narrower team workstream capability list.

Therefore every canonical product-owned capability must receive:

```text
release implementation
or explicit release exclusion
or explicit internal-foundation status
or retirement
or blocker
```

No owned capability disappears from the plan.

---


## 5A. Product/team requirement namespace rule

This PLAN inherits the SPEC namespace normalization:

```text
PROD-PROD-DCT-001..031
→ docs/product/documents.md

PROD-PROD-COL-001..029
→ docs/product/collaboration.md

TEAM-DCT-001..012
TEAM-COL-001..008
→ docs/workstreams/teams/documents-collaboration.md capability aliases
```

Bare `DCT-*` / `COL-*` identifiers are not normative in this PLAN when the source document is ambiguous.

Implementation traceability uses:

```text
DCREQ*
DC-* work units
DC-TST-* verification IDs
DC-EVID-* evidence bundles
```

and Product aliases only when Product-level provenance is needed.

---

# Current source topology

## 6. Current Documents vertical-slice shape

At the audited baseline:

```text
Documents Application: 36 C# files
Documents API:         18 C# files
Documents-specific Infrastructure: 7 C# files
```

Application/API are concentrated in:

```text
Pages
Blocks
Page authorization facts
```

while Domain/persistence also contain:

```text
ResourceLinks
DocumentVersions
DocumentSnapshots
PageTemplates
```

This asymmetry is a mandatory execution concern.

---

## 7. Current Collaboration vertical-slice shape

At the audited baseline:

```text
Collaboration Application: 20 C# files
Collaboration API:         12 C# files
Collaboration-specific Infrastructure/CrossContext: 16 C# files
```

Application/API are concentrated in:

```text
Comments
Attachments
Activity query
ResourceSummary public read boundary
```

while Domain/persistence additionally contain:

```text
Mentions
Reactions
Presence
ReadStates
Watchers
```

The plan must classify each missing vertical slice.

---

# Master source-debt ledger

## 8. Source-debt ledger rule

The ledger is mandatory.

A ledger item may not disappear merely because the implementation changes.

It exits only with one recorded disposition:

```text
RESOLVED-IMPLEMENTED
RESOLVED-HARDENED
RESOLVED-RETIRED
RESOLVED-OUT-OF-SCOPE
BLOCKED-DECISION
BLOCKED-UPSTREAM
```

---

## 9. DC-SRC-DOC-001 — MovePage handler missing

Source:

```text
backend/src/Notrelix.Application/Features/Documents/Pages/Commands/MovePage/MovePage.cs
```

Observed baseline:

```text
Handle(...)
→ throw new NotImplementedException()
```

SPEC:

```text
DCREQ006–010
DCREQ034
DCREQ105
DCREQ195
```

Required disposition:

```text
IMPLEMENT/HARDEN
```

unless product authority removes Page movement, which current product authority does not.

This is a release blocker for Page hierarchy movement.

---

## 10. DC-SRC-DOC-002 — PublishPage command unresolved

Source:

```text
.../Pages/Commands/PublishPage/PublishPage.cs
```

Observed:

```text
NotImplementedException
```

Canonical product Page lifecycle does not currently establish generic Page `Published` state equivalent to PageTemplate publishing.

Required execution:

```text
classify source semantic
→ if stale/legacy: RETIRE
→ if accepted product capability: update authority then implement
→ otherwise BLOCKED-DECISION
```

Forbidden:

```text
implement solely because file exists
```

SPEC:

```text
DCREQ196
DCSTOP014
```

---

## 11. DC-SRC-DOC-003 — SetPageDeadline command unresolved

Source:

```text
.../Pages/Commands/SetPageDeadline/SetPageDeadline.cs
```

Observed:

```text
NotImplementedException
```

Canonical Documents product model inspected does not establish Page deadline semantics.

Required disposition:

```text
RETIRE
or product decision + implementation
or BLOCKED-DECISION
```

Do not guess.

---

## 12. DC-SRC-DOC-004 — Page history query is a stub

Source:

```text
.../Pages/Queries/GetPageHistory/GetPageHistory.cs
```

Observed:

```text
page exists check
→ Result.Success(new List<PageHistoryDto>())
```

Related existing Domain:

```text
DocumentVersion
DocumentSnapshot
DocumentVersionCreatedDomainEvent
DocumentVersionRestoredDomainEvent
```

Required execution:

```text
define history source
define version creation policy
define history DTO
define restore contract
implement query or retire misleading endpoint
```

SPEC:

```text
DCREQ128–134
DCREQ198
```

---

## 13. DC-SRC-DOC-005 — Page update contract discards fields

API request currently contains:

```text
Title
IconType
IconValue
CoverUrl
```

Endpoint/Application currently forwards only:

```text
Title
```

Required execution:

```text
reconcile canonical Page metadata
→ implement supported fields
or remove unsupported public fields compatibly
```

No silent discard.

SPEC:

```text
DCREQ113
DCREQ197
```

---

## 14. DC-SRC-DOC-006 — PageVisibility incomplete vertical slice

Domain has:

```text
Private
Workspace
Public
```

but full mutation/use-case/API contract is not evidenced in current Pages feature tree.

Required execution:

```text
classify release scope
define mutation
define resource/action
define event/history effect
define query DTO exposure
define authorization semantics
```

or explicitly keep visibility read-only/internal for current release.

SPEC:

```text
DCREQ114–116
```

---

## 15. DC-SRC-DOC-007 — Documents permission-action mismatches

Observed current mappings include:

```text
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

while canonical Documents actions already exist.

Required execution:

```text
build complete request/action matrix
→ compare against AccessPolicyEngine semantics
→ correct mismatches
→ add policy matrix tests
```

SPEC:

```text
DCREQ032–035
DCREQ107–109
DCREQ191
```

---

## 16. DC-SRC-DOC-008 — ResourceLinks lack full vertical slice

Current:

```text
Domain exists
EF configuration exists
DbSet exists
```

No corresponding complete Application/API family appears in audited tree.

Required classification:

```text
release-scoped → implement vertical slice
internal foundation → document why
out of current release → explicit backlog
legacy → retire
```

Full Documents scope cannot be certified without disposition.

---

## 17. DC-SRC-DOC-009 — Versions/Snapshots lack complete vertical slice

Current:

```text
DocumentVersion Domain
DocumentSnapshot Domain
EF mapping
GetPageHistory endpoint stub
```

Required:

```text
version creation policy
snapshot schema
history query
restore semantics
retention
compatibility
```

or explicit release exclusion.

---

## 18. DC-SRC-DOC-010 — Templates lack full vertical slice

Current:

```text
PageTemplate Domain
EF mapping
events
```

No complete Application/API family appears in audited tree.

Required disposition as above.

---

## 19. DC-SRC-COL-001 — Activity projection exists, resource query is unwired

Source:

```text
.../Collaboration/Activity/Queries/GetResourceActivity/GetResourceActivity.cs
```

Observed query behavior:

```text
data = []
total = 0
```

But current Infrastructure already contains Activity projection consumers such as:

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

Therefore the source debt is specifically:

```text
existing product-fact projection
+
missing/unwired Collaboration resource-activity query
```

Required execution:

```text
identify the canonical existing Activity projection/store
→ wire GetResourceActivity to that projection where product semantics match
→ preserve target authorization/filtering
→ do not create a competing second Activity truth/store
```

or explicitly retire/defer the resource-activity endpoint.

SPEC:

```text
DCREQ185
DCREQ186
DCREQ187
DCREQ188
DCREQ198
```

---

## 20. DC-SRC-COL-002 — Mention lifecycle incomplete

CreateComment already writes Mention rows.

Missing/unclear vertical behavior includes:

```text
Mentioned user validation
edit diff
removed mention
unchanged mention
notification dedup
privacy after revoke
```

Required:

```text
HARDEN
```

if mentions remain release-scoped with Comments.

---

## 21. DC-SRC-COL-003 — Reactions Domain/persistence without full vertical slice

Required classification:

```text
release
internal foundation
out of release
retire
```

If release-scoped, implement:

```text
create/remove
uniqueness
retry
query/count
authorization
target retention
realtime
```

---

## 22. DC-SRC-COL-004 — Presence Domain/persistence without full vertical slice

Required classification.

If release-scoped:

```text
join/update/leave/heartbeat
expiry
scope
auth
reconnect
ephemeral semantics
```

must be implemented without treating presence as durable truth.

---

## 23. DC-SRC-COL-005 — ReadState Domain/persistence without full vertical slice

Required classification.

If release-scoped:

```text
get/read boundary
mark read
monotonicity
unread reconciliation
auth
realtime
```

---

## 24. DC-SRC-COL-006 — Watchers Domain/persistence without full vertical slice

Required classification.

If release-scoped:

```text
watch
unwatch
automatic watch rule
idempotency
authorization
notification interaction
```

---

## 25. DC-SRC-COL-007 — Durable Notification state exists; semantic/Application ownership is incomplete

Canonical Product authority keeps user-facing Notification semantics under Collaboration.

The audited source already contains durable Infrastructure persistence:

```text
NotificationItemRecord
NotificationRecipientRecord
NotificationPreferenceRecord
NotificationCounterRecord
```

and runtime wiring:

```text
MentionCreatedNotificationConsumer
```

which persists Notification + recipient state from committed Mention facts with deduplication.

Current durable records already represent:

```text
recipient attention state:
  Unread
  Seen
  Read
  Archived
  Dismissed

preferences:
  notification type
  channel
  enabled state
  delivery mode
  digest interval
  quiet hours
  timezone
```

Therefore the unresolved issue is not whether durable Notification state exists.

Required execution:

```text
reconcile Infrastructure persistence/runtime state
with Collaboration-owned Notification product semantics
→ classify Application/API release surface
→ classify recipient read/dismiss operations
→ classify preference operations
→ classify channel/provider delivery state
→ preserve dedup/privacy/authorization
```

Do not create a second Notification store merely because the existing records are located under Infrastructure.

SPEC:

```text
DCREQ179
DCREQ180
DCREQ181
DCREQ182
DCREQ183
DCREQ184
DCREQ210
DCREQ211
DCREQ212
DCREQ200
```

---

## 26. DC-SRC-COL-008 — Comment Domain richer than exposed vertical slice

Domain supports:

```text
reply
anchor
resolve
reopen
delete
restore
```

API/Application expose only part.

Required execution:

```text
classify each capability
complete released ones
remove misleading unsupported routes/contracts
```

---

## 27. DC-SRC-COL-009 — Collaboration request/resource/action matrix requires reconciliation

Observed current mappings include:

```text
CreateComment              → CreateComment

GetComments                → ViewBoard
UpdateComment              → UpdateItem
DeleteComment              → UpdateItem
ResolveComment             → UpdateItem

GetBoardItemAttachments    → ViewBoard
CreateBoardItemAttachment  → UpdateItem
DeleteAttachment           → UpdateItem on collaboration.attachment

GetResourceActivity        → ViewBoard on generic ResourceKind
```

Some target-specific mappings may ultimately be valid.

None are accepted merely because they compile or execute.

Required:

```text
full Collaboration request/resource/action matrix
per-target semantics
author/moderator semantics
attachment semantics
Activity query semantics
future released Mention/Reaction/Presence/ReadState/Watcher/Notification semantics
AccessPolicyEngine tests
```

SPEC:

```text
DCREQ044
DCREQ045
DCREQ046
DCREQ107
DCREQ192
```

---

## 28. DC-SRC-COL-010 — Attachment target scope ambiguity

Domain Attachment uses generic `ResourceRef`.

Application/API are currently BoardItem-specific.

Required decision:

```text
BoardItem-only release
or approved additional target kinds
```

Do not infer Page/Comment attachment support from Domain genericity.

---

# Phase map

## 29. Master phase sequence

```text
Phase 0   Exact baseline / source inventory
Phase 1   Product scope + source-debt disposition
Phase 2   Ownership/boundary reconciliation
Phase 3   Upstream/Platform prerequisite verification

Phase 4   Page identity/lifecycle/metadata/visibility/path/duplicate/delete workflow
Phase 5   Page hierarchy/reparent/concurrency
Phase 6   Block identity/type/content/properties
Phase 7   Block hierarchy
Phase 8   Ordering/editor concurrency/no-op
Phase 9   Document query/API/search boundary
Phase 10  ResourceLinks
Phase 11  Versions/Snapshots/history/restore
Phase 12  Templates/import-export/file-reference + client/integration compatibility
Phase 13  Documents authorization/tenant closure

Phase 14  Comment/reply/anchor/status
Phase 15  Collaboration target contracts
Phase 16  Mentions
Phase 17  Reactions
Phase 18  Attachments/upload lifecycle
Phase 19  Presence/cursor/typing
Phase 20  ReadState
Phase 21  Watchers
Phase 22  Notification/Activity/preferences/attention/search
Phase 23  Collaboration authorization/tenant/retention/thread/workspace/abuse

Phase 24  Events/cross-context/provider-integration handoffs
Phase 25  Realtime/recovery/optimistic-offline reconciliation

Phase 26  Persistence/migrations/content compatibility
Phase 27  Reliability/idempotency/failure hardening
Phase 28  Security/privacy hardening
Phase 29  Performance/observability
Phase 30  Cross-team integration
Phase 31  TESTS handoff
Phase 32  Contract/docs handoff
Phase 33  CERTIFICATION handoff
```

---

# Phase 0 — Exact baseline and source inventory

## 30. DC-INV-001 — Capture candidate baseline

Record:

```text
branch
HEAD SHA
working tree
solution
migration head
database engine
current generated OpenAPI
current event manifest
relevant CI workflows
```

Do this before modifying source.

---

## 31. DC-INV-002 — Full Documents Domain inventory

Inventory:

```text
Pages
Blocks
ResourceLinks
Versions
Snapshots
Templates
Rules
events
value objects
enums
```

For every Domain type:

```text
type
owner
aggregate/entity/value object
identity
Account/Workspace scope
lifecycle
version/concurrency
event output
SPEC requirements
source disposition
```

---

## 32. DC-INV-003 — Full Collaboration Domain inventory

Inventory:

```text
Attachments
Comments
Mentions
Presence
Reactions
ReadStates
Watchers
Rules
events
```

Same classification.

---

## 33. DC-INV-004 — Full Application inventory

For Documents and Collaboration record:

```text
command/query
request descriptor
handler
validator
auth declaration
DbContext/port
transaction
event
API route
tests
status
```

Explicitly identify:

```text
NotImplementedException
constant empty result
Domain capability with no use case
API field with no Application mapping
```

---

## 34. DC-INV-005 — Full API inventory

Record:

```text
route
method
request
response
Application request
resource/action
tenant scope
error semantics
OpenAPI operation
```

Also identify Application commands not mapped by API.

---

## 35. DC-INV-006 — Infrastructure/persistence inventory

Record per capability:

```text
table/schema
EF config
indexes
constraints
FK/cascade
concurrency
RLS
outbox
cross-context adapter
migration history
```

---

## 36. DC-INV-007 — Existing tests inventory

Classify all existing tests:

```text
SUFFICIENT
PARTIAL
STALE
DUPLICATE
NOT_RELEVANT
```

Pay special attention to existing Domain tests for:

```text
Block hierarchy
Mention
Reaction
Presence
ReadState
Watcher
Attachment
Version/Snapshot
ResourceLink
Template
```

These prove some invariants but do not prove vertical-slice readiness.

---

## 37. DC-INV-008 — Event/consumer inventory

Inventory:

```text
producer event
integration event
manifest version
consumer
consumer maturity
stub/noop status
outbox
dedup
poison policy
```

Documents stub consumers must be classified, not counted as implemented consumers.

---

## 38. DC-INV-009 — Cross-context contract inventory

Inspect dependencies with:

```text
Identity
Workspace
Governance
WorkManagement
Automation
Analytics
Platform
```

Record:

```text
consumer
producer
contract
sync/async
readiness
private persistence?
failure semantics
```

---

## 39. DC-INV-010 — Request/action inventory

Generate one canonical table for every protected request:

```text
request
resource kind
resource ID source
current action
expected product action
status
```

This inventory is mandatory before authorization changes.

---

## 40. Phase 0 exit

Phase 0 exits only when every current product-owned source area is visible in one inventory.

Missing API/Application does not remove the Domain capability from inventory.

---

# Phase 1 — Product scope and source-debt disposition

## 41. DC-SCOPE-001 — Release-scope matrix

Create matrix:

```text
Capability
Canonical product owner
Current source maturity
Current public API?
Release required?
Disposition
Blocker
```

At minimum include all capabilities in SPEC sections.

---

## 42. DC-SCOPE-002 — Separate core milestone from full workstream

Define candidate target:

```text
P4B CORE
or
FULL DOCUMENTS & COLLABORATION
```

Do not call a core-only execution "full scope".

Core may omit explicitly deferred:

```text
Templates
advanced history
Presence
Watchers
Notifications
etc.
```

only if release authority allows and CERTIFICATION reports them as out of release scope.

---

## 43. DC-SCOPE-003 — PublishPage decision

Resolve `DC-SRC-DOC-002`.

Required output:

```text
RETAIN+IMPLEMENT
or RETIRE
or BLOCKED-DECISION
```

No coding before decision.

---

## 44. DC-SCOPE-004 — SetPageDeadline decision

Resolve `DC-SRC-DOC-003`.

Same rule.

---

## 45. DC-SCOPE-005 — Notification ownership/release decision

Resolve:

```text
durable Notification model
transport-only consumer
release scope
recipient/read/dismiss semantics
```

If no durable Notification product is release-scoped, explicitly mark current consumer behavior as transport/attention integration rather than pretending full Notification capability exists.

---

## 46. DC-SCOPE-006 — Domain-rich/Application-thin disposition

For each:

```text
ResourceLinks
Versions/Snapshots
Templates
Reactions
Presence
ReadState
Watchers
```

record one disposition.

This gate prevents silent omission.

---


## 46A. DC-SCOPE-007 — Normalize Product/team requirement namespaces

Covers the repository collision between Product `DCT/COL` IDs and team capability aliases.

Execution artifacts must use:

```text
PROD-DCT-*
PROD-COL-*
TEAM-DCT-*
TEAM-COL-*
```

according to the SPEC rule.

No implementation agent may resolve bare `DCT-001`/`COL-001` without a source document.

Evidence:

```text
documentation traceability gate
DC-TST-TRACE-001
```

---

## 46B. DC-SCOPE-008 — Classify canonical unnumbered Product semantics

Classify:

```text
DCREQ201..DCREQ220
```

as:

```text
RELEASED
OUT_OF_RELEASE_SCOPE
NOT_APPLICABLE
BLOCKED-DECISION
```

before full-scope certification.

This includes Product semantics that do not have dedicated `PROD-DCT-*` / `PROD-COL-*` IDs.

---

# Phase 2 — Ownership/boundary reconciliation

## 47. DC-OWN-001 — Documents ownership

Confirm:

```text
Page
Block
ResourceLink
DocumentVersion
DocumentSnapshot
PageTemplate
```

are Documents-owned.

---

## 48. DC-OWN-002 — Collaboration ownership

Confirm:

```text
Comment
Mention
Reaction
Attachment
Presence
ReadState
Watcher
Notification/Activity semantics
```

remain Collaboration-owned.

---

## 49. DC-OWN-003 — Target-owner direction

For each released target:

```text
documents.page
documents.block if supported
work-management.board
work-management.board-item
```

record:

```text
owner
fact contract
scope contract
lifecycle fact
authorization resource/action
retention
```

---

## 50. DC-OWN-004 — Activity/Audit split

Ensure:

```text
user Activity → Collaboration projection
security/governance Audit → Governance
document history → Documents
```

No generic history table becomes owner of all three.

---

## 51. DC-OWN-005 — Search ownership

Documents provides facts.

Search/index is derived.

No search index becomes write authority.

---

# Phase 3 — Upstream and Platform prerequisites

## 52. DC-UP-001 — Actor/Account/Workspace

Verify stable current contracts for:

```text
actor
Account
Workspace
```

No feature-local current-user/current-workspace mechanism.

---

## 53. DC-UP-002 — Governance authorization

Verify canonical:

```text
request descriptor
resource facts
AccessPolicyEngine/accepted successor
production behavior
```

before action-matrix changes.

---

## 54. DC-UP-003 — Messaging

Verify:

```text
outbox
message identity
dedup
retry
poison
```

for released asynchronous flows.

---

## 55. DC-UP-004 — Realtime recovery

Determine Platform readiness:

```text
publish
subscribe
reconnect
gap detection
recovery trigger
```

If gap/recovery absent:

```text
realtime phases may scaffold
but realtime certification remains BLOCKED-UPSTREAM
```

---

# Phase 4 — Page identity/lifecycle/metadata/visibility

## 56. DC-PAGE-001 — Page canonical model

Covers:

```text
DCREQ001–005
```

Audit:

```text
ID
AccountId
WorkspaceId
ParentId
Title
Icon
CoverImage
Status
Visibility
aggregate Version
soft-delete state
events
```

---

## 57. DC-PAGE-002 — Lifecycle closure

Verify/implement:

```text
create
rename
archive
delete
restore
```

with explicit no-op behavior and historical effects.

---

## 58. DC-PAGE-003 — Page update API reconciliation

Resolve `DC-SRC-DOC-005`.

For:

```text
Title
IconType
IconValue
CoverUrl
```

choose canonical supported shape.

No field remains accepted-and-ignored.

---

## 59. DC-PAGE-004 — Visibility vertical slice

Covers:

```text
DCREQ113–116
```

If visibility mutation is release-scoped:

```text
Domain method
Application command
permission action
API
event/history
tests
```

If not release-scoped:

```text
remove misleading mutation contract
and document read-only/internal status
```

---

## 60. DC-PAGE-005 — Unsupported Page placeholders

After scope decision:

```text
PublishPage
SetPageDeadline
```

must be either:

```text
implemented from accepted product semantics
or removed/retired safely
```

No `NotImplementedException` remains in a certified request.

---


## 60A. DC-PAGE-006 — Page deletion orchestration

Covers:

```text
DCREQ004
DCREQ220
```

Define the Documents-owned workflow for Page deletion across applicable:

```text
Blocks
ResourceLinks
history/version retention
search/index removal
file/reference retention
producer events
```

Cross-context effects remain event/handoff-driven.

Do not let EF cascade become product policy.

---

## 60B. DC-PATH-001 — Derived Page path

Covers:

```text
DCREQ201
```

If Page paths are exposed:

```text
rename/reparent
→ derived path changes
→ Page ID remains unchanged
```

No second durable path identity/ownership mechanism.

---

## 60C. DC-DUP-001 — Duplicate Page

Covers:

```text
DCREQ202
```

If release-scoped:

```text
new Page ID
new Block IDs
explicit ResourceLink behavior
explicit attachment/file-reference behavior
no source history/Comment identity reuse
```

Otherwise record:

```text
OUT_OF_RELEASE_SCOPE
```

---

# Phase 5 — Page hierarchy/reparent/concurrency

## 61. DC-HIER-001 — Implement MovePage runtime

Resolve `DC-SRC-DOC-001`.

The handler must orchestrate:

```text
load source
load destination if present
scope validation
destination lifecycle
source auth
destination auth
ancestry facts
cycle validation
concurrency
Domain Page.Move
commit
event
```

---

## 62. DC-HIER-002 — Source/destination facts

Do not ask Domain to query persistence.

Application supplies authoritative ancestry/destination facts.

---

## 63. DC-HIER-003 — Cross-Workspace protection

Ordinary move cannot change Workspace/Account.

Any transfer request is separate migration work.

---

## 64. DC-HIER-004 — Concurrent move hardening

Determine canonical conflict mechanism:

```text
aggregate Version
provider concurrency token
conditional update
transaction isolation
```

and verify committed tree.

---

# Phase 6 — Block identity/type/content/properties

## 65. DC-BLK-001 — Block canonical model

Covers:

```text
DCREQ011–017
```

Audit:

```text
ID
scope
PageId
ParentId
BlockType
BlockContent
BlockProperties
Position
Version
delete/restore
events
```

---

## 66. DC-BLK-002 — Type registry/content validation

Reconcile all released Block types with:

```text
BlockContentValidator
properties rules
API serialization
frontend/generated contracts
history/template compatibility
```

---

## 67. DC-BLK-003 — Unknown type/version

Implement/verify safe compatibility behavior.

Do not coerce to paragraph/default.

---

## 68. DC-BLK-004 — API content contract

Ensure:

```text
content
properties
type
version
```

have stable serialization and validation.

No persistence entity leak.

---

# Phase 7 — Block hierarchy

## 69. DC-BTREE-001 — Promote Block hierarchy to first-class capability

Covers:

```text
DCREQ117–121
```

Audit current:

```text
BlockAncestorPath
BlockTreeRules
CreateChild
MoveUnder
MoveToRoot
```

---

## 70. DC-BTREE-002 — Same-page parent enforcement

Preserve:

```text
Account
Workspace
Page
```

match.

No raw ParentId direct assignment path.

---

## 71. DC-BTREE-003 — Cycle proof

Application builds ancestry facts.

Domain remains pure and validates supplied facts.

---

## 72. DC-BTREE-004 — Block cross-Page rule

Remove any PLAN/API assumption that ordinary Block move can move between Pages.

If product later needs transfer:

```text
new dedicated copy/migrate contract
```

---

## 73. DC-BTREE-005 — Child deletion policy

Decide and implement one canonical behavior for deleting a parent Block.

This decision must cover:

```text
Domain
persistence
history
anchors
search
```

---

# Phase 8 — Ordering/editor concurrency/no-op

## 74. DC-ORD-001 — Fractional ordering review

Verify canonical ordering primitive and tests.

No new numeric midpoint implementation.

---

## 75. DC-ORD-002 — Reorder operations

Map:

```text
ReorderBlocks
BatchUpdateBlocks
Block.MoveUnder/MoveToRoot
```

onto one coherent semantics.

Avoid competing reorder authorities.

---

## 76. DC-ORD-003 — Dense insertion and boundary behavior

Add/retain focused tests for:

```text
first
last
between
prefix
dense repeated insertion
```

---

## 77. DC-ORD-004 — Ordering concurrency

Test actual overlapping mutation.

Fresh DB context must verify committed order.

---

## 78. DC-EDIT-001 — Stale write contract

Reconcile existing aggregate version with API mutation contract.

If expected version is required, expose it consistently.

If current source lacks it on a conflict-sensitive route:

```text
HARDEN
```

---

## 79. DC-EDIT-002 — No-op behavior

Verify current Domain no-op methods do not create:

```text
version churn
event churn
history churn
```

and extend to newly completed capabilities.

---

# Phase 9 — Documents query/API/search boundary

## 80. DC-DOC-QRY-001 — Query inventory

Normalize:

```text
GetPage
GetPageTree
GetWorkspacePages
SearchPages
GetPageBreadcrumb
GetPageBlocks
GetPageHistory
```

---

## 81. DC-DOC-QRY-002 — Authorization parity

A Page denied by direct read cannot leak through:

```text
tree
search
breadcrumb
history
```

---

## 82. DC-DOC-QRY-003 — Bounded loading

Measure:

```text
large tree
large Page
large Block tree
search result
```

No N+1 foreign lookups.

---

## 83. DC-DOC-SRCH-001 — Search derived-state boundary

Reconcile current search implementation.

If DB-backed direct query is current implementation:

```text
classify it as current query mechanism
```

Do not invent a separate search service.

If a derived index exists/appears, enforce authorization filtering and non-authority.

---

# Phase 10 — ResourceLinks

## 84. DC-LINK-001 — Release-scope classification

Resolve `DC-SRC-DOC-008`.

If current release requires ResourceLinks, continue the phase.

Otherwise record:

```text
OUT_OF_RELEASE_SCOPE
```

without claiming full Documents scope.

---

## 85. DC-LINK-002 — Application ports/use cases

If release-scoped, create minimal use cases for canonical product needs such as:

```text
create
list/query
delete
restore if exposed
```

No generic foreign mutation.

---

## 86. DC-LINK-003 — Target authorization

Opening/rendering embedded target rechecks target permission.

Public Page does not transitively expose private target.

---

## 87. DC-LINK-004 — Target deletion behavior

Define:

```text
tombstone/unavailable/hidden/historical/cleanup
```

per released LinkType/target.

---

# Phase 11 — Versions/Snapshots/history/restore

## 88. DC-HIST-001 — Define version creation policy

Resolve:

```text
what creates DocumentVersion?
when snapshot is created?
what is user-visible?
what is internal checkpoint?
```

Do not equate aggregate Version to DocumentVersion.

---

## 89. DC-HIST-002 — Snapshot schema/version

Ensure snapshots are versioned/interpretable across Block content evolution.

---

## 90. DC-HIST-003 — Replace empty history stub

Resolve `DC-SRC-DOC-004`.

`GetPageHistory` must either:

```text
read canonical history
or be removed/deferred explicitly
```

---

## 91. DC-HIST-004 — Restore use case

If history restore is release-scoped:

```text
authorize
load version
validate current scope/schema
apply new current state
record new history
```

Never mutate historical record into current truth.

---

## 92. DC-HIST-005 — History/Audit separation

No Governance Audit table/query becomes document history implementation.

---

# Phase 12 — Templates/import-export/files

## 93. DC-TPL-001 — Template release-scope classification

Resolve `DC-SRC-DOC-010`.

---

## 94. DC-TPL-002 — Template vertical slice

If release-scoped:

```text
create
publish
archive
query
instantiate
```

must be Application-owned with canonical authorization.

---

## 95. DC-TPL-003 — Instantiation contract

Instantiation creates new Page/Block identities.

No hidden live link after creation unless separately designed.

---

## 96. DC-IO-001 — Import boundary

Only implement import formats actually released.

Map to validated Page/Block model.

---

## 97. DC-IO-002 — Export boundary

Only implement actual released formats.

No format becomes canonical storage.

---

## 98. DC-FILE-001 — File/media references

Review Block types that refer to media/files.

Preserve:

```text
stable object identity/reference
safe metadata
no raw binary event/log content
```

---


## 98A. DC-DOC-INT-001 — Documents provider-integration translation boundary

Covers:

```text
DCREQ203
DCREQ218 where Collaboration provider sync is involved separately
```

For released Documents integrations:

```text
provider payload/schema
→ approved Documents Application operation
→ canonical Page/Block model
```

Provider schema must not become canonical Documents schema.

---

## 98B. DC-DOC-RET-001 — Documents retention across derived/historical copies

Covers:

```text
DCREQ204
```

Inventory retention/purge/export behavior across:

```text
current content
DocumentVersion/Snapshot
search/index projection
exports
backups/restore copies
file references
```

Do not infer cross-system purge from deleting live rows.

---

## 98C. DC-MOB-001 — Reduced-client compatibility

Covers:

```text
DCREQ205
```

If mobile/reduced editors are part of release compatibility:

```text
unsupported Block Type/property
→ preserve/read-only/reject safely
```

Backend contracts must not cause silent data loss.

---

## 98D. DC-OFF-001 — Offline-editing classification

Covers:

```text
DCREQ206
```

Default disposition without an accepted offline protocol:

```text
OUT_OF_RELEASE_SCOPE
```

If released, define operation identity/reconciliation/conflict/auth recheck separately from generic realtime.

---

## 98E. DC-A11Y-001 — Accessibility-preserving content contract

Covers:

```text
DCREQ207
```

Verify backend Block schema retains semantic fields clients need for accessible rendering.

Do not encode accessibility-critical semantic content only in opaque presentation HTML.

---

# Phase 13 — Documents authorization/tenant closure

## 99. DC-DOC-AUTH-001 — Canonical request/action matrix

Resolve `DC-SRC-DOC-007`.

At minimum classify every current request.

Use exact source and current `PermissionAction` enum.

No blanket rename.

---

## 100. DC-DOC-AUTH-002 — Policy matrix tests

For each materially different action:

```text
owner
admin
member
guest
non-member
explicit grant
explicit deny
restricted/private/public audience where applicable
```

test the real AccessPolicyEngine.

---

## 101. DC-DOC-AUTH-003 — Move destination auth

Page and Block structural moves require source + destination semantics.

---

## 102. DC-DOC-TEN-001 — Cross-tenant matrix

Execute representative:

```text
read Page
update Page
move Page
create Block
move Block
history
ResourceLink if released
Template if workspace-scoped/released
```

---

## 103. DC-DOC-RLS-001 — RLS enforcement

If current Documents tables are under RLS:

```text
application role
session context
A/B data
```

must prove persistence-layer denial.

---

# Phase 14 — Comment/reply/anchor/status

## 104. DC-COM-001 — Comment model reconciliation

Covers:

```text
DCREQ036–039
DCREQ045
DCREQ147–152
```

Audit:

```text
Target
ParentId
Content
Anchor
CommentStatus
CreatedBy
Version
soft delete
events
```

---

## 105. DC-COM-002 — Reply vertical slice

Current create API already accepts:

```text
ParentCommentId
```

Therefore reply semantics are release-relevant unless explicitly removed.

Verify:

```text
same target
same tenant
parent existence
deleted-parent rule
thread ordering
```

---

## 106. DC-COM-003 — Anchor vertical slice

Determine whether current API exposes anchors.

If Domain-only/internal:

```text
classify
```

If release-scoped:

```text
request contract
validation
staleness
query DTO
```

---

## 107. DC-COM-004 — Resolve/reopen/restore classification

Current API exposes resolve.

Domain additionally supports reopen/restore.

Decide release surface.

Do not leave impossible lifecycle states in API/DTO.

---

## 108. DC-COM-005 — DTO correctness

Current DTO includes:

```text
ResolvedAt
```

but current query mapping must be audited against actual Domain state/timestamps.

Remove hard-coded/null projection if it misrepresents source truth.

---


## 108A. DC-COM-006 — Thread deletion semantics

Covers:

```text
DCREQ214
```

Define parent deletion/tombstone effects on replies.

The committed/query result must preserve coherent thread identity and target scope.

---

## 108B. DC-COM-007 — Comment edit-history classification

Covers:

```text
DCREQ215
```

If edit history is released:

```text
Collaboration owns the history
```

and defines storage/query/retention.

Otherwise record:

```text
NOT_APPLICABLE
or OUT_OF_RELEASE_SCOPE
```

No generic Activity/Audit table becomes Comment history.

---

## 108C. DC-COL-OPT-001 — Optimistic Collaboration reconciliation

Covers:

```text
DCREQ219
```

For released optimistic:

```text
Comment
Reaction
ReadState
Attachment
Notification attention
```

define:

```text
temporary identity
request identity
success reconciliation
failure rollback
stale conflict behavior
authoritative reload/recovery
```

---

# Phase 15 — Collaboration target contracts

## 109. DC-TGT-001 — Supported target matrix

Current Comment source explicitly supports factories for:

```text
work-management.board-item
documents.page
```

Record each as independent target contract.

Do not infer Board/Block support.

---

## 110. DC-TGT-002 — Page target facts

Use Documents-owned facts + Governance.

No Collaboration `IDocumentDbContext`.

---

## 111. DC-TGT-003 — BoardItem target facts

Use WorkManagement-owned public/application facts.

The current WorkManagement collaboration **read adapter** is consumer-side counts integration and must not be confused with Comment target authorization facts.

---

## 112. DC-TGT-004 — Unknown target fail-closed

No arbitrary generic repository fallback.

---

# Phase 16 — Mentions

## 113. DC-MEN-001 — Current Mention creation review

Current CreateComment creates one Mention per distinct non-empty mentioned user ID.

Audit:

```text
valid user?
workspace membership?
target access?
duplicates?
self mention?
deleted user?
```

---

## 114. DC-MEN-002 — Mention validation

Resolve stable Identity lookup and privacy rules.

Mention must not grant access.

---

## 115. DC-MEN-003 — Edit diff

UpdateComment must reconcile:

```text
existing mentions
new mentions
removed mentions
unchanged mentions
```

if MentionedUserIds/content mentions are release-supported on edit.

Do not resend unchanged attention.

---

## 116. DC-MEN-004 — Delivery idempotency

Pin current `MentionCreatedNotificationConsumer`.

Verify same message identity cannot duplicate one logical recipient effect.

---

# Phase 17 — Reactions

## 117. DC-REACT-001 — Release-scope classification

Resolve `DC-SRC-COL-003`.

---

## 118. DC-REACT-002 — Reaction vertical slice

If release-scoped:

```text
create
remove
query/count
```

with target authorization.

---

## 119. DC-REACT-003 — Uniqueness/retry

Use Domain/persistence constraints to guarantee deterministic uniqueness.

---

# Phase 18 — Attachments

## 120. DC-ATT-001 — Supported target decision

Resolve `DC-SRC-COL-010`.

Current Application/API are BoardItem-specific.

Document exact release target kinds.

---

## 121. DC-ATT-002 — Metadata/object-reference review

Current `FileMetadata`/Attachment creation must satisfy:

```text
safe filename
size/content type
URL/object identity
no raw binary
```

---

## 122. DC-ATT-003 — URL/download security

Current create accepts absolute HTTP(S) URL.

Determine whether arbitrary remote URL support is intentional.

If storage-owned object/signed URL contract is canonical, refactor accordingly.

Prevent URL from becoming permission proof.

---

## 123. DC-ATT-004 — Delete/retention

Define provider object cleanup separately from Collaboration history.

---


## 123A. DC-ATT-005 — Upload lifecycle

Covers:

```text
DCREQ208
```

If upload lifecycle is product-visible, define accepted states such as:

```text
pending
uploaded
failed
removed
```

and the transition between provider/object operation and durable Attachment metadata.

A signed upload URL/provider response is not durable Attachment truth.

---

# Phase 19 — Presence

## 124. DC-PRES-001 — Release-scope classification

Resolve `DC-SRC-COL-004`.

If not release-scoped:

```text
keep Domain/persistence as internal foundation or retire
```

and do not certify Presence.

---

## 125. DC-PRES-002 — Presence lifecycle

If released:

```text
connect
heartbeat/update
disconnect
expiry
reconnect
```

---

## 126. DC-PRES-003 — Presence authorization

No presence session/channel join without canonical target authorization.

No PresenceSession used as authorization proof.

---


## 126A. DC-PRES-004 — Cursor/typing classification

Covers:

```text
DCREQ209
```

If cursor/typing is released:

```text
ephemeral
resource-scoped
authorization-scoped
non-historical
```

No durable Activity/Audit/Comment-history write is created solely from cursor/typing transport.

---

# Phase 20 — ReadState

## 127. DC-READ-001 — Release-scope classification

Resolve `DC-SRC-COL-005`.

---

## 128. DC-READ-002 — Read boundary semantics

If released, define:

```text
LastReadAt
LastReadCommentId
UnreadCount
```

and which field is canonical/derived.

---

## 129. DC-READ-003 — Monotonicity

Reuse current Domain monotonic tests and add persistence/concurrency proof.

---

# Phase 21 — Watchers

## 130. DC-WATCH-001 — Release-scope classification

Resolve `DC-SRC-COL-006`.

---

## 131. DC-WATCH-002 — Watch/unwatch

If released:

```text
watch
unwatch
explicit/automatic trigger
idempotency
```

---

## 132. DC-WATCH-003 — Watcher/permission separation

A watcher whose access is revoked cannot receive protected content merely because watch state remains.

---

# Phase 22 — Notification and Activity

## 133. DC-NOTIF-001 — Notification architecture decision

Resolve `DC-SRC-COL-007`.

Record:

```text
durable product Notification owner/storage
delivery adapter
recipient identity
read/dismiss state if release-scoped
```

If no durable product Notification is currently in release:

```text
classify current Mention notification consumer as delivery integration
```

without falsely certifying Notification product.

---

## 134. DC-NOTIF-002 — Recipient fan-out/idempotency

For released notification delivery:

```text
one logical recipient effect
same message retry safe
permission rechecked on deep-link/view
```

---


## 134A. DC-NOTIF-003 — Delivery-channel state

Covers:

```text
DCREQ210
```

Reconcile current provider/channel implementation with durable Notification/recipient state.

Keep separate:

```text
Notification truth
recipient seen/read/archive/dismiss
channel/provider delivery attempts
```

---

## 134B. DC-NOTIF-004 — Notification preferences

Covers:

```text
DCREQ211
```

Current source already has `NotificationPreferenceRecord`.

Classify release-scoped operations for:

```text
enabled
channel
delivery mode
digest interval
quiet hours
timezone
Workspace scope
```

Do not create a duplicate preference store.

---

## 134C. DC-NOTIF-005 — Recipient attention lifecycle

Covers:

```text
DCREQ212
```

Current `NotificationRecipientRecord` already stores:

```text
SeenAt
ReadAt
ArchivedAt
DismissedAt
RecipientStatus
```

Classify and, if released, expose Application/API operations using that canonical state.

Attention-state mutation must not mutate source resources or provider-delivery truth.

---

## 134D. DC-NOTIF-006 — Reuse existing Notification runtime chain

Before adding Notification infrastructure, inspect and reuse current:

```text
MentionCreatedNotificationConsumer
NotificationItemRecord
NotificationRecipientRecord
NotificationPreferenceRecord
NotificationCounterRecord
MentionCreatedNotificationRuntimeChainIntegrationTests
NotificationsClassificationTests
```

Classify `LegacyCollabNotificationWriteBanTests` according to current authority and rename/replace stale wording if needed.

---

## 135. DC-ACT-001 — Activity projection model

Reconcile Infrastructure Activity projection consumers with Application Activity query.

Identify actual projection store/table if present.

If none:

```text
BLOCKED-IMPLEMENTATION
or retire/defer endpoint
```

---

## 136. DC-ACT-002 — Replace empty Activity stub

Resolve `DC-SRC-COL-001`.

No constant empty response in certified capability.

---

## 137. DC-ACT-003 — Activity != Audit

Projection only from product facts.

No broker-attempt activity items.

---


## 137A. DC-ACT-004 — Reuse existing Activity projection store

Resolve `DC-SRC-COL-001` by identifying existing:

```text
Activity projection consumers
WorkspaceActivityLogRecord
projection query infrastructure
```

before changing `GetResourceActivity`.

Forbidden:

```text
create second Activity table/model solely for Collaboration endpoint
```

unless architecture authority explicitly requires a new projection.

---

## 137B. DC-COL-SRCH-001 — Collaboration search

Covers:

```text
DCREQ217
```

If Comment/Activity search is released:

```text
derived search
+
current target authorization
+
tenant/resource lifecycle filtering
```

must be verified.

Otherwise classify it outside release scope.

---

# Phase 23 — Collaboration auth/tenant/retention

## 138. DC-COL-AUTH-001 — Canonical Comment action matrix

Resolve `DC-SRC-COL-009`.

For:

```text
GetComments
CreateComment
UpdateComment
DeleteComment
ResolveComment
future Reopen/Restore
```

define exact:

```text
resource kind
action
author/moderator semantics
target fact
```

---

## 139. DC-COL-AUTH-002 — Own/moderator policy

If author ownership matters, encode it as a product fact/policy input without replacing central Governance.

---

## 140. DC-COL-AUTH-003 — Target revocation

Revalidate permission for:

```text
Comment
Attachment
Mention/notification view
Reaction
Watcher
ReadState
realtime
```

as applicable.

---

## 141. DC-COL-TEN-001 — Cross-tenant matrix

Use at least two tenants/workspaces.

No target spoofing.

---

## 142. DC-RET-001 — Target deletion matrix

For each released target kind and Collaboration capability, record outcome.

Matrix rows:

```text
Comment
Reply
Anchor
Mention
Reaction
Attachment
Watcher
ReadState
Notification
Activity
Presence
```

Columns:

```text
archive
delete
restore
```

---

## 143. DC-RET-002 — Identity deletion/anonymization

Record behavior for:

```text
Comment author
Mentioned user
Reaction user
Watcher
ReadState owner
Presence
Notification recipient
Activity actor
```

---


## 143A. DC-RET-003 — Workspace deletion/archive workflow

Covers:

```text
DCREQ213
```

For each released Collaboration capability define:

```text
retain
hide
export
anonymize
purge
or delayed purge
```

under Workspace lifecycle.

Do not reuse target-deletion policy blindly because Workspace deletion is a broader cross-context workflow.

---

# Phase 24 — Events and cross-context handoffs

## 144. DC-EVT-001 — Producer event manifest

Inventory every released Documents/Collaboration producer fact.

Do not add events merely because Domain event exists.

---

## 145. DC-EVT-002 — Event privacy

Review whether event includes:

```text
raw Comment content
raw Block content
full snapshot
PII
URL
```

Only necessary producer facts allowed.

---

## 146. DC-EVT-003 — Version/manifest

Exact runtime name/version must match canonical manifest.

---

## 147. DC-EVT-004 — Post-commit/outbox

Representative:

```text
Page
Block
Comment
Mention
```

where released.

---

## 148. DC-X-AUT-001 — Automation

Use producer events/public commands.

No private handler invocation.

---

## 149. DC-X-ANA-001 — Analytics

Producer facts only.

Analytics owns projection.

---

## 150. DC-X-WM-001 — Collaboration summary read boundary

Preserve current architecture:

```text
WorkManagement consumer-owned port
→ cross-context adapter
→ Collaboration producer-owned ResourceSummary
```

No Collaboration DbContext leakage.

---


## 150A. DC-X-INT-001 — Collaboration provider-integration boundary

Covers:

```text
DCREQ218
```

If provider comment/message synchronization is released:

```text
provider identity/content/resource
→ approved Collaboration operation
```

and must pass:

```text
tenant scope
target validation
authorization
content safety
idempotency
```

Provider callbacks do not write Collaboration tables directly.

---

# Phase 25 — Realtime/recovery

## 151. DC-RT-001 — Resource/state inventory

For every released realtime state classify:

```text
durable
derived
ephemeral
```

Examples:

```text
Page/Block        durable
Comment/Reaction  durable
ReadState         durable/derived
Notification      durable if product state
Presence          ephemeral
```

---

## 152. DC-RT-002 — Duplicate/reorder semantics

Do not use one generic handler assertion for all state types.

Verify each released state model.

---

## 153. DC-RT-003 — Gap/recovery

Durable states recover from authoritative query/checkpoint.

Presence rebuilds from live connections/expiry.

Do not restore stale Presence from durable document state.

---

## 154. DC-RT-004 — Permission revocation

During disconnect/reconnect:

```text
permission revoked
→ recovery/subscription does not restore unauthorized content
```

---

## 155. DC-RT-005 — Final convergence

For each released durable state:

```text
client state == authoritative server state
```

after duplicate/reorder/gap/reconnect.

---


## 155A. DC-RT-006 — Offline editing remains separate from realtime

Covers:

```text
DCREQ206
```

Realtime reconnect/gap recovery does not certify offline mutation.

If offline editing is not release-scoped:

```text
record OUT_OF_RELEASE_SCOPE
```

If it is released, require a separate accepted reconciliation protocol and corresponding tests.

---

## 155B. DC-RT-007 — Optimistic Collaboration convergence

Covers:

```text
DCREQ219
```

Realtime/recovery tests for optimistic Collaboration state must verify authoritative identity/state after:

```text
success
reject
duplicate
stale mutation
reconnect
```

---

# Phase 26 — Persistence/migration/compatibility

## 156. DC-MIG-001 — Migration inventory

List migrations affecting:

```text
Pages
Blocks
ResourceLinks
Versions
Templates
Comments
Mentions
Reactions
Attachments
Presence
ReadState
Watchers
RLS/indexes
```

---

## 157. DC-MIG-002 — Clean database

Apply supported baseline/migrations from zero.

Start production graph.

---

## 158. DC-MIG-003 — Upgrade policy

Use supported prior schema or document canonical fresh-baseline-only policy.

Do not invent upgrade support.

---

## 159. DC-MIG-004 — Pending-model gate

No unexplained model drift.

---

## 160. DC-MIG-005 — Hierarchy/order migration

If changed:

```text
Page hierarchy
Block hierarchy
FractionalIndex
```

preserve semantic state.

---

## 161. DC-MIG-006 — Content family compatibility

A Block schema change must verify:

```text
live Block
DocumentSnapshot
DocumentVersion
PageTemplate
```

fixtures.

This is mandatory because historical/template copies can outlive live content.

---

## 162. DC-MIG-007 — Collaboration target/reference compatibility

If ResourceRef/storage representation changes, preserve all released Collaboration state.

---

# Phase 27 — Reliability/idempotency/failure

## 163. DC-REL-001 — Local atomicity

Representative:

```text
Page move
Block parent+order
Comment+Mention creation
```

where semantic atomicity requires it.

---

## 164. DC-REL-002 — Persistence failure

Real boundary failure.

Fresh context verifies rollback.

---

## 165. DC-REL-003 — Cross-context fact failure

Fail target fact/authorization dependency.

Fail closed.

---

## 166. DC-REL-004 — Idempotency classification

Classify:

```text
Comment create
Mention delivery
Reaction create
Watch
mark read
attachment create
```

according to retry characteristics.

---

## 167. DC-REL-005 — Message dedup/poison

Same logical message ID → one durable effect.

Poison A must not permanently block valid B.

---

# Phase 28 — Security/privacy

## 168. DC-SEC-001 — Protected-handler inventory

Every release-scoped handler is mapped to:

```text
resource
action
scope
```

No unclassified bypass.

---

## 169. DC-SEC-002 — Content/log redaction

Sentinel tests across:

```text
Block content
Comment content
Attachment URL/metadata
snapshot
event failure
```

---

## 170. DC-SEC-003 — Rich-content safety

For released rich content/Markdown/import:

```text
sanitization
escaping
link handling
```

---

## 171. DC-SEC-004 — Signed URL/object access

If attachments/media use signed/download URLs:

```text
expiry
scope
permission
no durable secret URL
```

---


## 171A. DC-SEC-005 — Collaboration abuse-control classification

Covers:

```text
DCREQ216
```

For public/guest/high-abuse release surfaces classify and implement applicable:

```text
rate limiting
content limits
moderation
spam/abuse controls
attachment constraints
```

If no such exposure exists:

```text
NOT_APPLICABLE
```

with release evidence.

---

# Phase 29 — Performance/observability

## 172. DC-PERF-001 — Documents hot paths

Measure:

```text
Page tree
large Block tree
search
history
order mutation
```

as applicable.

---

## 173. DC-PERF-002 — Collaboration hot paths

Measure:

```text
Comment thread
Activity
Reaction counts
ReadState/unread
Watcher fan-out
```

as released.

---

## 174. DC-OBS-001 — Safe correlation

Expose stable IDs without raw content.

---

## 175. DC-OBS-002 — Recovery diagnostics

Gap/recovery/duplicate/stale suppression observable.

---

# Phase 30 — Cross-team integration

## 176. DC-XINT-001 — Workspace/Governance

Production DI path:

```text
request
→ tenant context
→ resource facts
→ AccessPolicyEngine
→ handler
```

---

## 177. DC-XINT-002 — Documents → Collaboration Page target

Cases:

```text
allowed
restricted
wrong tenant
archived/deleted
fact dependency failure
```

---

## 178. DC-XINT-003 — WorkManagement BoardItem target

Only if release-scoped and upstream D4+.

No private persistence.

---

## 179. DC-XINT-004 — Automation

Released events only.

---

## 180. DC-XINT-005 — Analytics

Released projections/events only.

---

# Phase 31 — TESTS handoff

## 181. DC-TEST-HO-001 — Full canonical traceability

The rewritten TESTS must map:

```text
PROD-PROD-DCT-001..031
PROD-PROD-COL-001..029
DCREQ001..220
```

through SPEC IDs to executable proof.

Because DCT/COL IDs map into DCREQ IDs in SPEC, TESTS may use DCREQ as execution key but must retain a Product→SPEC trace table.

---

## 182. DC-TEST-HO-002 — Source-debt regression coverage

Every resolved `DC-SRC-*` ledger item must have:

```text
test
architecture/source gate
or explicit retirement verification
```

where mechanically testable.

---

## 183. DC-TEST-HO-003 — Capability scope visibility

TESTS must distinguish:

```text
REQUIRED FOR CORE
REQUIRED FOR FULL RELEASE
NOT_APPLICABLE
BLOCKED
```

No missing capability hidden by absent tests.

---

# Phase 32 — Contract/docs handoff

## 184. DC-DOC-HO-001 — OpenAPI

Intentional API changes regenerate/review canonical OpenAPI.

Especially:

```text
UpdatePage fields
Page visibility
history
Comment lifecycle
new released vertical slices
```

---

## 185. DC-DOC-HO-002 — Event manifest

Only released public events.

No unrelated semantic drift.

---

## 186. DC-DOC-HO-003 — Product docs

Execution must not rewrite canonical product docs unless an accepted product decision actually changes them.

Placeholder retirement that contradicts no product authority does not need product-doc expansion.

---

# Phase 33 — CERTIFICATION handoff

## 187. DC-CERT-HO-001 — Candidate snapshot

Provide:

```text
candidate SHA
release scope
source-debt dispositions
migration head
generated artifacts
test counts
CI
blockers
non-blocking debt
```

---

## 188. DC-CERT-HO-002 — Core vs full status

CERTIFICATION must be able to say independently:

```text
Documents Core
Documents Full Release Scope
Collaboration Core
Collaboration Full Release Scope
Target support by kind
Realtime by state type
```

---

## 189. DC-CERT-HO-003 — No placeholder in certified capability

A capability cannot be VERIFIED/STABLE if its request path still:

```text
throws NotImplementedException
returns constant fake success
returns constant empty product result
silently ignores public fields
```

---

# Execution waves

## 190. Wave A — Authority/inventory/debt

```text
Phase 0
Phase 1
Phase 2
Phase 3
```

No broad implementation before this wave closes.

---

## 191. Wave B — Documents transactional core

```text
Phase 4
Phase 5
Phase 6
Phase 7
Phase 8
Phase 13
```

This is the minimum stable producer foundation.

---

## 192. Wave C — Documents knowledge capabilities

```text
Phase 9
Phase 10
Phase 11
Phase 12
```

Execute only release-scoped sub-capabilities.

---

## 193. Wave D — Collaboration discussion core

```text
Phase 14
Phase 15
Phase 23 comment/target portions
```

---

## 194. Wave E — Collaboration attention/media

```text
Phase 16
Phase 17
Phase 18
Phase 19
Phase 20
Phase 21
Phase 22
```

Parallelism depends on shared target/auth semantics.

---

## 195. Wave F — Events/realtime/hardening

```text
Phase 24
Phase 25
Phase 26
Phase 27
Phase 28
Phase 29
Phase 30
```

---

## 196. Wave G — verification/certification

```text
Phase 31
Phase 32
Phase 33
```

---

# Safe parallelism

## 197. Safe Documents parallelism

After Page/Block scope semantics are stable:

```text
Page metadata/visibility
Block content
ResourceLink release-scope work
history design
Template classification
```

may progress independently if they do not redefine shared contracts.

---

## 198. Safe Collaboration parallelism

After target ResourceRef + authorization semantics are frozen:

```text
Mentions
Reactions
Attachments
ReadState
Watchers
Presence
```

may progress in separate work units.

---

## 199. Unsafe parallelism

Do not independently redesign:

```text
ResourceRef
authorization actions
target deletion policy
Block Type serialization
event identity/version
realtime recovery
```

in multiple capability PRs.

These are shared contracts.

---

# Layer impact

## 200. Documents layer matrix

| Capability | Domain | Application | Infrastructure | API | Platform | Migration |
|---|---|---|---|---|---|---|
| Page lifecycle | yes | yes | persistence | yes | events | maybe |
| metadata/visibility | yes | yes | persistence | yes | auth/event | maybe |
| Page hierarchy | yes | yes | persistence | yes | no | maybe |
| Block content | yes | yes | persistence | yes | no | maybe |
| Block hierarchy | yes | yes | persistence | yes | no | maybe |
| ordering | value/domain | yes | persistence | yes | shared primitive maybe | maybe |
| ResourceLink | yes | yes if release | persistence | yes if release | auth | maybe |
| Versions/Snapshots | yes | yes if release | persistence | yes if release | events | likely compatibility |
| Templates | yes | yes if release | persistence | yes if release | auth/events | maybe |
| search boundary | facts | query/port | adapter/index | yes | derived infra | no/derived |
| file refs | value semantics | yes | object adapter | yes | security | maybe |

---

## 201. Collaboration layer matrix

| Capability | Domain | Application | Infrastructure | API | Platform | Migration |
|---|---|---|---|---|---|---|
| Comment/reply/status | yes | yes | persistence | yes | events | maybe |
| Anchor | value/domain | yes if release | persistence | DTO | realtime maybe | maybe |
| Mention | yes | yes | persistence/consumer | maybe | messaging | maybe |
| Reaction | yes | yes if release | persistence | yes if release | realtime | maybe |
| Attachment | yes | yes | persistence/object adapter | yes | storage/security | maybe |
| Presence | yes | yes if release | persistence/cache | realtime | realtime | maybe |
| ReadState | yes | yes if release | persistence | yes if release | realtime | maybe |
| Watcher | yes | yes if release | persistence | yes if release | messaging | maybe |
| Notification | semantic owner | yes if release | provider/projection | yes if release | messaging | maybe |
| Activity | projection semantics | query | consumers/store | yes | messaging | maybe |

---

# PR/work-unit decomposition

## 202. PR decomposition principle

Each PR should close one coherent invariant family.

Do not create a single mega "Documents/Collaboration completion" PR.

---

## 203. Suggested PR sequence

```text
PR-DC-00  inventory + source-debt + release-scope decisions

PR-DC-01  Documents action-matrix cleanup + Page metadata contract
PR-DC-02  MovePage/hierarchy/concurrency
PR-DC-03  Block hierarchy/order/concurrency
PR-DC-04  Page visibility if release-scoped
PR-DC-05  Documents query/API parity

PR-DC-06  ResourceLinks if release-scoped
PR-DC-07  Versions/Snapshots/history/restore if release-scoped
PR-DC-08  Templates if release-scoped

PR-DC-09  Comment action matrix + reply/status/anchor
PR-DC-10  target contracts
PR-DC-11  Mention lifecycle
PR-DC-12  Reaction/ReadState/Watcher slices as released
PR-DC-13  Attachment hardening
PR-DC-14  Presence if released
PR-DC-15  Notification/Activity decision + implementation

PR-DC-16  events/cross-context handoffs
PR-DC-17  realtime/recovery
PR-DC-18  migrations/reliability/security/performance
PR-DC-19  certification closure
```

Actual split may combine tightly coupled schema/API changes.

---

## 204. Forbidden PR bundling

Do not bundle unrelated:

```text
Page hierarchy
Notification architecture
Block type migration
Presence
generic messaging refactor
```

into one cleanup PR.

---

# Migration sequencing

## 205. Schema change sequence

For each schema-affecting change:

```text
Domain/Application contract
→ EF config
→ migration
→ migration review
→ clean DB
→ upgrade/baseline policy
→ integration tests
→ pending-model check
```

---

## 206. Content family rollout

For Block schema evolution:

```text
compatible reader
→ live writer
→ snapshot/version/template compatibility
→ migration/backfill
→ remove legacy reader only after proof
```

where rollout model requires staged compatibility.

---

## 207. ResourceRef/target rollout

If target representation changes:

```text
compatible read
→ migrate historical collaboration state
→ update writers
→ verify target/auth/retention
→ remove legacy representation
```

---

# Stop conditions

## 208. Stop-condition authority

PLAN does not create a competing semantic stop taxonomy.

Canonical semantic stop IDs are:

```text
DCSTOP001–DCSTOP026
```

from SPEC.

PLAN references those IDs.

Execution-only blockers use:

```text
BLOCKED-DECISION
BLOCKED-UPSTREAM
BLOCKED-IMPLEMENTATION
BLOCKED-VERIFICATION
```

without inventing duplicate semantic stop IDs.

---

## 209. STOP — unknown product semantic

If a source placeholder is not product-backed:

```text
DCSTOP014
```

Stop before implementation.

---

## 210. STOP — private cross-context access

If a work unit needs foreign private persistence:

```text
DCSTOP002
```

Fix public contract first.

---

## 211. STOP — unsafe Page/Block structure

Hierarchy/order corruption risk:

```text
DCSTOP004
DCSTOP005
```

blocks release of that structural mutation.

---

## 212. STOP — content family compatibility

If live/snapshot/version/template content cannot all be interpreted:

```text
DCSTOP006
```

blocks rollout.

---

## 213. STOP — target/retention ambiguity

Use:

```text
DCSTOP008
DCSTOP011
```

No persistence cascade guess.

---

## 214. STOP — Notification/Activity ownership ambiguity

Use:

```text
DCSTOP015
```

Do not move semantic ownership into Platform/Governance merely to complete implementation.

---

# P4B core gates

## 215. Documents Core D4 gate

Requires executable proof for release-scoped:

```text
Page identity/lifecycle
Page metadata contract
visibility if exposed
Page hierarchy/reparent
Block identity/content
Block hierarchy
ordering
query/API
authorization
tenant isolation
```

No `NotImplementedException` on a core route.

---

## 216. Documents Core D5 gate

D4 plus:

```text
stable public contracts
stable resource/action semantics
migration clean
no critical source debt
consumer-safe Page target contract
exact-SHA CI
```

---

## 217. Documents Full Release gate

Adds every release-scoped:

```text
ResourceLink
history/version/snapshot
Template
search/index integration
import/export
file/media
events
realtime
```

---

## 218. Collaboration Core D4 gate

Requires:

```text
Comment/reply/status
Anchor if released
Page/BoardItem target support as released
authorization
tenant isolation
retention
query/API
```

---

## 219. Collaboration Core D5 gate

D4 plus:

```text
stable target contracts
stable request/action semantics
no private target persistence
exact-SHA CI
```

---

## 220. Collaboration Full Release gate

Adds every released:

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

## 221. Realtime D4+ gate

Per state type:

```text
duplicate
out-of-order
gap
recovery
revocation
convergence
```

A SignalR/WebSocket smoke test is insufficient.

---

# Evidence handoff

## 222. Required evidence bundles

PLAN hands TESTS/CERTIFICATION:

```text
DC-EVID-01  source inventory/disposition
DC-EVID-02  Page lifecycle/metadata/visibility
DC-EVID-03  Page hierarchy
DC-EVID-04  Block content/properties
DC-EVID-05  Block hierarchy
DC-EVID-06  ordering/concurrency
DC-EVID-07  ResourceLinks
DC-EVID-08  Versions/Snapshots/history
DC-EVID-09  Templates/import/export/files
DC-EVID-10  Documents authorization/tenant

DC-EVID-11  Comment/reply/anchor/status
DC-EVID-12  target adapters
DC-EVID-13  Mentions
DC-EVID-14  Reactions
DC-EVID-15  Attachments
DC-EVID-16  Presence
DC-EVID-17  ReadState/Watchers
DC-EVID-18  Notification/Activity
DC-EVID-19  Collaboration authorization/tenant/retention

DC-EVID-20  API/query contracts
DC-EVID-21  events/messaging
DC-EVID-22  realtime/recovery
DC-EVID-23  migration/compatibility
DC-EVID-24  reliability
DC-EVID-25  security/privacy
DC-EVID-26  performance/observability
DC-EVID-27  cross-team integration
DC-EVID-28  exact-SHA CI
```

---

# Source-debt closure matrix template

## 223. Mandatory closure record

Before certification, populate:

| Debt ID | Initial source state | Disposition | Change/evidence | Final state |
|---|---|---|---|---|
| DC-SRC-DOC-001 | MovePage NotImplemented | | | |
| DC-SRC-DOC-002 | PublishPage NotImplemented | | | |
| DC-SRC-DOC-003 | SetPageDeadline NotImplemented | | | |
| DC-SRC-DOC-004 | PageHistory empty | | | |
| DC-SRC-DOC-005 | UpdatePage fields ignored | | | |
| DC-SRC-DOC-006 | visibility incomplete | | | |
| DC-SRC-DOC-007 | Documents action drift | | | |
| DC-SRC-DOC-008 | ResourceLinks thin slice | | | |
| DC-SRC-DOC-009 | Versions/Snapshots thin slice | | | |
| DC-SRC-DOC-010 | Templates thin slice | | | |
| DC-SRC-COL-001 | Activity projection exists; resource query unwired | | | |
| DC-SRC-COL-002 | Mention lifecycle incomplete | | | |
| DC-SRC-COL-003 | Reactions thin slice | | | |
| DC-SRC-COL-004 | Presence thin slice | | | |
| DC-SRC-COL-005 | ReadState thin slice | | | |
| DC-SRC-COL-006 | Watchers thin slice | | | |
| DC-SRC-COL-007 | Durable Notification state exists; Application/product ownership incomplete | | | |
| DC-SRC-COL-008 | Comment capability partial exposure | | | |
| DC-SRC-COL-009 | Collaboration request/resource/action drift | | | |
| DC-SRC-COL-010 | Attachment target ambiguity | | | |

A blank final row blocks full-scope certification.

---

# Requirement family → PLAN mapping

## 224. DCREQ001–005

Plan:

```text
DC-PAGE-001
DC-PAGE-002
```

---

## 225. DCREQ006–010

Plan:

```text
DC-HIER-001..004
```

---

## 226. DCREQ011–017

Plan:

```text
DC-BLK-001..004
```

---

## 227. DCREQ018–022

Plan:

```text
DC-ORD-001..004
```

---

## 228. DCREQ023–026

Plan:

```text
DC-DOC-QRY-001..003
DC-DOC-SRCH-001
```

---

## 229. DCREQ027–031 / DCREQ136–137

Plan:

```text
DC-EDIT-001..002
DC-RT-*
```

---

## 230. DCREQ032–035 / 107–109 / 191

Plan:

```text
DC-DOC-AUTH-001..003
DC-DOC-TEN-001
DC-DOC-RLS-001
```

---

## 231. DCREQ113–121

Plan:

```text
DC-PAGE-003..005
DC-BTREE-001..005
```

---

## 232. DCREQ122–127

Plan:

```text
DC-LINK-001..004
```

---

## 233. DCREQ128–134

Plan:

```text
DC-HIST-001..005
```

---

## 234. DCREQ135–146 / 194

Plan:

```text
DC-TPL-001..003
DC-IO-001..002
DC-FILE-001
DC-MIG-006
```

---

## 235. DCREQ036–053 / 147–152 / 192

Plan:

```text
DC-COM-001..005
DC-TGT-001..004
DC-COL-AUTH-001..003
DC-RET-001..002
```

---

## 236. DCREQ153–157

Plan:

```text
DC-MEN-001..004
```

---

## 237. DCREQ158–161

Plan:

```text
DC-REACT-001..003
```

---

## 238. DCREQ162–166

Plan:

```text
DC-ATT-001..004
DC-SEC-004
```

---

## 239. DCREQ167–170

Plan:

```text
DC-PRES-001..003
DC-RT-*
```

---

## 240. DCREQ171–174

Plan:

```text
DC-READ-001..003
DC-COL-AUTH-003
```

---

## 241. DCREQ175–178

Plan:

```text
DC-WATCH-001..003
```

---

## 242. DCREQ179–188

Plan:

```text
DC-NOTIF-001..002
DC-ACT-001..003
```

---

## 243. DCREQ189–190

Plan:

```text
DC-RET-001
DC-RT-001..005
```

---

## 244. DCREQ054–071 / 193

Plan:

```text
DC-EVT-001..004
DC-X-*
DC-XINT-*
```

---

## 245. DCREQ072–082

Plan:

```text
DC-DOC-TEN-001
DC-DOC-RLS-001
DC-COL-TEN-001
DC-REL-001
DC-SEC-*
```

---

## 246. DCREQ083–092 / 110–112

Plan:

```text
DC-MIG-001..007
DC-REL-001..005
```

---

## 247. DCREQ093–099

Plan:

```text
DC-PERF-001..002
DC-OBS-001..002
```

---

## 248. DCREQ100–106 / 195–200

Plan:

```text
Phase 0
Phase 1
Phase 2
source-debt ledger
STOP authority
```

---


## 248A. DCREQ201–207 — additional Documents Product semantics

Plan:

```text
DC-PATH-001          → DCREQ201
DC-DUP-001           → DCREQ202
DC-DOC-INT-001       → DCREQ203
DC-DOC-RET-001       → DCREQ204
DC-MOB-001           → DCREQ205
DC-OFF-001           → DCREQ206
DC-A11Y-001          → DCREQ207
```

---

## 248B. DCREQ208–219 — additional Collaboration Product semantics

Plan:

```text
DC-ATT-005           → DCREQ208
DC-PRES-004          → DCREQ209
DC-NOTIF-003         → DCREQ210
DC-NOTIF-004         → DCREQ211
DC-NOTIF-005         → DCREQ212
DC-RET-003           → DCREQ213
DC-COM-006           → DCREQ214
DC-COM-007           → DCREQ215
DC-SEC-005           → DCREQ216
DC-COL-SRCH-001      → DCREQ217
DC-X-INT-001         → DCREQ218
DC-COL-OPT-001       → DCREQ219
DC-RT-007            → DCREQ219
```

---

## 248C. DCREQ220 — Page deletion orchestration

Plan:

```text
DC-PAGE-006
```

Cross-context Collaboration reaction remains:

```text
DC-RET-001
```

under `DCREQ189`.

---


## 248D. Explicit traceability closure for previously implicit requirements

The following requirements were conceptually covered before this patch but must remain explicit in PLAN text for machine-verifiable traceability:

```text
DCREQ110 → DC-REL-004 — mutation idempotency classification
DCREQ111 → DC-REL-005 — consumer deduplication
DCREQ112 → DC-REL-005 — poison isolation
DCREQ192 → DC-COL-AUTH-001 — Collaboration request/action matrix
DCREQ193 → DC-EVT-002 — public event payload minimization
DCREQ194 → DC-MIG-006 — snapshot/version/template compatibility
DCREQ199 → DC-SCOPE-006 — Domain-rich/Application-thin classification
DCREQ200 → DC-SCOPE-001 + DC-SCOPE-008 — full Product scope disposition
```

---


# Normative PLAN → TESTS traceability

## 249A. Traceability authority

This table is normative.

For every work unit below it records:

```text
PLAN work unit
→ SPEC requirement(s)
→ mandatory TEST ID/family
→ primary implementation/evidence surface
→ evidence bundle / CI
```

The rewritten TESTS artifact must define:

```text
DC-TST-TRACE-001
```

in the existing Architecture test project.

That gate must fail when:

```text
a PLAN work unit disappears from this matrix
a mapped SPEC requirement disappears
a mapped test ID/family is removed without updating PLAN
a work unit has no implementation/evidence surface
a work unit has no evidence/CI mapping
duplicate rows create competing authority
```

This documentation gate does not replace behavioral tests.

---

## 249B. Normative work-unit matrix

| PLAN work unit | SPEC requirement(s) | Mandatory TEST ID/family | Primary implementation/evidence surface | Evidence / CI |
|---|---|---|---|---|
| `DC-SRC-DOC-001` | DCREQ006, DCREQ007, DCREQ008, DCREQ009, DCREQ010, DCREQ195 | DC-TST-HIER-APP-001, DC-TST-SRC-001 | MovePage handler | DC-EVID-03, DC-EVID-01 |
| `DC-SRC-DOC-002` | DCREQ196 | DC-TST-SRC-001 | PublishPage placeholder/retirement | DC-EVID-01 |
| `DC-SRC-DOC-003` | DCREQ196 | DC-TST-SRC-001 | SetPageDeadline placeholder/retirement | DC-EVID-01 |
| `DC-SRC-DOC-004` | DCREQ132, DCREQ198 | DC-TST-SRC-002, DC-TST-HIST-API-001 | GetPageHistory | DC-EVID-08, DC-EVID-01 |
| `DC-SRC-DOC-005` | DCREQ113, DCREQ197 | DC-TST-SRC-003, DC-TST-PMETA-API-001 | UpdatePage API/Application contract | DC-EVID-02, DC-EVID-20 |
| `DC-SRC-DOC-006` | DCREQ114, DCREQ115, DCREQ116, DCREQ199 | DC-TST-PVIS-* | PageVisibility vertical slice | DC-EVID-02 |
| `DC-SRC-DOC-007` | DCREQ032, DCREQ033, DCREQ034, DCREQ035, DCREQ191 | DC-TST-DAUTH-ARCH-001, DC-TST-SRC-005 | Documents request/action matrix | DC-EVID-10 |
| `DC-SRC-DOC-008` | DCREQ122, DCREQ123, DCREQ124, DCREQ125, DCREQ126, DCREQ127, DCREQ199 | DC-TST-LINK-* | ResourceLinks vertical slice | DC-EVID-07 |
| `DC-SRC-DOC-009` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ199 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | Version/Snapshot/history vertical slice | DC-EVID-08 |
| `DC-SRC-DOC-010` | DCREQ135, DCREQ138, DCREQ139, DCREQ199 | DC-TST-TPL-* | PageTemplate vertical slice | DC-EVID-09 |
| `DC-SRC-COL-001` | DCREQ185, DCREQ186, DCREQ187, DCREQ188, DCREQ198 | DC-TST-SRC-004, DC-TST-ACT-* | existing Activity projection → GetResourceActivity | DC-EVID-18 |
| `DC-SRC-COL-002` | DCREQ153, DCREQ154, DCREQ155, DCREQ156, DCREQ157 | DC-TST-MEN-* | Mention lifecycle | DC-EVID-13 |
| `DC-SRC-COL-003` | DCREQ158, DCREQ159, DCREQ160, DCREQ161, DCREQ199 | DC-TST-REACT-* | Reaction vertical slice | DC-EVID-14 |
| `DC-SRC-COL-004` | DCREQ167, DCREQ168, DCREQ169, DCREQ170, DCREQ199 | DC-TST-PRES-* | Presence vertical slice | DC-EVID-16 |
| `DC-SRC-COL-005` | DCREQ171, DCREQ172, DCREQ173, DCREQ174, DCREQ199 | DC-TST-READ-* | ReadState vertical slice | DC-EVID-17 |
| `DC-SRC-COL-006` | DCREQ175, DCREQ176, DCREQ177, DCREQ178, DCREQ199 | DC-TST-WATCH-* | Watcher vertical slice | DC-EVID-17 |
| `DC-SRC-COL-007` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212, DCREQ200 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing durable Notification records/runtime → Application/API | DC-EVID-18 |
| `DC-SRC-COL-008` | DCREQ036, DCREQ037, DCREQ045, DCREQ147, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-* | Comment lifecycle surface | DC-EVID-11 |
| `DC-SRC-COL-009` | DCREQ044, DCREQ045, DCREQ046, DCREQ107, DCREQ192 | DC-TST-CAUTH-ARCH-001, DC-TST-SRC-005 | Collaboration request/resource/action matrix | DC-EVID-19 |
| `DC-SRC-COL-010` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment target/upload contract | DC-EVID-15 |
| `DC-INV-001` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-002` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-003` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-004` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-005` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-006` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-007` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-008` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-009` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-INV-010` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-INV-*, DC-TST-TRACE-001 | Domain/Application/API/Infrastructure/tests/event inventories | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-001` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-002` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-003` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-004` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-005` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-006` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-007` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-SCOPE-008` | DCREQ104, DCREQ195, DCREQ196, DCREQ199, DCREQ200, DCREQ201, DCREQ202, DCREQ203, DCREQ204, DCREQ205, DCREQ206, DCREQ207, DCREQ208, DCREQ209, DCREQ210, DCREQ211, DCREQ212, DCREQ213, DCREQ214, DCREQ215, DCREQ216, DCREQ217, DCREQ218, DCREQ219, DCREQ220 | DC-TST-INV-*, DC-TST-TRACE-001 | release-scope/source-debt classification | DC-EVID-01, DC-EVID-28 |
| `DC-OWN-001` | DCREQ067, DCREQ079, DCREQ080, DCREQ101, DCREQ102, DCREQ103, DCREQ179, DCREQ185 | DC-TST-OWN-*, DC-TST-ARCH-*, DC-TST-NOTIF-ARCH-001, DC-TST-ACT-ARCH-001 | bounded-context ownership / public boundaries | DC-EVID-18, DC-EVID-27 |
| `DC-OWN-002` | DCREQ067, DCREQ079, DCREQ080, DCREQ101, DCREQ102, DCREQ103, DCREQ179, DCREQ185 | DC-TST-OWN-*, DC-TST-ARCH-*, DC-TST-NOTIF-ARCH-001, DC-TST-ACT-ARCH-001 | bounded-context ownership / public boundaries | DC-EVID-18, DC-EVID-27 |
| `DC-OWN-003` | DCREQ067, DCREQ079, DCREQ080, DCREQ101, DCREQ102, DCREQ103, DCREQ179, DCREQ185 | DC-TST-OWN-*, DC-TST-ARCH-*, DC-TST-NOTIF-ARCH-001, DC-TST-ACT-ARCH-001 | bounded-context ownership / public boundaries | DC-EVID-18, DC-EVID-27 |
| `DC-OWN-004` | DCREQ067, DCREQ079, DCREQ080, DCREQ101, DCREQ102, DCREQ103, DCREQ179, DCREQ185 | DC-TST-OWN-*, DC-TST-ARCH-*, DC-TST-NOTIF-ARCH-001, DC-TST-ACT-ARCH-001 | bounded-context ownership / public boundaries | DC-EVID-18, DC-EVID-27 |
| `DC-OWN-005` | DCREQ067, DCREQ079, DCREQ080, DCREQ101, DCREQ102, DCREQ103, DCREQ179, DCREQ185 | DC-TST-OWN-*, DC-TST-ARCH-*, DC-TST-NOTIF-ARCH-001, DC-TST-ACT-ARCH-001 | bounded-context ownership / public boundaries | DC-EVID-18, DC-EVID-27 |
| `DC-UP-001` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ111, DCREQ112 | DC-TST-TEN-*, DC-TST-RLS-*, DC-TST-DEDUP-001, DC-TST-POISON-001 | upstream contracts / Platform | DC-EVID-21, DC-EVID-22, DC-EVID-25 |
| `DC-UP-002` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ111, DCREQ112 | DC-TST-TEN-*, DC-TST-RLS-*, DC-TST-DEDUP-001, DC-TST-POISON-001 | upstream contracts / Platform | DC-EVID-21, DC-EVID-22, DC-EVID-25 |
| `DC-UP-003` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ111, DCREQ112 | DC-TST-TEN-*, DC-TST-RLS-*, DC-TST-DEDUP-001, DC-TST-POISON-001 | upstream contracts / Platform | DC-EVID-21, DC-EVID-22, DC-EVID-25 |
| `DC-UP-004` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ111, DCREQ112 | DC-TST-TEN-*, DC-TST-RLS-*, DC-TST-DEDUP-001, DC-TST-POISON-001 | upstream contracts / Platform | DC-EVID-21, DC-EVID-22, DC-EVID-25 |
| `DC-PAGE-001` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PAGE-002` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PAGE-003` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PAGE-004` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PAGE-005` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PAGE-006` | DCREQ001, DCREQ002, DCREQ003, DCREQ004, DCREQ005, DCREQ113, DCREQ114, DCREQ115, DCREQ116, DCREQ197, DCREQ220 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-SRC-003 | Page Domain/Application/API/persistence | DC-EVID-02 |
| `DC-PATH-001` | DCREQ201 | DC-TST-PATH-* | Page query/path projection | DC-EVID-02, DC-EVID-20 |
| `DC-DUP-001` | DCREQ202 | DC-TST-DUP-* | Page duplication use case if released | DC-EVID-02, DC-EVID-23 |
| `DC-HIER-001` | DCREQ006, DCREQ007, DCREQ008, DCREQ009, DCREQ010, DCREQ034 | DC-TST-HIER-* | MovePage / Page hierarchy | DC-EVID-03 |
| `DC-HIER-002` | DCREQ006, DCREQ007, DCREQ008, DCREQ009, DCREQ010, DCREQ034 | DC-TST-HIER-* | MovePage / Page hierarchy | DC-EVID-03 |
| `DC-HIER-003` | DCREQ006, DCREQ007, DCREQ008, DCREQ009, DCREQ010, DCREQ034 | DC-TST-HIER-* | MovePage / Page hierarchy | DC-EVID-03 |
| `DC-HIER-004` | DCREQ006, DCREQ007, DCREQ008, DCREQ009, DCREQ010, DCREQ034 | DC-TST-HIER-* | MovePage / Page hierarchy | DC-EVID-03 |
| `DC-BLK-001` | DCREQ011, DCREQ012, DCREQ013, DCREQ014, DCREQ015, DCREQ016, DCREQ017 | DC-TST-BLOCK-* | Block Domain/Application/API | DC-EVID-04 |
| `DC-BLK-002` | DCREQ011, DCREQ012, DCREQ013, DCREQ014, DCREQ015, DCREQ016, DCREQ017 | DC-TST-BLOCK-* | Block Domain/Application/API | DC-EVID-04 |
| `DC-BLK-003` | DCREQ011, DCREQ012, DCREQ013, DCREQ014, DCREQ015, DCREQ016, DCREQ017 | DC-TST-BLOCK-* | Block Domain/Application/API | DC-EVID-04 |
| `DC-BLK-004` | DCREQ011, DCREQ012, DCREQ013, DCREQ014, DCREQ015, DCREQ016, DCREQ017 | DC-TST-BLOCK-* | Block Domain/Application/API | DC-EVID-04 |
| `DC-BTREE-001` | DCREQ117, DCREQ118, DCREQ119, DCREQ120, DCREQ121 | DC-TST-BTREE-* | Block ancestry / move / delete-child semantics | DC-EVID-05 |
| `DC-BTREE-002` | DCREQ117, DCREQ118, DCREQ119, DCREQ120, DCREQ121 | DC-TST-BTREE-* | Block ancestry / move / delete-child semantics | DC-EVID-05 |
| `DC-BTREE-003` | DCREQ117, DCREQ118, DCREQ119, DCREQ120, DCREQ121 | DC-TST-BTREE-* | Block ancestry / move / delete-child semantics | DC-EVID-05 |
| `DC-BTREE-004` | DCREQ117, DCREQ118, DCREQ119, DCREQ120, DCREQ121 | DC-TST-BTREE-* | Block ancestry / move / delete-child semantics | DC-EVID-05 |
| `DC-BTREE-005` | DCREQ117, DCREQ118, DCREQ119, DCREQ120, DCREQ121 | DC-TST-BTREE-* | Block ancestry / move / delete-child semantics | DC-EVID-05 |
| `DC-ORD-001` | DCREQ018, DCREQ019, DCREQ020, DCREQ021, DCREQ022 | DC-TST-ORDER-* | fractional ordering / persistence | DC-EVID-06 |
| `DC-ORD-002` | DCREQ018, DCREQ019, DCREQ020, DCREQ021, DCREQ022 | DC-TST-ORDER-* | fractional ordering / persistence | DC-EVID-06 |
| `DC-ORD-003` | DCREQ018, DCREQ019, DCREQ020, DCREQ021, DCREQ022 | DC-TST-ORDER-* | fractional ordering / persistence | DC-EVID-06 |
| `DC-ORD-004` | DCREQ018, DCREQ019, DCREQ020, DCREQ021, DCREQ022 | DC-TST-ORDER-* | fractional ordering / persistence | DC-EVID-06 |
| `DC-EDIT-001` | DCREQ027, DCREQ028, DCREQ029, DCREQ030, DCREQ031, DCREQ136, DCREQ137 | DC-TST-EDIT-* | editor mutation/concurrency | DC-EVID-06 |
| `DC-EDIT-002` | DCREQ027, DCREQ028, DCREQ029, DCREQ030, DCREQ031, DCREQ136, DCREQ137 | DC-TST-EDIT-* | editor mutation/concurrency | DC-EVID-06 |
| `DC-DOC-QRY-001` | DCREQ023, DCREQ024, DCREQ025, DCREQ026 | DC-TST-DQRY-* | Documents queries/API | DC-EVID-20 |
| `DC-DOC-QRY-002` | DCREQ023, DCREQ024, DCREQ025, DCREQ026 | DC-TST-DQRY-* | Documents queries/API | DC-EVID-20 |
| `DC-DOC-QRY-003` | DCREQ023, DCREQ024, DCREQ025, DCREQ026 | DC-TST-DQRY-* | Documents queries/API | DC-EVID-20 |
| `DC-DOC-SRCH-001` | DCREQ140, DCREQ141 | DC-TST-SEARCH-* | Documents search boundary | DC-EVID-20 |
| `DC-LINK-001` | DCREQ122, DCREQ123, DCREQ124, DCREQ125, DCREQ126, DCREQ127 | DC-TST-LINK-* | ResourceLink Domain/Application/API | DC-EVID-07 |
| `DC-LINK-002` | DCREQ122, DCREQ123, DCREQ124, DCREQ125, DCREQ126, DCREQ127 | DC-TST-LINK-* | ResourceLink Domain/Application/API | DC-EVID-07 |
| `DC-LINK-003` | DCREQ122, DCREQ123, DCREQ124, DCREQ125, DCREQ126, DCREQ127 | DC-TST-LINK-* | ResourceLink Domain/Application/API | DC-EVID-07 |
| `DC-LINK-004` | DCREQ122, DCREQ123, DCREQ124, DCREQ125, DCREQ126, DCREQ127 | DC-TST-LINK-* | ResourceLink Domain/Application/API | DC-EVID-07 |
| `DC-HIST-001` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ136, DCREQ137, DCREQ194, DCREQ198 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | DocumentVersion/Snapshot/history | DC-EVID-08 |
| `DC-HIST-002` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ136, DCREQ137, DCREQ194, DCREQ198 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | DocumentVersion/Snapshot/history | DC-EVID-08 |
| `DC-HIST-003` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ136, DCREQ137, DCREQ194, DCREQ198 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | DocumentVersion/Snapshot/history | DC-EVID-08 |
| `DC-HIST-004` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ136, DCREQ137, DCREQ194, DCREQ198 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | DocumentVersion/Snapshot/history | DC-EVID-08 |
| `DC-HIST-005` | DCREQ128, DCREQ129, DCREQ130, DCREQ131, DCREQ132, DCREQ133, DCREQ134, DCREQ136, DCREQ137, DCREQ194, DCREQ198 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* | DocumentVersion/Snapshot/history | DC-EVID-08 |
| `DC-TPL-001` | DCREQ135, DCREQ138, DCREQ139, DCREQ194 | DC-TST-TPL-* | PageTemplate vertical slice | DC-EVID-09 |
| `DC-TPL-002` | DCREQ135, DCREQ138, DCREQ139, DCREQ194 | DC-TST-TPL-* | PageTemplate vertical slice | DC-EVID-09 |
| `DC-TPL-003` | DCREQ135, DCREQ138, DCREQ139, DCREQ194 | DC-TST-TPL-* | PageTemplate vertical slice | DC-EVID-09 |
| `DC-IO-001` | DCREQ142, DCREQ143, DCREQ203 | DC-TST-IMP-*, DC-TST-EXP-* | import/export adapters | DC-EVID-09 |
| `DC-IO-002` | DCREQ142, DCREQ143, DCREQ203 | DC-TST-IMP-*, DC-TST-EXP-* | import/export adapters | DC-EVID-09 |
| `DC-FILE-001` | DCREQ144, DCREQ145, DCREQ146 | DC-TST-FILE-* | file/media reference boundary | DC-EVID-09, DC-EVID-25 |
| `DC-DOC-INT-001` | DCREQ203 | DC-TST-X-DOC-INT-* | Documents integration adapters/public operations | DC-EVID-27 |
| `DC-DOC-RET-001` | DCREQ204 | DC-TST-RET-DOC-* | retention/export/purge across Documents copies | DC-EVID-23, DC-EVID-25 |
| `DC-MOB-001` | DCREQ205 | DC-TST-COMPAT-MOB-001 | API/content compatibility | DC-EVID-20 |
| `DC-OFF-001` | DCREQ206 | DC-TST-OFF-001 | offline release classification/protocol | DC-EVID-22 |
| `DC-A11Y-001` | DCREQ207 | DC-TST-CONTENT-A11Y-001 | Block schema/contracts | DC-EVID-04 |
| `DC-DOC-AUTH-001` | DCREQ032, DCREQ033, DCREQ034, DCREQ035, DCREQ107, DCREQ109, DCREQ191 | DC-TST-DAUTH-* | Documents request descriptors / policy engine | DC-EVID-10 |
| `DC-DOC-AUTH-002` | DCREQ032, DCREQ033, DCREQ034, DCREQ035, DCREQ107, DCREQ109, DCREQ191 | DC-TST-DAUTH-* | Documents request descriptors / policy engine | DC-EVID-10 |
| `DC-DOC-AUTH-003` | DCREQ032, DCREQ033, DCREQ034, DCREQ035, DCREQ107, DCREQ109, DCREQ191 | DC-TST-DAUTH-* | Documents request descriptors / policy engine | DC-EVID-10 |
| `DC-DOC-TEN-001` | DCREQ072, DCREQ073, DCREQ108 | DC-TST-TEN-DOC-001 | Documents runtime/DB | DC-EVID-10, DC-EVID-25 |
| `DC-DOC-RLS-001` | DCREQ074 | DC-TST-RLS-DOC-001 | PostgreSQL RLS | DC-EVID-10, DC-EVID-25 |
| `DC-COM-001` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-002` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-003` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-004` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-005` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-006` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COM-007` | DCREQ036, DCREQ037, DCREQ038, DCREQ039, DCREQ045, DCREQ147, DCREQ148, DCREQ149, DCREQ150, DCREQ151, DCREQ152, DCREQ214, DCREQ215 | DC-TST-COM-*, DC-TST-CQRY-* | Comment Domain/Application/API | DC-EVID-11 |
| `DC-COL-OPT-001` | DCREQ219 | DC-TST-COL-OPT-001 | optimistic Collaboration client/server contract | DC-EVID-11, DC-EVID-22 |
| `DC-TGT-001` | DCREQ040, DCREQ041, DCREQ042, DCREQ043, DCREQ044, DCREQ046, DCREQ047 | DC-TST-TGT-* | target public fact adapters | DC-EVID-12 |
| `DC-TGT-002` | DCREQ040, DCREQ041, DCREQ042, DCREQ043, DCREQ044, DCREQ046, DCREQ047 | DC-TST-TGT-* | target public fact adapters | DC-EVID-12 |
| `DC-TGT-003` | DCREQ040, DCREQ041, DCREQ042, DCREQ043, DCREQ044, DCREQ046, DCREQ047 | DC-TST-TGT-* | target public fact adapters | DC-EVID-12 |
| `DC-TGT-004` | DCREQ040, DCREQ041, DCREQ042, DCREQ043, DCREQ044, DCREQ046, DCREQ047 | DC-TST-TGT-* | target public fact adapters | DC-EVID-12 |
| `DC-MEN-001` | DCREQ153, DCREQ154, DCREQ155, DCREQ156, DCREQ157 | DC-TST-MEN-* | Mention Application/persistence/consumer | DC-EVID-13 |
| `DC-MEN-002` | DCREQ153, DCREQ154, DCREQ155, DCREQ156, DCREQ157 | DC-TST-MEN-* | Mention Application/persistence/consumer | DC-EVID-13 |
| `DC-MEN-003` | DCREQ153, DCREQ154, DCREQ155, DCREQ156, DCREQ157 | DC-TST-MEN-* | Mention Application/persistence/consumer | DC-EVID-13 |
| `DC-MEN-004` | DCREQ153, DCREQ154, DCREQ155, DCREQ156, DCREQ157 | DC-TST-MEN-* | Mention Application/persistence/consumer | DC-EVID-13 |
| `DC-REACT-001` | DCREQ158, DCREQ159, DCREQ160, DCREQ161 | DC-TST-REACT-* | Reaction vertical slice | DC-EVID-14 |
| `DC-REACT-002` | DCREQ158, DCREQ159, DCREQ160, DCREQ161 | DC-TST-REACT-* | Reaction vertical slice | DC-EVID-14 |
| `DC-REACT-003` | DCREQ158, DCREQ159, DCREQ160, DCREQ161 | DC-TST-REACT-* | Reaction vertical slice | DC-EVID-14 |
| `DC-ATT-001` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment vertical slice / storage boundary | DC-EVID-15 |
| `DC-ATT-002` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment vertical slice / storage boundary | DC-EVID-15 |
| `DC-ATT-003` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment vertical slice / storage boundary | DC-EVID-15 |
| `DC-ATT-004` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment vertical slice / storage boundary | DC-EVID-15 |
| `DC-ATT-005` | DCREQ162, DCREQ163, DCREQ164, DCREQ165, DCREQ166, DCREQ208 | DC-TST-ATT-* | Attachment vertical slice / storage boundary | DC-EVID-15 |
| `DC-PRES-001` | DCREQ167, DCREQ168, DCREQ169, DCREQ170, DCREQ209 | DC-TST-PRES-* | Presence/cursor/typing | DC-EVID-16, DC-EVID-22 |
| `DC-PRES-002` | DCREQ167, DCREQ168, DCREQ169, DCREQ170, DCREQ209 | DC-TST-PRES-* | Presence/cursor/typing | DC-EVID-16, DC-EVID-22 |
| `DC-PRES-003` | DCREQ167, DCREQ168, DCREQ169, DCREQ170, DCREQ209 | DC-TST-PRES-* | Presence/cursor/typing | DC-EVID-16, DC-EVID-22 |
| `DC-PRES-004` | DCREQ167, DCREQ168, DCREQ169, DCREQ170, DCREQ209 | DC-TST-PRES-* | Presence/cursor/typing | DC-EVID-16, DC-EVID-22 |
| `DC-READ-001` | DCREQ171, DCREQ172, DCREQ173, DCREQ174 | DC-TST-READ-* | ReadState vertical slice | DC-EVID-17 |
| `DC-READ-002` | DCREQ171, DCREQ172, DCREQ173, DCREQ174 | DC-TST-READ-* | ReadState vertical slice | DC-EVID-17 |
| `DC-READ-003` | DCREQ171, DCREQ172, DCREQ173, DCREQ174 | DC-TST-READ-* | ReadState vertical slice | DC-EVID-17 |
| `DC-WATCH-001` | DCREQ175, DCREQ176, DCREQ177, DCREQ178 | DC-TST-WATCH-* | Watcher vertical slice | DC-EVID-17 |
| `DC-WATCH-002` | DCREQ175, DCREQ176, DCREQ177, DCREQ178 | DC-TST-WATCH-* | Watcher vertical slice | DC-EVID-17 |
| `DC-WATCH-003` | DCREQ175, DCREQ176, DCREQ177, DCREQ178 | DC-TST-WATCH-* | Watcher vertical slice | DC-EVID-17 |
| `DC-NOTIF-001` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-NOTIF-002` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-NOTIF-003` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-NOTIF-004` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-NOTIF-005` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-NOTIF-006` | DCREQ179, DCREQ180, DCREQ181, DCREQ182, DCREQ183, DCREQ184, DCREQ210, DCREQ211, DCREQ212 | DC-TST-NOTIF-*, DC-TST-MEN-IDEM-001 | existing Notification records/runtime + Application/API | DC-EVID-18 |
| `DC-ACT-001` | DCREQ185, DCREQ186, DCREQ187, DCREQ188, DCREQ198 | DC-TST-ACT-*, DC-TST-SRC-004 | existing Activity projection + query | DC-EVID-18 |
| `DC-ACT-002` | DCREQ185, DCREQ186, DCREQ187, DCREQ188, DCREQ198 | DC-TST-ACT-*, DC-TST-SRC-004 | existing Activity projection + query | DC-EVID-18 |
| `DC-ACT-003` | DCREQ185, DCREQ186, DCREQ187, DCREQ188, DCREQ198 | DC-TST-ACT-*, DC-TST-SRC-004 | existing Activity projection + query | DC-EVID-18 |
| `DC-ACT-004` | DCREQ185, DCREQ186, DCREQ187, DCREQ188, DCREQ198 | DC-TST-ACT-*, DC-TST-SRC-004 | existing Activity projection + query | DC-EVID-18 |
| `DC-COL-SRCH-001` | DCREQ217 | DC-TST-COL-SEARCH-001 | Collaboration search/query projection | DC-EVID-18, DC-EVID-20 |
| `DC-COL-AUTH-001` | DCREQ044, DCREQ045, DCREQ046, DCREQ107, DCREQ192 | DC-TST-CAUTH-* | Collaboration request/resource/action matrix | DC-EVID-19 |
| `DC-COL-AUTH-002` | DCREQ044, DCREQ045, DCREQ046, DCREQ107, DCREQ192 | DC-TST-CAUTH-* | Collaboration request/resource/action matrix | DC-EVID-19 |
| `DC-COL-AUTH-003` | DCREQ044, DCREQ045, DCREQ046, DCREQ107, DCREQ192 | DC-TST-CAUTH-* | Collaboration request/resource/action matrix | DC-EVID-19 |
| `DC-COL-TEN-001` | DCREQ072, DCREQ073, DCREQ108 | DC-TST-TEN-COL-001 | Collaboration runtime/DB | DC-EVID-19, DC-EVID-25 |
| `DC-RET-001` | DCREQ048, DCREQ049, DCREQ050, DCREQ189, DCREQ204, DCREQ213, DCREQ214, DCREQ215 | DC-TST-RET-* | retention/deletion/anonymization workflows | DC-EVID-19, DC-EVID-23, DC-EVID-25 |
| `DC-RET-002` | DCREQ048, DCREQ049, DCREQ050, DCREQ189, DCREQ204, DCREQ213, DCREQ214, DCREQ215 | DC-TST-RET-* | retention/deletion/anonymization workflows | DC-EVID-19, DC-EVID-23, DC-EVID-25 |
| `DC-RET-003` | DCREQ048, DCREQ049, DCREQ050, DCREQ189, DCREQ204, DCREQ213, DCREQ214, DCREQ215 | DC-TST-RET-* | retention/deletion/anonymization workflows | DC-EVID-19, DC-EVID-23, DC-EVID-25 |
| `DC-EVT-001` | DCREQ054, DCREQ055, DCREQ056, DCREQ057, DCREQ058, DCREQ193 | DC-TST-EVT-* | Domain/integration events/outbox | DC-EVID-21 |
| `DC-EVT-002` | DCREQ054, DCREQ055, DCREQ056, DCREQ057, DCREQ058, DCREQ193 | DC-TST-EVT-* | Domain/integration events/outbox | DC-EVID-21 |
| `DC-EVT-003` | DCREQ054, DCREQ055, DCREQ056, DCREQ057, DCREQ058, DCREQ193 | DC-TST-EVT-* | Domain/integration events/outbox | DC-EVID-21 |
| `DC-EVT-004` | DCREQ054, DCREQ055, DCREQ056, DCREQ057, DCREQ058, DCREQ193 | DC-TST-EVT-* | Domain/integration events/outbox | DC-EVID-21 |
| `DC-X-AUT-001` | DCREQ069 | DC-TST-X-AUT-001 | Automation public handoff | DC-EVID-27 |
| `DC-X-ANA-001` | DCREQ070 | DC-TST-X-ANA-001 | Analytics public handoff | DC-EVID-27 |
| `DC-X-WM-001` | DCREQ067, DCREQ068 | DC-TST-X-WM-READ-001 | WorkManagement↔Collaboration adapter | DC-EVID-27 |
| `DC-X-INT-001` | DCREQ218 | DC-TST-X-COL-INT-001 | Collaboration provider integration | DC-EVID-27 |
| `DC-RT-001` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-002` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-003` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-004` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-005` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-006` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-RT-007` | DCREQ059, DCREQ060, DCREQ061, DCREQ062, DCREQ063, DCREQ064, DCREQ065, DCREQ066, DCREQ190, DCREQ206, DCREQ219 | DC-TST-RT-*, DC-TST-PRES-RT-001 | Platform realtime + context reconciliation | DC-EVID-22 |
| `DC-MIG-001` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-002` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-003` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-004` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-005` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-006` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-MIG-007` | DCREQ083, DCREQ084, DCREQ085, DCREQ086, DCREQ087, DCREQ194 | DC-TST-MIG-* | EF migrations / compatibility fixtures | DC-EVID-23 |
| `DC-REL-001` | DCREQ088, DCREQ089, DCREQ090, DCREQ091, DCREQ092, DCREQ110, DCREQ111, DCREQ112 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 | transaction/messaging/runtime failure paths | DC-EVID-24 |
| `DC-REL-002` | DCREQ088, DCREQ089, DCREQ090, DCREQ091, DCREQ092, DCREQ110, DCREQ111, DCREQ112 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 | transaction/messaging/runtime failure paths | DC-EVID-24 |
| `DC-REL-003` | DCREQ088, DCREQ089, DCREQ090, DCREQ091, DCREQ092, DCREQ110, DCREQ111, DCREQ112 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 | transaction/messaging/runtime failure paths | DC-EVID-24 |
| `DC-REL-004` | DCREQ088, DCREQ089, DCREQ090, DCREQ091, DCREQ092, DCREQ110, DCREQ111, DCREQ112 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 | transaction/messaging/runtime failure paths | DC-EVID-24 |
| `DC-REL-005` | DCREQ088, DCREQ089, DCREQ090, DCREQ091, DCREQ092, DCREQ110, DCREQ111, DCREQ112 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 | transaction/messaging/runtime failure paths | DC-EVID-24 |
| `DC-SEC-001` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ076, DCREQ077, DCREQ078, DCREQ216 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-* | security/privacy/abuse controls | DC-EVID-25 |
| `DC-SEC-002` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ076, DCREQ077, DCREQ078, DCREQ216 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-* | security/privacy/abuse controls | DC-EVID-25 |
| `DC-SEC-003` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ076, DCREQ077, DCREQ078, DCREQ216 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-* | security/privacy/abuse controls | DC-EVID-25 |
| `DC-SEC-004` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ076, DCREQ077, DCREQ078, DCREQ216 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-* | security/privacy/abuse controls | DC-EVID-25 |
| `DC-SEC-005` | DCREQ072, DCREQ073, DCREQ074, DCREQ075, DCREQ076, DCREQ077, DCREQ078, DCREQ216 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-* | security/privacy/abuse controls | DC-EVID-25 |
| `DC-PERF-001` | DCREQ096, DCREQ097, DCREQ098, DCREQ099 | DC-TST-PERF-* | performance harness | DC-EVID-26 |
| `DC-PERF-002` | DCREQ096, DCREQ097, DCREQ098, DCREQ099 | DC-TST-PERF-* | performance harness | DC-EVID-26 |
| `DC-OBS-001` | DCREQ093, DCREQ094, DCREQ095 | DC-TST-OBS-* | logs/metrics/recovery telemetry | DC-EVID-26 |
| `DC-OBS-002` | DCREQ093, DCREQ094, DCREQ095 | DC-TST-OBS-* | logs/metrics/recovery telemetry | DC-EVID-26 |
| `DC-XINT-001` | DCREQ067, DCREQ068, DCREQ069, DCREQ070, DCREQ071, DCREQ090, DCREQ218 | DC-TST-X-* | production DI cross-context paths | DC-EVID-27 |
| `DC-XINT-002` | DCREQ067, DCREQ068, DCREQ069, DCREQ070, DCREQ071, DCREQ090, DCREQ218 | DC-TST-X-* | production DI cross-context paths | DC-EVID-27 |
| `DC-XINT-003` | DCREQ067, DCREQ068, DCREQ069, DCREQ070, DCREQ071, DCREQ090, DCREQ218 | DC-TST-X-* | production DI cross-context paths | DC-EVID-27 |
| `DC-XINT-004` | DCREQ067, DCREQ068, DCREQ069, DCREQ070, DCREQ071, DCREQ090, DCREQ218 | DC-TST-X-* | production DI cross-context paths | DC-EVID-27 |
| `DC-XINT-005` | DCREQ067, DCREQ068, DCREQ069, DCREQ070, DCREQ071, DCREQ090, DCREQ218 | DC-TST-X-* | production DI cross-context paths | DC-EVID-27 |
| `DC-TEST-HO-001` | DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | TESTS artifact / traceability | DC-EVID-28 |
| `DC-TEST-HO-002` | DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | TESTS artifact / traceability | DC-EVID-28 |
| `DC-TEST-HO-003` | DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | TESTS artifact / traceability | DC-EVID-28 |
| `DC-DOC-HO-001` | DCREQ057, DCREQ088, DCREQ197, DCREQ200 | DC-TST-TRACE-001, OpenAPI/event drift gates | OpenAPI / event manifest / Product docs | DC-EVID-20, DC-EVID-21, DC-EVID-28 |
| `DC-DOC-HO-002` | DCREQ057, DCREQ088, DCREQ197, DCREQ200 | DC-TST-TRACE-001, OpenAPI/event drift gates | OpenAPI / event manifest / Product docs | DC-EVID-20, DC-EVID-21, DC-EVID-28 |
| `DC-DOC-HO-003` | DCREQ057, DCREQ088, DCREQ197, DCREQ200 | DC-TST-TRACE-001, OpenAPI/event drift gates | OpenAPI / event manifest / Product docs | DC-EVID-20, DC-EVID-21, DC-EVID-28 |
| `DC-CERT-HO-001` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | CERTIFICATION artifact | DC-EVID-28 |
| `DC-CERT-HO-002` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | CERTIFICATION artifact | DC-EVID-28 |
| `DC-CERT-HO-003` | DCREQ104, DCREQ105, DCREQ106, DCREQ195, DCREQ199, DCREQ200 | DC-TST-TRACE-001 | CERTIFICATION artifact | DC-EVID-28 |

---

## 249C. Explicit SPEC coverage index

The PLAN claims traceability for exactly:

```text
DCREQ001 DCREQ002 DCREQ003 DCREQ004 DCREQ005 DCREQ006 DCREQ007 DCREQ008 DCREQ009 DCREQ010
DCREQ011 DCREQ012 DCREQ013 DCREQ014 DCREQ015 DCREQ016 DCREQ017 DCREQ018 DCREQ019 DCREQ020
DCREQ021 DCREQ022 DCREQ023 DCREQ024 DCREQ025 DCREQ026 DCREQ027 DCREQ028 DCREQ029 DCREQ030
DCREQ031 DCREQ032 DCREQ033 DCREQ034 DCREQ035 DCREQ036 DCREQ037 DCREQ038 DCREQ039 DCREQ040
DCREQ041 DCREQ042 DCREQ043 DCREQ044 DCREQ045 DCREQ046 DCREQ047 DCREQ048 DCREQ049 DCREQ050
DCREQ051 DCREQ052 DCREQ053 DCREQ054 DCREQ055 DCREQ056 DCREQ057 DCREQ058 DCREQ059 DCREQ060
DCREQ061 DCREQ062 DCREQ063 DCREQ064 DCREQ065 DCREQ066 DCREQ067 DCREQ068 DCREQ069 DCREQ070
DCREQ071 DCREQ072 DCREQ073 DCREQ074 DCREQ075 DCREQ076 DCREQ077 DCREQ078 DCREQ079 DCREQ080
DCREQ081 DCREQ082 DCREQ083 DCREQ084 DCREQ085 DCREQ086 DCREQ087 DCREQ088 DCREQ089 DCREQ090
DCREQ091 DCREQ092 DCREQ093 DCREQ094 DCREQ095 DCREQ096 DCREQ097 DCREQ098 DCREQ099 DCREQ100
DCREQ101 DCREQ102 DCREQ103 DCREQ104 DCREQ105 DCREQ106 DCREQ107 DCREQ108 DCREQ109 DCREQ110
DCREQ111 DCREQ112 DCREQ113 DCREQ114 DCREQ115 DCREQ116 DCREQ117 DCREQ118 DCREQ119 DCREQ120
DCREQ121 DCREQ122 DCREQ123 DCREQ124 DCREQ125 DCREQ126 DCREQ127 DCREQ128 DCREQ129 DCREQ130
DCREQ131 DCREQ132 DCREQ133 DCREQ134 DCREQ135 DCREQ136 DCREQ137 DCREQ138 DCREQ139 DCREQ140
DCREQ141 DCREQ142 DCREQ143 DCREQ144 DCREQ145 DCREQ146 DCREQ147 DCREQ148 DCREQ149 DCREQ150
DCREQ151 DCREQ152 DCREQ153 DCREQ154 DCREQ155 DCREQ156 DCREQ157 DCREQ158 DCREQ159 DCREQ160
DCREQ161 DCREQ162 DCREQ163 DCREQ164 DCREQ165 DCREQ166 DCREQ167 DCREQ168 DCREQ169 DCREQ170
DCREQ171 DCREQ172 DCREQ173 DCREQ174 DCREQ175 DCREQ176 DCREQ177 DCREQ178 DCREQ179 DCREQ180
DCREQ181 DCREQ182 DCREQ183 DCREQ184 DCREQ185 DCREQ186 DCREQ187 DCREQ188 DCREQ189 DCREQ190
DCREQ191 DCREQ192 DCREQ193 DCREQ194 DCREQ195 DCREQ196 DCREQ197 DCREQ198 DCREQ199 DCREQ200
DCREQ201 DCREQ202 DCREQ203 DCREQ204 DCREQ205 DCREQ206 DCREQ207 DCREQ208 DCREQ209 DCREQ210
DCREQ211 DCREQ212 DCREQ213 DCREQ214 DCREQ215 DCREQ216 DCREQ217 DCREQ218 DCREQ219 DCREQ220
```

The final traceability verification must prove:

```text
DCREQ001..DCREQ220
referenced: 220/220
```

Ranges or prose inference alone are not sufficient for automated traceability verification.

---

## 249D. Evidence-bundle handoff rule

TESTS must map executable results into all:

```text
DC-EVID-01..DC-EVID-28
```

CERTIFICATION consumes those bundles.

The required chain is:

```text
PLAN work unit
→ TEST
→ DC-EVID
→ CERTIFICATION gate
```

not:

```text
PLAN prose
→ manual reviewer inference
```

---

# Final implementation rules

## 249. No missing vertical slice by omission

A capability present in canonical product ownership and current Domain/persistence must be dispositioned even if there is no endpoint.

---

## 250. No placeholder completion

A route/handler is not implemented because it returns success or compiles.

Examples explicitly rejected:

```text
NotImplementedException
constant []
constant total=0
ignored request fields
stub/noop consumer
```

---

## 251. No action-name reuse by convenience

`ManageBoard`/`UpdateItem`/`ViewBoard` may not remain on unrelated resource kinds solely because the policy engine already understands them.

Resource/action semantics must be product-correct.

---

## 252. No cross-Page Block move

Ordinary Block structural mutation stays within one Page.

Any cross-Page UX requires dedicated transfer/copy semantics.

---

## 253. No Domain-only certification

Domain tests may prove invariants.

They do not prove:

```text
Application
API
authorization
tenant
persistence
migration
runtime integration
```

---

## 254. No full-scope claim with hidden deferred capability

If a canonical product-owned capability is `OUT_OF_RELEASE_SCOPE`, certification may close a narrower milestone but must not label the excluded capability complete.

---

## 255. Final execution sequence

For every capability:

```text
Product requirement
→ SPEC DCREQ
→ current source
→ source-debt/disposition
→ PLAN work unit
→ minimum implementation
→ TEST evidence
→ migration/security review
→ exact-SHA CERTIFICATION
```

Never:

```text
source file exists
→ implement whatever its name suggests
→ claim completion
```

---

## 256. Final PLAN completion criterion

This PLAN is complete when an implementation agent can execute the entire release scope without inventing, and when `DC-TST-TRACE-001` can mechanically prove the normative work-unit matrix is complete:

- product ownership;
- Page visibility semantics;
- Page/Block hierarchy semantics;
- Block cross-Page behavior;
- version/history meaning;
- template authority;
- ResourceLink ownership;
- Comment thread/anchor semantics;
- Mention lifecycle;
- Reaction uniqueness;
- Attachment target/download semantics;
- Presence durability;
- ReadState canonical boundary;
- Watcher behavior;
- Notification ownership;
- Activity/Audit relationship;
- permission-action mappings;
- retention;
- realtime recovery;
- migration compatibility.

If any of those must still be guessed:

```text
the affected work unit remains BLOCKED-DECISION
```


## 252A. DCSTOP016–026 execution mapping

The SPEC stop-condition authority is:

```text
DCSTOP001..DCSTOP026
```

The additional stop conditions introduced by the final authority patch map as follows:

| Stop condition | Execution owner | Mandatory TEST/evidence |
|---|---|---|
| DCSTOP016 — schema-less Block content | DC-BLK-* / DC-A11Y-001 | DC-TST-BLOCK-CONTENT-001, DC-TST-CONTENT-A11Y-001 |
| DCSTOP017 — Page sharing leaks linked/private target | DC-LINK-* / DC-DOC-AUTH-* | DC-TST-LINK-AUTHZ-001, DC-TST-DAUTH-POL-001 |
| DCSTOP018 — history/concurrency versions conflated | DC-HIST-* / DC-EDIT-* | DC-TST-EDIT-CONC-001, DC-TST-HIST-POLICY-001 |
| DCSTOP019 — template mutation rewrites existing Pages | DC-TPL-* | DC-TST-TPL-INST-001, DC-TST-TPL-CONTENT-001 |
| DCSTOP020 — Mention identity unstable | DC-MEN-* | DC-TST-MEN-APP-001, DC-TST-MEN-DOM-001 |
| DCSTOP021 — Reaction retry duplicates | DC-REACT-* / DC-REL-* | DC-TST-REACT-INF-001, DC-TST-IDEM-001 |
| DCSTOP022 — unread count unrecoverable | DC-READ-* | DC-TST-READ-CONC-001, DC-TST-READ-QRY-001 |
| DCSTOP023 — Notification collapses into delivery transport | DC-NOTIF-* | DC-TST-NOTIF-ARCH-001, DC-TST-NOTIF-CHANNEL-001 |
| DCSTOP024 — Activity used as Governance Audit | DC-ACT-* | DC-TST-ACT-ARCH-001 |
| DCSTOP025 — Workspace deletion policy unresolved | DC-RET-003 | DC-TST-RET-WS-001 |
| DCSTOP026 — required abuse controls unresolved | DC-SEC-005 | DC-TST-SEC-ABUSE-001 |

Execution rule:

```text
a triggered DCSTOP
→ affected work unit becomes BLOCKED
→ affected certification gate cannot become VERIFIED/STABLE
```

No coding agent may downgrade a canonical stop condition to non-blocking debt without an explicit SPEC/Product authority change.

---

