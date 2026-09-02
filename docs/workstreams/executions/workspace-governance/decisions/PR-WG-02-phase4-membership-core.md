# PR-WG-02 — Phase 4 Membership core verification

Evidence record for PLAN §44–54 (WG-MEM-001..010), SPEC §19–30 (WGREQ012–023), TESTS §20–30 (WG-TST-MEM-*).

## Baseline

- Branch: `feature/workspace-governance` (no commit; working tree includes Phase 4 changes reviewed below).
- Audit date: 2026-08-30.
- Authority read: product contexts (`docs/product/workspaces.md`, `docs/product/governance.md`, `docs/product/accounts.md`, `docs/product/identity.md`), RULE.md, root `AGENTS.md`, `backend/AGENTS.md`, `backend/tests/AGENTS.md`, backend architecture topics (domain-modeling, application-model, security-tenancy-authorization, infrastructure-and-data, api-and-contracts).
- Baseline (pre-phase, all green): Application 571, Domain 2576, Architecture 410, Integration 340, API 256, Infrastructure 134.

## Scope of this phase

Audit/verify `WorkspaceMember` baseline (D4+ target) and close decision gaps found in the audit:

1. **WG-MEM-008 / WGREQ022** — Identity deactivation must not be granted access through stale authorization state. Existing access-facts machinery was account-complete only (D3-B); the user-operational dimension was missing. Closed with a central **`UserOperational`** access fact + denial gate (D4-1).
2. **WGREQ016/017** — Add/remove-member are protected operations. `AddMember` lacked **target User identity validation** (an actor could be added without an existing `identity.users` row). Closed (D4-2).
3. **WG-MEM-009 / WGREQ012** — Membership query contract. `WorkspaceMemberDto` exposed no membership status, and `GetUserWorkspaces` included `Removed` memberships (active-account "My workspaces" would list left workspaces). Closed (D4-3).

## Verdict ledger

