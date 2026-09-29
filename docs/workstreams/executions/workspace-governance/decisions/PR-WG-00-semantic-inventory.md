# PR-WG-00 - Workspace and Governance semantic inventory

## Baseline

```text
Branch: develop
HEAD: f91c203bb63e799e3d7979433d9c1d799b5fe794
Solution: backend/backend.slnx
Migration head: 20260702093805_SchemaBaseline
```

This record implements PLAN Phases 0-2. It records source facts and semantic
classification only. It does not certify P2 and does not authorize a schema or
public-contract change.

## Authority read

```text
PRODUCT.md
RULE.md
docs/product/workspaces.md
docs/product/governance.md
docs/workstreams/backend-roadmap.md
docs/workstreams/cross-team-dependencies.md
docs/workstreams/teams/identity-accounts.md
docs/workstreams/teams/workspace-governance.md
backend/docs/architecture/application-model.md
backend/docs/architecture/domain-modeling.md
backend/docs/architecture/security-tenancy-authorization.md
workspace-governance.spec.md
workspace-governance.plan.md
workspace-governance.tests.md
```

## Phase 0 source facts

### Workspaces

- `Workspace` is the Account-scoped collaboration tenant and carries immutable
  `AccountId`.
- `WorkspaceFactory.CreateWithOwner` creates the Workspace and initial owner
  membership as one Domain result for an Application transaction.
- `WorkspaceMember` is a separate Workspace-scoped aggregate with active,
  suspended and removed lifecycle.
- `(WorkspaceId, UserId)` is unique in persistence.
- Workspace, membership, invitation, Team and Space models already exist with
  Domain events and package-local tests.

### Governance

- `ResourceKind` and `ResourceRef` in SharedKernel provide stable logical
  resource identity independently from routes, tables and CLR names.
- `PermissionAction` is the current typed action catalog consumed by protected
  Application requests.
- `IRequirePermission` declares action and resource requirements.
- `AuthorizationBehavior` enforces declared permission before the handler.
- `PermissionService` is registered as the current effective decision store.
- PermissionRule, ResourcePermission, WorkspacePolicy and CustomRole are all
  persisted, but do not currently form one complete semantic hierarchy.

### Downstream handshake

- WorkManagement Board requests already declare `work-management.board`
  resources and typed actions.
- The current evaluator reads `IWorkManagementDbContext.Boards` and
  `BoardMembers` directly. This conflicts with the P2 target that Governance
  must not depend on another context's private persistence.

## Phase 1 P1 producer contract

The Identity and Accounts certification records `P1 CORE CERTIFIED` and opens
`IA-GATE-004` for P2. Current source provides:

- trusted `ICurrentUser` identity resolved outside request payloads;
- stable User and Account Guid identity;
- `ICurrentTenantContext` Account/Workspace resolution;
- Application and RLS tenant-isolation enforcement;
- Account status through an explicit read contract;
- no Workspace dependency on private Identity persistence.

Result: Phase 1 PASS. No local Actor, Account or authentication abstraction may
be introduced by P2.

## Phase 2 semantic classification

### Workspaces Rules

| Type | Classification | Action |
|---|---|---|
| WorkspaceRules | Workspace validation helper | RETAIN |
| WorkspaceMemberRules | Workspace membership invariant helper | RETAIN |
| WorkspaceOwnerRules | Last-owner business invariant | RETAIN |
| WorkspaceInvitationRules | Invitation uniqueness business rule | RETAIN |
| TeamRules | Team validation helper | RETAIN |
| TeamLeadRules | Team last-lead business invariant | RETAIN |
| SpaceRules | Space validation helper | RETAIN |

None of these types is a Governance authorization policy.

### Permission

There is no separate persisted `Permission` aggregate. The stable permission
declaration is the typed `(ResourceKind, PermissionAction)` operation consumed
by `IRequirePermission`. Effective access is a decision over that operation,
the Actor, tenant scope, membership and applicable grants/rules.

Action: RETAIN the typed declaration contract. Do not introduce a duplicate
permission catalog entity without a separate approved migration decision.

### PermissionRule

`PermissionRule` is a stored action-level authorization rule. It identifies a
scope, optional resource, subject, action, effect, priority and validity window.
It is the only persisted model currently evaluated generically by action.

Action: RETAIN as the canonical advanced/action-rule model. Conditions beyond
currently supported semantics remain secondary and must fail closed.

### Policy

`WorkspacePolicy` owns Workspace-wide guest, sharing and public-resource
configuration. It is configuration input to authorization/share workflows, not
an independent generic permission decision engine. Current production
authorization does not evaluate it.

Action: RETAIN as secondary policy configuration. Do not add it to the P2 core
evaluator until a concrete action and composition rule requires it.

### ResourcePermission

`ResourcePermission` is a direct subject-to-resource ACL expressed as a
`PermissionLevel`. It is not a replacement for action-level PermissionRule.
The current evaluator uses only existence of a non-deleted row for private
Board visibility and ignores level, effect, condition and priority.

Action: RETAIN for secondary direct ACL compatibility, classify current Board
use as legacy fallback, and do not expand ACL semantics in P2 core.

### Role

`WorkspaceRole` is the built-in Workspace participation role vocabulary.
`CustomRole` and `MemberRoleAssignment` are secondary Governance capabilities.
The P2 core requires one explicit built-in-role-to-action baseline; custom roles
must not block that baseline.

### Effective decision hierarchy

```text
ICurrentUser Actor
  -> active WorkspaceMember
  -> built-in WorkspaceRole baseline
  -> ResourceKind + ResourceRef owned by the resource context
  -> PermissionAction owned by the protected operation contract
  -> applicable PermissionRule
  -> optional secondary ResourcePermission/Policy inputs
  -> one PermissionDecision
  -> AuthorizationBehavior before handler execution
```

The resource-owning context must provide a narrow resource authorization
snapshot/port. Governance must not query its private DbContext.

## Source debt

| ID | Debt | Required closure |
|---|---|---|
| WG-DEBT-001 | PermissionService depends on `IWorkManagementDbContext` | Replace with a resource-owner authorization snapshot port |
| WG-DEBT-002 | Built-in role grants are implicit and incomplete | Define and test one typed built-in role baseline |
| WG-DEBT-003 | ResourcePermission fallback ignores most stored semantics | Keep secondary; remove from P2 core decision or evaluate explicitly later |
| WG-DEBT-004 | WorkspacePolicy is persisted but not in effective decisions | Keep secondary until concrete composition semantics are approved |
| WG-DEBT-005 | PermissionRule supports subject kinds evaluator does not handle | Unsupported kinds must fail closed and remain outside P2 core |

## Architecture/product decision required

`WG-ROLE-DEC-001` remains unresolved for Phase 8.

Current backend integration evidence expects a plain Workspace `Member` with no
Board membership to pass `ManageBoardPermission` for a Workspace-visible Board.
The frontend centralized permission matrix grants Board management operations
to `Owner` and `Admin`, while `Member` receives item/view collaboration
operations. Product authority states that Workspace role is an authorization
input and must not automatically grant every action on every resource.

These contracts cannot all be treated as authoritative simultaneously:

1. preserve the current backend broad Workspace-visible Board grant;
2. align the backend built-in role baseline with the narrower centralized
   permission matrix;
3. require an explicit Board role/grant for management actions.

Recommendation: option 3 for resource management, with option 2 as the
Workspace-level default mapping. It is least-privilege and best matches the
product rule that resource policy remains distinct from Workspace membership.
This requires an explicit product/contract decision because it changes current
backend behavior and tests.

## Migration impact

No Phase 0-2 schema change is authorized. Existing resource-kind strings,
action enum values and persisted Governance rows must be inventoried before any
rename or removal. P2 core should prefer compatible Application contracts and
tests before considering migration.

## Decision and next work unit

Phases 0, 1 and 2 PASS for starting P2 core implementation.

Proceed in PLAN order:

1. verify/close Workspace core;
2. verify/close membership core;
3. stabilize Resource/Action and resource-owner snapshot contract;
4. define built-in role baseline and effective evaluator;
5. prove Board allow, deny and cross-tenant paths through production DI.

The semantic stop conditions are resolved for P2 core only. Advanced custom
roles, Policy composition, ResourcePermission inheritance and ShareLinks remain
secondary and are not implicitly decided by this record.

---

# Current-SHA amendment metadata

This amendment is appended to the historical record above. It does not alter what the original
`f91c203bb63e799e3d7979433d9c1d799b5fe794` record said.

Current documentation/source audit baseline:

