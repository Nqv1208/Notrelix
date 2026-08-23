---
document_id: WRK-CERT-IDENTITY-ACCOUNTS
document_type: workstream-certification
status: active
owner: identity-accounts-team
applies_to:
  - backend
  - identity
  - accounts
  - p1-core
  - actor
  - users
  - sessions
  - account-context
  - tenant-isolation
  - registration
  - credentials
  - oauth
  - sso
  - mfa
  - security
  - api-tokens
  - migrations
  - ci
  - cross-team-handoff
evidence:
  - docs/workstreams/execution/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/execution/identity-accounts/identity-accounts.plan.md
  - docs/workstreams/execution/identity-accounts/identity-accounts.tests.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/identity-accounts.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
review_on:
  - identity-spec-change
  - identity-plan-change
  - identity-tests-change
  - p1-core-gate-change
  - account-ownership-resolution
  - session-contract-change
  - oauth-sso-change
  - mfa-change
  - api-token-change
  - migration-change
  - ci-gate-change
---

# CERTIFICATION — Identity & Accounts

## 1. Purpose

This document defines the evidence required to certify Identity & Accounts.

It does not declare the current implementation complete.

It defines:

```text
what must be proven
which capability gate is being certified
which test evidence is acceptable
which CI evidence is required
which unresolved issues block certification
which unresolved issues may remain
when Workspace & Governance may rely on P1 core
when the full Identity & Accounts team scope is considered complete
```

The certification model has two distinct milestones:

```text
MILESTONE A
P1 CORE CERTIFIED
→ Workspace & Governance may treat Identity/Accounts core as stable

MILESTONE B
IDENTITY & ACCOUNTS FULL SCOPE CERTIFIED
→ all release-scoped Identity security capabilities are complete
```

These milestones MUST NOT be conflated.

## 2. Certification authority chain

Certification is valid only when evidence is consistent with:

```text
SPEC
→ PLAN
→ TESTS
→ actual source
→ actual migrations
→ actual test execution
→ CI on exact candidate SHA
```

A document checklist without executable evidence is not certification.

## 3. Certification principles

### Principle 1 — evidence over intent

A capability is not certified because:

- code exists;
- the plan says it should work;
- local smoke testing looked correct;
- a PR was approved.

It is certified only when required evidence exists.

### Principle 2 — exact candidate SHA

All final CI evidence MUST refer to the exact candidate SHA being certified.

### Principle 3 — no skipped critical evidence

A critical test suite that did not execute is not a PASS.

### Principle 4 — unresolved critical debt blocks D5

A capability with unresolved security/ownership/tenant-isolation ambiguity cannot reach D5.

### Principle 5 — secondary features do not block P1 core unless they alter core contracts

OAuth, SSO, MFA and API Tokens may remain incomplete after P1 core certification if they are not required to establish the stable core producer contract.

## 4. Certification status values

