---
document_id: WRK-TESTS-DOCUMENTS-COLLABORATION
document_type: workstream-tests
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
  - ci
evidence:
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.plan.md
  - docs/product/documents.md
  - docs/product/collaboration.md
  - docs/workstreams/teams/documents-collaboration.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/tests/
review_on:
  - documents-collaboration-spec-change
  - documents-collaboration-plan-change
  - source-debt-disposition-change
  - release-scope-change
  - authorization-action-change
  - page-contract-change
  - block-contract-change
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
  - realtime-recovery-change
  - migration-change
  - ci-gate-change
---

# TESTS — Documents & Collaboration

## 1. Purpose

This document is the executable verification contract for the complete Documents & Collaboration execution package.

It proves:

```text
Product requirement
→ SPEC requirement
→ PLAN work unit
→ source behavior
→ executable test
→ CI gate
→ certification evidence
```

It does not invent product behavior.

Product semantics come from canonical Product docs and SPEC.

Implementation order comes from PLAN.

Candidate PASS/STABLE status belongs only to CERTIFICATION after exact-SHA execution.

---

## 2. Verification scope

The test plan covers both:

```text
P4B Core
and
full release-scoped Documents & Collaboration
```

The test suite therefore distinguishes:

```text
CORE-REQUIRED
FULL-RELEASE-REQUIRED
OPTIONAL-IF-RELEASED
NOT-APPLICABLE-WITH-EVIDENCE
BLOCKED
```

A capability absent from API is not automatically `NOT_APPLICABLE`.

If Product owns it and source contains Domain/persistence, release scope must first be decided in PLAN.

---

## 3. Evidence principle

A capability is not verified because:

- a class exists;
- an EF configuration exists;
- Domain tests are green;
- an endpoint returns success;
- a test uses mocks;
- an old TAC/M7 record says complete.

Required proof must match the property being claimed.

Examples:

```text
Domain invariant
→ Domain test may be sufficient.

RLS
→ real PostgreSQL application-role test required.

cross-context target boundary
→ production adapter/integration test required.

realtime recovery
→ duplicate/reorder/gap/reconnect convergence test required.

migration compatibility
→ historical fixture + real migration/reader proof required.
```

---

## 3A. Product/team requirement namespace rule

This TESTS artifact follows the SPEC namespace normalization:

```text
PROD-DCT-001..031
→ canonical Documents Product requirements

PROD-COL-001..029
→ canonical Collaboration Product requirements

TEAM-DCT-001..012
TEAM-COL-001..008
→ team capability aliases only
```

Bare `DCT-*` / `COL-*` identifiers are not normative when the authority source is ambiguous.

Executable verification is keyed by:

```text
DCREQ*
DC-TST-*
DC-EVID-*
```

---

# Test taxonomy

## 4. T0 — static/compile/source gates

Used for:

```text
compilation
analyzers
source placeholders
forbidden dependency patterns
generated contract drift
```

---

## 5. T1 — Domain tests

Used for:

```text
aggregate/value-object invariants
lifecycle
scope
cycle prevention
no-op
Domain events
```

---

## 6. T2 — Application tests

Used for:

```text
command/query orchestration
validation
authorization descriptor
fact-port usage
error semantics
scope derivation
```

---

## 7. T3 — Infrastructure tests

Used for:

```text
EF mapping
indexes
constraints
RLS
ordering persistence
history/template storage
outbox
projection store
```

---

## 8. T4 — API tests

Used for:

```text
route
request/response
ignored-field protection
ProblemDetails
pagination
OpenAPI
403/404
conflict semantics
```

---

## 9. T5 — Architecture tests

Used for:

```text
layer direction
bounded-context ownership
private persistence isolation
request/action completeness
Public contract ownership
stub classification
```

---

## 10. T6 — Integration tests

Used for:

```text
production DI
real PostgreSQL
cross-context adapters
committed state
transaction rollback
outbox
RLS
```

---

## 11. T7 — Security tests

Used for:

```text
A→B tenant denial
target spoofing
permission revocation
existence disclosure
content leakage
signed URL safety
```

---

## 12. T8 — Concurrency tests

Used for:

```text
Page move races
Block tree/order races
stale writes
Reaction uniqueness races
ReadState monotonicity
idempotent retry
```

---

## 13. T9 — Migration/compatibility tests

Used for:

```text
clean DB
upgrade/fresh-baseline policy
pending-model
hierarchy/order conversion
Block live/history/template compatibility
target migration
```

---

## 14. T10 — Messaging/reliability tests

Used for:

```text
post-commit fact
outbox
dedup
poison
consumer retry
composite write rollback
```

---

## 15. T11 — Realtime/recovery tests

Used for:

```text
duplicate
out-of-order
gap
reconnect
permission revoke
durable-state convergence
ephemeral presence rebuild
```

---

## 16. T12 — Performance tests

Used for:

```text
Page tree
Block tree
history
Comment thread
Activity
Reaction/read-state/watch hot paths
```

---

## 17. T13 — Cross-context contract tests

Used for:

```text
Documents → Collaboration
WorkManagement → Collaboration
Collaboration → WorkManagement summary
Automation
Analytics
Identity recipient lookup
Governance authorization facts
```

---

# Existing evidence classification

## 18. Existing-test reuse rule

Current tests are candidate evidence only.

Each reused suite must be classified:

```text
EXISTING_SUFFICIENT
EXISTING_PARTIAL
EXISTING_STALE
DUPLICATE
NOT_RELEVANT
```

Certification records the final classification.

---

## 19. Existing Documents Domain evidence

Current baseline includes useful evidence such as:

```text
PageTests
PageHierarchyTests
PageRulesTests
PageTreeRulesTests

BlockTests
BlockContentValidatorTests
BlockHierarchyTests
BlockCreateChildScopeTests
BlockMoveFailureAtomicityTests
BlockTreeRulesTests
BlockPropertiesTests

ResourceLinkTests
ResourceLinkTenantTests

DocumentVersionTests
DocumentVersionImmutabilityTests
DocumentSnapshotTests

DocumentTemplateDefinitionTests
```

These may satisfy some Domain-level requirements.

They do not prove missing Application/API vertical slices.

---

## 20. Existing Collaboration Domain evidence

Current baseline includes:

```text
CommentTests
CommentReplyInvariantTests
CommentReplyScopeTests
CommentAnchorTests
CommentDeletionAtomicityTests
CommentWorkspaceScopeTests

MentionTests
MentionWorkspaceScopeTests

ReactionTests
ReactionDuplicateTests
ReactionWorkspaceScopeTests

AttachmentTests
AttachmentWorkspaceScopeTests
FileMetadataTests

PresenceExperimentalTests
PresenceIsolationTests

ReadStateMonotonicityTests

ResourceWatcherTests
ResourceWatcherWorkspaceScopeTests
```

Again:

```text
Domain proof != release-ready vertical slice
```

---

## 21. Existing integration evidence

Current useful integration evidence includes:

```text
PageCreatedOutboxEvidenceTests
PageArchivedOutboxEvidenceTests
CommentCreatedOutboxEvidenceTests
WorkManagementCollaborationReadAdapterTests
MentionCreatedNotificationRuntimeChainIntegrationTests
```

These are strong candidate evidence for:

```text
post-commit outbox
rollback
composite Comment+Mention atomicity
foreign target remains unchanged
Collaboration ResourceSummary read boundary

Mention
→ outbox
→ dispatcher
→ tenant restoration
→ dedup
→ durable Notification item/recipient persistence
```

The Notification runtime-chain evidence is especially relevant to:

```text
DC-TST-MEN-IDEM-001
DC-TST-NOTIF-FACT-001
DC-TST-NOTIF-IDEM-001
DC-TST-TEN-COL-001
```

but does not by itself prove complete Notification Application/API semantics.

---

## 22. Existing architecture evidence

Current useful architecture tests include:

```text
CollaborationReadBoundaryArchitectureTests
DocumentsCollaborationEventPinningArchitectureTests
NotificationsClassificationTests
LegacyCollabNotificationWriteBanTests
```

Reuse only where current semantics still match the candidate.

`LegacyCollabNotificationWriteBanTests` requires semantic review because its legacy naming/message may predate the current durable Notification projection model.

If the invariant remains valid, preserve the invariant and update stale wording rather than treating the historical test name as Product truth.

---

# Test ID convention

## 23. Canonical test ID

New execution-level IDs use:

```text
DC-TST-<AREA>-<LAYER>-<NNN>
```

Examples:

```text
DC-TST-PAGE-DOM-001
DC-TST-BTREE-CONC-001
DC-TST-HIST-INT-001
DC-TST-MEN-APP-001
DC-TST-PRES-RT-001
```

Existing concrete xUnit class/method names remain valid source evidence.

---

# Source-debt regression gates

## 24. DC-TST-SRC-001 — no certified NotImplemented handler

PLAN debt:

```text
DC-SRC-DOC-001
DC-SRC-DOC-002
DC-SRC-DOC-003
```

For every request that remains release-scoped:

```text
handler execution must not throw NotImplementedException
```

If `PublishPage` or `SetPageDeadline` is retired:

```text
route/contract/source retirement evidence replaces execution test
```

---

## 25. DC-TST-SRC-002 — no fake empty Page history

PLAN debt:

```text
DC-SRC-DOC-004
```

If history is released:

```text
seed actual history/version
→ query
→ non-empty canonical result
```

If history endpoint is retired/deferred:

```text
OpenAPI/API route must not claim completed product behavior
```

---

## 26. DC-TST-SRC-003 — no silently ignored Page update fields

PLAN debt:

```text
DC-SRC-DOC-005
```

For each field remaining in public request:

```text
valid supported value
→ durable/query-visible effect
```

or:

```text
unsupported value
→ explicit rejection
```

No successful silent discard.

---

## 27. DC-TST-SRC-004 — existing Activity projection reaches resource query

PLAN debt:

```text
DC-SRC-COL-001
```

If Activity is released:

```text
committed business fact
→ existing Activity projection consumer/store
→ GetResourceActivity
→ expected authorized item
```

The test fails if the endpoint still returns a hard-coded empty result.

It also fails if implementation creates a competing second Activity truth/store merely to satisfy the endpoint.

If the endpoint is retired/deferred, verify the public contract no longer claims the capability.

---

## 28. DC-TST-SRC-005 — request/resource/action drift gate

PLAN debt:

```text
DC-SRC-DOC-007
DC-SRC-COL-009
```

Architecture/Application test enumerates all protected Documents/Collaboration requests and requires each to appear in the accepted action matrix.

Collaboration coverage includes released:

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

New protected request with no classification fails.

---

# Page tests

## 29. DC-TST-PAGE-DOM-001 — stable identity

Requirements:

```text
DCREQ001
```

Rename/archive/restore/move must not replace Page ID.

Candidate existing evidence:

```text
PageTests
PageHierarchyTests
```

---

## 30. DC-TST-PAGE-SCOPE-001 — authoritative Account/Workspace

Requirements:

```text
DCREQ002
DCREQ072
DCREQ073
```

Cases:

```text
A→A allowed
A→B read denied
A→B mutate denied
forged route/body scope denied
```

Requires production request/tenant wiring for final proof.

---

## 31. DC-TST-PAGE-DOM-002 — archive != delete

Requirements:

```text
DCREQ003
DCREQ004
```

Assert separate lifecycle semantics and events.

---

## 32. DC-TST-PAGE-DOM-003 — restore/no-op lifecycle

Requirements:

```text
DCREQ003
DCREQ005
DCREQ137
```

Repeated archive/delete/restore no-op behavior must not create false version/event churn where current Domain defines no-op.

---

## 33. DC-TST-PAGE-RET-001 — Page delete downstream matrix

Requirements:

```text
DCREQ004
DCREQ189
```

For release-scoped dependencies record expected effects on:

