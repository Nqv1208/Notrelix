---
document_id: ADR-011
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-security
  - backend-tenancy
  - backend-rls
  - backend-infrastructure
  - backend-application
  - identity-context
  - workspaces-context
  - governance-context
evidence:
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsPolicyApplier.cs
  - backend/src/Notrelix.Infrastructure/Data/Rls/RuntimeDatabaseRoleGuard.cs
  - backend/src/Notrelix.Infrastructure/Data/OwnerDbContextProvider.cs
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/002_context_helpers.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/012_principal_scopes.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/013_co_member_profiles.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/014_anonymous_scopes.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/015_internal_principals.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/016_policies_derived_scope.sql
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/017_webhook_scope.sql
  - backend/src/Notrelix.Infrastructure/Data/DatabasePrincipal.cs
  - backend/tests/Notrelix.Integration.Tests/Rls/AppRoleConformanceTests.cs
  - backend/tests/Notrelix.Integration.Tests/Rls/RuntimeDatabaseRoleGuardIntegrationTests.cs
  - backend/tests/Notrelix.Integration.Tests/Rls/PolicyCoverageTests.cs
  - backend/tests/Notrelix.Integration.Tests/Rls/DerivedScopePolicyTests.cs
  - backend/tests/Notrelix.Architecture.Tests/InfrastructureLayer/IntegrationFleetRoleBoundaryTests.cs
review_on:
  - decision-superseded
  - rls-principal-model-change
  - database-role-change
  - rls-scope-change
  - anonymous-flow-added
  - internal-table-added
---

# ADR-011: Database Principal Model — Owner, Application Role, Worker Role and Bounded Scopes

## ID

`ADR-011`

## Status

Accepted

## Date

`2026-10-24`

## Owners

- `backend-architecture`
- `backend-security-architecture`

Semantic co-owners for the bounded writes named below: Identity (users, sessions, tokens), Workspaces
(invitations, members), Accounts (provisioning) and Governance (`authz.access_grants`).

## Context

PostgreSQL Row-Level Security is the persistence defense-in-depth layer of NRX-004: Application
authorization decides, RLS limits the blast radius of an Application bug. ADR-002 defines how the full
tenant context is applied on one physical connection. It does not say which database identity the process
connects as, and no environment answered that question in the intended way:

- Every non-test environment connects with a superuser or table-owner login, so the policy pack (applied
  with `FORCE ROW LEVEL SECURITY`) never constrains the serving path. The policies have been exercised only
  by a handful of tests that open a private non-superuser login.
- The one explicit bypass, `app.request_scope = 'worker'`, is a value the application connection writes
  itself and every access helper (`ops.has_workspace_access` and the others) returns `true` for it. Under a
  non-owner login that is an unbounded all-tenant bypass held by the serving process.
- Whole families of requests have no principal the policies understand: anonymous token and credential
  flows (registration, invitation, verification, reset, login, refresh), Global authenticated requests
  (no account to bind), resource location before the tenant is known, API-token authentication, and the
  messaging runtime that reads worker-only internal tables.
- Data migrations (`ItemKeyMigrationSql` and similar) require the migrating role to bypass RLS, so the
  owner cannot be a plain role; the serving role therefore cannot be the owner.

A conformance suite that runs representative flows under a non-owner `notrelix_app` login
(`AppRoleConformanceTests`) records which of these flows break today. Choosing the identities and the
bounded entry mechanism is a security and ownership decision that normal rules do not determine, and the
choice constrains every future anonymous endpoint and internal table.

## Decision

