---
document_id: WRK-PLAN-WORKSPACE-GOVERNANCE
document_type: workstream-plan
status: active
owner: workspace-governance-team
applies_to:
  - backend
  - workspaces
  - governance
  - workspace
  - workspace-membership
  - invitations
  - teams
  - spaces
  - workspace-rules
  - provisioning
  - resource-kind
  - actions
  - permissions
  - permission-rules
  - roles
  - policies
  - resource-permissions
  - share-links
  - audit
  - security-events
  - authorization
  - workmanagement-handoff
evidence:
  - docs/workstreams/execution/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
review_on:
  - workspace-governance-spec-change
  - p1-contract-change
  - membership-model-change
  - resource-action-model-change
  - permission-model-change
  - authorization-pipeline-change
  - governance-overlap-resolution
  - workmanagement-resource-contract-change
  - migration-change
  - p2-exit-gate-change
---

# PLAN — Workspace & Governance

## 1. Purpose

This PLAN converts `workspace-governance.spec.md` into a deterministic execution sequence for Priority 2.

Its goal is to move the backend from the current Workspaces/Governance source state to a stable P2 producer contract that allows WorkManagement and other downstream product contexts to implement protected business capabilities without inventing local authorization logic.

This revision is candidate-SHA/source-first. Any static tree/model examples in SPEC/PLAN/TESTS are hints,
not authority over source. Existing canonical authorization mechanisms are hardened and extended; they
are not rebuilt for P2 merely because the execution vocabulary is cleaner than current source naming.

This PLAN defines:

```text
what source must be inspected first
which ambiguous models must be classified before coding
what constitutes the P2 critical path
what can run in parallel
what must wait
which migrations are allowed
which cross-context handshakes must be proven
which PR boundaries are preferred
which conditions require STOP
what must be handed to TESTS and CERTIFICATION
```

## 2. Master P2 execution objective

The required P2 critical path is:

```text
Candidate SHA + current authorization baseline
        ↓
P1 Actor + Account contract
        ↓
Workspace identity + Account containment
        ↓
Workspace membership baseline
        ↓
Existing resource category / ResourceId / Action contract
(ResourceType/ResourceKind naming is classified, not preselected)
        ↓
Permission + built-in role semantics
        ↓
Harden existing AuthorizationBehavior / decision-store path
(or candidate-SHA canonical equivalent)
        ↓
Resource-owner authorization facts-provider boundary
        ↓
WorkManagement Board representative handshake
        ↓
P2 PROTECTED-SLICE CERTIFIED
```

P3 is opened in two stages: Domain/Data work may parallelize earlier after the D4 producer contracts are
stable; protected Application/API release waits for the P2 protected-slice gate. Governance therefore
protects product execution without serializing all WorkManagement implementation behind Policy D5.

Secondary Governance/Workspace features continue in parallel:

```text
advanced invitations
Teams
Spaces
custom roles
advanced policies
resource ACL depth
share links
governance templates
advanced audit/security-event surfaces
```

## 3. Critical distinction: P2 core vs complete team scope

P2 protected-slice core MUST be completed before P3 protected Application/API release.

P3 Domain/Data implementation MAY begin earlier once Workspace/containment/resource-action producer
contracts are D4+ and do not depend on unstable Governance internals.

P2 full team scope does NOT have to be 100% complete before P3.

### P2 core

Mandatory:

```text
Workspace
Account containment
WorkspaceMember baseline
resource authorization category + ResourceId (preserve valid source naming)
resource-owned Action vocabulary
Permission
Built-in Role baseline
Effective authorization
Central authorization handoff
WorkManagement resource handshake
```

### P2 secondary

May continue after P3 opens:

```text
advanced Invitation behavior
Teams
Spaces
custom roles
advanced Policies
advanced ResourcePermissions
ShareLinks
Templates
advanced Audit/SecurityEvents
```

## 4. Current source evidence to preserve

The source tree below is preparation-time context only. Phase 0 candidate-SHA discovery is authoritative;
the coding agent MUST NOT create folders/types solely because this document lists them.

Current/known Domain source contains Workspaces areas conceptually:

```text
Workspaces/
├── Invitations
├── Members
├── Rules
├── Spaces
├── Teams
├── Workspaces
└── WorkspaceRuleCodes.cs (or candidate-SHA equivalent)
```

and Governance areas conceptually:

```text
Governance/
├── Permissions
├── Policies
├── Roles
├── ShareLinks
├── Templates
└── GovernanceRuleCodes.cs (or candidate-SHA equivalent)

Audit/SecurityEvent capability surfaces are discovered from actual candidate-SHA source;
do not create Domain folders from documentation scope alone.
```

Current Application source also contains explicit Workspaces and Governance feature surfaces including:

```text
Workspaces:
Abstractions
DTOs
Events
Invitations
Members
Provisioning
Settings
Spaces
Teams
WorkspaceHome
Workspaces

Governance:
AuditLogs
DTOs
PermissionRules
Permissions
Policies
ResourcePermissions
Roles
SecurityEvents
ShareLinks
```

The PLAN MUST use this source reality as evidence.

It MUST NOT reorganize folders simply because the SPEC uses a cleaner conceptual model.

## 5. Source-first rule

For every capability:

```text
inspect current Domain
inspect current Application
inspect Infrastructure mapping
inspect API
inspect tests
inspect downstream consumers
classify existing semantics
then change
```

## 6. Prohibited implementation pattern

Forbidden:

```text
read SPEC
→ create idealized Role/Permission/Policy engine
→ retrofit current code
```

Required:

```text
inventory current model
→ identify overlap/debt
→ preserve valid semantics
→ resolve only required ambiguity
→ implement P2 critical path
```

# Master phases

## 7. Phase map

```text
Phase 0  Baseline + full source + existing authorization inventory
Phase 1  Upstream P1 contract verification
Phase 2  Workspace/Governance/security semantic-overlap classification
Phase 3  Workspace core
Phase 4  Membership core
Phase 5  Invitation baseline (parallel side-stream; not P2 critical-path dependency)
Phase 6  Existing resource category / ResourceId / Action contract
Phase 7  Permission model
Phase 8  Built-in roles
Phase 9  Existing authorization path hardening (no second pipeline)
Phase 10 WorkManagement resource-owner facts-provider handshake
Phase 11 P2 protected-slice verification + staged P3 handoff

Phase 12 Provisioning / Settings / WorkspaceHome
Phase 13 Teams / Spaces / Workspace Rules
Phase 14 Custom roles / Policies / ResourcePermissions
Phase 15 ShareLinks
Phase 16 Audit / SecurityEvents / Templates

Phase 17 API / Events / Authorization harmonization
Phase 18 Persistence / Migration / Compatibility
Phase 19 Security hardening
Phase 20 Concurrency / Reliability
Phase 21 Observability / Performance
Phase 22 Cross-context integration hardening
Phase 23 TESTS handoff
Phase 24 Docs / generated-contract handoff
Phase 25 CERTIFICATION handoff
```

Phase 5 may run after Membership semantics are stable and MUST NOT block Phase 6 unless the selected
representative product slice truly requires invitation acceptance to establish membership.

P3-A Domain/Data work may begin after the Phase 6 D4 producer contract and required Workspace/Membership
preconditions are stable. Phases 12–16 may partially overlap after Phase 11/P3-B as release scope permits.

Phases 17–22 run continuously as needed, but have explicit hardening passes before final certification.

# Phase 0 — Baseline and source inventory

## 8. WG-INV-001 — capture exact baseline

Record:

```text
branch
HEAD SHA
backend solution
migration head
test project inventory
relevant CI jobs
```

Use canonical repository commands.

Minimum:

```bash
git status --short
git branch --show-current
git rev-parse HEAD
```

Stop implementation if unrelated working-tree changes make ownership/evidence ambiguous.

## 9. WG-INV-002 — Domain Workspaces inventory

Run source-equivalent discovery:

```bash
find backend/src/Notrelix.Domain/Workspaces -type f | sort
```

For every type, record:

```text
Type
Namespace
Aggregate/Entity/VO/Enum
Owner
ID type
Parent scope
Lifecycle
Invariants
Events
Cross-context references
Persistence-sensitive assumptions
WGREQ coverage
```

Mandatory areas:

```text
Workspaces
Members
Invitations
Teams
Spaces
Rules
```

## 10. WG-INV-003 — Domain Governance inventory

Run:

```bash
find backend/src/Notrelix.Domain/Governance -type f | sort
```

Inventory:

```text
Permissions
Policies
Roles
Security
ShareLinks
Templates
Audit
```

For each type record:

```text
semantic purpose
scope
subject type
resource type
action relation
tenant/workspace relation
persistence
overlap with other Governance types
```

## 11. WG-INV-004 — Application Workspaces inventory

Run:

```bash
find backend/src/Notrelix.Application/Features/Workspaces -type f | sort
```

Classify:

```text
Abstractions
DTOs
Events
Invitations
Members
Provisioning
Settings
Spaces
Teams
WorkspaceHome
Workspaces
```

For each:

```text
commands
queries
handlers
validators
authorization declarations
idempotency
transactions
external dependencies
tests
```

## 12. WG-INV-005 — Application Governance inventory

Run:

```bash
find backend/src/Notrelix.Application/Features/Governance -type f | sort
```

Classify:

```text
AuditLogs
DTOs
PermissionRules
Permissions
Policies
ResourcePermissions
Roles
SecurityEvents
ShareLinks
```

Special attention:

```text
PermissionRules
Permissions
Policies
ResourcePermissions
```

because these are most likely to overlap semantically.

## 13. WG-INV-006 — Infrastructure inventory

Search actual source:

```bash
rg -n \
  "Workspace|WorkspaceMember|Invitation|Team|Space|Role|Permission|Policy|ResourcePermission|ShareLink|Audit" \
  backend/src/Notrelix.Infrastructure
```

Record:

- EF configurations;
- DbSets;
- repositories/adapters;
- unique indexes;
- membership constraints;
- role/permission tables;
- resource/action persistence;
- policy storage;
- share/invitation secret protection;
- caches;
- RLS/query filters;
- migrations.

## 14. WG-INV-007 — API inventory

Search:

```bash
rg -n \
  "Workspace|Member|Invitation|Team|Space|Role|Permission|Policy|ShareLink|Audit|SecurityEvent" \
  backend/src/Notrelix.API
```

Record each endpoint:

```text
route
method
Application use case
authentication
authorization declaration
request/response
error mapping
OpenAPI
WGREQ coverage
```

## 15. WG-INV-008 — test inventory

Search:

```bash
rg -n \
  "Workspace|Member|Invitation|Team|Space|Role|Permission|Policy|ResourcePermission|ShareLink|Authorization" \
  backend/tests
```

Classify into:

```text
Architecture
Domain
Application
Infrastructure
API
Integration
Platform where pipeline-related
```

## 16A. WG-INV-AUTH-001 — existing Application security authority

Before creating any P2 authorization abstraction, inventory the candidate SHA under the existing security
and pipeline areas, including at minimum current equivalents of:

```text
AuthorizationBehavior
IAuthorizationDecisionStore
IPermissionEvaluator
IPermissionService
IResourceReferenceResolver
IResourceScopeResolver
PermissionContext
PermissionDecision
WorkspacePermissionService
resource-scoped request contracts
```

Record:

```text
canonical interface
current source name (ResourceType/ResourceKind/etc.)
who constructs PermissionContext
who evaluates
who resolves resource scope/reference
production DI owner
pipeline order
cache semantics
failure behavior
```

STOP if a proposed P2 type duplicates an existing canonical responsibility without an explicit replacement
and migration decision.

## 16. WG-INV-009 — downstream authorization consumers

Search for:

```bash
rg -n \
  "ResourceKind|ResourceType|Permission|Authorize|Authorization|Role|IsAdmin|IsOwner" \
  backend/src/Notrelix.Application/Features \
  backend/src/Notrelix.Domain
```

Review especially:

```text
WorkManagement
Documents
Collaboration
Billing
Automation
Integrations
```

Record local role/permission logic that may duplicate Governance.

## 17. WG-INV-010 — resource-kind/action inventory

Search all backend source for existing resource/action identifiers.

Build table:

```text
ResourceKind
Owner context
Resource ID source
Supported actions
Persisted?
Referenced by role/policy?
Referenced by API?
Referenced by tests?
```

## 18. WG-INV-011 — duplicate auth mechanism scan

Find:

- handler role-name checks;
- endpoint-local permission checks;
- policy checks outside canonical mechanism;
- owner checks that may actually be auth policy;
- direct ResourcePermission lookups.

Classify each as:

```text
Domain invariant
canonical Governance
legacy
duplicate
source debt
```

## 19. Phase 0 exit

Must know:

```text
Workspace aggregate/model
Member model
Invitation model
Team/Space meaning
Workspace Rules meaning candidates
Permission model
PermissionRule model
Policy model
ResourcePermission model
Role model
ShareLink model
authorization pipeline integration
downstream role-check debt
```

# Phase 1 — Upstream P1 contract verification

## 20. Purpose

P2 depends on P1.

Before hardening Workspace/Governance, verify the actual available contract.

## 21. WG-P1-001 — Actor contract

Verify:

- trusted Actor abstraction;
- stable User ID;
- production DI;
- no handler payload impersonation.

If P1 is not fully implemented yet, use approved interface contract from P1 execution package without creating local alternatives.

## 22. WG-P1-002 — Account contract

Verify:

```text
stable Account ID
Account/Tenant meaning
current Account resolution
tenant isolation
Account active/inactive semantics
```

## 23. WG-P1-003 — Account→Workspace target relationship

Confirm canonical product/architecture relation.

Do not infer from FK shape.

Expected logical relationship:

```text
Account
→ one or more Workspaces
```

only if canonical authority confirms it.

## 24. WG-P1-004 — Identity lifecycle dependency

Determine expected behavior when:

- User disabled;
- User deleted/tombstoned;
- session invalid;
- Account disabled.

Workspace/Governance must consume upstream state, not duplicate it.

## 25. Phase 1 blocker

If Account/Actor semantics are materially unresolved:

```text
continue inventory/design only
STOP hardening P2 core
```

# Phase 2 — Semantic-overlap classification

## 26. Why this phase exists

Current source contains several concept families that could overlap:

```text
Workspaces/Rules
Governance/PermissionRules
Governance/Policies
Governance/Permissions
Governance/ResourcePermissions
```

P2 cannot be safely expanded until their boundaries are understood.

## 27. WG-SEM-001 — classify Workspaces Rules

For every rule type under Workspaces:

Classify as:

```text
A. Workspace business invariant
B. Workspace configurable setting/rule
C. Governance authorization policy
D. Automation rule
E. validation helper
F. legacy/dead code
```

Target action:

```text
RETAIN
REHOME
RENAME
DEPRECATE
REMOVE
```

No action without source evidence.

## 28. WG-SEM-002 — classify Permission

Determine whether Permission is:

- stable permission catalog entry;
- resource+action tuple;
- named capability;
- role assignment payload;
- another model.

Record:

```text
ID
scope
resource relation
action relation
persistence
consumer
```

## 29. WG-SEM-003 — classify PermissionRule

Determine whether PermissionRule is:

```text
policy condition
stored grant rule
role rule
resource ACL rule
application query abstraction
```

Do not assume "rule" implies a DSL.

## 30. WG-SEM-004 — classify Policy

Determine:

```text
what a Policy evaluates
what subject it applies to
what resource/action it constrains
how it composes
how it persists
```

## 31. WG-SEM-005 — classify ResourcePermission

Determine whether it is:

```text
direct ACL
subject-resource binding
role-resource binding
override
exception
```

## 32. WG-SEM-006 — build authorization semantic hierarchy

Create source-backed model:

```text
Actor/Subject
Membership
Role
Permission
Policy/Rule
ResourcePermission
Resource
Action
Decision
```

For each arrow, define ownership and meaning.

## 33. WG-SEM-007 — identify duplicate authority

If two models produce the same effective permission independently:

```text
STOP expansion
→ classify canonical/legacy
```

## 34. WG-SEM-008 — record semantic-resolution note

Execution evidence must contain:

```text
Workspaces Rules:
Permission:
PermissionRule:
Policy:
ResourcePermission:
Role:
Effective decision source:
Legacy/debt:
Migration impact:
Architecture decision required:
```

## 35. Phase 2 exit

The team must be able to explain one coherent authorization model without relying on folder names.

# Phase 3 — Workspace core

## 36. WG-WSP-001 — validate Workspace aggregate/model

Requirements:

```text
WGREQ001–WGREQ007
```

Inspect current Workspaces aggregate.

Confirm:

- stable ID;
- Account containment;
- lifecycle;
- metadata;
- events;
- soft-delete/archive semantics;
- concurrency/version semantics.

## 37. WG-WSP-002 — Account containment

Implement/verify:

```text
Workspace.AccountId
```

or canonical equivalent.

Required:

- cannot attach to invalid Account;
- tenant scope preserved;
- cross-Account move behavior explicit if supported.

Do not add Workspace reparenting unless product requires it.

## 38. WG-WSP-003 — Workspace lifecycle

Map source transitions.

For each:

```text
create
rename/update
archive/disable
delete
restore if canonical
```

record downstream effects.

## 39. WG-WSP-004 — Workspace lifecycle authorization

Each protected lifecycle operation must declare resource/action authorization.

No role-string checks.

## 40. WG-WSP-005 — Workspace event baseline

Emit/verify only meaningful producer-owned lifecycle facts needed downstream.

Do not emit every property mutation as an integration event automatically.

## 41. WG-WSP-006 — Workspace persistence

Verify:

- required indexes;
- Account FK/scope;
- unique names/slugs only if product-defined;
- concurrency token if used;
- migration.

## 42. WG-WSP-007 — Account disabled interaction — RESOLVED (D3-B)

If Account inactive:

Workspace protected operations must follow canonical failure policy.

Do not mutate Account state from Workspace.

Decision D3-B (recorded in `decisions/PR-WG-01-phase3-workspace-core.md`): central Application access-control enforcement — `AccessFacts.AccountOperational` fact + `AccessPolicyEngine` deny for Account/Workspace/Resource scopes, failed closed before handler effects. Implemented and proven (see PR-WG-01 verdict ledger).

## 43. Phase 3 exit

Workspace identity and Account containment must be D4-ready.

Status:

```text
D3-A decision recorded; archive/delete semantics fixed (effects phased: Membership P4, Invitations P5, Teams/Spaces P13).
D3-B decision recorded + implemented + proven (central account-operational enforcement).
WG-FIND-301..304 dispositioned in PR-WG-01.
Phase 3 CLOSED. Continue to Phase 4.
```