```text
Blocks
ResourceLinks
history
Comments
search
realtime
```

This may be multiple integration tests.

---

# Page metadata/visibility

## 34. DC-TST-PMETA-API-001 — update contract parity

Requirements:

```text
DCREQ113
DCREQ197
```

For every public update field:

```text
request
→ Application
→ Domain/persistence
→ query response
```

must be consistent.

---

## 35. DC-TST-PVIS-DOM-001 — visibility values

Requirements:

```text
DCREQ114
```

Verify accepted persisted enum/value contract.

---

## 36. DC-TST-PVIS-AUTHZ-001 — visibility does not bypass Governance

Requirements:

```text
DCREQ115
DCREQ116
```

Representative:

```text
Public Page + restricted embedded target
→ Page visibility does not grant target access

Private/Restricted Page
→ member without permission denied
```

according to canonical policy.

---

## 37. DC-TST-PVIS-EVT-001 — governed visibility mutation

Requirements:

```text
DCREQ116
```

If release-scoped:

```text
authorized change
→ persisted
→ history/event/realtime invalidation as required
```

If not release-scoped:

```text
NOT_APPLICABLE
```

with source/API evidence.

---

# Page hierarchy

## 38. DC-TST-HIER-DOM-001 — cycle prevention

Requirements:

```text
DCREQ006
DCREQ007
```

Cases:

```text
self parent
ancestor under descendant
deep cycle
```

Candidate evidence:

```text
PageHierarchyTests
PageTreeRulesTests
```

---

## 39. DC-TST-HIER-APP-001 — MovePage real handler

Requirements:

```text
DCREQ008
DCREQ105
DCREQ195
```

Must execute actual handler.

No mock-only replacement for missing implementation.

---

## 40. DC-TST-HIER-AUTHZ-001 — source/destination permission

Requirements:

```text
DCREQ008
DCREQ034
```

Cases:

```text
can edit source + can place under destination → allowed
can edit source + destination denied → rejected
```

---

## 41. DC-TST-HIER-SEC-001 — cross-Workspace move rejected

Requirements:

```text
DCREQ009
```

No scope change.

---

## 42. DC-TST-HIER-CONC-001 — concurrent reparent

Requirements:

```text
DCREQ007
DCREQ010
```

Use overlapping transactions.

Expected:

```text
one valid tree
or deterministic conflict
```

---

## 43. DC-TST-HIER-CONC-002 — destination lifecycle race

Move versus destination archive/delete.

No invalid parent commit.

---

# Block identity/content

## 44. DC-TST-BLOCK-DOM-001 — stable identity

Requirements:

```text
DCREQ011
```

---

## 45. DC-TST-BLOCK-SCOPE-001 — one Page only

Requirements:

```text
DCREQ012
```

Ordinary mutation cannot cross Page.

Candidate Domain evidence:

```text
BlockCreateChildScopeTests
BlockHierarchyTests
```

---

## 46. DC-TST-BLOCK-DOM-002 — lifecycle

Requirements:

```text
DCREQ013
```

Create/update/delete/restore + no-op.

---

## 47. DC-TST-BLOCK-CONTENT-001 — type-specific validation

Requirements:

```text
DCREQ014
DCREQ015
```

Candidate:

```text
BlockContentValidatorTests
BlockPropertiesTests
```

Need one scenario per released BlockType.

---

## 48. DC-TST-BLOCK-COMPAT-001 — historical valid live content readable

Requirements:

```text
DCREQ016
DCREQ086
```

Fixture across candidate reader.

---

## 49. DC-TST-BLOCK-COMPAT-002 — unknown type/version safe

Requirements:

```text
DCREQ017
```

No silent coercion/data loss.

---

# Block hierarchy

## 50. DC-TST-BTREE-DOM-001 — parent scope

Requirements:

```text
DCREQ117
DCREQ118
```

Candidate:

```text
BlockCreateChildScopeTests
BlockHierarchyTests
```

---

## 51. DC-TST-BTREE-DOM-002 — cycle prevention

Requirements:

```text
DCREQ119
```

Candidate:

```text
BlockTreeRulesTests
```

---

## 52. DC-TST-BTREE-APP-001 — parent+position coherent move

Requirements:

```text
DCREQ120
```

Execute production handler/use-case.

Verify fresh DB state.

---

## 53. DC-TST-BTREE-SEC-001 — cross-Page parent rejected

Requirements:

```text
DCREQ012
DCREQ118
```

This is mandatory even if client UI never offers the action.

---

## 54. DC-TST-BTREE-DEL-001 — parent deletion child policy

Requirements:

```text
DCREQ121
```

Test exact accepted policy:

```text
subtree delete
promote
or reject
```

No accidental FK-cascade-only proof.

---

# Ordering

## 55. DC-TST-ORDER-DOM-001 — deterministic sort

Requirements:

```text
DCREQ018
DCREQ019
```

---

## 56. DC-TST-ORDER-DENSE-001 — dense insertion

Requirements:

```text
DCREQ019
```

Repeated insert-between.

---

## 57. DC-TST-ORDER-APP-001 — reorder operations

Requirements:

```text
DCREQ020
```

Test:

```text
first
last
between
same parent
new parent same Page
```

---

## 58. DC-TST-ORDER-CONC-001 — concurrent reorder

Requirements:

```text
DCREQ021
```

Actual overlap.

---

## 59. DC-TST-ORDER-INF-001 — fresh-context roundtrip

Requirements:

```text
DCREQ022
```

---

# Editor/concurrency/no-op

## 60. DC-TST-EDIT-CONC-001 — stale write

Requirements:

```text
DCREQ030
DCREQ136
```

Candidate must prove accepted expected-version/conflict protocol.

---

## 61. DC-TST-EDIT-NOOP-001 — no false mutation

Requirements:

```text
DCREQ137
```

For identical:

```text
Page title
Block content
Block properties
Block position/parent
```

verify no false version/event/history where applicable.

---

## 62. DC-TST-EDIT-ARCH-001 — no accidental CRDT/OT

Requirements:

```text
DCREQ031
DCREQ100
```

Architecture/source review.

---

# Document query/search

## 63. DC-TST-DQRY-001 — direct/list/tree/search parity

Requirements:

```text
DCREQ023
DCREQ025
```

Denied resource must not leak across alternate query paths.

---

## 64. DC-TST-DQRY-002 — DTO boundary

Requirements:

```text
DCREQ023
```

Public contract is not EF entity exposure.

---

## 65. DC-TST-DQRY-PERF-001 — bounded Page/Block loading

Requirements:

```text
DCREQ024
DCREQ096
DCREQ097
```

Capture:

```text
query count
rows
payload
latency
```

for representative dataset.

---

## 66. DC-TST-SEARCH-SEC-001 — search permission filtering

Requirements:

```text
DCREQ140
DCREQ141
```

Current query/index implementation must not return inaccessible Page/content.

---

# ResourceLinks

## 67. DC-TST-LINK-DOM-001 — valid link creation

Requirements:

```text
DCREQ122
```

Candidate existing evidence:

```text
ResourceLinkTests.Create_ShouldSucceed
```

---

## 68. DC-TST-LINK-DOM-002 — self-reference rejected

Requirements:

```text
DCREQ124
```

Candidate existing evidence:

```text
ResourceLinkTests
ResourceLinkTenantTests
```

---

## 69. DC-TST-LINK-DOM-003 — cross-Workspace rejected

Requirements:

```text
DCREQ123
```

Candidate existing evidence.

---

## 70. DC-TST-LINK-DOM-004 — link delete/restore affects link only

Requirements:

```text
DCREQ126
```

Domain + integration target-unchanged proof.

---

## 71. DC-TST-LINK-AUTHZ-001 — linked target permission rechecked

Requirements:

```text
DCREQ125
```

Public/shared Page with private target must not expose target.

---

## 72. DC-TST-LINK-LIFE-001 — target deletion behavior

Requirements:

```text
DCREQ127
```

Per released target kind.

---

# Versions/Snapshots/history

## 73. DC-TST-VER-DOM-001 — DocumentVersion identity/scope

Requirements:

```text
DCREQ128
```

Candidate:

```text
DocumentVersionTests
DocumentVersionImmutabilityTests
```

---

## 74. DC-TST-SNAP-DOM-001 — snapshot value semantics

Requirements:

```text
DCREQ130
DCREQ131
```

Candidate:

```text
DocumentSnapshotTests
```

Existing tests are only partial because schema/version compatibility still needs integration fixtures.

---

## 75. DC-TST-HIST-POLICY-001 — version creation policy

Requirements:

```text
DCREQ129
DCREQ137
```

For each mutation classified to create history:

```text
meaningful change → history
no-op → no history
```

---

## 76. DC-TST-HIST-API-001 — real history query

Requirements:

```text
DCREQ132
DCREQ198
```

Must fail current constant-empty implementation until resolved.

---

## 77. DC-TST-HIST-RESTORE-001 — restore creates new current mutation

Requirements:

```text
DCREQ133
```

Verify:

```text
old history remains
newer history remains
new current state created
new restore fact/history emitted
```

---

## 78. DC-TST-HIST-ARCH-001 — history != Audit

Requirements:

```text
DCREQ134
```

No direct use of Governance Audit persistence as canonical document history.

---

# Templates

## 79. DC-TST-TPL-DOM-001 — lifecycle

Requirements:

```text
DCREQ135
DCREQ138
```

Candidate:

```text
DocumentTemplateDefinitionTests
```

---

## 80. DC-TST-TPL-IDEMP-001 — publish/archive no-op

Requirements:

```text
DCREQ137
DCREQ138
```

Candidate existing tests are useful.

---

## 81. DC-TST-TPL-CONTENT-001 — template content validates Block contract

Requirements:

```text
DCREQ139
DCREQ194
```

Must go beyond current simple JsonValue creation test if template release is claimed.

---

## 82. DC-TST-TPL-INST-001 — instantiation creates new identities

Requirements:

```text
DCREQ135
```

If release-scoped.

---

# Import/export/files

## 83. DC-TST-IMP-001 — untrusted import validation

Requirements:

```text
DCREQ142
DCREQ078
```

Only if import is released.

---

## 84. DC-TST-EXP-001 — export is derived

Requirements:

```text
DCREQ143
```

Changing export representation does not mutate source Page/Block.

---

## 85. DC-TST-FILE-SEC-001 — binary/reference boundary

Requirements:

```text
DCREQ144
DCREQ145
```

Guard against raw binary payload in Domain events/logs.

---

## 86. DC-TST-FILE-SEC-002 — media/download authorization

Requirements:

```text
DCREQ146
```

If released.

---

# Additional canonical Documents Product semantics

## 86A. DC-TST-PATH-001 — Page path is derived

Requirements:

```text
DCREQ201
```

If Page path is exposed:

```text
rename/reparent
→ path may change
→ Page ID remains unchanged
```

No competing durable path identity is allowed.

---

## 86B. DC-TST-DUP-001 — Duplicate Page creates new identities

Requirements:

```text
DCREQ202
```

If duplication is released:

```text
source Page ID != duplicate Page ID
source Block IDs != duplicate Block IDs
```

and verify explicit behavior for ResourceLinks, file references, history and Collaboration references.

---

## 86C. DC-TST-X-DOC-INT-001 — Documents provider integration boundary

Requirements:

```text
DCREQ203
```

A released provider/import path must map:

```text
provider payload
→ approved Documents Application operation
→ canonical Page/Block state
```

with normal validation and no direct private-table write.

---

## 86D. DC-TST-RET-DOC-001 — Documents retention across copies/projections

Requirements:

```text
DCREQ204
```

Verify accepted retention/purge/export disposition for applicable:

```text
live Page/Block
DocumentVersion/Snapshot
search projection
export
backup/restore copy
file/object reference
```

---

## 86E. DC-TST-COMPAT-MOB-001 — reduced client preserves unsupported content