1. **Three database identities, never mixed on one connection.**
   - **Owner**: migrations, `--rls-apply` and seed. A superuser or `BYPASSRLS` role (the data-migration
     guards require it). It is reached only through `ConnectionStrings:NotrelixMigrationDb` and
     `OwnerDbContextProvider`; the serving path never receives it. Development, CI and real-e2e use
     `postgres` for this purpose only; staging and production use a dedicated owner role.
   - **`notrelix_app`**: the serving role for requests. Not the owner, not superuser, no `BYPASSRLS`.
   - **`notrelix_worker`**: the role of the outbox and email dispatchers, the deduplication store,
     message consumers (including the first-workspace provisioning consumer), sweeps, reapers and job
     locks, on a second connection (`ConnectionStrings:NotrelixWorkerDb`, falling back to
     `ConnectionStrings:NotrelixDb` while unset so single-login development and test hosts keep working).
     A dependency-injection scope chooses its principal before its first database use
     (`DatabasePrincipal`): background loops create their scopes with `CreateWorkerAsyncScope()`, and the
     first consume filter of every message pipeline (`WorkerPrincipalConsumeFilter`) switches the consume
     scope; the context's connection is then fixed. Internal tables (`messaging.*`,
     `notifications.email_outbox`, `ops.*` job tables and similar) keep worker-only policies; no
     application-role policy gated by a session value is added for them.
   `notrelix_app` and `notrelix_worker` are `LOGIN` roles used directly. There is no `SET ROLE` switching
   inside a pooled connection. The owner creates them idempotently during policy application and the
   database init script creates them on fresh volumes; their passwords come from the same secret mechanism
   as the existing connection strings.

2. **The runtime verifies its own identity.** `RuntimeDatabaseRoleGuard` (`Rls:RoleEnforcement` =
   `Off`, `Warn`, `Enforce`) rejects a serving connection whose effective role is a superuser, has
   `BYPASSRLS`, or holds the privileges of the owner of the tenant tables. It deliberately also treats a
   login that is a superuser or `BYPASSRLS` and merely runs `SET ROLE notrelix_app` as a violation: such a
   session can `RESET ROLE` and return to the privileged identity, so the constraint is not a property of
   the connection. The guard defaults to `Enforce` in every environment once environments are switched.

3. **Principal kinds are `app.request_scope` values, and only `app` and `worker` are trusted for
   unbounded access to a tenant.**

   | Scope | Meaning | Bound by |
   |---|---|---|
   | `app` | authenticated user inside an account/workspace (ADR-002) | user id from the verified principal; account/workspace from the tenant bootstrap |
   | `user` | authenticated user with no tenant yet (Global requests, resource location, bootstrap lookups) | user id from the verified principal |
   | `token` | one account/workspace named by a presented token: an invitation, or an API token presented as a bearer credential (the purpose is recorded with the scope and policies check it) | derived by the database from the token digest |
   | `credential` | one user named by a presented credential digest or normalized email | derived by the database |
   | `registration` | the rows a registration creates | the ids of those rows |
   | `worker` | background processing | the connection's login: honoured only for a member of `notrelix_worker` |

   `ops.is_worker_scope()` is the scope value `worker` **and** a session login that is a member of
   `notrelix_worker` (`session_user`, because inside the definer function `current_user` is the owner). The
   scope value is a session setting any connection can write, so on its own it proves nothing: an
   application login that sets it gains nothing. `system`, `migration` and `migrator` were never set by any
   code and are no longer admitted. `user`, `token`, `credential` and `registration` are never members of
   it. Policies that admit these scopes do so through dedicated helpers (`ops.has_user_scope`,
   `ops.has_token_scope`) that compare the row to the bound values and are never satisfied in `app` scope.

4. **Bounded scope entry through owner-defined functions.** Token and credential scopes are entered with
   `SECURITY DEFINER` functions in the `ops` schema (`ops.enter_token_scope` first, credential and
   registration entries as those flows migrate). They are owned by the owner role, pin `search_path`, use
   no dynamic SQL, grant `EXECUTE` only to `notrelix_app` and are revoked from `PUBLIC`. The caller presents
   the secret digest, not identifiers: the function looks the row up (the owner bypasses RLS), derives the
   account, workspace or user itself, writes the bound session values with transaction-local `set_config`,
   and returns only the derived identifiers. Entry requires an open transaction.

