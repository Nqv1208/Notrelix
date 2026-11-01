---
document_id: BE-INFRASTRUCTURE-DATA
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Infrastructure
  - backend/tests/Notrelix.Infrastructure.Tests
  - backend/tests/Notrelix.Integration.Tests
evidence:
  - backend/src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj
  - backend/src/Notrelix.Infrastructure/Data/
  - backend/src/Notrelix.Infrastructure/Caching/
  - backend/src/Notrelix.Infrastructure/Auth/
  - backend/src/Notrelix.Infrastructure/Integrations/
  - backend/src/Notrelix.Infrastructure/Storage/
  - backend/docs/decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
  - backend/docs/decisions/ADR-011-database-principal-model.md
  - backend/tests/Notrelix.Infrastructure.Tests/
  - backend/tests/Notrelix.Integration.Tests/
review_on:
  - persistence-model-change
  - dbcontext-boundary-change
  - rls-model-change
  - migration-strategy-change
  - cache-architecture-change
  - provider-adapter-change
  - storage-search-change
  - database-provider-change
---

# Infrastructure and Data

> **Infrastructure implements persistence, database tenancy defense, cache, provider, storage, search, identity, and other technical adapters for inward Application/Domain contracts. It does not own business lifecycle or authorization semantics merely because those rules must eventually be persisted or executed through a provider.**
>
> PostgreSQL is the authoritative relational persistence class. RLS is defense-in-depth. Cache/search/projections are derived. Provider DTOs remain at the adapter boundary. Migration evidence comes from the EF model + migration chain + real existing-data proof—not a handwritten schema snapshot.

This document is the canonical backend owner for Infrastructure/data mechanics.

Application transaction/use-case semantics remain owned by `application-model.md`.

Platform delivery semantics remain owned by `platform-and-messaging.md`.

Repository migration/recovery policy remains under repository Delivery/Operations.

---

# 1. Infrastructure purpose

Infrastructure translates inward contracts into runtime/provider mechanics.

Current responsibility families include:

```text
EF Core / DbContext
PostgreSQL / Npgsql
migrations
RLS
query/read ports
Redis/cache
Identity/JWT/auth provider mechanics
email
integration providers
storage
search/read models/projections
logging/observability adapters
MassTransit/RabbitMQ adapter integration
runtime options/config binding
```

These technologies are replaceable mechanisms.

Their presence does not transfer product ownership.

---

# 2. BE-INF-001 — Infrastructure depends inward

Current project references:

```text
Infrastructure
→ Application
→ Domain
```

Infrastructure MAY know inward contracts/types to implement them.

Application/Domain MUST NOT depend on Infrastructure concrete types as ordinary architecture.

---

# 3. Current package evidence

Current `Notrelix.Infrastructure.csproj` includes packages such as:

```text
Microsoft.EntityFrameworkCore
Npgsql.EntityFrameworkCore.PostgreSQL
EFCore.NamingConventions
Microsoft.AspNetCore.Identity.EntityFrameworkCore
JWT/Bearer packages
Redis/StackExchange.Redis
Resend
Serilog
MassTransit
MassTransit.RabbitMQ
```

This is current executable evidence.

Package presence does not itself define semantic ownership.

---

# 4. Infrastructure folder evidence

Current source includes areas such as:

```text
Auth
Billing
Caching
Configuration
Data
Email
Events
Identity
Integrations/Providers
Messaging
Notifications
Observability
Operations
RateLimiting
ReadModels
Realtime
Security
Serialization
Services
Storage
```

Folders reflect implementation concerns.

Do not infer a new bounded context solely from an Infrastructure folder.

---

# 5. Persistence model

The authoritative relational path is:

```text
Domain/Application semantic model
        ↓
EF mapping/converter
        ↓
PostgreSQL schema
        ↓
migration history
```

Infrastructure adapts semantics to storage.

Do not reverse ownership:

```text
column/table convenience
→ product rule
```

---

# 6. BE-INF-002 — DbContext belongs to Infrastructure

Current:

```text
ApplicationDbContext
```

and related:

```text
DbSets
factory
initialiser
session/data scope
configurations
converters
interceptors
migrations
```

live under Infrastructure.

New persistence implementation remains here.

---

# 7. DbContext purpose

DbContext owns technical persistence composition:

```text
entity mappings
change tracking
transactions
query filters where used
converters
interceptors
database provider
migration model
```

It does not own:

```text
resource permission
business transition
feature entitlement
cross-context lifecycle
```

---

# 8. BE-INF-003 — DbContext is not a cross-context integration API

Do not let another context's Application handler use:

```text
DbContext.ForeignContextEntities
```

to mutate foreign-owned state as normal integration.

Use the target owner's Application contract/event.

---

# 9. Mapping

EF configurations map approved Domain/Application concepts to relational representation.

Review:

```text
table/schema
column type
nullability
length
conversion
owned types
keys
foreign keys
indexes
concurrency
delete behavior
```

Mapping must preserve semantic meaning.

---

# 10. BE-INF-004 — Persistence mapping does not bypass Domain API

Do not use:

```text
reflection/private-field mutation
materialization workaround
setter exposure
```

to perform ordinary business mutation that should go through Domain behavior.

Persistence rehydration support is allowed.

Business change still uses the owned API.

---

# 11. Schema organization

Schema/table naming can group technical ownership.

Do not treat a PostgreSQL schema as the only definition of bounded context.

A context boundary includes:

```text
semantics
write authority
contracts
events
```

not only table prefix.

---

# 12. BE-INF-005 — One table has one logical semantic owner

Shared database does not mean shared write authority.

Document/model foreign references carefully.

Cross-context read projections may join/copy data for queries, but writes return to the source owner.

---

# 13. Database constraints

Use database constraints to reinforce race-safe invariants such as:

```text
uniqueness
FK integrity
not-null
check conditions
```

where appropriate.

The business meaning remains Domain/Application-owned.

---

# 14. BE-INF-006 — Constraint failure maps to stable semantic outcome

Do not leak raw:

```text
constraint name
SQLSTATE
Npgsql exception
```

to public consumers.

Translate known conflicts through Infrastructure/Application result semantics.

---

# 15. Indexes

Indexes serve:

```text
authorized query patterns
tenant predicates
sort/filter
FK/uniqueness
migration/reconciliation
```

They do not redefine product semantics.

Index design should consider real cardinality and tenant distribution.

---

# 16. BE-INF-007 — Index follows query/invariant evidence

Do not add indexes mechanically to every column.

Do not omit tenant-leading/selective index design where RLS/query patterns require it.

Use query-plan/load evidence for performance-sensitive paths.

---

# 17. PostgreSQL authority

PostgreSQL is the production relational persistence class.

Tests for provider-specific semantics should use PostgreSQL-realistic evidence.

SQLite/InMemory cannot certify:

```text
RLS
Npgsql conversion
PostgreSQL locks
migration DDL
provider-specific indexes
```

---

# 18. BE-INF-008 — Provider substitution is architecture work

Changing PostgreSQL/provider is not a simple configuration swap if it changes:

```text
transactionality
RLS
locking
JSON/type mapping
index behavior
migration semantics
```

Review protected properties through architecture decision/change process.

---

# 19. EF model evidence

The current intended persistence model is represented by:

```text
Domain/Application source
+
EF configuration/model
+
migration chain/current schema
```

Do not maintain a handwritten schema snapshot as equal authority.

Generated schema output can be evidence when derived from the producer.

---

# 20. BE-INF-009 — Pending model changes are a failure to resolve, not a warning to suppress

If EF reports model drift:

```text
determine intended model
create/review migration
or correct unintended mapping change
```

Do not suppress `PendingModelChangesWarning` merely to let startup/migration proceed.

---

# 21. Migrations

Production schema change uses reviewed migrations.

Migration files are append-oriented historical persistence evidence.

Do not rewrite already-applied migration history casually.

The only sanctioned deviation is the governed dev-stage re-baseline policy owned by `docs/operations/migrations-and-data-change.md` §8 (BE-OPS-DATA-004): while the project has no production database, the chain may be consolidated into the single `SchemaBaseline` development migration, with development databases reset rather than upgraded.

---

# 22. BE-INF-010 — Migration changes real old state to the target meaning

For a persisted semantic change consider:

```text
existing rows
legacy invalid values
old writers
old readers
backlog/messages
RLS
indexes/constraints
rollback/forward recovery
```

Empty-database success is insufficient when old data exists.

---

# 23. Expand-contract model

When mixed versions can coexist:

```text
expand
→ compatible writer/reader
→ backfill
→ cutover
→ verify
→ contract
```

Do not combine incompatible expansion and destructive contraction in one first deployment unless true atomicity is proven.

---

# 24. BE-INF-011 — Destructive contraction waits for objective removal proof

Before dropping:

```text
column
table
old representation
index supporting old path
```

prove:

```text
old readers stopped
old writers stopped
backfill complete
rollback policy known
backlog/old clients handled
```

---

# 25. Migration initialisation

