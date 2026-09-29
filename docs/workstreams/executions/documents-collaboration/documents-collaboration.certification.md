---
document_id: WRK-CERT-DOCUMENTS-COLLABORATION
document_type: workstream-certification
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
  - ci
evidence:
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.spec.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.plan.md
  - docs/workstreams/executions/documents-collaboration/documents-collaboration.tests.md
  - docs/product/documents.md
  - docs/product/collaboration.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/tests/
review_on:
  - documents-collaboration-spec-change
  - documents-collaboration-plan-change
  - documents-collaboration-tests-change
  - source-debt-disposition-change
  - release-scope-change
  - authorization-action-change
  - target-contract-change
  - migration-change
  - realtime-recovery-change
  - exact-sha-change
---

# CERTIFICATION — Documents & Collaboration

## 1. Purpose

This document is the final evidence ledger and release gate for Documents & Collaboration.

It does not declare the current source complete.

It records:

```text
candidate SHA
release scope
capability readiness
source-debt disposition
test execution
migration state
generated-contract state
security/reliability/realtime evidence
blocking/non-blocking debt
final decision
```

Certification is evidence, not intent.

---

## 2. Authority chain

A valid decision follows:

```text
Product authority
→ SPEC
→ PLAN
→ TESTS
→ source/runtime/migrations
→ exact-SHA execution
→ CI
→ certification
```

Source existence alone is never certification.

---

## 3. Audited baseline

This artifact was rebuilt against:

```text
branch: develop
SHA:    35702d0fa9fb01ed68b0667bab500030d60bd028
```

Future certification must recapture the actual candidate SHA.

---

## 4. Initial state

Before execution:

```text
Documents Core:             NOT_EVALUATED
Documents Full Release:     NOT_EVALUATED
Collaboration Core:         NOT_EVALUATED
Collaboration Full Release: NOT_EVALUATED
Target Support:             NOT_EVALUATED
Events/Handoffs:            NOT_EVALUATED
Realtime/Recovery:          NOT_EVALUATED
P4B Core:                   NOT_EVALUATED
Full Workstream:            NOT_EVALUATED
```

No PASS/STABLE is prefilled.

---

## 4A. Product/team requirement namespace rule

This CERTIFICATION artifact inherits the SPEC authority normalization:

```text
PROD-DCT-001..031
→ docs/product/documents.md

PROD-COL-001..029
→ docs/product/collaboration.md

TEAM-DCT-001..012
TEAM-COL-001..008
→ team capability aliases only
```

A certification record must not use a bare `DCT-*` / `COL-*` identifier when the authority source is ambiguous.

Certification is keyed by:

```text
DCREQ*
DC-TST-*
DC-EVID-*
DC-SRC-*
```

---

# Status model

## 5. Allowed statuses

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
NOT_APPLICABLE
```

`VERIFIED = D4`.

`STABLE = D5`.

---

## 6. NOT_APPLICABLE rule

`NOT_APPLICABLE` requires:

```text
release-scope evidence
authority
source disposition
reviewer
```

No API is not enough.

---

# Evidence quality

## 7. Evidence levels

```text
E0 intent/docs
E1 static source
E2 unit/domain/application evidence
E3 production-wiring integration evidence
E4 security/concurrency/failure/migration evidence
E5 exact-SHA CI + downstream-stability proof
```

D4 normally needs E2 + E3 + applicable E4.

D5 requires D4 + E5 and no critical debt.

---

# Candidate identity

## 8. Candidate record

Populate:

```text
Branch:
Baseline SHA:
Candidate SHA:
Working tree clean?:

Release scope:
  P4B Core:
  Documents Full:
  Collaboration Full:
  Realtime:

Database:
  engine/version:
  application role:
  migration head:
  pending-model status:

Generated artifacts:
  OpenAPI:
  Event manifest:

CI:
  workflow:
  run:
  exact SHA:

Reviewer(s):
Execution date:
```

---

## 9. Exact-SHA rule

Do not mix evidence from different:

```text
source commits
migration states
generated artifacts
working trees
CI runs
runtime configurations
```

unless explicitly proven irrelevant.

---

## 10. Non-zero execution

Each critical suite records:

```text
selected
passed
failed
skipped
```

`selected = 0` is `BLOCKED-VERIFICATION`.

---

# Milestones

## 11. Milestone A — Documents Core Stable

Includes:

```text
Page identity/lifecycle
Page metadata
Page visibility if exposed
Page hierarchy
Block identity/content
Block hierarchy
Block ordering
query/API
Documents authorization
tenant isolation
```

---

## 12. Milestone B — Documents Full Release Stable

Adds release-scoped:

```text
ResourceLinks
Versions/Snapshots
history/restore
Templates
search/index
import/export
file/media references
Documents events
Documents realtime
```

---

## 13. Milestone C — Collaboration Core Stable

Includes:

```text
Comment lifecycle
reply/thread
CommentAnchor if released
status lifecycle
target contract
authorization
query
tenant isolation
retention
```

---

## 14. Milestone D — Collaboration Full Release Stable

Adds release-scoped:

```text
Mention
Reaction
Attachment
Presence
ReadState
Watcher
Notification
Activity
Collaboration realtime
```

---

## 15. Milestone E — Realtime Verified

Evaluated per state type.

---

## 16. Milestone F — P4B Core Certified

Requires Documents Core + Collaboration Core + required target support + cross-cutting gates.

---

## 17. Milestone G — Full Workstream Certified

Requires every release-scoped canonical capability plus cross-cutting closure.

---

# Upstream prerequisites

## 18. Actor/Account/Workspace

Required:

```text
Actor D5
Account D5
Workspace D5
```

Current:

```text
NOT_EVALUATED
```

---

## 19. Governance

Required:

```text
resource/action semantics D5
authorization enforcement D5
```

Current:

```text
NOT_EVALUATED
```

---

## 20. Platform messaging

Where async behavior is released:

```text
outbox D5
message identity D5
dedup D4+
poison handling D4+
```

Current:

```text
NOT_EVALUATED
```

---

## 21. Platform realtime recovery

For durable realtime:

```text
reconnect/gap/recovery D4+
```

Transport-only support is insufficient.

Current:

```text
NOT_EVALUATED
```

---

# Source-debt closure

## 22. Source-debt rule

No full certification while required `DC-SRC-*` rows are blank.

Final dispositions:

```text
RESOLVED-IMPLEMENTED
RESOLVED-HARDENED
RESOLVED-RETIRED
RESOLVED-OUT-OF-SCOPE
BLOCKED-DECISION
BLOCKED-UPSTREAM
```

---

## 23. Documents source debt

| ID | Audited state | Required closure | Final disposition |
|---|---|---|---|
| DC-SRC-DOC-001 | `MovePage` throws `NotImplementedException` | real handler + hierarchy/auth/concurrency proof | |
| DC-SRC-DOC-002 | `PublishPage` placeholder; product semantics unresolved | product decision + implement or retire | |
| DC-SRC-DOC-003 | `SetPageDeadline` placeholder; product semantics unresolved | product decision + implement or retire | |
| DC-SRC-DOC-004 | `GetPageHistory` always returns empty | real history or route retirement | |
| DC-SRC-DOC-005 | Page update request fields discarded | request→Application→persistence parity or removal | |
| DC-SRC-DOC-006 | PageVisibility incomplete vertical slice | release classification + full slice if released | |
| DC-SRC-DOC-007 | Documents requests reuse Board actions | accepted action matrix + policy tests | |
| DC-SRC-DOC-008 | ResourceLinks thin vertical slice | classify + implement if released | |
| DC-SRC-DOC-009 | Versions/Snapshots incomplete vertical slice | classify + history/restore if released | |
| DC-SRC-DOC-010 | Templates thin vertical slice | classify + implement if released | |

Current:

```text
NOT_EVALUATED
```

---

## 24. Collaboration source debt

| ID | Audited state | Required closure | Final disposition |
|---|---|---|---|
| DC-SRC-COL-001 | Activity projection/store exists; resource query is constant-empty/unwired | wire query to existing projection or retire/defer endpoint | |
| DC-SRC-COL-002 | Mention creation exists; lifecycle incomplete | validate/edit-diff/idempotency/privacy | |
| DC-SRC-COL-003 | Reactions Domain/persistence only | classify + implement if released | |
| DC-SRC-COL-004 | Presence Domain/persistence only | classify + implement if released | |
| DC-SRC-COL-005 | ReadState Domain/persistence only | classify + implement if released | |
| DC-SRC-COL-006 | Watchers Domain/persistence only | classify + implement if released | |
| DC-SRC-COL-007 | durable Notification records/runtime exist; Collaboration semantic/Application/API closure is incomplete | reconcile existing store/runtime with product semantics and release surface | |
| DC-SRC-COL-008 | Comment Domain richer than exposed surface | classify/complete lifecycle capabilities | |
| DC-SRC-COL-009 | Collaboration Comment/Attachment/Activity and future released requests require resource/action reconciliation | accepted Collaboration-wide action matrix + policy tests | |
| DC-SRC-COL-010 | generic Attachment Domain; BoardItem-centric public API | supported-target decision | |

Current:

```text
NOT_EVALUATED
```

---

## 24A. Source inventory / placeholder closure gate

Tests:

```text
DC-TST-INV-001
DC-TST-INV-QUAL-001
DC-TST-INV-STUB-001
DC-TST-SRC-001
DC-TST-SRC-004
```

Required outcome:

```text
all canonical source areas inventoried
all reused evidence quality-classified
no release-scoped placeholder/stub hidden
Activity query debt explicitly closed or blocking
```

Current:

```text
NOT_EVALUATED
```

---

## 24B. Public API/error semantics gate

Test:

```text
DC-TST-FAIL-API-001
```

Required for any changed/released public operation.

The candidate must preserve intentional error classes for:

```text
validation
not-found / existence-hiding
forbidden
conflict
unsupported content/target
dependency failure
```

Current:

```text
NOT_EVALUATED
```

---

## 24C. Execution traceability gate

Test:

```text
DC-TST-TRACE-001
```

Required result:

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

A behavioral suite may be green while this gate is BLOCKED.

Current:

```text
NOT_EVALUATED
```

---

# Documents Core

## 25. Page identity/scope

Requirements:

```text
DCREQ001
DCREQ002
DCREQ072
DCREQ073
```

Tests:

```text
DC-TST-PAGE-DOM-001
DC-TST-PAGE-SCOPE-001
```

Required D5:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 26. Page lifecycle

Requirements:

```text
DCREQ003–005
```

Tests:

```text
DC-TST-PAGE-DOM-002
DC-TST-PAGE-DOM-003
DC-TST-PAGE-RET-001
```

Current:

```text
NOT_EVALUATED
```

---

## 27. Page metadata

Requirements:

```text
DCREQ113
DCREQ197
```

Tests:

```text
DC-TST-PMETA-API-001
DC-TST-SRC-003
```

No ignored public field may remain.

Current:

```text
NOT_EVALUATED
```

---

## 28. Page visibility

Requirements:

```text
DCREQ114–116
```

Tests:

```text
DC-TST-PVIS-DOM-001
DC-TST-PVIS-AUTHZ-001
DC-TST-PVIS-EVT-001
```

May be `NOT_APPLICABLE` only if release excludes visibility mutation explicitly.

Current:

```text
NOT_EVALUATED
```

---

## 29. Page hierarchy/reparent

Requirements:

```text
DCREQ006–010
DCREQ034
```

Tests:

```text
DC-TST-HIER-DOM-001
DC-TST-HIER-APP-001
DC-TST-HIER-AUTHZ-001
DC-TST-HIER-SEC-001
DC-TST-HIER-CONC-001
DC-TST-HIER-CONC-002
```

Current:

```text
NOT_EVALUATED
```

---

## 30. Block identity/content

Requirements:

```text
DCREQ011–017
```

Tests:

```text
DC-TST-BLOCK-DOM-001
DC-TST-BLOCK-SCOPE-001
DC-TST-BLOCK-DOM-002
DC-TST-BLOCK-CONTENT-001
DC-TST-BLOCK-COMPAT-001
DC-TST-BLOCK-COMPAT-002
```

Current:

```text
NOT_EVALUATED
```

---

## 31. Block hierarchy

Requirements:

```text
DCREQ117–121
```

Tests:

```text
DC-TST-BTREE-DOM-001
DC-TST-BTREE-DOM-002
DC-TST-BTREE-APP-001
DC-TST-BTREE-SEC-001
DC-TST-BTREE-DEL-001
```

Current:

```text
NOT_EVALUATED
```

---

## 32. Block ordering

Requirements:

```text
DCREQ018–022
```

Tests:

```text
DC-TST-ORDER-DOM-001
DC-TST-ORDER-DENSE-001
DC-TST-ORDER-APP-001
DC-TST-ORDER-CONC-001
DC-TST-ORDER-INF-001
```

Current:

```text
NOT_EVALUATED
```

---

## 33. Editing/concurrency/no-op

Requirements:

```text
DCREQ027–031
DCREQ136
DCREQ137
```

Tests:

```text
DC-TST-EDIT-CONC-001
DC-TST-EDIT-NOOP-001
DC-TST-EDIT-ARCH-001
```

Current:

```text
NOT_EVALUATED
```

---

## 34. Query/search boundary

Requirements:

```text
DCREQ023–026
DCREQ140
DCREQ141
```

Tests:

```text
DC-TST-DQRY-001
DC-TST-DQRY-002
DC-TST-DQRY-PERF-001
DC-TST-SEARCH-SEC-001
```

Current:

```text
NOT_EVALUATED
```

---

## 35. Documents authorization

Requirements:

```text
DCREQ032–035
DCREQ107
DCREQ109
DCREQ191
```

Tests:

```text
DC-TST-DAUTH-ARCH-001
DC-TST-DAUTH-POL-001
DC-TST-DAUTH-DEST-001
DC-TST-DAUTH-DISC-001
DC-TST-SRC-005
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 36. Documents tenant isolation

Tests:

```text
DC-TST-TEN-DOC-001
DC-TST-RLS-DOC-001 where applicable
DC-TST-SEC-SYS-001
DC-TST-SEC-REV-001
```

Required:

```text
STABLE
```

Any cross-tenant leak is blocking.

Current:

```text
NOT_EVALUATED
```

---

## 36A. Page deletion orchestration

Requirements:

```text
DCREQ004
DCREQ220
```

Test:

```text
DC-TST-PAGE-DEL-ORCH-001
```

Certification must prove product deletion semantics are orchestrated across applicable:

```text
Blocks
ResourceLinks
history/version retention
search/index
file/reference retention
producer events
```

Cross-context Collaboration reaction remains separately governed by `DCREQ189`.

ORM cascade alone is not evidence.

Current:

```text
NOT_EVALUATED
```

---

## 37. Documents Core milestone

Required:

```text
Page identity/scope STABLE
Page lifecycle STABLE
Page metadata VERIFIED+
Page visibility release-appropriate
Page hierarchy VERIFIED+
Block identity/content VERIFIED+
Block hierarchy VERIFIED+
Block ordering VERIFIED+
editing/concurrency VERIFIED+
query/search VERIFIED+
authorization STABLE
tenant isolation STABLE
Page deletion orchestration VERIFIED+
```

plus:

```text
core source debt closed
migration/security green
exact-SHA CI green
```

Current:

```text
NOT_EVALUATED
```

---

# Documents Full Release

## 38. ResourceLinks

Requirements:

```text
DCREQ122–127
```

Tests:

```text
DC-TST-LINK-DOM-001..004
DC-TST-LINK-AUTHZ-001
DC-TST-LINK-LIFE-001
```

Current:

```text
NOT_EVALUATED
```

---

## 39. Versions/Snapshots

Requirements:

```text
DCREQ128–131
DCREQ136
DCREQ194
```

Tests:

```text
DC-TST-VER-DOM-001
DC-TST-SNAP-DOM-001
DC-TST-HIST-POLICY-001
DC-TST-MIG-SNAP-001
```

Current:

```text
NOT_EVALUATED
```

---

## 40. Page history

Requirements:

```text
DCREQ132
DCREQ198
```

Tests:

```text
DC-TST-HIST-API-001
DC-TST-SRC-002
```

Current audited stub prevents certification until resolved.

Current:

```text
NOT_EVALUATED
```

---

## 41. Restore/history ownership

Requirements:

```text
DCREQ133
DCREQ134
```

Tests:

```text
DC-TST-HIST-RESTORE-001
DC-TST-HIST-ARCH-001
```

Current:

```text
NOT_EVALUATED
```

---

## 42. PageTemplate

Requirements:

```text
DCREQ135
DCREQ138
DCREQ139
DCREQ194
```

Tests:

```text
DC-TST-TPL-DOM-001
DC-TST-TPL-IDEMP-001
DC-TST-TPL-CONTENT-001
DC-TST-TPL-INST-001
DC-TST-MIG-TPL-001
```

Current:

```text
NOT_EVALUATED
```

