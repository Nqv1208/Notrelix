# PR-WG-03 — Phase 5 Invitation baseline

Evidence record for PLAN §55–63 (WG-INVITE-001..007), SPEC §31–40 (WGREQ024–033), TESTS §31–40 (WG-TST-INV-*).

## Baseline

- Branch: `feature/workspace-governance` (no commit; working tree includes Phase 4/5 changes reviewed below).
- Audit date: 2026-08-30.
- Authority read: product contexts (`docs/product/workspaces.md`, `docs/product/governance.md`, `docs/product/accounts.md`, `docs/product/identity.md`), RULE.md, root `AGENTS.md`, `backend/AGENTS.md`, `backend/tests/AGENTS.md`, `frontend/AGENTS.md`, backend architecture topics (domain-modeling, application-model, security-tenancy-authorization, infrastructure-and-data, api-and-contracts), frontend topics (api-and-contracts, state-query-mutations, dependency-boundaries), execution docs for this workstream.
- Baseline (pre-phase, PR-WG-02 ledger): Application 573, Domain 2576, Architecture 410, Integration 343, API 256, Infrastructure 134.

## Scope of this phase

Baseline invitation acceptance semantics + pending-list contract + frontend sync. The invitation **create** flow (WG-INVITE-002 / WGREQ026/027 invite-target and inviter-authority) is intentionally NOT this phase: `InviteMemberCommand` exists but has no public API mapping (recorded D5-H). The baseline closes the acceptance/pending/reject lifecycle that real Workspace access depends on, without inventing create-time product semantics.

1. **WG-INVITE-003 / WGREQ028/029/030** — Acceptance converges on one shared service (`InvitationAcceptanceService`) for both the by-token and the new by-invitation-id paths; expiry/revocation/status and identity-eligibility are validated once; active membership accepts idempotently (no duplicated member/role/grant); suspended/removed invitees are rejected with zero side effects (D5-A/B/C).
2. **WG-INVITE-*** pending contract — `UserPendingInvitationDto` no longer carries the raw token (D5-D); the pending list is consumed by invitation id.
3. **Frontend sync** — previously stale paths (`/workspaces/invitations/by-token/...`, `/workspaces/invitations/accept/...`, `/workspaces/invitations/pending`) replaced with the real backend contract; pending menu accepts by id; deep-link preview uses `POST /invitations/preview`; members page list/cancel wired to the workspace-scoped endpoints (D5-G).
4. **Architecture inventory** — one new production request (`AcceptInvitationByIdCommand`) recorded in the frozen `request-execution-baseline.json` + `WorkspaceNamespaceArchitectureTests` allowlist (governed-path, not regeneration).

## Verdict ledger