```text
branch: develop
SHA: 35702d0fa9fb01ed68b0667bab500030d60bd028
tree: 271f825cb27ca06c0a7931804b70515fe3e2bae1
```

This SHA is an audit baseline, not automatically the final certification candidate.

Current synchronized execution package:

```text
workspace-governance.spec.v2.md
workspace-governance.plan.v2.md
workspace-governance.tests.v2.md
workspace-governance.certification.v2.md
```

Current runtime authority:

```text
RequestDescriptorRegistry
+ ExecutionContextSnapshot
+ IAccessFactsProvider
      ↓
AccessFacts
      ↓
IAccessPolicyEvaluator
      ↓
AccessPolicyEngine
      ↓
AccessDecision
      ↓
AccessControlBehavior
      ↓
handler only when allowed
```

Approved exception:

```text
ISystemInternalRequest
→ ApplicationPrincipalKind.System
→ frozen System-internal authorization contract
```

This exception is explicit and trusted. Ordinary background/non-HTTP execution is not automatically System.

## Amendment traceability index — semantic decisions

| Decision | Requirements |
|---|---|
| `WG-DEC-TOPO-001` | WGREQ183 |
| `WG-DEC-AUTHZ-001` | WGREQ080–WGREQ092, WGREQ185 |
| `WG-DEC-SYSTEM-001` | WGREQ091, WGREQ126, WGREQ159, WGREQ185 |
| `WG-DEC-FACTS-001` | WGREQ083–WGREQ086, WGREQ186 |
| `WG-DEC-FACTS-002` | WGREQ093–WGREQ096, WGREQ118–WGREQ122, WGREQ133, WGREQ186 |
| `WG-DEC-PERSIST-001` | WGREQ129–WGREQ133, WGREQ184 |
| `WG-DEC-RLS-001` | WGREQ021, WGREQ152–WGREQ154, WGREQ187 |
| `WG-DEC-ACTION-001` | WGREQ050–WGREQ053, WGREQ179 |
| `WG-DEC-PRULE-001` | WGREQ060–WGREQ062, WGREQ180, WGREQ188 |
| `WG-DEC-PRULE-002` | WGREQ057, WGREQ060–WGREQ061, WGREQ084, WGREQ180 |
| `WG-DEC-POLICY-001` | WGREQ071–WGREQ075, WGREQ188 |
| `WG-DEC-RPERM-001` | WGREQ076–WGREQ079, WGREQ154, WGREQ188 |
| `WG-DEC-RPERM-002` | WGREQ076–WGREQ079, WGREQ154 |
| `WG-DEC-ROLE-001` | WGREQ063–WGREQ067, WGREQ181 |
| `WG-ROLE-DEC-001` | WGREQ064, WGREQ067, WGREQ118–WGREQ121, WGREQ181, WGREQ190 |
| `WG-DEC-CROLE-001` | WGREQ068–WGREQ070, WGREQ188 |
| `WG-DEC-SHARE-001` | WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188 |
| `WG-DEC-TPL-001` | WGREQ111–WGREQ113, WGREQ188 |

## Amendment traceability index — debt

| Debt | Requirements |
|---|---|
| `WG-DEBT-001` | WGREQ093–WGREQ096, WGREQ121, WGREQ133, WGREQ186 |
| `WG-DEBT-002` | WGREQ063–WGREQ067, WGREQ181 |
| `WG-DEBT-003` | WGREQ076–WGREQ079, WGREQ154, WGREQ188 |
| `WG-DEBT-004` | WGREQ071–WGREQ075, WGREQ188 |
| `WG-DEBT-005` | WGREQ060–WGREQ062, WGREQ188 |
| `WG-DEBT-006` | WGREQ093–WGREQ096, WGREQ121, WGREQ133, WGREQ186 |
| `WG-DEBT-007` | WGREQ050–WGREQ053, WGREQ118–WGREQ121, WGREQ181, WGREQ190 |
| `WG-DEBT-008` | WGREQ021, WGREQ153, WGREQ187 |
| `WG-DEBT-009` | WGREQ019, WGREQ145 |
| `WG-DEBT-010` | WGREQ014, WGREQ144 |
| `WG-DEBT-011` | WGREQ071–WGREQ075, WGREQ188 |
| `WG-DEBT-012` | WGREQ068–WGREQ070, WGREQ188 |
| `WG-DEBT-013` | WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188 |
| `WG-DEBT-014` | WGREQ111–WGREQ113, WGREQ188 |
| `WG-DEBT-015` | WGREQ183, WGREQ185, WGREQ189 |

# Current-source amendment

## 6. Application topology amendment

**Requirements:** `WGREQ183`

Historical docs were written before the current Application topology was fully stabilized.

Current source authority is module-first:

```text
backend/src/Notrelix.Application/Features/Workspaces
backend/src/Notrelix.Application/Features/Governance
```

Current decision:

```text
WG-DEC-TOPO-001
Status: RESOLVED
Decision: RETAIN current module-first topology.
```

Rules:

```text
new/touched Workspace use cases
→ Features/Workspaces

new/touched Governance use cases
→ Features/Governance
```

Do not reintroduce deprecated parallel feature roots solely to match old docs.

## 7. Canonical authorization-runtime amendment

**Requirements:** `WGREQ080–WGREQ092, WGREQ185`

The historical record stated:

```text
AuthorizationBehavior
→ PermissionService / decision store
```

That is no longer the current runtime authority.

Current production path is:

```text
RequestDescriptorRegistry
+
ExecutionContextSnapshot
+
IAccessFactsProvider
        ↓
AccessFacts
        ↓
IAccessPolicyEvaluator
        ↓
AccessPolicyEngine
        ↓
AccessDecision
        ↓
AccessControlBehavior
        ↓
protected handler
```

Current pipeline order is registered as:

```text
ExceptionMappingBehavior
→ ApplicationTracingBehavior
→ RequestContractBehavior
→ ExecutionContextBehavior
→ DataSessionBehavior
→ AccessControlBehavior
→ IdempotencyBehavior
→ handler
```

Decision:

```text
WG-DEC-AUTHZ-001
Status: RESOLVED
Classification: RETAIN + HARDEN
```

`AccessControlBehavior` is the single canonical Application enforcement seam.

`AccessPolicyEngine` is the canonical Governance-owned evaluator through the neutral `IAccessPolicyEvaluator` seam.

`Application/Common` owns generic request/security mechanics.

Governance owns permission semantics.

No second evaluator/behavior/endpoint permission engine may be introduced.

### 7.1 Approved `ISystemInternalRequest` exception

**Requirements:** `WGREQ091, WGREQ126, WGREQ159, WGREQ185`

Current source contains an intentional trusted-system contract.

Frozen characterization:

```text
AccessPolicyEngineCharacterizationTests
SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler
```

Current policy path allows:

```text
descriptor.Principal == ApplicationPrincipalKind.System
→ AccessDecision.Allow()
```

for an explicitly classified System request.

This does **not** mean:

```text
background job
no HTTP caller
message consumer
```

automatically receives global System authority.

The accepted distinction is:

```text
ordinary background execution
→ reconstruct normal trusted execution/security context
→ no implicit System bypass

explicit ISystemInternalRequest
→ approved special contract
→ may bypass user permission evaluation
→ still requires trusted runtime/event scope and applicable data-session/RLS safeguards
```

Representative approved flow includes `ProvisionPersonalWorkspaceCommand` as an internal system operation.

Decision:

```text
WG-DEC-SYSTEM-001
Status: RESOLVED CURRENT CONTRACT
Classification: RETAIN + VERIFY
Requirements: WGREQ091, WGREQ126, WGREQ159, WGREQ185
```

STOP if:

- an external/user request can opt into `ApplicationPrincipalKind.System`;
- absence of a user automatically implies System;
- trusted Account/Workspace scope is sourced from untrusted payload data;
- the special contract is removed accidentally while "normalizing" authorization.

## 8. AccessFacts provider amendment

**Requirements:** `WGREQ083–WGREQ086, WGREQ186`

Current Infrastructure implementation:

```text
PostgresAccessFactsProvider
```

executes:

```text
AccessFactsQuery.Sql
```

using the active request data-session connection and transaction.

Decision:

```text
WG-DEC-FACTS-001
Status: ACCEPTED CURRENT MECHANISM
Classification: RETAIN + HARDEN
```

Mandatory constraint:

```text
facts provider returns neutral facts;
policy evaluator decides allow/deny.
```

Forbidden:

```text
facts adapter decides business authorization
```