5. **Writes the app role may not express as a row policy go through bounded owner-defined functions.**
   `authz.access_grants`, `account.accounts` and first-workspace rows are created under the application
   role by functions that validate their inputs and check the bound scope, not by broad INSERT policies
   (a grant-based INSERT check cannot bootstrap the first grant). `ops.upsert_access_grant` and
   `ops.revoke_access_grant` write and end grants inside a registration or invitee scope and, in the
   tenant (`app`) scope, only for the request's own account and workspace and the account-level Member
   grant that accompanies a workspace membership (an Owner grant only by the current Owner, the ownership
   transfer, or for the caller creating a new workspace of their account; an administrator's account grant
   is never overwritten). Application authorization remains the authority on whether the caller may change
   the membership; the function is a bound, not a second authorization engine. A person leaving ends their
   own grant through the one narrow own-grant update policy. The background login (first-workspace
   provisioning, other consumers) writes through its own worker policy.

   Security audit rows (`audit.security_events`, often tenantless) are written through the insert-only
   `ops.record_security_event` function: the event type must be a known one, the actor is the user of the
   current scope (or none) and the tenant the workspace of the current tenant scope (or none), neither
   supplied by the caller, and no read, update or delete is granted. A failed write is logged as an error
   and counted (`security_audit_event_write`), never silent and never a failed request.

   Other people's email and phone are read through `ops.disclosable_member_emails` and
   `ops.shared_member_phones`, which apply the Application rule underneath it (email: reader is an active
   Owner or Admin of the Workspace, subjects are active non-guest members; phone: reader and subject are
   active non-guest members, the subject opted in). Neither widens a row policy.

6. **Co-member display name and avatar** are exposed by an owner-defined function that returns only
   `id`, display name and avatar fields for users who share at least one workspace membership with the
   caller in any state (active, suspended or removed, so historical attribution keeps resolving). Guests
   resolve names and avatars the same way; which members a guest may list stays an Application
   authorization rule. Email, phone and credentials are never returned (the Owner/Admin email disclosure
   rule is unchanged). A row policy on `identity.users` is not used: it would expose `password_hash` and
   email.

7. **Every scope entry is audited operationally.** Entering a `token`, `credential`, `registration` or
   system scope writes a structured log line and increments a metric carrying the scope kind, the reason
   and the derived identifiers, never a token value. Flows that already record security activity keep
   their durable `audit.security_events` row; no new durable audit table is created for anonymous flows.

8. **Scope.** Registration, invitation preview/decline/accept, email verification, password reset, login,
   refresh, MFA challenge, OAuth login and step-up, identify, forgot-password, resend verification and
   resolve-slug are all anonymous credential flows and all move to bounded scopes. Verification and reset
   are bounded to the user derived from the token digest, an invitation to the token's workspace,
   registration to the rows it creates.

The application role is the only identity the integration test fleet uses for the serving path; the owner
connection seeds and truncates.

## Alternatives Considered

### Alternative A — Keep a superuser or owner serving connection

**Description**

Leave the status quo and treat the policy pack as documentation of intent.

**Benefits**

- No flow changes; no new roles or connection strings.

**Costs / risks**

- RLS is dead code in every environment; a missed Application authorization check has no second line.
- The policy pack rots because nothing exercises it.

**Why not chosen**

NRX-004 positions RLS as a real second layer; an unenforced layer is a false assurance.

### Alternative B — Broad `worker` scope for anonymous flows

**Description**

Run anonymous flows in the existing `worker` scope under `notrelix_app`.

**Benefits**

- Smallest change; every table already admits `worker`.

**Costs / risks**

- The scope is asserted by the application, so any injection or bug in an anonymous endpoint reaches every
  tenant. It removes the blast-radius reduction the layer exists to provide.

**Why not chosen**

Rejected by the owner of the decision: anonymous exposure must be bounded to one account, workspace or user.

### Alternative C — A role per flow with `SET ROLE` switching

**Description**