# Phase 4 — Membership core

## 44. WG-MEM-001 — validate WorkspaceMember model

Requirements:

```text
WGREQ012–WGREQ023
```

Confirm:

- User/Actor reference;
- Workspace reference;
- status/state;
- role assignment relation;
- lifecycle;
- uniqueness;
- events.

## 45. WG-MEM-002 — membership uniqueness

Enforce no duplicate active membership according to canonical semantics.

Use DB constraint/transaction safety where required.

## 46. WG-MEM-003 — add member

Implement/verify:

```text
authorization
User identity validation
Workspace scope
duplicate prevention
role bootstrap if applicable
event
```

## 47. WG-MEM-004 — remove member

Define:

- authorization;
- last-admin invariant;
- role/resource grant cleanup;
- historical attribution;
- event.

## 48. WG-MEM-005 — self leave

Only if supported.

Verify last-admin/owner behavior.

## 49. WG-MEM-006 — membership state changes

If suspend/disable exists:

- authorization;
- effective permission impact;
- cache invalidation;
- event.

## 50. WG-MEM-007 — role association

Membership references Governance roles through approved contract.

Do not embed permission list inside Member if Governance owns it.

## 51. WG-MEM-008 — Identity deactivation

Verify disabled User cannot continue authorization because membership cache is stale.

## 52. WG-MEM-009 — membership query contract

Provide approved query/read model for:

- list members;
- get current member;
- membership status;

without exposing Identity private state.

## 53. WG-MEM-010 — concurrency hardening

Test:

```text
concurrent add
concurrent remove
concurrent demote/remove last admin
```

where relevant.

## 54. Phase 4 exit

WorkspaceMember reaches at least D4.

Status:

```text
D4-A decision recorded + implemented + proven (central user-operational enforcement, WG-MEM-008).
D4-B decision recorded + implemented (AddMember target User identity validation, WGREQ016).
D4-C self-leave deferred (no command; owner semantics undefined).
D4-D membership concurrency hardening deferred (WG-MEM-010; WG-TST-MEM-INF-001/CONC-001 carried).
D4-E state-change authorization actions are TRANSITION until Phase 8 roles.
WG-FIND-401 (AddMember literal activeOwnerCount) + WG-FIND-402 (suspended-member pipeline negative) recorded.
Full suites green (Application 573 / Domain 2576 / Architecture 410 / Integration 343 / API 256 / Infrastructure 134).
Phase 4 CLOSED. Continue to Phase 5.
```

# Phase 5 — Invitation baseline

## 55. Purpose

Invitations matter for real Workspace access but advanced invitation features do not need to block P2 core if membership can otherwise be established for tests/initial product.

## 56. WG-INVITE-001 — invitation model

Requirements:

```text
WGREQ024–WGREQ033
```

Verify:

- target;
- Workspace;
- intended role/access;
- expiry;
- status;
- secret/token model;
- events.

## 57. WG-INVITE-002 — create

Protected operation.

Must ensure inviter cannot grant access beyond permitted authority.

## 58. WG-INVITE-003 — accept

Required sequence:

```text
validate invitation
validate target identity
validate expiry/revocation
validate intended access
create/activate membership
mark invitation accepted
```

Transaction boundary must avoid duplicate membership.

## 59. WG-INVITE-004 — revoke

Revoked invitation cannot later create access.

## 60. WG-INVITE-005 — replay/idempotency

Repeated accept must be safe.

## 61. WG-INVITE-006 — race

Accept vs revoke/expiry.

Authoritative final state must not grant revoked access.

## 62. WG-INVITE-007 — secret safety

No raw bearer invitation secret in ordinary logs/list responses.

## 63. Phase 5 exit

Baseline invitation can remain D3/D4 if not required for P3 gate.

Membership itself must remain stable.

Status:

```text
Phase 5 baseline audit outcome in decisions/PR-WG-03-phase5-invitation-baseline.md (ledger).
D5-A shared acceptance service (token + by-id converge on one InvitationAcceptanceService) — implemented + proven.
D5-B active membership accept = idempotent consume (no duplicate member/grant/role) — implemented + proven.
D5-C suspended/removed invitee acceptance = side-effect-free rejection — implemented + proven.
D5-D Token removed from UserPendingInvitationDto; pending-list accept now by invitation id — implemented + proven.
D5-E replay/race matrix + realtime-on-accept deferred (WG-INVITE-005/006; WG-TST-INV-CONC-001 carried).
D5-F RLS risk recorded: AcceptInvitationByIdCommand is IGlobalRequest → DataSessionBehavior skips tenant scope →
    RlsSessionContext.ApplyAsync not invoked; prod as notrelix_app (FORCE RLS) may deny writes. Open follow-up.
D5-G frontend sync to real contract: pending menu accepts by id; deep-link uses POST /invitations/preview;
    workspace-scoped members table wired to LIST + Cancel; create stays stub (no backend create endpoint this phase).
New tests: Application +14 (service + by-id handler suite), API +4 invitation endpoints (260 total), Integration +3 (real Postgres), Architecture request-execution baseline +1 (AcceptInvitationByIdCommand).
Full suites green (Application 587 / Domain 2576 / Architecture 410 / Integration 346 / API 260 / Infrastructure 134).
Frontend gates green (typecheck, lint, format, architecture, test-taxonomy, node 313, web 2, mock-freeze); codegen:check pending commit of generated schema.
Phase 5 CLOSED. Continue to Phase 6.
```

# Phase 6 — Resource authorization category / ResourceId / Action contract

## 64. Purpose

Stabilize the **existing** resource authorization vocabulary and the resource-owner facts boundary that
unlocks WorkManagement and later Documents/Billing/Automation/Integrations. This phase does not authorize
a `ResourceType`→`ResourceKind` rename by documentation preference.

## 65. WG-RES-001 — inventory existing resource abstractions

Search candidate source for:

```text
ResourceKind
ResourceType
ResourceId
ResourceReference
ResourceScope
Subject
Action
AuthorizationRequirement / IRequirePermission
PermissionContext
IResourceReferenceResolver
IResourceScopeResolver
```

Record every variant, persisted consumer and current canonical usage.

Evidence (executed):

```text
ResourceKind            → canonical Domain value object (Domain/SharedKernel/ResourceKind),
                          open string-based "{context}.{resource}" record struct, 16 active kinds:
                          8 work-management.*, 2 documents.*, 2 collaboration.*,
                          2 governance.*, 2 automation.*. No legacy ResourceType found.
ResourceId              → ResourceId is part of ResourceRef (Domain/SharedKernel/ResourceRef).
ResourceScope           → none found as distinct abstraction (scope lives on request markers).
Subject                 → no legacy enum Subject in backend/src or backend/tests.
Action                  → canonical PermissionAction enum
                          (Domain/Governance/Permissions/PermissionAction.cs), 31 members.
AuthorizationRequirement→ IRequirePermission (Application/Common/Requests/Security),
                          exposes PermissionAction Action + ResourceRef? Resource.
PermissionContext       → none found.
IResourceReferenceResolver → none.
IResourceScopeResolver  → none; the scope/resolver role is served by IResourceLocator
                          (Application/Common/Context) + ResourceLocator (Infrastructure/Services).
Persisted consumers of ResourceKind (string):
                          ResourcePermissionGrantedIntegrationEvent / RevokedIntegrationEvent
                          (string ResourceKind field), RealtimeResourceChangedV1 (property),
                          RealtimeTopic (Namespace + ResourceKind + ResourceId).
```

## 66. WG-RES-002 — select canonical resource category without forced rename

Use the existing source model when it already provides:

```text
stable semantic category
resource ID
tenant/workspace scope
transport-neutral authorization request representation
```

If source currently uses `ResourceType` and its semantics are correct, keep it. Rename to `ResourceKind`
only with explicit ADR/source decision plus persisted role/permission/policy/downstream migration proof.

Do NOT create another resource abstraction merely to match SPEC vocabulary.

## 67. WG-RES-003 — resource/action ownership

For every resource/action:

```text
resource owner context
authorization category semantic name
resource-specific Action owner
persisted identity?
permission/role/policy consumers?
```

Resource teams own resource identity, lifecycle and business Action meaning. Governance may own generic
registry/mapping mechanics but cannot invent product actions on behalf of WorkManagement/Documents/etc.

## 68. WG-RES-004 — define authorization FACTS contract

For resources whose authorization depends on facts not already available in request/tenant context, define
a minimal transport-neutral facts projection. Example categories only when source-approved:

```text
WorkspaceId / AccountId
resource lifecycle accessibility
visibility/audience fact
actor↔resource relationship fact
parent scope
```

Do not return full foreign aggregates. Do not return `CanEdit/CanDelete/...` if those are Governance policy
decisions rather than resource-owned facts.

## 68A. WG-RES-005 — provider ownership and adapter placement

Canonical direction:

```text
Application/Common/Security or approved neutral SPI
                  ↑
resource-owner Infrastructure implementation
                  ↓
resource-owner private persistence
```

For Board, any implementation that reads `Boards`, `BoardMembers`, `BoardRole`, `BoardVisibility` or
`IWorkManagementDbContext` belongs to WorkManagement infrastructure/adapter ownership.