Requirements:

```text
DCREQ205
```

If reduced/mobile editing compatibility is claimed, unsupported Block types/properties must be preserved, read-only, or explicitly rejected without silent loss.

---

## 86F. DC-TST-OFF-001 — offline editing classification/protocol

Requirements:

```text
DCREQ206
```

Without an accepted offline protocol, expected classification is:

```text
NOT_APPLICABLE / OUT_OF_RELEASE_SCOPE
```

If released, verify operation identity, reconciliation, conflict, authorization recheck, target lifecycle and retry semantics.

---

## 86G. DC-TST-CONTENT-A11Y-001 — typed content preserves accessibility semantics

Requirements:

```text
DCREQ207
```

For applicable Block types, backend serialization preserves semantic fields required by clients for accessible rendering.

---

# Additional canonical Collaboration Product semantics

## 86H. DC-TST-ATT-UPLOAD-001 — Attachment upload lifecycle

Requirements:

```text
DCREQ208
```

If product-visible, verify accepted upload states/transitions and that provider upload success alone is not durable Attachment truth.

---

## 86I. DC-TST-PRES-EPH-001 — cursor/typing is ephemeral

Requirements:

```text
DCREQ209
```

If released, cursor/typing disappears/rebuilds after disconnect/expiry and does not create durable Activity/Audit/Comment-history/Notification state by itself.

---

## 86J. DC-TST-NOTIF-CHANNEL-001 — channel delivery separate from Notification truth

Requirements:

```text
DCREQ210
```

Provider/channel attempts can fail/retry independently from Notification existence and recipient attention state.

---

## 86K. DC-TST-NOTIF-PREF-001 — Notification preferences

Requirements:

```text
DCREQ211
```

If released, verify persisted/application semantics for notification type, channel, enablement, delivery mode, digest and quiet-hours/timezone scope.

---

## 86L. DC-TST-NOTIF-ATTN-001 — Notification recipient attention lifecycle

Requirements:

```text
DCREQ212
```

If released, verify Seen/Read/Archived/Dismissed transitions and ensure those mutations do not mutate source-resource or provider-delivery state.

---

## 86M. DC-TST-RET-WS-001 — Workspace deletion Collaboration workflow

Requirements:

```text
DCREQ213
```

Verify accepted retain/hide/export/anonymize/purge behavior for each released Collaboration state family.

---

## 86N. DC-TST-COM-THREAD-DEL-001 — parent deletion preserves thread coherence

Requirements:

```text
DCREQ214
```

Replies must retain coherent parent/thread/target semantics after parent deletion/tombstone.

---

## 86O. DC-TST-COM-HIST-001 — Comment edit-history ownership

Requirements:

```text
DCREQ215
```

If released, Comment edit history is Collaboration-owned and distinct from Activity, Notification, Governance Audit and Documents history.

---

## 86P. DC-TST-SEC-ABUSE-001 — abuse controls

Requirements:

```text
DCREQ216
```

For public/guest/high-abuse surfaces, execute applicable rate/content/moderation/spam/attachment controls.

---

## 86Q. DC-TST-COL-SEARCH-001 — Collaboration search is derived and authorized

Requirements:

```text
DCREQ217
```

If released, Comment/Activity search respects current tenant, target access, lifecycle and revocation.

---

## 86R. DC-TST-X-COL-INT-001 — Collaboration provider integration boundary

Requirements:

```text
DCREQ218
```

Released provider sync must use approved Collaboration operations with target validation, tenant scope, authorization, content safety and idempotency.

---

## 86S. DC-TST-COL-OPT-001 — optimistic Collaboration reconciliation

Requirements:

```text
DCREQ219
```

Released optimistic Comment/Reaction/ReadState/Attachment/Notification-attention UI must reconcile temporary state/identity to authoritative server state across success, rejection, stale conflict, duplicate delivery and reconnect.

---

## 86T. DC-TST-PAGE-DEL-ORCH-001 — Page deletion orchestration

Requirements:

```text
DCREQ220
```

Verify Page deletion coordinates applicable Blocks, ResourceLinks, history/version retention, search removal, file/reference retention and producer events.

ORM cascade alone is not proof.

---

# Documents authorization

## 87. DC-TST-DAUTH-ARCH-001 — request/action matrix complete

Requirements:

```text
DCREQ032
DCREQ033
DCREQ107
DCREQ191
```

Enumerate release-scoped request descriptors.

Every request must match accepted matrix.

---

## 88. DC-TST-DAUTH-POL-001 — Documents policy matrix

For representative actions:

```text
CreatePage
ViewPage
UpdatePage
ArchivePage
DeletePage
move/reparent action
visibility action if released
history action if released
```

evaluate actual `AccessPolicyEngine`.

Existing:

```text
CreatePageTargetPolicyMatrixTests
```

is candidate evidence for only CreatePage.

---

## 89. DC-TST-DAUTH-DEST-001 — structural destination authorization

Requirements:

```text
DCREQ034
```

Page move and Block parent move.

---

## 90. DC-TST-DAUTH-DISC-001 — existence disclosure

Requirements:

```text
DCREQ035
DCREQ109
```

---

# Comments/replies/anchors/status

## 91. DC-TST-COM-DOM-001 — create/update/delete/restore

Requirements:

```text
DCREQ036–039
```

Candidate:

```text
CommentTests
CommentDeletionAtomicityTests
```

---

## 92. DC-TST-COM-REPLY-001 — same target/thread

Requirements:

```text
DCREQ150
DCREQ152
```

Candidate:

```text
CommentReplyInvariantTests
CommentReplyScopeTests
```

---

## 93. DC-TST-COM-REPLY-002 — deleted parent rejected

Requirements:

```text
DCREQ151
```

Candidate existing evidence.

---

## 94. DC-TST-COM-ANCH-001 — anchor value semantics

Requirements:

```text
DCREQ147
```

Candidate:

```text
CommentAnchorTests
```

---

## 95. DC-TST-COM-ANCH-002 — anchor does not own/mutate content

Requirements:

```text
DCREQ147–149
```

Integration:

```text
edit/delete Comment
→ anchored Page/Block unchanged
```

---

## 96. DC-TST-COM-STATUS-001 — resolve/reopen

Requirements:

```text
DCREQ037
DCREQ045
```

Domain + Application/API as released.

---

## 97. DC-TST-COM-DTO-001 — status projection accurate

PLAN:

```text
DC-COM-005
```

`ResolvedAt`/status fields may not be hard-coded inaccurately.

---

# Target contracts

## 98. DC-TST-TGT-PAGE-001 — Page target facts

Requirements:

```text
DCREQ040–047
DCREQ067
```

Cases:

```text
exists/allowed
restricted/denied
wrong tenant
deleted/archived
dependency failure
```

---

## 99. DC-TST-TGT-ITEM-001 — BoardItem target facts

Requirements:

```text
DCREQ040–047
DCREQ068
```

Use WorkManagement public/application contract.

---

## 100. DC-TST-TGT-ARCH-001 — no foreign private persistence

Requirements:

```text
DCREQ042
DCREQ079
DCREQ080
DCREQ102
```

---

## 101. DC-TST-TGT-UNKNOWN-001 — unknown kind rejected

Requirements:

```text
DCREQ041
```

---

# Comment authorization

## 102. DC-TST-CAUTH-ARCH-001 — Comment action matrix complete

Requirements:

```text
DCREQ044–046
DCREQ107
DCREQ192
```

Current mismatches must be classified.

---

## 103. DC-TST-CAUTH-CREATE-001 — create policy

Candidate:

```text
CreateCommentTargetPolicyMatrixTests
```

This test is reusable for:

```text
BoardItem CreateComment
Page CreateComment
```

but not Update/Delete/Resolve/Get.

---

## 104. DC-TST-CAUTH-READ-001 — GetComments target-specific policy

Requirements:

```text
DCREQ044
DCREQ046
```

Page and BoardItem cases.

No universal `ViewBoard` assumption.

---

## 105. DC-TST-CAUTH-EDIT-001 — author edit policy

Requirements:

```text
DCREQ045
```

Author/other/moderator + target access.

---

## 106. DC-TST-CAUTH-DELETE-001 — author/admin delete policy

Requirements:

```text
DCREQ045
```

---

## 107. DC-TST-CAUTH-STATUS-001 — resolve/reopen policy

Requirements:

```text
DCREQ045
```

---

## 108. DC-TST-CAUTH-REV-001 — revoked target access

Requirements:

```text
DCREQ046
DCREQ076
```

After revoke:

```text
read/edit/delete/resolve/realtime recovery denied
```

---

# Mentions

## 109. DC-TST-MEN-DOM-001 — Mention stable facts/scope

Requirements:

```text
DCREQ153
DCREQ154
```

Candidate:

```text
MentionTests
MentionWorkspaceScopeTests
```

---

## 110. DC-TST-MEN-APP-001 — mentioned-user validation

Requirements:

```text
DCREQ154
```

Application must validate supported MentionType identities and recipient constraints.

Current Domain non-empty Guid proof is insufficient.

---

## 111. DC-TST-MEN-APP-002 — create Comment + Mention atomicity

Requirements:

```text
DCREQ081
DCREQ153
```

Strong candidate:

```text
CommentCreatedOutboxEvidenceTests.MentionEnrollmentFailure_RollsBackCommentMentionAndBothOutboxFacts
```

---

## 112. DC-TST-MEN-EDIT-001 — mention diff on edit

Requirements:

```text
DCREQ155
```

Cases:

```text
unchanged mention → no duplicate attention
new mention → one new durable mention/fact
removed mention → canonical removal policy
```

---

## 113. DC-TST-MEN-IDEM-001 — delivery idempotency

Requirements:

```text
DCREQ156
DCREQ111
```

Same logical Mention event/message twice → one recipient side effect.

---

## 114. DC-TST-MEN-PRIV-001 — mention does not grant access

Requirements:

```text
DCREQ157
```

---

# Reactions

## 115. DC-TST-REACT-DOM-001 — create/remove

Requirements:

```text
DCREQ158
DCREQ160
```

Candidate:

```text
ReactionTests
```

---

## 116. DC-TST-REACT-DOM-002 — duplicate invariant

Requirements:

```text
DCREQ159
```

Candidate:

```text
ReactionDuplicateTests
```

---

## 117. DC-TST-REACT-INF-001 — persistence uniqueness under race

Requirements:

```text
DCREQ159
```

Domain callback test alone is not enough for concurrent duplicate requests.

Use real DB unique constraint/transaction semantics where release-scoped.

---

## 118. DC-TST-REACT-QRY-001 — count derived correctly

Requirements:

```text
DCREQ161
```

---

## 119. DC-TST-REACT-AUTHZ-001 — target access

Reaction cannot be created/viewed via stale/foreign target permission.

---

# Attachments

## 120. DC-TST-ATT-DOM-001 — lifecycle

Requirements:

```text
DCREQ162
DCREQ166
```

Candidate:

```text
AttachmentTests
```

---

## 121. DC-TST-ATT-SCOPE-001 — workspace scope

Requirements:

```text
DCREQ163
DCREQ164
```

Candidate:

```text
AttachmentWorkspaceScopeTests
```

---

## 122. DC-TST-ATT-APP-001 — supported target kinds only

Requirements:

```text
DCREQ163
```

Current BoardItem-specific Application/API must reject or lack unsupported targets.

---

## 123. DC-TST-ATT-SEC-001 — URL/object safety

Requirements:

```text
DCREQ162
DCREQ165
```

If arbitrary HTTP(S) link remains supported:

```text
validate security/product policy
```

If signed-object model is used:

```text
expiry
authorization
no durable secret URL
```

---

## 124. DC-TST-ATT-LIFE-001 — target/attachment retention

Requirements:

```text
DCREQ166
DCREQ189
```

---

# Presence

## 125. DC-TST-PRES-DOM-001 — lifecycle

