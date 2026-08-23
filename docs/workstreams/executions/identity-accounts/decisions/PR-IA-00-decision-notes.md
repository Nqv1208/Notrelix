# PR-IA-00 — Decision notes (Phase 0/1/2 evidence)

> Status: accepted for Phase 1–2 execution
> Baseline: branch `develop` @ `5db7ec68d7cbb2385607eab6f96d7f8a69b10918`
> Scope: docs-only. No production code changes.

## 1. Account ownership decision — RETAIN

```text
Current representations:
  - Domain aggregate: Notrelix.Domain.Accounts.Accounts.Account
    (backend/src/Notrelix.Domain/Accounts/)
  - Members: AccountMember, roles AccountRole (Owner/Admin/Member/...)
  - Domain contexts: Invitations, Domains, IdentityProviders, Scim,
    WorkspaceRoutes, Regions, Rules/AccountOwnerRules
  - Application: Features/Accounts/ (Abstractions, Services, Provisioning)
  - Persistence: account.* schema (accounts, account_members, ...)
  - RLS: 007_policies_platform.sql — account schema policies
  - No Account API endpoints exist yet
Canonical business meaning: SaaS organization/tenant boundary; owns
  membership, invitations, domains, identity providers, SCIM, workspace routes.
  Identity context proves "who is acting"; it does not own membership/authz.
Selected action: RETAIN
Why:
  - Canonical Account aggregate already exists in Domain under Accounts context
  - Per-request account access verification exists and reads account_members
    (TenantBootstrapStore.VerifyAccountAccessAsync)
  - Product authority (docs/product/accounts.md, docs/product/identity.md)
    confirms Accounts owns the organization boundary
  - Downstream contract is clean: only AcceptInvitation consumes Accounts
    ports cross-context (IA-INV-007)
Affected layers: none (no move)
Affected tables: none
Affected consumers: none
Migration required: no
Architecture decision required: no
```

Plan/spec premises claiming "no top-level Accounts Domain module" are
classified `DOC_STALE` and corrected in this PR.

## 2. IA-PLAN-STOP-015 — RLS access-grant runtime writer absent

Registered in the PLAN stop-condition registry.

Evidence:

```text
writer of authz.access_grants:     InitDb.cs seed only
Application writer:                none
Domain event consumer:             none
SQL trigger / stored procedure:    none
migration backfill:                none
AccountMembershipProvisioner:      writes account_members only
AccountProvisioningService:        writes account + owner member only
RLS predicate source:              authz.access_grants (003 helpers, 007–010)
```

Impact: accounts/members created at runtime have no grants; RLS and the
bootstrap membership check under RLS deny them whenever the application role
is enforced. Development works only because the connection is a table owner
(bypasses RLS).

Decision needed at Phase 5 (Account / tenant isolation):

```text
Who owns the grant lifecycle (create/update/revoke) for runtime members?
- Account context projection/consumer of AccountMember events?
- direct write in Account membership provisioning?
- replacement predicate source?
```

Until decided, do not add grant writes inside PR-IA-01..04 and do not claim
RLS-enforced tenant isolation.

## 3. Certification amendment — CORE-CERT-006 enforcement guard

Added to certification.md: tenant-isolation tests must run with a real
enforcement role (`notrelix_app`), `Rls:SetSessionContext` enabled, policies
asserted, deny-path proven. Owner-bypass green is invalid evidence.

## 4. Backlog findings (recorded, not actioned)

| ID | Finding | Evidence | Severity | Assign |
|---|---|---|---|---|
| IA-FIND-001 | No bulk session revocation on Deactivate/Suspend (User.cs emits domain events only; no UserSession invalidation path found) | Domain/Identity/Users/User.cs, Sessions/UserSession.cs | High | Phase 4 session lifecycle |
| IA-FIND-002 | CSRF double-submit cookie SameSite=Strict + Secure=prod drops cookie for cross-subdomain SPA | Infrastructure/Auth/Csrf/CsrfProtector.cs | Medium | Phase 8 / security UX |
| IA-FIND-003 | Six Application feature folders are empty placeholders (ApiTokens, Credentials, SSO, Security, Sessions, Users) | Features/Identity/ | Low | confirm intent via IA-INV-003 |
| IA-FIND-004 | No Account API endpoints; downstream only via AcceptInvitation | API/Endpoints | Low | P1 gate CORE-CERT-007 / PR-IA-04 |

## 5. Phase 3 verification — User / Actor / Profile (IA-USER-*, IA-ACTOR-*, IA-PROFILE-*)

Status: STABLE with two recorded gaps. Verified against IAREQ001–011, IAREQ099, IAREQ123–125.

### User aggregate (IAREQ001–005, IAREQ099) — PASS

```text
Identity:      Guid aggregate (SoftDeletableAggregateRoot); stable across auth mechanisms
Lifecycle:     Active / Inactive / Suspended / PendingVerification
               + soft delete/restore (DeletedAt/DeletedBy/DeleteReason) — no hard delete,
               historical attribution preserved
Uniqueness:    idx_users_email (unique) + idx_users_normalized_email (unique) —
               DB-level, concurrency-safe (IAREQ099)
OAuth subject: unique (Provider, ProviderId) — DB-level
Email:         Email value object + NormalizedEmail (trim, lowercase)
Invariants:    name max 100, login time cannot move backwards, OAuth provider
               unique per user, semantic no-ops (same hash/password/status)
Events:        Registered, ProfileUpdated, EmailChanged, PasswordChanged, LoggedIn,
               Activated, Deactivated, Suspended, EmailConfirmed, OAuth link/unlink/
               token-rotated, Deleted, Restored
Tests:         UserTests, UserActorSemanticsTests, UserVersionTests cover lifecycle,
               no-op, audit-actor, version semantics
```

Note: `User_ShouldNotHaveSessionManagement` confirms Domain design keeps session
state OUT of the User aggregate — session revocation must be wired through
Application/events (Phase 4), not added to User.

### Actor abstraction (IAREQ006–009) — GAP recorded

```text
Application:   IExecutionContextAccessor / ExecutionContext (Common/Context) —
               Application-owned, no HTTP concepts
Infrastructure: HttpRequestContextMiddleware fills from JWT claims after
               UseAuthentication; ICurrentTenantContext + RlsSessionContext
Domain:        no HttpContext dependency
```

Gap: no principal-type classification (IAREQ007/008). ExecutionContext has only
`UserId` + `IsSystemContext` bool; an API-token principal is not distinguishable
from a browser-session principal. `IActorLookupService` port exists. Decision
needed for PR-IA-01: add principal-type, or defer until API-token capability
(Phase 12) — SPEC requires only types present in canonical architecture.

### Profile boundary (IAREQ010–011, IAREQ123–125) — PASS

```text
UserProfile:   timezone/locale/theme/preferences — personal preferences only,
               no workspace-member metadata, no sensitive/security fields
User:          Name + Avatar (personal identity fields) — same Identity context
Endpoints:     PUT /api/v1/profile (name/avatar), PUT /api/v1/profile/email —
               self-service; UpdateEmail resets EmailConfirmed (verified)
Authorization: self-service via authenticated principal; no admin mutation path
               exists; email update separated from generic profile update
```

Note: personal profile fields live in two places (User.Name/Avatar vs
UserProfile preferences) — acceptable placement, no move required; recorded for
IA-PROFILE-001.

### Phase 3 exit — MET (two open items)

```text
User identity stable:        yes (Guid, no ID-type change)
Actor abstraction stable:    partial — principal-type gap (decision for PR-IA-01)
Profile boundary stable:     yes
Uniqueness understood:       yes (email x2, oauth provider+subject, DB-level)
Lifecycle preserves history: yes (soft delete, no cascades)
```

## 6. IA-ACTOR-PRINCIPAL-001 — principal-type decision (PR-IA-01 outcome)

```text
Decision: do NOT add a PrincipalType enum now.

Reason:
- UserSession is the only principal type actually used at runtime today
- ApiToken capability belongs to Phase 12 and must not be designed ahead of it
- System/background principal added only when a concrete background/system
  use case requiring Actor semantics is inventoried and proven
- IAREQ007 requires explicit supported principal types — it does not require
  an enum to exist before multiple principal behaviors exist
- no types created merely to prepare for future capability

Supported now:
- authenticated User/session

Deferred:
- ApiToken: Phase 12
- System/background: only when a concrete use case exists

PR-IA-01 result: NO PRODUCTION CODE CHANGE.
- User/Actor contract audited against IAREQ001–IAREQ009 and meets them
- Actor contract verified: no implementation gap
- PR-IA-01 closed as no-production-code-change (no empty/artificial PR)
- recorded as approved decision IA-ACTOR-PRINCIPAL-001
Next: PR-IA-02 / Phase 4 Authentication + Session
```