Forbidden:

```text
Notrelix.Infrastructure.Governance.*
  → directly queries IWorkManagementDbContext / Boards / BoardMembers
```

If a current class such as `BoardAuthorizationSnapshotResolver` has that dependency while living under
Governance ownership, STOP and rehome/redesign before building further policy on top of it.

## 68B. WG-RES-006 — facts vs policy classification

For every projected field/mapping, record:

```text
source-owned fact?
Governance policy?
stable cross-context vocabulary?
migration impact?
```

Examples such as `BoardRole.Observer → Viewer` or `BoardVisibility.Workspace → WorkspaceAudience` are
not accepted automatically. They require proof that the target vocabulary is a fact projection rather
than a hidden second permission hierarchy.

## 69. WG-RES-007 — Account/Workspace consistency

Ensure authorization context cannot pair Account A with Workspace/resource belonging to Account B and
still evaluate as valid.

## 70. WG-RES-008 — transport-neutral evolution seam

The provider contract MUST NOT expose EF, gRPC, HTTP or message-broker types. Implementations may evolve:

```text
now:    in-process resource-owner query / EF read model
later:  gRPC client to extracted owner service
or:     event-fed local authorization projection
or:     cache / hybrid freshness verification
```

Future extraction changes adapters, not business ownership or authorization request semantics.

## 71. WG-RES-009 — module-first slice placement

When touching Workspaces/Governance Application code, follow the candidate-SHA canonical module-first
layout. Do not add new production use cases to deprecated legacy feature paths just to minimize diff.

## 73. Phase 6 exit

Required D4 producer contract:

- stable resource category using canonical current source naming;
- stable resource-owned Action vocabulary;
- resource-owner facts lookup/provider ownership defined;
- no Governance private EF read of downstream contexts;
- representative Board contract can be implemented without private-context coupling;
- P3-A Domain/Data work may open if Workspace/containment prerequisites are also D4+.

Status:

```text
WG-RES-001 inventory COMPLETE — 16 ResourceKind kinds, no legacy ResourceType/PermissionContext/Subject.
WG-RES-002 canonical category KEPT as ResourceKind — no forced rename (source already uses it; no rename proof required).
WG-RES-003 ownership CLEAR — resource teams own identity/lifecycle/Action; Governance owns only registry mechanics
    (see www-data plan §67). No Governance-invented product actions.
WG-RES-004 FACTS contract CONFIRMED — AccessFacts (Application/Common/Security/AccessFacts.cs) is entirely
    fact-based (UserExists, EmailVerified, AccountExists, AccountMemberRole, WorkspaceExists, WorkspaceMemberRole,
    ResourceExists, ResourceAudience, ResourceMemberRole, HasExplicitResourcePermission, PermissionRules,
    HasActiveSubscription, SubscriptionTier, FeatureEnabled, AccountOperational, UserOperational). No Can* policy fields.
WG-RES-005 provider ownership = D6-A — ResourceLocator documented as approved cross-context read port:
    concentrates per-DbContext reads for the ExecutionContextBehavior scope resolution; performs no authz/mutation;
    serves the cross-cutting concern so does not become WorkManagement business coupling. Action: canonical doc entry
    added to backend/docs/architecture/security-tenancy-authorization.md §BE-SEC-011.
WG-RES-006 facts vs policy CLEAN — no projected field was introduced as a hidden second permission hierarchy;
    PermissionRule carries Governance policy; AccessFacts stays source-owned facts.
WG-RES-007 Account/Workspace pairing PROVEN — ExecutionContextBehavior derives Account ALWAYS from the owned
    workspace/resource row (workspace → ResolveWorkspaceAccessAsync; resource → ResourceLocator); API-token path
    additionally enforces BoundAccountId equality. New negative proof added: Application.Tests
    ExecutionContextBehaviorTests "Workspace_request_with_api_token_bound_to_different_account_is_denied"
    (ApiToken bound to Account B + workspace owned by Account A → ForbiddenException, no snapshot).
WG-RES-008 transport-neutral seam CONFIRMED — IResourceLocator (Application/Common/Context) exposes no EF/gRPC/HTTP/
    broker types; ResourceLocator implementation may evolve independently.
WG-RES-009 module-first layout respected — no production use case added to deprecated legacy feature paths.
No Governance private EF read of downstream contexts — GovernanceStubConsumers (Messaging/Consumers/Governance)
    are pure logging stubs with no downstream DbContext/service/provider coupling.
Representative Board contract is implementable without private-context coupling — ResourceLocator reads Boards only
    as the cross-cutting scope resolver (D6-A), not as a WorkManagement business use case.
Evidence: backend.doc security-tenancy-authorization.md doc entry; Application.Tests +1 (588 total);
    Architecture.Tests 410. Phase 6 CLOSED. P3-A Domain/Data may open under the approved producer contract.
```

# Phase 7 — Permission model

## 74. WG-PERM-001 — canonical Permission model

Requirements:

```text
WGREQ054–WGREQ062
```

Using Phase 2 semantic map, establish one canonical meaning.

## 75. WG-PERM-002 — permission identity

Ensure stable ID/key if persisted.

Document migration semantics.

## 76. WG-PERM-003 — tenant/resource scope

Permission evaluation must include correct Workspace/resource context.

## 77. WG-PERM-004 — default deny

Verify absence of grant/policy produces denied protected action according to architecture.

## 78. WG-PERM-005 — explicit deny only if current model supports it

Do not introduce deny precedence casually.

If current model already supports deny, document exact precedence and test it.

## 79. WG-PERM-006 — PermissionRule integration

After classification:

- retain valid rules;
- remove/deprecate duplicate legacy model only through migration;
- no new DSL.

## 80. WG-PERM-007 — permission cache

If present:

```text
key
scope
invalidation
security window
tenant isolation
```

must be explicit.

## 81. WG-PERM-008 — DB/index hardening

Verify indexes for:

```text
subject
workspace
resource
action
role
```

as relevant.

## 82. Phase 7 exit

Permission semantics D4/D5-ready.

Status:

```text
WG-PERM-001 canonical model ESTABLISHED — single decision path = AccessPolicyEngine.EvaluatePermission
    (pure, architecture-tested) + AccessFacts (server-derived facts) + AccessFactsQuery (single canonical
    SQL authority) + IRequirePermission (PermissionAction + ResourceRef). §11 contract inventory corrected
    (removed stale IPermissionService/IPermissionEvaluator/... names; added actual contracts) — DOC_STALE resolved.
WG-PERM-002 permission identity DOCUMENTED — PermissionAction enum members persist as ToString(); ResourceKind
    record struct persists via converter as {context}.{resource} string. Renaming stored action/resource_type is
    a persisted-meaning change → data migration, not in-place rewrite. Recorded in §15a + ledger.
WG-PERM-003 tenant/resource scope CONFIRMED — AccessFactsQuery binds account_id + workspace_id to the owning
    Workspace/request scope; subject_type='User'/subject_id; scope_type='Workspace' or in-workspace resource match.
    Workspace A rule can never authorize Workspace B. Proven by EvaluateAsync_BoardFromAnotherWorkspace_IsHidden
    + CrossTenantIsolationTests.
WG-PERM-004 default deny CONFIRMED — role null → Deny; no applicable allow band → tail default deny
    (only ViewWorkspace/ViewBoard/ViewMembers baseline). Proven: ShouldDenyNonMembers,
    AccountScope_ShouldDenyNonMembers, AccountScope_ShouldDenySuspendedMembers, InactiveOrOutOfWindowRule_IsIgnored.
WG-PERM-005 deny precedence CONFIRMED + TESTED — within min-priority rule band, any Deny → deny before Allow.
    Proven: EvaluateAsync_SamePriorityDenyOverridesAllow, AccountScope_ExplicitGovernanceDeny_OverridesAdminFallback,
    AccountScope_ExplicitGovernanceAllow_GrantsBaselineDeniedAction. Precedence documented in §15a.
WG-PERM-006 PermissionRule integration CONFIRMED — PermissionRule is the single canonical persisted action-level
    rule; ResourcePermission = subject→resource ACL (engine uses row-existence); WorkspacePolicy = secondary config
    not in evaluator. No duplicate DSL. §11/§15a single-owner statement.
WG-PERM-007 permission cache NONE — no runtime permission-decision cache in the effective path; AccessFacts
    computed per protected request. Persisted resource_permission_inheritance_cache projection is NOT on the
    decision path (BE-SEC-024 revocation effective next request, no cache security window).
WG-PERM-008 DB/index hardening VERIFY + DEFER (measured promotion gate) — existing permission_rules indexes:
    (workspace_id), (scope_type, action), (status); resource_permissions: (resource_kind, resource_id), (subject_id).
    FINDING: hot-path access-facts query filters permission_rules by (account_id, workspace_id, action,
    subject_type, subject_id, scope_type, status + validity/resource predicates); no subject-aware/index aligned
    to the query leading predicate. correctness/security blocker: NO; Phase-7 semantic blocker: NO; performance
    finding: YES. DEFERRED by decision: run EXPLAIN (ANALYZE, BUFFERS) at representative permission-rule
    cardinality/Workspace distributions in a performance/migration hardening task before choosing index shape
    ((workspace_id, action, subject_id) not pre-committed; may need account_id/subject_type/partial for Active).
    No DDL this phase. Owner/follow-up registered in PR-WG-05 ledger.
WG-TST-PRULE-SEC-001 (WGREQ062) client claims not trusted — CONFIRMED by design: Action originates from
    server-side IRequirePermission declaration; all membership/ownership/role/resource facts are server-derived
    rows; no client-supplied role/workspace/owner presented to the engine.
Evidence: canonical doc §11 correction + §15a permission evaluation contract; Application.Tests 588 (no delta);
    Architecture.Tests 410 (no source change). Phase 7 CLOSED. Phase 8 (built-in roles, PR-WG-05 continuation)
    may open.
```