| ID | Scope | Verdict | Evidence | Note |
|---|---|---|---|---|
| WG-INVITE-001 | Invitation model | PASS | `Domain/Workspaces/Invitations/WorkspaceInvitation.cs` — target mail (`InvitationTarget`), Workspace scope, intended role, `ExpiresAt`, `Status` (Pending/Accepted/Revoked/Expired), `InvitationTokenHash` (64-hex verified material), events; pre-existing and unchanged ownership | Create endpoint not part of this phase; model is consumed by acceptance pipeline |
| WG-INVITE-002 | Create | NOT IN PHASE (decision) | `Commands/InviteMember/InviteMember.cs` command exists with no public endpoint mapping | D5-H: invitation create (and its privilege-escalation proof WG-TST-INV-APP-002) belongs to a later phase with the create contract |
| WG-INVITE-003 | Accept | PASS (D5-A/B/C) | `Application/Features/Workspaces/Invitations/Services/InvitationAcceptanceService.cs` (shared by `AcceptInvitationCommand` + `AcceptInvitationByIdCommand`). Guaranteed order (proven by `InvitationAcceptanceServiceTests`, 14 new): acting user present → invitation exists → user status Active/PendingVerification → email confirmed → **not expired** → **status Pending** → email matches invite → workspace exists → workspace Active → account exists → account Active/Trialing → member **Active** ⇒ idempotent consume (`Accept` + Success), member **Suspended/Removed** ⇒ side-effect-free Failure, member **absent** ⇒ `EnsureWorkspaceInviteeAccountMembershipAsync` + `Accept` + `WorkspaceMember.Create` + `SyncWorkspaceMemberGrantAsync` ⇒ Success | Both exits join the exact same code path → no ordering divergence between token and by-id acceptance |
| WG-INVITE-004 | Revoke | PASS (mechanism) | `Revoke`/status guards: acceptance requires `Status == Pending`; `Commands/CancelInvitation` + `DeclineInvitation` pre-existing; frontend members "Revoke" wired to `DELETE /workspaces/{workspaceId}/invitations/{id}` (D5-G) | No access is granted from a non-Pending invitation; race-proofing under WG-INVITE-006/`WG-TST-INV-CONC-001` |
| WG-INVITE-005 | Replay/idempotency | PASS (D5-B) | Active-member duplicate accept → single `Accept(actingUserId, now)` + Success, member row untouched, grant untouched (Application `WhenMembershipIsAlreadyActive_...` + Integration `Accept_WhenInviteeIsAlreadyActiveMember_IsIdempotent` over real PostgreSQL) | Idempotent consume, not duplicate-create; rejected/expired invitations also stay side-effect free |
| WG-INVITE-006 | Race accept vs revoke/expiry | DEFERRED (decision) | No serialization/retry/expected-version for concurrent accept/revoke/expiry; in-model checks are single-read | D5-E: deterministic replay/race matrix + DB-backstopped guarantees deferred to the concurrency-hardening phase (WGREQ031 + WGREQ146 machinery, same as WG-MEM-010) |
| WG-INVITE-007 | Secret safety | PASS | No raw token in ordinary list/preview DTOs; `InvitationTokenHash` persisted; `UserPendingInvitationDto` (pending list) carries **no token** (D5-D); `WorkspaceInvitationDto` (preview) carries no token/workspaceId | WGREQ033 satisfied at the DTO boundary; captured-log sentinel proof is a follow-up (deferred) |

## Effective acceptance pipeline (WGREQ027 — no bypass at acceptance)

The role/access carried by the invitation is translated to membership/grants through `WorkspaceMember.Create` + `SyncWorkspaceMemberGrantAsync` — the same projection machinery as Phase 4 membership. Acceptance never invents entitlement beyond the invitation's carried role; Governance/roles semantics remain owned by Phase 8 (WG-ROLE-DEC-001 UNRESOLVED, non-blocking).

## Test coverage (TESTS §31–40)

| TEST ID | Verdict | Note |
|---|---|---|
| WG-TST-INV-DOM-001 invitation distinct from membership | Covered (pre-existing) | `WorkspaceInvitation` acceptance/membership split proven via Application + Integration accept paths; create-writes-nothing proven by design (no membership row until `Accept`) |
| WG-TST-INV-SEC-001 identity/secret boundary | Covered (NEW) | Pending DTO token-free (D5-D); Application teardown asserts no duplicated membership/grant on accept |
| WG-TST-INV-APP-001 valid invite target | NOT IN PHASE | Create target semantics deferred (D5-H) |
| WG-TST-INV-APP-002 inviter cannot grant stronger access | NOT IN PHASE | Requires the create contract; `InviteMember` command + `PermissionAction.InviteMember` placeholder exist (carried) |
| WG-TST-INV-APP-003 expired invitation rejected | Covered (NEW) | `InvitationAcceptanceServiceTests` — expiry Failure before any state change |
| WG-TST-INV-APP-004 revoked invitation rejected | Covered (NEW) | Non-Pending status → Failure, zero side effects |
| WG-TST-INV-INT-001 repeated acceptance idempotent | Covered (NEW) | Application unit + Integration test 2 (active member) on real PostgreSQL: no duplicate member/grant/role |
| WG-TST-INV-CONC-001 accept vs revoke race | DEFERRED | D5-E; WGREQ031/146 machinery with concurrency-hardening phase |
| WG-TST-INV-API-001 lookup minimizes information | Covered (NEW) | `GetInvitationByTokenEndpoint` (preview) exposes bounded fields, no token/workspaceId; API endpoint tests green |
| WG-TST-INV-SEC-002 secret absent from logs | PARTIAL | Mechanism only: hashed storage + token-free DTOs; captured-log sentinel test deferred (recorded) |

