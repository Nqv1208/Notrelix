# PR-WG-01 — Phase 3 Workspace core verification

Evidence record for PLAN §36–43 (WG-WSP-001..007), SPEC §8–14 (WGREQ001–007), TESTS §9–15 (WG-TST-WSP-*).

## Baseline

- Branch: `feature/workspace-governance` @ `ae4af8f0` (from `develop`).
- Audit date: 2026-08-30.
- Authority read: product contexts (`docs/product/workspaces.md`, `docs/product/governance.md`), RULE.md, root `AGENTS.md`, `backend/AGENTS.md`, `backend/tests/AGENTS.md`, backend architecture topics (domain-modeling, application-model, security-tenancy-authorization, infrastructure-and-data).

## Verdict ledger

| ID | Scope | Verdict | Evidence | Note |
|---|---|---|---|---|
| WG-WSP-001 | Workspace aggregate/model | PASS | `Notrelix.Domain/Workspaces/Workspaces/Workspace.cs:6–14` (root, `IAccountScoped`), `:18–50` Create, lifecycle status + soft-delete via `Common/SoftDeletableAggregateRoot.cs`, `Common/AggregateRoot.cs:9,14–20` (Version/IncrementVersion), `Common/Entity.cs:5` stable Guid; Events under `Workspaces/Events/` | Stable ID, lifecycle, metadata, events, soft-delete/archive, concurrency version all present |
| WG-WSP-002 | Account containment | PASS w/ notes | `Commands/CreateWorkspace/CreateWorkspace.cs:33` AccountId session-derived (`RequireAccountId`); `tests/.../WorkspaceTenantOwnershipTests.cs:11–29` (private setter, no `UpdateAccountId`); `Infrastructure/.../Workspaces/WorkspaceConfiguration.cs:19` account_id required | No reparenting (correct). Notes: no DB FK → `accounts`; no RLS policy for `workspace` schema tables |
| WG-WSP-003 | Lifecycle + downstream effects | PARTIAL | Transitions implemented in `Workspace.cs` + integration mapper `Application/EventMappers/Workspaces/WorkspaceEventMapper.cs:56–122`; `Application/Events/Workspaces/WorkspaceLifecycleIntegrationEvents.cs` | DELETE/ARCHIVE effects NOT handled: consumers are log-only stubs (`Infrastructure/Messaging/Consumers/Workspaces/WorkspacesStubConsumers.cs:5–80`), no `WorkspaceDeletedIntegrationEvent` consumer; WG-TST-WSP-X-001 cannot pass as specified |
| WG-WSP-004 | Lifecycle authorization | PASS | All lifecycle commands declare `IRequirePermission` + `PermissionAction` (CreateWorkspace=CreateWorkspace; Delete/Archive/Unarchive/Restore/Transfer/Update=ManageWorkspace + `ResourceRef`); `IExpectedVersionRequest` enforced in pipeline (`Common/Requests/Execution/RequestDescriptorValidator.cs:37–77`, `Common/Behaviors/DataSessionBehavior.cs:47–56`); no role-string authz | `Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs` proves create is authorized by canonical pipeline over real PostgreSQL before handler |
| WG-WSP-005 | Event baseline | PASS | Only meaningful owned facts emitted; deliberate mapping to integration events via outbox; no property-noise events | |
| WG-WSP-006 | Persistence | PASS w/ notes | `WorkspaceConfiguration.cs:9` (`workspace` schema), `:19` account_id required, `:39–42` `ux_workspaces_account_slug_active` (unique AccountId+Slug, filtered `deleted_at IS NULL`), `:45–48` personal-per-account, `:36` name index; Version by convention; migration `SchemaV2Baseline` + snapshot | Notes: no DB FK → `accounts`; no RLS policy for `workspace` schema tables |
| WG-WSP-007 | Account disabled interaction | RESOLVED (D3-B) | Central pipeline enforcement now denies non-operational Account; evidence in `AccessPolicyEngine.cs:40–45`, `AccessFactsQuery.cs:74–77`, `PostgresAccessFactsProvider.cs:85`, `AccessFacts.cs:17`; negative proof in `WorkspaceCreationPipelineAuthorizationTests.cs` (suspended account) + `AccessPolicyEngineCharacterizationTests` (3 fail-closed scenarios) | Previously only `AcceptInvitation.cs:132` gated `AccountStatus.Active|Trialing`; canonical failure-policy owner decided D3-B |

## RLS finding

`RlsSqlScripts/008_policies_workspace_scoped_domain.sql` defines scoped business policies for `work`, `docs`, `collab`, `automation`, `integration`, `reporting`, `billing` — and `003_authz_access_helpers.sql` provides `ops.has_workspace_access/is_workspace_admin/has_permission`. Across scripts 001–011 there is **no policy definition for `workspace` schema tables** (`workspaces`, `workspace_members`, `workspace_invitations`, `teams`, `spaces`). `011_verification.sql:32–38` reports tables without row security as `RLS_DISABLED`; runtime state not verified here.

Recorded (follow-up, not Phase 3 blocking):

