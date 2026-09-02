# PR-WG-06 — Phase 9 Authorization pipeline integration / hardening

Evidence record for PLAN §91–102 (WG-AUTHZ-001..010), TESTS §86–97 (WG-TST-AUTHZ-APP-*, WG-TST-PIPE-*), and the
WorkspaceAdmin administrative-baseline fix. Phase 9 is "Existing authorization path hardening": focused
verification of the canonical path + one real source-debt fix.

## Baseline

- Branch: `feature/workspace-governance`.
- Phase 9 opens from Phase 8 close (`eae00378`).
- Baseline suites (Phase 6 close `2734ee4`): Application 588, Integration 346, Architecture 410.
- Phase 8 closed with Integration 350 (current inherited baseline before Phase 9 tests).

## Scope of this phase

1. **Focused verification** of the existing canonical Application authorization path — no second
   AuthorizationBehavior/PermissionService/evaluator/decision-store stack, no mass role-check rewrite.
2. **One approved fix**: close the **WorkspaceAdmin administrative-baseline gap** (user decision).

## The gap (evidence)

`AccessPolicyEngine` previously let only Workspace **Owner** short-circuit to allow (the `role == "Owner"`
branch). At Workspace scope on `workspaces.workspace`, a Workspace **Admin** is denied workspace-management
actions (`ManageWorkspace`, `InviteMember`, `ChangeMemberRole`, `RemoveMember`, `ManageWorkspaceSettings`,
`CreateBoard`) unless a custom `governance.permission_rules` row exists. No production DDL seeding grants
these (only test fixtures insert `permission_rules`). This contradicted:

- WG-ROLE-DEC-001 (Phase 8): "Owner/Admin = administrative class".
- `PRODUCT.md` §164: Administrators are responsible for "account/workspace lifecycle; membership and access; security".

Classification: `SOURCE_DEBT` (canonical product/role intent clear; source violated it). Scope guard prevents
leak into Resource/board scope. Board-management remains resource-owned (Phase 8) in this fix.

## Change

### Production code

`backend/src/Notrelix.Application/Common/Security/AccessPolicyEngine.cs` — added the WorkspaceAdmin
administrative baseline in the non-account branch, after the `DeleteWorkspace` Owner-only denial and before
the board-resource branch:

```csharp
if (descriptor.Scope == ApplicationScopeKind.Workspace
    && string.Equals(role, "Admin", StringComparison.Ordinal)
    && IsWorkspaceAdministrativeAction(permission.Action))
{
    return AccessDecision.Allow();
}
```

Helper `IsWorkspaceAdministrativeAction` = { ManageWorkspace, InviteMember, ChangeMemberRole, RemoveMember,
ManageWorkspaceSettings, CreateBoard }.

Preserved invariants:

- Owner short-circuit unchanged.
- Board management resource-owned: `ManageBoard`, `ManageBoardPermission`, `CreateField`, `UpdateField`,
  `DeleteField`, `ShareBoardView`, `CreateBoardView`, `UpdateBoardView` are NOT in the admin set — Admin still
  needs Board owner/admin role or explicit resource permission (Phase 8 intact).
- `DeleteWorkspace` is Owner-only (the Admin grant sits after this hard denial).
- PermissionRule min-priority-band deny precedence + default-deny (Phase 7) intact.
- Member baseline (`ViewWorkspace`/`ViewBoard`/`ViewMembers`) unchanged.
- Scope guard `== Workspace` prevents the grant reaching Resource/board scope.

### Tests

`backend/tests/Notrelix.Integration.Tests/Baselines/AccessPolicyEngineCharacterizationTests.cs` — 7 new
characterization tests (names registered in `CharacterizationTestNames`):

- `WorkspaceAdminCanManageWorkspace` — Admin + ManageWorkspace → Allowed
- `WorkspaceAdminCanInviteMember` — Admin + InviteMember → Allowed
- `WorkspaceAdminCanChangeMemberRole` — Admin + ChangeMemberRole → Allowed
- `WorkspaceMemberCannotAdministerWorkspace` — Member + ManageWorkspace → Forbidden
- `WorkspaceGuestCannotAdministerWorkspace` — Guest + ManageWorkspace → Forbidden
- `WorkspaceAdminCannotManageBoardWithoutBoardAuthority` — Admin + ManageBoard at Resource scope → Forbidden (Phase 8 boundary)
- `WorkspaceAdminCannotDeleteWorkspace` — Admin + DeleteWorkspace → Forbidden (Owner-only)

## Phase 9 verdicts (WG-AUTHZ-*)