---

## 43. Search/index

Tests:

```text
DC-TST-SEARCH-SEC-001
DC-TST-DQRY-PERF-001
```

Current:

```text
NOT_EVALUATED
```

---

## 44. Import/export

Tests:

```text
DC-TST-IMP-001
DC-TST-EXP-001
```

May be `NOT_APPLICABLE` if not released.

Current:

```text
NOT_EVALUATED
```

---

## 45. Document file/media references

Tests:

```text
DC-TST-FILE-SEC-001
DC-TST-FILE-SEC-002
```

Current:

```text
NOT_EVALUATED
```

---

## 46. Documents Full milestone

Requires Documents Core STABLE plus every released capability above, Documents events/realtime if claimed, and compatibility across live/history/template content.

Current:

```text
NOT_EVALUATED
```

---

## 46A. Derived Page path

Requirements:

```text
DCREQ201
```

Test:

```text
DC-TST-PATH-001
```

If Page path is release-visible, rename/reparent may change the derived path but must preserve Page identity.

If no public/release path exists:

```text
NOT_APPLICABLE
```

with scope evidence.

Current:

```text
NOT_EVALUATED
```

---

## 46B. Duplicate Page

Requirements:

```text
DCREQ202
```

Test:

```text
DC-TST-DUP-001
```

If released, duplicate Page/Blocks require fresh identities and explicit ResourceLink/file/history/Collaboration behavior.

Otherwise:

```text
NOT_APPLICABLE / OUT_OF_RELEASE_SCOPE
```

with authority-backed evidence.

Current:

```text
NOT_EVALUATED
```

---

## 46C. Documents provider-integration boundary

Requirements:

```text
DCREQ203
```

Test:

```text
DC-TST-X-DOC-INT-001
```

If released, provider payloads must enter through approved Documents operations and typed Block validation.

Current:

```text
NOT_EVALUATED
```

---

## 46D. Documents retention across derived/historical copies

Requirements:

```text
DCREQ204
```

Test:

```text
DC-TST-RET-DOC-001
```

Required disposition covers applicable:

```text
live Page/Block
Version/Snapshot
search/index
exports
backups
file references
```

Current:

```text
NOT_EVALUATED
```

---

## 46E. Reduced/mobile client compatibility

Requirements:

```text
DCREQ205
```

Test:

```text
DC-TST-COMPAT-MOB-001
```

If compatibility is claimed, unsupported typed content may not be silently destroyed.

Current:

```text
NOT_EVALUATED
```

---

## 46F. Offline editing

Requirements:

```text
DCREQ206
```

Test:

```text
DC-TST-OFF-001
```

Without an accepted offline mutation protocol:

```text
NOT_APPLICABLE / OUT_OF_RELEASE_SCOPE
```

is the expected result.

Realtime reconnect is not sufficient evidence.

Current:

```text
NOT_EVALUATED
```

---

## 46G. Accessibility-preserving Block contract

Requirements:

```text
DCREQ207
```

Test:

```text
DC-TST-CONTENT-A11Y-001
```

Backend typed content must preserve semantic fields needed for accessible clients.

Current:

```text
NOT_EVALUATED
```

---

# Collaboration Core

## 47. Comment lifecycle

Tests:

```text
DC-TST-COM-DOM-001
```

Current:

```text
NOT_EVALUATED
```

---

## 48. Replies/thread

Tests:

```text
DC-TST-COM-REPLY-001
DC-TST-COM-REPLY-002
DC-TST-CQRY-002
```

Current:

```text
NOT_EVALUATED
```

---

## 49. CommentAnchor

Tests:

```text
DC-TST-COM-ANCH-001
DC-TST-COM-ANCH-002
```

Current:

```text
NOT_EVALUATED
```

---

## 50. Comment status

Tests:

```text
DC-TST-COM-STATUS-001
DC-TST-COM-DTO-001
DC-TST-CAUTH-STATUS-001
```

Current:

```text
NOT_EVALUATED
```

---

## 51. Target representation

Tests:

```text
DC-TST-TGT-ARCH-001
DC-TST-TGT-UNKNOWN-001
DC-TST-X-TGT-UNCHANGED-001
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 52. Page target

Tests:

```text
DC-TST-TGT-PAGE-001
DC-TST-CAUTH-CREATE-001
DC-TST-CAUTH-READ-001
DC-TST-RET-MATRIX-001
```

Current:

```text
NOT_EVALUATED
```

---

## 53. BoardItem target

Tests:

```text
DC-TST-TGT-ITEM-001
DC-TST-CAUTH-CREATE-001
DC-TST-CAUTH-READ-001
DC-TST-RET-MATRIX-001
```

Current:

```text
NOT_EVALUATED
```

---

## 54. Comment authorization

Tests:

```text
DC-TST-CAUTH-ARCH-001
DC-TST-CAUTH-CREATE-001
DC-TST-CAUTH-READ-001
DC-TST-CAUTH-EDIT-001
DC-TST-CAUTH-DELETE-001
DC-TST-CAUTH-STATUS-001
DC-TST-CAUTH-REV-001
DC-TST-SRC-005
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 55. Comment query

Tests:

```text
DC-TST-CQRY-001
DC-TST-CQRY-002
DC-TST-CQRY-PERF-001
```

Current:

```text
NOT_EVALUATED
```

---

## 56. Retention

Tests:

```text
DC-TST-RET-MATRIX-001
DC-TST-RET-IDENT-001
```

Current:

```text
NOT_EVALUATED
```

---

## 57. Collaboration tenant isolation

Tests:

```text
DC-TST-TEN-COL-001
DC-TST-RLS-COL-001 where applicable
DC-TST-SEC-REV-001
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 58. Collaboration Core milestone

Requires:

```text
Comment lifecycle VERIFIED+
reply/thread VERIFIED+
Anchor release-appropriate
status lifecycle VERIFIED+
target representation STABLE
required target kinds STABLE
authorization STABLE
query VERIFIED+
retention STABLE
tenant isolation STABLE
```

plus migration/security/exact-SHA CI.

Current:

```text
NOT_EVALUATED
```

---

# Collaboration Full Release

## 59. Mention

Tests:

```text
DC-TST-MEN-DOM-001
DC-TST-MEN-APP-001
DC-TST-MEN-APP-002
DC-TST-MEN-EDIT-001
DC-TST-MEN-IDEM-001
DC-TST-MEN-PRIV-001
```

Current:

```text
NOT_EVALUATED
```

---

## 60. Reaction

Tests:

```text
DC-TST-REACT-DOM-001
DC-TST-REACT-DOM-002
DC-TST-REACT-INF-001
DC-TST-REACT-QRY-001
DC-TST-REACT-AUTHZ-001
```

Current:

```text
NOT_EVALUATED
```

---

## 61. Attachment

Tests:

```text
DC-TST-ATT-DOM-001
DC-TST-ATT-SCOPE-001
DC-TST-ATT-APP-001
DC-TST-ATT-SEC-001
DC-TST-ATT-LIFE-001
```

Current:

```text
NOT_EVALUATED
```

---

## 62. Presence

Tests:

```text
DC-TST-PRES-DOM-001
DC-TST-PRES-AUTHZ-001
DC-TST-PRES-RT-001
DC-TST-PRES-SEC-001
```

Current:

```text
NOT_EVALUATED
```

---

## 63. ReadState

Tests:

```text
DC-TST-READ-DOM-001
DC-TST-READ-CONC-001
DC-TST-READ-QRY-001
DC-TST-READ-AUTHZ-001
```

Current:

```text
NOT_EVALUATED
```

---

## 64. Watcher

Tests:

```text
DC-TST-WATCH-DOM-001
DC-TST-WATCH-SCOPE-001
DC-TST-WATCH-IDEM-001
DC-TST-WATCH-RULE-001 where applicable
DC-TST-WATCH-AUTHZ-001
```

Current:

```text
NOT_EVALUATED
```

---

## 65. Notification ownership/runtime classification

Tests:

```text
DC-TST-NOTIF-ARCH-001
DC-TST-NOTIF-FACT-001
DC-TST-NOTIF-IDEM-001
```

The audited source already has durable:

```text
NotificationItemRecord
NotificationRecipientRecord
NotificationPreferenceRecord
NotificationCounterRecord
```

plus `MentionCreatedNotificationConsumer`.

Therefore certification must reconcile the existing durable store/runtime with Collaboration-owned product semantics.

`NOT_APPLICABLE` is valid only for a specific unreleased Application/API sub-capability, not as a way to pretend durable Notification state does not exist.

Current:

```text
NOT_EVALUATED
```

---

## 66. Durable Notification semantics

If released:

```text
DC-TST-NOTIF-RECIP-001
DC-TST-NOTIF-FACT-001
DC-TST-NOTIF-DEL-001
DC-TST-NOTIF-PRIV-001
DC-TST-NOTIF-IDEM-001
DC-TST-NOTIF-CHANNEL-001 where channel state is released
DC-TST-NOTIF-PREF-001 where preferences are released
DC-TST-NOTIF-ATTN-001 where attention operations are released
```

Current:

```text
NOT_EVALUATED
```

---

## 67. Activity

Tests:

```text
DC-TST-ACT-ARCH-001
DC-TST-ACT-PROJ-001
DC-TST-ACT-API-001
DC-TST-ACT-PRIV-001
DC-TST-SRC-004
```

Current source already contains Activity projection consumers and `WorkspaceActivityLogRecord`.

The audited defect is the unwired/constant-empty Collaboration resource query.

Certification must prove the resource query reuses/reconciles the existing projection rather than creating a competing second Activity truth.

Current:

```text
NOT_EVALUATED
```

---

## 68. Collaboration Full milestone

Requires Collaboration Core STABLE plus every released full-scope Collaboration capability and events/realtime closure.

Current:

```text
NOT_EVALUATED
```

---

## 68A. Attachment upload lifecycle

Requirements:

```text
DCREQ208
```

Test:

```text
DC-TST-ATT-UPLOAD-001
```

If product-visible, upload/provider state and durable Attachment state must follow the accepted lifecycle.

Current:

```text
NOT_EVALUATED
```

---

## 68B. Cursor/typing ephemeral state

Requirements:

```text
DCREQ209
```

Test:

```text
DC-TST-PRES-EPH-001
```

If released, cursor/typing is ephemeral and must not create durable Activity/Audit/Comment-history/Notification state solely from transport.

Current:

```text
NOT_EVALUATED
```

---

## 68C. Notification delivery-channel state

Requirements:

```text
DCREQ210
```

Test:

```text
DC-TST-NOTIF-CHANNEL-001
```

Provider/channel delivery state must remain distinct from durable Notification truth and recipient attention state.

Current:

```text
NOT_EVALUATED
```

---

## 68D. Notification preferences

Requirements:

```text
DCREQ211
```

Test:

```text
DC-TST-NOTIF-PREF-001
```

Current source already contains `NotificationPreferenceRecord`.

If preference operations are release-scoped, certification must cover:

```text
type
channel
enablement
delivery mode
digest
quiet hours/timezone
scope
```

Current:

```text
NOT_EVALUATED
```

---

## 68E. Notification recipient attention lifecycle

Requirements:

```text
DCREQ212
```

Test:

```text
DC-TST-NOTIF-ATTN-001
```

Current source already persists recipient attention fields/states.

If released, seen/read/archive/dismiss transitions must be verified independently from source-resource mutation and provider delivery.

Current:

```text
NOT_EVALUATED
```

---

## 68F. Workspace deletion Collaboration workflow

Requirements:

```text
DCREQ213
```

Test:

```text
DC-TST-RET-WS-001
```

The release must explicitly define retain/hide/export/anonymize/purge behavior for each released Collaboration state family.

Current:

```text
NOT_EVALUATED
```

---

## 68G. Thread deletion semantics

Requirements:

```text
DCREQ214
```

Test:

```text
DC-TST-COM-THREAD-DEL-001
```

Parent Comment deletion/tombstone must preserve coherent reply/thread/target semantics.

Current:

```text
NOT_EVALUATED
```

---

## 68H. Comment edit history

Requirements:

```text
DCREQ215
```

Test:

```text
DC-TST-COM-HIST-001
```

If supported, edit history is Collaboration-owned and distinct from Activity, Notification, Documents history and Governance Audit.

Current:

```text
NOT_EVALUATED
```

---

## 68I. Abuse controls

Requirements:

```text
DCREQ216
```

Test:

```text
DC-TST-SEC-ABUSE-001
```

Public/guest/high-abuse release surfaces require applicable abuse/rate/content controls.

If no such surface exists:

```text
NOT_APPLICABLE
```

with scope evidence.

Current:

```text
NOT_EVALUATED
```

---

## 68J. Collaboration search

Requirements:

```text
DCREQ217
```

Test:

```text
DC-TST-COL-SEARCH-001
```

If released, Comment/Activity search is derived and filtered by current target authorization/lifecycle.

Current:

```text
NOT_EVALUATED
```

---

## 68K. Collaboration provider integration

Requirements:

```text
DCREQ218
```

Test:

```text
DC-TST-X-COL-INT-001
```

Released provider sync must enter through approved Collaboration operations.

Current:

```text
NOT_EVALUATED
```

---

## 68L. Optimistic Collaboration reconciliation

Requirements:

```text
DCREQ219
```

Test:

```text
DC-TST-COL-OPT-001
```

If optimistic Comment/Reaction/ReadState/Attachment/Notification-attention behavior is released, temporary state must converge to authoritative server identity/state after success, reject, conflict, duplicate and reconnect.

Current:

```text
NOT_EVALUATED
```

---

# Events / cross-context

## 69. Documents producer events

Tests:

```text
DC-TST-EVT-DOC-001
DC-TST-EVT-PRIV-001
DC-TST-EVT-VER-001
DC-TST-EVT-STUB-001
```

Current:

```text
NOT_EVALUATED
```

---

## 70. Collaboration producer events

Tests:

```text
DC-TST-EVT-COM-001
DC-TST-EVT-MEN-001
DC-TST-EVT-PRIV-001
DC-TST-EVT-VER-001
```

Current:

```text
NOT_EVALUATED
```

---

## 71. WorkManagement Collaboration summary boundary

Test:

```text
DC-TST-X-WM-READ-001
```

Current:

```text
NOT_EVALUATED
```

---

## 72. Foreign target remains unchanged

Test:

```text
DC-TST-X-TGT-UNCHANGED-001
```

Current:

```text
NOT_EVALUATED
```

---

## 73. Automation handoff

Test:

```text
DC-TST-X-AUT-001
```

Current:

```text
NOT_EVALUATED
```

---

## 74. Analytics handoff

Test:

```text
DC-TST-X-ANA-001
```

Current:

```text
NOT_EVALUATED
```

---

## 75. Activity/notification failure isolation

Test:

```text
DC-TST-X-ACT-001
```

Current:

```text
NOT_EVALUATED
```

---

# Realtime

## 76. Documents realtime

Tests:

```text
DC-TST-RT-DOC-001
DC-TST-RT-DOC-002
DC-TST-RT-REV-001
DC-TST-RT-CONV-001
DC-TST-OBS-RT-001
```

Current:

```text
NOT_EVALUATED
```

---

## 77. Comment realtime

Tests:

```text
DC-TST-RT-COM-001
DC-TST-RT-REV-001
DC-TST-RT-CONV-001
```

Current:

```text
NOT_EVALUATED
```

---

## 78. Reaction realtime

If released:

```text
DC-TST-RT-REACT-001
DC-TST-RT-CONV-001
```

Current:

```text
NOT_EVALUATED
```

---

## 79. ReadState realtime

If released:

```text
DC-TST-RT-READ-001
DC-TST-RT-REV-001
```

Current:

```text
NOT_EVALUATED
```

---

## 80. Notification realtime

If released:

```text
DC-TST-RT-NOTIF-001
DC-TST-RT-REV-001
```

Current:

```text
NOT_EVALUATED
```

---

## 81. Presence realtime

Tests:

```text
DC-TST-PRES-RT-001
DC-TST-PRES-AUTHZ-001
```

Presence uses expiry/rebuild, not durable replay.

Current:

```text
NOT_EVALUATED
```

---

## 82. Realtime blocker rule

Durable realtime without Platform gap/recovery evidence:

```text
BLOCKED-UPSTREAM
```

Non-realtime core may still certify.

---

# Architecture / reliability / migration / security

## 83. Bounded-context ownership

Tests:

```text
DC-TST-OWN-ARCH-001
DC-TST-OWN-ARCH-002
DC-TST-ARCH-BOUND-001
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 84. No foreign private persistence

Tests:

```text
DC-TST-TGT-ARCH-001
DC-TST-X-WM-READ-001
DC-TST-ARCH-REF-001
```

Required:

```text
STABLE
```

Current:

```text
NOT_EVALUATED
```

---

## 85. No architecture creep

Tests:

```text
DC-TST-ARCH-CRDT-001
DC-TST-ARCH-CMS-001
DC-TST-ARCH-REF-001
DC-TST-ARCH-BOUND-001
```