## 7. Phase 4 verification — Authentication / Session core (IA-AUTH-*, IA-SESSION-*)

Status: core flow works; ONE material gap (IA-SESSION-007 / IAREQ098) becomes
the PR-IA-02 scope.

### Auth mechanisms (IA-AUTH-001) — PASS

```text
Login (local credentials)          — exists, works
Registration                      — exists
RefreshToken                      — exists, rotation
Logout                            — exists, revoke + blacklist
Forgot/ResetPassword              — exists
EmailVerification                 — exists
OAuth start/complete              — exists
SSO / Credentials / Sessions App folders — placeholders (later phases)
```

### Local credentials (IA-AUTH-002, IAREQ093–098) — PASS with observation

```text
normalization:    trim + lowercase (Login.cs:41)
password verify:  BCrypt (WorkFactor 12)
failure classes:  generic "Invalid email or password" for not-found/wrong-password
                  (enumeration resistance OK for those two)
lock/rate:        ADR-004 rate limiting middleware (not re-verified in Phase 4)
session creation: IAuthSessionIssuer
```

Observation: `Login.cs:52-62` returns distinct messages for
Inactive/Suspended accounts — leaks account state to a caller who knows the
email. Needs policy decision (IAREQ014 "beyond accepted policy").

### Session domain (IA-SESSION-001, IAREQ016) — PASS

`UserSession` aggregate: Create (hash, expiry>created), UpdateRefreshToken
(Active only), Revoke (expired cannot be revoked), Expire (revoked cannot
expire), no-ops, events, audit, version. `SessionStatus` = Active/Revoked/Expired.

### Session creation / bootstrap / expiry (IA-SESSION-002..004, IAREQ017-019) — PASS

```text
creation:  AuthSessionIssuer — JWT access (1h) + refresh (30d) + UserSession row
bootstrap: GetBootstrap + GetCurrentUser endpoints exist (IAREQ018)
expiry:    JWT lifetime validated, ClockSkew=0; refresh path checks
           Status==Active && ExpiresAt>now
```

### Logout (IA-SESSION-005, IAREQ020) — PASS

```text
Revoke session row + blacklist access JWT jti in Redis (TTL = remaining
lifetime); transactional request; no success-before-durable-revoke
```

### Revoke all/select (IA-SESSION-006) — N/A

No product-defined revoke-all/select operation; per plan "only implement
operations already product-defined". Not implemented — correct.

### User disable interaction (IA-SESSION-007, IAREQ004/IAREQ098) — GAP (PR-IA-02)

Login blocks Inactive/Suspended (Login.cs:52-62), BUT:

```text
- RefreshToken handler does NOT check user.Status → a deactivated/suspended
  user can keep refreshing: new 30-day session + new 1h JWT indefinitely
- JWT validation is lifetime/signature only (no status claim) → existing
  access tokens stay valid until exp
- Deactivate/Suspend/UpdatePassword never revoke UserSession rows (no event
  consumer; UserSessionRevoked only from logout/refresh rotation)
- no policy defines the intended effect (SPEC IAREQ004 requires explicit
  effects on sessions)
```

### Revocation cache (IA-SESSION-008, IAREQ117/IAREQ102) — PASS

```text
JWT blacklist: Redis, TTL = remaining lifetime (no stale window by design;
  depends on Redis availability)
refresh race:  rotation revokes old session within one transactional request;
  a second refresh with the same token fails (session no longer Active)
```

### Phase 4 exit — BLOCKED on PR-IA-02

```text
User → Actor → Session flow:      works end-to-end (login/refresh/logout)
revocation works:                 logout yes; user-disable interaction NO
```

## 8. PR-IA-02 scope (Session/Auth core) — proposed

```text
1. RefreshToken: reject when user Status != Active (blocks deactivated/suspended
   users from refreshing) — IAREQ098
2. Decide + implement session effect on Deactivate/Suspend/UpdatePassword:
   - option A: event consumer revokes all active sessions for the user
   - option B: policy = soft block via status check only (sessions expire
     naturally; access tokens live until exp)
3. Resolve login enumeration observation (distinct inactive/suspended messages)
   per IAREQ014 policy
4. Tests: IA-TST session paths incl. disable-while-refreshing
```

## 9. PR-IA-02 implementation — part 1 DONE (refresh block)

```text
Implemented (backend/src/Notrelix.Application/.../RefreshToken/RefreshToken.cs):
- after loading user, if user.Status is not UserStatus.Active:
  → session.Revoke(now)  (presented session dies immediately)
  → Result.Failure("Refresh token is invalid or expired")
  (message intentionally identical to invalid/expired — no state enumeration)
- deleted users: already invisible to queries via global soft-delete filter
  (ApplicationDbContext OnModelCreating: DeletedAt == null) → "User not found"

Tests (RefreshTokenTests.cs, 4 total):
- Handle_WhenUserIsInactive_RevokesSessionAndFails   → PASS
- Handle_WhenUserIsSuspended_RevokesSessionAndFails  → PASS
- (existing valid-session + session-not-found kept)  → PASS
Command: dotnet test Notrelix.Application.Tests --filter FullyQualifiedName~RefreshTokenTests
Result: 4/4 passed

Remaining PR-IA-02 decisions (blocking):
- revoke-all effect on Deactivate/Suspend/UpdatePassword: option A (event
  consumer revokes all sessions) vs option B (soft block via status check,
  current behavior). Note: no Application caller of Deactivate/Suspend exists
  today; UserDeactivatedConsumer is a log-only stub. Option B is already
  covered by the refresh block above.
- login enumeration messages (Login.cs distinct Inactive/Suspended) per IAREQ014
```

## 10. IA-SESSION-SECURITY-001 — implemented (PR-IA-02 part 2)

```text
Decision (user, 2026-08-13): revocation watermark, not boolean blacklist.

Policy locked:
- Deactivate/Suspend target: MUST immediately invalidate access + revoke all
  Active UserSessions (when Application commands exist)
- Password change classified: authenticated self-change → revoke others (current
  session retention only if explicitly supported); reset/recovery/admin reset →
  revoke all
- No dead event consumers while no Application caller exists
- Current refresh soft-block = defense in depth only, NOT final enforcement

Implemented:
1. IJwtBlacklistService += GetUserRevokedBeforeAsync(Guid) / RevokeUserBeforeAsync(
   Guid, DateTimeOffset, TimeSpan)  [Application contract]
2. JwtBlacklistService: Redis key auth:user-revoked-before:{userId}, value =
   UTC cutoff ("O" roundtrip), TTL caller-supplied; zero/negative TTL no-write
3. AccessTokenRevocationEvaluator (Infrastructure/Auth/Jwt): reject when
   watermark exists AND (iat missing → fail-closed OR token.iat <= watermark)
4. JwtBearerEvents.OnTokenValidated: existing jti blacklist check retained, then
   sub→userId→watermark read→evaluator → context.Fail("User access has been revoked")
5. Login normalization (IAREQ014): unknown-user / wrong-password / Inactive /
   Suspended → identical public failure "Invalid email or password" (same
   Result/HTTP contract); password verify runs in ALL paths (dummy BCrypt hash
   WF12 for unknown user → timing-equalized); status reason only in internal
   telemetry (logger severity), never in response; no session issued on any
   failure path

Verified security gap closed for access tokens:
- logout jti blacklist:   already rejected (existing)            [KEEP]
- deactivate/suspend:     watermark → tokens issued at/before cutoff
                          rejected on next request                [NEW]

Tests:
- Infrastructure.Tests Auth: JwtBlacklistServiceTests (4: key format, TTL write,
  zero-TTL no-write, roundtrip parse) + AccessTokenRevocationEvaluatorTests (5:
  after/before/equal/iat-missing-fail-closed/no-watermark) → 9/9 PASS
- Application.Tests: LoginTests (5: generic contract + VerifyPassword once for
  unknown user + no session on Inactive/Suspended) + RefreshTokenTests (4:
  valid/not-found/inactive-revoke/suspended-revoke) → 9/9 PASS
- dotnet build API slnx graph: 0 errors
Commands: dotnet test --filter on both projects; dotnet build Notrelix.API

NOT covered (recorded, not actioned):
- host-seam integration test of OnTokenValidated wiring: NotrelixApiFactory
  replaces JwtBearer with TestAuthHandler → cannot exercise real JWT events in
  API.Tests; covered by Infrastructure unit tests + wiring is a single lambda
- actual RevokeUserBeforeAsync caller: MUST be invoked only by future
  authoritative Deactivate/Suspend/ResetPassword admin operations (per decision)
- PasswordChanged event policy implementation: deferred until commands exist

Remaining PR-IA-02 items: none blocking. Phase 4 exit now: MET for session/auth
core (access-token revocation gap closed; revoke-all deferred to future commands
by decision IA-SESSION-SECURITY-001).

## 11. IA-ACC-GRANT-LIFECYCLE-001 — grant lifecycle ownership (resolves IA-PLAN-STOP-015)

Phase 5 decision (2026-08-13): the grant lifecycle is owned by a synchronous,
same-transaction projection — NOT by an event consumer.

```text
Contract:      IAccessGrantProjectionService (Application/Common/Tenancy)
Implementation: AccessGrantProjectionService (Infrastructure/Data/Authz),
               operates on the same scoped ApplicationDbContext as the handler