| ID | Scope | Verdict | Evidence | Note |
|---|---|---|---|---|
| WG-MEM-001 | WorkspaceMember model | PASS | `Domain/Workspaces/Members/WorkspaceMember.cs:5` (`AggregateRoot`, `IWorkspaceScoped`), `:7–11` stable `AccountId/WorkspaceId/UserId/WorkspaceRole/WorkspaceMemberStatus`; `Domain/Workspaces/Members/Events/*`; `Version` via `Common/AggregateRoot.cs` | References stable User/Actor (`UserId`) and Workspace (`WorkspaceId`); MUST NOT own credentials — `UserId` only, no Identity private state |
| WG-MEM-002 | Membership uniqueness | PASS w/ notes | `Infrastructure/Data/Configurations/Workspaces/WorkspaceMemberConfiguration.cs:25` — unique `idx_workspace_members_workspace_user` on `(WorkspaceId, UserId)`; `AddMember.cs:57–58` rejects existing active membership | Non-filtered unique index backstops one row per (Workspace, User). Concurrent add (two handlers both miss then insert) → unique violation mapping not yet clean — WG-MEM-010/`WG-TST-MEM-INF-001` deferred |
| WG-MEM-003 | Add member | PASS (D4-2) | `AddMember.cs` — `IRequirePermission` (`InviteMember` placeholder until Phase 8) → central pipeline; workspace scope validated (`:42–46`); **target User existence validated** (`:48–50` `IActorLookupService.FindAsync` → `NotFoundException`); duplicate-active rejected (`:57–58`); re-add suspended reactivates (`:60–61`); re-add removed throws `CannotActivateRemoved` (terminal, `Domain`); grant projection synced (`:76–82`); event `WorkspaceMemberAddedDomainEvent` | Target-identity validation (WGREQ016 enforcement completeness) was the Phase 4 gap. Latent bug: `ChangeRole(..., activeOwnerCount: 1)` literal at `:61` — see WG-FIND-401 |
| WG-MEM-004 | Remove member | PASS | `RemoveMember.cs` — `IRequirePermission` (`RemoveMember`); `:51` `WorkspaceMember.Remove(activeOwnerCount, ...)` with `WorkspaceOwnerRules.EnsureCanRemoveOwner` (`Rules/WorkspaceOwnerRules.cs:25–30`) → fail closed on last owner; row retained as `Status=Removed` (no delete) → historical attribution preserved (WGREQ023); event `WorkspaceMemberRemovedDomainEvent`; grant projection revoked (`:53–58`) | |
| WG-MEM-005 | Self leave | NOT IMPLEMENTED (decision) | No command/endpoint exists | Owners/admins self-leave is undefined product behavior; do not invent owner semantics. No repository-invariant concurrency issue while absent. Decision D4-C records deferral |
| WG-MEM-006 | Membership state changes | PASS w/ notes | `Commands/SuspendMember`, `Commands/ActivateMember` exist, `IRequirePermission` (action placeholder reused from `RemoveMember` — Phase 8 alignment); `Suspend.cs` guards last owner; events `WorkspaceMemberSuspendedDomainEvent` / `WorkspaceMemberActivatedDomainEvent`; grant projection revoked on suspend / restored on activate | Effective permission impact is deterministic: access control reads live DB facts (below), no membership permission cache exists. Authorization action name is a Phase 8 alignment item (permission semantics owned by Governance/roles) |
| WG-MEM-007 | Role association | PASS | Membership carries a single `WorkspaceRole`; no embedded permission list; role → effective permission via grant projection (`IAccessGrantProjectionService`) + Governance tables | Deferred to Phase 8: built-in role/permission engine (WG-ROLE-DEC-001 UNRESOLVED, does not block Phase 4) |
| WG-MEM-008 | Identity deactivation interaction | PASS (D4-1) | New `AccessFacts.UserOperational` (16th) fact — `AccessFactsQuery.Sql:77–79` (`identity.users u ... u.status IN ('Active','PendingVerification')`); `Infrastructure/Data/Authz/PostgresAccessFactsProvider` reads col 15; `Application/Common/Security/AccessPolicyEngine` denies non-operational User for Account/Workspace/Resource scopes → `Forbidden` ("This user is not operational.") before handler effects; fail-closed for unknown/new statuses | WGREQ022 satisfied: access follows current identity validity; membership history unaffected; Workspaces never mutates Identity state. `PendingVerification` allowed so `AcceptInvitation` remains pending-policy-correct. Negative proofs: `AccessPolicyEngineCharacterizationTests` (account-scope + workspace-scope non-operational user, single EvaluationCount) + `WorkspaceCreationPipelineAuthorizationTests.CreateWorkspace_WhenActorUserIsSuspended_FailsClosedBeforeHandlerEffects` |
| WG-MEM-009 | Membership query contract | PASS (D4-3) | `WorkspaceMemberDto` + `Status` (WORKSPACE_MEMBER → string, after `Role`, before `JoinedAt`); `GetWorkspaceMembers` populates `Status`; `GetUserWorkspaces` excludes `WorkspaceMemberStatus.Removed` (join + member-count) | Exposes membership status, never Identity private state (email confirmation/credential facts stay in Identity). API tests green (API 256) |
| WG-MEM-010 | Concurrency hardening | DEFERRED (decision) | In-domain last-owner guards present + unit-proven single-thread (`WorkspaceMemberTests`), but no serialization/retry for concurrent remove/demote and no duplicate-add race handling beyond the unique index | `WG-TST-MEM-INF-001`, `WG-TST-MEM-CONC-001` deferred to concurrency-hardening phase (same phase as WGREQ144/145 machinery). Decision D4-D |

## Effective-access mechanism (WGREQ021 — no stale cache)

Access policy reads **live DB facts** every evaluation (`AccessFactsQuery.Sql` — the canonical single source; never forked):