| ID | Verdict | Evidence |
|---|---|---|
| WG-AUTHZ-001 | DONE | Single canonical path (AccessPolicyEngine + AccessControlBehavior + PostgresAccessFactsProvider + AccessFactsQuery + IRequirePermission; 100+ adopters). No ResourceType symbol — ResourceKind canonical |
| WG-AUTHZ-002 | DONE | `IAccessPolicyEvaluator → AccessPolicyEngine` singleton (`Application/DependencyInjection.cs:44`); facts/locator in `PersistenceRegistration.cs:97-107`; behaviors in `Application/DependencyInjection.cs:32-38` |
| WG-AUTHZ-003 | DONE | ExceptionMapping → ApplicationTracing → RequestContract → ExecutionContext → DataSession → **AccessControl** → Idempotency; no reorder |
| WG-AUTHZ-004 | DONE | AccessFacts + IRequirePermission supply Actor/Account/Workspace/Resource/Action; ResourceKind canonical (no ResourceType symbol) |
| WG-AUTHZ-005 | DONE | One decision path composes membership/role/permission/resource-permission |
| WG-AUTHZ-006 | DONE | `AccessControlBehavior.cs:50-68` only Allowed→next(); SecurityMisconfiguration/Forbidden/NotFound otherwise; ResourceLocator null→deny; no try/catch→Allow |
| WG-AUTHZ-007 | DONE | Pipeline denies before handler/commit; `MemberApiToken_..._GovernanceDeniesManageWorkspaceSettings` proof |
| WG-AUTHZ-008 | DONE | Only registered business invariants (TransferOwnership, last-owner, bootstrap) via `HandlerAuthorizationBypassArchitectureTests` registry; no mass rewrite |
| WG-AUTHZ-009 | DONE | API is scope-gating only (`EndpointAccessAttribute`; HttpRequestContextMiddleware delegates business authz to Application) |
| WG-AUTHZ-010 | DONE | TenantContextConsumeFilter sets System/Account/Workspace; ISystemInternalRequest commands guarded; lease commits after handler success |

## Suite evidence

| Suite | Result | Note |
|---|---|---|
| Application.Tests | 588 green | no delta beyond engine helpers |
| Integration.Tests | **357 green** (350 → 357) | +7 new characterization tests |
| Architecture.Tests | 410 green | engine purity invariants unchanged |
| backend.slnx build | 0 errors | SDK 9.0.317 (no rollback) |

## Decisions

### D9-A — Close the WorkspaceAdmin administrative-baseline gap — DECIDED + TESTED

A Workspace Admin may perform workspace-scope administration (`ManageWorkspace`, `InviteMember`,
`ChangeMemberRole`, `RemoveMember`, `ManageWorkspaceSettings`, `CreateBoard`) without a custom rule, at
Workspace scope. This resolves the source-debt and matches WG-ROLE-DEC-001 administrative class + PRODUCT.md.

### D9-B — Board management stays resource-owned — DECIDED

The Admin grant does NOT include board-management actions; Phase 8 board baseline (Board owner/admin role or
explicit resource permission) is preserved. Proven by `WorkspaceAdminCannotManageBoardWithoutBoardAuthority`.

### D9-C — Owner-only actions preserved — DECIDED

`DeleteWorkspace` remains Owner-only (Admin denied). Proven by `WorkspaceAdminCannotDeleteWorkspace`.

### D9-D — No role-check debt mass migration — DECIDED

Focused choice: register existing handler-local checks as business invariants; no rewrite of TransferOwnership
or member last-owner checks this phase.

### D9-E — `ResourceKind` is canonical; no `ResourceType` symbol — DOCUMENTED

WG-AUTHZ-004 clarified there is no `ResourceType` domain symbol; `ResourceKind` is the canonical resource
category. No rename performed.

## Findings

- Declared-only enum members (`ArchiveWorkspace`, `RestoreWorkspace`, `ManageSpaces`, `ManageTeams`,
  `DeleteWorkspace`) are not referenced by commands; the corresponding operations all declare `ManageWorkspace`,
  so they are covered by the Admin grant. Re-wiring to the dedicated members would require product-level
  consumption — deferred, not a blocker.
- No production seeding of `permission_rules`; the built-in Owner/Admin baselines are the default grants,
  consistent with Phase 8 Board baseline and this Phase 9 Workspace baseline.

## Phase 9 exit

Authorization integration reached D4/D5 for representative resources (PLAN §102). **Phase 9 CLOSED.**
Phase 10 (WorkManagement handshake, PLAN §103+, PR-WG-07) may open.
