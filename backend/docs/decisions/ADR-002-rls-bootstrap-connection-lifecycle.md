---
document_id: ADR-002
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-security
  - backend-tenancy
  - backend-rls
  - backend-application
  - backend-infrastructure
evidence:
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/application-model.md
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsPolicyApplier.cs
  - backend/src/Notrelix.Application/Common/Behaviors/DbRequestScopeBehavior.cs
  - backend/tests/Notrelix.Integration.Tests/
review_on:
  - decision-superseded
  - rls-bootstrap-change
  - rls-session-model-change
  - database-connection-lifecycle-change
  - tenant-bootstrap-change
---

# ADR-002: RLS Bootstrap Connection Lifecycle

## ID

`ADR-002`

## Status

Accepted

## Date

`2026-08-11`

Historical note:

```text
The original ADR did not contain an explicit Date section.
This date is recovered from the Git history entry that introduced/preserved
the ADR in the current documentation refoundation commit.
```

## Owners

Current stewardship:

- `backend-architecture`

Historical authorship/owner:

```text
Not recorded explicitly in the original ADR.
```

This normalization does not infer historical authorship from current stewardship.

---

## Context

Notrelix uses PostgreSQL Row-Level Security as a persistence defense-in-depth mechanism.

RLS policies depend on trusted database session context such as:

```text
app.current_user_id
app.current_account_id
app.current_workspace_id
app.request_scope
app.correlation_id
```

The original problem arises before the full Account/Workspace tenant scope is known.

`TenantBootstrapBehavior` runs in the outer Application pipeline because it must resolve authoritative Workspace/Account access information before the normal tenant-scoped transaction can execute.

At bootstrap time:

```text
authenticated UserId
→ known

AccountId
→ not yet known

WorkspaceId
→ not yet known
```

The bootstrap query itself still reaches PostgreSQL tables protected by RLS.

The original ADR records an important distinction:

```text
IgnoreQueryFilters()
→ bypasses EF Core global query filters

IgnoreQueryFilters()
≠ bypasses PostgreSQL RLS
```

Without a minimal PostgreSQL session context, RLS functions evaluating `current_user_id` can see no usable user identity and deny the bootstrap query that is required to resolve the remaining scope.

The architecture therefore needed a secure bridge between:

```text
authenticated user only
        ↓
bootstrap query
        ↓
resolved Account/Workspace
        ↓
full tenant-scoped transaction
```

without granting a broad system bypass.

---

## Decision

The accepted decision is that the tenant bootstrap persistence path owns the physical connection lifecycle required for the bootstrap query and sets **minimal RLS session context** before that query.

The bootstrap path uses the same scoped `ApplicationDbContext` / physical `NpgsqlConnection` that the later request DB scope will use.

### Step 1 — Obtain the scoped physical connection

The bootstrap persistence implementation obtains the physical `NpgsqlConnection` from the scoped `ApplicationDbContext`.

It does not create an unrelated second connection for the same request flow.

### Step 2 — Open the connection when necessary

If the connection is not already open, the bootstrap path opens it before issuing session-context commands or the bootstrap query.

EF Core can then reuse the already-open connection for the remainder of the scoped request lifecycle.

### Step 3 — Set minimal bootstrap RLS context

Before the bootstrap query, set only the session values known and required at this stage:

```text
app.current_user_id
→ authenticated UserId

app.request_scope
→ "app"

app.correlation_id
→ current correlation/Activity identity
```

Do **not** set:

```text
app.current_account_id
app.current_workspace_id
```

to fabricated/default tenant values because those facts are not yet known.

### Step 4 — Execute bootstrap authorization/scope lookup

The bootstrap query runs on the same open physical connection under the minimal user-level RLS context.

The accepted decision allows the query to bypass EF Core query filters where required while still being constrained by PostgreSQL RLS.

Permission/resource-scope evaluation that depends on the same bootstrap session context runs on that connection lifecycle.

### Step 5 — Apply full request RLS context later

After authoritative tenant/resource scope is resolved, the normal database request scope applies the full context.

Current architecture delegates this through the request data-session boundary; the current Infrastructure `RlsSessionContext` applies values including:

```text
UserId
AccountId
WorkspaceId
request scope
correlation ID
```

using transaction-local `set_config(..., true)` semantics.

### Step 6 — Continue the normal request transaction

The accepted lifecycle is conceptually:

```text
open connection
        ↓
set minimal bootstrap session context
        ↓
bootstrap query / permission scope resolution
        ↓
begin normal DB request scope / transaction as required
        ↓
apply full transaction-local RLS context
        ↓
authorized Application/Domain work
        ↓
commit / rollback
        ↓
connection returned to pool
```

Npgsql connection-pool reset semantics are part of the safety assumption preventing bootstrap session state from leaking to a later request.

---

## Decision invariants

### Same request connection lifecycle

Bootstrap session state and later full request scope are designed around the same scoped physical connection lifecycle.

A change that introduces a different connection between those phases can invalidate this decision.

### Minimal bootstrap privilege

Bootstrap context includes only information known and required to resolve the remaining tenant scope.

It is not a hidden system context.

### No fake tenant values

Unknown Account/Workspace IDs are not populated with arbitrary values merely to satisfy an RLS helper.

### Full context overwrites bootstrap context

Once authoritative tenant scope is known, the normal request DB scope applies the complete context required for tenant-protected SQL.