## 9. Facts ownership amendment

**Requirements:** `WGREQ093–WGREQ096, WGREQ118–WGREQ125, WGREQ133, WGREQ186`

The current facts provider is not semantically uniform.

It combines several source mechanisms.

### 9.1 Documents

Current source uses:

```text
IPageAuthorizationFacts
```

for Page lifecycle/visibility.

Classification:

```text
producer-owned semantic facts
Status: ACCEPTED
```

### 9.2 Billing subscription requirement

Current source uses:

```text
IBillingSubscriptionFacts
```

for active-subscription/minimum-tier satisfaction.

Classification:

```text
producer-owned commercial decision
Status: ACCEPTED
```

Governance consumes:

```text
SubscriptionRequirementSatisfied: bool
```

and does not order Billing subscription tiers.

### 9.3 WorkManagement Board facts

`AccessFactsQuery` still reads:

```text
work.boards
work.board_members
```

for Board existence, audience and resource membership.

This is materially different from the historical debt:

```text
PermissionService directly injecting IWorkManagementDbContext
```

That historical dependency is gone.

However:

```text
foreign WorkManagement persistence read
```

still exists inside the shared authorization composite read.

Current classification:

```text
WG-DEC-FACTS-002
Status: PARTIALLY_RESOLVED
Old direct Application DbContext dependency: CLOSED
Shared SQL foreign-table read: REMAINS / REQUIRES EXPLICIT ARCHITECTURE CLASSIFICATION
```

Do not state:

```text
WorkManagement facts have been fully producerized
```

until the candidate architecture/evidence explicitly proves that.

Do not expand the SQL with new WorkManagement tables as an unreviewed convenience.

### 9.4 Identity / Accounts / Workspaces

The SQL also reads current actor/account/workspace membership state directly from owned schemas.

These reads are supporting authorization composite-read mechanics.

They must not be reinterpreted as:

```text
Governance owns Identity
Governance owns Accounts
Governance owns Workspace membership
```

Semantic ownership remains with the producer contexts.

### 9.5 Billing feature capacity

`AccessFactsQuery` currently also reads Billing entitlement/usage data for feature-gate facts, while the subscription requirement has been normalized through a Billing-owned public seam.

Current classification:

```text
feature-capacity shared SQL path:
  CURRENT SUPPORTING MECHANISM
  not authorization semantic ownership

subscription/tier decision:
  Billing-owned Public seam
```

Future cleanup must follow the architecture closure authority and cannot be improvised inside P2.

## 10. Owner-local persistence abstraction amendment

**Requirements:** `WGREQ129–WGREQ133, WGREQ184`

Current Application contracts include:

```text
IWorkspaceDbContext
IGovernanceDbContext
```

Decision:

```text
WG-DEC-PERSIST-001
Status: RESOLVED
Classification: OWNER-LOCAL APPLICATION PERSISTENCE ABSTRACTION
```

They are allowed under the current documented Application EF exception.

They are **not**:

```text
cross-context Public contracts
shared domain services
general authorization fact APIs
```

A foreign context injecting either interface to read private Workspace/Governance state is a boundary violation unless a specific architecture exception explicitly authorizes it.

## 11. Access-grant / RLS projection amendment

**Requirements:** `WGREQ021, WGREQ152–WGREQ154, WGREQ187`

Current source includes:

```text
IWorkspaceGrantProjectionService
WorkspaceGrantProjectionServiceAdapter
AccessGrantProjectionService
authz.access_grants
```

The Workspaces-owned seam synchronizes Workspace membership changes with the RLS authorization projection.

Decision:

```text
WG-DEC-RLS-001
Status: ACCEPTED CURRENT DESIGN
Classification: RETAIN + VERIFY
```

Semantic ownership remains:

```text
Workspace membership
→ Workspaces

authz.access_grants projection / RLS enforcement
→ Infrastructure security mechanism
```

The projection must not become a second source of business membership truth.

Current verification debt remains:

```text
membership create → grant
membership role/state change → sync/revoke
suspend/remove → stale access cannot remain
```

Certification must execute these paths before D5.

# Current semantic classification

## 12. PermissionAction

**Requirements:** `WGREQ050–WGREQ053, WGREQ179`

Current enum includes business actions such as:

```text
CreateWorkspace
ViewWorkspace
ManageWorkspace
ArchiveWorkspace
RestoreWorkspace
ViewMembers
InviteMember
ChangeMemberRole
RemoveMember
ManageWorkspaceSettings
ManageSpaces
ManageTeams
DeleteWorkspace

ViewBoard
CreateBoard
ManageBoard
ManageBoardPermission
CreateBoardView
UpdateBoardView
ShareBoardView
CreateField
UpdateField
DeleteField
CreateItem
UpdateItem
MoveItem
AssignItem

ViewPage
CreatePage
UpdatePage
DeletePage
ArchivePage
SharePage
ManagePagePermission
CreateComment

ManageAccount
ManageIntegrations
```

Decision:

```text
WG-DEC-ACTION-001
Status: RESOLVED FOR CURRENT CATALOG
Classification: RETAIN
```

Rules:

- action names are business-semantic;
- owning product context defines when the action is requested;
- Governance decides whether the principal may perform it;
- persisted/public identifiers must not be renamed casually;
- no new generic `Execute`, `Write`, `UpdateHttpPut` action vocabulary.

## 13. PermissionRule

**Requirements:** `WGREQ060–WGREQ062, WGREQ180, WGREQ188`

Current Domain model supports richer subject vocabulary than the current SQL/evaluator path actually consumes.

`PermissionSubjectType` includes:

```text
User
WorkspaceRole
Team
PublicLink
ExternalEmail
```

Current `AccessFactsQuery` effective rule fetch is restricted to:

```text
subject_type = 'User'
subject_id = current user
```

Current evaluator uses:

```text
Priority
Effect
```

from the projected rule facts.

Decision:

```text
WG-DEC-PRULE-001
Status: RESOLVED FOR P2 CORE SUBSET
Classification: RETAIN USER-SUBJECT ACTION RULES
```

Current supported P2 core subset:

```text
active rule
User subject
workspace/resource scope
action
priority
effect
starts/expires filtering
```

Current non-core semantics:

```text
WorkspaceRole subject
Team subject
PublicLink subject
ExternalEmail subject
arbitrary ConditionJson semantics
```

must not be advertised as active evaluator support until implemented and tested.

Unknown/unsupported semantics must fail closed or remain outside released scope.

## 14. PermissionRule precedence

**Requirements:** `WGREQ057, WGREQ060–WGREQ061, WGREQ084, WGREQ180`

Current source evaluates only the minimum numeric priority present in `AccessFacts.PermissionRules`.

Within that winning priority:

```text
Deny
```

takes precedence over:

```text
Allow
```

Decision:

```text
WG-DEC-PRULE-002
Status: ACCEPTED CURRENT POLICY
Classification: RETAIN + VERIFY
```

Certification must prove:

- time filtering occurs before effective selection;
- same-priority Deny dominates Allow;
- unsupported rules do not accidentally broaden access.

## 15. WorkspacePolicy

**Requirements:** `WGREQ071–WGREQ075, WGREQ188`

Current Domain contains:

```text
WorkspacePolicy
GuestAccessPolicy
ResourcePolicy
SharingPolicy
```

Current production authorization path does not use WorkspacePolicy as the generic decision engine.

Decision:

```text
WG-DEC-POLICY-001
Status: RESOLVED FOR P2 CORE
Classification: SECONDARY / DOMAIN-PRESENT
```

Do not add WorkspacePolicy into `AccessPolicyEngine` until a concrete product action and composition precedence are defined.

Do not claim:

```text
WorkspacePolicy enforcement VERIFIED
```

from Domain/persistence presence alone.

## 16. ResourcePermission

**Requirements:** `WGREQ076–WGREQ079, WGREQ154, WGREQ188`

Current Domain supports direct subject-to-resource ACL through:

```text
ResourcePermission
PermissionLevel
```

Current levels are technically ranked in Governance policy as:

```text
None      0
Viewer    1
Commenter 2
Editor    3
Manager   4
Owner     5
```

Current runtime facts now include more than the historical boolean fallback:

```text
HasExplicitResourcePermission
ActiveResourcePermissionRank
TargetPermissionRank
```

Current `AccessPolicyEngine` enforces:

- ACL-management threshold;
- grant ceiling;
- revoke ceiling;
- exact target rank comparisons.

Decision:

```text
WG-DEC-RPERM-001
Status: HISTORICAL DEBT PARTIALLY CLOSED
Classification: RETAIN + HARDEN
```

The historical statement that current runtime “ignores permission level” is no longer generally true.

However, not every Domain lifecycle operation is necessarily delivered through Application/API.

Full lifecycle remains layer-specific and must be certified honestly.

## 17. ResourcePermission management authority

**Requirements:** `WGREQ076–WGREQ079, WGREQ154`

Current policy explicitly distinguishes:

```text
resource visibility/use
```

from:

```text
ACL management
```

For:

```text
ManageBoardPermission
ManagePagePermission
```

a non-owner requires active resource permission rank of at least:

```text
Manager
```

Decision:

```text
WG-DEC-RPERM-002
Status: RESOLVED
Classification: RETAIN
```

This closes the historical class of:

```text
Workspace-visible resource
→ ordinary member
→ automatically manages ACL
```

for these dedicated ACL-management actions.

Grant/revoke is additionally bounded by the actor’s effective permission rank.

## 18. Built-in WorkspaceRole

**Requirements:** `WGREQ063–WGREQ067, WGREQ181`

Current built-in Workspace role vocabulary remains:

```text
Guest
Member
Admin
Owner
```

Decision:

```text
WG-DEC-ROLE-001
Status: RESOLVED FOR ROLE IDENTITY
Classification: RETAIN
```

Roles are facts.

They are not the entire authorization architecture.

## 19. Historical WG-ROLE-DEC-001 amendment

**Requirements:** `WGREQ064, WGREQ067, WGREQ118–WGREQ121, WGREQ181, WGREQ190`

The historical record identified ambiguity around:

```text
Workspace Member
+
Workspace-visible Board
+
Board management
```

Current source is more precise than the historical state.

### Confirmed current behavior

For a Workspace-visible Board:

```text
ViewBoard
```

may be allowed to a Workspace `Member` without:

```text
board_members row
resource_permission row
```

The current integration test:

```text
WorkspaceMember_OnWorkspaceVisibleBoard_WithoutMembershipOrAcl_Allows
```

uses:

```text
ListBoardRelationsQuery
PermissionAction.ViewBoard
```

Therefore that test is now a visibility/read baseline, not proof that the member may manage Board ACL.

### ACL management

For:

```text
ManageBoardPermission
```

ordinary Workspace visibility is insufficient.

Non-owner requires:

```text
ActiveResourcePermissionRank >= Manager
```

### Remaining ambiguity

The generic:

```text
PermissionAction.ManageBoard
```

is still used by many WorkManagement mutation requests.

The current Board branch ultimately allows `ManageBoard` for a non-Guest Workspace member on a Workspace-visible Board unless:

```text
resource is restricted
explicit deny applies
resource lifecycle denies
another more specific policy branch applies
```

Therefore the historical product question is **not fully closed**.

Current decision:

```text
WG-ROLE-DEC-001
Status: PARTIALLY_RESOLVED
```

Resolved:

```text
ViewBoard baseline
ManageBoardPermission ACL management ceiling
```

Still requiring product/WorkManagement contract clarity:

```text
which ManageBoard mutations are Workspace-member usage rights
vs
which require explicit Board role/resource permission/stronger Workspace role
```

Do not widen or narrow `ManageBoard` globally inside P2 without a WorkManagement action matrix.

P3 WorkManagement execution must inventory the concrete commands that currently reuse `ManageBoard`.

## 20. CustomRole

**Requirements:** `WGREQ068–WGREQ070, WGREQ188`

Current Domain/persistence contains:

```text
CustomRole
CustomRolePermission
MemberRoleAssignment
```

Current P2 core policy does not require these for the built-in role baseline.

Decision:

```text
WG-DEC-CROLE-001
Status: DEFERRED FROM P2 CORE
Classification: DOMAIN/PERSISTENCE GROUNDWORK UNTIL RUNTIME DELIVERY PROVEN
```

Before CustomRole participates in effective authorization, define:

```text
assignment validity
built-in vs custom composition
deny/allow precedence
permission-action mapping
archive/delete effect
cache/projection behavior
concurrency
API administration
```

## 21. ShareLink

**Requirements:** `WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188`

Current Domain contains a ShareLink lifecycle with token hashing, expiry/disable and token rotation semantics.

Current Application/API delivery is narrower than the full Domain lifecycle.

Decision:

```text
WG-DEC-SHARE-001
Status: SECONDARY
Classification: RETAIN + COMPLETE ONLY TO RELEASED SCOPE
```

A Domain ShareLink aggregate does not prove an end-to-end public capability-token authorization path.

Any anonymous/public token consumption requires dedicated security certification.

## 22. PermissionTemplate

**Requirements:** `WGREQ111–WGREQ113, WGREQ188`

Current Domain has substantial PermissionTemplate semantics and tests.

Decision:

```text
WG-DEC-TPL-001
Status: SECONDARY
Classification: DOMAIN-STRONG / DELIVERY TO BE VERIFIED
```

Do not create an “apply template” workflow merely because the Domain model exists.

If promoted into release scope, define:

```text
target resource scope
permission/action compatibility
atomic apply semantics
partial failure
versioning
audit
```

first.

# Current effective authorization hierarchy

## 23. Canonical hierarchy

**Requirements:** `WGREQ080–WGREQ096, WGREQ185–WGREQ187`

Current source should be understood as:

```text
request security descriptor
        ↓
trusted execution context
  UserId / AccountId / WorkspaceId / ResourceRef
        ↓
AccessFacts
  user/account/workspace existence
  account/workspace membership role
  resource existence/audience
  resource member role
  explicit resource permission
  permission rules
  entitlement/feature facts
  target permission ranks
        ↓
AccessPolicyEngine
        ↓
AccessDecision
        ↓
AccessControlBehavior
        ↓
handler only when Allowed
```

This replaces the historical hierarchy ending in:

```text
PermissionDecision
→ AuthorizationBehavior
```

## 24. Decision ordering

**Requirements:** `WGREQ080–WGREQ092, WGREQ185`

Current effective ordering is conceptually:

```text
principal classification
→ required trusted scope
→ membership/role availability
→ resource lifecycle/existence
→ Owner fast-path
→ winning PermissionRule priority
→ explicit Deny / Allow
→ account/workspace/resource-specific policy
→ resource-management ceiling
→ grant/revoke ceiling
→ commercial/feature gates
→ final AccessDecision
```

Important:

`Owner` fast-path occurs after resource existence/lifecycle check for resource-scoped requests.

Therefore even an Owner does not make a missing/archived/deleted resource exist.

## 25. Facts vs policy decision

**Requirements:** `WGREQ083, WGREQ094, WGREQ186`

Current accepted boundary:

```text
facts:
  User exists?
  Account exists?
  Workspace exists?
  membership role?
  resource exists?
  audience?
  resource member role?
  ACL present/rank?
  active action rules?
  commercial requirement satisfied?

policy:
  does this role/rule/rank/resource/action combination authorize the request?
```

`AccessFacts` must not contain a precomputed generic:

```text
Allowed = true
```

from a producer adapter unless the producer fact is itself a domain-owned commercial/semantic decision such as Billing requirement satisfaction.

# Historical source-debt transition

## 26. WG-DEBT-001 — PermissionService → IWorkManagementDbContext

**Requirements:** `WGREQ093–WGREQ096, WGREQ121, WGREQ133, WGREQ186`

Historical debt:

```text
PermissionService directly depends on IWorkManagementDbContext
```

Current status:

```text
SUPERSEDED / PARTIALLY CLOSED
```

Closed:

```text
PermissionService no longer current authority
AccessPolicyEngine is persistence-free
no Governance Application evaluator injection of IWorkManagementDbContext
```

Remaining:

```text
AccessFactsQuery still directly reads work.boards/work.board_members
```

New tracking:

```text
WG-DEBT-006
```

## 27. WG-DEBT-002 — implicit built-in role grants

**Requirements:** `WGREQ063–WGREQ067, WGREQ181`

Historical debt:

```text
built-in role grants are implicit/incomplete
```

Current status:

```text
PARTIALLY CLOSED
```

Improvements:

- explicit `AccessPolicyEngine`;
- explicit account/workspace role branches;
- explicit Board/Page/Integrations action branches;
- explicit ACL-management rank ceiling;
- tests around representative flows.

Remaining:

- generic `ManageBoard` is reused across a large command surface;
- exact WorkManagement action matrix is not yet fully classified.

Track under:

```text
WG-DEBT-007
```