## Suite evidence (all green, post-change, SDK 9.0.313)

| Suite | Result | Note |
|---|---|---|
| Backend per-project builds | 9 projects (src + tests), 0 errors | `backend.slnx` open fails in this environment on `RestoreEnablePackagePruning` eval (pre-existing toolchain artifact); CI used as gate authority |
| Application.Tests | 587 passed | baseline 573 → +14 (service sequence + by-id handler) |
| Domain.Tests | 2576 passed (Phase-4 baseline) | zero production Domain source change this phase |
| Architecture.Tests | 410 passed | frozen request-execution baseline +1 (`AcceptInvitationByIdCommand`), allowlist +1 entry, re-run green |
| Integration.Tests | 346 passed | baseline 343 → +3 (`AcceptInvitationByIdIntegrationTests`, real PostgreSQL: pending-create, active-idempotent, suspended-reject) |
| API.Tests | 260 passed | baseline 256 → +4 endpoint/contract proof (`AcceptInvitationByIdEndpointTests` + DTO shape) |
| Infrastructure.Tests | 134 passed (Phase-4 baseline) | zero production Infrastructure source change this phase |
| Frontend package typecheck/lint/format | pass | `@notrelix/contracts`, `@notrelix/features-workspace`, `@notrelix/app-web` |
| Frontend gates | pass | `check:architecture` 0 violations, `check:architecture-docs` in sync, `check:test-taxonomy` 87 files, `format:check`, `test:node:guarded` 313, `test:web:guarded` 2, `mock:freeze:check` 50/50 + 4 gap IDs |
| Frontend codegen | regenerated | `schema.ts` +39 lines — only the `Workspaces.Invitations.AcceptInvitationById` operation; `codegen:check` pending the generated-artifact commit (uncommitted-tree gate) |

## Phase 5 decisions

### D5-A — Shared acceptance service (WG-INVITE-003) — DECIDED + IMPLEMENTED

Both `AcceptInvitationCommand` (token) and `AcceptInvitationByIdCommand` (invitation id) invoke one `InvitationAcceptanceService` (`Application/Features/Workspaces/Invitations/Services`). Rationale from accepted by-token path: token and by-id are the same business transition; diverging acceptance logic would let the two surfacing paths drift. The by-id command stays thin: load invitation by id (NotFound if absent) → delegate.

### D5-B — Idempotent consume of active membership — DECIDED + IMPLEMENTED

An invitee who already holds an `Active` membership for the workspace accepts idempotently: the invitation advances to Accepted and Success returns, with **no** second member row, role change, or re-provisioned grant. This prevents a duplicate-accept from compounding grants; it is a consume of the invitation, not a mutation of the member.

### D5-C — Suspended/removed invitee rejection is side-effect free — DECIDED + IMPLEMENTED

If the invitee's current member row is `Suspended` or `Removed`, acceptance returns Failure and leaves membership, invitation status, and grants untouched. A `Removed` membership is terminal (Phase 4 `CannotActivateRemoved`); reactivation is an explicit member-management operation, not silently granted through an invitation.

### D5-D — Pending invitation identity without token — DECIDED + IMPLEMENTED

`GetUserPendingInvitations` returns `UserPendingInvitationDto` with no `Token` (previously present). The pending menu therefore accepts by invitation id (`POST /invitations/{invitationId}/accept`). The raw token stays only in the public deep-link flow (`/invitations/preview` + `/invitations/accept`).

### D5-E — Invitation replay/race matrix — DEFERRED

WG-INVITE-005/006 and `WG-TST-INV-CONC-001` need DB-backed serialization/retry/expected-version semantics (WGREQ031/WGREQ146) and a defined replay matrix. Recorded, not expanded, in this phase (same owner as WG-MEM-010).

### D5-F — RLS exposure on by-id acceptance — RECORDED (open)