- `WG-FIND-301` — `workspace` schema tables have no RLS policy; verify runtime RLS state + decide coverage scope with `security-tenancy-authorization.md` owner.
- `WG-FIND-302` — `workspace.workspaces.account_id` has no DB FK to `accounts`; containment is Application/authz-enforced. Decide whether DB-level constraint is required.
- `WG-FIND-303` — workspace lifecycle consumers are log-only stubs; delete/archive downstream effects (membership/invitation/team/space) unhandled.
- `WG-FIND-304` — no account-inactive guard on workspace protected operations.

## Test coverage (TESTS §9–15)

| TEST ID | Verdict | Note |
|---|---|---|
| WG-TST-WSP-DOM-001 identity stability | Covered | WorkspaceTests / WorkspaceVersionTests / WorkspaceScopeTests |
| WG-TST-WSP-DOM-002 containment + invalid Account negative | Partial | Containment covered (WorkspaceTenantOwnershipTests); invalid-Account negative not explicit — AccountId is session-derived, structurally unreachable via API; decide whether a negative is meaningful |
| WG-TST-WSP-INT-001 cross-tenant invisibility | Partial | API layer present (WorkspaceQueryTests, WorkspaceAuthTests); explicit Account A/B integration denial to be verified in Phase 4 |
| WG-TST-WSP-DOM-003/004 lifecycle valid + invalid no side effect | Covered | WorkspaceTests, WorkspaceRulesTests, WorkspaceDeletionAtomicityTests, WorkspaceMutationOrderingTests |
| WG-TST-WSP-X-001 archive/delete effects explicit | NOT implemented | Stub consumers; open D3-A |
| WG-TST-WSP-APP-001 update cannot touch Governance | By construction | UpdateWorkspaceProfile/UpdateWorkspaceSettings mutate owned fields only; explicit test optional |
| WG-TST-WSP-APP-002 / INT-002 provisioning ownership + idempotency | Partial | Create = `WorkspaceFactory.CreateWithOwner` (workspace + owner membership) + grant sync atomically; idempotency proof belongs to WGREQ009 (provisioning) item, later |

## Open decisions (Phase 3 close required both)

### D3-A — Workspace delete/archive downstream effects (WG-WSP-003) — DECIDED

**Accepted: semantic split.**

- **Archive** = reversible freeze: preserve membership/invitation/team/space state, deny/freeze protected operations, no destructive cleanup, keep `Unarchive`.
- **Delete** = tombstone: eliminate effective access immediately, revoke/invalidate pending invitations, transition membership/team/space per their own lifecycle preserving historical attribution, notify downstream contexts via explicit contracts/events. **No cross-context DB cascade as business semantics.**
- Phasing: Phase 3 resolves semantics + producer lifecycle facts only; Membership effects → Phase 4; Invitation effects → Phase 5; Team/Space effects → Phase 13; external BEC consumers → cross-context hardening.
- Existing log-only consumers stay explicitly **TRANSITION** until the owning phase ships (see `WorkspacesStubConsumers.cs`).

### D3-B — Account-inactive failure policy (WG-WSP-007) — DECIDED + IMPLEMENTED

**Accepted: central Application access-control enforcement.**

- Accounts owns the semantic "Is this Account operational for protected product operations?" (`docs/product/accounts.md` §13: active Account = operational per Account policy; operational = `AccountStatus.Active|Trialing`, matching `AcceptInvitation`).
- The existing AccessControl pipeline consumes the operational fact and **fails closed before protected handler side effects**: new `AccessFacts.AccountOperational` fact (SQL `EXISTS account.accounts ... status IN ('Active','Trialing')`), enforced in `AccessPolicyEngine` for Account/Workspace/Resource scopes → `Forbidden` ("This account is not operational.").
- **Not** in `DataSession`. **Not** Identity/session revocation. **Not** per-handler checks (no duplicate `Active|Trialing` enum checks in handlers). `AcceptInvitation` keeps its bounded token-scoped local check but is not the template.
- Missing / non-operational / unavailable Account state → fail closed; API mapping preserves the existing failure policy (`auth.forbidden`, 403 via default deny→`ForbiddenException`).

## Phase 3 exit

Workspace identity + Account containment are D4-ready (WG-WSP-001/002/004/005/006 evidenced). Phase 3 close:

1. D3-A and D3-B decisions recorded — done;
2. D3-B enforcement implemented and proven — done;
3. follow-up items WG-FIND-301..304 dispositioned — disposition below.

## Follow-up disposition (WG-FIND-301..304)

- `WG-FIND-301` (workspace schema RLS) — not Phase-3 blocking; verify runtime RLS state + coverage scope with `security-tenancy-authorization.md` owner (separate investigation).
- `WG-FIND-302` (no DB FK `workspaces.account_id → accounts`) — accepted as-is; containment is Application/authz-enforced. Revisit if a DB-level constraint becomes product-required.
- `WG-FIND-303` (stub lifecycle consumers) — explicitly TRANSITION until owning phase ships per D3-A phasing.
- `WG-FIND-304` (no account-inactive guard) — **closed** by D3-B central enforcement.