## 28. WG-DEBT-003 — ResourcePermission fallback ignores stored semantics

**Requirements:** `WGREQ076–WGREQ079, WGREQ154, WGREQ188`

Historical debt:

```text
ResourcePermission used mainly as existence fallback
```

Current status:

```text
PARTIALLY CLOSED
```

Current runtime now consumes:

```text
active permission rank
target permission rank
explicit permission existence
```

and applies grant/revoke management ceilings.

Remaining:

- inheritance semantics are not proven solely by cache model presence;
- full lifecycle across Domain/Application/API remains uneven;
- subject kinds beyond User require explicit support classification.

## 29. WG-DEBT-004 — WorkspacePolicy not in effective decisions

**Requirements:** `WGREQ071–WGREQ075, WGREQ188`

Historical debt remains:

```text
OPEN / NON-BLOCKING FOR P2 CORE
```

Current action:

```text
retain as secondary policy configuration
do not silently integrate
```

## 30. WG-DEBT-005 — unsupported PermissionRule subject kinds

**Requirements:** `WGREQ060–WGREQ062, WGREQ188`

Historical debt remains but is more precisely bounded.

Current effective SQL supports:

```text
User
```

only.

Domain enum includes:

```text
WorkspaceRole
Team
PublicLink
ExternalEmail
```

Current status:

```text
OPEN / NON-BLOCKING FOR P2 CORE
```

Required behavior:

```text
unsupported subject kind
→ not treated as effective rule
→ never broaden access
```

# Current source-debt register

## 31. WG-DEBT-006 — shared authorization SQL reads foreign WorkManagement persistence

**Requirements:** `WGREQ093–WGREQ096, WGREQ121, WGREQ133, WGREQ186`

Current source:

```text
AccessFactsQuery.Sql
→ work.boards
→ work.board_members
```

Classification:

```text
supporting composite-read debt / architecture-classification required
```

This is not the historical direct Application `IWorkManagementDbContext` violation.

It still means P2 must not claim a fully producer-owned WorkManagement facts boundary without explicit architecture evidence.

Required closure options must follow existing architecture authority, for example:

```text
accepted composite-read classification
or
producer-owned facts seam
or
approved projection/cache
```

Do not invent a network boundary.

## 32. WG-DEBT-007 — generic ManageBoard action is semantically overloaded

**Requirements:** `WGREQ050–WGREQ053, WGREQ118–WGREQ121, WGREQ181, WGREQ190`

Current `ManageBoard` is used by many WorkManagement mutations across:

```text
Boards
BoardGroups
Labels
Forms
SavedFilters/Views
Relations
Templates
Documents legacy-like usages
```

Risk:

```text
one broad action
→ Workspace-visible Board baseline
→ may authorize more mutation families than intended
```

Required closure:

```text
WorkManagement execution inventories every ManageBoard request
→ classifies usage right vs management right
→ either retains with explicit matrix
   or introduces narrower additive actions with compatibility plan
```

P2 must not unilaterally solve this by changing all WorkManagement permissions.

## 33. WG-DEBT-008 — access-grant revocation proof incomplete in documentation audit

**Requirements:** `WGREQ021, WGREQ153, WGREQ187`

Source includes:

```text
IWorkspaceGrantProjectionService
SyncWorkspaceMemberGrantAsync
RevokeWorkspaceMemberGrantAsync
```

Preparation tests prove membership-creation grant behavior.

Explicit exact candidate evidence for:

```text
suspend
remove
role downgrade
```

must be confirmed before D5.

## 34. WG-DEBT-009 — concurrent last-owner persistence race evidence

**Requirements:** `WGREQ019, WGREQ145`

Domain rules reject actions on the last owner.

That does not by itself prove two concurrent transactions cannot jointly leave zero owners.

Required:

```text
PostgreSQL/application concurrency evidence
```

before strong D5 owner-safety certification.

## 35. WG-DEBT-010 — duplicate active membership race evidence

**Requirements:** `WGREQ014, WGREQ144`

Source/model indicates membership uniqueness.

Certification still requires exact database-level evidence for the relevant unique constraint/conflict path.

## 36. WG-DEBT-011 — WorkspacePolicy delivery depth

**Requirements:** `WGREQ071–WGREQ075, WGREQ188`

Current source is rich in Domain/persistence.

Runtime authorization use remains unproven.

Do not mark full Governance policy complete.

## 37. WG-DEBT-012 — CustomRole delivery depth

**Requirements:** `WGREQ068–WGREQ070, WGREQ188`

Current Domain/persistence/test source exists.

Application/API/effective-decision composition must be independently proven.

## 38. WG-DEBT-013 — ShareLink end-to-end consumption

**Requirements:** `WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188`

Current Domain lifecycle exists.

End-to-end capability-token use/revocation/expiry must be proven before public ShareLink authorization is certified.

## 39. WG-DEBT-014 — PermissionTemplate application

**Requirements:** `WGREQ111–WGREQ113, WGREQ188`

Current Domain model exists.

No apply workflow should be inferred without actual source evidence and product semantics.

## 40. WG-DEBT-015 — legacy authorization terminology remains in docs/source comments

**Requirements:** `WGREQ183, WGREQ185, WGREQ189`

Current runtime authority is:

```text
ADR-006
→ seven-behavior pipeline

ADR-007
→ Governance-owned AccessPolicyEngine
→ neutral IAccessPolicyEvaluator seam

production DI
→ AccessControlBehavior
→ AccessPolicyEngine
```

Legacy names remain in several historical or stale explanatory surfaces.

### Architecture documentation

`backend/docs/architecture/security-tenancy-authorization.md` still contains a historical/current-source-evidence list with names such as:

```text
IPermissionEvaluator
IPermissionService
IWorkspacePermissionService
IAuthorizationDecisionStore
PermissionContext
PermissionDecision
```

The same document later correctly states:

```text
single canonical evaluator = AccessPolicyEngine
neutral seam = IAccessPolicyEvaluator
Common pipeline = AccessControlBehavior
```

`ADR-001` is explicitly `Superseded` by ADR-006, but its normalized body still contains present-tense
"current evidence" describing the old nineteen-behavior pipeline and `AuthorizationBehavior`.

### Source/test comments

Current source/test search also finds stale explanatory comments, including:

```text
backend/src/Notrelix.Domain/Governance/Permissions/PermissionRules.cs
  "The Application layer hosts the PermissionService..."

backend/tests/Notrelix.Integration.Tests/Auth/ApiTokenHttpFlowTests.cs
  "PermissionService over the production graph"
```

These are comments/evidence text, not current runtime dependencies.

Decision:

```text
ADR-006 / ADR-007 / production DI / executable architecture tests
= current authority

historical or stale AuthorizationBehavior / PermissionService wording
= documentation/source-comment debt only
```

Required closure:

- remove or historical-label misleading present-tense architecture wording;
- update stale source/test comments to current `AccessControlBehavior` / `AccessPolicyEngine` terminology;
- retain forbidden-name architecture tests that intentionally mention legacy names as negative guards;
- do not use documentation cleanup as authorization for runtime redesign.

Certification relation:

```text
WG-DEBT-015
→ WG-DEBT-CERT-001
→ CERT-DOC-001
```

This debt does not block the current runtime architecture, but it must never be cited as evidence that the
legacy services are still production authority.

# P1 producer contract amendment

## 41. Upstream Identity & Accounts

**Requirements:** `WGREQ114–WGREQ117, WGREQ190`

Historical PR-WG-00 recorded P1 as certified and available to P2.

That upstream decision remains a prerequisite.

Current P2 must consume trusted identity/account scope through current execution/public contracts.

Do not introduce:

```text
P2 credential ownership
P2 session ownership
client-supplied trusted Account/Workspace identity
duplicate Actor/User model
```

At final P2 certification, re-check P1 invalidation rules against the candidate SHA.

# Workspace current-state classification

## 42. Workspace aggregate

**Requirements:** `WGREQ001–WGREQ011, WGREQ178, WGREQ188`

Current classification:

```text
DOMAIN_PRESENT
APPLICATION_PRESENT
API_PRESENT
TEST_PRESENT
```

Execution classification:

```text
RETAIN + VERIFY
```

Do not rewrite the aggregate merely to satisfy the new docs.

## 43. WorkspaceMember

**Requirements:** `WGREQ012–WGREQ023, WGREQ144–WGREQ145, WGREQ178, WGREQ187`

Current classification:

```text
DOMAIN_PRESENT
APPLICATION_PRESENT
API_PRESENT
TEST_PRESENT
RLS-PROJECTION-INTEGRATED
```