Current:

```text
NOT_EVALUATED
```

---

## 86. Transaction atomicity

Tests:

```text
DC-TST-TX-PAGE-001
DC-TST-TX-BLOCK-001
DC-TST-TX-COMMENT-001
DC-TST-XTX-ARCH-001
```

Current:

```text
NOT_EVALUATED
```

---

## 87. Reliability

Tests:

```text
DC-TST-FAIL-DB-001
DC-TST-FAIL-DEP-001
DC-TST-FAIL-MSG-001
DC-TST-FAIL-RT-001
DC-TST-IDEM-001
DC-TST-DEDUP-001
DC-TST-POISON-001
```

Current:

```text
NOT_EVALUATED
```

---

## 88. Migration/compatibility

Tests:

```text
DC-TST-MIG-001
DC-TST-MIG-002
DC-TST-MIG-003
DC-TST-MIG-HIER-001
DC-TST-MIG-ORDER-001
DC-TST-MIG-BLOCK-001
DC-TST-MIG-SNAP-001
DC-TST-MIG-TPL-001
DC-TST-MIG-TGT-001
```

Current:

```text
NOT_EVALUATED
```

---

## 89. Security/privacy

Tests:

```text
DC-TST-TEN-DOC-001
DC-TST-TEN-COL-001
DC-TST-RLS-DOC-001 where applicable
DC-TST-RLS-COL-001 where applicable
DC-TST-SEC-SYS-001
DC-TST-SEC-REV-001
DC-TST-SEC-LOG-001
DC-TST-SEC-RICH-001 where applicable
DC-TST-ATT-SEC-001 where applicable
DC-TST-FILE-SEC-002 where applicable
```

Any critical failure blocks D5.

Current:

```text
NOT_EVALUATED
```

---

## 90. Performance

Tests:

```text
DC-TST-PERF-PAGE-001
DC-TST-PERF-BLOCK-001
DC-TST-PERF-COL-001
DC-TST-PERF-ACT-001 where applicable
DC-TST-PERF-ATTN-001 where applicable
```

Current:

```text
NOT_EVALUATED
```

---

## 91. Observability

Tests:

```text
DC-TST-OBS-001
DC-TST-OBS-002
DC-TST-OBS-RT-001 where applicable
```

Current:

```text
NOT_EVALUATED
```

---

# Matrices

## 92. Documents Core matrix

| Capability | Required | Actual |
|---|---:|---|
| Page identity/scope | D5 | NOT_EVALUATED |
| Page lifecycle | D5 | NOT_EVALUATED |
| Page metadata | D4+ | NOT_EVALUATED |
| Page visibility if exposed | D4+/D5 auth | NOT_EVALUATED |
| Page hierarchy | D4+ | NOT_EVALUATED |
| Block identity/content | D4+ | NOT_EVALUATED |
| Block hierarchy | D4+ | NOT_EVALUATED |
| ordering | D4+ | NOT_EVALUATED |
| concurrency/no-op | D4+ | NOT_EVALUATED |
| query/search | D4+ | NOT_EVALUATED |
| authorization | D5 | NOT_EVALUATED |
| tenant isolation | D5 | NOT_EVALUATED |

---

## 93. Documents Full matrix

| Capability | Required | Actual |
|---|---:|---|
| ResourceLinks | D4+/D5 if consumed | NOT_EVALUATED |
| Version/Snapshot | D4+ | NOT_EVALUATED |
| Page history | D4+ | NOT_EVALUATED |
| restore | D4+ | NOT_EVALUATED |
| Templates | D4+ if released | NOT_EVALUATED |
| search/index | D4+ | NOT_EVALUATED |
| import/export | D4+ if released | NOT_EVALUATED |
| file/media refs | D4+ if released | NOT_EVALUATED |
| Documents events | D4+/D5 if consumed | NOT_EVALUATED |
| Documents realtime | D4+ if released | NOT_EVALUATED |
| Page path | D4+ if exposed | NOT_EVALUATED |
| Duplicate Page | D4+ if released | NOT_EVALUATED |
| provider integration boundary | D4+ if released | NOT_EVALUATED |
| retention across derived/historical copies | D4+/D5 policy | NOT_EVALUATED |
| reduced/mobile compatibility | D4+ if claimed | NOT_EVALUATED |
| offline editing | D4+ if released | NOT_EVALUATED |
| accessibility-preserving content contract | D4+ | NOT_EVALUATED |

---

## 94. Collaboration Core matrix

| Capability | Required | Actual |
|---|---:|---|
| Comment lifecycle | D5 | NOT_EVALUATED |
| reply/thread | D4+ | NOT_EVALUATED |
| CommentAnchor | D4+ if released | NOT_EVALUATED |
| status lifecycle | D4+ if released | NOT_EVALUATED |
| target representation | D5 | NOT_EVALUATED |
| Page target | D5 if released | NOT_EVALUATED |
| BoardItem target | D5 if released | NOT_EVALUATED |
| Comment authorization | D5 | NOT_EVALUATED |
| query | D4+ | NOT_EVALUATED |
| retention | D5 | NOT_EVALUATED |
| tenant isolation | D5 | NOT_EVALUATED |

---

## 95. Collaboration Full matrix

| Capability | Required | Actual |
|---|---:|---|
| Mention | D4+/D5 if consumed | NOT_EVALUATED |
| Reaction | D4+ | NOT_EVALUATED |
| Attachment | D4+ | NOT_EVALUATED |
| Presence | D4+ if released | NOT_EVALUATED |
| ReadState | D4+ | NOT_EVALUATED |
| Watcher | D4+ | NOT_EVALUATED |
| Notification | D4+/D5 if durable product | NOT_EVALUATED |
| Activity | D4+ | NOT_EVALUATED |
| Collaboration events | D4+/D5 if consumed | NOT_EVALUATED |
| Collaboration realtime | D4+ if released | NOT_EVALUATED |
| Attachment upload lifecycle | D4+ if product-visible | NOT_EVALUATED |
| cursor/typing ephemeral state | D4+ if released | NOT_EVALUATED |
| Notification channel state | D4+ if released | NOT_EVALUATED |
| Notification preferences | D4+ if released | NOT_EVALUATED |
| Notification recipient attention state | D4+ if released | NOT_EVALUATED |
| Workspace deletion retention | D5 policy | NOT_EVALUATED |
| thread deletion semantics | D4+ | NOT_EVALUATED |
| Comment edit history | D4+ if released | NOT_EVALUATED |
| abuse controls | D4+ where applicable | NOT_EVALUATED |
| Collaboration search | D4+ if released | NOT_EVALUATED |
| provider integration boundary | D4+ if released | NOT_EVALUATED |
| optimistic reconciliation | D4+ if released | NOT_EVALUATED |

---

## 96. Cross-cutting matrix

| Capability | Required | Actual |
|---|---:|---|
| ownership | D5 | NOT_EVALUATED |
| private persistence isolation | D5 | NOT_EVALUATED |
| transactions | D4+ | NOT_EVALUATED |
| migration | D5 | NOT_EVALUATED |
| security/privacy | D5 | NOT_EVALUATED |
| messaging | D4+/D5 | NOT_EVALUATED |
| realtime recovery | D4+ if released | NOT_EVALUATED |
| performance | accepted threshold | NOT_EVALUATED |
| observability | D4+ | NOT_EVALUATED |
| OpenAPI | clean | NOT_EVALUATED |
| event manifest | clean | NOT_EVALUATED |
| exact-SHA CI | required | NOT_EVALUATED |

---

# Existing evidence reuse

## 97. Reusable Domain evidence

Candidate reusable suites include:

```text
ResourceLinkTests
ResourceLinkTenantTests
DocumentVersionTests
DocumentVersionImmutabilityTests
DocumentSnapshotTests
DocumentTemplateDefinitionTests
CommentReplyInvariantTests
CommentReplyScopeTests
CommentAnchorTests
MentionTests
ReactionTests
ReactionDuplicateTests
AttachmentTests
PresenceExperimentalTests
PresenceIsolationTests
ReadStateMonotonicityTests
ResourceWatcherTests
```

Re-run on candidate SHA.

---

## 98. Reusable integration/architecture evidence

Candidate:

```text
PageCreatedOutboxEvidenceTests
PageArchivedOutboxEvidenceTests
CommentCreatedOutboxEvidenceTests
WorkManagementCollaborationReadAdapterTests
MentionCreatedNotificationRuntimeChainIntegrationTests
CollaborationReadBoundaryArchitectureTests
DocumentsCollaborationEventPinningArchitectureTests
NotificationsClassificationTests
LegacyCollabNotificationWriteBanTests
```