Startup/local initialization may apply migrations according to environment/runtime policy.

Production migration timing/credential is controlled by Delivery/Infrastructure runtime.

Do not make every application instance race to perform privileged DDL without an approved strategy.

---

# 26. BE-INF-012 — Migration privilege is separable from steady-state runtime

Where infrastructure supports it:

```text
migration/admin identity
≠
normal API/worker DB identity
```

Normal runtime should not require broad DDL/admin privilege solely because migrations exist.

---

# 27. Seed

Seed/bootstrap data exists for development/testing or explicit production bootstrap.

Do not make development seed a source of product truth.

Do not reset/populate production silently.

---

# 28. BE-INF-013 — Seed is environment-scoped and idempotent where repeatable

A repeatable seed should avoid duplicate/corrupt state.

Production seed must be explicit and safe.

---

# 29. RLS purpose

PostgreSQL Row-Level Security is defense-in-depth for tenant isolation.

It complements:

```text
Application authorization
```

It does not replace it.

---

# 30. BE-INF-014 — RLS and Application authorization are both required where the architecture declares them

Correct:

```text
Application denies unauthorized use case
+
RLS prevents unintended cross-tenant DB access
```

Do not intentionally weaken either because the other exists.

---

# 31. RLS context

Current architecture uses PostgreSQL session/transaction configuration for identity/scope facts such as:

```text
current user
Account
Workspace
request scope
correlation
```

exact variables/functions are current implementation evidence.

RLS context must be applied before tenant-protected SQL requiring it.

---

# 32. BE-INF-015 — RLS session context is connection-scoped state that must not leak

Connection pooling means Infrastructure must ensure:

```text
correct set
correct transaction-local/session semantics
cleanup/reset on return
no previous-request tenant leakage
```

Tests must cover reuse/lifecycle.

---

# 33. Tenant bootstrap exception path

ADR-002 defines a current bootstrap path before full Account/Workspace context is known.

Current design:

```text
authenticated user known
→ open same physical Npgsql connection
→ set minimal RLS session context
→ bootstrap query resolves scope
→ later DbRequestScope applies full transaction-local RLS context
```

This is an accepted specialized lifecycle.

---

# 34. BE-INF-016 — Bootstrap RLS context is minimal, not a global bypass

The bootstrap path sets only the facts required to safely resolve the remaining scope.

Do not use:

```text
IgnoreQueryFilters
```

as proof PostgreSQL RLS is bypassed.

EF query filters and PostgreSQL RLS are separate controls.

---

# 34A. Owner and serving database connections

Three identities exist on purpose:

```text
owner     migrations, --rls-apply, seed        ConnectionStrings:NotrelixMigrationDb
serving   requests (notrelix_app)              ConnectionStrings:NotrelixDb
worker    dispatchers, deduplication, consumers, sweeps, reapers, job locks (notrelix_worker)
                                               ConnectionStrings:NotrelixWorkerDb
```

`OwnerDbContextProvider` supplies the owner `ApplicationDbContext` to the initialiser and the policy
applier only (an architecture test restricts who may name it); the serving path never receives it.
`NotrelixMigrationDb` is required while `Rls:RoleEnforcement` is `Enforce` (the default of every environment,
the Compose stacks and the container smoke included); only a host that opted out with `Warn` or `Off` (test hosts
on one privileged login) falls back to the serving string. `RuntimeDatabaseRoleGuard` inspects BOTH serving connections at startup (`Rls:RoleEnforcement`:
`Off`, `Warn`, `Enforce`; `Enforce` is the default) and rejects a superuser, BYPASSRLS,
an owner-privileged login, or a login that could leave a `SET ROLE` constraint. The application login must
not be a member of `notrelix_worker`; the background login must be one; and a missing
`ConnectionStrings:NotrelixWorkerDb` (background work would run as the application login) is refused too.
`PostgresTestContainer` migrates and applies the pack as the owner and hands out the application login
(`ConnectionString`, what every host and pipeline under test uses), the worker login
(`WorkerConnectionString`) and the owner (`OwnerConnectionString`: seeding, reset, verification only,
`CreateOwnerContext`). An architecture test (`IntegrationFleetRoleBoundaryTests`) keeps a host from being
given the owner connection and names the few tests that build a container of their own (migrations and
backfills, the policy pack on a database of the test's own making).

**The worker connection.** A dependency-injection scope's `ApplicationDbContext` connects as the principal
the scope chose before its first use (`DatabasePrincipal`, `Data/DatabasePrincipal.cs`). Request scopes are
the application principal. Background loops (`OutboxDispatcher`, `EmailDispatcher`,
`OnboardingDraftSweepService`, `SecurityActivityRetentionService`, the two object reaper services) create
their scopes with `IServiceScopeFactory.CreateWorkerAsyncScope()`; every message-consume scope is switched by
`WorkerPrincipalConsumeFilter`, the first filter of each transport's consume pipeline, so the deduplication
claim, the consumer's own writes and the commands it dispatches (the first-workspace provisioning command
included) share one worker-login context and keep their single-transaction atomicity. Switching after the
connection was resolved throws. `ConnectionStrings:NotrelixWorkerDb` is required while `Rls:RoleEnforcement` is
`Enforce` (configuration binding refuses the host); only a host that opted out with `Warn` or `Off` falls back to
`NotrelixDb`. The worker login is the only connection for which
`ops.is_worker_scope()` is true (see 34B); `ops.job_locks` and every `messaging.*` table have `p_worker_all`
for it and no policy for the application role. Architecture tests
(`InternalPrincipalArchitectureTests`) pin that every `BackgroundService` creates worker scopes, that the
worker filter precedes every other consume filter, and where a system context may be entered.

**Provisioning the two logins.** `notrelix_app` and `notrelix_worker` are LOGIN roles used directly (no
`SET ROLE`). `001_roles.sql` creates them NOLOGIN so the grants have a target; the owner then makes them logins
with `RlsLoginRoleProvisioner` at the end of `--rls-apply` (and of the Development startup policy application):
`ALTER ROLE ... LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD ...`, re-asserted on
every run, and any worker membership of the application login is revoked. The password is the one inside the
login's own connection string (`NotrelixDb` must name `notrelix_app`, `NotrelixWorkerDb` must name
`notrelix_worker`, both with a password), so the secret that opens the connection is the single source of the role's
password and rotating it is a secret change plus an `--rls-apply`. The server quotes the password
(`format('%L')`); it is never logged. On a fresh compose-managed data directory the init script
`infra/postgres/init/20-create-runtime-logins.sh` creates the same roles from `NOTRELIX_APP_DB_PASSWORD` and
`NOTRELIX_WORKER_DB_PASSWORD` (init scripts run only on an empty volume; existing volumes and external databases
rely on `--rls-apply`).

---

# 34B. Bounded principal scopes

`RlsSqlScripts/012_principal_scopes.sql` adds the scopes of ADR-011 next to `app` and `worker`. None of them
is a member of `ops.is_worker_scope()` (an architecture test pins that list). `ops.is_worker_scope()` itself
is the scope value `worker` AND a connection whose login (`session_user`) is a member of `notrelix_worker`:
the scope value is a session setting any connection can write, so an application login that sets it, or
sets `system`, `migration` or `migrator`, sees nothing extra:

```text
user    authenticated user, no tenant        ApplyUserScopeAsync(userId)
token   one account/workspace named by a    EnterTokenScopeAsync(request)  ->  ops.enter_token_scope(...)
        presented token digest
```

`DataSessionBehavior` applies the user scope to every Global authenticated request
(`RequestDataSessionOptions.UserScopeUserId`; mutually exclusive with the tenant scope). `ResourceLocator`
runs its lookup in a short transaction in the user scope when no data session is open, so a row the
caller holds no grant on is not found; inside an open data session the session's own scope applies. Both
scope entries are transaction-local and require an open transaction. `ApplyUserScopeAsync` is a no-op while
`Rls:SetSessionContext` is false. `EnterTokenScopeAsync` is the only way to enter a token scope: the database
derives account and workspace from the digest, logs the entry with its reason, and counts it
(`rls_scope_entry`); the token value is never logged. The invitation flows (34D) and API-token authentication
use it; the scope records its purpose (`app.scope_token_purpose`: `workspace-invitation` or `api-token`) and
every policy written for one purpose checks it.

---

# 34C. Co-member display facts

`identity.users` exposes only the caller's own row to the application role: the row carries the email, the
password hash and security state, and a row policy cannot hide columns. Display name and avatar of another
person resolve through owner-defined functions in `RlsSqlScripts/013_co_member_profiles.sql`:

```text
ops.co_member_profiles(uuid[])          -> (user_id, name, avatar)      ActorLookupService
ops.co_member_avatar_storage_key(uuid)  -> storage key                  AvatarStorageKeyLookup
ops.shares_workspace_with_caller(uuid)  the relationship rule both use
```