Wired into:    AccountProvisioningService (owner on registration)
               AccountMembershipProvisioner (invitee account membership)
               AcceptInvitation, AddMember, ActivateMember, SuspendMember,
               RemoveMember, UpdateMemberRole, TransferOwnership,
               CreateWorkspace, ProvisionPersonalWorkspace
Semantics:     upsert account-level grant (workspace_id NULL) for account
               membership; upsert workspace grant for workspace membership;
               revoke = RevokedAt set; re-add = Activate clears RevokedAt;
               role/admin flags via AccessGrantProjectionMapping
```

Why synchronous projection instead of a domain-event consumer:

```text
- RLS predicates read authz.access_grants on the SAME request that mutates
  membership; eventual consistency would authorize/deny against stale state
- NRX-010: durable security state must commit with the protected mutation;
  no success-before-durable-grant window is acceptable
- no new Platform consumer/message identity/retry semantics needed
- the projection is pure state mapping; membership aggregates stay canonical
```

Deliberate non-goals (recorded, not gaps):

```text
- Workspace archive/delete does NOT revoke grants: grant lifecycle follows
  MEMBERSHIP lifecycle; workspace lifecycle visibility stays an application
  concern (EF query filters). Restore/unarchive then needs no grant repair.
- SuspendMember/RemoveMember both revoke via RevokedAt; membership_status
  column stays "Active" on the revoked row. RLS blocks on revoked_at first;
  the row is a projection, membership rows remain authoritative.
```

Supporting changes:

```text
- Migration 20260813092355_AccessGrantSyncProjection:
  1) backfill authz.access_grants from account.account_members (Active,
     not deleted) and workspace.workspace_members (Active) with NOT EXISTS
     guards — fixes databases upgraded from before the projection existed
  2) partial unique index ux_access_grants_account_user_account_level
     (account_id, user_id) WHERE workspace_id IS NULL
- InitDb.SeedAuthzGrantsAsync now also projects account-level grants for
  AccountMembers (previously workspace grants only). Without this, the
  seeded owner failed ops.is_account_admin under enforced RLS and could not
  UPDATE account.accounts or other account-admin rows.
```

## 12. PR-IA-03 implementation record — runtime access-grant writer (Phase 5)

```text
Work unit: PR-IA-03 part 1 — runtime access-grant projection
SPEC requirements: IAREQ034/IAREQ035 (tenant isolation), IAREQ074–IAREQ078
  (data ownership: RLS predicate source gains a runtime writer)
PLAN work units: IA-ACC-009, IA-PLAN-STOP-015 resolution
Source inspected: authz RLS scripts 003/004/007, AccessGrant + configuration,
  all membership write paths (IA-INV cross-check: no unwired writer remains;
  seed InitDb handled separately), AccountMember/WorkspaceMember EF configs
Files changed (production):
  Application: Common/Tenancy/IAccessGrantProjectionService.cs (new)
    + projection calls in AccountProvisioningService,
    AccountMembershipProvisioner, AcceptInvitation, AddMember,
    ActivateMember, SuspendMember, RemoveMember, UpdateMemberRole,
    TransferOwnership, CreateWorkspace, ProvisionPersonalWorkspace
  Infrastructure: Data/Authz/AccessGrantProjectionService.cs (new),
    Data/Authz/AccessGrantProjectionMapping.cs (new),
    Data/Authz/AccessGrant.cs (Activate added), AccessGrantConfiguration
    (account-level partial unique index), migration
    20260813092355_AccessGrantSyncProjection (+ backfill),
    PersistenceRegistration (DI), Data/Seed/InitDb.cs (account-level grants)
Domain impact: none (AccessGrant is an Infrastructure projection entity;
  no Domain changes)
Migration: backfill + partial unique index in existing uncommitted
  migration file (amended, not a new migration); DBs where the migration
  already ran need make dev-reset or a manual re-run of the backfill
  [2026-08-13 later consolidated into SchemaV2Baseline — see §13]
Tests added:
  Integration Data/Authz/AccessGrantProjectionTests.cs (7): create/update/
  revoke/reactivate/separate-grant semantics against real PostgreSQL
  Integration RlsRuntimeEnforcementTests.RuntimeMembershipCreation_
  WritesGrant_AndEnforcesUnderAppRole: handler + projection writes grant;
  enforced notrelix_app role sees own rows, unrelated user sees none
  Application AddMember/SuspendMember handler tests: projection called
  with correct account/workspace/user/role (incl. re-add path)
Commands run:
  dotnet build backend.slnx → 0 errors
  dotnet test Notrelix.Application.Tests → 619/619 passed
  dotnet test Notrelix.Integration.Tests --filter
    "AccessGrantProjectionTests|RlsRuntimeEnforcementTests" → 20/20 passed
Stop condition resolved: IA-PLAN-STOP-015 — runtime writer exists;
  CORE-CERT-006 enforcement guard evidence now includes the runtime-writer
  end-to-end proof under the notrelix_app role
Architecture/product decision required: none
Full-suite verification (2026-08-13, working tree on develop @ 5db7ec68):
  Architecture 371/371, Domain 2558/2558, Application 619/619,
  Infrastructure 89/89, API 219/219, Platform 147/147,
  Integration 244/244 (incl. SeedDataInitialiserTests with the new
  account-level seed grants, MigrationSmokeTests with backfill + index)
Phase 5 exit: MET — canonical Account retained, current Account resolution
  verified (Phase 2), tenant isolation proven under enforced app role,
  no dual Account truth, downstream references identified (IA-INV-007)
Next: Phase 6 — P1 downstream producer contract (IA-X-*) / PR-IA-04
```

## 13. Migration consolidation — single SchemaV2Baseline (2026-08-13)

User decision (dev phase): keep exactly ONE migration file. The two
incremental migrations were merged into `20260702093805_SchemaV2Baseline`
and their files deleted:

```text
20260806160643_EnforceIdempotencyRecordStatePayloadInvariant (DELETED)
  → baseline now creates ops.idempotency_records directly with the strict
    ck_idempotency_records_completed_result (Processing-empty OR
    Completed-populated). The old DO $$ legacy-row guard is dropped: a
    consolidated baseline only ever runs against empty schemas, so there
    are no legacy rows to guard.
20260813092355_AccessGrantSyncProjection (DELETED)
  → ux_access_grants_account_user_account_level partial unique index moved
    into the baseline index section. The membership backfill SQL is DROPPED:
    it existed for pre-projection upgraded databases, but after
    consolidation the baseline only runs fresh (members tables empty, seed
    writes the grants; runtime projection maintains them afterwards).
Baseline Designer now carries the final model; ModelSnapshot unchanged
(already final). Down() needed no edits (drops tables, taking
indexes/constraints with them).
```

Consequences recorded:

```text
- Obsolete test removed: IdempotencyStoreIntegrationTests.
  EnforceIdempotencyRecordStatePayloadInvariantMigration_InvalidLegacyRow_
  FailsBeforeConstraintReplacement (+ CreateMigrationGuardContext helper).
  Its contract (multi-step upgrade from the lax constraint, FZ-INF-IDEM-
  SCHEMA-01 guard) no longer exists by design. The enforced contract itself
  stays proven by IDEM_SCHEMA_003_CheckConstraints_EnforceStateContract,
  which passes against the consolidated baseline.
- Any existing dev database carries orphaned history rows for the deleted
  migration IDs (harmless — EF ignores history entries unknown to the
  assembly) but will NOT gain the merged schema changes; use make dev-reset
  for a clean baseline. This is accepted for the dev phase.
```

Verification:

```text
dotnet build backend.slnx → 0 errors
MigrationSmokeTests 4/4 (143 tables, schema state correct from the single
  baseline) and IdempotencyStoreIntegrationTests 18/18 (strict constraint
  enforced from baseline) on fresh Testcontainers PostgreSQL