Requirements:

```text
DCREQ167
DCREQ170
```

Candidate:

```text
PresenceExperimentalTests
PresenceIsolationTests
```

---

## 126. DC-TST-PRES-AUTHZ-001 — Presence does not authorize

Requirements:

```text
DCREQ169
```

No session/channel join without current target permission.

---

## 127. DC-TST-PRES-RT-001 — heartbeat/expiry/reconnect/rebuild

Requirements:

```text
DCREQ167
DCREQ170
DCREQ190
DCREQ209
```

Verify heartbeat refresh, disconnect, expiry, reconnect, duplicate-session handling and ghost cleanup.

Presence rebuilds rather than durable replay.

Cursor/typing sharing Presence transport must remain ephemeral and non-historical.

---

## 128. DC-TST-PRES-SEC-001 — scope isolation

Requirements:

```text
DCREQ168
DCREQ072
```

A/B Workspace/user/resource visibility.

---

# ReadState

## 129. DC-TST-READ-DOM-001 — create/mark/unread

Requirements:

```text
DCREQ171
DCREQ173
```

Candidate:

```text
ReadStateMonotonicityTests
```

---

## 130. DC-TST-READ-CONC-001 — monotonic under stale/reordered update

Requirements:

```text
DCREQ172
```

Current Domain test name is useful, but candidate must verify actual stale/reordered behavior rather than only simple state transitions.

---

## 131. DC-TST-READ-QRY-001 — unread reconcile

Requirements:

```text
DCREQ173
```

Projection/count can be reconstructed/reconciled from stable boundary.

---

## 132. DC-TST-READ-AUTHZ-001 — mark-read after revoke denied

Requirements:

```text
DCREQ174
DCREQ076
```

---

# Watchers

## 133. DC-TST-WATCH-DOM-001 — create/unwatch

Requirements:

```text
DCREQ175
DCREQ177
```

Candidate:

```text
ResourceWatcherTests
```

---

## 134. DC-TST-WATCH-SCOPE-001 — target Workspace scope

Requirements:

```text
DCREQ175
```

Candidate:

```text
ResourceWatcherWorkspaceScopeTests
```

---

## 135. DC-TST-WATCH-IDEM-001 — watch/unwatch retry

Requirements:

```text
DCREQ177
```

Needs persistence/application proof, not only Domain event.

---

## 136. DC-TST-WATCH-RULE-001 — automatic watching

Requirements:

```text
DCREQ176
```

Only if auto-watch is release-scoped.

---

## 137. DC-TST-WATCH-AUTHZ-001 — watcher does not grant access

Requirements:

```text
DCREQ178
```

---

# Notifications

## 138. DC-TST-NOTIF-ARCH-001 — Notification ownership/runtime classification

Requirements:

```text
DCREQ179
DCREQ200
DCREQ210
DCREQ211
DCREQ212
```

Current source already has durable Notification records and Mention-driven runtime persistence.

Certification must establish how those Infrastructure records satisfy Collaboration-owned Notification semantics, what Application/API surface is released, and which state belongs to recipient attention versus provider/channel delivery.

Creating a second Notification store is not an acceptable shortcut.

---

## 139. DC-TST-NOTIF-RECIP-001 — explicit recipient

Requirements:

```text
DCREQ180
```

If product Notification exists.

---

## 140. DC-TST-NOTIF-FACT-001 — committed fact source

Requirements:

```text
DCREQ181
```

No notification from rolled-back Mention/Comment.

Candidate evidence includes:

```text
CommentCreatedOutboxEvidenceTests
MentionCreatedNotificationRuntimeChainIntegrationTests
```

The runtime-chain test must prove Notification persistence begins from committed outbox/Mention facts.

---

## 141. DC-TST-NOTIF-DEL-001 — provider result separate

Requirements:

```text
DCREQ182
```

Provider failure must not erase durable product Notification if such model exists.

If transport-only integration, state that product Notification is not being certified.

---

## 142. DC-TST-NOTIF-PRIV-001 — historical Notification after revoke

Requirements:

```text
DCREQ183
```

Deep link/view rechecks permission.

---

## 143. DC-TST-NOTIF-IDEM-001 — recipient fan-out dedup

Requirements:

```text
DCREQ184
```

Strong candidate evidence:

```text
MentionCreatedNotificationRuntimeChainIntegrationTests
```

Duplicate delivery of the same logical Mention/message must produce one logical Notification/recipient effect.

---

# Activity

## 144. DC-TST-ACT-ARCH-001 — Activity != Audit

Requirements:

```text
DCREQ185
DCREQ188
```

---

## 145. DC-TST-ACT-PROJ-001 — business facts project Activity

Requirements:

```text
DCREQ186
```

Use the existing Activity projection consumer/store where it matches Product semantics.

The test guards against a competing second Activity truth/store.

---

## 146. DC-TST-ACT-API-001 — real Activity result

Requirements:

```text
DCREQ187
DCREQ198
```

Seed/projection → query non-empty expected result.

Current constant-empty handler must fail this gate until resolved.

---

## 147. DC-TST-ACT-PRIV-001 — permission-filtered Activity

Old Activity item does not reveal inaccessible current resource content.

---

# Collaboration query

## 148. DC-TST-CQRY-001 — target-scoped Comment query

Requirements:

```text
DCREQ051
```

---

## 149. DC-TST-CQRY-002 — deterministic ordering/thread shape

Requirements:

```text
DCREQ053
DCREQ152
```

---

## 150. DC-TST-CQRY-PERF-001 — bounded actor/authorization loading

Requirements:

```text
DCREQ052
DCREQ098
```

Current actor batching is good candidate behavior; verify no per-Comment permission fan-out.

---

# Retention

## 151. DC-TST-RET-MATRIX-001 — target lifecycle matrix

Requirements:

```text
DCREQ048
DCREQ049
DCREQ189
```

For every released target kind × capability.

---

## 152. DC-TST-RET-IDENT-001 — Identity deletion/anonymization

Requirements:

```text
DCREQ050
```

Cover released:

```text
Comment author
Mention
Reaction
Watcher
ReadState
Presence
Notification recipient
Activity actor
```

---

# Events/outbox

## 153. DC-TST-EVT-DOC-001 — Page producer outbox

Requirements:

```text
DCREQ054
DCREQ056–058
```

Candidate:

```text
PageCreatedOutboxEvidenceTests
PageArchivedOutboxEvidenceTests
```

Strong evidence includes rollback and real DB failure.

---

## 154. DC-TST-EVT-COM-001 — Comment producer outbox

Requirements:

```text
DCREQ055–058
```

Candidate:

```text
CommentCreatedOutboxEvidenceTests
```

---

## 155. DC-TST-EVT-MEN-001 — Mention composite fact rollback

Requirements:

```text
DCREQ055
DCREQ058
DCREQ081
```

Candidate existing integration test.

---

## 156. DC-TST-EVT-PRIV-001 — payload minimum/safety

Requirements:

```text
DCREQ077
DCREQ193
```

No full tree/snapshot/thread in ordinary event.

Raw content inclusion must be explicitly classified for purpose/privacy.

---

## 157. DC-TST-EVT-VER-001 — event pinning

Requirements:

```text
DCREQ057
```

Candidate:

```text
DocumentsCollaborationEventPinningArchitectureTests
```

---

## 158. DC-TST-EVT-STUB-001 — stub consumer not counted as implemented

Requirements:

```text
DCREQ105
DCREQ199
```

Current Documents stub consumers must remain classified as STUB unless implementation is real.

---

# Cross-context

## 159. DC-TST-X-WM-READ-001 — Collaboration summary boundary

Requirements:

```text
DCREQ067
DCREQ068
DCREQ079
DCREQ080
```

Candidate:

```text
CollaborationReadBoundaryArchitectureTests
WorkManagementCollaborationReadAdapterTests
```

---

## 160. DC-TST-X-TGT-UNCHANGED-001 — Comment does not mutate foreign target

Requirements:

```text
DCREQ043
```

Candidate:

```text
CommentCreatedOutboxEvidenceTests.CommentOnForeignBoardItem_TargetAggregateUnchanged
```

---

## 161. DC-TST-X-AUT-001 — Automation handoff

Requirements:

```text
DCREQ069
```

Only released trigger contracts.

---

## 162. DC-TST-X-ANA-001 — Analytics handoff

Requirements:

```text
DCREQ070
```

---

## 163. DC-TST-X-ACT-001 — producer failure isolation

Requirements:

```text
DCREQ071
DCREQ091
```

Activity/notification consumer failure must not undo producer commit.

---

# Tenant isolation/RLS/security

## 164. DC-TST-TEN-DOC-001 — Documents A→B matrix

Requirements:

```text
DCREQ072–075
DCREQ108
```

At minimum:

```text
Page read/update/move
Block create/update/move
history if released
ResourceLink if released
```

---

## 165. DC-TST-TEN-COL-001 — Collaboration A→B matrix

Requirements:

```text
DCREQ072–075
DCREQ108
```

At minimum released:

```text
Comment
Mention
Reaction
Attachment
ReadState
Watcher
Presence subscription
```

---

## 166. DC-TST-RLS-DOC-001 — real PostgreSQL Documents RLS

Requirements:

```text
DCREQ074
```

Use application role/session context.

---

## 167. DC-TST-RLS-COL-001 — real PostgreSQL Collaboration RLS

Same.

---

## 168. DC-TST-SEC-SYS-001 — missing background scope fails closed

Requirements:

```text
DCREQ075
```

No null/empty global fallback.

---

## 169. DC-TST-SEC-REV-001 — permission revocation cross-surface

Requirements:

```text
DCREQ076
```

Verify released:

```text
document query
comment
attachment
watch/notification view
search
realtime
```

---

## 170. DC-TST-SEC-LOG-001 — sensitive-content sentinel

Requirements:

```text
DCREQ077
DCREQ093
```

Sentinel absent from ordinary logs/errors/metrics.

---

## 171. DC-TST-SEC-RICH-001 — render/import sanitization

Requirements:

```text
DCREQ078
```

Only for released markup/import surface.

---

# Persistence/transactions

## 172. DC-TST-OWN-ARCH-001 — Documents persistence private

Requirements:

```text
DCREQ079
```

---

## 173. DC-TST-OWN-ARCH-002 — Collaboration persistence private

Requirements:

```text
DCREQ080
```

---

## 174. DC-TST-TX-PAGE-001 — Page move atomicity

Requirements:

```text
DCREQ081
```

---

## 175. DC-TST-TX-BLOCK-001 — Block parent/order atomicity

Requirements:

```text
DCREQ081
```

Candidate Domain failure-atomicity tests are partial; final proof should inspect committed persistence state.

---

## 176. DC-TST-TX-COMMENT-001 — Comment+Mention atomicity

Requirements:

```text
DCREQ081
```

Candidate existing integration evidence is strong.

---

## 177. DC-TST-XTX-ARCH-001 — no hidden distributed/shared transaction

Requirements:

```text
DCREQ082
```

---

# Migration/compatibility

## 178. DC-TST-MIG-001 — clean DB

Requirements:

```text
DCREQ083
```

---

## 179. DC-TST-MIG-002 — supported upgrade/fresh-baseline policy

Requirements:

```text
DCREQ083
```

---

## 180. DC-TST-MIG-003 — no pending model changes

Requirements:

```text
DCREQ083
```

---

## 181. DC-TST-MIG-HIER-001 — Page/Block hierarchy preservation

Requirements:

```text
DCREQ084
```

---

## 182. DC-TST-MIG-ORDER-001 — ordering preservation

Requirements:

```text
DCREQ085
```

---

## 183. DC-TST-MIG-BLOCK-001 — live Block content compatibility

Requirements:

```text
DCREQ086
```

---

## 184. DC-TST-MIG-SNAP-001 — Snapshot/Version compatibility

Requirements:

```text
DCREQ131
DCREQ194
```

Use historical fixture produced by prior supported schema.

---

## 185. DC-TST-MIG-TPL-001 — Template compatibility

Requirements:

```text
DCREQ139
DCREQ194
```

---

## 186. DC-TST-MIG-TGT-001 — Collaboration target preservation

Requirements:

```text
DCREQ087
```

---

# Failure/reliability

## 187. DC-TST-FAIL-API-001 — stable public error classes

Requirements:

```text
DCREQ088
```

Cover:

```text
validation
forbidden/not-found
conflict
unsupported content/target
dependency failure
```

---

## 188. DC-TST-FAIL-DB-001 — persistence rollback

Requirements:

```text
DCREQ089
```

Candidate Page outbox integration tests provide strong patterns.

Fresh context required.

---

## 189. DC-TST-FAIL-DEP-001 — target dependency fail-closed

Requirements:

```text
DCREQ090
```

No Comment/Reaction/Attachment/Watcher creation.

---

## 190. DC-TST-FAIL-MSG-001 — post-commit messaging failure

Requirements:

```text
DCREQ091
```

Producer remains committed.

---

## 191. DC-TST-FAIL-RT-001 — realtime publication failure

Requirements:

```text
DCREQ092
```

Authoritative query remains correct.

---

## 192. DC-TST-IDEM-001 — retry-sensitive mutation matrix

Requirements:

```text
DCREQ110
```

Classify and test as applicable:

```text
Comment create
Mention delivery
Reaction create
Attachment create
Watch
mark-read
```

---

## 193. DC-TST-DEDUP-001 — same message identity

Requirements:

```text
DCREQ111
```

---

## 194. DC-TST-POISON-001 — poison isolation

Requirements:

```text
DCREQ112
```

---

# Realtime/recovery

## 195. DC-TST-RT-DOC-001 — Documents duplicate/reorder

Requirements:

```text
DCREQ059
DCREQ060
DCREQ062
DCREQ063
```

---

## 196. DC-TST-RT-DOC-002 — Documents gap/recovery

Requirements:

```text
DCREQ064–066
```

Recover:

```text
Page lifecycle
Block content
Block parent
Block order
```

as released.

---

## 197. DC-TST-RT-COM-001 — Comment duplicate/reorder

Requirements:

```text
DCREQ059
DCREQ061–063
DCREQ190
```

---

## 198. DC-TST-RT-REACT-001 — Reaction replay-safe

Requirements:

```text
DCREQ190
```

No count duplication/regression.

---

## 199. DC-TST-RT-READ-001 — ReadState replay-safe

Requirements:

```text
DCREQ172
DCREQ190
```

No monotonic regression.

---

## 200. DC-TST-RT-NOTIF-001 — Notification replay-safe

Requirements:

```text
DCREQ184
DCREQ190
```

Only if product Notification released.

---

## 202. DC-TST-RT-REV-001 — permission revoked while disconnected

Requirements:

```text
DCREQ076
DCREQ065
```

Reconnect/recovery cannot restore unauthorized data.

---

## 203. DC-TST-RT-CONV-001 — final convergence

Requirements:

```text
DCREQ065
```

For every released durable realtime state:

```text
client projection == authoritative server query
```

---

# Performance/observability

## 204. DC-TST-PERF-PAGE-001 — Page hierarchy

Requirements:

```text
DCREQ096
```

---

## 205. DC-TST-PERF-BLOCK-001 — large Block tree

Requirements:

```text
DCREQ097
DCREQ099
```

---

## 206. DC-TST-PERF-COL-001 — Comment thread

Requirements:

```text
DCREQ098
```

---

## 207. DC-TST-PERF-ACT-001 — Activity query

Requirements:

```text
DCREQ098
```

If released.

---

## 208. DC-TST-PERF-ATTN-001 — Reaction/ReadState/Watcher hot path

Requirements:

```text
DCREQ099
```

Only release-scoped features.

---

## 209. DC-TST-OBS-001 — safe correlation

Requirements:

```text
DCREQ093
```

---

## 210. DC-TST-OBS-002 — failure categories

Requirements:

```text
DCREQ094
```

---

## 211. DC-TST-OBS-RT-001 — recovery telemetry

Requirements:

```text
DCREQ095
```

---

# Architecture/non-goals

## 212. DC-TST-ARCH-CRDT-001

Requirements:

```text
DCREQ100
```

No unauthorized CRDT/OT/offline subsystem.

---

## 213. DC-TST-ARCH-CMS-001

Requirements:

```text
DCREQ101
```

Documents remains typed semantic owner.

---

## 214. DC-TST-ARCH-REF-001

Requirements:

```text
DCREQ102
```

No arbitrary foreign table repository behind ResourceRef.

---

## 215. DC-TST-ARCH-BOUND-001

Requirements:

```text
DCREQ103
```

No combined DocumentsCollaboration aggregate/private persistence.

---

# Brownfield/meta verification

## 216. DC-TST-INV-001 — inventory completeness

Requirements:

```text
DCREQ104
DCREQ200
```

Every canonical product-owned capability appears in source/disposition matrix.

---

## 217. DC-TST-INV-STUB-001 — placeholders classified

Requirements:

```text
DCREQ105
DCREQ195
DCREQ196
DCREQ198
DCREQ199
```

Fail closure if any known source-debt row is blank.

---

## 218. DC-TST-INV-QUAL-001 — reused evidence quality

Requirements:

```text
DCREQ106
```

For every reused critical test, record:

```text
property proven
test layer
production DI?
real DB?
negative path?
committed-state proof?
current candidate semantics?
```

---

# Product requirement traceability

## 219. PROD-DCT-001–010

Mapped to:

```text
DC-TST-PAGE-*
DC-TST-PMETA-*
DC-TST-PVIS-*
DC-TST-HIER-*
DC-TST-BLOCK-*
DC-TST-BTREE-*
DC-TST-ORDER-*
```

---

## 220. PROD-DCT-011–020

Mapped to:

```text
DC-TST-EDIT-*
DC-TST-RT-DOC-*
DC-TST-COM-ANCH-*
DC-TST-LINK-*
DC-TST-VER-*
DC-TST-SNAP-*
DC-TST-HIST-*
DC-TST-TPL-*
DC-TST-DAUTH-*
DC-TST-X-*
```

---

## 221. PROD-DCT-021–031

Mapped to:

```text
DC-TST-SEARCH-*
DC-TST-FILE-*
DC-TST-EVT-*
DC-TST-RT-*
DC-TST-EDIT-NOOP-001
DC-TST-IMP-*
DC-TST-EXP-*
DC-TST-RET-*
DC-TST-BTREE-DEL-001
DC-TST-HIST-RESTORE-001
DC-TST-HIST-ARCH-001
```

---

## 222. PROD-COL-001–010

Mapped to:

```text
DC-TST-TGT-*
DC-TST-X-TGT-UNCHANGED-001
DC-TST-COM-*
DC-TST-MEN-*
DC-TST-REACT-*
DC-TST-ATT-*
```

---

## 223. PROD-COL-011–020

Mapped to:

```text
DC-TST-ATT-SEC-001
DC-TST-PRES-*
DC-TST-READ-*
DC-TST-WATCH-*
DC-TST-NOTIF-*
DC-TST-ACT-*
```

---

## 224. PROD-COL-021–029

Mapped to:

```text
DC-TST-TGT-*
DC-TST-CAUTH-*
DC-TST-NOTIF-PRIV-001
DC-TST-RT-*
DC-TST-RET-*
DC-TST-SEC-RICH-001
```

---

# DCREQ family traceability

## 225. DCREQ001–010

Covered by:

```text
DC-TST-PAGE-DOM-001..003
DC-TST-PAGE-SCOPE-001
DC-TST-PAGE-RET-001
DC-TST-HIER-*
```

---

## 226. DCREQ011–022

Covered by:

```text
DC-TST-BLOCK-*
DC-TST-BTREE-*
DC-TST-ORDER-*
```

---

## 227. DCREQ023–035

Covered by:

```text
DC-TST-DQRY-*
DC-TST-SEARCH-SEC-001
DC-TST-EDIT-*
DC-TST-DAUTH-*
```

---

## 228. DCREQ036–053

Covered by:

```text
DC-TST-COM-*
DC-TST-TGT-*
DC-TST-CAUTH-*
DC-TST-CQRY-*
DC-TST-RET-*
```

---

## 229. DCREQ054–071

Covered by:

```text
DC-TST-EVT-*
DC-TST-X-*
DC-TST-RT-*
```

---

## 230. DCREQ072–082

Covered by:

```text
DC-TST-TEN-*
DC-TST-RLS-*
DC-TST-SEC-*
DC-TST-OWN-*
DC-TST-TX-*
DC-TST-XTX-*
```

---

## 231. DCREQ083–099

Covered by:

```text
DC-TST-MIG-*
DC-TST-FAIL-*
DC-TST-PERF-*
DC-TST-OBS-*
```

---

## 232. DCREQ100–112

Covered by:

```text
DC-TST-ARCH-*
DC-TST-INV-*
DC-TST-IDEM-001
DC-TST-DEDUP-001
DC-TST-POISON-001
```

---

## 233. DCREQ113–121

Covered by:

```text
DC-TST-PMETA-*
DC-TST-PVIS-*
DC-TST-BTREE-*
```

---

## 234. DCREQ122–127

Covered by:

```text
DC-TST-LINK-*
```

---

## 235. DCREQ128–139

Covered by:

```text
DC-TST-VER-*
DC-TST-SNAP-*
DC-TST-HIST-*
DC-TST-TPL-*
DC-TST-EDIT-NOOP-001
```

---

## 236. DCREQ140–146

Covered by:

```text
DC-TST-SEARCH-*
DC-TST-IMP-*
DC-TST-EXP-*
DC-TST-FILE-*
```

---

## 237. DCREQ147–157

Covered by:

```text
DC-TST-COM-ANCH-*
DC-TST-COM-REPLY-*
DC-TST-MEN-*
```

---

## 238. DCREQ158–166

Covered by:

```text
DC-TST-REACT-*
DC-TST-ATT-*
```

---

## 239. DCREQ167–178

Covered by:

```text
DC-TST-PRES-*
DC-TST-READ-*
DC-TST-WATCH-*
```

---

## 240. DCREQ179–190

Covered by:

```text
DC-TST-NOTIF-*
DC-TST-ACT-*
DC-TST-RET-*
DC-TST-RT-*
```

---

## 241. DCREQ191–200

Covered by:

```text
DC-TST-SRC-*
DC-TST-DAUTH-ARCH-001
DC-TST-CAUTH-ARCH-001
DC-TST-EVT-PRIV-001
DC-TST-MIG-SNAP-001
DC-TST-MIG-TPL-001
DC-TST-INV-*
```

---


## 241A. Explicit DCREQ traceability closure

The following requirements were previously represented only through ranges or family prose and must remain explicit for machine verification:

```text
DCREQ026 → DC-TST-DQRY-002 / DC-TST-TGT-ARCH-001
DCREQ027 → DC-TST-EDIT-CONC-001 / DC-TST-EDIT-NOOP-001
DCREQ028 → DC-TST-EDIT-CONC-001
DCREQ029 → DC-TST-COL-OPT-001 where Collaboration applies
DCREQ038 → DC-TST-COM-DOM-001
DCREQ039 → DC-TST-COM-DOM-001 / DC-TST-SEC-RICH-001
DCREQ047 → DC-TST-TGT-PAGE-001 / DC-TST-TGT-ITEM-001
DCREQ066 → DC-TST-RT-DOC-002 / Platform recovery evidence
DCREQ148 → DC-TST-COM-ANCH-002
DCREQ149 → DC-TST-COM-ANCH-002
```

---

## 241B. DCREQ201–207

Covered by:

```text
DCREQ201 → DC-TST-PATH-001
DCREQ202 → DC-TST-DUP-001
DCREQ203 → DC-TST-X-DOC-INT-001
DCREQ204 → DC-TST-RET-DOC-001
DCREQ205 → DC-TST-COMPAT-MOB-001
DCREQ206 → DC-TST-OFF-001
DCREQ207 → DC-TST-CONTENT-A11Y-001
```

---

## 241C. DCREQ208–220

Covered by:

```text
DCREQ208 → DC-TST-ATT-UPLOAD-001
DCREQ209 → DC-TST-PRES-EPH-001 / DC-TST-PRES-RT-001
DCREQ210 → DC-TST-NOTIF-CHANNEL-001
DCREQ211 → DC-TST-NOTIF-PREF-001
DCREQ212 → DC-TST-NOTIF-ATTN-001
DCREQ213 → DC-TST-RET-WS-001
DCREQ214 → DC-TST-COM-THREAD-DEL-001
DCREQ215 → DC-TST-COM-HIST-001
DCREQ216 → DC-TST-SEC-ABUSE-001
DCREQ217 → DC-TST-COL-SEARCH-001
DCREQ218 → DC-TST-X-COL-INT-001
DCREQ219 → DC-TST-COL-OPT-001
DCREQ220 → DC-TST-PAGE-DEL-ORCH-001
```

---

# Core readiness sets

## 242. Documents Core D4 minimum

At minimum:

```text
DC-TST-PAGE-DOM-001..003
DC-TST-PAGE-SCOPE-001

DC-TST-PMETA-API-001
DC-TST-PVIS-* if visibility release-scoped

DC-TST-HIER-DOM-001
DC-TST-HIER-APP-001
DC-TST-HIER-AUTHZ-001
DC-TST-HIER-SEC-001
DC-TST-HIER-CONC-001

DC-TST-BLOCK-DOM-001..002
DC-TST-BLOCK-SCOPE-001
DC-TST-BLOCK-CONTENT-001
DC-TST-BLOCK-COMPAT-001

DC-TST-BTREE-DOM-001..002
DC-TST-BTREE-APP-001
DC-TST-BTREE-SEC-001
DC-TST-BTREE-DEL-001

DC-TST-ORDER-DOM-001
DC-TST-ORDER-DENSE-001
DC-TST-ORDER-APP-001
DC-TST-ORDER-CONC-001
DC-TST-ORDER-INF-001

DC-TST-DQRY-001
DC-TST-DAUTH-ARCH-001
DC-TST-DAUTH-POL-001
DC-TST-TEN-DOC-001

DC-TST-SRC-001
DC-TST-SRC-003
DC-TST-SRC-005
```

plus applicable RLS/migration gates.

---

## 243. Documents Full Release additional minimum

For every released capability add applicable:

```text
DC-TST-LINK-*
DC-TST-VER-*
DC-TST-SNAP-*
DC-TST-HIST-*
DC-TST-TPL-*
DC-TST-SEARCH-*
DC-TST-IMP-*
DC-TST-EXP-*
DC-TST-FILE-*
```

---

## 244. Collaboration Core D4 minimum

At minimum:

```text
DC-TST-COM-DOM-001
DC-TST-COM-REPLY-001..002
DC-TST-COM-STATUS-001 if resolve released

DC-TST-TGT-PAGE-001 if Page target released
DC-TST-TGT-ITEM-001 if BoardItem target released
DC-TST-TGT-ARCH-001
DC-TST-TGT-UNKNOWN-001

DC-TST-CAUTH-ARCH-001
DC-TST-CAUTH-CREATE-001
DC-TST-CAUTH-READ-001
DC-TST-CAUTH-EDIT-001
DC-TST-CAUTH-DELETE-001
DC-TST-CAUTH-REV-001

DC-TST-CQRY-001..002
DC-TST-TEN-COL-001
DC-TST-RET-MATRIX-001

DC-TST-SRC-005
```

Anchor only if release-scoped.

---

## 245. Collaboration Full Release additional minimum

Per released capability:

```text
Mentions      → DC-TST-MEN-*
Reactions     → DC-TST-REACT-*
Attachments   → DC-TST-ATT-*
Presence      → DC-TST-PRES-*
ReadState     → DC-TST-READ-*
Watchers      → DC-TST-WATCH-*
Notifications → DC-TST-NOTIF-*
Activity      → DC-TST-ACT-*
```

---

# Realtime D4+ minimum

## 246. Durable-state realtime minimum

For each durable released state:

```text
duplicate
out-of-order
gap
reconnect
permission revoke
authoritative reload
final convergence
```

Required IDs include:

```text
DC-TST-RT-DOC-*
DC-TST-RT-COM-001
DC-TST-RT-REACT-001 if released
DC-TST-RT-READ-001 if released
DC-TST-RT-NOTIF-001 if released
DC-TST-RT-REV-001
DC-TST-RT-CONV-001
```

---

## 247. Ephemeral Presence realtime minimum

Presence does not need durable replay.

It does need:

```text
authorization
scope
heartbeat/expiry
disconnect/reconnect
duplicate connection safety
```

---

# Evidence bundle mapping

## 247A. Evidence bundle authority

Every verification result populates one or more canonical bundles:

```text
DC-EVID-01..DC-EVID-28
```

CERTIFICATION consumes these bundles.

---

## 247B. Bundle → TEST mapping

| Bundle | TEST families / gates |
|---|---|
| DC-EVID-01 | DC-TST-INV-*, DC-TST-SRC-*, DC-TST-TRACE-001 |
| DC-EVID-02 | DC-TST-PAGE-*, DC-TST-PMETA-*, DC-TST-PVIS-*, DC-TST-PATH-*, DC-TST-DUP-* |
| DC-EVID-03 | DC-TST-HIER-* |
| DC-EVID-04 | DC-TST-BLOCK-*, DC-TST-CONTENT-A11Y-001 |
| DC-EVID-05 | DC-TST-BTREE-* |
| DC-EVID-06 | DC-TST-ORDER-*, DC-TST-EDIT-* |
| DC-EVID-07 | DC-TST-LINK-* |
| DC-EVID-08 | DC-TST-VER-*, DC-TST-SNAP-*, DC-TST-HIST-* |
| DC-EVID-09 | DC-TST-TPL-*, DC-TST-IMP-*, DC-TST-EXP-*, DC-TST-FILE-* |
| DC-EVID-10 | DC-TST-DAUTH-*, DC-TST-TEN-DOC-*, DC-TST-RLS-DOC-* |
| DC-EVID-11 | DC-TST-COM-*, DC-TST-COL-OPT-001 |
| DC-EVID-12 | DC-TST-TGT-* |
| DC-EVID-13 | DC-TST-MEN-* |
| DC-EVID-14 | DC-TST-REACT-* |
| DC-EVID-15 | DC-TST-ATT-*, DC-TST-ATT-UPLOAD-001 |
| DC-EVID-16 | DC-TST-PRES-*, DC-TST-PRES-EPH-001 |
| DC-EVID-17 | DC-TST-READ-*, DC-TST-WATCH-* |
| DC-EVID-18 | DC-TST-NOTIF-*, DC-TST-ACT-*, DC-TST-COL-SEARCH-001 |
| DC-EVID-19 | DC-TST-CAUTH-*, DC-TST-TEN-COL-*, DC-TST-RET-* |
| DC-EVID-20 | DC-TST-DQRY-*, DC-TST-CQRY-*, DC-TST-PMETA-API-001, OpenAPI drift |
| DC-EVID-21 | DC-TST-EVT-*, DC-TST-DEDUP-001, DC-TST-POISON-001 |
| DC-EVID-22 | DC-TST-RT-*, DC-TST-PRES-RT-001, DC-TST-OFF-001, DC-TST-COL-OPT-001 |
| DC-EVID-23 | DC-TST-MIG-*, DC-TST-RET-DOC-001 |
| DC-EVID-24 | DC-TST-FAIL-*, DC-TST-IDEM-001, DC-TST-DEDUP-001, DC-TST-POISON-001 |
| DC-EVID-25 | DC-TST-SEC-*, DC-TST-TEN-*, DC-TST-RLS-*, DC-TST-RET-* |
| DC-EVID-26 | DC-TST-PERF-*, DC-TST-OBS-* |
| DC-EVID-27 | DC-TST-X-*, DC-TST-X-DOC-INT-001, DC-TST-X-COL-INT-001 |
| DC-EVID-28 | DC-TST-TRACE-001, exact-SHA suite counts, CI/artifact drift gates |

---

## 247C. Bundle completeness

Final automated traceability target:

```text
DC-EVID bundles defined: 28
DC-EVID bundles mapped:  28
unmapped required bundle: 0
```

---


## 247D. Normative direct TEST → evidence-bundle index

The family table above is convenient for humans.

This direct index is normative for machine comparison with CERTIFICATION.

Each defined `DC-TST-*` ID must appear exactly once below with its complete evidence-bundle set.