Execution classification:

```text
RETAIN + HARDEN
```

Primary hardening evidence:

```text
duplicate membership race
last-owner race
grant revoke/suspend synchronization
```

## 44. WorkspaceInvitation

**Requirements:** `WGREQ024–WGREQ033, WGREQ146, WGREQ188`

Current classification:

```text
DOMAIN_PRESENT
APPLICATION_PRESENT
API_PRESENT
TEST_PRESENT
TRANSACTION EVIDENCE PRESENT
```

Execution classification:

```text
RETAIN + HARDEN
```

Focus:

```text
race
secret/log safety
delivery retry
```

## 45. Team

**Requirements:** `WGREQ034–WGREQ037, WGREQ188`

Current classification:

```text
DOMAIN_PRESENT
APPLICATION_PRESENT
API_PRESENT
TEST_PRESENT
```

Execution classification:

```text
RETAIN + VERIFY SECONDARY
```

## 46. Space

**Requirements:** `WGREQ038–WGREQ041, WGREQ188`

Current classification:

```text
DOMAIN_PRESENT
APPLICATION_PRESENT
API_PRESENT
TEST_PRESENT
```

Execution classification:

```text
RETAIN + VERIFY SECONDARY
```

`SpaceVisibility` is a fact.

It is not automatically action authorization.

## 47. Workspace Settings

**Requirements:** `WGREQ010, WGREQ188`

Current classification:

```text
DOMAIN STATE PRESENT
APPLICATION PRESENT
API PRESENT
TEST PRESENT
```

Execution classification:

```text
RETAIN + VERIFY OWNERSHIP
```

Do not allow generic Workspace settings to mutate Governance policy implicitly.

## 48. WorkspaceHome

**Requirements:** `WGREQ011, WGREQ188`

Current Application read surface exists.

Current classification:

```text
APPLICATION PRESENT
DELIVERY DEPTH TO VERIFY
```

Execution classification:

```text
CLASSIFY + VERIFY
```

Do not treat composition reads as foreign state ownership.

# Governance current-state classification

## 49. AccessPolicyEngine

**Requirements:** `WGREQ080–WGREQ092, WGREQ185`

Current classification:

```text
APPLICATION PRESENT
PRODUCTION REGISTERED
ARCHITECTURE TESTS PRESENT
```

Execution classification:

```text
RETAIN + HARDEN
```

## 50. ResourcePermission

**Requirements:** `WGREQ076–WGREQ079, WGREQ147, WGREQ154, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
APPLICATION PRESENT
API PRESENT
TEST PRESENT
INTEGRATION EVIDENCE PRESENT
```

Execution classification:

```text
RETAIN + HARDEN
```

Full lifecycle depth must be certified by layer.

## 51. PermissionRule

**Requirements:** `WGREQ060–WGREQ062, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
PERSISTENCE PRESENT
RUNTIME USER-SUBJECT CONSUMPTION PRESENT
TEST PRESENT
ADMIN DELIVERY DEPTH TO VERIFY
```

Execution classification:

```text
RETAIN CORE SUBSET + CLASSIFY SECONDARY SUBJECTS
```

## 52. WorkspacePolicy

**Requirements:** `WGREQ071–WGREQ075, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
PERSISTENCE PRESENT
TEST PRESENT
RUNTIME POLICY COMPOSITION NOT PROVEN
```

Execution classification:

```text
SECONDARY
```

## 53. CustomRole

**Requirements:** `WGREQ068–WGREQ070, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
PERSISTENCE PRESENT
TEST PRESENT
APPLICATION/API EFFECTIVE DELIVERY NOT PROVEN
```

Execution classification:

```text
SECONDARY
```

## 54. ShareLink

**Requirements:** `WGREQ097–WGREQ104, WGREQ149, WGREQ156, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
APPLICATION/API PARTIAL SURFACE PRESENT
TEST PRESENT
PUBLIC CAPABILITY CONSUMPTION TO VERIFY
```

Execution classification:

```text
SECONDARY + HARDEN IF RELEASED
```

## 55. PermissionTemplate

**Requirements:** `WGREQ111–WGREQ113, WGREQ188`

Current classification:

```text
DOMAIN PRESENT
PERSISTENCE PRESENT
RICH DOMAIN TESTS PRESENT
APPLICATION/API APPLY DELIVERY NOT PROVEN
```

Execution classification:

```text
SECONDARY / DOMAIN GROUNDWORK UNTIL PROMOTED
```

# Cross-context handshake amendment

## 56. WorkManagement

**Requirements:** `WGREQ118–WGREQ121, WGREQ179, WGREQ182, WGREQ186, WGREQ190`

Current representative Board contract uses:

```text
ResourceKind: work-management.board
PermissionAction: typed Governance action vocabulary
```

Current production evidence candidates include:

```text
CreateBoardInWorkspacePipelineTests
GovernanceResourcePermissionFlowTests
```

Current decision:

```text
P3 resource/action handshake:
  design defined
  source evidence present
  certification NOT_EVALUATED
```

Do not treat presence of these tests as D5.

## 57. Documents

**Requirements:** `WGREQ122, WGREQ186`

Current Page facts are composed through:

```text
IPageAuthorizationFacts
```

Decision:

```text
producer-owned semantic facts
ACCEPTED
```

Governance must not absorb Page lifecycle ownership.

## 58. Billing

**Requirements:** `WGREQ124–WGREQ125, WGREQ186`

Current subscription requirement is composed through:

```text
IBillingSubscriptionFacts
```

Decision:

```text
Billing owns commercial tier/subscription decision
Governance consumes neutral satisfied/not-satisfied fact
```

This is the preferred cross-context semantic pattern.

## 59. Automation / Integrations

**Requirements:** `WGREQ126–WGREQ127, WGREQ185`

Current policy has dedicated:

```text
PermissionAction.ManageIntegrations
```

for relevant Workspace/Integration resources.

Decision:

```text
dedicated action RETAIN
background/system execution remains explicit
no local shadow RBAC
```

## 60. Collaboration

**Requirements:** `WGREQ123`

Collaboration authorization must use target-resource facts/policy.

No separate Collaboration permission engine is authorized by PR-WG-00.

## 61. Analytics

**Requirements:** `WGREQ128`

Analytics may consume authorization/audit facts/events/projections.

Analytics is not an authorization source of truth.

# Test evidence amendment

## 62. Existing source evidence candidates

**Requirements:** `WGREQ189`

Current source contains relevant tests including:

```text
Domain:
  WorkspaceTests
  WorkspaceOwnerRulesTests
  WorkspaceMemberTests
  WorkspaceInvitationTests
  TeamTests
  SpaceTests
  PermissionRuleLifecycleTests
  ResourcePermissionTests
  CustomRoleTests
  WorkspacePolicyTests
  ShareLinkTests
  PermissionTemplateLifecycleTests

Application:
  AccessControlBehaviorTests
  PipelineOrderTests
  CanonicalKindValidatorsTests
  GrantResourcePermissionCommandValidatorTests

Architecture:
  AuthPipelineArchitectureTests
  HandlerAuthorizationBypassArchitectureTests
  UseCaseSecurityClassificationTests
  RlsArchitectureTests
  RlsPolicyArchitectureTests
  DbContextBoundaryArchitectureTests

Integration:
  PostgresAccessFactsProviderTests
  GovernanceResourcePermissionFlowTests
  RlsRuntimeEnforcementTests
  CreateBoardInWorkspacePipelineTests
  WorkspaceCreationPipelineAuthorizationTests
  AcceptInvitationTransactionEvidenceTests
  WorkspaceCreatedOutboxEvidenceTests
  WorkspaceMembershipOutboxEvidenceTests
  MigrationSmokeTests
```

Record status:

```text
EXISTING-SOURCE
```

not:

```text
PASS
```

until candidate execution.

## 63. Preparation-time gaps

**Requirements:** `WGREQ144–WGREQ177, WGREQ185–WGREQ190`

The synchronized TESTS artifact records current evidence gaps.

Important core candidates include:

```text
duplicate membership database race
concurrent last-owner mutation
membership suspend/remove → access-grant revocation
same-priority PermissionRule matrix
Board-specific cross-account P2 gate
authorization dependency fail-closed fault injection
secret telemetry scan
supported-upgrade database fixture when schema changes
```

These remain:

```text
NOT_EVALUATED / GAP
```

until executed or proven covered by existing candidate tests.

# CI amendment

## 64. Current HEAD CI observation

**Requirements:** `WGREQ189`