Relationship: the caller (`app.current_user_id`, in the `app` or `user` scope) and the requested person hold a
`workspace.workspace_members` row in the same workspace, in any membership state (active, suspended,
removed) and any role (guests included), so historical attribution keeps resolving. A person always resolves
themselves; the worker scope (a worker login only) resolves any id; a token (other than the inviter case of an
invitation), credential or registration scope resolves nobody. The
functions are `SECURITY DEFINER` with a pinned `search_path`, revoked from PUBLIC and executable by the
application role only. Which members a person may LIST remains an Application authorization rule; the
functions resolve display facts for ids the Application already decided to show. Email and phone are not
reachable through them. The avatar endpoint additionally keeps its Application rule (an active shared
workspace) on top of the function's relationship.

---

# 34D. Anonymous credential flows

`RlsSqlScripts/014_anonymous_scopes.sql` gives the flows that run before the caller has a tenant their own
bounded scopes. Each is entered through an owner-defined `SECURITY DEFINER` function (pinned `search_path`,
revoked from PUBLIC, executable by the application role only), each derives its bound from a presented digest
or from the rows it is about to create rather than from identifiers the application asserts, and each ends
with its transaction. None is a member of `ops.is_worker_scope()`.

```text
token         one invitation: account + workspace + digest        enter_token_scope        (preview, decline)
invitee       token + the person the invitation is addressed to   enter_invitee_scope       (accept while signed in)
registration  one brand-new user and the one account it creates   enter_registration_scope  (register, register-and-accept)
credential    one existing user derived from a credential         enter_credential_scope    (login, refresh, verify, reset)
```

How they work together:

- `InvitationTokenScope.FindAsync` enters the token scope from the digest of the presented secret; the database
  derives the invitation's own account and workspace. A bare token scope reads exactly the invitation row, the
  invited workspace row and (for a resource invitation) the one named board or page, may update only that
  invitation, may insert the internal event/outbox/audit rows the decline raises, and resolves only the inviter's
  display name. Unknown, malformed, expired or used tokens enter nothing that can be written through.
- `enter_invitee_scope(user)` binds the person accepting: the user must be the authenticated caller, the
  invitation pending and unexpired, and the invited address the person's own. The person is then provisionally
  inside the invited account/workspace for that request (`ops.has_workspace_access`/`has_account_access`
  honour `ops.has_invitee_scope`, never as admin), so the acceptance can read what it validates and every
  grant-derived policy applies unchanged.
- The five grant helpers (`has_account_access`, `has_workspace_access`, `is_account_admin`, `is_workspace_admin`,
  `has_permission`) are static `LANGUAGE sql` bodies in `RlsSqlScripts/025_authz_access_helpers_static.sql`, which
  runs after the schema-aware `003` versions and leaves its bodies installed. A row policy calls them once per row, so
  they must not build SQL or probe the catalog per call (the dynamic versions cost about 0.18 ms per row). An active
  grant is `revoked_at IS NULL AND membership_status = 'Active'`; a column of `authz.access_grants` that goes missing
  fails the apply instead of changing who has access. Any change keeps the old/new truth table at zero differences.
- `enter_registration_scope(user)` is refused for an id that already exists. A registration may insert exactly
  that user (and, when it carries an invitation binding, only with the invited address) and exactly the one
  account named by its first grant. Whether an address is already registered is answered by
  `ops.email_registered`, a boolean, never a row.
- Grants (Q7): inside a registration or invitee scope `AccessGrantProjectionService` writes the grant
  immediately through `ops.upsert_access_grant`, which derives the role codes and admin flags itself and
  accepts only the owner grant of the one new account (registration) or the invitee's account-level Member grant
  and the invitation's own role in the invitation's own workspace (invitee). Outside those scopes the grant
  projection is unchanged. Account and user rows are written with INSERT-only policies bound to the scope, not
  broad policies.
- `enter_credential_scope(kind, credential, hash_version)` finds the ONE user a presented credential names (a
  normalized address, a refresh token digest, an email-verification digest, a linked provider identity) and binds
  the scope to that user alone: the ordinary own-row policies then apply, and one-time credential rows
  (`email_verification_tokens`, `password_reset_tokens`) are readable and writable only for the scope's user. An
  unknown credential enters nothing, so the handler's own lookups find nothing, exactly like today. Login,
  forgot/reset password, resend/confirm verification, refresh and OAuth sign-in use it (`CredentialScope`); MFA
  completion and OAuth step-up use the ordinary user scope for the user their server-held challenge or state
  record names. There is no broad SELECT policy on `identity.users` for any of this.
- Boolean probes and exact-row reads replace row reads that no scope may have: `ops.email_registered` (an
  address is taken; registration, invited registration, invitation preview, email change, OAuth sign-up),
  `ops.oauth_identity_linked` (a provider identity is linked to someone else), and
  `ops.resolve_workspace_slug` (the public resolve-by-slug card of one active workspace). Each returns only its
  answer or its fixed columns.
- Audit (Q3): entering a bounded scope writes a structured log line (scope kind, reason, derived ids, never a
  token or password) and increments `rls_scope_entry`; durable `audit.security_events` rows of the flows are
  unchanged.

---

# 34E. Internal principals: API tokens, audit, grant writes, member email and phone

`RlsSqlScripts/015_internal_principals.sql` (and the `ops.upsert_access_grant` / `ops.revoke_access_grant` part
of `014`) add the bounded paths that remain for the application login:

- **API-token authentication.** `ApiTokenAuthenticationHandler` hashes the presented bearer secret and enters
  the token scope with purpose `api-token` (`ops.enter_token_scope`), inside a short transaction of its own.
  The function finds the token by digest and enters the scope only for a token that is active, unexpired and
  bound to a user; an unknown, revoked, expired or user-less token enters nothing. Policies
  (`ops.has_api_token_binding`) let the scope read and update that single `identity.api_tokens` row (recording
  its use) and insert the internal event and outbox rows that recording raises for the token's own
  tenant; nothing else of the tenant opens up. The request then runs through the ordinary pipeline as the
  token's user and workspace.
- **Security audit.** `SecurityAuditSink` writes `audit.security_events` through the insert-only
  `ops.record_security_event` (known event types only; actor and tenant derived from the scope it enters, never
  supplied). A failed write is logged at error level and counted (`security_audit_event_write`). On
  PostgreSQL the sink always uses the function; only a provider without it inserts directly.
- **Admin-driven grant writes.** `AccessGrantProjectionService` writes grants through `ops.upsert_access_grant`
  and ends them through `ops.revoke_access_grant` whenever the scope runs as the application principal on
  PostgreSQL (a service built without a session context, such as an owner seeding connection, writes directly): add, remove, suspend, activate and re-role a member, create a workspace, transfer
  ownership. Inside the tenant scope a write lands only in the request's own account and workspace; Owner only
  by the current Owner (transfer) or for the caller creating a new workspace of their account; the
  account-level grant is Member only and never replaces an administrator's. A person leaving still ends their
  own grant through the own-grant update policy. A worker-principal scope (consumers: the first-workspace
  provisioning) writes the rows directly through its worker policy.
- **Other people's email and phone.** `ops.disclosable_member_emails` and `ops.shared_member_phones` mirror the
  Application rules (email: active Owner/Admin reader, active non-guest subjects; phone: active non-guest reader
  and subject, subject opted in) and take the Workspace as a parameter, which is why
  `IIdentityUserFacts.FindEmailsAsync` and `IMemberPhoneLookup.FindSharedPhonesAsync` take a workspace id.
  `MemberEmailReader` and `MemberPhoneLookup` always call them on PostgreSQL.

---

# 34F. Policy coverage, derived scopes and the webhook scope