| TEST ID | Evidence bundle(s) |
|---|---|
| `DC-TST-ACT-API-001` | DC-EVID-18 |
| `DC-TST-ACT-ARCH-001` | DC-EVID-18 |
| `DC-TST-ACT-PRIV-001` | DC-EVID-18 |
| `DC-TST-ACT-PROJ-001` | DC-EVID-18 |
| `DC-TST-ARCH-BOUND-001` | DC-EVID-27 |
| `DC-TST-ARCH-CMS-001` | DC-EVID-27 |
| `DC-TST-ARCH-CRDT-001` | DC-EVID-27 |
| `DC-TST-ARCH-REF-001` | DC-EVID-25, DC-EVID-27 |
| `DC-TST-ATT-APP-001` | DC-EVID-15 |
| `DC-TST-ATT-DOM-001` | DC-EVID-15 |
| `DC-TST-ATT-LIFE-001` | DC-EVID-15 |
| `DC-TST-ATT-SCOPE-001` | DC-EVID-15 |
| `DC-TST-ATT-SEC-001` | DC-EVID-15 |
| `DC-TST-ATT-UPLOAD-001` | DC-EVID-15 |
| `DC-TST-BLOCK-COMPAT-001` | DC-EVID-04 |
| `DC-TST-BLOCK-COMPAT-002` | DC-EVID-04 |
| `DC-TST-BLOCK-CONTENT-001` | DC-EVID-04 |
| `DC-TST-BLOCK-DOM-001` | DC-EVID-04 |
| `DC-TST-BLOCK-DOM-002` | DC-EVID-04 |
| `DC-TST-BLOCK-SCOPE-001` | DC-EVID-04 |
| `DC-TST-BTREE-APP-001` | DC-EVID-05 |
| `DC-TST-BTREE-DEL-001` | DC-EVID-05 |
| `DC-TST-BTREE-DOM-001` | DC-EVID-05 |
| `DC-TST-BTREE-DOM-002` | DC-EVID-05 |
| `DC-TST-BTREE-SEC-001` | DC-EVID-05 |
| `DC-TST-CAUTH-ARCH-001` | DC-EVID-19 |
| `DC-TST-CAUTH-CREATE-001` | DC-EVID-19 |
| `DC-TST-CAUTH-DELETE-001` | DC-EVID-19 |
| `DC-TST-CAUTH-EDIT-001` | DC-EVID-19 |
| `DC-TST-CAUTH-READ-001` | DC-EVID-19 |
| `DC-TST-CAUTH-REV-001` | DC-EVID-19 |
| `DC-TST-CAUTH-STATUS-001` | DC-EVID-19 |
| `DC-TST-COL-OPT-001` | DC-EVID-11, DC-EVID-22 |
| `DC-TST-COL-SEARCH-001` | DC-EVID-18 |
| `DC-TST-COM-ANCH-001` | DC-EVID-11 |
| `DC-TST-COM-ANCH-002` | DC-EVID-11 |
| `DC-TST-COM-DOM-001` | DC-EVID-11 |
| `DC-TST-COM-DTO-001` | DC-EVID-11 |
| `DC-TST-COM-HIST-001` | DC-EVID-11 |
| `DC-TST-COM-REPLY-001` | DC-EVID-11 |
| `DC-TST-COM-REPLY-002` | DC-EVID-11 |
| `DC-TST-COM-STATUS-001` | DC-EVID-11 |
| `DC-TST-COM-THREAD-DEL-001` | DC-EVID-11 |
| `DC-TST-COMPAT-MOB-001` | DC-EVID-20 |
| `DC-TST-CONTENT-A11Y-001` | DC-EVID-04 |
| `DC-TST-CQRY-001` | DC-EVID-20 |
| `DC-TST-CQRY-002` | DC-EVID-20 |
| `DC-TST-CQRY-PERF-001` | DC-EVID-20 |
| `DC-TST-DAUTH-ARCH-001` | DC-EVID-10 |
| `DC-TST-DAUTH-DEST-001` | DC-EVID-10 |
| `DC-TST-DAUTH-DISC-001` | DC-EVID-10 |
| `DC-TST-DAUTH-POL-001` | DC-EVID-10 |
| `DC-TST-DEDUP-001` | DC-EVID-21, DC-EVID-24 |
| `DC-TST-DQRY-001` | DC-EVID-20 |
| `DC-TST-DQRY-002` | DC-EVID-20 |
| `DC-TST-DQRY-PERF-001` | DC-EVID-20 |
| `DC-TST-DUP-001` | DC-EVID-02 |
| `DC-TST-EDIT-ARCH-001` | DC-EVID-06 |
| `DC-TST-EDIT-CONC-001` | DC-EVID-06 |
| `DC-TST-EDIT-NOOP-001` | DC-EVID-06 |
| `DC-TST-EVT-COM-001` | DC-EVID-21 |
| `DC-TST-EVT-DOC-001` | DC-EVID-21 |
| `DC-TST-EVT-MEN-001` | DC-EVID-21 |
| `DC-TST-EVT-PRIV-001` | DC-EVID-21 |
| `DC-TST-EVT-STUB-001` | DC-EVID-21 |
| `DC-TST-EVT-VER-001` | DC-EVID-21 |
| `DC-TST-EXP-001` | DC-EVID-09 |
| `DC-TST-FAIL-API-001` | DC-EVID-24 |
| `DC-TST-FAIL-DB-001` | DC-EVID-24 |
| `DC-TST-FAIL-DEP-001` | DC-EVID-24 |
| `DC-TST-FAIL-MSG-001` | DC-EVID-24 |
| `DC-TST-FAIL-RT-001` | DC-EVID-24 |
| `DC-TST-FILE-SEC-001` | DC-EVID-09 |
| `DC-TST-FILE-SEC-002` | DC-EVID-09 |
| `DC-TST-HIER-APP-001` | DC-EVID-03 |
| `DC-TST-HIER-AUTHZ-001` | DC-EVID-03 |
| `DC-TST-HIER-CONC-001` | DC-EVID-03 |
| `DC-TST-HIER-CONC-002` | DC-EVID-03 |
| `DC-TST-HIER-DOM-001` | DC-EVID-03 |
| `DC-TST-HIER-SEC-001` | DC-EVID-03 |
| `DC-TST-HIST-API-001` | DC-EVID-08 |
| `DC-TST-HIST-ARCH-001` | DC-EVID-08 |
| `DC-TST-HIST-POLICY-001` | DC-EVID-08 |
| `DC-TST-HIST-RESTORE-001` | DC-EVID-08 |
| `DC-TST-IDEM-001` | DC-EVID-24 |
| `DC-TST-IMP-001` | DC-EVID-09 |
| `DC-TST-INV-001` | DC-EVID-01 |
| `DC-TST-INV-QUAL-001` | DC-EVID-01 |
| `DC-TST-INV-STUB-001` | DC-EVID-01 |
| `DC-TST-LINK-AUTHZ-001` | DC-EVID-07 |
| `DC-TST-LINK-DOM-001` | DC-EVID-07 |
| `DC-TST-LINK-DOM-002` | DC-EVID-07 |
| `DC-TST-LINK-DOM-003` | DC-EVID-07 |
| `DC-TST-LINK-DOM-004` | DC-EVID-07 |
| `DC-TST-LINK-LIFE-001` | DC-EVID-07 |
| `DC-TST-MEN-APP-001` | DC-EVID-13 |
| `DC-TST-MEN-APP-002` | DC-EVID-13 |
| `DC-TST-MEN-DOM-001` | DC-EVID-13 |
| `DC-TST-MEN-EDIT-001` | DC-EVID-13 |
| `DC-TST-MEN-IDEM-001` | DC-EVID-13 |
| `DC-TST-MEN-PRIV-001` | DC-EVID-13 |
| `DC-TST-MIG-001` | DC-EVID-23 |
| `DC-TST-MIG-002` | DC-EVID-23 |
| `DC-TST-MIG-003` | DC-EVID-23 |
| `DC-TST-MIG-BLOCK-001` | DC-EVID-23 |
| `DC-TST-MIG-HIER-001` | DC-EVID-23 |
| `DC-TST-MIG-ORDER-001` | DC-EVID-23 |
| `DC-TST-MIG-SNAP-001` | DC-EVID-23 |
| `DC-TST-MIG-TGT-001` | DC-EVID-23 |
| `DC-TST-MIG-TPL-001` | DC-EVID-23 |
| `DC-TST-NOTIF-ARCH-001` | DC-EVID-18 |
| `DC-TST-NOTIF-ATTN-001` | DC-EVID-18 |
| `DC-TST-NOTIF-CHANNEL-001` | DC-EVID-18 |
| `DC-TST-NOTIF-DEL-001` | DC-EVID-18 |
| `DC-TST-NOTIF-FACT-001` | DC-EVID-18 |
| `DC-TST-NOTIF-IDEM-001` | DC-EVID-18 |
| `DC-TST-NOTIF-PREF-001` | DC-EVID-18 |
| `DC-TST-NOTIF-PRIV-001` | DC-EVID-18 |
| `DC-TST-NOTIF-RECIP-001` | DC-EVID-18 |
| `DC-TST-OBS-001` | DC-EVID-26 |
| `DC-TST-OBS-002` | DC-EVID-26 |
| `DC-TST-OBS-RT-001` | DC-EVID-26 |
| `DC-TST-OFF-001` | DC-EVID-22 |
| `DC-TST-ORDER-APP-001` | DC-EVID-06 |
| `DC-TST-ORDER-CONC-001` | DC-EVID-06 |
| `DC-TST-ORDER-DENSE-001` | DC-EVID-06 |
| `DC-TST-ORDER-DOM-001` | DC-EVID-06 |
| `DC-TST-ORDER-INF-001` | DC-EVID-06 |
| `DC-TST-OWN-ARCH-001` | DC-EVID-27 |
| `DC-TST-OWN-ARCH-002` | DC-EVID-27 |
| `DC-TST-PAGE-DEL-ORCH-001` | DC-EVID-02 |
| `DC-TST-PAGE-DOM-001` | DC-EVID-02 |
| `DC-TST-PAGE-DOM-002` | DC-EVID-02 |
| `DC-TST-PAGE-DOM-003` | DC-EVID-02 |
| `DC-TST-PAGE-RET-001` | DC-EVID-02 |
| `DC-TST-PAGE-SCOPE-001` | DC-EVID-02 |
| `DC-TST-PATH-001` | DC-EVID-02 |
| `DC-TST-PERF-ACT-001` | DC-EVID-26 |
| `DC-TST-PERF-ATTN-001` | DC-EVID-26 |
| `DC-TST-PERF-BLOCK-001` | DC-EVID-26 |
| `DC-TST-PERF-COL-001` | DC-EVID-26 |
| `DC-TST-PERF-PAGE-001` | DC-EVID-26 |
| `DC-TST-PMETA-API-001` | DC-EVID-02, DC-EVID-20 |
| `DC-TST-POISON-001` | DC-EVID-21, DC-EVID-24 |
| `DC-TST-PRES-AUTHZ-001` | DC-EVID-16 |
| `DC-TST-PRES-DOM-001` | DC-EVID-16 |
| `DC-TST-PRES-EPH-001` | DC-EVID-16 |
| `DC-TST-PRES-RT-001` | DC-EVID-16, DC-EVID-22 |
| `DC-TST-PRES-SEC-001` | DC-EVID-16 |
| `DC-TST-PVIS-AUTHZ-001` | DC-EVID-02 |
| `DC-TST-PVIS-DOM-001` | DC-EVID-02 |
| `DC-TST-PVIS-EVT-001` | DC-EVID-02 |
| `DC-TST-REACT-AUTHZ-001` | DC-EVID-14 |
| `DC-TST-REACT-DOM-001` | DC-EVID-14 |
| `DC-TST-REACT-DOM-002` | DC-EVID-14 |
| `DC-TST-REACT-INF-001` | DC-EVID-14 |
| `DC-TST-REACT-QRY-001` | DC-EVID-14 |
| `DC-TST-READ-AUTHZ-001` | DC-EVID-17 |
| `DC-TST-READ-CONC-001` | DC-EVID-17 |
| `DC-TST-READ-DOM-001` | DC-EVID-17 |
| `DC-TST-READ-QRY-001` | DC-EVID-17 |
| `DC-TST-RET-DOC-001` | DC-EVID-19, DC-EVID-23, DC-EVID-25 |
| `DC-TST-RET-IDENT-001` | DC-EVID-19, DC-EVID-25 |
| `DC-TST-RET-MATRIX-001` | DC-EVID-19, DC-EVID-25 |
| `DC-TST-RET-WS-001` | DC-EVID-19, DC-EVID-25 |
| `DC-TST-RLS-COL-001` | DC-EVID-25 |
| `DC-TST-RLS-DOC-001` | DC-EVID-10, DC-EVID-25 |
| `DC-TST-RT-COM-001` | DC-EVID-22 |
| `DC-TST-RT-CONV-001` | DC-EVID-22 |
| `DC-TST-RT-DOC-001` | DC-EVID-22 |
| `DC-TST-RT-DOC-002` | DC-EVID-22 |
| `DC-TST-RT-NOTIF-001` | DC-EVID-22 |
| `DC-TST-RT-REACT-001` | DC-EVID-22 |
| `DC-TST-RT-READ-001` | DC-EVID-22 |
| `DC-TST-RT-REV-001` | DC-EVID-22 |
| `DC-TST-SEARCH-SEC-001` | DC-EVID-20, DC-EVID-25 |
| `DC-TST-SEC-ABUSE-001` | DC-EVID-25 |
| `DC-TST-SEC-LOG-001` | DC-EVID-25 |
| `DC-TST-SEC-REV-001` | DC-EVID-25 |
| `DC-TST-SEC-RICH-001` | DC-EVID-25 |
| `DC-TST-SEC-SYS-001` | DC-EVID-25 |
| `DC-TST-SNAP-DOM-001` | DC-EVID-08 |
| `DC-TST-SRC-001` | DC-EVID-01 |
| `DC-TST-SRC-002` | DC-EVID-01 |
| `DC-TST-SRC-003` | DC-EVID-01 |
| `DC-TST-SRC-004` | DC-EVID-01 |
| `DC-TST-SRC-005` | DC-EVID-01 |
| `DC-TST-TEN-COL-001` | DC-EVID-19, DC-EVID-25 |
| `DC-TST-TEN-DOC-001` | DC-EVID-10, DC-EVID-25 |
| `DC-TST-TGT-ARCH-001` | DC-EVID-12 |
| `DC-TST-TGT-ITEM-001` | DC-EVID-12 |
| `DC-TST-TGT-PAGE-001` | DC-EVID-12 |
| `DC-TST-TGT-UNKNOWN-001` | DC-EVID-12 |
| `DC-TST-TPL-CONTENT-001` | DC-EVID-09 |
| `DC-TST-TPL-DOM-001` | DC-EVID-09 |
| `DC-TST-TPL-IDEMP-001` | DC-EVID-09 |
| `DC-TST-TPL-INST-001` | DC-EVID-09 |
| `DC-TST-TRACE-001` | DC-EVID-01, DC-EVID-28 |
| `DC-TST-TX-BLOCK-001` | DC-EVID-24 |
| `DC-TST-TX-COMMENT-001` | DC-EVID-24 |
| `DC-TST-TX-PAGE-001` | DC-EVID-24 |
| `DC-TST-VER-DOM-001` | DC-EVID-08 |
| `DC-TST-WATCH-AUTHZ-001` | DC-EVID-17 |
| `DC-TST-WATCH-DOM-001` | DC-EVID-17 |
| `DC-TST-WATCH-IDEM-001` | DC-EVID-17 |
| `DC-TST-WATCH-RULE-001` | DC-EVID-17 |
| `DC-TST-WATCH-SCOPE-001` | DC-EVID-17 |
| `DC-TST-X-ACT-001` | DC-EVID-27 |
| `DC-TST-X-ANA-001` | DC-EVID-27 |
| `DC-TST-X-AUT-001` | DC-EVID-27 |
| `DC-TST-X-COL-INT-001` | DC-EVID-27 |
| `DC-TST-X-DOC-INT-001` | DC-EVID-27 |
| `DC-TST-X-TGT-UNCHANGED-001` | DC-EVID-27 |
| `DC-TST-X-WM-READ-001` | DC-EVID-27 |
| `DC-TST-XTX-ARCH-001` | DC-EVID-24, DC-EVID-27 |