```

## 14. Phase 5 residual closure — IA-ACC-006 / IA-ACC-007 / IA-ACC-008 (2026-08-14)

```text
IA-ACC-006 tenant spoofing protection — CLOSED
  Application-level seam: TenantBootstrapBehavior never trusts a client-
  controlled Account/Workspace ID:
    IWorkspaceRequest  → ResolveWorkspaceAccessAsync(workspaceId, actorUserId)
                         → CanAccess=false ⇒ ForbiddenException (TenantBootstrapBehavior.cs)
    IAccountRequest    → marker is metadata-only ("AccountId is resolved from
                         tenant context, not from request"); account comes from
                         ICurrentTenantContext.AccountId (session/header-selected)
                         then VerifyAccountAccessAsync (active AccountMember,
                         TenantBootstrapStore.cs:64-74) ⇒ Forbidden if absent
  New tests: Application.Tests/Common/Behaviors/TenantBootstrapBehaviorTests.cs
    (5, added 2026-08-14):
    - WorkspaceRequest_RequestedWorkspaceOfOtherTenant_ThrowsForbidden_HandlerNotCalled
    - WorkspaceRequest_AuthorizedActor_ResolvesScopeAndSetsTenantContext
    - AccountRequest_AccountIdComesFromTenantContext_NotFromRequestPayload
    - AccountRequest_NoAccountSelected_ThrowsAccountSelectionRequired_HandlerNotCalled
    - AccountRequest_ActorWithoutActiveMembership_ThrowsForbidden_HandlerNotCalled
  DB-level negative proof already exists: RlsRuntimeEnforcementTests
    AppRole_CrossAccount_GrantInOneAccount_DoesNotSeeOtherAccount.
  Note: IAccountAccessEvaluator (Infrastructure/Services/AccountAccessEvaluator.cs)
  remains an unused stub (DI-registered only, no callers) and is NOT the
  enforcement path; no change made to avoid speculative abstraction (NRX-006).

IA-ACC-007 Account context in background work — CLOSED (inventory, no code change)
  Inventory (2026-08-14):
    BackgroundServices: QueuedJobWorker (N8nDispatchJob only),
      OutboxDispatcher (batches messaging.outbox_messages, FOR UPDATE SKIP
      LOCKED; MessagingProcessedEvent carries ActorUserId),
      EmailDispatcher (email outbox)
    Messaging: MassTransit (InMemory/RabbitMQ) with TenantContextConsumeFilter
      (restores tenant context per message) + DeduplicationConsumeFilter;
      ConsumerRegistrySetup.cs catalogs all consumers; live Identity downstream
      consumer = WorkspaceProvisioningConsumer (RegistrationCompleted)
    Platform ConsumerHost/MessagingRuntime: present but NOT wired into the
      running graph (Infrastructure uses MassTransit)
  No "null tenant = global" convenience: RLS scope enum has explicit
    app/worker/system/background; RlsRuntimeEnforcementTests prove
    BackgroundScope_NoGrant_FailsClosed and
    BackgroundScope_WithGrant_SeesOwnRowsOnly_NoBypass, and
    WorkerAndSystemScopes_BypassWorkspacePolicies_SeeAll is policy-gated.
  No consumer touches Identity private entities (sessions/password/OAuth/MFA).

IA-ACC-008 Account lifecycle downstream contract — defined (forward-looking)
  Current state: no Account disable/delete command exists today
  (Features/Accounts has Abstractions/Provisioning/Services only).
  Ownership statement (backend AGENTS rule 39):
    Accounts owns lifecycle + identity. Downstream effects are transported by
    committed integration events consumed by each owning context. No consumer
    behavior is implemented inside the Account transaction.
  Contract (when disable/delete commands land):
    Workspaces           → member removal/revocation already handled by
                           membership-driven grant projection (§11); workspace
                           visibility stays an application concern (EF filters)
    Billing              → entitlement/lifecycle fact via AccountLifecycle event;
                           Billing owns Plan/Subscription semantics
    Automation/Integrations → disable rules/connections, consumer-owned,
                           after-commit via events
    Analytics            → retention/privacy policy per §15 IA-X-006
  Recorded: Account aggregate holds plan_code scalar + AccountPlanCodeChanged-
  DomainEvent with NO current producer callers — open item: rehome plan_code
  producer to Billing when Billing integration lands (see §15 IA-X-003).
  Deliberate non-goal (unchanged from §11): Workspace archive/delete does not
  revoke grants; restore needs no grant repair.
```

Re-verification on current tree (2026-08-14, 5 tests added since §12):

```text
dotnet build backend.slnx → 0 errors, 0 warnings
dotnet test backend.slnx → 4251 tests passed, 7 projects (16 pre-existing
  CS8602 warnings in GlobalExceptionHandlerTests, unrelated)
Application.Tests 624/624 (619 per §12 + 5 new TenantBootstrapBehaviorTests)
Integration AccessGrantProjectionTests|RlsRuntimeEnforcementTests → 20/20
Integration Register|OAuth|CreateWorkspace|WorkspaceLifecycle → 10/10
```

## 15. Phase 6 — P1 downstream producer contract (IA-X-001..007 + PR-IA-04) (2026-08-14)

Phase 6 is a verification + contract-definition phase; current source already
satisfies every IA-X requirement, so NO production code change was required.
Evidence per work unit:

```text
IA-X-001 stable User/Actor reference contract — VERIFIED
  Downstream contexts consume the stable User identity as bare Guid (UserId on
  WorkspaceMember/TeamMember/Workspace.OwnerId/Invitation.InvitedBy and the
  account-scoped ApiToken). Display facts via IActorLookupService
  (ActorSnapshot: UserId/Name/AvatarUrl, Infrastructure/Services/
  ActorLookupService.cs). Availability facts via IIdentityUserLookupService
  (IdentityUserSnapshot, used by AcceptInvitation for status gating).
  No actor references Identity private entities: zero matches for
  Notrelix.Domain.Identity/{Users,Sessions,Mfa,...} in
  Features/Workspaces and Features/WorkManagement; only AuthSessionIssuer
  (Common/Security/Auth) touches the User entity, inside Identity scope.
  Requirements IAREQ037-039 / IAREQ068-073: consumer side fully industrialised.

IA-X-002 Workspace/Governance handoff — VERIFIED
  Required contract surface already consumed:
    Actor/User ID = Guid; Account ID = Guid (tenant key on every entity);
    Account scope semantics = active AccountMembership + access-grant
    projection (workspace_id NULL account-level rows);
    User availability = IdentityUserSnapshot status (Active or
    PendingVerification) checked at AcceptInvitation.
  Workspace does NOT reference password/OAuth/MFA/session entities (verified
  by source grep; only workspace setting flag EnforceMfa exists, which is a
  policy toggle, not an Identity MFA entity).

IA-X-003 Billing handoff — VERIFIED with one recorded open item
  Billable identity = AccountId (Billing module keyed by account:
  Infrastructure/Billing/DatabaseSubscriptionChecker.cs).
  Lifecycle fact = account status consumed via read ports + future
  AccountLifecycle integration event (§14 IA-ACC-008).
  Billing-admin actor identity = ICurrentRequestContext.UserId.
  No Plan/Subscription aggregate in Accounts (Domain/Accounts has no such
  entities).
  OPEN (recorded, not actioned): Account.PlanCode +
  AccountPlanCodeChangedDomainEvent ('accounts.account-plan-code-changed')
  exist with no producer callers today. Contract decision required when Billing
  integration lands: either Billing publishes the fact and Accounts consumes
  it as a reference, or Accounts remains the producer of a billing-owned
  reference code. Do not implement unilaterally (NRX-006 / no-invent policy).

IA-X-004 WorkManagement/Documents handoff — VERIFIED
  Consumers reference stable AccountId/UserId scalars only
  (WorkManagement 126 Domain files, Documents 22 Domain files carry
  AccountId); no Identity private reads anywhere.

IA-X-005 background/system actor handoff — VERIFIED (inventory in §14
  IA-ACC-007)
  System/background semantics are explicit scopes (ICurrentTenantContext
  SetSystem/IsSystemContext + RLS scope enum app/worker/system/background),
  never fake User rows; outbox messages carry ActorUserId; tenant context is
  restored per message by TenantContextConsumeFilter. Platform ConsumerHost
  is dormant — recorded, no change (NRX-006).

IA-X-006 Analytics handoff — CONTRACT DEFINED
  Analytics (Domain+Application only today; no Infrastructure persistence)
  may reference AccountId/UserId as stable keys and consume aggregates
  (snapshots/metrics). Forbidden: email, name, avatar, password/OAuth/MFA/
  session payloads, access grants. Applies from the moment Analytics
  persistence lands; no source change needed today.

IA-X-007 integration contract tests handoff — VERIFIED (already documented)
  Required tests exist in identity-accounts.tests.md:
    IA-TST-X-ARCH-001 downstream does not reference Identity EF entities
    IA-TST-X-INT-001 Workspace consumes stable User/Actor/Account contract
    IA-TST-X-INT-002 mutable Profile does not corrupt downstream identity
    + "Account lifecycle downstream tests" section. Executable evidence:
    Architecture.Tests dependency gates (371/371) + RLS integration proof.

PR-IA-04 — producer contract / P1 core gate
  Downstream abstractions/events: EXIST (IdentityRegistrationCompleted-
  IntegrationEventV1 → WorkspaceProvisioningConsumer → ProvisionPersonal-
  WorkspaceCommand via outbox).
  Workspace/Governance integration proof: RuntimeMembershipCreation_Writes-
  Grant_AndEnforcesUnderAppRole (RlsRuntimeEnforcementTests) + 20/20 RLS tests.
  P1 core certification evidence: collected in §12/§14/§15; Phase 7 gates
  (IA-GATE-001..004) remain to be executed by the certification review pass —
  next workstream activity after this commit.

Phase 6 acceptance (§226: "Downstream consumer contracts no longer require
private Identity/Account persistence") — MET, verified by executable evidence
above.
```

## 16. Phase 7 — P1 core gate execution record (IA-GATE-001..004) (2026-08-14)

Baseline SHA `5db7ec68`, candidate SHA `48553a5c` (includes `aac87873`);
working tree exactly equals candidate. Full solution suite 4251/4251.

```text
IA-GATE-001 core source review — PASS
  Canonical stores: single users table (SchemaV2Baseline:2298 CreateTable;
    FKs at 3305/3338/5528 point to it); single accounts/account_members
    store; no private User/Identity persistence introduced downstream
    (grep-verified across Features/ during exploration; Architecture.Tests
    371/371 green).
  Pipeline ownership: tenant resolution + account verification stay at the
    Application seam (TenantBootstrapBehaviorTests, 5 facts); IAccountRequest
    remains metadata-only (account from ICurrentTenantContext, never from
    payload); IAccountAccessEvaluator remains an unused stub — recorded, not
    wired (NRX-006); no handler-local auth bypass added; no RLS weakening.
  Security baseline facts re-verified on candidate:
    - spoofing denial: cross-account + spoofed-account requests rejected
    - session revocation effective: watermark (user-revoked-before) honored
      on login and refresh (commit aac87873)
    - auth failure privacy: GlobalExceptionHandlerTests green
    - CSRF: CsrfValidationMiddleware (API/Middleware) + CsrfProtector
      (Infrastructure/Auth/Csrf) wired in API DI; API.Tests validations
    - secrets: no gitleaks-class scanner in CI (only CI JWT test key in
      be-ci.yml) — recorded as non-blocking debt, see §17
  Logout covered: Application Tests Features/Identity/Auth/Commands/Logout-
  Tests.cs + Integration Tests Auth/LogoutCommandHandlerTests.cs.
  Table checked: users single CreateTable; cookie/CSRF hits were artifacts in
  obj DLLs only — no source-level cookie auth.

IA-GATE-002 downstream smoke integration — PASS
  Execute: dotnet test Notrelix.Integration.Tests --filter
    "MigrationSmokeTests|SeedDataInitialiserTests|IdempotencyStoreIntegration-
    Tests|AccessGrantProjectionTests|RlsRuntimeEnforcementTests|Register-
    CommandHandlerTests|CompletedOAuthLoginCommandHandlerTests|CreateWorkspace-
    CommandHandlerTests|WorkspaceLifecycleTests"
  Result: 60/60 passed (31.2s), runtime role notrelix_app (not superuser).
  Proves: clean-DB + seed + strict idempotency constraint + grant projection
  under app role + RLS fail-closed + registration/oauth login/workspace
  lifecycle handlers + workspace provisioning consumer-adjacent paths.
  Contract-level compile proof: User/Actor/Account → Workspace/Governance
  compiles against approved abstractions (no EF/Identity imports; checked by
  IA-TST-X-ARCH-001 equivalent in Architecture.Tests).

IA-GATE-003 core migration proof — PASS
  - 143 tables created from single consolidated baseline; public schema
    empty after MigrationSmoke
  - IdempotencyStore 18/18: ck_idempotency_records_completed_result strict
    from baseline (upgrade-without-strict impossible)
  - SeedDataInitialiserTests green (seed users + account-level grants)
  - dotnet ef migrations has-pending-model-changes → "No changes have been
    made to the model since the last migration."
  - API startup / DI graph correctness covered by API.Tests + integration
    suites on candidate
  Upgrade-path policy: dev fresh-baseline (`make dev-reset`); incremental
  history consolidated (decision §13). Recorded as policy, not a blocker.

IA-GATE-004 open P2 — OPEN
  Milestone A marked P1 CORE CERTIFIED (certification.md §19 record);
  Workspace & Governance may start broad implementation; Identity continues
  secondary scope (Phases 8-12).
```

Certification records — CORE-CERT-001..010 all written with status/evidence/
reviewer/decision-date into identity-accounts.certification.md §19. Session
certified VERIFIED (D4) — rationale recorded (downstream P1 consumers depend
on session identity per-request, not on user-disable batch revocation;
D5 upgrade scoped to security hardening). Debt recorded non-blocking
(bulk revocation on disable IA-FIND-001, secret-scan CI job, Session D4).

Phase 7 acceptance: "Identity core evidence is reviewable and Milestone A gate
decision is recorded" — MET. No source change in this phase; documentation is
in the gitignored workstreams tree (inventory: certification.md §19/§20/§21
records + this §16). No commits created in Phase 7.
```

## 17. Phase 8 — Registration & Credentials execution record (2026-08-14)

Baseline `48553a5c` (P1 core candidate); candidate HEAD `aa0c5ad`.
Full solution suite 4275/4275 (7 projects) — 24 tests added (baseline 4251);
`dotnet ef migrations has-pending-model-changes` → none (no schema change).

```text
Decisions (user-confirmed 2026-08-14):
- password minimum length 8 applies to ALL credential-setting flows
  (Register + ChangePassword + ResetPassword) via shared constant
  PasswordPolicy.MinimumLength (Application/Common/Security/Auth) — a
  reset/change path without the rule would be a policy bypass
- ChangePassword implemented in Identity/Auth/Commands/ChangePassword;
  empty Credentials/ scaffold (Commands/DTOs/Queries .gitkeep) deleted
- ChangePassword revokes ALL active sessions including the current one,
  mirroring ResetPassword (session rows + revocation watermark via
  JwtBlacklistService.RevokeUserBeforeAsync, 24h TTL) — closing the
  "change credentials while attacker session stays valid" window

Commits (WHAT-only messages):
- 2e6702c feat(identity): add authenticated change-password flow with
  session revocation — command/handler/validator + PasswordPolicy +
  /api/v1/auth/change-password endpoint (MapAuthenticatedPost +
  AuthStrictByIp rate limit) + Credentials/ scaffold removal +
  ChangePasswordTests (Application: success/revoke/blacklist/email,
  invalid current no-mutation, user-not-found) + ChangePasswordEndpointTests
  (401/weak-400/missing-current-400) + PasswordHasherTests (roundtrip,
  wrong-false, unique salts, malformed-false)
- a73c863 feat(identity): enforce minimum password length on
  credential-setting flows — PasswordPolicy.MinimumLength into
  RegisterCommandValidator + ResetPasswordCommandValidator (replaces
  hardcoded 8); PublicAuthEndpointTests weak-register now 400 (was OK),
  min-length-8 register OK, weak-reset 400
- b75e6ab fix(identity): map registration email-uniqueness race to conflict
  result — ExceptionMappingBehavior catches DbUpdateException unique
  violation (duplicate key / SQLSTATE 23505) → ConflictException →
  ProblemDetailsMapper 409; behavior tests (5 facts) +
  RegisterDuplicateRaceTests on real PostgreSQL (two concurrent same-email
  registrations → second save unique violation → maps to ConflictException)
- aa0c5ad test(identity): add OTP single-use and attempt-limit proof —
  OtpServiceTests (single-use delete after success, wrong code no delete,
  attempts>5 deletes code and rejects, attempts reset on generate)
```

Rationale recorded:

- Race path rationale: RegisterCommandHandler pre-checks normalized email but
  the DB unique index is the authoritative backstop; the interleaving window
  previously surfaced as a 500 (unmapped DbUpdateException). Mapping unique
  violations to ConflictException at the outermost pipeline behavior covers
  the registration race without new schema or handler logic. Non-unique
  DbUpdateExceptions still propagate to the generic error path.
- Credential verification pattern: ChangePassword verifies the current
  password through IPasswordHasher (BCrypt) with a constant dummy hash on
  unknown user to equalize timing; email notification is best-effort
  (try/catch, non-failing), consistent with ResetPassword.

Certification records — certification.md §24.1 (registration VERIFIED D4:
exactly-once + race handled + Accounts-owned bootstrap + no hidden cascade +
enumeration resistance) and §25.1 (credentials VERIFIED D4: hasher proof +
approved-proof update + OTP replay resistance + no raw credential storage +
session impact defined) written with evidence and commit SHAs.

Phase 8 acceptance: "each release-scoped secondary capability reaches
D4/D5 individually without breaking core" — Registration VERIFIED (D4),
Credentials VERIFIED (D4); core gates unaffected (no schema change, pipeline
order untouched, full suite green). Records are in the gitignored workstreams
tree. Commits 2e6702c/a73c863/b75e6ab/aa0c5ad remain unpushed on develop.
```

## 18. Phase 9 — OAuth & SSO audit record (2026-08-15)

Baseline HEAD `f91c203b`; source audit against spec IAREQ040–047
(spec.md §48–55), plan items IA-OAUTH-001..008 + IA-SSO-001..004
(plan.md §81–92), tests.md §63–77, product docs/identity.md §26–31.

### Decisions (recorded BEFORE implementation, user-reviewed 2026-08-15)

IA-OAUTH-UNLINK-001 — canonical security policy for IAREQ045:

```text
An active User MUST NOT self-unlink their last primary authentication
method. Primary methods = password credential (HasPasswordCredential) +
linked OAuth provider identities. MFA methods (Phase 10) are secondary
and do not count toward this invariant.
Enforced in Domain: User.UnlinkOAuthAccount throws
Identity_User_LastPrimaryAuthMethod before mutation (failure-atomic).
Domain state: User.HasPasswordCredential (bool) — creation semantics
explicit at every call site (no default in User.Create):
  RegisterCommandHandler → true
  CompleteOAuthLogin.CreateNewUser → false (OAuth-only, sentinel hash)
  InitDb seed → true
  UpdatePassword (ChangePassword/ResetPassword) → true
Login uses the flag while keeping timing-safe hash verification:
OAuth-only user verifies against the constant dummy hash (timing
identical to unknown-user path), then generic failure.
```

IA-OAUTH-MIGRATION-001 — migration strategy for the new column:

```text
Schema change folded into the consolidated SchemaV2Baseline (Designer +
ModelSnapshot), per the recorded dev-phase decision (see §13,
2026-08-13): a consolidated baseline only ever runs against EMPTY
schemas — therefore NO existing users exist at migration time and
backfill is vacuous; DEFAULT false never applies to a row created by a
Domain factory (all creation sets the flag explicitly).
Consequences documented: existing dev DBs do not gain the column; use
make dev-reset (accepted for the dev phase, §13). If this repo ever
applies migrations to a populated/production database, BE-OPS-DATA-004
requires a NEW forward migration with an explicit backfill decision
(pre-migration rows' password-credential state is not inferable from
stored hashes) — obligation recorded here, not silent.
```

### IA-OAUTH-001 — OAuth vs SSO classification — RESOLVED

```text
OAuth = social/external identity linking/login — REAL, implemented end-to-end
        Domain OAuthAccount aggregate + Application start/complete commands +
        Infrastructure (options/provider-client/state-store) + API endpoints +
        Integration/Domain tests
SSO  = planned placeholder — Identity/SSO is .gitkeep-only in
        Application/Infrastructure/API; no IAREQ SSO requirement exists in
        spec (grep SSO = 0); product ID-009 defers enterprise IdP to Accounts
NOT MERGED — OAuth and SSO remain separate folders/semantics
```

### IA-OAUTH-002 — provider contract inventory — PASS

```text
providers:  Google, GitHub, Microsoft, Apple (Domain OAuthProvider enum)
protocol:   OAuth2/OIDC authorization-code; PKCE verifier/challenge generated
            (Google currently; per-provider applicability)
subject:    provider-owned stable ProviderId (OAuthSubject); email NOT used
            as identity key (IAREQ040)
scopes:     per-provider OAuthProviderConfig.Scopes
callback:   per-provider RedirectUri + frontend FrontendSuccessUrl/FailureUrl
state:      Redis OAuthStateStore, key = SHA256(state), 10-min TTL,
            consume-once
tokens:     access/refresh persisted via SecretRef columns (access_token_ref /
            refresh_token_ref); current login flows persist NO token (null)
profile:    ExternalOAuthProfile via OAuthProviderClient (raw jsonb snapshot)
link/unlink: Domain User.LinkOAuthAccount/UnlinkOAuthAccount/UpdateOAuthProfile/
            RotateOAuthToken — Application link/unlink commands MISSING (gap)
tests:      StartOAuthLoginCommandHandlerTests, CompletedOAuthLoginCommand-
            HandlerTests (PostgreSQL), Domain OAuth tests (events, snapshot,
            secret-leakage, failure-atomicity, actor semantics)
```

### IA-OAUTH-003 — OAuth start — PASS (one observation)

StartOAuthLoginCommand: state + nonce (crypto-random 32B), PKCE S256 for
Google, redirect URI per provider, 10-min expiry, state stored
hash-keyed. `ReturnUrl` is captured but never consumed by the callback
(redirect target is server-config FrontendSuccessUrl) — dead data, no
open-redirect risk; recorded, not actioned.

### IA-OAUTH-004 — OAuth callback — PASS (one material defect found)

Validated: provider enum, state consume-once (replay-safe, expiry via TTL),
provider mismatch, PKCE code-verifier + nonce, redirect URI, email-verified
gate for account creation. Subject → canonical User via unique
(Provider, ProviderId) index.

### IA-OAUTH-005 — collision matrix — ONE VIOLATION (SOURCE_DEBT)

```text
subject already linked to this User            → login existing account   OK
subject linked to another User                 → login that account       OK (login flow)
same email, different provider subject         → AUTO-LINK + login        VIOLATION
User already has this provider linked          → Domain guard rejects     OK
provider changed email                         → subject-authoritative    OK
callback replay                                → consume-once rejected    OK
```

Violation: `CompleteOAuthLoginCommandHandler.LinkToExistingUser` auto-links a
provider subject to an existing User solely on provider-verified email match.
Spec IAREQ044: "MUST NOT auto-merge identities merely because emails match
unless canonical policy explicitly permits it"; product ID-008: email alone is
not sufficient proof; §28 account-takeover defense lists "email-only merge".
No canonical decision permits it. Account-takeover vector: attacker registers
a provider identity with the victim's email, then login callback links the
attacker's subject into the victim's account.

RESOLUTION (decision IA-OAUTH-COLLISION-001): login callback MUST NOT
auto-link by email. Email match with no existing provider link → stable
failure directing the user to their existing sign-in method or the
authenticated link flow. Integration test codifying auto-link
(Handle_WhenUserExistsWithVerifiedEmail_ShouldAutoLinkAndLogin) is invalid
relative to policy → replaced with rejection proof.

### IA-OAUTH-006 — link flow — MISSING (implemented this phase)

Authenticated User links a provider identity: `StartOAuthLinkCommand`
(IAuthenticatedRequest) + `CompleteOAuthLinkCommand` (IAuthenticatedRequest,
ITransactionalRequest). State carries OAuthFlowKind (Login|Link) + BoundUserId
so a login state cannot complete a link and vice versa (IA-TST-OAUTH-SEC-001
"state bound to another flow"). Link callback issues NO new session
(already authenticated). Subject collision: linked to self → no-op success;
linked to another User → conflict, never reassignment (IA-TST-OAUTH-SEC-003).

### IA-OAUTH-007 — unlink flow — MISSING (implemented this phase)

`UnlinkOAuthCommand` (IAuthenticatedRequest, ITransactionalRequest). Last
viable method protection (IAREQ045 / §28, decision IA-OAUTH-UNLINK-001):
rejected when removal would leave zero primary methods. Requires a
"password credential viable" signal — added Domain flag
`User.HasPasswordCredential` (explicit at creation; UpdatePassword →
true) + consolidated-baseline column `has_password_credential`
(decision IA-OAUTH-MIGRATION-001). Invariant enforced in Domain
`User.UnlinkOAuthAccount`.

### IA-OAUTH-008 — provider-token protection — PASS (no action)

Current flows persist no provider tokens (login/link pass null). SecretRef
stores a reference string (value-object, converter), never raw secret in
ordinary logs/events (OAuth events carry providerId only; OAuthSecretLeakage-
Tests enforce). If a future flow persists tokens, SecretRef must reference
vault-managed secrets.

### IA-SSO-001..004 — SSO — classified, deferred (no stop)

```text
IA-SSO-001: SSO = planned placeholder (no source to audit)
IA-SSO-002: ownership = Identity authenticates, Account owns IdP config
            (ID-009); Account-level IdP model not defined in this workstream
            → plan stop condition applies → SSO NOT implemented in Phase 9
IA-SSO-003/004: N/A until SSO exists
```

Certification record written in certification.md (Phase 9 section, D-levels
per evidence). Frontend contract regenerated for new endpoints
(notrelix.v1.json + schema.ts).

## 19. Phase 13 record — API taxonomy / account-scope authz / self-service (2026-08-21)

Source baseline: develop @ c95baa2e (working tree uncommitted at write time).

### IA-API-003 — CSRF classification — DEFERRED (Platform contract change required)

Evidence gathered (source, not assumption):

```text
Backend:  CsrfValidationMiddleware + CsrfProtector expect header "X-CSRF-Token";
          gated by Security:Csrf:Enabled = false in base appsettings.json;
          no environment-specific appsettings overrides the Security:Csrf section
          → CSRF validation currently disabled in all environments.
Frontend: packages/foundation/contracts/src/client/csrf.ts reads
          meta[name="csrf-token"] or XSRF-TOKEN cookie;
          api-client.ts sends header "X-XSRF-TOKEN" on unsafe methods.
Mismatch: backend expects X-CSRF-Token, frontend sends X-XSRF-TOKEN;
          no token issuance/rotation endpoint or cookie contract exists.
```

Enabling CSRF today would break every browser mutation flow. Classification as
"requires CSRF" is a Platform/security contract change needing:

```text
1. header-name alignment decision (single canonical header)
2. token issuance + rotation contract (endpoint/cookie)
3. exempt inventory for public auth endpoints (login/refresh/register/OAuth callbacks)
4. rollout order + failure mode definition
```

Owner: Platform/security architecture. Per plan §118 no endpoint-local CSRF
conventions were implemented. Deferred with this evidence record.

### Register email-existence reveal — ACCEPTED tradeoff (product owner sign-off pending)

Registration responses distinguish existing emails (verified by
RegisterDuplicateRace integration tests). Enumeration-resistant alternative
(always-succeed + verification email) changes signup UX and requires a product
decision. Current behavior retained as accepted tradeoff; flagged for product
owner confirmation.

### Event inventory spot-check (IA-EVT) — earlier claims corrected

Earlier working notes claimed orphaned events. Re-verification against source
contradicts all three claims:

```text
identity.user-registered : producer UserRegisteredDomainEvent present;
                           consumer endpoint notrelix-identity-user-registered-v1
                           present; no v2 variant exists anywhere in source.
accounts.account-created : producer AccountCreatedDomainEvent present;
                           AccountId is required Guid with non-empty guard in
                           AccountScopedDomainEvent base ctor (not nullable).
identity.user-deactivated: producer UserDeactivatedDomainEvent + consumer
                           UserDeactivatedConsumer present; naming consistent.
```

Conclusion: no orphaned-event cleanup is self-evident from source; the earlier
claims were stale/inaccurate working notes and are superseded by this section.
Full IA-EVT consumer-inventory certification remains open work requiring
outbox/DLQ inspection across environments; not blocked by Phase 13.

### Self-service actor-is-self (IAREQ092) — implemented

GetCurrentUser/UpdateProfile/Logout derive the actor from ICurrentRequestContext;
commands carry no client-shaped UserId; IAuthenticatedRequest marker enforces
pipeline authentication. UpdateProfile validator dropped its UserId rule.

### Ad-hoc error shapes (IA-API-002 completion) — canonicalized

Nine endpoint-local ad-hoc error bodies replaced with canonical helpers:
EndpointExtensions.InvalidInput (400 validation.failed,
HttpValidationProblemDetails shape with errors._errors) for invalid
provider/purpose enum binding; EndpointExtensions.UnauthorizedProblem
(401 auth.unauthorized) for missing refresh cookie. Status codes unchanged;
undocumented `{ error }` bodies replaced by the documented ProblemDetails shape.

## 20. Phase 17 — Cross-team integration hardening closure (2026-08-23)

Source baseline: develop @ `450bea973307980ce03c4bc27b5f32c1ad6c91cf` (working
tree clean for this scope; pre-existing unrelated dirty file
`frontend/apps/marketing/vercel.json` + untracked skills dirs remain as found).

### P17-X-001 — frontend/browser contract handoff — VERIFIED

Required search executed (`rg "csrf_token|X-CSRF-Token|XSRF-TOKEN|X-XSRF-TOKEN|
getCsrfToken|Csrf|CSRF" frontend`, node_modules/dist excluded; 180 hits):

```text
SOURCE (all compliant with ADR-005, no competing convention):
  packages/foundation/contracts/src/client/csrf.ts
    → bootstrap GET auth/csrf + in-memory token + CSRF_HEADER=X-CSRF-Token;
      legacy conventions documented as removed/forbidden (comment only)
  packages/foundation/contracts/src/client/api-client.ts
    → csrfAwareFetch on unsafe browser requests, single-flight bootstrap,
      security.csrf_validation_failed ProblemDetails detection → clearToken +
      re-bootstrap recovery; refresh path CSRF-aware
  packages/foundation/contracts/src/generated/rest/schema.ts
    → generated /api/v1/auth/csrf operation (Identity.Auth.IssueCsrfToken),
      description references ADR-005 (regenerated artifact, not hand-written)
  packages/foundation/contracts/src/client/index.ts → public exports only
TESTS (negative legacy assertions):
  __tests__/csrf-transport.unit.test.ts asserts X-XSRF-TOKEN absent,
  no cookie/meta/storage discovery; source-scan guard over legacy strings
DOCS:
  docs/architecture/api-and-contracts.md §59–60 still describe the
  pre-closure drift as "current" → DOC_STALE (fix = Phase 19 IA-DOC scope)
  docs/decisions/FE-ADR-005-auth-session-model.md "Current CSRF mismatch
  evidence" section same classification → DOC_STALE (Phase 19)
MOBILE/MARKETING: zero hits — no competing transport outside contracts client.
```

Executable proof: `vitest run --config tooling/testing/vitest.node.config.ts
packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts`
→ **11/11 passed** (frontend @ 450bea97).

### P17-X-002 — Workspace/Governance authorization handoff — VERIFIED

```text
AccountRole references in Application/API (source grep):
  Common/Security/PermissionService.cs:185,199,208 → central policy engine
    (Owner bypass + frozen IAREQ090 baseline fallback incl.
    CreateWorkspace→Admin) — the ONLY authorization decision site
  Common/Tenancy/IAccessGrantProjectionService.cs +
  Features/Accounts/{Services,Provisioning} → grant projection writes
    (state mapping, not authorization decisions)
  Features/Workspaces, Features/Governance: ZERO AccountRole references
  Notrelix.API: ZERO references
Workspace/Governance consume the shared PermissionService/pipeline contract;
no downstream inspection of Identity handlers or AccountRole exists.
```

Executable proof: `dotnet test Notrelix.Integration.Tests --filter
"FullyQualifiedName~PermissionServiceTests"` → **33/33 passed** on real
PostgreSQL (frozen 5x2 role/action matrix, Governance deny precedence,
allow-grant, suspended/absent/wrong-account denials).

### P17-X-003 — event consumer handoff — VERIFIED

```text
ConsumerRegistrySetup.cs: every consumer definition carries explicit
  EventVersion (compound identity) + ConsumerMaturity metadata:
  18 Implemented / 26 Stub — stubs recorded AS STUB per plan §156.
Stable IDs/scope: TenantContextConsumeFilter restores tenant context per
  message; consumers carry stable Guid ActorUserId/AccountId facts.
No private Identity persistence: fresh grep over Application Features finds
  no downstream reference to UserSession/MfaMethod/OAuthAccount/password
  state outside Identity's own features (session queries live under
  Features/Identity/Sessions — Identity-owned self-service).
```

### P17-X-004 — v1/v2 migration fixture — VERIFIED

`tests/Notrelix.Infrastructure.Tests/Messaging/
IntegrationEventCatalogResolutionTests.cs:125` — controlled dual-version
fixture `test.versioned-fact` v1+v2 crossing ContractRegistry →
IntegrationEventCatalog → production serializer round-trip; v3 lookup throws;
no registry collision; no production event bumped.
Executable proof: filter `VersionedContractMigrationFixtureTests` →
**5/5 passed** (maps IA-TST-X-EVT-004 / IA-TST-EVT-MIG-001..002 /
IA-TST-MIG-EVT-002).

### P17-X-005 — operational event evidence handoff — NOT_APPLICABLE_UNTIL_DEPLOYMENT

No production environment exists at candidate SHA. Local staging/dev
operational evidence was collected in the P13-EVT-OPS record (outbox
Processed=2/Pending=0/Failed=0/DLQ=0, poison=0). Runbook trigger: first
production deployment re-executes the outbox/DLQ inspection per
P13-EVT-OPS-001 procedure.

### Phase 17 exit — MET

Cross-stack consumer (frontend) consumes only the ADR-005 protocol;
cross-context consumers use stable IDs/scope with explicit versions and
classified maturity; v1/v2 coexistence proven; operational evidence properly
deferred to deployment. The two
DOC_STALE doc sections are Phase 19 scope and do not constitute a consumer
dependency on pre-closure ambiguity.

## 21. Phase 18 — TESTS handoff closure (2026-08-23)

### IA-TEST-HO-001 — TESTS synced with closure implementation — VERIFIED

`identity-accounts.tests.md` contains 216 unique concrete `IA-TST-*` IDs.
Mandatory family presence (unique-ID count):

```text
IA-TST-CSRF-*          19
IA-TST-AUTHZ-*         13
IA-TST-EVT-INV-*        3   (+EVT-DOM/EVT-INT/EVT-PRIV/EVT-MIG/EVT-OPS)
IA-TST-EVT-SEC-*        1
IA-TST-EVT-VER-*        5
IA-TST-EVT-CONTRACT-*   4
IA-TST-MIG-*           11
IA-TST-OBS-*            6
IA-TST-REL-*            8
IA-TST-X-*             14
```

### IA-TEST-HO-002 — project/CI mapping present — VERIFIED

Existing canonical mapping sections cover every required field family-wide:

```text
tests.md §235–244 CI mapping → suite/job per test family
tests.md §259–273 requirement-by-requirement → test-family coverage
per-test definitions carry scenario/classification in their sections
```

No closure requirement remains represented only by "architecture/source
review": the residual gates §292–294 were implemented as executable
architecture tests (verified green below via the Architecture suite).

### IA-TEST-HO-003 — full regression at exact SHA 450bea97 — EXECUTED

Actual counts recorded (not carried forward):

```text
Notrelix.Architecture.Tests      398 passed / 0 failed / 0 skipped
Notrelix.Domain.Tests           2576 passed / 0 failed / 0 skipped
Notrelix.Application.Tests       649 passed / 0 failed / 0 skipped
Notrelix.Infrastructure.Tests    132 passed / 0 failed / 0 skipped
Notrelix.Platform.Tests          147 passed / 0 failed / 0 skipped
Notrelix.API.Tests               256 passed / 0 failed / 0 skipped
Notrelix.Integration.Tests       338 passed / 0 failed / 0 skipped
BACKEND TOTAL                   4496 passed

frontend pnpm test:node           72 files / 313 tests passed
  (includes contracts csrf-transport = IA-TST-CSRF-CLIENT-*/X-CSRF-001)
```

Focused Phase 17 proofs re-confirmed inside these suites:
PermissionServiceTests 33/33 (Integration),
VersionedContractMigrationFixtureTests 5/5 (Infrastructure),
Architecture suite includes IA-TST-TRACE-001 / CSRF-ARCH / EVT-CONTRACT gates.

### IA-TEST-HO-004 — non-zero execution — VERIFIED

Every suite above matched and executed real tests; one initial mis-targeted
filter (`PermissionServiceTests` against Application.Tests) returned zero and
was corrected to its actual home (Integration.Tests) before recording.
No zero-test green result is used as evidence anywhere in this handoff.

### Phase 18 exit — MET

TESTS has complete Phase 13–17 traceability; full regression executed at the
exact candidate SHA with no material missing test and no failure/skip.

## 22. Phase 19 — Documentation / generated-contract handoff closure (2026-08-23)

### IA-DOC-001 — CSRF authority alignment — DONE

```text
backend/docs/decisions/ADR-003-csrf-protection.md
  → status Superseded → ADR-005 (historical record preserved); no active
    XSRF/double-submit transport remains as source truth
backend/docs/decisions/ADR-005-csrf-cross-origin-bootstrap.md
  → Accepted; canonical bootstrap protocol
frontend/docs/architecture/api-and-contracts.md
  → §57 rewritten to describe the full accepted protocol (bootstrap GET
    auth/csrf, body+HttpOnly cookie, X-CSRF-Token from memory,
    security.csrf_validation_failed recovery);
  → §59 retitled "CSRF source alignment (closed)" — drift recorded as
    historical, repaired at Phase 13 closure;
  → §60 FE-API-030 reclassified RESOLVED with standing single-spelling rule
frontend/docs/decisions/FE-ADR-005-auth-session-model.md
  → "Current CSRF mismatch evidence" marked CLOSED with repair evidence;
  → "Evidence interpretation": CSRF wire spelling → RESOLVED
docs/generated/rule-index.md
  → regenerated through producer scripts/docs/generate-rule-index.mjs
    after §60 retitle; --check PASS (never hand-edited)
```

Both sides now describe the same ADR-005 protocol; remaining legacy-name
mentions in frontend docs are explicitly historical/resolution context.

### IA-DOC-002 — canonical OpenAPI evidence — VERIFIED

Fresh producer export (`dotnet run --project src/Notrelix.API --
--export-openapi`) compared semantically against committed
`backend/contracts/openapi/notrelix.v1.json` → **CLEAN** (no drift; bootstrap
operation was already part of the approved contract).

### IA-DOC-003 — event contract artifact — VERIFIED

`backend/contracts/events/notrelix.events.json` matches generated source
shape: `CanonicalManifest_MatchesGeneratedSourceShape` executed explicitly
(1/1 passed) in addition to full Architecture suite coverage.

### IA-DOC-004 — authorization architecture docs — DONE

`backend/docs/architecture/security-tenancy-authorization.md` BE-SEC-013
extended with the canonical chain:

```text
protected Application use case
→ request authorization contract
→ AuthorizationBehavior (central policy evaluation)
→ handler business logic
```

plus explicit statement that target-role business invariants are distinct
from current-actor authorization and that downstream contexts consume the
central policy engine without inspecting Identity handlers/role state.

### IA-DOC-005 — event versioning architecture docs — DONE

`backend/docs/architecture/platform-and-messaging.md` §106 Message evolution
extended with the accepted versioning contract: EventContractKey(Name,
Version) runtime identity, deterministic duplicate/unknown failure, v1/v2
coexistence, per-version schema baseline with same-version drift rejection,
consumer maturity metadata (Implemented/Stub/None), rollout order, and
outbox/DLQ drain before retirement. Section numbering preserved.

### IA-DOC-006 — workstream status from evidence — DONE

Execution state is tracked by certification records only: Milestone B +
Phase 13 DONE via P13-FINAL-01 (2026-08-22); Phases 14–19 records appended
as executed. plan.md frontmatter remains `active` until Phase 20 closes.

### Docs governance gates

```text
make docs-check → ALL PASS
  links: 124 files / 2911 targets / 0 error
  metadata: 86 canonical docs / unique ids
  authority: 87 required paths / 0 retired link
  rule-ids: 109 files / 2663 declarations / 2663 unique
  source-inventory: 5 backend projects / 9 frontend families / 3 hosts
  generated: 4 artifacts / 4 producer-owned drift checks executed
```

Note: an earlier session note mentioned "114 pre-existing violations";
current gate output shows zero violations — the earlier number referred to a
pre-refoundation state and is obsolete.

### Phase 19 exit — MET

No canonical document contradicts the implemented CSRF/authz/event closure
contracts.

## 23. Phase 20 — Final certification handoff (2026-08-23)

Full Scope Certification Record filled at certification.md §150 with actual
evidence (no prefilled PASS):

```text
Candidate:      develop @ 450bea973307980ce03c4bc27b5f32c1ad6c91cf
Migration head: 20260702093805_SchemaV2Baseline (unchanged)
OpenAPI sha256: f4391c79…c9758   drift CLEAN
Events  sha256: dda01452…fa0f4   drift CLEAN
Suites:         backend 4496 passed / frontend node 313 passed (all non-zero)
Closure table:  all P13 units DONE; EVT-OPS = NOT_APPLICABLE_UNTIL_DEPLOYMENT
Blocker rule:   no blocker remains (IA-CERT-HO-004 checklist recorded)
Decision:       IDENTITY & ACCOUNTS FULL SCOPE CERTIFIED (source-level);
                human sign-off pending; CSRF production enablement remains a
                deployment action per staged rollout P14-MIG-003
```

Honesty constraints honored: remote CI has not run on the (unpushed)
documentation commit; the record cites local executions at the exact source
SHA and marks remote CI as a post-commit action.

Workstream serial order complete:
P13-CLOSE-00 … P13-FINAL-01 → Phase 14 → 15 → 16 → 17 → 18 → 19 → 20.