- col 5 workspace role subquery filters `wm.status = 'Active'` (`:19–21`) → suspended/removed membership yields no role → deny.
- col 15 user operational gate filters `identity.users` status → suspended/inactive/soft-deleted User denies before any role/grant suggests otherwise.

No membership-permission cache exists; nothing to invalidate. This is the same D3-B pattern applied to the User dimension.

## Test coverage (TESTS §20–30)

| TEST ID | Verdict | Note |
|---|---|---|
| WG-TST-MEM-DOM-001 stable upstream identity | Covered | `WorkspaceMemberTests` (identity/refs), model evidence above |
| WG-TST-MEM-INF-001 concurrent duplicate blocked | DEFERRED | Unique index present as DB backstop; race mapping under WG-MEM-010 |
| WG-TST-MEM-DOM-002 lifecycle transitions | Covered | `WorkspaceMemberTests` (Create/ChangeRole/Suspend/Activate/Remove, no-op semantics, last-owner guards) |
| WG-TST-MEM-APP-001 add requires auth (deny before state) | Covered | Command `IRequirePermission`; `HandlerAuthorizationBypassArchitectureTests` gate; Application tests prove rejection leaves no grant (D4-2 suite) |
| WG-TST-MEM-APP-002 remove requires auth | Covered | Command `IRequirePermission`; `WorkspaceAuthTests` (unauthenticated deny); architecture bypass gate |
| WG-TST-MEM-APP-003 self-leave policy | NOT APPLICABLE | No self-leave command; policy deferred with WG-MEM-005 (D4-C) |
| WG-TST-MEM-CONC-001 last-admin concurrent removal | DEFERRED | In-domain guard proven single-thread; concurrency-safe enforcement + proof under WG-MEM-010 |
| WG-TST-MEM-APP-004 role delegates to Governance | Covered | Membership carries `WorkspaceRole` only; effective grants via `IAccessGrantProjectionService`; no local permission resolver |
| WG-TST-MEM-INT-001 suspended/inactive member loses access | Covered (mechanism) | SQL col 5 role only for `status='Active'`; grant revoked on suspend; no cache. Explicit full-pipeline suspended-*member* negative is a follow-up proof candidate (same shape as the D4-1 suspended-*user* proof) |
| WG-TST-MEM-INT-002 disabled Identity User loses effective access | Covered (NEW) | D4-1 characterization (account + workspace scope) + `CreateWorkspace_WhenActorUserIsSuspended_FailsClosedBeforeHandlerEffects` over real PostgreSQL; `EvaluationCount == 1` proves single evaluation fail-closed |
| WG-TST-MEM-X-001 removed member historical attribution preserved | Covered | `Remove` mutates status to `Removed`, never deletes the `workspace_members` row; authorship/attribution in downstream contexts unaffected (WGREQ023) |

## Suite evidence (all green, post-change, SDK 9.0.313)

| Suite | Result | Note |
|---|---|---|
| Solution build (`backend.slnx`) | 17 projects, 0 errors, 55 warnings | |
| Application.Tests | 573 passed | baseline 571 → +2 (suspended reactivate + removed-reject handler tests) |
| Integration.Tests | 343 passed | baseline 340 → +3 (2 user-gate characterization + 1 suspended-user negative); pre-existing EV pipeline seed fixed to coherent actor identity |
| Domain.Tests | 2576 passed | unchanged |
| Architecture.Tests | 410 passed | unchanged |
| API.Tests | 256 passed | `WorkspaceMemberDto` shape + endpoint contract green |
| Infrastructure.Tests | 134 passed | unchanged |

## Phase 4 decisions

### D4-A — User-operational failure policy (WG-MEM-008 / WGREQ022) — DECIDED + IMPLEMENTED

Identity owns "is this User operational for protected product operations". Workspace access must follow current identity/actor validity, not membership history.