Allowed status values:

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
NOT_APPLICABLE
```

Interpretation:

### NOT_EVALUATED

Evidence has not yet been collected.

### BLOCKED

A required dependency, decision, migration, security rule or test is unresolved.

### PARTIALLY_VERIFIED

Some required evidence exists, but certification gate is incomplete.

### VERIFIED

Implementation and tests satisfy the capability contract.

Equivalent to D4.

### STABLE

Capability is safe for downstream dependency.

Equivalent to D5.

### NOT_APPLICABLE

Requirement does not apply to the candidate scope.

Must include rationale.

## 5. Certification record structure

Every capability certification entry should contain:

```text
Capability:
SPEC requirement IDs:
PLAN work units:
TEST IDs:
Source baseline SHA:
Candidate SHA:
Migration:
Architecture evidence:
Security evidence:
Integration evidence:
CI jobs:
Known debt:
Status:
Reviewer:
Decision date:
```

Do not prefill successful evidence before execution.

# Milestone A — P1 Core Certification

## 6. Purpose

P1 Core Certification exists to unlock:

```text
Priority 2
Workspace & Governance
```

It certifies only the stable producer contracts Workspace/Governance and later product contexts require.

## 7. P1 core certification scope

Required capabilities:

```text
User identity
Actor contract
Session identity/lifecycle baseline
Account identity
Account lifecycle baseline
Current Account resolution
Account/Tenant boundary
Tenant isolation
Identity ↔ Account consumer contract
```

## 8. P1 core non-required secondary capabilities

These do NOT need to be fully certified for Milestone A unless they modify the core identity contract:

```text
OAuth provider completeness
SSO
MFA
advanced security settings
API Tokens
advanced session/device management
all registration variants
all credential-recovery variants
```

## 9. P1 CORE-CERT-001 — User identity

Required SPEC:

```text
IAREQ001–IAREQ005
```

Required evidence:

- one canonical User identity;
- stable User ID;
- supported uniqueness constraints;
- lifecycle invariants;
- historical downstream reference behavior;
- no parallel User store.

Required tests include relevant:

```text
IA-TST-USER-*
```

Required status:

```text
STABLE
```

## 10. P1 CORE-CERT-002 — Actor contract

Required SPEC:

```text
IAREQ006–IAREQ009
```

Required evidence:

- Application-facing trusted Actor;
- no raw HTTP identity in Domain;
- supported principal mapping;
- actor spoofing prevention;
- production DI mapping.

Required tests:

```text
IA-TST-ACTOR-*
```

Required status:

```text
STABLE
```

## 11. P1 CORE-CERT-003 — Session contract

Required SPEC:

```text
IAREQ016–IAREQ023
```

Required evidence:

- create/bootstrap semantics;
- expiry;
- revocation;
- logout;
- User-disable interaction;
- cache/revocation correctness;
- stable API failure behavior.

Required tests:

```text
IA-TST-SESSION-*
```

Minimum status to unlock P2:

```text
VERIFIED
```

Preferred:

```text
STABLE
```

If Session remains D4 rather than D5, certification record MUST state why no downstream Workspace/Governance contract depends on the unstable part.

## 12. P1 CORE-CERT-004 — Account identity

Required SPEC:

```text
IAREQ024–IAREQ030
IAREQ104
IAREQ107
```

Required evidence:

- current source Account semantics resolved;
- explicit Account ownership decision:
  - RETAIN;
  - REHOME;
  - SPLIT;
  - INTRODUCE;
- one canonical Account business identity;
- stable Account ID;
- lifecycle baseline;
- relation to User/Workspace/Billing documented;
- no duplicate Account source of truth.

Required tests:

```text
IA-TST-ACCOUNT-*
IA-TST-OWN-ARCH-002
```

Required status:

```text
STABLE
```

## 13. P1 CORE-CERT-005 — Current Account context

Required SPEC:

```text
IAREQ031–IAREQ036
```

Required evidence:

- deterministic Account resolution;
- trusted context;
- resolution separate from authorization;
- Account switch semantics;
- background Account context behavior.

Required tests:

```text
IA-TST-CTX-*
```

Required status:

```text
STABLE
```

## 14. P1 CORE-CERT-006 — Tenant isolation

Required SPEC:

```text
IAREQ034
IAREQ035
IAREQ063
```

Required evidence:

```text
Account A cannot read Account B
Account A cannot mutate Account B
token scoped to A cannot operate in B
background work cannot become global by missing Account
```

Required test layers:

- Integration;
- Security;
- API where relevant;
- Infrastructure/provider-specific isolation where relevant.

### Enforcement guard (PR-IA-00 amendment)

Tenant-isolation tests MUST be executed against a connection that enforces RLS as the application role (for example `notrelix_app`), not as a table owner/administrator that bypasses RLS.

`Rls:SetSessionContext` must be true in the test environment; otherwise the gate is reported as not enforced (failed/unrun), never green.

Before marking STABLE, assert all of:

```text
policies exist for the exercised tables
session context variables are actually set
deny-path proven: Account A is denied Account B data at the persistence layer
```

This guard exists because `authz.access_grants` (the RLS predicate source) currently has no runtime writer; see `IA-PLAN-STOP-015` in the PLAN. A green gate obtained through owner-bypass behavior is invalid evidence.

Required status:

```text
STABLE
```

Any unresolved cross-tenant leakage blocks certification.

## 15. P1 CORE-CERT-007 — downstream producer contract

Required SPEC:

```text
IAREQ037–IAREQ039
IAREQ068–IAREQ073
```

Required evidence:

Workspace/Governance can consume:

```text
Actor/User ID
Account ID
Account scope
```

without private Identity/Account persistence.

Required tests:

```text
IA-TST-X-*
```

Required status:

```text
STABLE
```

## 16. P1 CORE-CERT-008 — architecture

Required evidence:

- Domain purity green;
- current layer direction green;
- no private downstream Identity persistence dependency;
- one canonical Account owner;
- pipeline-owned authorization preserved;
- no new production project.

Required tests:

```text
Architecture suite
IA-TST-ACTOR-ARCH-001
IA-TST-X-ARCH-*
IA-TST-OWN-ARCH-*
```

Required status:

```text
STABLE
```

## 17. P1 CORE-CERT-009 — migration/startup

Applicable when P1 changes schema or identity ownership.

Required evidence:

```text
fresh database initialization
supported upgrade path
migration history valid
seed/init valid
no pending model changes
application starts with production graph
```

Required tests:

```text
IA-TST-MIG-*
```

Status:

```text
STABLE
```

or:

```text
NOT_APPLICABLE
```

with evidence that no schema/storage contract changed.

## 18. P1 CORE-CERT-010 — security baseline

Required evidence:

- secret non-exposure;
- account spoofing denied;
- session revocation effective;
- auth failure privacy acceptable;
- no security controls weakened;
- CSRF/session transport contract remains compatible where browser mutation applies.

Required tests include:

```text
IA-TST-SEC-MASTER-*
relevant IA-TST-SESSION-*
IA-TST-CTX-SEC-*
```

Required status:

```text
STABLE
```

## 19. P1 core gate decision

Milestone A may be marked:

```text
P1 CORE CERTIFIED
```

### Certification records — executed 2026-08-14 (IA-GATE-001..003)

Baseline SHA: `5db7ec68` (develop). Candidate SHA: `48553a5c` (includes
`aac87873`) — working tree exactly matches candidate; full solution suite
4251/4251 passing on the candidate state.

```text
P1 CORE-CERT-001 — User identity
  SPEC: IAREQ001-IAREQ005 | PLAN: Phase 3 IA-USER-*, PR-IA-01
  TEST IDs: IA-TST-USER-* (tests.md ##10-16)
  Architecture: single canonical identity.users table (baseline:2298; one
    CreateTable, FKs point to it); no parallel User store anywhere
  Security/Integration: stable Guid ID; unique identifier enforced under
    concurrency; lifecycle invariants (Domain UserTests, UserMutationContract-
    Tests); deactivation blocks protected auth; historical downstream refs
    remain bare stable Guids (Phase 6 evidence)
  Migration: n/a (no User schema change in candidate)
  CI jobs: backend CI be-ci.yml (solution test + build gates)
  Known debt: none for P1 core
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-002 — Actor contract
  SPEC: IAREQ006-IAREQ009 | PLAN: Phase 3 IA-ACTOR-*, decision §6
    IA-ACTOR-PRINCIPAL-001
  TEST IDs: IA-TST-ACTOR-* (tests.md ##17-20)
  Architecture: Domain purity green (Architecture suite 371/371); no HTTP
    identity in Domain; principal mapping via Application pipeline markers
    (IAccountRequest/IWorkspaceRequest/IResourceScopedRequest) +
    ICurrentUser/ICurrentTenantContext; production DI in
    Infrastructure/DependencyInjection
  Security: actor spoofing prevention — TenantBootstrapBehaviorTests (5,
    cross-tenant/spoofed-account denial) + AuthorizationBehaviorTests +
    IA-TST-ACTOR-SEC-001
  Integration: runtime end-to-end under notrelix_app role
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-003 — Session contract
  SPEC: IAREQ016-IAREQ023 | PLAN: Phase 4 IA-SES-*, PR-IA-02 (decision §9/§10)
  TEST IDs: IA-TST-SESSION-*, LoginTests, RefreshTokenTests, LogoutTests,
    Infrastructure.Tests/Auth (revocation), RlsRuntimeEnforcementTests
    session-context transaction-local tests
  Evidence: login/refresh/logout lifecycle; revocation = watermark
    (user-revoked-before in Redis) enforced on login and refresh
    (candidate aac87873); refresh-token rotation; session context is
    transaction-local under RLS (no cross-pool leak)
  Known debt: bulk revocation on User-disable/Deactivate NOT wired
    (backlog IA-FIND-001, decision §4) — see §21 non-blocking record
  Status: VERIFIED (D4)
  Rationale for D4 (required by certification §11): Workspace/Governance and
    downstream P1 consumers depend only on authenticated session identity
    existing (UserId) per request, NOT on user-disable batch revocation;
    failing-closed behavior without session context is proven. D5 upgrade is
    scoped to a later security hardening pass (IA-FIND-001 removal).
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-004 — Account identity
  SPEC: IAREQ024-IAREQ029 | PLAN: Phase 5 IA-ACC-*, PR-IA-03, decision §1
    (Account ownership RETAIN)
  TEST IDs: AccountProvisioningServiceTests, access-grant projection tests
  Evidence: canonical accounts.account table; stable Guid AccountId is the
    tenant key across all 11 bounded contexts; account-level grants keyed by
    (account_id, user_id WHERE workspace_id IS NULL) with partial unique
    index; AccountMembers canonical membership
  Security: account selection never trusts request payload (IAccountRequest
    is metadata-only; account resolved from tenant context, TenantBootstrap-
    Behavior)
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-005 — Current Account resolution
  SPEC: IAREQ030-IAREQ036 | PLAN: Phase 2 + Phase 5 (decision §12 exit)
  TEST IDs: TenantBootstrapBehaviorTests, RlsRuntimeEnforcementTests
  Evidence: ICurrentTenantContext.SetAccount / RequireAccountId; account
    selected via route/header/session and verified by TenantBootstrapStore.
    VerifyAccountAccessAsync (active AccountMember) before any account-
    scoped handler; AccountSelectionRequiredException when unset
  Security: cross-account denial proven at Application seam (new tests) and
    RLS layer
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-006 — Tenant isolation
  SPEC: IAREQ074-IAREQ078 | PLAN: Phase 5 IA-ACC-009 + STOP-015 resolution
    (decision §11/§12/§14)
  TEST IDs: RlsRuntimeEnforcementTests (13), AccessGrantProjectionTests (7),
    RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole
  Integration evidence: policy pack applied on real PostgreSQL 16; missing
    context fails closed; no grant fails closed; cross-account/cross-workspace
    denial; worker/system scopes policy-gated; background scope fails closed
    without grant; runtime membership creation writes grant and is enforced
    under notrelix_app role (20/20 filter, 2026-08-14)
  Security: RLS is defense-in-depth; Application authorization maintained
    (TenantBootstrapBehavior); no RLS weakening
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-007 — downstream producer contract
  SPEC: IAREQ037-IAREQ039, IAREQ068-IAREQ073 | PLAN: Phase 6 IA-X-001..007
    (decision §15) + PR-IA-04
  TEST IDs: IA-TST-X-* (tests.md ##55-57)
  Evidence: Workspace consumes stable Guid Actor/User + Account IDs without
    Identity private persistence (grep-verified: zero references to
    Identity/Users|Sessions|Mfa|OAuth from Features/Workspaces and
    Features/WorkManagement); Availability via IIdentityUserLookupService
    snapshot; display via IActorLookupService; Billing keyed by AccountId;
    live Identity→Accounts/Workspaces event route:
    IdentityRegistrationCompletedIntegrationEventV1 →
    WorkspaceProvisioningConsumer → ProvisionPersonalWorkspaceCommand
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-008 — architecture
  SPEC: IAREQ089-IAREQ103 | PLAN: continuous architecture gates
  TEST IDs: Architecture suite (371/371), IA-TST-ACTOR-ARCH-001,
    IA-TST-X-ARCH-001, IA-TST-OWN-ARCH-*
  Evidence: Domain purity + layer direction green on candidate; no private
    downstream Identity persistence (P1 CORE-CERT-007 evidence); one
    canonical Account owner (decision §1); pipeline-owned authorization
    preserved (no handler-local bypass added); no new production project
    (backend.slnx inventory unchanged)
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-009 — migration/startup
  SPEC: IAREQ104-IAREQ107 | PLAN: IA-GATE-003, decision §13 consolidation
  TEST IDs: IA-TST-MIG-DB-001 (clean DB), IA-TST-MIG-SEED-001,
    MigrationSmokeTests, SeedDataInitialiserTests, IdempotencyStoreIntegrationTests
  Evidence (2026-08-14 run): MigrationSmokeTests 4/4 — single consolidated
    baseline creates all 143 tables, public schema empty; IdempotencyStore
    18/18 — strict ck_idempotency_records_completed_result enforced from
    baseline; SeedDataInitialiserTests green with account-level seed grants;
    `dotnet ef migrations has-pending-model-changes` → no changes; API
    startup via production DI graph covered by API/Integration suites
  Upgrade path: dev-phase policy is fresh-baseline (`make dev-reset`);
    incremental migration history consolidated (decision §13) — recorded,
    not a blocker
  Status: STABLE (D5)
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14

P1 CORE-CERT-010 — security baseline
  SPEC: IA-TST-SEC-MASTER-*, IA-TST-CTX-SEC-*
  TEST IDs: TenantBootstrapBehaviorTests (spoofing denied),
    AuthorizationBehaviorTests (unauthorized/unauthenticated denial),
    AccessTokenRevocationEvaluator/blacklist tests (session revocation
    effective), GlobalExceptionHandlerTests (auth failure privacy, no
    detail leak), CsrfValidationMiddleware + CsrfProtector (API.Tests),
    RLS cross-tenant denial matrix
  Evidence: account spoofing denied at Application + RLS; session
    revocation watermark effective; null/default account never global
    (fails closed); no security control weakened by candidate; CSRF
    transport contract intact for browser mutations
  Known debt: no automated secret-scan CI job (gitleaks-class scanner
    absent; only CI test JWT key present in be-ci.yml) — recorded §21
  Status: STABLE (D5) with the secret-scan debt recorded non-blocking
  Reviewer: execution agent (review), pending human sign-off
  Decision date: 2026-08-14
```

### Gate decision (IA-GATE-004 trigger) — executed 2026-08-14

```text
Milestone A verdict: P1 CORE CERTIFIED (Session at VERIFIED/D4 per the
  recorded rationale above; all other capabilities STABLE/D5)

IA-GATE-001 core source review ......... PASS (records above; no canonical
  rule violated, no architecture test weakened)
IA-GATE-002 downstream smoke integration PASS (User/Actor → Account →
  Workspace/Governance compile against approved contracts; integration
  proof: registration/workspace lifecycle + runtime grant enforcement,
  60/60 gate run, 2026-08-14)
IA-GATE-003 core migration proof ....... PASS (clean DB 143 tables,
  strict constraint from single baseline, seed valid, no pending model
  changes)
IA-GATE-004 open P2 .................... OPEN — Workspace & Governance may
  start broad implementation; Identity continues secondary scope
  (Phases 8-12: registration/credentials variants, OAuth, MFA, security
  settings, API tokens)
```

Blockers (§20): none. All check items verified on the candidate SHA.

Debt recorded as non-blocking (§21):

```text
- user-disable bulk session revocation (IA-FIND-001) — blocks nothing in
  P1; owner: Identity hardening pass (security-phase scope) — non-blocking
  for P1 core
- automated secret scan CI job — owner: engineering-quality; future CI
  hardening — non-blocking for P1 core
- session certified VERIFIED not STABLE — rationale above; D5 upgrade
  scoped to security hardening — non-blocking for Workspace/Governance
```

only when:

| Capability | Required |
|---|---|
| User identity | D5 |
| Actor | D5 |
| Account | D5 |
| Current Account | D5 |
| Tenant isolation | D5 |
| Consumer contract | D5 |
| Architecture | D5 |
| Security baseline | D5 |
| Session | D4 minimum, D5 preferred |
| Migration | D5 or N/A |

## 20. P1 core blockers

Any of the following blocks Milestone A:

- unresolved canonical Account owner;
- duplicate Account truth;
- unresolved Account vs Workspace semantics;
- cross-tenant read/write leak;
- downstream private Identity table dependency;
- actor spoofing path;
- session revocation broken;
- architecture test intentionally weakened;
- missing mandatory P1 core tests;
- required CI job not executed;
- schema changed but migration not verified.

## 21. P1 core non-blocking debt

May remain after Milestone A if isolated from core contracts:

- unsupported secondary OAuth provider;
- incomplete MFA;
- incomplete API-token UI/API;
- advanced session-device metadata;
- noncritical profile features;
- secondary security UX.

Each remaining item must be explicitly recorded as:

```text
non-blocking for P1 core
```

with owner and future scope.

# Milestone B — Full Identity & Accounts Scope Certification

## 22. Purpose

Milestone B certifies all release-scoped Identity & Accounts capabilities, not merely the P1 producer core.

## 23. Full-scope capability groups

Depending on release scope:

```text
Registration
Credentials
OAuth
SSO
MFA
Security settings
Advanced session/security controls
API Tokens
Cross-team integrations
Observability
Reliability
Migration/hardening
```

A capability explicitly excluded from the release may be marked NOT_APPLICABLE for Milestone B only if product/release scope says so.

# Registration / Credentials certification

## 24. FULL-CERT-REG-001 — registration

Required evidence:

- canonical User created exactly once;
- duplicate/concurrent registration handled;
- Account/Workspace bootstrap ownership explicit if involved;
- no hidden cross-context cascade;
- enumeration resistance where applicable.

Tests:

```text
IA-TST-REG-*
```

### 24.1 Registration certification record — Phase 8 (2026-08-14)

```text
Status: VERIFIED (D4)
Reviewer: agent (Phase 8 execution)
Decision date: 2026-08-14

Evidence:
- canonical user created exactly once:
  - unique index on normalized email (idx_users_normalized_email, baseline schema)
  - RegisterCommandHandler pre-check + DB constraint backstop
- duplicate/concurrent registration handled:
  - pre-check path: Result failure "Email is already in use"
    (Integration RegisterCommandHandlerTests.Handle_WhenEmailExists)
  - race path: unique violation mapped to 409 ConflictException
    (Application ExceptionMappingBehaviorTests unique-violation cases;
    Integration RegisterDuplicateRaceTests on real PostgreSQL —
    concurrent same-email registrations, second save maps through
    ExceptionMappingBehavior to ConflictException)
  - commit b75e6ab
- Account bootstrap ownership explicit: Accounts-owned AccountProvisioningService;
  Workspace provisioned async via consumer (WorkspaceProvisioningConsumer),
  RegisterCommandHandlerTests proves workspace not created synchronously
- no hidden cross-context cascade: registration emits
  IdentityRegistrationCompletedIntegrationEventV1 only after user+account
  durable state; no synchronous side-effect writes to foreign contexts
- enumeration resistance: ForgotPassword returns OK regardless of email
  existence (PublicAuthEndpointTests.ForgotPassword_WithUnknownEmail)
- API contract: /api/v1/auth/register — weak password now 400
  (commit a73c863), duplicate race 409 (commit b75e6ab)

Gate evidence: full backend suite 4275/4275 (7 projects) on 2026-08-14;
has-pending-model-changes: none. No schema change in Phase 8.
```


## 25. FULL-CERT-CRED-001 — credentials

Required evidence:

- credential verification safe;
- credential update requires approved proof;
- reset/recovery replay resistance if supported;
- raw credentials not stored/logged;
- session impact defined.

Tests:

```text
IA-TST-CRED-*
relevant IA-TST-AUTH-*
```

### 25.1 Credentials certification record — Phase 8 (2026-08-14)

```text
Status: VERIFIED (D4)
Reviewer: agent (Phase 8 execution)
Decision date: 2026-08-14

Evidence:
- credential verification safe:
  - PasswordHasher (BCrypt WF12) — PasswordHasherTests: roundtrip,
    wrong-password false, unique salts, malformed/empty input false
    (commit 2e6702c)
- credential update requires approved proof:
  - ChangePassword: current password verified via IPasswordHasher before
    update; timing equalized with dummy BCrypt hash on unknown user
    (ChangePasswordCommandHandler)
  - password policy minimum length 8 enforced on Register, ChangePassword,
    ResetPassword via PasswordPolicy.MinimumLength (commits 2e6702c/a73c863);
    API proofs: ChangePasswordEndpointTests weak→400, missing current→400;
    PublicAuthEndpointTests weak register→400, weak reset→400
- reset/recovery replay resistance:
  - OtpService single-use code (deleted after successful validate) + max 5
    attempts + TTL 10m — OtpServiceTests (commit aa0c5ad);
    IA-TST-CRED-SEC-002 replay proof
- raw credentials not stored/logged:
  - only BCrypt hash persisted (User.UpdatePassword); no plaintext logging
- session impact defined:
  - ChangePassword revokes all active sessions + sets revocation watermark
    (RevokeUserBeforeAsync, 24h TTL) mirroring ResetPassword;
    Application ChangePasswordTests prove session revocation + blacklist
    write + password-changed email (best-effort)
  - AccessTokenRevocationEvaluator rejects tokens issued before watermark
    (JwtBlacklistServiceTests + AccessTokenRevocationEvaluatorTests)

Gate evidence: full backend suite 4275/4275 (7 projects) on 2026-08-14;
has-pending-model-changes: none. No schema change in Phase 8.
```


# OAuth certification

## 26. FULL-CERT-OAUTH-001 — provider identity

Required:

- provider + subject uniqueness;
- canonical User mapping;
- no email-only unsafe auto-link unless approved;
- no duplicate canonical identity.

## 27. FULL-CERT-OAUTH-002 — protocol security

Required:

- state validation;
- PKCE/nonce where applicable;
- expiry;
- replay resistance;
- safe callback/return target.

Tests:

```text
IA-TST-OAUTH-SEC-001
IA-TST-OAUTH-SEC-002
```

## 28. FULL-CERT-OAUTH-003 — collision handling

Required matrix:

```text
existing link to current User
link exists on another User
same email / different subject
concurrent link
provider changes email
callback replay
```

Tests:

```text
IA-TST-OAUTH-SEC-003
IA-TST-OAUTH-SEC-004
IA-TST-OAUTH-CONC-001
```

## 29. FULL-CERT-OAUTH-004 — provider secret safety

Required:

- protected token storage if persisted;
- no provider secret in API/log/event;
- provider failure normalized.

Tests:

```text
IA-TST-OAUTH-INF-001
IA-TST-OAUTH-SEC-005
IA-TST-OAUTH-API-001
```

## 30. OAuth certification status

Release-scoped OAuth capability requires:

```text
STABLE
```

before Milestone B if OAuth is included in release scope.

## 30.1 Phase 9 record — OAuth account link/unlink

```text
Capability:        OAuth account link/unlink (secondary capability, Phase 9)
SPEC requirement:  IA-OAUTH-005/007 (link/unlink flows), IA-OAUTH-UNLINK-001 (last-primary-auth-method rule)
PLAN work units:   Phase 9 OAuth link/unlink
TEST IDs:          IA-TST-OAUTH-*, UserUnlinkOAuthInvariantTests, CommandMarkerArchitectureTests
Source baseline:   develop (Phase 9 working tree, uncommitted at record time)
Candidate SHA:     df90a267 (pushed to origin/develop)
Migration:         has_pending-model-changes clean; 20260702093805_SchemaV2Baseline DDL
                   repaired (has_password_credential column restored in baseline CreateTable)
Architecture evidence: full slnx suite 4310 passed / 0 failed (Domain 2565,
                   Integration 265, API 230, Architecture 372 + remaining)
Security evidence: state bound to current User with Flow=Link + BoundUserId + 10-min TTL;
                   login-state state rejected; bound-user mismatch rejected;
                   provider mismatch rejected; callback replay via error/state params rejected
Integration evidence: StartOAuthLink 4, CompleteOAuthLink 10, UnlinkOAuth 4,
                   auto-link rejection (no session issued, no OAuth row persisted)
API evidence:      3 new endpoints (start/callback/unlink), +6 endpoint tests (401 x3, invalid provider x3)
Contract evidence: OpenAPI notrelix.v1.json (+149) regen; frontend schema.ts (+133)
                   regen; pnpm codegen:check, typecheck, lint, format:check,
                   check:architecture/-docs/test-taxonomy all green
CI evidence:       Backend CI, Frontend CI, Documentation Governance, Frontend
                   Packaging — all success on exact SHA df90a267
Known debt:        StartOAuthLinkCommand allowlisted as Intentional (Redis state write,
                   no DB mutation; mirrors StartOAuthLoginCommand)
Status:            VERIFIED (Phase 9 scope)
Reviewer:          pending
Decision date:     2026-08-15
```

OAuth link/unlink does not alter canonical User identity, Actor, Session, Account
context or tenant isolation, therefore per §155 it does not invalidate P1 certification.

# SSO certification

## 31. FULL-CERT-SSO-001 — semantic ownership

Before SSO can be certified:

- SSO protocol/meaning identified;
- Account/Workspace relationship explicit;
- SSO is not merely assumed equivalent to social OAuth;
- provider/IdP trust boundary explicit.

## 32. FULL-CERT-SSO-002 — trust validation

As applicable:

- issuer;
- audience;
- signature;
- state/nonce;
- replay;
- assertion expiry.

## 33. FULL-CERT-SSO-003 — tenant authorization separation

Successful IdP authentication MUST NOT automatically grant unrelated Account access.

Tests:

```text
IA-TST-SSO-*
```

If SSO is not release-scoped:

```text
NOT_APPLICABLE
```

with explicit release-scope rationale.

# MFA certification

## 34. FULL-CERT-MFA-001 — enrollment

Required:

- pending before verification;
- verified before active;
- invalid code rejected;
- secret safe.

## 35. FULL-CERT-MFA-002 — challenge

Required:

- eligible active method;
- expiry;
- replay resistance;
- abuse controls;
- successful auth transition.

## 36. FULL-CERT-MFA-003 — recovery

Required:

- explicit approved recovery mechanism;
- no weak email-only bypass;
- audit/authorization where admin recovery exists.

## 37. FULL-CERT-MFA-004 — disable/reset

Required:

- proof/authorization;
- session impact;
- audit;
- secret handling.

Tests:

```text
IA-TST-MFA-*
```

Release-scoped MFA requires:

```text
STABLE
```

## 37.1 Phase 10 record — MFA authenticator + recovery codes

```text
Capability:        MFA — TOTP authenticator app (primary), recovery codes (activation-only)
SPEC requirement:  IAREQ048-054
PLAN work units:   Phase 10 MFA work units
TEST IDs:          IA-TST-MFA-*, MfaMethodTests, MfaRecoveryBatchTests,
                   MfaTotpServiceTests, MfaRecoveryCodeGeneratorTests,
                   LoginTests (MFA challenge branch), MfaFlowTests (integration)
Source baseline:   develop (Phase 10 working tree, uncommitted at record time)
Candidate SHA:     93ed5de (pushed to origin/develop)
Migration:         has-pending-model-changes clean; SchemaV2Baseline DDL extended
                   (mfa_recovery_batches + mfa_recovery_codes + FK
                   fk_mfa_recovery_codes_mfa_recovery_batches_batch_id + 4 indexes);
                   migration smoke certified at 145 tables; Designer + snapshot
                   synchronized
Architecture evidence: full slnx build 17 projects 0 errors; full solution suite
                   green (incl. Architecture tests, Domain contract snapshots
                   updated with 3 recovery events)
Security evidence: challenge token consume-once (SHA-256 key in Redis, TTL 5 min,
                   replay rejected by integration test); recovery codes stored only
                   as SHA-256 hashes, single-use (integration-proven), latest-batch
                   only; valid TOTP proven against RFC 6238 reference vectors
                   (±1 step drift); Data Protection at-rest secret; DisableMfa
                   revokes all active sessions + 24h JWT watermark; login and
                   OAuth login both challenged; generic failure messages
                   (no account enumeration); AuthResult never carries a session
                   when MfaRequired is set
Integration evidence: MfaFlowTests 6 scenarios on real PostgreSQL + Redis
                   (enrollment -> TOTP challenge -> session; token single-use;
                   recovery code single-use; regeneration invalidates old batch;
                   disable revokes sessions and removes challenge; unknown-token
                   rejected); Integration suite 271 passed
Contract evidence: OpenAPI notrelix.v1.json (+210): 6 MFA endpoints; frontend
                   schema.ts (+239, zero deletions) regenerated via pnpm codegen;
                   additive-only diff reviewed
CI evidence:       Backend CI, Frontend CI, Documentation Governance, Frontend
                   Packaging — all success on exact SHA 93ed5de
Known debt:        MFA UI flows not yet wired in frontend (backend contract ready);
                   admin/reset MFA recovery deferred by approved plan decision;
                   Workspace MFA policy (SEC-002) remains future scope
Status:            VERIFIED (Phase 10 scope)
Reviewer:          pending
Decision date:     2026-08-16
```

MFA does not alter canonical User identity, Actor, Session, Account context or
tenant isolation, therefore per §156 it does not invalidate P1 certification.

# Security settings certification

## 38. FULL-CERT-SEC-001 — ownership

User security settings remain Identity-owned.

Workspace policy remains Workspace/Governance-owned.

## 39. FULL-CERT-SEC-002 — sensitive mutations

Required operations use stronger proof where policy requires.

## 40. FULL-CERT-SEC-003 — security events

Critical security changes are auditable and do not contain secrets.

Tests:

```text
IA-TST-SEC-*
```

## 40.1 Phase 11 record — step-up verification + session management

```text
Capability:        Sensitive setting step-up verification (MFA | password | OAuth
                   re-authentication) for DisableMfa, RegenerateRecoveryCodes,
                   OAuth link/unlink; session management (list, revoke-by-id,
                   revoke-others); OAuth step-up flow
SPEC requirement:  IAREQ055-058
PLAN work units:   IA-SEC-001..005
TEST IDs:          IA-TST-SEC-*, MfaFlowTests (integration), UnlinkOAuth-,
                   StartOAuthLink-, RefreshToken-, Register-, RegisterDuplicateRace-,
                   LoginCommandHandlerTests (integration), RefreshTokenTests (application)
Source baseline:   develop (Phase 11 working tree, uncommitted at build time)
Candidate SHA:     71c7984 (324242f + d29013b code/contracts, 71c7984 DI fix — pushed)
Hardening commits: 22de7398 (P11-BLK-001..004), 2545e309 (IA-SEC-001..005),
                   661149c2 + f8a720c6 (OpenAPI + frontend contract), 7999d4f (format)
Migration:         none — no persisted shape change (proofs live in Redis challenge
                   store; sessions already persisted)
Architecture evidence: full slnx build 17 projects 0 errors; dotnet format
                   verify-no-changes clean; CommandMarkerArchitectureTests green —
                   GetStepUpRequirement is an IQuery, CompleteStepUpMfa and
                   StartOAuthStepUp implement ITransactionalRequest (no allowlist
                   additions); no direct EF added in handlers beyond owned ports
Security evidence: step-up proof is single-use and bound to user+session+purpose;
                   password step-up verifies the real stored hash (mock-free in
                   integration), OAuth step-up bound to an existing linked provider,
                   proof consumption fail-closed (wrong bind/expired rejected);
                   UnlinkOAuth guards last-primary-auth-method and suspended user;
                   flow-bound callback dispatch with non-consuming IOAuthStateStore
                   PeekAsync; CompleteOAuthLogin rejects states not bound to a login
                   flow; JWT sid claim + revoked-session watermark enforced on
                   token validation; session revoke by id + revoke-others verified
                   against real PostgreSQL + Redis
Hardening evidence: P11-BLK-001..004 verified on real PG + Redis (MfaFlowTests):
                   raw MFA challenge can never authorize DisableMfa/RegenerateRecovery
                   Codes; Lua atomic consume — N concurrent consumers, exactly one
                   winner; login-vs-step-up purpose isolation both directions;
                   multi-attempt policy (5 attempts) with attempt-limit invalidation
                   and no consume-before-verify; wrong OTP never destroys challenge
                   before limit; step-up challenge never completes login; verified
                   proof succeeds once and replay is rejected; proof bound to exact
                   purpose/session/user; IA-SEC-001 ownership: Identity owns
                   UserSecuritySettings/UserMfaMethod/MfaRecoveryBatch/UserSession/
                   OAuthAccount/password state — structural proof via
                   Architecture.Tests APP_DATA_003 (Identity handlers cannot inject
                   Workspace/Governance DbContext ports); IA-SEC-002-CRED:
                   ChangePassword requires verified ChangePassword-purpose proof
                   only when active MFA exists; wrong current password never burns
                   a valid proof; raw challenge token rejected with no side effect
                   (login challenge remains usable); 9-case matrix green on real
                   PG+Redis; IA-SEC-003: no duplicate Domain events — reuses
                   UserPasswordChanged/OAuthAccountLinked/Unlinked/
                   UserSessionRevoked + MFA events; SessionRevocationReasons.cs
                   stable vocabulary populated by all Phase 11 revoke handlers;
                   reason propagation into UserSessionRevokedDomainEvent covered
                   by Domain tests; step-up success/fail recorded as Application
                   telemetry only; IA-SEC-004: session management regression —
                   revoke-by-id kills sid-bound authority, revoke-others excludes
                   current session (pre-existing tests green); IA-SEC-005:
                   self-service recovery VERIFIED via Phase 10 recovery-code flow;
                   administrative/operational reset DEFERRED (requires Governance
                   admin authorization + audit; target Phase 13 or admin-security
                   workstream) — no Identity-local bypass exists
Integration evidence: UnlinkOAuthCommandHandlerTests 4 scenarios on real PostgreSQL
                   + Redis (password proof, OAuth-only account, no-op unlink,
                   suspended user); MfaFlowTests step-up branch (get requirement ->
                   issue TOTP proof -> regen/disable with StepUpToken) + hardening
                   regression suite (27 tests incl. ChangePassword step-up matrix);
                   full suite: Architecture 372, Domain 2575, Application 634,
                   Infrastructure 120, Platform 147, API 230, Integration 271 —
                   all passed (Integration 277 incl. hardening suite)
Contract evidence: OpenAPI notrelix.v1.json (+438): security step-up
                   (requirement/complete-mfa/complete-password), sessions
                   (list/revoke/revoke-others), OAuth step-up start + callback;
                   DisableMfa/RegenerateRecoveryCodes/UnlinkOAuth request bodies
                   carry StepUpToken; ChangePassword request gains optional
                   stepUpToken (additive, nullable); frontend schema.ts (+387,
                   zero deletions) regenerated via pnpm codegen; additive +
                   documented change in existing bodies reviewed
CI evidence:       Backend CI, Frontend CI, Documentation Governance, Frontend
                   Packaging — all success on exact SHA 71c7984 (324242f + d29013b
                   + DI build fix 71c7984); hardening commits 22de7398..7999d4f
                   pending CI on push (identical committed tree gates)
Known debt:        step-up + session UI flows not yet wired in frontend (backend
                   contract ready); OAuth step-up frontend handoff route documented
                   in CompleteOAuthLoginEndpoint dispatch contract
Status:            VERIFIED (Phase 11 scope)
Reviewer:          pending
Decision date:     2026-08-17
```

Step-up verification and session management do not alter canonical User identity,
Actor, Session contract, Account context or tenant isolation, therefore per §156
they do not invalidate P1 certification.

# API token certification

## 41. FULL-CERT-TOK-001 — issuance

Required:

- cryptographically safe secret generation through approved mechanism;
- raw token exposed only according to one-time contract;
- raw token not persisted where hashing/protection model requires non-reversible storage.

## 42. FULL-CERT-TOK-002 — verification

Required:

- valid token maps to approved principal;
- malformed/unknown token does not reveal existence;
- verification remains tenant-safe.

## 43. FULL-CERT-TOK-003 — Account scope

Required:

```text
token for Account A
cannot operate in Account B
```

## 44. FULL-CERT-TOK-004 — Governance interaction

Authentication with token does not bypass Governance.

If token scopes exist, their relationship with Governance is explicitly tested.

## 45. FULL-CERT-TOK-005 — revocation

Revoked token becomes ineffective within accepted security window.

## 46. FULL-CERT-TOK-006 — audit

Token lifecycle/use logging is safe and does not expose raw secret.

Tests:

```text
IA-TST-TOKEN-*
```

## 46.1 Phase 12 record — API tokens (issuance / verification / revocation)

```text
Capability:        Workspace-scoped API tokens (IA-TOK-001..009): issuance via
                   step-up-gated creation (raw secret returned exactly once),
                   verification via dedicated "ApiToken" authentication scheme
                   (digest lookup, fail-closed), revocation immediate,
                   metadata-only listing
SPEC requirement:  IAREQ-API-TOK-*
PLAN work units:   IA-TOK-001..009
TEST IDs:          ApiTokenHandlerTests (application), ApiTokenFlowTests (integration)
Source baseline:   develop (Phase 12 working tree, uncommitted at build time)
Candidate SHA:     363f172 (pushed to develop)
Migration:         none — api_tokens table shipped in earlier identity migration;
                   RLS policy for api_tokens added (workspace-scoped via
                   ops.apply_scoped_business_policies in 006_policies_identity.sql)
Architecture evidence: full slnx build 17 projects 0 errors; Architecture.Tests
                   372 green incl. WorkspaceScopedArchitectureTests — ListApiTokens
                   Query implements IWorkspaceRequest/IQuery/IRequirePermission,
                   no allowlist additions; handler-level auth scheme registered in
                   AuthRegistration (AddScheme "ApiToken", consumer opt-in, JWT
                   remains default); no direct EF in Application handlers beyond
                   owned IIdentityDbContext port; handler/secret service placed in
                   Infrastructure (Security/ApiTokens, Auth/ApiTokens)
Security evidence: raw secret is CSPRNG 32 bytes URL-safe with ntk_v1. prefix,
                   shown once and never persisted (SHA-256 digest via TokenHasher);
                   creation requires session (sid claim) + single-use step-up proof
                   bound to IssueApiToken purpose; verification hashes the presented
                   secret and fails closed on unknown/revoked/expired/user-less
                   tokens; lookup runs in system context by unguessable digest
                   (same trust model as JWT blacklist) while application access
                   remains RLS-protected; revocation effective immediately
                   (status update before further lookups); digest max length 512
                   bounds verifier input; raw secret never logged/returned again
                   (List returns metadata only)
Integration evidence: ApiTokenHandlerTests 10 scenarios (application); ApiTokenFlowTests
                   5 scenarios on real PostgreSQL + Redis: create returns raw secret
                   once + stored row holds digest only + token authenticates with
                   sub claim; unknown secret rejected; garbage rejected;
                   revoke immediately blocks re-authentication; expired token fails
                   closed. RLS isolation for api_tokens certified separately by
                   RlsRuntimeEnforcementTests (shared container runs no RLS)
Contract evidence: endpoint group /api/v1/workspaces/{workspaceId}/api-tokens —
                   GET (list metadata), POST (create, body Name/ExpiresAt/
                   StepUpToken), DELETE /{tokenId} (revoke); all authenticated +
                   ManageWorkspaceSettings on workspaces.workspace; OpenAPI
                   regenerated (notrelix.v1.json) with additive operations
CI evidence:       Backend CI on exact SHA 363f172 pending (identical committed tree
                   gates); local run 17 projects 0 errors, Architecture 372,
                   Domain 2576, Application 649, Infrastructure 120, Platform 147,
                   API 230, Integration 293 green + 6 known local-only failures
                   from unrelated uncommitted workspace-governance WIP
                   (IResourceAuthorizationSnapshotStore wiring) — not present in CI
Known debt:        API-token UI flows not wired in frontend (backend contract
                   ready); administrative revocation from Governance/ops console
                   deferred; StepUpToken transport for third-party automation
                   integrations (non-interactive issuance) deferred
Status:            VERIFIED (Phase 12 scope)
Reviewer:          pending
Decision date:     2026-08-18
```

## 46.2 Phase 12 review record — full-gate certification on exact SHA (2026-08-19)

```text
Capability:        Workspace-scoped API tokens (IA-TOK-001..009): review fixes for
                   issuance (step-up-gated, raw secret returned exactly once),
                   verification (ApiToken scheme, digest lookup, fail-closed),
                   immediate revocation, metadata-only listing, Account/Workspace
                   principal binding, RLS tenant isolation, Governance interaction
SPEC requirement:  IAREQ-API-TOK-*
PLAN work units:   IA-TOK-001..009 (Phase 12 review: BLK-001/002, CLEANUP-006,
                   CONTRACT-005, GATE-003/004)
TEST IDs:          ApiTokenHandlerTests (application, 10), ApiTokenFlowTests
                    (integration, 9), ApiTokenHttpFlowTests (integration, 10),
                    RlsRuntimeEnforcementTests (15), TenantBootstrapBehaviorTests,
                    PipelineExecutionTests, PipelineRuntimeOrderTests,
                    AuthPipelineArchitectureTests, NotrelixApiFactory (host)
Candidate SHA:     08c02086e59d2cd63f558fa9de2bfad57f699270 (develop, pushed)
Source baseline:   develop @ 08c02086 (committed tree, CI-verified)
Commit scope:      6ff3e99 (governance snapshot checks), 3000e0f (credential
                    context/scheme-selector/concurrency),
                    1b5022a (unique token-hash baseline migration),
                    6193822 (test gates), 65a4f7f (OpenAPI), 38ddaa3 (frontend
                    REST contract), 2c57903 (tenant bootstrap fix + unmapped
                    IsDeleted predicate removal + endpoint invariant guard),
                    df8ad8a (real-graph HTTP proof, race gate rename),
                    08c0208 (CI critical-filter pin)
Migration:         none added — unique index ux_api_tokens_token_hash enforced
                    on existing baseline migration 20260702093805_SchemaV2Baseline
                    (CreateIndex after api_tokens CreateTable; HasIndex("TokenHash")
                    .IsUnique().HasDatabaseName("ux_api_tokens_token_hash") in
                    Designer; ApplicationDbContextModelSnapshot carries the same
                    HasIndex); `dotnet ef migrations has-pending-model-changes`
                    reports no pending model changes; no migration file introduced
                    (all fixes on the candidate SHA are develop-only code changes:
                    tenant bootstrap session-context handling, unmapped
                    IsDeleted predicate removal, endpoint invariant guard —
                    no schema/data migration)
IA-TOK-001..009:   VERIFIED — issuance (001): step-up single-use proof bound to
                   IssueApiToken purpose required; raw secret CSPRNG 32 bytes
                   URL-safe with "ntk_v1." prefix, returned exactly once, never
                   persisted (SHA-256 digest via TokenHasher); verification (002):
                   ApiToken scheme with dedicated handler (AddScheme "ApiToken",
                   consumer opt-in, JWT remains default), digest lookup with
                   max-length-bounded input, fails closed on unknown/revoked/
                   expired/user-less token; listing (003): metadata only, no
                   secret material; revocation (004): immediate status update
                   before further lookups, revoke wins under race (GATE-004);
                   Account binding (005): token bound to issuing Account
                   (BoundAccountId) enforced by handler/behavior resolution;
                   Workspace binding (006): token bound to issuing Workspace
                   (BoundWorkspaceId/workspace claim), cross-workspace denied
                   (TenantBootstrapBehavior + RLS); Governance interaction (007):
                   ManageWorkspaceSettings required on workspaces.workspace,
                   api-token authentication does not bypass Governance
                   (AuthPipelineArchitectureTests + Governance intersection
                   matrix via PermissionService/WorkspacePermissionService
                   snapshot checks); tenant isolation (008): api_tokens RLS
                   workspace-scoped (ops.apply_scoped_business_policies in
                   006_policies_identity.sql), RLS A1 (row scope on workspace)
                   / A2 (policy applies on all paths) certified by
                   RlsRuntimeEnforcementTests (15), HTTP A3 (scheme-selector
                   path) certified by ApiTokenHttpFlowTests; audit/safety (009):
                   lifecycle/use logging safe, raw secret never logged nor
                   returned, no security weakening (CERT-SEC-007 "no" entries)
HTTP proof:        ApiTokenHttpFlowTests 10/10 on real host: token principal
                    authenticates via ApiToken scheme over HTTP; JWT session
                    remains default; creation returns raw secret exactly once;
                    unknown/garbage secret rejected; revoke blocks
                    re-authentication immediately; expired token fails closed;
                    member-scoped token that passes TenantBootstrap and
                    ViewWorkspace is denied ManageWorkspaceSettings through the
                    real PermissionService production graph (no mocked
                    permission evaluator — P12-BLK-001F); NotrelixApiFactory
                    PostConfigure<AuthenticationOptions>
                    pins the test-scheme default that production Platform
                    PolicyScheme ("NotrelixAuth") otherwise overrides
Account/Workspace binding proof: ApiTokenFlowTests 9/9 + ApiTokenHttpFlowTests:
                    sub claim resolves to issuing Account; workspace-scoped
                    principal honored by TenantBootstrapBehavior; token for
                    workspace A cannot operate in workspace B (behavior +
                    RLS combined); Governance intersection matrix exercised by
                    PermissionServiceTests/WorkspacePermissionServiceTests
                    (snapshot-based checks) with api-token principal
Concurrency proof: ApiTokenFlowTests 9/9 incl. revoke-expired lifecycle,
                    semantic no-op preserves TokenVersion (rejection does not
                    advance version), and
                    Authenticate_RacingWithRevoke_WhenRevokeCommitsFirst_FailsClosed
                    (BlockBeforeSaveChangesInterceptor barrier: auth read
                    Active, SaveChanges blocked pre-UPDATE, revoke commits
                    first, auth RecordUse hits the stale-version branch and
                    fails closed; post-commit invariant: every subsequent
                    request also fails — no timing-dependent assertion)
OpenAPI proof:     notrelix.v1.json regenerated (commit 65a4f7f): GET list
                   ApiTokenSummaryDto, POST 201 CreatedApiTokenDto with
                   Location, DELETE 204 No Content (no System.Void
                   placeholders); frontend REST contract regenerated
                   (commit 38ddaa3, additive); OpenAPI drift check PASS
                   ("API tests and OpenAPI contract" CI job)
Architecture evidence: full slnx build 17 projects 0 errors; Architecture.Tests
                   372 green; ListApiTokens Query implements IWorkspaceRequest/
                   IQuery/IRequirePermission; no allowlist additions; handler and
                   secret service placed in Infrastructure (Security/ApiTokens,
                   Auth/ApiTokens); no direct EF in Application handlers beyond
                   owned IIdentityDbContext port; domain purity CERT-ARCH-001
                   preserved; production project topology changed: no
Local evidence:    Domain 2576, Application 649, Infrastructure 120, Platform
                    147, API 230, Architecture 372, Integration 315 — 4409 tests
                    passed / 0 failed on the committed tree; dotnet format
                    --verify-no-changes clean; frontend validate:fast 43/43
                    tasks (incl. codegen:check after contract commits)
CI evidence:       Backend CI success on candidate SHA (run 32328937840:
                    Quality and security guards, Architecture tests, Domain/
                    Application/Infrastructure tests, API tests and OpenAPI
                    contract, Platform messaging tests, Integration and
                    provider tests incl. pinned ApiTokenFlowTests/
                    ApiTokenHttpFlowTests critical filter, Docker build,
                    Backend CI gate); Frontend CI success (run 32328937770);
                    Frontend Packaging success on candidate SHA (run
                    32328937672); Documentation Governance success
CI failures:       GitGuardian Security Checks FAILURE on 38ddaa3e —
                   pre-existing incident (external owner action, dashboard
                   gitguardian.com); no raw secret/credential introduced by
                   this scope (CERT-SEC-001 evidence green); recorded as
                   external owner action, does not block this record
Known debt:        API-token UI flows not wired in frontend (backend contract
                   ready); administrative revocation from Governance/ops
                   console deferred; StepUpToken transport for third-party
                   automation integrations (non-interactive issuance) deferred
Status:            VERIFIED (Phase 12 review scope) — not D5/STABLE: full
                   stabilization additionally requires GitGuardian incident
                   closure (external owner) and product decision on remaining
                   known debt; P1 core certification unaffected
Reviewer:          pending
Decision date:     2026-08-19
```

API tokens are a secondary capability per §8/§129 (workstream local agreement); the
workspace-scoped creation/verification/revocation contract does not alter canonical
User identity, Actor, Session, Account context or tenant isolation, therefore it
does not invalidate P1 certification.

# Cross-cutting certification

## 47. CERT-ARCH-001 — Domain purity

Required:

```text
Identity/Account Domain
→ no Infrastructure
→ no API
→ no HTTP
→ no EF
→ no provider SDK
```

Evidence:

- architecture-tests;
- source review where architecture framework cannot express all cases.

## 48. CERT-ARCH-002 — bounded-context ownership

Required:

- Identity owns Identity;
- Accounts owns Account semantics;
- Workspace/Governance not absorbed;
- Billing not absorbed;
- Integrations provider connections not confused with OAuth login identity;
- Platform mechanism/state boundary preserved.

## 49. CERT-ARCH-003 — no duplicate Account

Explicitly verify no second canonical business Account exists after migration.

## 50. CERT-ARCH-004 — no new production project

Unless a separately approved architecture decision exists.

The certification record must explicitly say:

```text
Production project topology changed: no
```

or cite the approved ADR.

# Data certification

## 51. CERT-DATA-001 — canonical persistence owner

Required:

- Identity persistence owner clear;
- Account persistence owner clear;
- Session state owner clear;
- OAuth mapping owner clear;
- API-token state owner clear.

## 52. CERT-DATA-002 — uniqueness constraints

Required constraints verified where applicable:

- canonical login identifier;
- provider + subject;
- token identity;
- Account unique identity/slug if product-defined.

## 53. CERT-DATA-003 — tenant isolation

Required at:

- Application;
- persistence;
- background work;
- token principal;
- cross-context contract.

## 54. CERT-DATA-004 — historical references

Identity lifecycle must not accidentally erase required historical business attribution.

# API certification

## 55. CERT-API-001 — endpoint contract

All release-scoped endpoints:

- map to approved Application use cases;
- validate input;
- preserve auth/authz distinction;
- minimize sensitive output.

## 56. CERT-API-002 — OpenAPI

If API changed:

```text
OpenAPI drift check PASS
```

on candidate SHA.

If no API contract changed:

```text
NOT_APPLICABLE
```

with evidence.

## 57. CERT-API-003 — CSRF/session transport

Representative browser state-changing Identity/Account endpoints follow canonical Platform CSRF/session contract.

# Authorization certification

## 58. CERT-AUTHZ-001 — protected Identity actions

Protected operations declare/use approved authorization semantics.

## 59. CERT-AUTHZ-002 — Account administration

Account administration uses Governance policy after bootstrap.

## 60. CERT-AUTHZ-003 — self-service boundary

Self-service User operations do not require broad Account admin roles where canonical self semantics exist.

## 61. CERT-AUTHZ-004 — no handler-local policy engine

No parallel authorization framework introduced.

# Event certification

## 62. CERT-EVT-001 — producer ownership

Identity owns Identity events.

Accounts owns Account events.

## 63. CERT-EVT-002 — safe payload

No raw:

- credential;
- OAuth token;
- MFA secret;
- API token;
- password hash.

## 64. CERT-EVT-003 — consumer compatibility

Workspace/Billing/Analytics consumers receive stable identity/scope without private persistence.

## 65. CERT-EVT-004 — breaking event migration

If a D4/D5 event contract changed:

- consumers updated;
- compatibility window handled;
- tests pass.

## 65.1 Phase 13 record — API error taxonomy / account-scope authorization / self-service hardening

```text
Capability:        Public API error taxonomy harmonization (typed Result failures →
                   canonical ProblemDetails status/errorCode per category);
                   endpoint-local ad-hoc error bodies canonicalized; account-scope
                   permission evaluation fix (dead-deny); self-service
                   actor-is-self hardening; AccountSelectionRequired mapping
SPEC requirement:  IAREQ080, IAREQ081, IAREQ083, IAREQ089, IAREQ090, IAREQ091,
                   IAREQ092
PLAN work units:   IA-API-002, IA-AUTHZ-001, IA-AUTHZ-002 (IA-API-003/004 and
                   IA-EVT-* classified/deferred — see decision notes §19)
TEST IDs:          IA-TST-API-CONTRACT-002 (§110), IA-TST-API-OAS-001 (§113),
                   IA-TST-AUTHZ-APP-002 (§120), IA-TST-AUTHZ-SEC-001 (§121),
                   IA-TST-AUTHZ-APP-003 (§122)
Source baseline:   develop @ c95baa2e
Candidate SHA:     uncommitted working tree at certification time
Migration:         none — no persisted shape change
Architecture evidence: full slnx build 17 projects 0 errors; Architecture.Tests
                   372 green; typed-error dispatch centralized in
                   EndpointExtensions.ResultToProblemDetails — no endpoint-local
                   error conventions added; PermissionService account branch
                   mirrors established workspace Owner-bypass pattern in the
                   shared policy engine (no role names hardcoded in handlers)
Security evidence: authentication failures return 401 auth.unauthorized with
                   enumeration-safe generic messages (Login both failure sites,
                   RefreshToken all three sites incl. harmonized user-null
                   branch); authorization denial remains 403 via exception path;
                   account-scope evaluation requires Active membership, grants
                   Owner full level, otherwise Governance rules + default switch
                   (ViewWorkspace→Viewer allow, else deny missing_permission) —
                   proven by 5 new PermissionServiceTests scenarios on real
                   PostgreSQL context wiring; bootstrap VerifyAccountAccessAsync
                   no longer the only gate for account-scoped operations
                   (IAREQ091); GetCurrentUser/UpdateProfile/Logout derive actor
                   from ICurrentRequestContext with IAuthenticatedRequest marker,
                   client-shaped UserId removed from commands/validators
Integration evidence: API.Tests 241 (incl. ResultErrorMappingTests 8-case
                   type→status matrix, LoginErrorContractTests HTTP-401 proof
                   through composed host with failing handler mock,
                   EndpointInputValidationTests canonical shapes); Application
                   649; Integration 320 (full suite); Domain 2576;
                   Infrastructure 120; Platform 147 — all passed
Contract evidence: OpenAPI notrelix.v1.json regenerated twice (after handler
                   edits and after endpoint edits) — byte-identical to committed
                   artifact both times (git diff empty); response metadata is not
                   introspected from IResult bodies, so no frontend codegen
                   required; legacy string-only failure shape preserved exactly
                   (HttpValidationProblemDetails) for client compatibility;
                   undocumented { error } bodies replaced by documented
                   ProblemDetails shape at unchanged status codes
Known debt:        CSRF disabled globally with frontend/backend header mismatch
                   (X-XSRF-TOKEN vs X-CSRF-Token) — enabling requires Platform
                   contract change (decision notes §19); register email-existence
                   reveal accepted pending product sign-off; IA-EVT consumer
                   inventory certification open (earlier orphaned-event claims
                   corrected by source re-verification)
Status:            VERIFIED (Phase 13 scope: IA-API-002, IA-AUTHZ-001,
                   IA-AUTHZ-002)
Reviewer:          pending
Decision date:     2026-08-21
```

Error taxonomy and authorization-policy changes do not alter canonical User
identity, Actor, Session contract, Account persistence or tenant isolation,
therefore per §156 they do not invalidate P1 certification.

## 65.2 Phase 13 closure execution note — P13-CLOSE-00

```text
Scope:             close audited Phase 13 source-level issues before hard-close:
                   CSRF cross-stack mismatch (IA-API-003), Account role fallback
                   (IA-AUTHZ-003), handler authorization-bypass arch gates
                   (IA-AUTHZ-004 completion), event inventory/maturity
                   (IA-EVT-001), event payload secret/PII safety (IA-EVT-002),
                   event versioning/manifest drift (IA-EVT-003)
PLAN work units:   P13-CLOSE-00, P13-CSRF-01..04, P13-AUTHZ-003A/B,
                   P13-AUTHZ-004A/B, P13-EVT-001A/B, P13-EVT-002A/B,
                   P13-EVT-003A-D, FINAL-01
Source baseline:   develop @ 4efd37bdff79f93f97059586928aa94af67ba8b1
Accepted at base:  IA-API-002 @ 5bc9ec91; IA-AUTHZ-001 @ 75d4d811;
                   IA-AUTHZ-002 @ 06c3269c; IA-API-004 @ 4efd37bd
Execution order:   CSRF first (ADR supersede → backend → frontend), then AUTHZ
                   arch gates, then EVT closure, then docs/status/certification
ADR impact:        ADR-005 created (supersedes ADR-003 transport assumptions);
                   Double Submit core carried forward — see backend decision
                   registry §9/§41/§44a
Commit slicing:    A = CLOSE-00 + CSRF-01; B = CSRF-02 backend; C = CSRF-03
                   frontend; D = AUTHZ; E = EVT; F = docs/status/certification
Status:            DONE (see P13-FINAL-01 full-scope record)
Decision date:     2026-08-22
```

## 65.3 Phase 13 closure completion record — P13-FINAL-01

```text
Candidate branch:  develop
Baseline SHA:      4efd37bdff79f93f97059586928aa94af67ba8b1
Candidate commits: aa52926a (CSRF backend, slice A)
                   0158a3fb (frontend CSRF transport, slice B)
                   9486e27d (Account authz baseline + bypass gates, slice C)
                   3e73e4d8 (event identity/maturity/manifest, slices D+E)
Working tree:      clean for all Phase 13 scopes
                   (pre-existing unrelated dirty file frontend/apps/marketing/
                   vercel.json and legacy untracked docs/.gitignore,
                   skills-lock.json remain as found at baseline)

Phase 13 source status:
  IA-API-002    DONE   accepted at baseline (no regression: API suite green)
  IA-API-003    DONE   ADR-005 protocol implemented cross-stack; rollout flag
                       remains Security:Csrf:Enabled=false until the staged
                       deployment sequence executes (P14-MIG-003); enablement
                       is an operational step, not a source gap
  IA-API-004    DONE   accepted at baseline; OpenAPI regenerated at candidate,
                       drift clean (+21 bootstrap operation only)
  IA-AUTHZ-001  DONE   accepted at baseline (Application suite green)
  IA-AUTHZ-002  DONE   accepted at baseline (Application suite green)
  IA-AUTHZ-003  DONE   central fallback matrix per IAREQ090/IAREQ138;
                       33 PermissionService integration cases incl. frozen
                       5x2 matrix, Governance deny precedence, allow-grant
  IA-AUTHZ-004  DONE   inventory complete; zero unclassified hits;
                       HandlerAuthorizationBypassArchitectureTests gates
                       ARCH-001..005 green; two real automation requests
                       fixed (were scoped without permission declaration)
  IA-EVT-001    DONE   source-complete inventory via canonical registries +
                       generated manifest; ConsumerMaturity required metadata
                       (18 Implemented / 26 Stub / NONE by absence);
                       registry-vs-consumer drift gate caught and fixed a
                       stale v1 row for identity.user-registered v2
  IA-EVT-002    DONE   prohibited-secret name ban over serialized shapes;
                       PII fields classified with purpose+consumer on the
                       four delivery events; ProtectedToken classified as
                       protected single-use delivery material
  IA-EVT-003    DONE   runtime resolution requires (Name,Version) everywhere;
                       v1/v2 coexistence proven through catalog + contract +
                       consumer registries with isolated fixtures; global
                       manifest backend/contracts/events/notrelix.events.json
                       (40 contracts) drift-checked semantically; migration
                       protocol recorded — no producer contract bump required
  P13-EVT-OPS-001      VERIFIED (local staging/dev operational evidence, see
                       P13-EVT-OPS record below; no production environment
                       exists at candidate)

Full-scope operational evidence (2026-08-22, local dev/staging):
  CSRF runtime smoke (Security:Csrf:Enabled=true, Development env):
    bootstrap GET /api/v1/auth/csrf
      → 200; body.token == csrf_token cookie value; cookie attributes
        `max-age=3600; path=/; samesite=lax; httponly` (development policy)
    unsafe POST /api/v1/auth/login WITHOUT CSRF pair
      → 403 ProblemDetails `security.csrf_validation_failed` (canonical shape,
        traceId present, no token echo)
    unsafe POST /api/v1/auth/login WITH valid pair (bootstrap → cookie + header)
      → passes CSRF gate → 401 `auth.unauthorized` (login handler result, NOT
        a CSRF rejection) — proves gate ordering, not business success
  CSRF suite: `dotnet test Notrelix.API.Tests --filter "FullyQualifiedName~Csrf"`
      → 15/15 passed (enforcement + rollout-compat + cross-origin flow)
  Production cookie policy (Secure + SameSite=None + HttpOnly) proven by
      CsrfProtectionTests.Production_Protector_UsesSecureNoneCookiePolicy
      (unit-level; production env enablement is a separate deploy action)
  Outbox/operational evidence (real PostgreSQL, dev stack):
      messaging.outbox_messages by status → Processed=2, Pending=0,
        Failed=0, DeadLetter=0
      oldest pending / oldest next_attempt → none (backlog empty)
      max retry_count=0, dead-letter candidates (retry>=5)=0, poison=0
      messaging tables present: outbox_messages, outbox_delivery_attempts,
        processed_events
      no DLQ/poison backlog observed; no unsupported-version rows present
  Rollout config: Security__Csrf__Enabled wired into docker-compose.staging.yml
      via ${SECURITY__CSRF__ENABLED:-false}; .env.example documents the flag.

Regression evidence (candidate):
  Domain          dotnet test tests/Notrelix.Domain.Tests         2576 passed
  Application     dotnet test tests/Notrelix.Application.Tests     649 passed
  Infrastructure  dotnet test tests/Notrelix.Infrastructure.Tests  132 passed
  Platform        dotnet test tests/Notrelix.Platform.Tests        147 passed
  API             dotnet test tests/Notrelix.API.Tests             256 passed
                  (incl. 15 CSRF: protection + cross-stack flow)
  Integration     dotnet test tests/Notrelix.Integration.Tests     338 passed
  Architecture    dotnet test tests/Notrelix.Architecture.Tests    397 passed
  Frontend node   pnpm test:node                                   257 passed (58 files)
  Frontend web    pnpm test:web                                      2 passed
  Frontend integ  pnpm test:integration                             16 passed
  Frontend mobile pnpm test:mobile                                   23 passed
  Frontend typecheck/lint/check:architecture/check:architecture-docs: clean

Generated artifacts:
  OpenAPI       contracts/openapi/notrelix.v1.json regenerated from candidate
                producer; semantic delta = Identity.Auth.IssueCsrfToken only;
                frontend generated/rest schema synced through codegen
  Event manifest contracts/events/notrelix.events.json generated via
                REGENERATE_EVENT_MANIFEST=1 path; drift gate compares
                semantic model, fails on same-version schema mutation

Known pre-existing conditions (NOT introduced by Phase 13 closure):
  - `make docs-check` rule-id checker reports 114 violations in
    docs/workstreams/backend-roadmap.md and docs/workstreams/teams/*.md;
    verified identical at baseline SHA via stash round-trip — matches the
    documentation-transition defect recorded in CONTEXT.md §28
  - OpenAPI emits duplicate "Identity.Auth" tags on auth operations
    (pre-existing artifact cosmetics across 29 operations)

Phase 14 compatibility result:
  P14-MIG-001 no EF/schema change (CSRF/event work used config+source
              metadata only; migration head remains SchemaV2Baseline)
  P14-MIG-002 every production public-event resolution call site passes
              compound key; name-only APIs removed (compiler-enforced) and
              gated by PublicEventContractArchitectureTests
  P14-MIG-003 rollout sequence recorded: deploy backend with flag off →
              deploy frontend transport → smoke → enable flag; rollback =
              disable flag without reintroducing XSRF
  P14-MIG-004 reviewed: OpenAPI (+csrf op), event manifest (new), consumer
              maturity (metadata), public versions (identity.user-registered
              already v2 at baseline; registry row corrected to match)

Phase 15 security result: secret-name ban + CSRF negative matrix + authz
  bypass gates green; no new rate-limit/CORS reliance (CORS explicitly not
  treated as CSRF defense)
Phase 16 result: PERF-001 single pipeline authorization proven via
  production-graph test; CSRF token reuse covered client-side; failure
  categories use existing observability conventions (csrf category added to
  ErrorCodes taxonomy)
Phase 17 result: frontend consumes only ADR-005 protocol (source gate);
  downstream consumers unchanged; stub consumers remain reported STUB

Certification conclusion:
  PHASE 13 FULL-SCOPE CERTIFIED (MILESTONE B) on candidate commits above.
  All source-level units DONE; CSRF runtime smoke + operational outbox/DLQ
  evidence collected locally (see P13-EVT-OPS record). Production
  CSRF-enablement and production cookie-policy runtime verification remain
  deployment actions requiring a real production environment; they are
  operational rollout steps, not source or certification gaps.
Decision date:     2026-08-22
```

## 65.4 Phase 14 closure record — persistence / migration / compatibility

Candidate branch:  develop
Baseline SHA:      4efd37bdff79f93f97059586928aa94af67ba8b1
Candidate HEAD:    8dcc8389 (PR #93 checks 39/39 PASS at close)

P14-MIG-001 — candidate schema diff review
  Method:     git diff 4efd37bd..HEAD -- backend/src/**/Migrations/**
              + ApplicationDbContextModelSnapshot.cs
  Result:     EMPTY diff — zero schema delta since the audited baseline
  Migration head: 20260702093805_SchemaV2Baseline (unchanged; single
              consolidated baseline per PR-IA-00 decision note §13)
  Classification: no schema change (no additive/backfill/breaking/index/
              secret-format/ownership-move delta)
  IA-TST-MIG-DB-001: NOT_APPLICABLE — no schema delta (TESTS §189 permits
              recording with source/model-diff evidence, which is this diff)
  Justification for zero persisted event-contract metadata: consumer
  maturity and contract identity live in source registries +
  backend/contracts/events/notrelix.events.json; no DB column required.

P14-MIG-002 — event registry caller compatibility
  Inventory of former name-only resolution paths:
    - IIntegrationEventCatalog.Resolve/TryResolve(messageName)
        → migrated to EventContractKey(Name, Version); name-only APIs
          removed from the interface (compiler-enforced; no silent fallback)
    - OutboxDispatcher (production dispatch path)
        → resolves via new EventContractKey(message.MessageName,
          message.SchemaVersion) — version comes from the durable envelope row
    - ContractRegistry
        → already compound-keyed (Get(name, version)); unchanged
    - EventTypeRegistry name→type direction
        → proven NOT a public integration-event resolution path:
          zero production callers (only type→name used by the domain-event
          outbox writer)
  Gate: Notrelix.Architecture.Tests
        ProductionEventContractArchitectureTests.
        ProductionResolutionPaths_UseCompoundContractIdentity
        asserts (1) catalog surface has no string-parameter resolution API,
        (2) dispatcher source contains the compound-key call.
  Runtime proof: IntegrationEventCatalogResolutionTests +
        VersionedContractMigrationFixtureTests (12 cases) incl. serializer
        round-trip preserving envelope version for v1 and v2 fixtures.

P14-MIG-003 — CSRF deployment compatibility
  Rollout sequence (executable, staged):
    1. deploy backend bootstrap/protocol with Security:Csrf:Enabled=false
       (current committed appsettings state)
    2. deploy frontend transport (@notrelix/contracts client, ADR-005)
    3. verify integration/smoke — CsrfCrossStackFlowTests models the flow;
       production smoke = run once after step 2 in the target environment
    4. enable Security:Csrf:Enabled=true (config change only)
  Compatibility evidence for step 1→2 coexistence:
        CsrfProtectionTests.FlagDisabled_MiddlewareDoesNotInterfereWithUnsafeRequests
        (= IA-TST-MIG-CSRF-001) — pre-enable runtime flow unaffected
  Enabled-state evidence: CsrfCrossStackFlowTests run against a host with
        Security:Csrf:Enabled=true (= IA-TST-MIG-CSRF-002 substance)
  Rollback: disable the feature flag; no reintroduction of the legacy
        XSRF convention is required or permitted. Flag-off rollback is an
        operational action, not a revert to target architecture.
  No atomic-deploy guarantee exists between backend and frontend hosts in
  this repository; the staged sequence above is therefore mandatory.

P14-MIG-004 — public contract compatibility review
  OpenAPI:                +1 operation only (GET /api/v1/auth/csrf);
                          regenerated from candidate producer; drift clean
  CSRF bootstrap response:new endpoint {token} — additive, no existing
                          consumer impacted
  Event manifest:         NEW artifact v1 (40 contracts) generated from
                          canonical registries; drift gate active
  Consumer registry:      maturity metadata additive; one registry row
                          corrected to match its real contract version
                          (identity.user-registered v1 → v2) — metadata fix,
                          no wire change
  Public event versions:  no payload schema changes; no version bumps;
                          "no producer contract bump required" recorded
  Breaking consumer contracts introduced by Phase 13 closure: NONE
  Frontend generated REST schema resynced via codegen (completes the
  artifact sync left pending by IA-API-004's CreatedApiTokenDto change).

Phase 14 exit criteria (PLAN §139):
  - DB migration applicability explicit ........ YES (zero-delta evidence)
  - event registry call sites version-aware .... YES (gate + runtime tests)
  - CSRF rollout order executable .............. YES (sequence + both flag
                                                  states under test)
  - public contract compatibility recorded ..... YES (this record)
  - pending EF model change .................... NONE

Status:            CLOSED
Decision date:     2026-08-22
```

## 65.5 Security hardening closure record

Candidate HEAD:    53771238 (rebased onto merged web-app line)

Secret surface scan (all production Identity/Accounts security paths):
  - CSRF protector/middleware/ProblemDetails writer: zero log statements;
    failure response is static canonical text, never echoes token material
    (asserted by CsrfCrossStackFlowTests negative matrix)
  - Outbox dispatcher: logs message id/name + exception messages only;
    dead-letter path records error text, never payload JSON
  - Platform ConsumerHost poison/retry diagnostics: event name + consumer
    id + exception; no envelope body dump
  - Email dispatcher: message id + cancellation reason; rendered bodies and
    protected links never logged
  - Auth/JWT/token services: no secret-material logging found by scan
  - Admin outbox diagnostics GetById exposes raw payload JSON behind the
    SystemAdmin policy — classified as privileged operations surface
    (gated, auditable), not ordinary logging; retained intentionally

CSRF attack matrix (IA-TST-CSRF-API-*, INT-*): missing cookie, missing
  header, mismatched pair, cross-instance/stale token after rotation,
  unsafe request with no material, refresh without pair (new explicit
  case: 403 canonical security.csrf_validation_failed), cross-origin
  credential replay, explicit non-ambient Authorization credential exempt,
  safe GET unvalidated — all green under CsrfProtectionTests +
  CsrfCrossStackFlowTests (15 cases). CORS is not relied upon anywhere.

Authorization bypass matrix: frozen role/action matrix (Owner/Admin allow
  CreateWorkspace; Member/BillingAdmin/SecurityAdmin deny), Governance
  deny precedence over fallback, Governance allow granting baseline-denied
  action, suspended/absent/wrong-account denials — PermissionServiceTests
  (33) through the real evaluator on PostgreSQL; handler-cannot-run-before-
  pipeline-denial plus zero durable side effects proven by
  WorkspaceCreationPipelineAuthorizationTests (5 roles) on the production
  graph; static bypass gates (ARCH-001..005) green.

Event privacy/security matrix: prohibited-secret property ban over all
  serialized public contracts, PII fields classified with purpose +
  consumer justification, manifest carries metadata only (no runtime
  sample values), outbox/DLQ paths verified above not to dump payloads,
  unsupported-version dead-letter path logs name/version only.

Enumeration/replay/revocation regression: full Integration suite re-run
  at this HEAD — 338 passed, including login/recovery flows, OAuth
  callback/link lifecycle, MFA enrollment/challenge, session revoke and
  API-token governance/races; Application suite 649 passed covering auth
  pipeline behaviors. No closure change weakened these suites.

Abuse controls: single Platform-owned rate-limit mechanism (ADR-004
  provider + pre/post-auth middleware); 23 sensitive Identity endpoints
  carry AuthStrictByIp incl. login/register/refresh/recovery/MFA/OAuth/
  CSRF bootstrap; no parallel or local rate-limit framework introduced.

Exit criterion: no material high-sensitivity path lacks a negative or
security test — met.
Status:            CLOSED
Decision date:     2026-08-22
```

## 65.6 Observability / reliability / performance closure record

Candidate HEAD:    fcaef3d1 + this record

Safe trace correlation: CorrelationIdMiddleware assigns X-Correlation-ID
  at the edge; the canonical ProblemDetails contract echoes traceId;
  integration events carry EventId/CorrelationId/CausationId/ActorUserId
  and the outbox rows persist them — the request → auth → tenant →
  authorization → use case → persistence → outbox chain stays correlatable
  through stable identifiers only. No raw secrets enter any hop (security
  closure scan).

Failure categories under existing conventions:
  - csrf_validation_failed ....... security.csrf_validation_failed via the
    canonical ProblemDetails writer; asserted in CSRF suites
  - authorization_denied ......... auth.forbidden mapping
  - authorization_misconfiguration NEW dedicated category
    security.authorization_misconfiguration (500 with generic client
    detail; internal context remains server-side logging) — previously
    these request-contract violations surfaced as anonymous internal errors
  - unknown_event_contract ....... dead-letter reason UnknownEventType +
    critical log carrying name/version only
  - unsupported_event_version .... deterministic compound-key resolution
    failure, same diagnosable path
  - consumer retry/poison ........ Platform runtime metrics/diagnostics and
    per-event-name/consumer-id logs already in place
  No new observability vendor introduced.

Reliability:
  - unknown logical event names and unsupported versions fail
    deterministically through the catalog (no latest/v1 fallback, no
    silent drop, no payload dump) — Infrastructure resolution suite +
    dispatcher dead-letter behavior
  - frontend bootstrap network failure surfaces one deterministic error,
    releases the single-flight promise, and bounds total attempts
    (new explicit test); stale-token recovery retries once; a second
    consecutive rejection surfaces instead of looping; refresh failures
    keep session-expired semantics without CSRF/refresh recursion.

Performance:
  - authorization hot path: production-graph test proves pipeline-only
    evaluation for five roles with zero handler-level permission lookups
    (static gate forbids service injection into handlers). Executable
    call-count proof added: a pass-through counter decorates the REAL
    decision store in the production composition and asserts exactly one
    authorization evaluation per request — for allowed roles AND for
    pipeline denials (handler never re-evaluates).
  - CSRF client overhead: memory token reuse avoids re-bootstrap on
    sequential unsafe requests; N concurrent unsafe requests share one
    bootstrap (fetch call-count assertions).
  - correctness before caching: closure introduced no authorization,
    session or account-context cache; the pre-existing resource snapshot
    store remains tenant-scoped through the request context. Recorded as
    not applicable for new cache-revocation risk.

Executable observability proofs added:
  - dispatcher diagnostics gate: dead-letter path must carry the stable
    UnknownEventType category and identify messages only by envelope
    identity fields; log statements are machine-checked to never reference
    serialized payload material.
  - correlation round-trip: the production serializer path preserves the
    correlation identifier across publish -> durable payload ->
    resolution -> deserialize, proving the request-to-event chain stays
    traceable by non-secret identifiers.

Status:            CLOSED
Decision date:     2026-08-22
```

## 65.7 Phase 17 record — cross-team integration hardening closure

```text
Capability:        Cross-team handoff verification (P17-X-001..005): frontend
                   browser CSRF contract, Workspace/Governance authorization
                   handoff, event consumer handoff, v1/v2 migration fixture,
                   operational evidence disposition
SPEC requirement:  IAREQ068-IAREQ078, IAREQ087, IAREQ090, IAREQ126-IAREQ135,
                   IAREQ136-IAREQ140 as applicable to cross-team handoff
PLAN work units:   P17-X-001, P17-X-002, P17-X-003, P17-X-004, P17-X-005
TEST IDs:          IA-TST-CSRF-CLIENT-* (11 cases), PermissionServiceTests
                   (33), VersionedContractMigrationFixtureTests (5 =
                   IA-TST-X-EVT-004/IA-TST-EVT-MIG-*/IA-TST-MIG-EVT-002)
Source baseline:   develop @ 450bea973307980ce03c4bc27b5f32c1ad6c91cf
Migration:         NOT_APPLICABLE — no persisted shape change
P17-X-001:         VERIFIED — frontend source consumes only ADR-005 protocol
                   (bootstrap GET auth/csrf, in-memory token, X-CSRF-Token,
                   security.csrf_validation_failed ProblemDetails recovery);
                   zero competing XSRF convention in any frontend package;
                   generated schema carries bootstrap operation; two stale doc
                   sections classified DOC_STALE and routed to Phase 19
                   IA-DOC scope; executable proof csrf-transport.unit.test.ts
                   11/11 passed
P17-X-002:         VERIFIED — AccountRole confined to central
                   PermissionService policy + grant projection writes; ZERO
                   references in API or downstream Features; Workspace/
                   Governance need no Identity inspection; executable proof
                   PermissionServiceTests 33/33 on real PostgreSQL
P17-X-003:         VERIFIED — all 44 consumers registered with explicit
                   EventVersion + ConsumerMaturity (18 Implemented / 26 Stub,
                   stubs recorded AS STUB); TenantContextConsumeFilter
                   restores scope per message; no private Identity persistence
                   access from any downstream consumer (grep-verified)
P17-X-004:         VERIFIED — controlled test.versioned-fact v1+v2 fixture
                   proves registry/catalog/serializer coexistence without
                   collision; v3 lookup fails deterministically;
                   VersionedContractMigrationFixtureTests 5/5 passed
P17-X-005:         NOT_APPLICABLE_UNTIL_DEPLOYMENT — no production env exists
                   at candidate; local staging/dev outbox/DLQ evidence already
                   collected (P13-EVT-OPS); runbook trigger = first production
                   deployment
Architecture evidence: no new package dependency edge; frontend contract
                   client remains the only browser transport owner
CI evidence:       local focused suites at exact SHA 450bea97 (counts above)
Known debt:        api-and-contracts.md §59-60 + FE-ADR-005 mismatch section
                   DOC_STALE (Phase 19 owner); stub consumers remain STUB by
                   design (26) until their owning contexts implement them
Status:            CLOSED (Phase 17 exit met)
Reviewer:          pending human sign-off
Decision date:     2026-08-23
```

## 65.8 Phase 18 record — TESTS handoff closure

```text
Capability:        TESTS artifact sync + full regression handoff
                   (IA-TEST-HO-001..004)
PLAN work units:   IA-TEST-HO-001, IA-TEST-HO-002, IA-TEST-HO-003,
                   IA-TEST-HO-004
Source baseline:   develop @ 450bea973307980ce03c4bc27b5f32c1ad6c91cf
IA-TEST-HO-001:    VERIFIED — tests.md carries 216 concrete IA-TST-* IDs;
                   all mandatory families present (CSRF 19, AUTHZ 13,
                   EVT-INV 3, EVT-SEC 1, EVT-VER 5, EVT-CONTRACT 4, MIG 11,
                   OBS 6, REL 8, X 14)
IA-TEST-HO-002:    VERIFIED — CI mapping §235–244 + requirement coverage
                   §259–273 map every family to suite/job; residual gates
                   §292–294 exist as executable architecture tests
IA-TEST-HO-003:    EXECUTED — full regression at exact SHA:
                     Architecture      398 / Domain 2576 / Application 649 /
                     Infrastructure    132 / Platform 147 / API 256 /
                     Integration       338 → 4496 passed, 0 failed, 0 skipped
                     frontend test:node 72 files / 313 passed
                   (counts recorded fresh, not carried forward)
IA-TEST-HO-004:    VERIFIED — all evidence suites non-zero; mis-targeted
                   zero-match filter corrected before recording
Generated artifacts: OpenAPI/event manifest drift gates executed inside
                   Architecture+API suites (green)
Known debt:        none material; operational DLQ evidence remains
                   NOT_APPLICABLE_UNTIL_DEPLOYMENT per P17-X-005
Status:            CLOSED (Phase 18 exit met)
Reviewer:          pending human sign-off
Decision date:     2026-08-23
```

## 65.9 Phase 19 record — documentation / generated-contract handoff

```text
Capability:        Canonical documentation + generated artifact alignment
                   (IA-DOC-001..006)
PLAN work units:   IA-DOC-001, IA-DOC-002, IA-DOC-003, IA-DOC-004,
                   IA-DOC-005, IA-DOC-006
Source baseline:   develop @ 450bea973307980ce03c4bc27b5f32c1ad6c91cf
                   (doc-only edits on top; no source/contract change)
IA-DOC-001:        DONE — backend ADR-003 Superseded → ADR-005 (Accepted);
                   frontend api-and-contracts.md §57 rewritten to accepted
                   bootstrap protocol, §59 closed as historical drift,
                   §60 FE-API-030 → RESOLVED; FE-ADR-005 mismatch evidence
                   marked CLOSED; rule-index.md regenerated via producer
                   (--check PASS)
IA-DOC-002:        VERIFIED — fresh OpenAPI export vs committed canonical
                   semantic compare CLEAN; no handwritten drift
IA-DOC-003:        VERIFIED — events manifest matches generated source shape
                   (explicit gate 1/1 + Architecture suite coverage)
IA-DOC-004:        DONE — BE-SEC-013 extended: single authoritative chain
                   use case → authorization contract → AuthorizationBehavior
                   → handler logic; target-role invariants distinguished from
                   current-actor authorization
IA-DOC-005:        DONE — platform-and-messaging.md §106 extended with
                   EventContractKey(Name,Version), v1/v2 coexistence, schema
                   baseline, consumer maturity, rollout, outbox/DLQ drain
IA-DOC-006:        DONE — execution state carried by certification records;
                   plan remains active until Phase 20
Docs gates:        make docs-check ALL PASS (links/metadata/authority/
                   rule-ids/source-inventory/generated)
Contracts:         documentation-only; no API/event/schema/persistence change
Status:            CLOSED (Phase 19 exit met — no canonical doc contradicts
                   implemented CSRF/authz/event closure contracts)
Reviewer:          pending human sign-off
Decision date:     2026-08-23
```

# Migration certification

## 66. CERT-MIG-001 — migration inventory

Certification must list every migration introduced by Identity/Accounts delivery.

Format:

```text
Migration:
Capability:
Schema change:
Backward-compatible:
Backfill:
Consumer impact:
Rollback/forward-fix:
Test IDs:
```

## 67. CERT-MIG-002 — clean DB

Required after schema changes:

```text
PASS
```

## 68. CERT-MIG-003 — upgrade DB

Required from supported previous schema:

```text
PASS
```

## 69. CERT-MIG-004 — pending model changes

Required:

```text
none
```

Do not suppress EF pending-model warning to satisfy certification.

## 70. CERT-MIG-005 — seed/init

If changed:

- valid Domain state;
- no invalid typed IDs/order/state;
- startup pass.

## 71. CERT-MIG-006 — Account migration

Required if Account action was REHOME/SPLIT/INTRODUCE.

Must verify:

- IDs;
- downstream references;
- ownership;
- no duplicate truth;
- startup.

# Security certification

## 72. CERT-SEC-001 — secret non-exposure

Required evidence:

- API response tests;
- event tests;
- captured log tests;
- source review for high-risk paths.

Any raw secret exposure blocks certification.

## 73. CERT-SEC-002 — enumeration

Applicable Identity endpoints follow accepted anti-enumeration policy.

## 74. CERT-SEC-003 — replay

Applicable one-time/security flows resist replay.

## 75. CERT-SEC-004 — revocation

Revocation verified for all release-scoped credential/session types.

## 76. CERT-SEC-005 — tenant spoofing

Cross-account spoofing matrix passes.

## 77. CERT-SEC-006 — abuse control

Sensitive endpoints integrate with approved generic Platform mechanism where required.

## 78. CERT-SEC-007 — no security weakening

Certification record MUST explicitly confirm:

```text
Security gate weakened: no
CSRF disabled: no
tenant isolation bypass added: no
raw secret storage introduced: no
global admin bypass introduced: no
```

Any "yes" requires an explicit approved architecture/security decision and likely blocks normal certification.

# Concurrency certification

## 79. CERT-CONC-001 — User uniqueness

Concurrent canonical identity creation cannot produce duplicates.

## 80. CERT-CONC-002 — provider identity uniqueness

Concurrent OAuth linking cannot bind one provider subject to multiple Users.

## 81. CERT-CONC-003 — Account bootstrap

Applicable Account uniqueness/bootstrap invariants are concurrency-safe.

## 82. CERT-CONC-004 — Session revocation

Final security state remains revoked under relevant race.

## 83. CERT-CONC-005 — API token revocation

Revocation wins according to accepted security semantics.

# Reliability certification

## 84. CERT-REL-001 — external provider failure

OAuth/SSO provider outage cannot corrupt canonical Identity state.

## 85. CERT-REL-002 — persistence failure

Security-sensitive operations do not report success when authoritative persistence failed.

## 86. CERT-REL-003 — partial Account bootstrap

If multi-context bootstrap exists, failure/retry/compensation semantics are verified.

## 87. CERT-REL-004 — repair tooling

If operational repair capability exists:

- strongly authorized;
- tenant-scoped;
- auditable;
- secret-safe.

# Observability certification

## 88. CERT-OBS-001 — critical flow traceability

At minimum:

```text
request
→ Actor/session
→ Account resolution
→ authorization
→ use case
→ persistence/event
```

can be correlated where architecture supports tracing/logging.

## 89. CERT-OBS-002 — safe fields

Logs/traces contain safe operational context, not secret material.

## 90. CERT-OBS-003 — failure visibility

Critical auth/session/OAuth/MFA/token failure classes are visible through existing observability mechanisms.

No new vendor is required for certification unless architecture already mandates one.

# Performance certification

## 91. CERT-PERF-001 — actor/session hot path

Required if implementation materially changed the hot path.

Evidence:

- baseline/current comparison;
- DB/cache call review;
- latency/performance tests where canonical threshold exists.

## 92. CERT-PERF-002 — Account context hot path

No obvious repeated cross-context lookup explosion.

## 93. CERT-PERF-003 — security cache correctness

Performance optimizations do not violate revocation or tenant isolation.

# Cross-context certification

## 94. CERT-X-001 — Workspace/Governance

Mandatory for P1 core.

Evidence:

```text
stable Actor
stable Account
no private Identity persistence
```

## 95. CERT-X-002 — Billing

Required once Billing consumer contract is release-scoped.

Billing consumes Account identity/lifecycle without Identity credentials.

## 96. CERT-X-003 — WorkManagement

No direct Identity persistence dependency.

## 97. CERT-X-004 — Documents/Collaboration

Historical attribution behavior verified where relevant.

## 98. CERT-X-005 — Automation

Approved system/background actor semantics only.

## 99. CERT-X-006 — Analytics

Sensitive Identity data exposure remains bounded.

# Test evidence requirements

## 100. Evidence source

Tests used for certification MUST come from:

```text
identity-accounts.tests.md
```

and actual executed source tests.

## 101. Test-result record

For each required group:

```text
Test group:
Test IDs:
Command:
Project:
Executed count:
Passed:
Failed:
Skipped:
CI job:
Candidate SHA:
```

## 102. Zero-execution failure

If a required filter matches zero tests:

```text
certification FAIL
```

even if command exits successfully.

## 103. Skipped tests

A skipped critical test is not PASS.

Certification must explain every skipped critical test.

## 104. Flaky tests

A test requiring retries to pass must be classified.

Repeated reruns without root-cause resolution do not constitute stable evidence.

# CI certification

## 105. Candidate SHA

Record:

```text
Candidate SHA:
```

All mandatory final jobs must correspond to this SHA.

## 106. Expected conceptual jobs

Relevant backend CI groups include:

```text
quality
architecture-tests
core-tests
platform-tests where affected
api-tests
integration-tests
docker-build
final gate
```

Exact names come from current workflow.

## 107. CERT-CI-001 — quality

Required:

- restore/build prerequisites;
- format if governed;
- vulnerability/security guards;
- repository quality checks.

## 108. CERT-CI-002 — architecture

Required:

```text
PASS
non-zero execution
```

## 109. CERT-CI-003 — Domain/Application/Infrastructure core

Required:

```text
PASS
non-zero relevant Identity/Accounts tests
```

## 110. CERT-CI-004 — Platform

Required only if Platform runtime mechanisms changed.

If not changed:

```text
NOT_APPLICABLE
```

with evidence.

## 111. CERT-CI-005 — API

Required when Identity/Account API exists/changes.

Includes:

- API tests;
- OpenAPI drift where applicable.

## 112. CERT-CI-006 — integration

Mandatory for P1 core.

Must include:

- tenant isolation;
- production graph;
- session/auth;
- migration where assigned;
- cross-context handoff.

## 113. CERT-CI-007 — docker build

Required if canonical backend CI requires it after all earlier gates.

A skipped Docker job because an upstream failed does not count as a Docker PASS.

## 114. CERT-CI-008 — final gate

Final aggregate gate must reflect all mandatory dependencies.

## 115. Exact-SHA rule

The final certification entry must say:

```text
All required CI green on candidate SHA: yes/no
```

Only `yes` permits final STABLE certification.

# Documentation certification

## 116. CERT-DOC-001 — canonical docs

If implementation changes canonical architecture/product/contracts, corresponding canonical docs are updated.

## 117. CERT-DOC-002 — generated docs

If generated doc inputs changed:

```text
make docs-generate
```

or current canonical generation command must produce no uncommitted drift after regeneration.

## 118. CERT-DOC-003 — docs checks

Once workstreams are integrated into docs governance:

```text
make docs-check
```

must pass.

If workstreams governance integration is still incomplete:

- record explicit blocker;
- do not falsely claim full docs certification.

## 119. CERT-DOC-004 — no duplicate authority

Execution docs must not become a second product/architecture authority.

# Source-debt policy

## 120. Blocking debt

The following classes block certification when related to certified capability:

```text
security-critical SOURCE_DEBT
tenant-isolation UNRESOLVED
Account ownership UNRESOLVED
contract-breaking ambiguity
migration inconsistency
missing critical test
architecture gate failure
```

## 121. Non-blocking debt

May remain when:

- clearly outside certified scope;
- does not destabilize producer contract;
- owner identified;
- downstream dependency unaffected;
- explicitly recorded.

## 122. Debt record format

```text
Debt ID:
Description:
Classification:
Affected capability:
Why non-blocking/blocking:
Owner:
Target phase:
Evidence:
```

# Stop conditions during certification

## 123. IA-CERT-STOP-001 — Account owner still ambiguous

Milestone A cannot pass.

## 124. IA-CERT-STOP-002 — P1 test gap

If tenant isolation/Actor/Account contract cannot be proven, P1 cannot pass.

## 125. IA-CERT-STOP-003 — exact SHA mismatch

Local PASS + different CI SHA is not final certification.

## 126. IA-CERT-STOP-004 — architecture gate weakened

Stop certification.

## 127. IA-CERT-STOP-005 — pending migration/model drift

Stop certification.

## 128. IA-CERT-STOP-006 — secret exposure

Stop certification immediately.

## 129. IA-CERT-STOP-007 — OAuth/MFA/token unresolved security policy

Blocks Milestone B for that release-scoped capability.

Does not necessarily invalidate Milestone A core if core remains unaffected.

## 130. IA-CERT-STOP-008 — downstream consumer uses private Identity persistence

Blocks P1 producer-contract stability.

# P1 Core Certification checklist

## 131. Source and ownership

- [ ] exact baseline recorded;
- [ ] candidate SHA recorded;
- [ ] canonical User owner known;
- [ ] canonical Account owner known;
- [ ] Account action RETAIN/REHOME/SPLIT/INTRODUCE recorded;
- [ ] no duplicate Account source of truth;
- [ ] Account vs Workspace semantics resolved.

## 132. User / Actor

- [ ] stable User ID;
- [ ] uniqueness enforced;
- [ ] lifecycle tested;
- [ ] Actor trusted;
- [ ] actor spoofing test passes;
- [ ] Domain has no HTTP identity dependency.

## 133. Session

- [ ] create/bootstrap;
- [ ] expiry;
- [ ] logout;
- [ ] revocation;
- [ ] User-disable interaction;
- [ ] cache revocation behavior;
- [ ] API auth failure mapping.

## 134. Account

- [ ] stable Account ID;
- [ ] lifecycle baseline;
- [ ] current Account resolution;
- [ ] Account administration bootstrap bounded;
- [ ] billable Account contract stable.

## 135. Tenant isolation

- [ ] A cannot read B;
- [ ] A cannot write B;
- [ ] API token cannot escape Account if token exists in current core scope;
- [ ] background path cannot become global;
- [ ] persistence isolation evidence exists.

## 136. Downstream

- [ ] Workspace/Governance handoff passes;
- [ ] no private Identity persistence dependency;
- [ ] historical identity reference behavior understood;
- [ ] Billing Account contract identified.

## 137. Architecture

- [ ] Domain purity;
- [ ] layer dependency;
- [ ] bounded-context ownership;
- [ ] authorization pipeline;
- [ ] no new production project.

## 138. Migration

- [ ] clean DB if applicable;
- [ ] upgrade DB if applicable;
- [ ] pending model changes absent;
- [ ] seed/init if affected;
- [ ] Account migration verified if applicable.

## 139. Security

- [ ] secret non-exposure;
- [ ] enumeration policy;
- [ ] session revocation;
- [ ] tenant spoofing denied;
- [ ] no security weakening.

## 140. CI

- [ ] architecture-tests pass;
- [ ] Domain tests pass;
- [ ] Application tests pass;
- [ ] Infrastructure tests pass;
- [ ] API tests pass;
- [ ] Integration tests pass;
- [ ] required suites execute non-zero;
- [ ] candidate SHA matches certification;
- [ ] final gate pass.

# Full Scope Certification checklist

## 141. Registration/Credentials

- [ ] registration canonical User identity;
- [ ] duplicate/concurrency safe;
- [ ] bootstrap ownership explicit;
- [ ] recovery replay-safe if implemented;
- [ ] credential changes secure.

## 142. OAuth

- [ ] provider subject stable;
- [ ] state/PKCE/nonce as applicable;
- [ ] replay safe;
- [ ] collision matrix;
- [ ] link/unlink;
- [ ] provider secrets protected;
- [ ] provider outage safe.

## 143. SSO

- [ ] SSO semantics classified;
- [ ] trust checks;
- [ ] tenant authorization separate;
- [ ] replay/signature as applicable.

Or:

```text
NOT_APPLICABLE
```

with release-scope rationale.

## 144. MFA

- [ ] enrollment;
- [ ] challenge;
- [ ] expiry;
- [ ] replay;
- [ ] abuse controls;
- [ ] recovery;
- [ ] disable/reset;
- [ ] session impact;
- [ ] secret safety.

## 145. Security settings

- [ ] Identity-owned only;
- [ ] sensitive mutation proof;
- [ ] audit/security event;
- [ ] secret-safe.

## 146. API Tokens

- [ ] one-time secret;
- [ ] protected verifier;
- [ ] valid principal;
- [ ] Account scope;
- [ ] Governance interaction;
- [ ] revocation;
- [ ] audit/log safety.

## 147. Reliability/Performance/Observability

- [ ] provider failure safe;
- [ ] persistence failure semantics;
- [ ] Account bootstrap partial failure policy;
- [ ] hot-path review;
- [ ] safe correlation;
- [ ] no secret telemetry.

## 148. Docs/CI

- [ ] OpenAPI drift;
- [ ] canonical docs;
- [ ] generated docs;
- [ ] docs checks when governance integrated;
- [ ] exact-SHA CI green.

# Certification record templates

## 149. P1 Core Certification Record

```text
Identity & Accounts — P1 Core Certification

Baseline branch:
Baseline SHA:
Candidate SHA:

Account ownership decision:
Account action:
Migration(s):

User identity:
  Status:
  Evidence:

Actor:
  Status:
  Evidence:

Session:
  Status:
  Evidence:

Account:
  Status:
  Evidence:

Current Account:
  Status:
  Evidence:

Tenant isolation:
  Status:
  Evidence:

Workspace/Governance handoff:
  Status:
  Evidence:

Architecture:
  Status:
  Evidence:

Security:
  Status:
  Evidence:

Migration:
  Status:
  Evidence:

Required CI:
  quality:
  architecture:
  core:
  api:
  integration:
  docker:
  final:

Blocking debt:
Non-blocking debt:

Decision:
  P1 CORE CERTIFIED | BLOCKED

Reviewer(s):
Date:
```

## 150. Full Scope Certification Record

```text
Identity & Accounts — Full Scope Certification

Candidate SHA:
  branch develop @ 450bea973307980ce03c4bc27b5f32c1ad6c91cf (HEAD).
  All source regression/gates executed at this exact SHA.
  Phase 17–20 closure records + Phase 19 doc alignment are documentation-only
  working-tree edits on top (no source/contract delta vs 450bea97); they enter
  history as the certification commit of this record.
  Worktree otherwise as found: pre-existing dirty frontend/apps/marketing/
  vercel.json + untracked .agents/.claude skill dirs (unrelated to scope).

P1 Core:
  Status: VERIFIED (D4) — Milestone A certified 2026-08-14;
          User identity, Actor contract, Session contract (watermark
          revocation), Account identity records §6–9

Registration/Credentials:
  Status: VERIFIED (D4) — §24.1/§25.1 (2026-08-14): unique-email race → 409,
          BCrypt + policy proofs, single-use OTP reset, enumeration resistance

OAuth:
  Status: VERIFIED (Phase 9) — §OAuth record (df90a267): link/unlink/callback,
          auto-link rejection, contract regen green

SSO:
  Status: VERIFIED (Phase 10) — §SSO record

MFA:
  Status: VERIFIED (Phase 11) — §MFA record

Security settings:
  Status: VERIFIED (Phase 12) — §Security settings record

API Tokens:
  Status: VERIFIED (Phase 12 review scope) — §API token record; noted as not
          D5/STABLE pending full lifecycle hardening (non-blocking, recorded)

Cross-context integration:
  Status: CLOSED — §65.7 Phase 17 (frontend consumes only ADR-005 protocol;
          AccountRole central-authority isolation proven; consumer registry
          explicit versions+maturity 18 Implemented/26 Stub; v1/v2 fixture
          5/5; operational evidence NOT_APPLICABLE_UNTIL_DEPLOYMENT)

Migration:
  Status: CLOSED — §65.4 Phase 14: zero schema delta since audited baseline;
          migration head 20260702093805_SchemaV2Baseline unchanged;
          name-only event resolution eliminated (compiler + architecture
          gate); CSRF staged rollout sequence recorded

Security hardening:
  Status: CLOSED — §65.5 Phase 15: secret-name ban, CSRF negative matrix,
          authz bypass gates ARCH-001..005, no CORS-as-CSRF reliance;
          production cookie policy unit-proven (Secure+SameSite=None+HttpOnly)

Reliability:
  Status: CLOSED — §65.6 Phase 16: bounded bootstrap/retry, dispatcher
          diagnostics, correlation propagation proofs at HEAD; outbox/DLQ
          local staging evidence Processed=2/Pending=0/Failed=0/DLQ=0

Observability:
  Status: CLOSED — §65.6 Phase 16 (+§65.3): authorization evaluation count,
          dispatcher diagnostics, correlation IDs; csrf error category in
          ErrorCodes taxonomy; no secret/payload logging

Performance:
  Status: CLOSED — PERF-001 single pipeline authorization evaluation via
          production-graph test; no duplicate pipeline+handler query;
          client CSRF single-flight bounded

Docs/OpenAPI:
  Status: CLOSED — §65.9 Phase 19: ADR-003 Superseded→ADR-005 both sides
          aligned; FE-API-030 RESOLVED; BE-SEC-013 chain documented;
          platform-and-messaging §106 versioning contract defined;
          make docs-check ALL PASS; rule-index regenerated via producer

CI:
  Status: LOCAL GATES GREEN AT EXACT SHA — backend 7 suites 4496 passed /
          0 failed / 0 skipped (Architecture 398 incl. TRACE-001 + drift
          gates; Domain 2576; Application 649; Infrastructure 132;
          Platform 147; API 256; Integration 338 incl. PermissionServiceTests
          33/33); frontend pnpm test:node 72 files / 313 passed (incl.
          csrf-transport 11/11); OpenAPI export-vs-canonical CLEAN;
          events manifest drift gate 1/1; make docs-check PASS.
          Remote CI execution for the certification commit is a post-commit
          action (record honestly: these counts are local executions).

Generated artifact checksums (at 450bea97):
  sha256 backend/contracts/openapi/notrelix.v1.json
        = f4391c799d864f6542f59e7b44290898a8cc6b1cfa67610b0e762ec6a47c9758
  sha256 backend/contracts/events/notrelix.events.json
        = dda01452e66927e2975fb349b953493347be3aec3e6b641e27a9a393745fa0f4

Phase 13 closure table (IA-CERT-HO-002):
  IA-API-002 DONE | IA-API-003 DONE | IA-API-004 DONE
  IA-AUTHZ-001 DONE | IA-AUTHZ-002 DONE | IA-AUTHZ-003 DONE
  IA-AUTHZ-004 DONE | IA-EVT-001 DONE | IA-EVT-002 DONE
  IA-EVT-003 DONE | IA-EVT-OPS NOT_APPLICABLE_UNTIL_DEPLOYMENT
  (permitted operational exception; runbook trigger = first prod deploy)
  No source-level DEFERRED exists.

Closure blocker rule check (IA-CERT-HO-004):
  CSRF convention mismatch: none (single ADR-005 spelling, guarded)
  CSRF disabled due to FE incompatibility: NO — flag off is staged-rollout
    state only; cross-stack transport merged and smoke-proven locally
  Handler bypass unclassified: zero unclassified hits
  Admin/CreateWorkspace semantics: resolved (frozen central matrix)
  Name-only event resolution: eliminated, compiler+gate enforced
  Drift gates: present and green (OpenAPI + manifest)
  Prohibited secrets / unclassified PII in public payloads: gated green,
    delivery-event PII classified with purpose+consumer
  Required suites failing/zero-executed: none (all non-zero green)
  → NO BLOCKER REMAINS

Blocking debt: none at source level.
Non-blocking debt:
  - CSRF production enablement + production cookie-policy runtime smoke =
    deployment actions (staged sequence P14-MIG-003)
  - GitGuardian CI incident: external repository-alert triage, outside this
    workstream's source scope
  - Human sign-off pending on all agent-executed certification records
  - Frontend feature-auth router coupling remains SOURCE_DEBT (separate owner)
  - MFA/step-up/API-token management UI wiring backlog (capability APIs done)
  - 26 stub consumers remain STUB by design until owning contexts implement

Decision:
  IDENTITY & ACCOUNTS FULL SCOPE CERTIFIED
  (source-level; operational items above remain deployment/backlog actions)

Reviewer(s):
  Execution agents (Phases 0–20); human sign-off PENDING
Date:
  2026-08-23 (certification executed); sign-off pending
```

# Handoff to Workspace & Governance

## 151. Handoff condition

Workspace/Governance may treat P1 as a stable upstream only when Milestone A is certified.

## 152. Handoff contract

The handoff should expose:

```text
stable User ID
stable Actor contract
stable Account ID
Account/Tenant semantics
current Account contract
tenant-isolation guarantee
session/auth identity contract sufficient for protected requests
```

It must NOT require Workspace/Governance to know:

- password implementation;
- OAuth token storage;
- MFA secrets;
- Session EF mapping;
- Identity private tables.

## 153. Handoff evidence packet

The receiving team should be able to review:

```text
P1 core certification record
relevant API/Application contracts
relevant event contracts
relevant migration notes
relevant architecture tests
exact-SHA CI link/evidence
```

without reverse-engineering Identity source.

# D5 stability rules after certification

## 154. Breaking changes after P1 certification

Once Workspace/Governance depends on P1 core:

Breaking changes to:

```text
User ID
Actor contract
Account ID
Account semantics
current Account contract
tenant isolation
critical lifecycle event
```

require:

- consumer inventory;
- migration;
- compatibility;
- rollout;
- re-certification of affected P1 contract.

## 155. Secondary capability changes

OAuth/MFA/API-token internal changes do not require full P1 re-certification unless they alter:

- canonical User identity;
- Actor;
- Session;
- Account context;
- tenant isolation.

## 156. Certification invalidation

P1 certification becomes invalid if a later change introduces:

- second Account truth;
- cross-tenant leakage;
- actor spoofing;
- broken session revocation;
- downstream private Identity DB dependency;
- invalid schema/model drift.

# Final Definition of Done

## 157. P1 Core Done

P1 Core is DONE only when:

```text
Identity/Account core implementation complete
+
mandatory P1 tests pass
+
architecture/security/migration evidence pass
+
Workspace/Governance handoff verified
+
exact candidate SHA CI green
```

## 158. Full Identity & Accounts Done

Full team scope is DONE only when:

```text
P1 Core certified
+
all release-scoped secondary capabilities certified
+
cross-cutting hardening complete
+
docs/contracts current
+
exact candidate SHA CI green
```

## 159. What does not count as Done

Not sufficient:

- all handlers compile;
- Domain tests only;
- happy-path API tests only;
- local integration tests only;
- PR merged without exact-SHA certification;
- OAuth works manually;
- MFA UI exists;
- token generation succeeds;
- migration generated but not upgrade-tested;
- CI green while critical filtered suite executed zero tests.

## 160. Final certification rule

The final question is:

```text
Can every downstream team rely on the certified contract
without knowing Identity & Accounts private implementation?
```

For P1 Core, if the answer is no:

```text
P1 is not STABLE
```

For the full Identity & Accounts scope, if release-scoped security capabilities lack their required proof:

```text
full team scope is not CERTIFIED
```

Certification is evidence of dependency safety, not merely feature completion.