Reuse only for the exact tested invariant.

`LegacyCollabNotificationWriteBanTests` must be semantically reviewed; stale legacy wording is not current Product authority.

---

# Generated artifacts and CI

## 99. OpenAPI record

```text
Candidate SHA:
Generator:
Artifact:
Checksum:
Drift:
Intentional changes:
Client regeneration:
```

---

## 100. Event manifest record

```text
Candidate SHA:
Generator:
Artifact:
Checksum:
Drift:
Documents changes:
Collaboration changes:
Unrelated drift:
```

---

## 101. CI table

| Suite | Selected | Passed | Failed | Skipped | SHA | Result |
|---|---:|---:|---:|---:|---|---|
| Architecture | | | | | | NOT_EVALUATED |
| Domain | | | | | | NOT_EVALUATED |
| Application | | | | | | NOT_EVALUATED |
| Infrastructure | | | | | | NOT_EVALUATED |
| Platform | | | | | | NOT_EVALUATED |
| API | | | | | | NOT_EVALUATED |
| Integration | | | | | | NOT_EVALUATED |
| Migration | | | | | | NOT_EVALUATED |
| OpenAPI | | | | | | NOT_EVALUATED |
| Event manifest | | | | | | NOT_EVALUATED |
| Startup/Docker | | | | | | NOT_EVALUATED |

---

# Blockers/debt

## 102. Blocker classes

```text
BLOCKED-DECISION
BLOCKED-UPSTREAM
BLOCKED-IMPLEMENTATION
BLOCKED-VERIFICATION
BLOCKED-MIGRATION
BLOCKED-SECURITY
BLOCKED-CI
BLOCKED-OPERATIONS
```

---

## 103. Critical debt examples

```text
NotImplemented handler on certified route
constant fake history/activity
silently ignored API field
unclassified permission action
cross-tenant leak
foreign private persistence
unsafe hierarchy/order concurrency
unreadable history/template content
undefined required retention
released durable realtime without recovery
```

---

## 104. Non-blocking debt rule

Only outside release scope, with explicit owner and rationale.

---

# P4B and full-scope decisions

## 105. P4B Core gate

Requires:

```text
Documents Core STABLE
Collaboration Core STABLE
required target kinds STABLE
ownership/private-persistence gates STABLE
migration/security green
required source debt closed
exact-SHA CI green
```

Current:

```text
NOT_EVALUATED
```

---

## 106. Full Workstream gate

Requires:

```text
P4B Core CERTIFIED
Documents Full release-scoped capabilities closed
Collaboration Full release-scoped capabilities closed
events/handoffs stable
released realtime VERIFIED
migration/reliability/security/performance/observability closed
OpenAPI/event manifest clean
all required DC-SRC-* closed
exact-SHA CI green
```

Current:

```text
NOT_EVALUATED
```

---

# Evidence bundles

## 107. Bundle matrix

| Bundle | Scope | Status |
|---|---|---|
| DC-EVID-01 | source inventory/disposition | NOT_EVALUATED |
| DC-EVID-02 | Page lifecycle/metadata/visibility | NOT_EVALUATED |
| DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| DC-EVID-06 | ordering/concurrency | NOT_EVALUATED |
| DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| DC-EVID-08 | Versions/Snapshots/history | NOT_EVALUATED |
| DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| DC-EVID-10 | Documents auth/tenant | NOT_EVALUATED |
| DC-EVID-11 | Comment/reply/anchor/status | NOT_EVALUATED |
| DC-EVID-12 | target adapters | NOT_EVALUATED |
| DC-EVID-13 | Mentions | NOT_EVALUATED |
| DC-EVID-14 | Reactions | NOT_EVALUATED |
| DC-EVID-15 | Attachments | NOT_EVALUATED |
| DC-EVID-16 | Presence | NOT_EVALUATED |
| DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| DC-EVID-18 | Notification/Activity | NOT_EVALUATED |
| DC-EVID-19 | Collaboration auth/tenant/retention | NOT_EVALUATED |
| DC-EVID-20 | API/query contracts | NOT_EVALUATED |
| DC-EVID-21 | events/messaging | NOT_EVALUATED |
| DC-EVID-22 | realtime/recovery | NOT_EVALUATED |
| DC-EVID-23 | migration/compatibility | NOT_EVALUATED |
| DC-EVID-24 | reliability | NOT_EVALUATED |
| DC-EVID-25 | security/privacy | NOT_EVALUATED |
| DC-EVID-26 | performance/observability | NOT_EVALUATED |
| DC-EVID-27 | cross-team integration | NOT_EVALUATED |
| DC-EVID-28 | exact-SHA CI | NOT_EVALUATED |

---

## 107A. Evidence bundle consumption rule

`documents-collaboration.tests.md` is the normative TEST → evidence-bundle map.

CERTIFICATION consumes exactly:

```text
DC-EVID-01..DC-EVID-28
```

For each bundle the candidate record must include:

```text
candidate SHA
test/job/source evidence
selected/passed/failed/skipped where executable
release classification
status
blocker/debt
```

A bundle row marked `NOT_EVALUATED` cannot contribute to D4/D5.

---

# Candidate record templates

## 108. Documents record

```text
Documents Core:
  Page:
  Metadata/visibility:
  Hierarchy:
  Block:
  Block hierarchy:
  Ordering:
  Query:
  Authorization:
  Tenant:
  Status:

Documents Full:
  ResourceLinks:
  Versions/Snapshots:
  History/Restore:
  Templates:
  Search:
  Import/Export:
  File refs:
  Page path:
  Duplicate Page:
  Provider integrations:
  Retention across copies:
  Mobile/reduced compatibility:
  Offline editing:
  Accessibility contract:
  Events:
  Realtime:
  Status:
```

---

## 109. Collaboration record

```text
Collaboration Core:
  Comment:
  Reply:
  Anchor:
  Status lifecycle:
  Targets:
  Authorization:
  Query:
  Retention:
  Tenant:
  Status:

Collaboration Full:
  Mention:
  Reaction:
  Attachment:
  Presence:
  ReadState:
  Watcher:
  Notification:
  Notification channel/preferences/attention:
  Activity:
  Attachment upload lifecycle:
  Cursor/typing:
  Workspace deletion:
  Thread deletion:
  Comment edit history:
  Abuse controls:
  Search:
  Provider integrations:
  Optimistic reconciliation:
  Events:
  Realtime:
  Status:
```

---

## 110. Source-debt record

```text
DC-SRC-DOC-001:
DC-SRC-DOC-002:
DC-SRC-DOC-003:
DC-SRC-DOC-004:
DC-SRC-DOC-005:
DC-SRC-DOC-006:
DC-SRC-DOC-007:
DC-SRC-DOC-008:
DC-SRC-DOC-009:
DC-SRC-DOC-010:

DC-SRC-COL-001:
DC-SRC-COL-002:
DC-SRC-COL-003:
DC-SRC-COL-004:
DC-SRC-COL-005:
DC-SRC-COL-006:
DC-SRC-COL-007:
DC-SRC-COL-008:
DC-SRC-COL-009:
DC-SRC-COL-010:
```

---

## 111. Realtime record

```text
Platform recovery:
Documents:
Comments:
Reactions:
ReadState:
Notification:
Presence:
Final realtime decision:
```

---

# Normative TEST → CERTIFICATION consumption matrix

## 111A. Consumption rule

This matrix is normative.

Every `DC-TST-*` ID defined by TESTS must appear exactly once in this matrix or in an explicitly documented helper-only exception.

There are no helper-only exceptions in the current package.

The automated traceability target is:

```text
defined TEST IDs:   215
consumed TEST IDs:  215
undefined TEST refs: 0
```

---

## 111B. Test consumption matrix

