# PR-WG-00 - Workspace and Governance semantic inventory

## Baseline

```text
Branch: develop
HEAD: f91c203bb63e799e3d7979433d9c1d799b5fe794
Solution: backend/backend.slnx
Migration head: 20260702093805_SchemaV2Baseline
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