# Phase 8 — Built-in roles

## 83. WG-ROLE-001 — role model audit

Requirements:

```text
WGREQ063–WGREQ070
```

Record:

```text
role ID
scope
built-in/custom
permissions
subject assignment
deletion
events
```

## 84. WG-ROLE-002 — built-in role baseline

Select only product-approved roles already present/defined.

Do not invent role taxonomy.

## 85. WG-ROLE-003 — stable role identity

Display-name change must not break persisted role assignments.

## 86. WG-ROLE-004 — permission mapping

Built-in role → permission mapping must be deterministic and version/migration-safe.

## 87. WG-ROLE-005 — assignment

Verify:

- member/team subject;
- Workspace scope;
- authorization;
- duplicates;
- revocation.

## 88. WG-ROLE-006 — last-admin invariant

If admin/owner role encodes required Workspace administration, enforce concurrency-safe invariant.

## 89. WG-ROLE-007 — custom-role deferral

If custom roles are not required for P3:

```text
do not block P2 core
```

## 90. Phase 8 exit

Built-in role baseline at least D4.

# Phase 9 — Existing authorization path hardening

## 91. Purpose

Bind accepted Governance semantics behind the **existing canonical Application authorization path**.
Do not create a second behavior/evaluator/decision-store stack if candidate source already has one.

## 92. WG-AUTHZ-001 — inventory central authorization behavior

Search:

```bash
rg -n \
  "AuthorizationBehavior|IAuthorizationDecisionStore|IPermissionEvaluator|PermissionContext|IRequirePermission|ResourceType|ResourceKind|Permission" \
  backend/src backend/tests
```

Record:

- implementation;
- interfaces;
- DI registration;
- pipeline ordering;
- error type;
- cache/dependency;
- tests.

Decision rule:

```text
existing responsibility is valid → reuse/harden
existing responsibility is incomplete → extend behind same contract
existing responsibility conflicts semantically → stop + explicit migration decision
new parallel authorization stack → forbidden
```

## 93. WG-AUTHZ-002 — production registration

Prove central behavior is registered in production DI.

## 94. WG-AUTHZ-003 — pipeline ordering

Inspect ordering relative to:

```text
validation
authorization
idempotency
transaction
logging
```

Do not reorder unless architecture evidence requires it.

## 95. WG-AUTHZ-004 — canonical decision inputs

Ensure the pipeline can receive:

```text
Actor
Account
Workspace/resource context
ResourceCategory (canonical source name, currently possibly ResourceType)
ResourceId
Action
```

using current abstractions.

## 96. WG-AUTHZ-005 — effective decision evaluator

Connect:

```text
membership
role
permission
policy/resource permission where supported
```

to one decision path.

## 97. WG-AUTHZ-006 — fail closed

Missing/invalid context or evaluator failure must not grant access.

## 98. WG-AUTHZ-007 — unauthorized handler side effect proof

Representative protected handler:

```text
authorization denied
→ handler protected work does not commit
```

## 99. WG-AUTHZ-008 — role-check debt inventory

Review local checks found in Phase 0.

For each:

```text
keep as Domain invariant
migrate to Governance
remove duplicate
defer with source debt
```

Do not mass-rewrite without semantic proof.

## 100. WG-AUTHZ-009 — API-only checks

Where endpoint has business permission logic:

move declaration/enforcement to approved Application path while preserving transport adaptation.

## 101. WG-AUTHZ-010 — background actor

For relevant background operations:

verify explicit principal/authorization semantics.

No global bypass.

## 102. Phase 9 exit

Authorization integration reaches D4/D5 for representative resources.

# Phase 10 — WorkManagement handshake

## 103. Purpose

This is the required representative consumer proof before P3-B protected Application/API release.
P3-A Domain/Data work may already be running under the Phase 6 producer contract.

## 104. WG-WM-001 — identify first WorkManagement resource

Default candidate:

```text
Board
```

because Board is P3 transactional root.

Do not start with every WorkManagement resource.

## 105. WG-WM-002 — Board resource authorization category

WorkManagement owns semantic declaration.

Governance consumes:

```text
kind
resource ID
Workspace/Account scope
supported actions
```

## 106. WG-WM-003 — Board actions

Select only current product-approved Board actions needed for first vertical slice.

Examples are not authoritative.

Do not predesign every future Board action.

## 107. WG-WM-004 — resource lookup

If authorization requires Board→Workspace or actor↔Board facts:

- use an approved transport-neutral authorization facts SPI/resource-owner contract;
- implement the Board data access adapter under WorkManagement ownership;
- keep `Board`, `BoardMember`, `BoardRole`, `BoardVisibility` and `IWorkManagementDbContext` knowledge behind that adapter;
- Governance consumes facts and evaluates policy.

Do not place a Board resolver under `Infrastructure.Governance` if it directly queries WorkManagement private persistence.

## 108. WG-WM-005 — representative allow path

Test:

```text
Actor with valid Workspace membership + permission
→ Board action allowed
```

## 109. WG-WM-006 — representative deny path

Test:

```text
Actor lacks permission
→ Board action denied
→ handler does not commit
```

## 110. WG-WM-007 — cross-tenant deny

Test:

```text
Actor in Account A
→ Board in Workspace under Account B
→ denied
```

## 111. WG-WM-008 — no role-string dependency

WorkManagement handler must not know Governance role display names.

## 112. WG-WM-009 — BoardItem deferral

Only add BoardItem independent resource contract if current product requires independent authorization.

Do not create per-entity ACL by default.

## 113. Phase 10 exit

WorkManagement handshake proven.

# Phase 11 — P2 protected-slice verification and staged P3 handoff

## 114. P3-A producer gate

This gate enables WorkManagement Domain/Data parallelization, not protected product release.

| Contract | Target |
|---|---|
| Workspace identity | D4+ |
| Account→Workspace containment | D4+ |
| resource authorization category / ResourceId | D4+ |
| resource-owned Action vocabulary | D4+ |
| no Governance private downstream persistence dependency | proven |

When this gate passes, WorkManagement may implement its resource-owned transactional core in parallel.

## 115. P3-B protected-slice gate

Required before representative protected Application/API release:

| Contract | Target |
|---|---|
| Workspace | D5 |
| Account→Workspace containment | D5 |
| WorkspaceMember | D4+ |
| resource category/resource | D5 |
| Action | D5 |
| Permission | D5 for representative slice |
| Built-in role baseline | D4+ |
| existing central authorization path | D5 |
| resource-owner facts provider boundary | D5 |
| WorkManagement Board handshake | D5 |

## 116. WG-GATE-001 — architecture proof

Required:

- Workspaces and Governance remain separate logical contexts;
- resource contexts own resource/action/facts semantics;
- Governance owns permission/role/policy decision semantics;
- Application pipeline owns enforcement;
- no Governance adapter queries private WorkManagement/Documents/Billing/etc. persistence;
- no second authorization pipeline/evaluator stack;
- no forced `ResourceType`→`ResourceKind` rename without migration decision;
- Domain purity and production project topology preserved.

## 117. WG-GATE-002 — representative security proof

Required scenarios:

```text
Account A → Workspace A allowed
Account A → Workspace B under Account B denied
member removed → no stale access
role removed → no stale access
Board allow through canonical AuthorizationBehavior/decision path
Board deny before handler side effects
Board cross-tenant deny
resource lookup unavailable/invalid → fail closed according to canonical policy
```

## 118. WG-GATE-003 — cross-context ownership proof

For WorkManagement representative adapter verify:

```text
interface/contract is transport-neutral
implementation owner = WorkManagement
private DB access = WorkManagement only
Governance receives facts, not Board aggregate/private EF model
no policy decision hidden inside resource facts provider
```

Repeat the same architecture rule for Documents/Billing/Automation/Integrations when they onboard.

## 119. WG-GATE-004 — migration proof

If schema or persisted authorization identifiers changed:

- clean DB;
- supported upgrade DB;
- no pending model changes;
- persisted permission/role/policy/resource-action compatibility;
- seed/init if affected.

## 120. WG-GATE-005 — staged P3 handoff

```text
P3-A passed → Domain/Data parallel work allowed
P3-B passed → protected Application/API slice + release certification allowed
```

Secondary Workspace/Governance scope continues in parallel and must not change the frozen producer contract
without reopening review gates.

# Phase 12 — Provisioning / Settings / WorkspaceHome

## 120. WG-PROV-001 — provisioning source audit

Inspect Application `Provisioning`.

Determine whether it creates:

```text
Workspace
Member
roles
Team
Space
Account-related state
```

## 121. WG-PROV-002 — orchestration ownership

For every created object:

```text
owner context
transaction boundary
idempotency
event
partial failure
```

No giant aggregate.

## 122. WG-PROV-003 — provisioning idempotency

Retry must not duplicate:

- Workspace;
- bootstrap member;
- role assignment;
- Team/Space.

## 123. WG-PROV-004 — partial failure

If multi-context:

define compensation/retry/partial state.

## 124. WG-SET-001 — Settings classification

Inspect current `Settings`.

Classify each setting:

```text
Workspace
Governance
Identity/User
Account
Product-specific
```

## 125. WG-HOME-001 — WorkspaceHome classification

Determine whether WorkspaceHome is:

- persisted business aggregate/read model;
- composition query;
- UI-oriented DTO;
- placeholder.

Do not promote composition DTO into Domain model without evidence.

# Phase 13 — Teams / Spaces / Workspace Rules

## 126. WG-TEAM-001 — Team model

Requirements:

```text
WGREQ034–WGREQ037
```

Verify:

- Workspace containment;
- membership;
- lifecycle;
- optional Governance subject role.

## 127. WG-TEAM-002 — Team membership

Only Workspace members may become Team members unless product says otherwise.

## 128. WG-TEAM-003 — Team authorization subject

If supported, integrate through Governance subject abstraction.

No automatic permission escalation.

## 129. WG-SPACE-001 — Space model

Requirements:

```text
WGREQ038–WGREQ041
```

Classify exact business meaning before expanding.

## 130. WG-SPACE-002 — containment

If Space is Workspace child:

enforce same-Workspace/Account consistency.

## 131. WG-SPACE-003 — authorization

If governable, register Space resource/actions through same handshake.

## 132. WG-RULE-001 — implement classification from Phase 2

For Workspaces Rules:

- retain Workspace rules;
- rehome Governance rules only with migration;
- separate Automation rules;
- remove legacy only when proven unused.

# Phase 14 — Custom Roles / Policies / ResourcePermissions

## 133. WG-CROLE-001 — custom role release scope

Only implement if product release requires it.

P3 is already allowed to proceed after P2 core.

## 134. WG-CROLE-002 — create/update/delete

Must define:

- authorization;
- permission assignment;
- subject impact;
- cache invalidation;
- deletion migration.

## 135. WG-POL-001 — Policy semantics

Use Phase 2 classification.

Do not broaden policy language.

## 136. WG-POL-002 — policy evaluation

Verify deterministic, tenant-scoped, fail-closed behavior.

## 137. WG-POL-003 — policy versioning

Persisted schema requires migration/versioning.

## 138. WG-RPERM-001 — ResourcePermission implementation

Use canonical meaning only.

If direct ACL:

define:

```text
subject
resource
actions/permissions
scope
expiry if any
```

## 139. WG-RPERM-002 — inheritance

Only if existing product/model supports it.

No speculative inheritance tree.

## 140. WG-RPERM-003 — revoke

Invalidate effective-access cache safely.

# Phase 15 — ShareLinks

## 141. WG-SHARE-001 — source audit

Inspect Domain/Application/Infrastructure/API model.

Map:

```text
resource
secret/token
permissions/actions
expiry
revocation
anonymous/authenticated use
audit
```

## 142. WG-SHARE-002 — secure issuance

Use approved secure random/secret protection.

## 143. WG-SHARE-003 — bounded access

A valid link grants only intended resource/action.

No Workspace-wide escalation unless product explicitly says so.

## 144. WG-SHARE-004 — expiry/revocation

Prove stale cache cannot preserve access beyond accepted window.

## 145. WG-SHARE-005 — non-member semantics

Do not auto-create normal membership unless explicit flow requires it.

## 146. WG-SHARE-006 — secret response policy

Raw secret only in intended issuance/share flow.

No ordinary list readback.

# Phase 16 — Audit / SecurityEvents / Templates

## 147. WG-AUD-001 — AuditLogs source audit

Classify:

```text
governance audit
general activity
observability log
```

Governance should not duplicate every system event.

## 148. WG-AUD-002 — critical change audit

Ensure critical:

- membership;
- role;
- permission;
- policy;
- share-link

changes produce appropriate audit evidence.

## 149. WG-AUD-003 — audit query security

Audit APIs themselves require permission and tenant scope.

## 150. WG-SEC-EVT-001 — SecurityEvents classification

Distinguish:

```text
security fact
infrastructure exception
general activity
```

## 151. WG-TPL-001 — Templates

Verify current Domain `Templates` semantics.

Do not assume template automatically becomes role/policy live state.

## 152. WG-TPL-002 — template apply

Validate permission/action compatibility at application time.

# Phase 17 — API / Events / Authorization harmonization

## 153. WG-API-001 — endpoint inventory reconciliation

Every endpoint maps to approved Application use case.

## 154. WG-API-002 — error taxonomy

Normalize:

```text
unauthenticated
forbidden
not found/privacy
validation
conflict
expired/revoked link/invite
invalid state transition
```

## 155. WG-API-003 — sensitive responses

Invitation/share secret leakage prohibited.

## 156. WG-API-004 — OpenAPI

Update drift/generation when API contracts change.

## 157. WG-EVT-001 — event inventory

Map:

```text
producer
event meaning
payload
Account
Workspace
subject/resource
consumers
security/PII
```

## 158. WG-EVT-002 — consumer compatibility

WorkManagement/Billing/Analytics consumers use stable events/contracts.

## 159. WG-AUTHZ-HARM-001 — resource/action declaration scan

Ensure all protected P2 endpoints/handlers use approved authorization path.

# Phase 18 — Persistence / Migration / Compatibility

## 160. WG-MIG-001 — schema diff inventory

Classify every change:

```text
additive
constraint
backfill
resource/action ID migration
role/permission migration
policy schema migration
secret-format migration
ownership move
```

## 161. WG-MIG-002 — membership

Preserve:

- User reference;
- Workspace reference;
- role assignments;
- audit.

## 162. WG-MIG-003 — resource/action identifiers

If renaming:

update:

```text
permissions
roles
policies
resource permissions
share links
consumer declarations
fixtures
tests
```

## 163. WG-MIG-004 — built-in roles

Semantic change to a built-in role requires migration/impact analysis for existing Workspaces.

## 164. WG-MIG-005 — policy schema

Backfill/version stored policies.

## 165. WG-MIG-006 — invitation/share secret

Define compatibility/rotation/revocation.

## 166. WG-MIG-007 — clean DB

Required after schema changes.

## 167. WG-MIG-008 — upgrade DB

Required from supported prior state.

## 168. WG-MIG-009 — pending model

Must be none.

Do not suppress warning.

# Phase 19 — Security hardening

## 169. WG-HARD-SEC-001 — privilege escalation matrix

Test:

```text
self role elevation
grant beyond authority
invite with excessive role
Team membership escalation
cross-Workspace resource grant
policy mutation without authority
```

## 170. WG-HARD-SEC-002 — stale cache matrix

Test revocation of:

```text
membership
role
permission
policy
share link
```

## 171. WG-HARD-SEC-003 — cross-tenant matrix

Test Account/Workspace/resource mismatch combinations.

## 172. WG-HARD-SEC-004 — bearer secret leakage

Search/capture:

```text
invitation secret
share-link secret
authorization header
session/API token
```

must not appear in ordinary logs/events/responses.

## 173. WG-HARD-SEC-005 — fail closed

Simulate:

- missing policy data;
- permission evaluator failure;
- resource lookup failure.

Protected action must not become allowed.

## 174. WG-HARD-SEC-006 — background path

No global/bypass principal without approved semantics.

# Phase 20 — Concurrency / Reliability

## 175. WG-CONC-001 — duplicate membership

Concurrent add/accept.

## 176. WG-CONC-002 — last admin

Concurrent demote/remove.

## 177. WG-CONC-003 — invitation race

Accept vs revoke/expire.

## 178. WG-CONC-004 — role assignment race

Assign/remove.

## 179. WG-CONC-005 — policy stale update

If versioned concurrency exists.

## 180. WG-CONC-006 — share revoke

Revoke vs use/refresh.

## 181. WG-REL-001 — provisioning partial failure

Verify defined behavior.

## 182. WG-REL-002 — evaluator dependency outage

Fail safely.

## 183. WG-REL-003 — event publication

Use existing outbox/transaction semantics.

## 184. WG-REL-004 — cache outage

Fallback must not default-allow.

# Phase 21 — Observability / Performance

## 185. WG-OBS-001 — authorization trace

Ensure safe correlation:

```text
Actor
Account
Workspace
ResourceKind
ResourceId
Action
Decision
```

## 186. WG-OBS-002 — admin mutation auditability

Membership/role/policy/share changes traceable.

## 187. WG-OBS-003 — secret-safe telemetry

No bearer tokens/secrets.

## 188. WG-PERF-001 — authorization hot path

Inspect:

- DB calls;
- cache calls;
- policy evaluation count;
- resource-parent lookups.

## 189. WG-PERF-002 — membership lookup

Ensure correct indexes/caching.

## 190. WG-PERF-003 — permission lookup

Avoid unbounded scans.

## 191. WG-PERF-004 — list pagination

Member/audit/permission lists use canonical pagination strategy.

## 192. No invented performance thresholds

Use current quality authority/baseline.

## Cross-context handshake rule used by Phase 22

For every producer/consumer pair, produce a contract card:

```text
Producer context:
Consumer context:
Owned business state:
Stable IDs:
Authorization facts exported:
Actions owned by producer:
Policy mapping owned by Governance:
Synchronous lookup required?:
Events/projection available?:
Consistency/freshness requirement:
Failure mode / fail-closed rule:
Current in-process adapter:
Future extraction seam (gRPC/event projection/cache/hybrid):
Migration/versioning:
Forbidden private dependencies:
```

Do not select gRPC now unless extraction is actually being implemented. The seam must make gRPC possible
later without making the modular monolith behave like a distributed system today.

# Phase 22 — Cross-context integration hardening

## 193. WG-X-001 — Identity/Accounts

Prove no credential/private Identity dependency.

## 194. WG-X-002 — WorkManagement

Mandatory P2 handoff.

Prove:

```text
Board resource/action declaration
allow
deny
cross-tenant deny
no role-name coupling
```

## 195. WG-X-003 — Documents

Prove Page/Document can use same Governance handshake when ready.

## 196. WG-X-004 — Collaboration

Comment authorization uses target resource access contract, not target private tables.

## 197. WG-X-005 — Billing

Billing admin action can combine:

```text
Governance authorization
+
Billing entitlement/business rule
```

without conflation.

## 198. WG-X-006 — Automation/Integrations

Administration actions use Governance; background execution uses approved actor semantics.

## 199. WG-X-007 — Analytics

Audit/governance facts exposed through approved events/read contracts only.

# Phase 23 — TESTS handoff

## 200. Purpose

Produce deterministic input for:

```text
workspace-governance.tests.md
```

## 201. WG-TEST-HO-001 — requirement matrix

Build:

```text
WGREQ
Capability
PLAN work unit
Existing test
Missing test
Test layer
Security/concurrency?
Migration?
CI gate
```

## 202. WG-TEST-HO-002 — mandatory test families

TESTS must include:

```text
Workspace
Membership
Invitation
Teams/Spaces if release scope
resource authorization category / Action
Permission
Role
Policy
ResourcePermission
Authorization pipeline
ShareLinks
Audit/SecurityEvents
Tenant isolation
Privilege escalation
Concurrency
Migration
WorkManagement handoff
```

## 203. WG-TEST-HO-003 — production graph tests

Flag tests requiring:

- real DI;
- DB;
- authorization behavior;
- cache;
- tenant isolation;
- migration.

## 204. WG-TEST-HO-004 — architecture tests

Must enforce:

```text
Domain purity
Workspaces/Governance separation
no private downstream EF dependency
pipeline-owned authorization
```

## 205. WG-TEST-HO-005 — P2 gate tests

TESTS must define a dedicated P2 core gate group.

# Phase 24 — Docs / generated-contract handoff

## 206. WG-DOC-001 — canonical docs

Update only changed canonical product/architecture authorities.

Potential:

```text
workspaces.md
governance.md
security-tenancy-authorization.md
application-model.md
API contracts
ADR if architecture changes
```

## 207. WG-DOC-002 — OpenAPI/generated

Regenerate/check if API changed.

## 208. WG-DOC-003 — workstream state

Only mark D4/D5 when evidence exists.

## 209. WG-DOC-004 — no authority duplication

Do not copy full role/permission catalogs into multiple docs if source/generated evidence is canonical.

# Phase 25 — CERTIFICATION handoff

## 210. Required certification milestones

`workspace-governance.certification.md` must define:

```text
MILESTONE A
P2 CORE CERTIFIED
→ WorkManagement may rely on P2

MILESTONE B
WORKSPACE & GOVERNANCE FULL SCOPE CERTIFIED
→ release-scoped secondary capabilities complete
```

## 211. WG-CERT-HO-001 — P2 core inputs

Must include:

```text
Workspace D5
containment D5
Member D4+
Resource/Action D5
Permission D5
Built-in Role D4+
Authorization D5
WorkManagement handshake D5
Architecture
Security
Migration
CI exact SHA
```

## 212. WG-CERT-HO-002 — secondary scope

Separate certification for:

```text
Invitations
Teams
Spaces
custom roles
advanced policies
ResourcePermissions
ShareLinks
Audit/SecurityEvents
Templates
```

only when release-scoped.

# Layer-by-layer execution contract

## 213. Domain

Allowed:

- Workspace/Member/Invitation/Team/Space invariants;
- Governance Role/Permission/Policy/ShareLink semantics;
- Domain events.

Forbidden:

- EF;
- HTTP;
- authorization pipeline code;
- provider/cache mechanism;
- WorkManagement private state.

## 214. Application

Owns:

- use cases;
- validation;
- authorization declaration;
- orchestration;
- interfaces;
- transactions;
- result/error mapping.

## 215. Infrastructure

Owns:

- persistence;
- indexes;
- migrations;
- secret verification storage;
- cache implementation;
- resource lookup adapters where architecture assigns them.

Infrastructure MUST NOT decide permission semantics.

## 216. API

Owns:

- transport;
- endpoint mapping;
- HTTP errors;
- OpenAPI.

API MUST NOT be sole business authorization enforcement.

## 217. Platform

Touched only for generic mechanism:

- authorization pipeline;
- generic cache/rate/observability;
- messaging.

Do not move Governance business state to Platform.

# File-change discipline

## 218. Before changing Workspaces Domain

Inspect:

```text
backend/src/Notrelix.Domain/Workspaces/**
relevant Domain tests
downstream Workspace references
```

## 219. Before changing Governance Domain

Inspect:

```text
backend/src/Notrelix.Domain/Governance/**
relevant Domain tests
Application Governance consumers
```

## 220. Before changing Application

Inspect:

```text
backend/src/Notrelix.Application/Features/Workspaces/**
backend/src/Notrelix.Application/Features/Governance/**
current authorization/common pipeline abstractions
```

## 221. Before changing Infrastructure

Inspect actual configuration/storage/migration files discovered in Phase 0.

Do not assume paths.

## 222. Before changing API

Inspect current Workspace/Governance endpoints and auth extensions.

## 223. Files/areas not to change casually

Do not change merely to simplify P2:

```text
Identity credential/session internals
WorkManagement Domain
Documents Domain
Billing Domain
Platform messaging
production project topology
global architecture rules
```

# PR decomposition

## 224. PR strategy

Prefer contract-first vertical slices.

Do not create one giant "rewrite authorization" PR.

## 225. PR-WG-00 — semantic inventory/decision

Contains:

- overlap classification;
- source debt record;
- migration decisions if required.

Minimal/no production change if possible.

## 226. PR-WG-01 — Workspace core

Scope:

- Workspace identity;
- Account containment;
- lifecycle;
- persistence/migration;
- tests.

## 227. PR-WG-02 — Membership core

Scope:

- Member model;
- add/remove;
- active/inactive;
- last-admin invariant;
- tests.

## 228. PR-WG-04 — Phase 6 Resource/Action contract

Scope:

- canonical resource/action model;
- resource registration mechanism;
- tests.

No WorkManagement business changes except minimal consumer fixture/contract if necessary.

> Execution note: PR IDs are monotonic execution identities; they do not equal phase numbers.
> Executed history: PR-WG-01 (Phase 3), PR-WG-02 (Phase 4), PR-WG-03 (Phase 5). PR-WG-04
> corresponds to Phase 6 (Resource/Action contract).

## 229. PR-WG-05 — Phase 7/8 Permission + built-in Role

Scope:

- permission semantics;
- built-in roles;
- effective mapping;
- tests.

## 230. PR-WG-06 — Phase 9 Authorization pipeline integration

Scope:

- Governance evaluator;
- Application enforcement;
- role-check debt migration limited to representative P2 paths;
- architecture tests.

## 231. PR-WG-07 — Phase 10/11 WorkManagement handshake / P2 gate

Scope:

- Board resource/action integration;
- allow/deny/cross-tenant proof;
- P2 certification evidence.

## 232. PR-WG-08 — Invitations / Provisioning

As release scope requires.

## 233. PR-WG-09 — Teams / Spaces

Independent after core where possible.

## 234. PR-WG-10 — Custom roles / Policies / ResourcePermissions

Keep policy complexity isolated from P2 core PRs.

## 235. PR-WG-11 — ShareLinks

Focused security review.

## 236. PR-WG-12 — Audit / SecurityEvents / Templates

Only release-scoped work.

## 237. PR-WG-13 — hardening/migration cleanup

Use only for residual cross-cutting fixes, not miscellaneous refactor.

# Parallelization

## 238. Before P2 core gate

Safe parallelism after Phase 2 semantics resolved:

```text
Workspace core
Membership core
Resource/Action contract
TESTS drafting from source inventory
```

Permission/Role work may overlap once resource/action semantics are stable.

## 239. After P2 core gate

Parallel:

```text
WorkManagement starts
Invitations/provisioning
Teams/Spaces
custom roles/policies
ShareLinks
Audit/SecurityEvents
```

## 240. Unsafe parallelism

Do not concurrently redesign:

- Permission in one PR;
- Policy in another;
- ResourcePermission in another

without shared semantic hierarchy established in Phase 2.

## 241. Shared-file rule

If two workstreams modify same evaluator/model:

- one owner;
- explicit sequence;
- no merge-conflict semantic reconciliation by convenience.

# Migration order

## 242. Resource/action migration before downstream hardening

If persisted resource-category/Action IDs change (whether named ResourceType, ResourceKind or equivalent), complete the approved migration before downstream consumers harden against the new contract.

## 243. Role/permission compatibility

Prefer additive migration:

```text
add new semantic identifier
migrate assignments/policies
update consumers
remove legacy last
```

when deployment cannot be atomic.

## 244. Security links

For invitation/share-token format changes:

define:

```text
existing link validity
rotation
forced revoke
compatibility window
```

# Error handling

## 245. Workspace errors

Potential categories:

```text
Workspace not found
Workspace inaccessible
Workspace inactive
member conflict
last-admin violation
invalid lifecycle
```

Use canonical API taxonomy.

## 246. Governance errors

Potential:

```text
forbidden
unknown resource/action
invalid policy
role conflict
permission conflict
expired/revoked share link
```

Do not expose sensitive policy internals.

# Coding-agent reporting

## 247. Required report per work unit

```text
Work unit:
WGREQ IDs:
Source inspected:
Files changed:
Why:
Workspaces impact:
Governance impact:
Application impact:
Infrastructure impact:
API impact:
Migration:
Downstream contract:
Tests:
Commands:
Result:
Source debt:
Architecture/product decision:
Stop condition:
```

## 248. No hidden semantic decisions

Any change to:

```text
membership ownership
Permission meaning
Policy precedence
ResourcePermission meaning
role identity
resource/action identity
```

must be explicitly tied to WGREQ/source decision.

# Stop registry

## 249. WG-PLAN-STOP-001 — P1 upstream unstable

Stop affected hardening.

## 250. WG-PLAN-STOP-002 — Account/Workspace ambiguity

Do not encode containment until resolved.

## 251. WG-PLAN-STOP-003 — membership ownership conflict

Do not create duplicate membership.

## 252. WG-PLAN-STOP-004 — Workspaces Rules overlap

Resolve classification first.

## 253. WG-PLAN-STOP-005 — Permission/PermissionRule/Policy overlap

Do not expand model until hierarchy is explicit.

## 254. WG-PLAN-STOP-006 — ResourcePermission ambiguity

Do not add ACL depth.

## 255. WG-PLAN-STOP-007 — resource/action persistence consumers unknown

No rename/removal.

## 256. WG-PLAN-STOP-008 — local handler checks needed

Do not bypass central architecture.

## 257. WG-PLAN-STOP-009 — background global bypass

Not allowed.

## 258. WG-PLAN-STOP-010 — share security undefined

Stop ShareLink release only; P2 core may continue.

## 259. WG-PLAN-STOP-011 — migration model drift

No pending-model suppression.

## 260. WG-PLAN-STOP-012 — architecture gate conflict

Do not weaken test.

## WG-PLAN-STOP-013 — foreign persistence adapter under Governance

If a Governance-owned implementation directly references another bounded context's DbContext/private
aggregate/table (for example Board authorization resolver → `IWorkManagementDbContext`), STOP and restore
resource-owner adapter ownership before continuing.

## WG-PLAN-STOP-014 — duplicate authorization mechanism

If new resolver/evaluator/behavior duplicates an existing `Application/Common/Security` responsibility,
STOP until reuse/extension/replacement is explicitly classified.

## WG-PLAN-STOP-015 — forced resource naming migration

If implementation renames `ResourceType`/equivalent to `ResourceKind` solely because this execution package
uses that vocabulary, STOP. Require semantic + migration evidence.

## WG-PLAN-STOP-016 — facts become policy

If a resource facts provider returns or derives Governance decisions (`CanEdit`, effective permission sets,
implicit manager-is-allow hierarchy) without explicit ownership approval, STOP and separate facts from policy.

## WG-PLAN-STOP-017 — transport leakage

If canonical Application/Domain contracts expose gRPC/HTTP/message-specific DTOs to prepare for future split,
STOP. Keep the contract transport-neutral and adapt at Infrastructure boundaries.

# Phase acceptance

## 261. Phase 0

Source model known.

## 262. Phase 1

P1 contracts sufficient.

## 263. Phase 2

Authorization semantics coherent.

## 264. Phase 3

Workspace identity + Account containment proven; archive/delete semantics + account-inactive failure policy decided (D3-A/D3-B) and D3-B enforced centrally. CLOSED.

Workspace D4-ready.

## 265. Phase 4

Membership D4+; decisions D4-A..D4-E recorded in PR-WG-02; CLOSED.

## 266. Phase 6

Resource/Action D4+.

## 267. Phase 7

Permission D4/D5-ready.

## 268. Phase 8

Built-in role D4+.

## 269. Phase 9

Authorization enforcement D5-ready.

## 270. Phase 10

WorkManagement handshake proven.

## 271. Phase 11

P3-B protected-slice gate passed; protected P3 Application/API release open.

## 272. Phases 12–16

Secondary features independently D4/D5 according to release scope.

## 273. Phases 17–22

Cross-cutting hardening complete.

## 274. Phase 23

TESTS has complete traceability input.

## 275. Phase 25

CERTIFICATION can evaluate exact evidence.

# Required discovery commands

## 276. Domain structure

```bash
find backend/src/Notrelix.Domain/Workspaces -type f | sort
find backend/src/Notrelix.Domain/Governance -type f | sort
```

## 277. Application structure

```bash
find backend/src/Notrelix.Application/Features/Workspaces -type f | sort
find backend/src/Notrelix.Application/Features/Governance -type f | sort
```

## 278. Governance semantics

```bash
rg -n \
  "PermissionRule|ResourcePermission|Policy|Permission|Role|ResourceKind|ResourceType|Action" \
  backend/src backend/tests
```

## 279. Workspace semantics

```bash
rg -n \
  "WorkspaceMember|Invitation|Team|Space|WorkspaceRule|WorkspaceId|AccountId|TenantId" \
  backend/src backend/tests
```

## 280. Authorization debt

```bash
rg -n \
  "IsAdmin|IsOwner|Role ==|Role !=|HasPermission|Authorize|Forbidden" \
  backend/src
```

## 281. Downstream resource declarations

```bash
rg -n \
  "ResourceKind|ResourceType|Action|Permission" \
  backend/src/Notrelix.Application/Features/WorkManagement \
  backend/src/Notrelix.Application/Features/Documents \
  backend/src/Notrelix.Application/Features/Billing
```

# Required certification commands

## 282. Build

Use canonical backend build, for example:

```bash
dotnet build backend/backend.slnx
```

only if still current.

## 283. Tests

Run canonical project/suite commands for:

```text
Architecture
Domain
Application
Infrastructure
API
Integration
Platform if authorization pipeline mechanism changed
```

Exact mapping belongs to TESTS.

## 284. Migration

Use canonical EF migration/check tooling.

Do not mutate production DB from this plan.

## 285. Docs

When integrated:

```bash
make docs-generate
make docs-check
```

# Definition of Done — P2 Core

## 286. Functional DoD

- Workspace stable;
- Account containment stable;
- membership baseline stable;
- resource/action contract stable;
- permission semantics stable;
- built-in role baseline;
- one effective authorization decision;
- WorkManagement handshake proven.

## 287. Architecture DoD

- Workspaces and Governance remain separate contexts;
- no new service/project;
- no private WorkManagement persistence dependency;
- Domain pure;
- pipeline-owned enforcement preserved.

## 288. Security DoD

- cross-tenant denied;
- stale membership/role revocation safe;
- privilege escalation tests;
- fail closed;
- no bearer secret leakage.

## 289. Migration DoD

- schema migration complete;
- resource/action/role/permission compatibility handled;
- clean DB;
- upgrade DB;
- no pending model changes.

## 290. Testing DoD

P2 gate scenarios mapped in TESTS and executed before certification.

## 291. Handoff DoD

WorkManagement can implement Board protected operations without asking:

```text
Which role string should I check?
Which permission table should I query?
How do I identify current Workspace?
How do I bypass Governance?
```

It only consumes stable contracts.

# Definition of Done — full team scope

## 292. Workspace secondary scope

Release-scoped:

- Invitations;
- Provisioning;
- Teams;
- Spaces;
- Settings/Home.

## 293. Governance secondary scope

Release-scoped:

- custom roles;
- policies;
- resource permissions;
- share links;
- audit/security events;
- templates.

## 294. Full-scope quality

All release-scoped secondary capabilities meet:

```text
security
migration
integration
CI
documentation
```

requirements without destabilizing P2 core.

# Final execution output

## 295. Required package

At team completion:

```text
workspace-governance.spec.md
workspace-governance.plan.md
workspace-governance.tests.md
workspace-governance.certification.md
```

## 296. Required code outcome

The final model must remain conceptually:

```text
Account
↓
Workspace
↓
Membership
        \
         → Governance subject context

Resource-owning context
→ resource category / ResourceId / resource-owned Action
                   \
                    → Governance
                       Role / Permission / Policy
                              ↓
                     Effective decision
                              ↓
                 Application pipeline enforcement
```

## 297. Required sequencing outcome

Sequencing is staged:

```text
P3-A producer gate
→ WorkManagement Domain/Data core may run in parallel

P3-B protected-slice gate
→ protected WorkManagement Application/API slice may release/certify
```

Workspace/Governance continues secondary depth in parallel without changing the frozen producer contract.

## 298. Final rule

When source structure and conceptual model differ:

```text
do not force folder symmetry
```

Instead:

```text
identify semantic owner
preserve valid existing code
migrate only conflicts
stabilize the producer contract
```

The purpose of this PLAN is to make P2 a reliable authorization backbone, not to create the most elaborate RBAC system possible.