**Every application table has forced row-level security and a policy.** `RlsPolicyApplier` verifies the
catalog after applying the pack and fails the apply for ANY base table of ANY schema (except the migration
history `ops.__EFMigrationsHistory`) that has RLS disabled, not forced, or no policy; `PolicyCoverageTests`
asserts the same against the database the migrations produced, and that no policy of the application role is
unconditional except the read-only shared catalogs (`billing.plans`, `plan_prices`, `plan_limits`,
`automation.automation_templates`). The generic helpers no longer skip silently: `apply_scoped_business_policies`
raises for a table with no `account_id`/`workspace_id`, `apply_user_owned_policies` raises for a missing owner
column, and `apply_worker_internal_policies` creates an application INSERT policy only when a tenant scope
exists (a tenantless internal log is the worker's alone; the old fallback of `true` admitted any insert).

`016_policies_derived_scope.sql` declares the tables without a tenant column of their own, which had been
open to the application login because the generic helper skipped them: `automation.automation_execution_steps`,
`automation.automation_templates` (shared catalog), `billing.invoice_line_items`, `subscription_items`,
`usage_metric_history`, `governance.custom_role_permissions`, `governance.member_role_assignments`,
`identity.mfa_recovery_batches`, `identity.mfa_recovery_codes`, `integration.calendar_events`,
`calendar_event_links`, `integration_scopes`, `integration_secret_versions`, `integration_sync_cursors`,
`integration_secret_blobs`, `inbound_webhook_receipts`, `notifications.notification_recipients` (owner column
`recipient_user_id`; the earlier call named a column that does not exist) and `ops.idempotency_records`
(partition string `account:{N}[:workspace:{N}|:user:{N}]` checked with `ops.has_idempotency_scope`; `system` is
the worker's). A child inherits its parent's scope; `DerivedScopePolicyTests` proves the application login
reads and writes only inside a scope it holds.

**Tenantless person events.** A Global request, and a tenant-scoped request that changes one of the person's
own facts (choosing the default workspace changes the profile), raises domain events with no tenant. The
INSERT-only policies `p_app_insert_user_scope` on `events.domain_event_logs` and `messaging.outbox_messages`
admit such a row (`ops.has_tenantless_actor_scope`) in the user scope or the `app` scope of the person who
raised it, never for another actor.

**Verified calendar webhook.** `HandleCalendarWebhook` authenticates by provider signature, so no user's
grants exist for a policy to read. After verification the handler enters a token-kind scope with purpose
`calendar-webhook` (`RlsSessionContext.EnterWebhookScopeAsync` -> `ops.enter_webhook_scope(path)`), which the
database derives from the active, undeleted calendar binding the webhook path identifies. The scope admits
only the receipt claim (`integration.inbound_webhook_receipts` INSERT and the RETURNING read-back) and the
processing message (`messaging.outbox_messages` INSERT) for that account and workspace
(`ops.has_webhook_binding`, `017_webhook_scope.sql`); the tenant-scoped consumer then runs on the worker
principal. The entry is logged with its reason and counted (`rls_scope_entry`, scope `webhook`); the path is
never logged.

---

# 34G. Access tightening (runs last in the pack)

`RlsSqlScripts/019`–`024` only remove access (`019_policy_helpers_migrator_only`, `020_support_role_no_content`, `021_billing_and_account_writes`, `022_audit_append_only`, `023_custom_role_assignments`) or add the two bounded functions of the last bullet (`024_membership_end_access`), and run after every script that grants access (004/005 re-grant on each apply):

- **Policy-DDL helpers are migrator-only.** `ops.create_policy`, `drop_all_policies_for_table`, `enable_rls_for_table` and every `ops.apply_*` are `SECURITY DEFINER` and edit policies; a function keeps EXECUTE for PUBLIC unless it is revoked, so any runtime login could have added an unconditional policy of its own. EXECUTE is revoked from PUBLIC and the four runtime roles. Moving the security helpers to another schema was considered and not done: grants are per function, so a move adds no stricter grant and would rewrite every policy expression.
- **Support reads no tenant data and no credential.** `notrelix_support_readonly` keeps SELECT only on the shared catalogs (`billing.plans`, `plan_prices`, `plan_limits`, `automation.automation_templates`); its open `p_support_select` policy is dropped everywhere else so a grant added by mistake stays closed. Each support case type (billing, account/login, feature errors, data recovery) gets its own bounded grant when it is designed.
- **Billing.** The application and auth roles have no INSERT/UPDATE/DELETE on `billing_customers`, `payment_methods`, `invoices`, `invoice_line_items`, `subscription_items`, `entitlements`, `billing_events`, `usage_metrics`, `usage_metric_history` or the plan catalog, and `feature_usage_ledger` is append-only for them. `subscriptions` and `workspace_feature_usages` stay writable because the development plan switch and capacity consumption still run as the application role (open decision, see the change record).
- **Account plan and status.** UPDATE on `account.accounts` is column-granted to `name, legal_name, default_region_code, updated_at, updated_by, version`; plan, status and the deletion marker are the platform operator's (worker role).
- **Audit is append-only** for the application, auth and support roles on every `audit` table; the worker keeps its retention sweep.
- **Custom-role assignments.** `governance.member_role_assignments` is written only by a workspace administrator for an ACTIVE member of that workspace (`member_id` is the person's user id: the Governance event carrying it maps to the integration event's `UserId`); reading follows workspace access.
- **Derived access ends with the membership.** `ops.revoke_account_grant_if_last_workspace` ends a person's account-level Member grant in the request transaction when they hold no other active workspace grant of the account (an account administrator's grant is never touched), and `ops.clear_member_custom_roles` deletes a member's assignments. Both are bounded to the `app` scope of the request's own account and workspace and to the person themself or a workspace administrator. `RemoveMember` and `LeaveWorkspace` call both; `ActivateMember` and a returning `AddMember` call the second (a reactivated member returns to their built-in role and is configured again). A suspension keeps the account grant: it is reversible and the person stays a member.

Tests: `DatabaseAccessTighteningTests` (grants, policies, support, DDL helpers, assignments) and `AdminGrantWriteTests` (the lifecycle over HTTP as the application role).

---

# 35. Same physical connection

ADR-002 relies on the same scoped DbContext/physical connection lifecycle between bootstrap and later full request scope.

If this lifecycle changes, the ADR/architecture/test assumptions must be re-evaluated.

---

# 36. BE-INF-017 — RLS lifecycle changes require connection-pooling/reuse proof

Test:

```text
bootstrap
full scope
transaction
pool return
next request with different tenant
background/system scope
```

as applicable.

Do not prove only first-request success.

---

# 37. RLS policy design

Policy should be based on explicit ownership relationships.

Do not auto-generate policy solely from column naming such as:

```text
workspace_id exists
→ generate one standard policy
```

because membership/resource semantics differ.

---

# 38. BE-INF-018 — RLS policy is context-aware persistence enforcement

Before adding policy identify:

```text
table owner
Account/Workspace ownership path
user/service access pattern
system/background access
required indexes
delete/archive semantics
```

Then implement/test.

---

# 39. RLS tests

At minimum for changed tenant-protected persistence:

```text
allowed tenant
denied foreign tenant
```

plus any relevant:

```text
pooled connection leakage
background worker scope
system operation
bootstrap
```

Use PostgreSQL/Testcontainers.

---

# 40. BE-INF-019 — RLS test uses real PostgreSQL semantics

Do not use EF InMemory/SQLite to claim policy correctness.

The policy is a database feature.

---

# 41. EF global query filters

Query filters can improve default scoping/lifecycle behavior.

They are not a replacement for RLS or authorization.

A query intentionally bypassing an EF filter still remains subject to DB RLS unless privileged/system path changes it.

---

# 42. BE-INF-020 — Filter bypass is explicit and narrow

Use filter bypass only for:

```text
bootstrap
admin/system operation
restore/history
special query
```

with security reasoning.

Do not call `IgnoreQueryFilters()` as a general fix for missing results.

---

# 43. System context

Infrastructure may support privileged/system DB scope.

Such scope must be:

```text
explicit
audited
least privilege
bounded to operation
```

Do not make system context ambient/global.

---

# 44. BE-INF-021 — System DB context is not a universal tenant bypass

A system process still identifies:

```text
operation purpose
resource scope
audit/correlation
```

where applicable.

Do not let ordinary handlers opt into system context to avoid RLS problems.

---

# 45. Persistence concurrency

Infrastructure maps aggregate/resource version to database concurrency mechanism.

It should detect stale update and translate technical conflict to Application semantics.

---

# 46. BE-INF-022 — Persistence concurrency cannot silently overwrite newer state

Do not disable concurrency checks because retries are difficult.

Do not automatically reload + update unless the Application/product operation defines merge/retry.

---

# 47. Transactions

Infrastructure implements the transaction declared by Application.

It must preserve:

```text
atomic local state
RLS transaction scope
outbox/durable enrollment
rollback on failure
```

according to architecture.

---

# 48. BE-INF-023 — Infrastructure does not extend transaction around arbitrary external provider calls

Provider call inside the same method does not make provider state transactional.

Avoid long locks and unknown outcomes.

Use Application/Platform orchestration.

---

# 49. Interceptors

EF interceptors may implement technical cross-cutting persistence mechanisms.

Examples:

```text
auditing
outbox enrollment
tenant/session behavior
```

where architecture defines them.

Do not hide product business transitions in interceptors.

---

# 50. BE-INF-024 — Interceptor cannot invent Domain event/business state

An interceptor may persist/transform technical metadata from an already-decided operation.

It must not decide:

```text
subscription should cancel
workspace role should change
item should move
```

based on database state alone.

---

# 51. Read ports

Infrastructure can implement optimized Application read ports.

Read models may use:

```text
projection
SQL/EF query
joins
denormalized columns
```

as long as source ownership/security remain correct.

---

# 52. BE-INF-025 — Read model may denormalize, but source writes remain owned

Do not expose:

```text
read projection repository.Save()
```

as a mutation path to source contexts.

Rebuild derived data from authoritative sources.

---

# 53. Cross-context joins

Within the modular monolith/shared DB, a read projection may join context-owned tables for an explicitly owned read use case.

This is not permission for cross-context write coupling.

Future extraction may replace the join with projection/event replication.

---

# 54. BE-INF-026 — Cross-context read coupling is visible and replaceable

For high-value cross-context query:

```text
identify source owners
identify freshness need
identify extraction alternative
```

Do not bury it in generic repository code.

---

# 55. Cache role

Redis/cache is:

```text
derived/scoped acceleration
```

unless a separately accepted architecture explicitly assigns another role.

Cache loss must not lose authoritative product state.

---

# 56. BE-INF-027 — Cache is never the sole authorization or product authority

Do not store the only copy of:

```text
permission
membership
entitlement
business mutation
```

in Redis.

Cache miss/outage should fall back safely or degrade.

---

# 57. Cache keys

Include scope needed to prevent collision/leakage.

Potential dimensions:

```text
Account
Workspace
resource
user/principal
permission version
query/version
```

depending on data.

Do not use one global key for tenant-sensitive result.

---

# 58. BE-INF-028 — Authorization-sensitive cache is scope/version aware

On:

```text
membership revoke
permission change
share revoke
resource move
```

stale allow/result must be invalidated or made unreachable through versioned key semantics.

---

# 59. Cache serialization

Cache payload is a derived contract.

Version/serialization changes should either:

```text
remain backward readable
version key
invalidate old cache
```

Do not let deserialization failure take down authoritative writes.

---

# 60. Cache outage

If authoritative DB path is safe/capable:

```text
cache miss/outage
→ safe origin read
```

Otherwise degrade according to operations policy.

Do not serve unsafe stale permission data to preserve hit rate.

---

# 61. BE-INF-029 — Cache fallback is capacity-aware and fail-safe

A global cache outage can stampede PostgreSQL.

Use bounded fallback/monitoring.

Do not turn cache bypass into another outage.

---

# 62. Provider adapter

Infrastructure adapter maps between:

```text
Application capability contract
↔
provider protocol/SDK
```

Provider terms remain at the edge.

---

# 63. BE-INF-030 — Provider DTO does not leak into Domain/Application public semantics

Convert:

```text
provider status/code/payload
```

to stable Application result/fact.

Keep raw provider details for safe diagnostics where needed.

---

# 64. Provider timeout

Every network/provider operation has bounded timeout/cancellation appropriate to the operation.

For external writes, timeout may mean unknown outcome.

---

# 65. BE-INF-031 — Adapter distinguishes transient failure, terminal rejection, and unknown outcome

Examples:

```text
429/rate limit
→ transient/backoff

invalid credential
→ terminal/re-auth required

timeout after write request
→ outcome unknown/reconcile
```

Do not classify every exception as retryable.

---

# 66. Provider retry

Adapter-level retry may be appropriate for transient safe operations.

It must preserve:

```text
idempotency
rate limit
cancellation
provider capacity
```

Do not retry irreversible write without stable provider operation identity/reconciliation.

---

# 67. BE-INF-032 — Provider retry never creates duplicate business side effect silently

Use provider idempotency key/correlation or reconcile current provider reality.

Application owns pending/unknown semantic result where needed.

---

# 68. Email

Email provider adapter implements notification/email capability.

It does not own:

```text
who should receive
what business event occurred
what permission user has
```

Application/context decides recipient/content intent; Infrastructure sends through provider.

---

# 69. Identity/auth provider mechanics

Infrastructure can own:

```text
ASP.NET Identity persistence
password hashing
JWT signing/validation implementation
OAuth provider adapters
token persistence
```

Product Identity semantics remain Identity context/Application.

---

# 70. BE-INF-033 — Authentication provider mechanism does not define Workspace authorization

A valid JWT/user account does not imply resource permission.

Keep resource authorization in Application/Governance.

---

# 71. Password hashing

Password hashes are sensitive technical authentication data.

Use established secure hashing implementation/config.

Do not expose hashes to Domain events/logs/API.

Hash algorithm/cost changes are security/data migration concerns.

---

# 72. JWT

JWT claims are authentication/context inputs.

Do not treat client-presented claim as final resource authorization if current server policy must resolve resource membership/permission.

Signing keys are secret runtime configuration.

---

# 73. OAuth provider mapping

External subject/provider IDs map to Identity-owned user/account semantics.

Do not replace internal stable user identity with provider subject as the global Domain ID automatically.

Provider unlink/relink/rotation must preserve internal ownership.

---

# 74. Storage

Storage adapter owns binary/object mechanics.

Domain/Application typically own:

```text
metadata
ownership
lifecycle
authorization intent
```

Infrastructure owns:

```text
upload/download/delete protocol
object key
signed access mechanism
provider error
```

---

# 75. BE-INF-034 — Object URL/key is not authorization

A storage key identifies an object.

Access is checked by the owning use case and/or time-bounded signed mechanism.

Do not expose a permanent public URL for protected data merely for convenience.

---

# 76. File upload

For multi-step:

```text
object upload
+
DB metadata
```

define pending/final/orphan semantics.

Do not report final success if the durable object or metadata did not complete according to the contract.

## Attachment upload (slice 1) pending/final/orphan semantics

The object is written inside the request before the metadata row is saved; no pending row exists. The handler registers a compensation (`IRequestRollbackCompensations`) before the write. `EfRequestDataSession` runs it when the request fails before the commit starts (stream error, size cap, Domain rejection, cap exceeded at the commit point, SaveChanges failure, client abort), so no row and no object remain. If the commit itself fails the outcome is unknown and the object is deliberately left (an object without a row is unreachable because downloads require the row; the reverse is not acceptable). The orphan reaper below reconciles those objects. The storage port is key-centric (`IStorageService`: write, open, delete, list); a presigned flow is an additional capability, not a replacement.

### Per-item cap at the commit point

The early count before streaming is advisory (fail fast). The authoritative check runs after the bytes are stored and right before the row is added: the handler takes `IAttachmentTargetLock` (PostgreSQL `pg_advisory_xact_lock` over a SHA-256 hash of the board item resource, same pattern as `PostgresWorkOrderingScopeLock`), recounts the item's non-deleted attachments and lets the Domain factory reject when the cap is reached. The lock lives inside the request transaction, is released at its commit or rollback, and is never taken before or during the body stream, so a minutes-long upload does not serialise or block other writers of the item. Link creation takes the same lock and recounts, so links and files share one cap atomically. A rejected upload is compensated like any other pre-commit failure (409 `attachment.limit_exceeded`, object deleted, no row).

### Retention, purge and orphan reconciliation

`AttachmentObjectReaperService` (Infrastructure hosted service, options `Collaboration:Attachments:Retention`, see `operations/configuration-and-runtime.md`) runs two idempotent steps per pass under the explicit system (worker) tenant context with the RLS worker scope (`SystemContextScope` + `IRlsSessionContext`; no RLS bypass, it only reads and updates `collab.attachments`):

1. Purge: stored-file rows soft-deleted strictly longer than `DeletedFileRetention` (default 30 days) and not yet purged are claimed with `FOR UPDATE SKIP LOCKED`; per row the object is deleted first, then `purged_at` is set in the claim transaction. A crash between the two repeats an idempotent delete. A purged attachment cannot be restored (`Attachment.Restore` is rejected). A failure on one object is logged and does not stop the batch; the row is retried by the next pass.
2. Orphan sweep: attachment objects (`{account}/{workspace}/attachments/{object}`) older than `OrphanGracePeriod` (default 24 h, by the provider's last-modified time, never by request state, and longer than the longest upload so an in-flight upload is never reaped) that no non-purged row references are deleted. One instance sweeps at a time (transaction-scoped try-advisory lock).

The additive column `collab.attachments.purged_at` (migration `AddAttachmentObjectPurge`, nullable, plus the partial index `ix_attachments_purge_pending`) is covered by the existing table RLS policies; no policy change was needed.

Storage usage: soft-deleted rows keep their object (and therefore count toward storage usage) until purged; a future Billing quota must count non-purged stored files, deleted ones included, until `purged_at` is set. No quota is implemented.

Not covered: the reaper does not cascade-delete attachments of a deleted board item or workspace; it only handles rows that are soft-deleted for any reason.

### Profile photo objects and the avatar sweep

A profile photo object is stored under `users/{userId:N}/avatar/{objectId:N}.{png|jpg|webp}` and referenced by `identity.users.avatar_storage_key` (additive nullable column, migration `AddAvatarObjectAndDefaultWorkspace`, together with the nullable `identity.user_profiles.default_workspace_id`). A replaced or removed photo leaves an unreferenced object; `AvatarObjectReaperService` removes it. A pass lists `users/` objects older than `OrphanGracePeriod` (same options as the attachment sweep) and hands each batch to `PostgresAvatarReaperStore`, which runs under the explicit system (worker) tenant context with the RLS worker scope and the same transaction-scoped try-advisory lock as the attachment sweep. It FAILS CLOSED: an object is deleted only when its owner's `identity.users` row is visible to the sweep and names a different object; an owner row that cannot be read (missing policy, wrong scope, deleted owner) keeps its objects and logs a warning, and a query error deletes nothing. For the application role the sweep reads users through the worker-scope SELECT policy `p_app_select_worker_scope` (`006_policies_identity.sql`), which `ops.is_worker_scope()` gates; a request session never satisfies it. Policies are applied by the RLS script (`--rls-apply`), not by an EF migration.

The application role also has one narrow write on `authz.access_grants` (`p_app_update_revoke_own`, `007_policies_platform.sql`): a person may end their OWN active grant (the row must be active before and revoked after). It exists for self-leave and cannot create, edit, reactivate or touch another person's grant. `LeaveWorkspace` flushes the membership change before revoking the grant (the leaver's grant is what lets them write the membership row), inside the request transaction.


---

# 77. Search

Search adapter/index is derived.

Search can optimize:

```text
full text
ranking
cross-resource discovery
```

It does not own source business data.

---

# 78. BE-INF-035 — Search index is rebuildable/derived unless explicitly classified otherwise

Write source context first.

Update index/projection asynchronously/synchronously as architecture defines.

If index is lost, rebuild from authoritative source.

---

# 79. Search authorization

Index/search result must preserve tenant/resource visibility.

Do not return search result then rely solely on frontend to hide unauthorized entries.

Use:

```text
scoped index
permission projection
post-filter with authoritative check
```

as architecture requires.

---

# 80. Projections/read models

Infrastructure can materialize projections for:

```text
governance
analytics
work-management read models
search
```

Current source contains projection/read-model folders.

These are derived representations.

---

# 81. BE-INF-036 — Projection has explicit source facts and freshness

Know:

```text
producer
source owner
update trigger
lag/freshness
rebuild path
tenant scope
```

Do not let projection drift silently become business truth.

---

# 82. Analytics storage

Analytics snapshots/history may intentionally preserve historical reporting state.

Distinguish:

```text
rebuildable current projection
historical immutable snapshot
```

Do not rebuild/overwrite historical report evidence automatically unless product Analytics semantics say so.

---

# 83. Messaging adapter boundary

Infrastructure currently references MassTransit/RabbitMQ and contains messaging integration.

Platform owns reusable delivery semantics.

Infrastructure can implement broker-specific adapter/registration/serialization.

---

# 84. BE-INF-037 — Broker provider implementation does not own delivery semantics

Infrastructure may configure:

```text
RabbitMQ endpoint
MassTransit adapter
transport serializer
connection
```

Platform owns:

```text
logical message/consumer identity
retry/poison/order rules
```

Product context owns event meaning.

---

# 85. Event persistence

Infrastructure may persist:

```text
outbox
consumer dedup
ordering cursor
poison/dead-letter metadata
```

as Platform requires.

The schema supports the mechanism.

It does not redefine the Platform semantics.

---

# 86. BE-INF-038 — Delivery schema changes preserve Platform identity/invariant

Before changing message tables/indexes:

```text
inspect logical key
retention
ordering
retry
replay
migration/backlog
```

Do not rename/drop technical columns without delivery-contract review.

---

# 87. Serialization

Infrastructure serialization can map:

```text
Domain/Application/event contract
→ transport/storage representation
```

Versioning must preserve compatibility where old data/messages exist.

Do not serialize private CLR type name as long-term public discriminator by default.

---

# 88. BE-INF-039 — Persisted discriminator/event name is a compatibility contract

Changing:

```text
enum string
JSON type discriminator
event name
provider mapping key
```

can be a C3/C4 migration even if C# refactor is small.

Inventory old data/consumers before change.

---

# 89. JSON columns

JSON can be useful for flexible/versioned data.

It does not eliminate schema design.

Define:

```text
owner
version
validation
query/index needs
migration
unknown fields
```

Do not dump arbitrary provider payload into authoritative Domain storage without ownership.

---

# 90. BE-INF-040 — JSON schema/version evolves deliberately

If persisted JSON shape changes:

```text
reader compatibility
writer version
backfill/lazy migration
old rows
```

must be understood.

Do not assume serializer default handles all historical shapes forever.

---

# 91. EF converters

Converters bridge Domain value types and provider representation.

They should be deterministic/reversible as required.

Do not hide lossy conversion of meaningful precision/identity.

---

# 92. BE-INF-041 — Converter preserves Domain equality/meaning

Round-trip:

```text
Domain value
→ DB
→ Domain value
```

should preserve semantic equality.

Test provider-specific conversions with real PostgreSQL where relevant.

---

# 93. Date/time

Persist timestamps with explicit UTC/offset semantics consistent with Domain/Application time contracts.

Do not mix local server timezone implicitly.

Time-zone presentation belongs outside persistence.

---

# 94. Money/decimal

Persist exact money/decimal semantics with appropriate precision/scale.

Do not silently convert to floating point.

Billing product semantics define currency/rounding; Infrastructure preserves them.

---

# 95. IDs

Current Domain base uses Guid/Guid v7.

PostgreSQL representation should preserve full identity semantics.

Provider external IDs remain separate columns/value mappings where needed.

Do not overload internal ID column with provider ID.

---

# 96. BE-INF-042 — Persistence identity mapping is stable across provider/internal IDs

Store explicit:

```text
internal stable ID
provider external ID
```

when both exist.

Do not make provider reconnection alter source entity identity.

---

# 97. Delete behavior

Database cascade/restrict/set-null behavior must match product aggregate/reference lifecycle.

Do not enable cascade merely to make FK cleanup easy.

A DB cascade can erase foreign context/history unexpectedly.

---

# 98. BE-INF-043 — Delete behavior is reviewed with semantic ownership

Ask:

```text
is child aggregate-owned?
is reference cross-context?
is historical record retained?
is restore supported?
is physical deletion allowed?
```

Then choose FK/delete behavior.

---

# 99. Soft-delete/query filters

If a context uses logical deletion/tombstone mechanism, EF filters can hide rows in ordinary queries.

Do not equate query filtering with lifecycle authorization.

Restore/history/system paths may need explicit access.

---

# 100. BE-INF-044 — Query filter is persistence convenience, not lifecycle rule

Domain/Application still decide:

```text
what deleted means
who may restore
which operations are forbidden
```

Do not implement product deletion solely with an EF filter.

---

# 101. Auditing

Infrastructure can persist shared audit fields and Governance audit records through approved contracts.

Do not turn EF SaveChanges interception into the only product Activity/Audit definition.

Application/product owner decides which facts need which history.

---

# 102. BE-INF-045 — Infrastructure audit mechanism consumes decided semantic facts

It may record:

```text
actor
time
entity/resource
operation
```

after use-case decision.

It must not infer high-level product event solely from changed columns if semantics are ambiguous.

---

# 103. Observability

Infrastructure logs/metrics should expose:

```text
provider/database dependency health
query/migration/backlog correlation
safe resource/tenant identifiers
retry/outcome class
```

without secrets/private payload dumping.

---

# 104. BE-INF-046 — SQL/provider logging is privacy and cardinality aware

Do not enable full sensitive-data logging in production as ordinary diagnostics.

Do not label metrics by arbitrary resource IDs with unbounded cardinality.

---

# 105. Connection pooling

Npgsql connection pools are shared runtime resources.

Session-level settings, temporary state, transaction status, and failures must be safely reset/reused.

ADR-002 specifically depends on pool cleanup semantics.

---

# 106. BE-INF-047 — Connection state does not leak between requests

Test/reason about:

```text
tenant A
→ pool return
→ tenant B
```

especially for RLS/session config.

A successful first request proves nothing about pool isolation.

---

# 107. Database retry

Transient database retry must respect transaction/idempotency.

Do not retry an operation whose commit outcome is unknown without knowing whether duplicate mutation can occur.

Provider/EF execution strategies require careful transaction semantics.

---

# 108. BE-INF-048 — Retry classification is mechanism-aware

Classify:

```text
connection transient
deadlock/serialization conflict
constraint violation
cancellation
timeout with unknown commit
```

differently.

Do not wrap every DB exception in an infinite retry policy.

---

# 109. Deadlocks/locks

For hot paths monitor/review:

```text
lock ordering
transaction duration
batch size
index
query plan
```

Do not solve deadlock by disabling transaction correctness.

Application/Domain invariant still defines what must remain atomic.

---

# 110. Backfills

Infrastructure may implement migration/backfill tooling.

It must be:

```text
bounded
resumable
idempotent
tenant-safe
observable
cancellable
```

for large production work.

Use repository migration template for material cases.

---

# 111. BE-INF-049 — Backfill has stable traversal and checkpoint

Avoid:

```text
OFFSET page through changing table
```

for very large mutable datasets where it can skip/duplicate.

Use a stable key/range strategy appropriate to the schema.

---

# 112. Backfill invalid data

If legacy rows cannot map deterministically:

```text
quarantine/report
approved normalization
block migration
explicit sentinel
```

Do not guess.

Product/context owner decides meaning.

---

# 113. Database backup/recovery boundary

Infrastructure provides database backup/restore mechanism.

Operations owns recovery decision/reconciliation.

Do not claim:

```text
snapshot exists
→ recovery solved
```

Recovery must verify application/RLS/outbox/provider state.

---

# 114. BE-INF-050 — Restore does not skip reconciliation

After DB restore/repair inspect:

```text
schema/migration
RLS
outbox
dedup/order
provider effects
object storage
cache/search/projections
```

according to recovery policy.

---

# 115. Configuration/options

Infrastructure can bind provider/runtime options.

Typed options should validate required values.

Do not use permissive production fallback for:

```text
DB
JWT
provider secret
CORS/security
```

Exact environment ownership belongs to repository Infrastructure docs.

---

# 116. BE-INF-051 — Secret value never becomes Application/Domain config data

Infrastructure resolves secrets at the adapter/composition boundary.

Pass only the semantic/provider result needed inward.

Do not store secret in Domain event/result/log.

---

# 117. Dependency injection

Infrastructure registers implementations for inward contracts.

Composition should make provider choice replaceable.

Avoid service locator/static global access from Domain/Application.

---

# 118. BE-INF-052 — DI registration does not erase ownership

A service registered as `Singleton`/`Scoped` does not decide its semantic layer.

The interface responsibility and implementation dependency determine ownership.

---

# 119. Lifetime

Choose lifetime based on:

```text
thread safety
request/transaction scope
connection/provider client guidance
cache/state
```

not habit.

DbContext remains scoped to the request/unit of work as architecture requires.

---

# 120. Background services

Infrastructure may host provider/integration background mechanics where appropriate, while Platform owns generic consumer/delivery mechanics.

Every background path still reconstructs:

```text
tenant
correlation
idempotency
scope
```

as required.

---

# 121. BE-INF-053 — Background Infrastructure does not use unrestricted DB scope by default

Use explicit worker/system context only when the operation genuinely requires it.

Tenant-specific work should remain tenant-scoped.

---

# 122. Rate limiting implementation

Infrastructure can implement stores/counters/provider mechanics for rate limiting.

API/Application owns policy placement/operation identity as accepted architecture defines.

Do not embed product entitlement semantics into Redis counter code.

---

# 123. Realtime implementation

Infrastructure may implement SignalR/realtime adapters if current topology places them here.

The adapter delivers Application/Platform-approved messages.

It does not own source state or permission semantics.

---

# 124. BE-INF-054 — Realtime adapter revalidates/uses authoritative scope contract

Do not trust a stale client subscription forever after membership/share revocation.

Security architecture determines subscription authorization/reconciliation.

---

# 125. Notifications implementation

Provider delivery mechanics belong here.

Notification product record/recipient semantics belong to owning context/Application.

Email/push provider failure becomes stable delivery outcome, not product event ownership.

---

# 126. Billing provider

Payment provider adapter handles:

```text
API/webhook protocol
provider IDs
idempotency
signature
unknown outcome
```

Billing context owns:

```text
Subscription
Entitlement
Invoice/Payment business meaning
Usage
```

Do not map provider state one-to-one into Billing Domain without deliberate translation.

---

# 127. BE-INF-055 — Financial provider reconciliation preserves external reality

For timeout/replay/webhook:

```text
identify provider operation/event
deduplicate
reconcile current provider state
```

Do not duplicate charge or delete financial evidence to “fix” state.

---

# 128. Webhooks

Infrastructure validates provider-level:

```text
signature
timestamp/replay token
schema
provider identity
```

before passing trusted normalized event to Application/context.

Application still decides product effect.

---

# 129. BE-INF-056 — Unverified webhook never mutates product state

Signature/replay validation happens before use-case effect.

Do not let Application receive an untrusted provider payload and hope business validation will compensate.

---

# 130. Provider webhooks and idempotency

Provider event ID is often a stable dedup fact.

Do not treat repeated webhook delivery as a new business occurrence automatically.

Persist/coordinate dedup according to context/provider contract.

---

# 131. Search/storage provider change

Switching provider can require:

```text
data migration
index rebuild
object migration
URL/key mapping
dual read
cutover
reconciliation
```

Do not change adapter registration and assume old durable external data follows automatically.

---

# 132. BE-INF-057 — Durable external-provider migration has an explicit data/state plan

Object storage/search/payment/provider resources may outlive one binary.

Inventory existing external state before provider replacement.

---

# 133. Infrastructure tests

Primary:

```text
backend/tests/Notrelix.Infrastructure.Tests
```

Use for:

```text
mapping
converters
RLS
migrations
cache adapters
provider mapping
persistence
```

Use Integration for production graph/cross-layer behavior.

---

# 134. BE-INF-058 — Test fidelity matches the Infrastructure property

Use real:

```text
PostgreSQL
```

for PostgreSQL/RLS semantics.

Use mocks/fakes only for adapter orchestration where protocol fidelity is not the protected property.

Do not overclaim.

---

# 135. Migration tests

For schema changes:

```text
clean database
+
representative previous state
+
migration
+
semantic verification
+
RLS
+
Application read/write where relevant
```

Do not only assert migration command exited successfully.

---

# 136. Cache tests

Prove:

```text
scope
serialization
expiry/invalidation
fallback
permission-sensitive behavior
```

Do not assert only `SetAsync` was called.

---

# 137. Provider tests

At the adapter seam, test:

```text
request mapping
response mapping
error classification
timeout
rate limit
cancellation
idempotency/correlation
unknown outcome
```

Use sandbox/contract integration when real provider semantics are required and safe.

---

# 138. RLS architecture tests

Where possible, gates can ensure:

```text
tenant-owned tables are registered
policy coverage rules
forbidden privileged bypass usage
```

but do not auto-generate policies from naming assumptions.

Real policy behavior still needs PostgreSQL tests.

---

# 139. BE-INF-059 — Architecture guard cannot replace runtime RLS proof

Static/source checks can detect missing declarations.

They cannot prove PostgreSQL policy execution for real session context.

Use both where useful.

---

# 140. Data ownership tests

For cross-context persistence:

```text
ensure foreign context cannot directly mutate
ensure projection writes are isolated
ensure source context remains authoritative
```

through architecture/integration tests where possible.

---

# 141. Current EF exception interaction

Application's EF package exception does not move:

```text
ApplicationDbContext
migrations
configuration
RLS
```

out of Infrastructure.

Any new direct persistence use in Application must be separately governed.

---

# 142. BE-INF-060 — Infrastructure remains the persistence implementation owner while EF exception exists

The exception narrows dependency purity.

It does not create dual persistence ownership.

---

# 143. Schema naming/versioning

Persisted strings can become compatibility contracts.

Examples:

```text
status values
discriminators
event names
feature codes
provider type
JSON version
```

Change them with migration/consumer inventory.

---

# 144. BE-INF-061 — CLR rename does not automatically rename persisted identity

A refactor can preserve:

```text
column
discriminator
event name
```

until migration is complete.

Do not let convention-based mapping silently rename production storage/public identities.

---

# 145. Naming conventions

EF naming-convention package can translate CLR naming to SQL naming.

Convention is a mechanism.

Explicit mapping should override it where stable schema compatibility requires.

Do not make a code-style rename a destructive DB migration accidentally.

---

# 146. Query filters and soft-delete uniqueness

Logical deletion can interact with unique constraints/indexes.

Decide whether deleted values can be reused according to product lifecycle.

Do not add filtered unique index solely because physical rows remain.

---

# 147. BE-INF-062 — Persistence uniqueness follows current product lifecycle

Examples:

```text
slug reusable after archive?
email reusable after account deletion?
provider connection unique while revoked?
```

Product owner decides; schema enforces.

---

# 148. Retention

Infrastructure implements retention/deletion jobs according to Product/Security/Operations policy.

Do not invent retention periods in code/config without approved owner.

Retention jobs must be tenant-safe, bounded, auditable where required.

---

# 149. BE-INF-063 — Retention mechanism does not erase required history blindly

Before deletion consider:

```text
audit
financial
legal/privacy
replay
recovery
provider state
```

Route policy to the correct owner.

---

# 150. Data repair

Production repair tooling should be:

```text
scoped
reviewed
idempotent
observable
tenant-safe
reproducible
```

Do not expose repair endpoints as ordinary product APIs.

---

# 151. BE-INF-064 — Repair derives target value from authoritative semantics

Do not guess plausible values.

Use:

```text
known source facts
deterministic migration rule
approved correction
historical evidence
```

and record the repair.

---

# 152. Infrastructure performance

Performance-sensitive persistence/provider work should consider:

```text
cardinality
query plan
indexes
connection pool
transaction duration
cache stampede
provider limits
batch size
```

Do not optimize by violating tenant or ownership boundaries.

---

# 153. BE-INF-065 — Measured performance problem does not transfer semantic ownership

A denormalized read projection/cache may solve performance.

Source writes still route to the semantic owner.

---

# 154. Connection pool capacity

Pool size is runtime configuration.

Do not hardcode one canonical number in architecture docs.

Monitor:

```text
pool exhaustion
wait time
DB max connections
worker/API concurrency
```

and tune by environment/workload.

---

# 155. Provider client lifetime

HTTP/provider client lifetime follows provider/runtime guidance and DI safety.

Do not create new raw client per request if it causes connection exhaustion.

Do not share mutable tenant credentials across scopes unsafely.

---

# 156. BE-INF-066 — Provider client separates shared transport from tenant credential/state

A reusable HTTP transport can be shared.

Tenant/provider connection authorization/tokens remain scoped and secret-safe.

---

# 157. Secrets

Infrastructure may receive:

```text
JWT key
DB secret
Redis password
provider API key
OAuth client secret
webhook secret
```

through typed runtime config/secret delivery.

Never commit/log/expose them.

---

# 158. BE-INF-067 — Secret is not persisted into ordinary business table/event accidentally

Store secret reference/encrypted credential according to approved design.

Do not copy secret into:

```text
Domain event
Activity
Audit message
cache payload
public DTO
```

---

# 159. Encryption/protection

Sensitive stored credentials may require encryption/key protection.

Infrastructure implements crypto mechanism.

Product/security owner defines which data requires protection and lifecycle.

Do not create custom cryptography without approved design.

---

# 160. Serialization privacy

Provider/raw payload logging should be minimized.

Store raw payload only when product/audit/replay requirements justify it and retention/security are defined.

Do not keep everything “for debugging”.

---

# 161. BE-INF-068 — Raw provider payload is not default durable storage

Prefer normalized owned facts.

If raw evidence is required:

```text
scope
encryption
retention
access
redaction
```

must be explicit.

---

# 162. Database timestamps

Database-generated timestamps can differ from Application-supplied business time.

Use the correct owner:

```text
business occurrence time
→ Application/Domain supplied

technical DB insertion time
→ database may supply
```

Do not substitute one silently.

---

# 163. BE-INF-069 — Persistence-generated technical metadata does not overwrite business timestamp semantics

If an event/aggregate says the operation occurred at T, mapping should preserve T.

DB `now()` may be a separate technical field.

---

# 164. Read consistency

A query may require:

```text
same transaction
current authoritative DB
cache-tolerant stale
projection eventual
```

Application defines the use-case expectation.

Infrastructure implements an appropriate query mechanism.

---

# 165. BE-INF-070 — Read port documents freshness/authority where material

Do not swap:

```text
DB read
→ stale projection/cache
```

without preserving the query contract.

Performance optimization can change user-visible semantics.

---

# 166. Replica use

If read replicas are introduced later, replication lag becomes part of query consistency.

Do not route authorization/critical read-after-write to replica without explicit tolerance.

This would be a runtime/data architecture change.

---

# 167. Multi-region

Current docs do not assume a multi-region database architecture.

Do not invent active-active conflict semantics.

If introduced, it requires system/data architecture decision.

---

# 168. Partitioning/sharding

Do not shard by tenant merely because SaaS systems sometimes do.

First prove:

```text
cardinality/load
hot tenant
storage
operational need
```

and preserve RLS/data ownership/migration semantics.

---

# 169. BE-INF-071 — Scaling topology follows evidence

Partitioning/sharding/read replica/provider change is consequential architecture.

Use ADR/change plan.

Do not hide it inside a repository implementation refactor.

---

# 170. Database schema extraction

Future service extraction may move owned tables/data.

Prepare by maintaining clear logical table ownership now.

Do not force one schema/database per context prematurely solely for extraction optics.

---

# 171. BE-INF-072 — Extraction changes data topology, not semantic owner

The migration plan moves:

```text
owned tables/state
contracts
backlog
provider mappings
```

while preserving source context semantics.

---

# 172. Current data tree is evidence

Current `Infrastructure/Data` contains:

```text
Configurations
Converters
Interceptors
Migrations
Rls
ReadPorts
Projections
Events
Authz
Audit
Ops
...
ApplicationDbContext*
EfRequestDataSession
SystemContextScope
```

This is current implementation evidence.

Do not treat every directory as a permanent architecture module.

---

# 173. Infrastructure change classes

Typical:

```text
mapping/index/cache adapter
→ C2/C7 depending impact

schema/backfill
→ C4

RLS/auth provider
→ C6 (+ C4/C7)

new dependency/provider
→ C7/C5

persisted identity rename
→ C3/C4

destructive retention
→ C8
```

Obligations are cumulative.

---

# 174. ADR trigger

A new ADR may be required for:

```text
database/provider strategy
RLS/session architecture
new cache authority model
provider abstraction foundation
cross-context persistence model
migration foundation
new durable search/storage technology
```

Routine EF mapping change does not need an ADR if canonical rules determine it.

---

# 175. Persistence review checklist

```text
[ ] semantic owner
[ ] table/schema mapping
[ ] keys/FKs
[ ] nullability
[ ] converter
[ ] concurrency
[ ] index
[ ] tenant ownership
[ ] RLS
[ ] filter
[ ] existing-data migration
[ ] old/new compatibility
[ ] rollback/forward recovery
[ ] PostgreSQL tests
```

---

# 176. RLS review checklist

```text
[ ] table owner
[ ] Account/Workspace path
[ ] user/service/system access
[ ] bootstrap/full context
[ ] connection lifecycle
[ ] allowed tenant
[ ] denied tenant
[ ] pool reuse
[ ] indexes
[ ] background scope
```

---

# 177. Cache review checklist

```text
[ ] derived only
[ ] key scope
[ ] permission sensitivity
[ ] invalidation
[ ] version
[ ] serialization
[ ] outage fallback
[ ] stampede/capacity
[ ] tenant leak negative test
```

---

# 178. Provider review checklist

```text
[ ] inward Application contract
[ ] provider mapping
[ ] credentials
[ ] timeout
[ ] cancellation
[ ] retry class
[ ] rate limit
[ ] idempotency
[ ] unknown outcome
[ ] webhook/signature
[ ] reconciliation
[ ] logs/privacy
```

---

# 179. Migration review checklist

```text
[ ] current real data
[ ] target meaning
[ ] expand/contract
[ ] old/new binaries
[ ] backfill
[ ] invalid legacy rows
[ ] RLS/index/constraint
[ ] backlog/provider state
[ ] completion proof
[ ] destructive removal proof
[ ] recovery
```

---

# 180. Stop conditions

Stop Infrastructure implementation if:

- the owning product/context meaning is unresolved;
- mapping change is being used to redefine Domain behavior;
- a foreign context is being given direct write access;
- RLS is being disabled to make a query work;
- `IgnoreQueryFilters()` is proposed as a broad tenant bypass;
- migration only works on empty DB for an existing-data change;
- EF model drift is being suppressed;
- provider timeout is treated as definite failure without outcome analysis;
- cache becomes permission/source authority;
- public/provider DTO leaks inward;
- broker schema change ignores Platform identity/backlog;
- object/search/provider migration ignores existing durable external state;
- connection/session state can leak across pooled requests;
- a privileged system context is becoming the default path.

---

# 181. Executable evidence

Primary source:

```text
backend/src/Notrelix.Infrastructure
backend/src/Notrelix.Infrastructure/Data
backend/src/Notrelix.Infrastructure/Caching
backend/src/Notrelix.Infrastructure/Auth
backend/src/Notrelix.Infrastructure/Integrations
backend/src/Notrelix.Infrastructure/Storage
```

Primary tests:

```text
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Architecture.Tests
```

Focused:

```bash
cd backend
dotnet test tests/Notrelix.Infrastructure.Tests/Notrelix.Infrastructure.Tests.csproj
```

RLS/migration/production graph changes require PostgreSQL-realistic integration proof.

---

# 182. Related architecture

```text
backend-overview.md
domain-modeling.md
application-model.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Related ADR:

```text
../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md
```

Repository:

```text
../../../docs/delivery/migration-policy.md
../../../docs/operations/recovery-and-data-safety.md
../../../docs/infrastructure/environment-model.md
```

---

# 183. Non-responsibilities

Infrastructure does not own:

```text
aggregate boundary
product lifecycle
resource authorization semantics
Billing entitlement meaning
Automation rule meaning
HTTP route/public API version
frontend caching behavior
release cohort
RPO/RTO/SLO values
```

It implements the mechanisms those owners require.

---

# 184. Final Infrastructure rule

A healthy Infrastructure implementation can be explained as:

```text
inward Application/Domain contract
        ↓
provider-neutral capability boundary
        ↓
EF/PostgreSQL/Redis/provider/storage/search implementation
        ↓
tenant-safe mapping/session/RLS
        ↓
migration/retry/reconciliation mechanics
        ↓
stable semantic result back inward
```

with:

```text
no business rule invented from schema
no direct cross-context write shortcut
no RLS bypass for convenience
no EF model drift suppression
no cache/source confusion
no provider DTO leakage
no blind retry of unknown external outcome
no migration that ignores real old data
```

The objective is a replaceable outer mechanism layer that makes PostgreSQL/provider behavior reliable and secure without making those technologies the owners of Notrelix business meaning.