Use `notrelix_auth`, `notrelix_worker` and `notrelix_app` as in-process switches on a pooled connection.

**Benefits**

- Uses roles that already exist; per-role grants.

**Costs / risks**

- Role state on pooled connections must be reset exactly; a leaked `SET ROLE` is a silent privilege change.
- The login behind the switch must be able to assume the privileged role, so a compromise of the process can
  reach it.

**Why not chosen**

Distinct LOGIN roles on distinct connections keep the privilege of a connection immutable for its lifetime.

### Alternative D — Row policy or column grants on `identity.users` for co-members

**Description**

Let co-members read each other's `users` rows through a policy, or grant column-level SELECT.

**Benefits**

- No definer function.

**Costs / risks**

- Policies are per row and expose all columns the role may read; column grants cannot vary by the
  relationship between two rows. Credentials and email would be reachable.

**Why not chosen**

A definer function returning exactly the permitted fields is the only form that cannot over-disclose.

### Alternative E — Trust application-supplied account and workspace ids for token scopes

**Description**

Let the Application compute the account and workspace from the token and set them as session values.

**Benefits**

- No definer entry functions.

**Costs / risks**

- The bound is only as honest as the Application code that sets it, which is the code the layer protects
  against.

**Why not chosen**

The database derives the bound from the presented digest; the application never supplies the ids.

## Consequences

### Positive

- RLS constrains the serving path in every environment and in the test fleet; regressions fail at startup
  or in the conformance suite.
- Anonymous blast radius is one account, workspace or user.
- Worker-only internal tables stay unreachable from the request path, and the unbounded worker scope can no
  longer be claimed by the serving process.

### Negative / trade-offs

- A small allow-list of token, credential and registration policies and entry functions that must be kept in
  sync with the flows (guard tests enforce the shape).
- One extra statement per bounded request and an extra short transaction for resource location.
- Two connection strings and two pools to operate, plus provisioning of two login secrets. A message and
  everything its consumer does (the deduplication claim, the effect, the commands it dispatches) run on the
  worker connection, whose policies admit all tenants: tenant policies do not bound consumer effects, the
  consumer's declared tenant scope and the Application do.
- Session values remain settable by the application login: a compromised application process holding the
  login can still assert scopes. RLS defends against Application bugs and injection, not against a hostile
  process holding the serving credentials.
- Definer functions are a privilege-escalation surface and need review on every change.

### New obligations

- tests/gates: conformance tests under `notrelix_app`; negative cross-tenant proofs per bounded scope;
  architecture tests that new scope values are not in `ops.is_worker_scope()` and that entry functions are
  definer with a pinned `search_path` and restricted `EXECUTE`; a pooled-connection reuse proof per entry.
- operations: owner role per environment; two LOGIN secrets; `Rls:RoleEnforcement=Enforce`.
- security: new anonymous endpoint, new internal table or new role needs this ADR's mechanism, not a
  broader policy.
- performance: access helpers use dynamic SQL per row; list endpoints must be measured under enforcement.
- documentation: `security-tenancy-authorization.md`, `infrastructure-and-data.md`,
  `configuration-and-runtime.md`.
- ownership: Governance, Accounts and Identity own the content of the bounded writes.

## Compatibility / Migration

No schema table changes: policies, functions and roles only, applied by the idempotent policy pack. Nothing
is deployed beyond development, so there is no production role or connection to preserve. Ordering inside an
environment is still: apply the new policies and functions as owner, deploy application code that uses them
while the connection is still privileged (behavior unchanged because a privileged role bypasses RLS), prove
the flows under the application role, then switch the serving connection and set the guard to `Enforce`.
Rollback is pointing the serving string back at a privileged login with the guard at `Warn`; a missing
policy fails closed (not found, empty, or a denied write), so roll forward by adding the policy.
ADR-002 is not superseded: its minimal-context bootstrap on one physical connection still holds and now also
applies to Global and Resource-scoped requests.

## Evidence

### Canonical current architecture

- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/infrastructure-and-data.md`

### Source / manifests

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RuntimeDatabaseRoleGuard.cs`
- `backend/src/Notrelix.Infrastructure/Data/OwnerDbContextProvider.cs`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/`

### Tests / gates

- `backend/tests/Notrelix.Integration.Tests/Rls/AppRoleConformanceTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/RuntimeDatabaseRoleGuardIntegrationTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Authorization/RlsPolicyArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Authorization/RlsPrincipalScopeArchitectureTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/PrincipalScopePrimitivesTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/UserScopeIsolationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/CoMemberProfileTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/InvitationScopeTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/CredentialScopeTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/WorkerPrincipalTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/ApiTokenScopeTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/SecurityEventWriteTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/AdminGrantWriteTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/MemberDisclosureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Authorization/InternalPrincipalArchitectureTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/PolicyCoverageTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Rls/DerivedScopePolicyTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/InfrastructureLayer/IntegrationFleetRoleBoundaryTests.cs`

### Contracts / migrations / generated evidence

- None: no public contract and no schema table change.

## Supersedes

`None`

## Superseded By

`None`

## Notes

Extends ADR-002 (connection lifecycle) with the identity the connection runs as and the bounded scopes
that replace the application-asserted `worker` bypass for request-path flows. Decided by the product owner's
delegate on 2026-10-24: explicit bounded scopes for anonymous flows, a user scope for Global requests,
co-member visibility of display name and avatar, application role in every environment.

## Amendment 2026-10-25

Recorded while the integration fleet moved onto the real roles. The decision is unchanged; these are
refinements that the fleet made necessary or made visible.

- **Coverage is a catalog rule.** Every base table of every application schema (all but
  `ops.__EFMigrationsHistory`) must have forced row-level security and a policy, or the policy apply fails; the
  generic helpers raise instead of skipping a table they cannot scope. Eighteen tables that the helpers had
  skipped silently (open to the application login through the schema grants) now inherit the scope of their
  parent or owner (`016_policies_derived_scope.sql`), and `apply_worker_internal_policies` no longer falls back
  to an unconditional application INSERT for a tenantless table.
- **Tenantless person events** (`events.domain_event_logs`, `messaging.outbox_messages`) are admitted for the
  authenticated person in the user scope or the `app` scope, INSERT-only, for the actor's own actions.
- **Verified calendar webhook scope.** The inbound calendar webhook is an anonymous token-class flow (it was
  named in the plan's Q2 class list as already having a narrow lookup scope); after signature verification it
  enters a `calendar-webhook` token-kind scope derived by the database from the binding's webhook path, which
  admits only the receipt claim and the processing outbox message for that account and workspace.
- **Both logins are checked.** `RuntimeDatabaseRoleGuard` also verifies the background login, refuses an
  application login that is a member of `notrelix_worker` and a missing `ConnectionStrings:NotrelixWorkerDb`
  (the single-login fallback of decision 1 now exists only with `Rls:RoleEnforcement=Off`). `Enforce` is the code and
  shipped-settings default (fail closed); the Compose stacks and container smoke opt down to `Warn` explicitly
  until they are switched to the two logins.
- **Member email, member phone and the security-event sink always use their owner-defined functions on
  PostgreSQL**; the session-context gating is gone, so a host cannot silently take the direct-read path.
- **Residual, recorded and unchanged:** the `app.scope_*` and `app.current_*` values are session settings the
  application login can write. The bounded scopes make an honest application bounded and an injection or a
  handler bug unable to widen a scope through ids it is trusted with, but a process that holds the application
  credentials can assert a scope; only the worker scope is additionally tied to the login. This is the
  trade-off listed under Consequences, not a gap to close in this decision.
- **Test fleet.** Hosts and pipelines under test run on the application login and background work on the
  worker login; the owner migrates, applies the pack, seeds, resets and verifies
  (`IntegrationFleetRoleBoundaryTests` keeps this from eroding).