The direct index and the family table must remain semantically equivalent.

`DC-TST-TRACE-001` must fail if:

```text
a defined TEST has no direct bundle row
a bundle set differs from CERTIFICATION
a direct row references an undefined TEST
```

---

# CI rules

## 248. Exact candidate SHA

All final evidence records:

```text
candidate SHA
command/job
selected count
passed
failed
skipped
```

---

## 249. Zero selected tests

```text
0 selected
```

is not PASS.

It is:

```text
BLOCKED-VERIFICATION
```

---

## 250. Required suite families

Use existing repository projects wherever possible:

```text
Architecture
Domain
Application
Infrastructure
Platform
API
Integration
```

Do not create one new test project per capability without architectural need.

---

## 251. Real DB requirement

Properties requiring PostgreSQL semantics must use real PostgreSQL, including as applicable:

```text
RLS
unique constraints
concurrency
ordering persistence
outbox transaction
migration
projection persistence
```

---

## 252. Production DI requirement

Critical integration evidence should use the production composition path where the property depends on:

```text
middleware
pipeline behaviors
authorization
interceptors
mappers
cross-context adapters
```

---

## 253. OpenAPI drift

If any public contract changes:

```text
canonical generation
→ compare
→ intentional review
```

No automatic update-before-compare.

---

## 254. Event manifest drift

Same rule.

Unrelated bounded-context drift blocks closure until classified.

---

# Normative traceability verification

## 254A. DC-TST-TRACE-001 — execution-package traceability is closed

This Architecture/documentation gate validates:

```text
Product
→ SPEC DCREQ
→ PLAN work unit
→ TEST ID/family
→ DC-EVID
→ CERTIFICATION
```

It must fail when:

```text
any DCREQ001..220 is absent from PLAN or TESTS
a normative PLAN work-unit row maps to no TEST family
a defined DC-TST ID is unconsumed by evidence/certification unless explicitly helper-only
a referenced DC-TST ID is undefined
any DC-EVID-01..28 has no TEST mapping
the normative direct TEST→DC-EVID bundle set differs from CERTIFICATION
any DC-SRC-DOC-001..010 or DC-SRC-COL-001..010 has no TEST/CERT closure path
Product/team DCT/COL namespace is ambiguous
```

Expected closure target:

```text
DCREQ definitions:             220/220
PLAN DCREQ coverage:           220/220
TESTS DCREQ coverage:          220/220
PLAN normative work-unit rows: 100%
defined TEST IDs consumed:     100%
undefined TEST references:     0
DC-EVID bundles mapped:        28/28
source-debt closure paths:     20/20
```

This proves documentation traceability only, not runtime behavior.

---

# Anti-patterns

## 255. Do not infer full capability from Domain tests

Example:

```text
ResourceWatcherTests green
```

does not prove Watch API, authorization, persistence uniqueness, notifications, or realtime.

---

## 256. Do not infer permission from ResourceRef possession

A valid GUID/kind is not authorization proof.

---

## 257. Do not use one target's policy to certify another

```text
BoardItem Comment PASS
```

does not certify Page Comment.

---

## 258. Do not certify current action mappings by compile success

`ManageBoard`, `ViewBoard`, `UpdateItem` on Documents/Comment resources require semantic evidence.

---

## 259. Do not test concurrency sequentially

Use overlapping operations.

---

## 260. Do not use in-memory provider for RLS/constraint claims

---

## 261. Do not certify history from constant empty response

---

## 262. Do not certify Activity from registered consumer alone

Need actual projection/query or explicit non-release classification.

---

## 263. Do not certify Notification from provider consumer alone

Transport consumer != durable Notification product.

---

## 264. Do not certify realtime from one SignalR delivery

---

## 265. Do not certify content compatibility against live Blocks only

Also test:

```text
DocumentSnapshot
DocumentVersion
PageTemplate
```

when they retain serialized content.

---

## 266. Do not treat unscoped ResourceRef Domain acceptance as authorization proof

Some current Domain tests allow ResourceRef without WorkspaceId.

Application/runtime must still establish authoritative scope before release operation.

---

# Certification handoff

## 267. Per-capability evidence row

```text
Capability:
Release classification:
Product requirement(s):
SPEC requirement(s):
PLAN work unit(s):
Source debt ID(s):
Existing tests reused:
New test IDs:
Test project:
Command/job:
Selected:
Passed:
Failed:
Skipped:
Candidate SHA:
DB/migration state:
OpenAPI/event artifact:
Blockers:
Status:
```

---

## 268. Existing-test reuse record

For each reused suite:

```text
Test class:
Candidate SHA:
Property proven:
Why semantics still current:
Production relevant?:
Gaps remaining:
Classification:
  EXISTING_SUFFICIENT
  | EXISTING_PARTIAL
  | EXISTING_STALE
```

---

# Review checklist

## 269. Documents Core

- [ ] Page identity/scope;
- [ ] archive/delete/restore;
- [ ] metadata API parity;
- [ ] visibility semantics;
- [ ] Page cycles;
- [ ] MovePage real implementation;
- [ ] move concurrency;
- [ ] Block identity/content;
- [ ] Block parent scope;
- [ ] Block cycles;
- [ ] child delete policy;
- [ ] ordering dense/concurrent/roundtrip;
- [ ] stale mutation;
- [ ] no-op;
- [ ] query parity;
- [ ] request/action matrix;
- [ ] A→B isolation.

---

## 270. Documents Full

- [ ] ResourceLinks;
- [ ] link authorization/non-transitivity;
- [ ] Version policy;
- [ ] Snapshot schema compatibility;
- [ ] real Page history;
- [ ] restore;
- [ ] Template lifecycle/content;
- [ ] search permission filtering;
- [ ] import/export if released;
- [ ] file/media reference security.

---

## 271. Collaboration Core

- [ ] Comment lifecycle;
- [ ] reply same-target;
- [ ] deleted-parent rule;
- [ ] Anchor if released;
- [ ] resolve/reopen contract;
- [ ] Page target;
- [ ] BoardItem target;
- [ ] unknown target;
- [ ] no foreign persistence;
- [ ] action matrix;
- [ ] target revocation;
- [ ] query ordering;
- [ ] tenant isolation;
- [ ] target retention.

---

## 272. Collaboration Full

- [ ] Mention validation/edit diff/idempotency;
- [ ] Reaction uniqueness/persistence race;
- [ ] Attachment target/security/retention;
- [ ] Presence auth/expiry;
- [ ] ReadState monotonicity;
- [ ] Watcher idempotency/access;
- [ ] Notification classification/recipient/privacy;
- [ ] Activity projection/query.

---

## 273. Messaging/reliability

- [ ] Page outbox;
- [ ] Comment outbox;
- [ ] Comment+Mention rollback;
- [ ] target unchanged;
- [ ] dedup;
- [ ] poison;
- [ ] post-commit failure isolation.

---

## 274. Realtime

- [ ] state durability classified;
- [ ] duplicate;
- [ ] reorder;
- [ ] gap;
- [ ] reconnect;
- [ ] permission revoke;
- [ ] final convergence;
- [ ] Presence rebuild/expiry.

---

## 275. Migration

- [ ] clean DB;
- [ ] supported upgrade/fresh-baseline rationale;
- [ ] pending model;
- [ ] hierarchy;
- [ ] ordering;
- [ ] live content;
- [ ] snapshot/version content;
- [ ] template content;
- [ ] target refs.

---

## 276. Source debt

- [ ] DC-SRC-DOC-001;
- [ ] DC-SRC-DOC-002;
- [ ] DC-SRC-DOC-003;
- [ ] DC-SRC-DOC-004;
- [ ] DC-SRC-DOC-005;
- [ ] DC-SRC-DOC-006;
- [ ] DC-SRC-DOC-007;
- [ ] DC-SRC-DOC-008;
- [ ] DC-SRC-DOC-009;
- [ ] DC-SRC-DOC-010;
- [ ] DC-SRC-COL-001;
- [ ] DC-SRC-COL-002;
- [ ] DC-SRC-COL-003;
- [ ] DC-SRC-COL-004;
- [ ] DC-SRC-COL-005;
- [ ] DC-SRC-COL-006;
- [ ] DC-SRC-COL-007;
- [ ] DC-SRC-COL-008;
- [ ] DC-SRC-COL-009;
- [ ] DC-SRC-COL-010.

Every row must have final disposition/evidence before full-scope certification.

---

# Definition of Done

## 277. TESTS artifact is complete when

The verification package can prove or explicitly block every release-scoped requirement in:

```text
PROD-DCT-001..031
PROD-COL-001..029
DCREQ001..220
```

and every known `DC-SRC-*` debt has a regression/retirement/classification gate.

---

## 278. Core completion does not require all optional/full capabilities

A P4B Core candidate may omit explicitly non-release:

```text
Templates
Reactions
Presence
Watchers
Notification
Activity
etc.
```

only when:

```text
PLAN scope says so
TESTS marks N/A/out-of-release with evidence
CERTIFICATION does not call them complete
```

---

## 279. Full release completion

Full release tests require every release-scoped canonical capability, including `DCREQ201..220`, to have:

```text
Domain proof where applicable
Application proof
API proof if public
authorization/tenant proof
persistence proof
migration compatibility
failure proof
realtime proof if realtime
exact-SHA CI
```

---

## 280. Final verification rule

Before certification, ask:

```text
Can Page/Block structure survive stale/concurrent mutation?

Can metadata/visibility be trusted end-to-end?

Can historical Block content still be interpreted in live state,
snapshots, versions and templates?

Can a ResourceLink expose relationship without owning target permission?

Can Comments/replies/anchors remain target-safe?

Can Mention/Reaction/Attachment/Watcher/ReadState operate without
granting foreign access?

Can Presence disappear/reconnect without corrupting durable truth?

Can Notification/Activity be distinguished from provider delivery and Audit?

Can every target, tenant, migration, retry and reconnect path fail closed?

Does every test result belong to the exact candidate SHA?

Did every critical filtered suite execute non-zero tests?
```

If any release-required answer is not proven:

```text
the corresponding capability is not VERIFIED
```

That rule is mandatory.