At documentation audit HEAD:

```text
35702d0fa9fb01ed68b0667bab500030d60bd028
```

PR workflow:

```text
Notrelix CI run 36030280159
overall: SUCCESS
Backend CI: SKIPPED
Documentation CI: SUCCESS
CI gate: SUCCESS
```

Therefore this run is **not sufficient backend certification evidence**.

Push workflow:

```text
Notrelix CI run 36030239715
Backend preflight/static guards: SUCCESS
Backend architecture/core tests: SUCCESS
Backend API/platform/integration tests: SUCCESS
Backend gate: SUCCESS

Web image validation: FAILURE
Infrastructure assembled stack health: FAILURE
aggregate final gate: FAILURE
```

Therefore:

```text
Backend source evidence: supportive
Final exact-SHA aggregate certification: NOT SATISFIED
```

## 65. Historical full-green CI

**Requirements:** `WGREQ189`

Historical source:

```text
ff7274d0bf89e848dd8b3acf0ce5262fe870e7a6
Notrelix CI run 35726725430
```

had:

```text
Backend gates: SUCCESS
Infrastructure gate: SUCCESS
Container gate: SUCCESS
Frontend gate: SUCCESS
Documentation gate: SUCCESS
Dependency security gate: SUCCESS
Final CI gate: SUCCESS
```

Classification:

```text
HISTORICAL SUPPORTING EVIDENCE
```

not:

```text
current P2 exact-SHA certification
```

# Current decision matrix

## 66. Core decisions

| Decision | Current status | Required action |
|---|---|---|
| WG-DEC-TOPO-001 Application module-first topology | RESOLVED | retain |
| WG-DEC-AUTHZ-001 AccessControlBehavior canonical enforcement | RESOLVED | retain/harden |
| WG-DEC-FACTS-001 AccessFacts neutral provider contract | RESOLVED | retain/harden |
| WG-DEC-FACTS-002 WorkManagement facts boundary | PARTIALLY_RESOLVED | classify/normalize without ownership violation |
| WG-DEC-PERSIST-001 owner-local DbContext abstractions | RESOLVED | retain local-only |
| WG-DEC-RLS-001 membership→access-grant projection | ACCEPTED | verify revoke/suspend |
| WG-DEC-ACTION-001 PermissionAction catalog | RESOLVED CURRENT | inventory compatibility |
| WG-DEC-PRULE-001 User-subject core subset | RESOLVED CORE | defer unsupported subjects |
| WG-DEC-PRULE-002 rule priority/Deny precedence | ACCEPTED | execute tests |
| WG-DEC-POLICY-001 WorkspacePolicy | SECONDARY | do not silently integrate |
| WG-DEC-RPERM-001 ResourcePermission | PARTIALLY_CLOSED | verify full layer lifecycle |
| WG-DEC-RPERM-002 ACL management ceiling | RESOLVED | retain |
| WG-DEC-ROLE-001 built-in role identity | RESOLVED | retain |
| WG-ROLE-DEC-001 ManageBoard semantics | PARTIALLY_RESOLVED | WorkManagement action matrix |
| WG-DEC-CROLE-001 CustomRole | SECONDARY | runtime composition before release |
| WG-DEC-SHARE-001 ShareLink | SECONDARY | certify only released depth |
| WG-DEC-TPL-001 PermissionTemplate | SECONDARY | no invented apply flow |

## 67. Core source-debt disposition

| Debt | Historical state | Current state |
|---|---|---|
| WG-DEBT-001 direct evaluator DbContext coupling | OPEN | SUPERSEDED / PARTIAL CLOSE |
| WG-DEBT-002 built-in role baseline implicit | OPEN | PARTIAL CLOSE |
| WG-DEBT-003 ResourcePermission semantics too shallow | OPEN | PARTIAL CLOSE |
| WG-DEBT-004 WorkspacePolicy not in evaluator | OPEN | OPEN / intentional secondary |
| WG-DEBT-005 unsupported rule subject kinds | OPEN | OPEN / bounded core subset |
| WG-DEBT-006 WorkManagement tables in AccessFactsQuery | n/a | OPEN |
| WG-DEBT-007 generic ManageBoard overload | n/a | OPEN |
| WG-DEBT-008 grant revocation evidence | n/a | OPEN evidence gap |
| WG-DEBT-009 last-owner concurrency evidence | n/a | OPEN evidence gap |
| WG-DEBT-010 membership uniqueness race evidence | n/a | OPEN evidence gap |
| WG-DEBT-011 WorkspacePolicy delivery depth | n/a | OPEN secondary |
| WG-DEBT-012 CustomRole delivery depth | n/a | OPEN secondary |
| WG-DEBT-013 ShareLink consumption | n/a | OPEN secondary |
| WG-DEBT-014 PermissionTemplate apply | n/a | OPEN secondary |
| WG-DEBT-015 legacy names in architecture docs | n/a | OPEN docs debt |

# Migration decision

## 68. Current PR-WG-00 migration authority

**Requirements:** `WGREQ171–WGREQ177`

PR-WG-00 remains a semantic/source decision record.

It authorizes no schema migration by itself.

Before changing:

```text
ResourceKind strings
PermissionAction stored/string values
WorkspaceRole values
PermissionLevel values
PermissionRule schema
ResourcePermission schema
ShareLink/Invitation token formats
RLS policies
authz.access_grants
```

the executing work unit must record:

```text
current persisted consumers
candidate migration
compatibility
backfill
rollback
clean DB
supported upgrade
pending model changes
```

No migration should be generated merely because docs were rewritten.

# Public-contract decision

## 69. No forced naming migration

**Requirements:** `WGREQ044–WGREQ055, WGREQ171–WGREQ175`

The current source already uses stable logical kinds such as:

```text
work-management.board
documents.page
workspaces.workspace
integrations.calendar-integration
```

PR-WG-00 does not authorize renaming them for stylistic consistency.

A rename requires:

```text
consumer inventory
persisted-row inventory
API/event impact
compatibility plan
```

## 70. No duplicate Permission catalog

**Requirements:** `WGREQ054–WGREQ055`

PR-WG-00 still rejects adding a generic persisted Permission catalog solely to mirror the `PermissionAction` enum.

Add a new persisted concept only if it has independent product semantics.

# Security decisions

## 71. Default deny

**Requirements:** `WGREQ058, WGREQ085, WGREQ151`

Current security rule:

```text
missing role
missing required context
missing resource
unknown unsupported action/resource combination
unmet explicit authority
```

must never fall through into an implicit allow.

Exact outcome may be:

```text
Unauthorized
Forbidden
NotFound
SecurityMisconfiguration
```

according to privacy/request-contract semantics.

## 72. Resource lifecycle before owner authority

**Requirements:** `WGREQ086, WGREQ121`

For resource-scoped requests:

```text
resource exists/active?
```

is evaluated before the `Owner` fast path.

Decision:

```text
RETAIN
```

Owner status cannot resurrect missing/archived/deleted resource state.

## 73. ACL management is stronger than visibility

**Requirements:** `WGREQ076–WGREQ079, WGREQ154`

Decision:

```text
view/use resource
≠
manage resource ACL
```

`ManageBoardPermission` / `ManagePagePermission` require management authority.

Ordinary Workspace-visible resource access is insufficient.

## 74. RLS does not replace business authorization

**Requirements:** `WGREQ152, WGREQ187`

Decision:

```text
AccessControlBehavior
→ action authorization

RLS
→ persistence isolation
```

Both are required for P2 protected-slice certification.

# Stop conditions

## 75. Stop — second authorization engine

**Requirements:** `WGREQ080–WGREQ092, WGREQ185`

STOP if a work unit requires:

```text
new AuthorizationBehavior
new PermissionService
endpoint-local role matrix as canonical security
handler-local permission evaluator
```

without explicit replacement architecture.

## 76. Stop — foreign persistence ownership

**Requirements:** `WGREQ129–WGREQ133, WGREQ184, WGREQ186`

STOP if Governance/Workspaces must inject:

```text
foreign I*DbContext
foreign internal repository
foreign Domain aggregate
```

as a normal cross-context contract.

Supporting composite-read SQL must be explicitly classified under current architecture authority; it is not permission to spread foreign persistence dependencies.

## 77. Stop — ManageBoard semantics assumed globally

**Requirements:** `WGREQ118–WGREQ121, WGREQ190`

STOP if P2 attempts to decide:

```text
all Workspace Members can ManageBoard
```

or:

```text
only Board Manager ACL can ManageBoard
```