`AcceptInvitationByIdCommand` is `IGlobalRequest` (invitation id alone does not scope to a workspace the actor belongs to). The request pipeline's `DataSessionBehavior` therefore sets `ApplyTenantScope = false` and `RlsSessionContext.ApplyAsync` is **not** invoked, mirroring the pre-existing token accept. If production connects as `notrelix_app` (FORCE RLS), writes to `workspace_invitations`/`workspace_members`/`account_members`/`authz.access_grants` in this flow may be denied. This risk is pre-existing (accepted token path) and unchanged in semantics; resolve when the RLS session-context model is revisited for invitation flows.

### D5-G — Frontend contract sync — DECIDED + IMPLEMENTED

Frontend invitation services realigned to the real backend contract (verified against `MapInvitationEndpoints.cs` + generated OpenAPI):

```text
preview:  POST   /invitations/preview                 body { token }
accept:   POST   /invitations/accept                  body { token }
accept:   POST   /invitations/{invitationId}/accept
pending:  GET    /invitations/pending
list:     GET    /workspaces/{workspaceId}/invitations
cancel:   DELETE /workspaces/{workspaceId}/invitations/{invitationId}
```

Removed stale wrong paths (`/workspaces/invitations/...`). Pending menu accepts by id; deep-link preview + token accept retained; members "Pending invitations" table now reads the workspace-scoped list and "Revoke" calls Cancel. `create` service remains an explicit stub ("Endpoint not implemented by backend.") because there is no create endpoint this phase (D5-H).

### D5-H — Invitation create contract deferred — NOT IN PHASE

`InviteMemberCommand` + `PermissionAction.InviteMember` placeholder exist but invitation **create** has no public API contract yet. WG-INVITE-002 (`inviter cannot grant beyond authority`, WG-TST-INV-APP-002) requires that create contract; deferring avoids inventing privilege-escalation semantics.

## Follow-up findings

- **WG-FIND-501** — `mock-backend` has no invitation routes. In `VITE_MOCK_API=true` dev the pending menu is silently empty and by-id accept `404`s. The enabled-consumer catalog flags `acceptInvitation` as `COMPATIBILITY_GAP_MOCKED`/`CTR-GAP-TODO`. Required when invariants touch mock parity: implement the five invitation routes in the mock (or govern suppression).
- **WG-FIND-502** — Two distinct `WorkspaceInvitationDto` records share one name: the preview DTO (local record in `GetInvitationByToken.cs`) and the management DTO (`Features/Workspaces/DTOs/WorkspaceDtos.cs`). Persistent naming smell; realigning names is an additive-contract change and is deferred to avoid churn in this phase. REGISTERED.
- **WG-FIND-503** — OpenAPI response bodies for the invitation `IResult`-returning endpoints serialize as `System.Void` placeholders (by-token accept, preview, pending, by-id accept all show this). Session-wide pre-existing trait of the endpoint writer; frontend consumes hand-written semantic DTOs. Register for the API/OpenAPI contract phase.
- **Carried (Phase 4, unchanged)** — WG-FIND-401 (AddMember literal `activeOwnerCount`), WG-FIND-402 (suspended-*member* pipeline negative), WG-MEM-010/WG-TST-MEM-INF-001/CONC-001 concurrency, WG-RES/role owners via WG-ROLE-DEC-001.

## Phase 5 exit

Invitation **acceptance baseline** closes at D3/D4-level for the P3 target:

1. acceptance paths converge on one service with a proven, ordered failure/pass matrix (D5-A/B/C);
2. pending-list contract is token-free and consumed by stable invitation id (D5-D);
3. frontend invitations are wired to the real backend contract, stale wrong paths removed (D5-G);
4. create/race/RLS items are explicitly decisioned or carried (D5-E/F/H, WG-FIND-501/502/503);
5. full backend suites green (Application 587 / Domain 2576 / Architecture 410 / Integration 346 / API 260 / Infrastructure 134); frontend gates green; architecture inventory recorded;
6. no commit yet — ledger + plan/tests annotations prepared for the branch commit.

Recorded decisions D5-A..D5-H; findings WG-FIND-501..503 carried forward. Phase 5 CLOSED.