### Query-filter bypass is not RLS bypass

EF filtering and PostgreSQL RLS remain independent protections.

### Connection state must not leak

Session-level bootstrap state must be cleaned/reset when the physical connection returns to the pool.

---

## Alternatives Considered

### Alternative A — Add a separate `IRlsContextInitializer`

The original ADR explicitly records this alternative as rejected.

Conceptually:

```text
Tenant bootstrap
→ call a new RLS initialization abstraction
→ initialize the connection/session
```

#### Potential benefit

- could separate RLS initialization behind an additional abstraction.

#### Costs / reasons rejected

The original ADR records that no additional abstraction was needed.

The tenant bootstrap persistence implementation already owns the bootstrap query and therefore directly owns the physical connection lifecycle needed to execute it safely.

Adding another abstraction would separate responsibility from the code that must coordinate:

```text
open physical connection
set minimal context
execute bootstrap query
```

without adding a distinct semantic owner.

### Other alternatives

```text
Not recorded in the original ADR.
```

This normalized ADR does not invent alternatives such as:

```text
disable RLS for bootstrap
use a separate privileged connection
move bootstrap after full transaction
use a globally unrestricted system role
```

because those were not documented as historical alternatives in the original record.

---

## Consequences

### Positive

The original ADR records these benefits:

- bootstrap queries receive `app.current_user_id`, allowing user-level RLS policy evaluation;
- the connection is explicitly open before the bootstrap query;
- the normal request later applies the complete Account/Workspace context;
- no additional `IRlsContextInitializer` abstraction is required;
- connection ownership stays with the persistence component executing the bootstrap query.

### Security consequence

The bootstrap path remains:

```text
user-scoped
```

rather than:

```text
tenantless/unrestricted
```

even before Account/Workspace IDs are known.

### Operational consequence

The accepted model depends on correct physical-connection lifecycle and pool cleanup.

Connection reuse/reset is therefore a security property and must be tested with multiple request/tenant scopes.

### Complexity consequence

The solution deliberately uses two context phases:

```text
minimal session context
→ full transaction-local context
```

rather than pretending all tenant facts are known before bootstrap.

---

## Compatibility / Migration

The original ADR did not record a persisted-schema migration.

The decision affects runtime security/session behavior.

Compatibility requirements include:

```text
RLS helper functions/policies must understand the bootstrap variables
bootstrap persistence must use the expected scoped connection lifecycle
full request scope must later apply complete context
old/new runtimes must not disagree on required session variable semantics during rollout
```

A future change to any of these can require a new migration/ADR:

```text
session variable names/meaning
connection lifecycle
bootstrap trust model
RLS policy dependency
full-context application model
system/worker request-scope semantics
```

If policy rollout starts requiring a new session variable, the runtime that sets that variable must be compatible before or during the policy deployment according to the migration plan.

---

## Evidence

### Canonical current architecture

- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/infrastructure-and-data.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/operations/configuration-and-runtime.md`
- `backend/docs/operations/migrations-and-data-change.md`

### Current source

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs`
  - applies full RLS session context;
  - requires AccountId for non-system request context;
  - sets user/account/workspace/request-scope/correlation values transaction-locally.
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsPolicyApplier.cs`
  - uses the physical `NpgsqlConnection` from the scoped `ApplicationDbContext`;
  - demonstrates explicit connection open/use for RLS administration.
- `backend/src/Notrelix.Application/Common/Behaviors/DbRequestScopeBehavior.cs`
  - classifies the request;
  - refuses a global request that claims tenant RLS;
  - enters the request data-session boundary with tenant/resource-scope requirements.
- current tenant-bootstrap persistence implementation referenced by the original ADR.

### Tests / gates

Current proof should include, where applicable:

```text
bootstrap user-level RLS access
full tenant-scope application
tenant A allowed
tenant B denied
connection pool reuse without context leakage
production request-data-session/RLS behavior
```

Primary current test projects:

- `backend/tests/Notrelix.Infrastructure.Tests/`
- `backend/tests/Notrelix.Integration.Tests/`
- `backend/tests/Notrelix.Architecture.Tests/`

Current Integration foundation contains explicit tenant/RLS suites including:

```text
TenantIsolationTests
CrossTenantIsolationTests
RlsRuntimeEnforcementTests
```

as executable security evidence.

---

## Supersedes

`None`

The original ADR does not record a prior ADR superseded by this decision.

---

## Superseded By

`None`

Current registry status remains:

```text
Accepted
```

No newer backend ADR currently supersedes ADR-002.

---

## Historical normalization note

This file was normalized to the current ADR schema without changing the accepted decision.

The normalization adds:

```text
metadata
ID
recoverable date
current stewardship
explicit alternatives section
compatibility/migration section
current architecture/source/test evidence
supersession metadata
```

while preserving the original core decision:

```text
bootstrap uses minimal user-level RLS session context
on the same scoped physical connection
before later full tenant RLS context is applied.
```

Unknown historical authorship and unrecorded alternatives remain explicitly unknown.

---

## Decision-change trigger

A superseding ADR should be considered if Notrelix materially changes:

```text
the bootstrap security/trust model
the required bootstrap RLS variables
the same-connection lifecycle assumption
the RLS session-context architecture
the transaction-local/full-context application model
the relationship between bootstrap lookup and Application authorization
```

Routine implementation fixes that preserve those semantics do not require a new ADR.