globally without the WorkManagement action matrix.

## 78. Stop — secondary model treated as delivered

**Requirements:** `WGREQ068–WGREQ079, WGREQ097–WGREQ113, WGREQ188`

STOP if any of:

```text
CustomRole
WorkspacePolicy
PermissionTemplate
ShareLink
ResourcePermission inheritance
```

is marked full-stack complete solely from Domain/persistence source.

## 79. Stop — historical record used as current runtime evidence

**Requirements:** `WGREQ185, WGREQ189`

STOP if current implementation/certification cites:

```text
AuthorizationBehavior
PermissionService
```

as the active runtime merely because they appear in the original PR-WG-00 text.

## 80. Stop — exact candidate evidence mismatch

**Requirements:** `WGREQ189`

STOP final certification when:

```text
source SHA
test SHA
CI SHA
migration/OpenAPI evidence SHA
```

cannot be reconciled under repository governance.

# Phase 0–2 current amendment result

## 81. Phase 0 — current source inventory

**Requirements:** `WGREQ183, WGREQ188, WGREQ189`

For documentation synchronization:

```text
Status: COMPLETE AS SOURCE AUDIT
Certification effect: NONE
```

The current audit has enough evidence to update SPEC/PLAN/TESTS/CERTIFICATION and this decision record.

It is not a substitute for executing the PLAN on the final candidate.

## 82. Phase 1 — upstream P1 contract

**Requirements:** `WGREQ114–WGREQ117, WGREQ190`

Historical P1 certification exists and is the intended producer prerequisite.

Current execution must re-check that the required P1 contract remains valid at candidate time.

Current documentation status:

```text
P1 dependency known
candidate compatibility: NOT_EVALUATED
```

## 83. Phase 2 — semantic classification

**Requirements:** `WGREQ042–WGREQ084, WGREQ188`

Current semantic classification is now sufficiently explicit to continue execution:

```text
Workspace Rules                RETAIN
ResourceKind/ResourceRef       RETAIN
PermissionAction               RETAIN
PermissionRule User subset     RETAIN
WorkspacePolicy                SECONDARY
ResourcePermission             RETAIN + HARDEN
WorkspaceRole                  RETAIN
CustomRole                     SECONDARY
ShareLink                      SECONDARY
PermissionTemplate             SECONDARY

AccessControlBehavior          canonical enforcement
AccessPolicyEngine             canonical Governance evaluator
PostgresAccessFactsProvider    canonical Infrastructure facts provider
RLS access-grant projection    canonical isolation projection mechanism
```

Unresolved decisions are explicitly tracked as debt/STOP conditions.

# Next execution work

## 84. Immediate sequence

Do not jump directly to a new permission architecture.

Proceed:

```text
1. recapture exact execution candidate
2. execute updated Phase 0 inventory against that candidate
3. append current decision evidence if source changed
4. verify Workspace core
5. verify Membership core
6. close membership uniqueness / owner concurrency / grant revocation evidence
7. verify ResourceKind/PermissionAction stability
8. verify PermissionRule/default-deny semantics
9. verify built-in role baseline
10. verify AccessControlBehavior + AccessFacts + AccessPolicyEngine
11. prove RLS isolation
12. prove Board allow / deny / cross-tenant protected handshake
13. execute migration/OpenAPI/docs/CI evidence
14. fill workspace-governance.certification.md
```

## 85. WorkManagement follow-up required

**Requirements:** `WGREQ118–WGREQ121, WGREQ190`

P3 execution must explicitly inventory every request currently using:

```text
PermissionAction.ManageBoard
```

and classify it as one of:

```text
Workspace-member usage right
Board management right
ACL management right
more specific business action required
```

This decision belongs at the WorkManagement/Governance contract boundary.

Do not solve it with a broad global role change.

## 86. Secondary-scope sequencing

**Requirements:** `WGREQ034–WGREQ041, WGREQ068–WGREQ079, WGREQ097–WGREQ113, WGREQ188`

After the protected P2 gate is stable:

```text
Invitation hardening
Provisioning
Teams
Spaces
WorkspaceHome

then, independently:
CustomRole
WorkspacePolicy
ResourcePermission depth
ShareLinks
PermissionTemplate
Audit/SecurityEvent delivery
```

Each secondary capability must use layer-status certification.

# Decision record reporting format

## 87. Future amendment format

**Requirements:** `WGREQ189`

Every later amendment to PR-WG-00 should append:

```text
Amendment ID:
Date:
Branch:
Candidate SHA:
Trigger:
Source paths inspected:
Previous decision:
New observation:
Decision:
Migration impact:
Public contract impact:
Security impact:
Tests:
CI:
Debt changed:
Downstream impact:
Certification invalidation:
```

Do not overwrite historical evidence without preserving why the earlier statement was valid at the earlier SHA.

# Final current-state decision

## 88. Current authority summary

**Requirements:** `WGREQ183–WGREQ187`

As of the documentation audit baseline:

```text
Canonical Workspace owner:
  Workspaces bounded context

Canonical Workspace membership:
  WorkspaceMember

Canonical action/resource declaration:
  ResourceKind + PermissionAction

Canonical Application enforcement:
  AccessControlBehavior

Canonical neutral policy seam:
  IAccessPolicyEvaluator

Canonical Governance evaluator:
  AccessPolicyEngine

Canonical datastore facts provider:
  PostgresAccessFactsProvider

Canonical facts SQL:
  AccessFactsQuery.Sql

Canonical persistence isolation:
  RLS + authz.access_grants projection

Canonical owner-local persistence abstractions:
  IWorkspaceDbContext
  IGovernanceDbContext

Current representative P3 protected resource:
  work-management.board
```

## 89. Current unresolved core items

**Requirements:** `WGREQ144–WGREQ177, WGREQ185–WGREQ190`

P2 must not be marked STABLE until evidence resolves at least:

```text
membership uniqueness race
last-owner concurrency
membership grant revocation/suspend behavior
PermissionRule winning-priority matrix
Board-specific cross-tenant protected proof
authorization dependency fail-closed evidence
RLS enforcing-role evidence
candidate migration/model state
exact candidate CI
ManageBoard contract impact for downstream WorkManagement
```

Some items may turn out already implemented/tested.

The execution process must prove that rather than assuming it.

## 90. Current unresolved secondary items

**Requirements:** `WGREQ068–WGREQ079, WGREQ097–WGREQ113, WGREQ188`

These do not automatically block P2 protected core:

```text
CustomRole runtime composition
WorkspacePolicy runtime composition
ShareLink public capability consumption
PermissionTemplate application
ResourcePermission inheritance depth
advanced Audit/SecurityEvent delivery
```

They block only the corresponding full-scope certification claim unless a protected downstream flow begins to depend on them.

## 91. Final PR-WG-00 rule

**Requirements:** `WGREQ189–WGREQ190`

This record now establishes the following execution rule:

```text
Historical PR-WG-00 explains where P2 came from.

Current amendment explains what the source means now.

SPEC defines the target.
PLAN defines the work.
TESTS defines proof.
CERTIFICATION decides status.
```

No historical source statement, rich Domain model, existing test file, or previous green CI run can substitute for candidate-SHA certification.

The next allowed action is execution/verification against the updated Workspace & Governance package — not creation of a second authorization architecture.

## Amendment evidence binding

The current amendment is evaluated against the corrected artifact package:

```text
SPEC:          workspace-governance.spec.v2.md
PLAN:          workspace-governance.plan.v3.md
TESTS:         workspace-governance.tests.v2.md
CERTIFICATION: workspace-governance.certification.v3.md
```

Traceability rule:

```text
decision / debt
→ WGREQ
→ PLAN work unit
→ exact WG-TST scenario
→ candidate-SHA evidence
→ CERTIFICATION
```

The amendment itself does not convert any source observation into PASS/D4/D5.

## Amendment debt handoff binding

The certification package now carries the stable semantic-debt namespace directly.

Mandatory P3 handoff debt:

```text
WG-DEBT-006
  current AccessFactsQuery WorkManagement composite-read boundary

WG-DEBT-007
  generic ManageBoard semantic overload
```

`WG-DEBT-008`, `WG-DEBT-009` and `WG-DEBT-010` are Milestone-B blocking evidence debts and therefore
must be closed before protected P3-B release rather than silently handed downstream.

The canonical debt disposition/certification crosswalk lives in:

```text
workspace-governance.certification.v3.md
→ Semantic debt → certification disposition
```

This amendment remains the semantic source of debt identity; CERTIFICATION owns the release disposition.