| TEST ID | Evidence bundle | Certification gate | Initial status |
|---|---|---|---|
| `DC-TST-ACT-API-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-ACT-ARCH-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-ACT-PRIV-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-ACT-PROJ-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-ARCH-BOUND-001` | DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-ARCH-CMS-001` | DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-ARCH-CRDT-001` | DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-ARCH-REF-001` | DC-EVID-25, DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-ATT-APP-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-ATT-DOM-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-ATT-LIFE-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-ATT-SCOPE-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-ATT-SEC-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-ATT-UPLOAD-001` | DC-EVID-15 | Attachments | NOT_EVALUATED |
| `DC-TST-BLOCK-COMPAT-001` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BLOCK-COMPAT-002` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BLOCK-CONTENT-001` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BLOCK-DOM-001` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BLOCK-DOM-002` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BLOCK-SCOPE-001` | DC-EVID-04 | Block content/properties | NOT_EVALUATED |
| `DC-TST-BTREE-APP-001` | DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| `DC-TST-BTREE-DEL-001` | DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| `DC-TST-BTREE-DOM-001` | DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| `DC-TST-BTREE-DOM-002` | DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| `DC-TST-BTREE-SEC-001` | DC-EVID-05 | Block hierarchy | NOT_EVALUATED |
| `DC-TST-CAUTH-ARCH-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-CREATE-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-DELETE-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-EDIT-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-READ-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-REV-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-CAUTH-STATUS-001` | DC-EVID-19 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-COL-OPT-001` | DC-EVID-11, DC-EVID-22 | Collaboration optimistic reconciliation | NOT_EVALUATED |
| `DC-TST-COL-SEARCH-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-COM-ANCH-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-ANCH-002` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-DOM-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-DTO-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-HIST-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-REPLY-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-REPLY-002` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-STATUS-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COM-THREAD-DEL-001` | DC-EVID-11 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-COMPAT-MOB-001` | DC-EVID-20 | Client/API compatibility | NOT_EVALUATED |
| `DC-TST-CONTENT-A11Y-001` | DC-EVID-04 | Block semantic contract | NOT_EVALUATED |
| `DC-TST-CQRY-001` | DC-EVID-20 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-CQRY-002` | DC-EVID-20 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-CQRY-PERF-001` | DC-EVID-20 | Comment/reply/anchor/status/query | NOT_EVALUATED |
| `DC-TST-DAUTH-ARCH-001` | DC-EVID-10 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-DAUTH-DEST-001` | DC-EVID-10 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-DAUTH-DISC-001` | DC-EVID-10 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-DAUTH-POL-001` | DC-EVID-10 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-DEDUP-001` | DC-EVID-21, DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-DQRY-001` | DC-EVID-20 | Documents query/API | NOT_EVALUATED |
| `DC-TST-DQRY-002` | DC-EVID-20 | Documents query/API | NOT_EVALUATED |
| `DC-TST-DQRY-PERF-001` | DC-EVID-20 | Documents query/API | NOT_EVALUATED |
| `DC-TST-DUP-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-EDIT-ARCH-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-EDIT-CONC-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-EDIT-NOOP-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-EVT-COM-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EVT-DOC-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EVT-MEN-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EVT-PRIV-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EVT-STUB-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EVT-VER-001` | DC-EVID-21 | Events/messaging | NOT_EVALUATED |
| `DC-TST-EXP-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-FAIL-API-001` | DC-EVID-24 | Public API/error semantics | NOT_EVALUATED |
| `DC-TST-FAIL-DB-001` | DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-FAIL-DEP-001` | DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-FAIL-MSG-001` | DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-FAIL-RT-001` | DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-FILE-SEC-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-FILE-SEC-002` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-HIER-APP-001` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIER-AUTHZ-001` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIER-CONC-001` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIER-CONC-002` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIER-DOM-001` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIER-SEC-001` | DC-EVID-03 | Page hierarchy | NOT_EVALUATED |
| `DC-TST-HIST-API-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-HIST-ARCH-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-HIST-POLICY-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-HIST-RESTORE-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-IDEM-001` | DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-IMP-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-INV-001` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-INV-QUAL-001` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-INV-STUB-001` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-LINK-AUTHZ-001` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-LINK-DOM-001` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-LINK-DOM-002` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-LINK-DOM-003` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-LINK-DOM-004` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-LINK-LIFE-001` | DC-EVID-07 | ResourceLinks | NOT_EVALUATED |
| `DC-TST-MEN-APP-001` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MEN-APP-002` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MEN-DOM-001` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MEN-EDIT-001` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MEN-IDEM-001` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MEN-PRIV-001` | DC-EVID-13 | Mentions | NOT_EVALUATED |
| `DC-TST-MIG-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-002` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-003` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-BLOCK-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-HIER-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-ORDER-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-SNAP-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-TGT-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-MIG-TPL-001` | DC-EVID-23 | Migration/compatibility | NOT_EVALUATED |
| `DC-TST-NOTIF-ARCH-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-ATTN-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-CHANNEL-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-DEL-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-FACT-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-IDEM-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-PREF-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-PRIV-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-NOTIF-RECIP-001` | DC-EVID-18 | Notification/Activity/search | NOT_EVALUATED |
| `DC-TST-OBS-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-OBS-002` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-OBS-RT-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-OFF-001` | DC-EVID-22 | Offline protocol classification | NOT_EVALUATED |
| `DC-TST-ORDER-APP-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-ORDER-CONC-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-ORDER-DENSE-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-ORDER-DOM-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-ORDER-INF-001` | DC-EVID-06 | Ordering/concurrency | NOT_EVALUATED |
| `DC-TST-OWN-ARCH-001` | DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-OWN-ARCH-002` | DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-PAGE-DEL-ORCH-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PAGE-DOM-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PAGE-DOM-002` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PAGE-DOM-003` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PAGE-RET-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PAGE-SCOPE-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PATH-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PERF-ACT-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-PERF-ATTN-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-PERF-BLOCK-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-PERF-COL-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-PERF-PAGE-001` | DC-EVID-26 | Performance/observability | NOT_EVALUATED |
| `DC-TST-PMETA-API-001` | DC-EVID-02, DC-EVID-20 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-POISON-001` | DC-EVID-21, DC-EVID-24 | Reliability/failure | NOT_EVALUATED |
| `DC-TST-PRES-AUTHZ-001` | DC-EVID-16 | Presence/ephemeral awareness | NOT_EVALUATED |
| `DC-TST-PRES-DOM-001` | DC-EVID-16 | Presence/ephemeral awareness | NOT_EVALUATED |
| `DC-TST-PRES-EPH-001` | DC-EVID-16 | Presence/ephemeral awareness | NOT_EVALUATED |
| `DC-TST-PRES-RT-001` | DC-EVID-16, DC-EVID-22 | Presence/realtime | NOT_EVALUATED |
| `DC-TST-PRES-SEC-001` | DC-EVID-16 | Presence/ephemeral awareness | NOT_EVALUATED |
| `DC-TST-PVIS-AUTHZ-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PVIS-DOM-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-PVIS-EVT-001` | DC-EVID-02 | Documents Page contract | NOT_EVALUATED |
| `DC-TST-REACT-AUTHZ-001` | DC-EVID-14 | Reactions | NOT_EVALUATED |
| `DC-TST-REACT-DOM-001` | DC-EVID-14 | Reactions | NOT_EVALUATED |
| `DC-TST-REACT-DOM-002` | DC-EVID-14 | Reactions | NOT_EVALUATED |
| `DC-TST-REACT-INF-001` | DC-EVID-14 | Reactions | NOT_EVALUATED |
| `DC-TST-REACT-QRY-001` | DC-EVID-14 | Reactions | NOT_EVALUATED |
| `DC-TST-READ-AUTHZ-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-READ-CONC-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-READ-DOM-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-READ-QRY-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-RET-DOC-001` | DC-EVID-19, DC-EVID-23, DC-EVID-25 | Documents retention | NOT_EVALUATED |
| `DC-TST-RET-IDENT-001` | DC-EVID-19, DC-EVID-25 | Retention/privacy | NOT_EVALUATED |
| `DC-TST-RET-MATRIX-001` | DC-EVID-19, DC-EVID-25 | Retention/privacy | NOT_EVALUATED |
| `DC-TST-RET-WS-001` | DC-EVID-19, DC-EVID-25 | Collaboration Workspace retention | NOT_EVALUATED |
| `DC-TST-RLS-COL-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-RLS-DOC-001` | DC-EVID-10, DC-EVID-25 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-RT-COM-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-CONV-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-DOC-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-DOC-002` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-NOTIF-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-REACT-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-READ-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-RT-REV-001` | DC-EVID-22 | Realtime/recovery | NOT_EVALUATED |
| `DC-TST-SEARCH-SEC-001` | DC-EVID-20, DC-EVID-25 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-SEC-ABUSE-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-SEC-LOG-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-SEC-REV-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-SEC-RICH-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-SEC-SYS-001` | DC-EVID-25 | Security/privacy | NOT_EVALUATED |
| `DC-TST-SNAP-DOM-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-SRC-001` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-SRC-002` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-SRC-003` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-SRC-004` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-SRC-005` | DC-EVID-01 | Source inventory/debt closure | NOT_EVALUATED |
| `DC-TST-TEN-COL-001` | DC-EVID-19, DC-EVID-25 | Collaboration authorization/tenant | NOT_EVALUATED |
| `DC-TST-TEN-DOC-001` | DC-EVID-10, DC-EVID-25 | Documents authorization/tenant | NOT_EVALUATED |
| `DC-TST-TGT-ARCH-001` | DC-EVID-12 | Target adapters | NOT_EVALUATED |
| `DC-TST-TGT-ITEM-001` | DC-EVID-12 | Target adapters | NOT_EVALUATED |
| `DC-TST-TGT-PAGE-001` | DC-EVID-12 | Target adapters | NOT_EVALUATED |
| `DC-TST-TGT-UNKNOWN-001` | DC-EVID-12 | Target adapters | NOT_EVALUATED |
| `DC-TST-TPL-CONTENT-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-TPL-DOM-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-TPL-IDEMP-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-TPL-INST-001` | DC-EVID-09 | Templates/import-export/files | NOT_EVALUATED |
| `DC-TST-TRACE-001` | DC-EVID-01, DC-EVID-28 | Execution traceability | NOT_EVALUATED |
| `DC-TST-TX-BLOCK-001` | DC-EVID-24 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-TX-COMMENT-001` | DC-EVID-24 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-TX-PAGE-001` | DC-EVID-24 | Traceability/manual classification | NOT_EVALUATED |
| `DC-TST-VER-DOM-001` | DC-EVID-08 | Version/Snapshot/history | NOT_EVALUATED |
| `DC-TST-WATCH-AUTHZ-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-WATCH-DOM-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-WATCH-IDEM-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-WATCH-RULE-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-WATCH-SCOPE-001` | DC-EVID-17 | ReadState/Watchers | NOT_EVALUATED |
| `DC-TST-X-ACT-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-ANA-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-AUT-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-COL-INT-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-DOC-INT-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-TGT-UNCHANGED-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-X-WM-READ-001` | DC-EVID-27 | Cross-team/provider integration | NOT_EVALUATED |
| `DC-TST-XTX-ARCH-001` | DC-EVID-24, DC-EVID-27 | Traceability/manual classification | NOT_EVALUATED |

---
## 111C. Consumption integrity

`DC-TST-TRACE-001` must compare the actual TESTS definitions against this matrix.

Any of the following blocks certification:

```text
defined TEST ID missing here
undefined TEST ID referenced here
duplicate TEST ID row
TEST mapped to no evidence bundle
TEST evidence-bundle set differs from TESTS normative direct index
release-required TEST left without a certification gate
```

---

## 111D. Explicit SPEC requirement coverage index

CERTIFICATION consumes the complete requirement space:

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

The semantic certification gate remains the capability/test/evidence mapping above; this index exists so automated traceability verification does not depend on range expansion or prose inference.

---


## 111E. Canonical stop-condition consumption rule

SPEC defines:

```text
DCSTOP001..DCSTOP026
```

PLAN maps these stop conditions to execution work units and mandatory verification.

CERTIFICATION does not redefine their semantics.

If a canonical `DCSTOP` is triggered for a release-required capability:

```text
affected capability status = BLOCKED
```

until the stop condition is resolved by Product/SPEC authority and executable evidence.

---

# Final decision

## 112. Allowed final decisions

Choose the narrowest valid result:

```text
NOT_EVALUATED
BLOCKED
DOCUMENTS CORE STABLE
DOCUMENTS FULL RELEASE STABLE
COLLABORATION CORE STABLE
COLLABORATION FULL RELEASE STABLE
P4B CORE CERTIFIED
DOCUMENTS & COLLABORATION FULL RELEASE CERTIFIED
```

---

## 113. Final decision record

```text
Candidate SHA:
Release scope:

Documents Core:
Documents Full:

Collaboration Core:
Collaboration Full:

Targets:
  Page:
  BoardItem:
  other released:

Events:
Cross-context:
Realtime:
Architecture:
Migration:
Security:
Reliability:
Performance:
Observability:
OpenAPI:
Event manifest:
Exact-SHA CI:

Unresolved source debt:
Blocking debt:
Non-blocking debt:

Final decision:

Reviewer(s):
Decision date:
```

---

# Re-certification triggers

## 114. Documents triggers

Re-certify affected scope after changes to:

```text
Page identity/lifecycle/visibility
Page hierarchy
Block content/hierarchy/order
ResourceLink
Version/Snapshot/history
Template
authorization
tenant/RLS
```

---

## 115. Collaboration triggers

Re-certify after changes to:

```text
Comment/reply/anchor/status
target contract
Mention
Reaction
Attachment
Presence
ReadState
Watcher
Notification
Activity
authorization
retention
tenant/RLS
```

---

## 116. Realtime triggers

Re-run after:

```text
event identity/version
subscription scope
gap/recovery
projection reconciliation
permission revocation handling
Presence expiry/reconnect
```

---

## 117. Migration triggers

Any persisted representation change affecting certified state requires compatibility re-evaluation.

---

# Definition of Done

## 118. Documents Core Done

Only when:

```text
core capability gates are satisfied
source debt is closed
authorization/tenant are stable
migration/security are green
exact-SHA CI is green
```

---

## 119. Documents Full Done

Documents Core plus every release-scoped Documents knowledge capability and every release-scoped canonical semantic in `DCREQ201..207`.

---

## 120. Collaboration Core Done

Only when:

```text
Comment/reply/target semantics are verified
authorization and retention are stable
tenant isolation is stable
required target contracts are stable
exact-SHA CI is green
```

---

## 121. Collaboration Full Done

Collaboration Core plus every release-scoped:

```text
Mention
Reaction
Attachment
Presence
ReadState
Watcher
Notification
Activity
Attachment upload lifecycle
cursor/typing if released
Notification channel/preferences/attention if released
Workspace deletion retention
thread deletion semantics
Comment edit history if released
abuse controls where applicable
Collaboration search if released
provider integrations if released
optimistic reconciliation if released
realtime
```

---

## 122. Realtime Done

Durable state requires:

```text
duplicate
reorder
gap
reconnect
permission revoke
authoritative convergence
```

Presence requires:

```text
scope
authorization
heartbeat/expiry
disconnect/reconnect
```

---

## 123. Full workstream Done

Only when:

```text
every release-scoped canonical capability is dispositioned
DCREQ001..220 traceability is closed
all required milestones are satisfied
all required DC-SRC-* debt is closed
no critical debt remains
generated contracts are clean
exact candidate SHA CI is green
```

---

# Final safety questions

## 124. Documents safety question

Can downstream consumers depend on Documents without:

```text
reading private persistence
guessing Page visibility
guessing Page/Block hierarchy
guessing Block content compatibility
guessing history/version meaning
```

If no:

```text
Documents is not STABLE
```

---

## 125. Collaboration safety question

Can Collaboration operate through public target facts without:

```text
owning target state
mutating foreign aggregates
granting access through Mention/Watcher/Presence/Notification
losing retention/privacy semantics
```

If no:

```text
Collaboration is not STABLE
```

---

## 126. Source-debt safety question

Does any certified path still contain:

```text
NotImplementedException
constant fake empty/success response
silently ignored public field
unclassified permission action
unclassified Domain-rich/Application-thin capability
unconsumed DC-TST ID
unmapped DC-EVID bundle
```

If yes:

```text
affected certification is BLOCKED
```

---

## 127. Realtime safety question

Can every released durable realtime state recover from missed/duplicate/reordered delivery and converge while respecting current permission?

If no:

```text
that realtime surface is not VERIFIED
```

---

## 128. Full-scope safety question

Has every release-scoped canonical capability, including the unnumbered Product semantics captured by `DCREQ201..220`, been:

```text
implemented + verified
or
explicitly NOT_APPLICABLE / OUT_OF_RELEASE_SCOPE
with authority-backed evidence?
```

Has every `DC-SRC-*` row been closed or declared blocking?

If no:

```text
DOCUMENTS & COLLABORATION FULL RELEASE CERTIFIED
```

is not a valid decision.

---

## 129. Final certification rule

Never compress mixed readiness into generic:

```text
DONE
```

Valid granular examples:

```text
Documents Core STABLE
Collaboration Core BLOCKED

P4B CORE CERTIFIED
Presence NOT_APPLICABLE
Notification NOT_APPLICABLE
Activity BLOCKED

Documents Full Release STABLE
Collaboration Full Release PARTIALLY_VERIFIED

DOCUMENTS & COLLABORATION FULL RELEASE CERTIFIED
```

Only the last state represents full release certification.