- New central `AccessFacts.UserOperational` fact (`identity.users` exists, not soft-deleted, `status IN ('Active','PendingVerification')`), enforced in `AccessPolicyEngine` for Account/Workspace/Resource scopes → `Forbidden` before handler effects.
- Same failure policy as D3-B: central Application access control, **not** per-handler checks, **not** Identity/session revocation, **not** inside member commands.
- `PendingVerification` stays allowed (invitation accept is pending-policy-correct); a future Identity status gains fail-closed behavior by default (deny — not allow), satisfying WG-MEM-008.

### D4-B — AddMember target identity validation (WGREQ016) — DECIDED + IMPLEMENTED

- `AddMemberCommandHandler` MUST validate the target User exists (`IActorLookupService` → `NotFoundException(nameof(User), request.UserId)`); a membership must never be created for a phantom identity.
- Group membership (through future Team/Space invites) still relies on the member-summary inventory from Identity when threaded through; direct subject validation is the current contract.

### D4-C — Self-leave (WG-MEM-005 / WGREQ018) — DEFERRED

No self-leave command exists. Owner/admin self-leave and last-required-administrator behavior are undefined product semantics. Do not invent owner behavior (SPEC §25: "Do not invent owner behavior"). Re-open when an explicit product decision defines self-leave.

### D4-D — Membership concurrency hardening (WG-MEM-010) — DEFERRED

- Last-owner/`activeOwnerCount` guards are enforced in-domain and fail closed on single-read semantics (`WorkspaceOwnerRules`), but `activeOwnerCount` is computed outside the aggregate (`RemoveMember.cs:48–49`) with no serialization; two concurrent owner removals can both observe `activeOwnerCount == 2` and remove both.
- Concurrent duplicate add is only prevented by the DB unique index; handler-level race mapping (clean `BusinessRuleException` vs EF unique-violation) is not yet defined.
- Deferred to the concurrency-hardening phase (WGREQ144/145/WGREQ019 machinery) together with `WG-TST-MEM-INF-001` and `WG-TST-MEM-CONC-001`.

### D4-E — Membership state-change authorization action (WG-MEM-006) — RECORDED

`SuspendMember`/`ActivateMember` currently reuse `PermissionAction.RemoveMember` and `AddMember` uses `InviteMember` as placeholders. These are TRANSITION until Phase 8 owns built-in roles/actions (WG-ROLE-DEC-001). No Phase 4 blocker.

## Follow-up findings

- **WG-FIND-401** — `AddMember.cs:61` passes a literal `activeOwnerCount: 1` into `ChangeRole` on the suspend→reactivate path. It fails closed (demoting an Owner always throws "Cannot downgrade the last owner" even when other owners exist), which is safe but over-restrictive; resolve with correct owner-count computation in WG-MEM-010.
- **WG-FIND-402** — Explicit full-pipeline negative for a suspended *workspace member* (as opposed to suspended *User*) is not yet a dedicated integration test; the mechanism is proven by SQL col 5 (`status='Active'`) + grant revocation + characterization. Candidate follow-up proof for the concurrency/Phase 8 slice.
- **WG-FIND-301/302/303** (Phase 3) — unchanged ownership; RLS workspace-schema policy, DB FK, stub lifecycle consumers remain open follow-ups. `WorkspacesStubConsumers` stays TRANSITION per D3-A.

## Phase 4 exit

WorkspaceMember reaches **D4** for the scoped release:

1. identity/containment/lifecycle/events/uniqueness audited (WG-MEM-001/002);
2. add/remove protected and validated (WG-MEM-003/004);
3. state changes deterministic, no stale access (WG-MEM-006/007/008);
4. query contract exposes membership status, hides Identity private state, excludes Removed (WG-MEM-009);
5. decision gaps WG-MEM-005/010 deferred with explicit owners;
6. full backend suites green (Application 573 / Domain 2576 / Architecture 410 / Integration 343 / API 256 / Infrastructure 134).

Recorded decisions D4-A..D4-E; findings WG-FIND-401/402 carried forward. Phase 4 CLOSED.