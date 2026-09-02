# PR-WG-07 — Phase 10 WorkManagement resource-owner facts-provider handshake

Evidence record for PLAN §103–113 (WG-WM-001..009) and the P2-gate WorkManagement handshake (PLAN §231, TESTS §190
WG-TST-P2-CORE-006). Phase 10 proves that the Board slice of protected authorization flows through the canonical
Governance path via a WorkManagement-owned, transport-neutral resource-owner facts adapter.

## Baseline

- Repository root: `todo-app`; `backend/` hosts the solution (`backend.slnx`).
- Phase 9 closed at `1e789fc3` (docs) — Application.Tests 588, Integration.Tests 357, Architecture.Tests 410.
- SDK pinned `9.0.317` (`~/.dotnet/dotnet`, no rollback).

## Scope

Full production refactor (user directive) of the representative WorkManagement **Board** slice:

1. Introduce a transport-neutral **resource-authorization facts SPI** in `Application.Common.Security`.
2. Implement a **WorkManagement-owned facts adapter** exposing only resource-owned facts.
3. Remove raw `work.boards` / `work.board_members` reads from `AccessFactsQuery`; compose neutral Board facts in
   `PostgresAccessFactsProvider` feeding the existing `AccessPolicyEngine` (no second evaluator).
4. Route the WorkManagement side of `ResourceLocator` through the same adapter/boundary.
5. Preserve transaction / fail-closed semantics; no pre-transaction snapshot caching.
6. Real-pipeline integration proofs: allow, deny + no-commit, cross-tenant mismatch, restricted/hidden board.
7. Architecture guards for the boundary.
8. No schema migration.

Explicitly scoped OUT: Documents/Automation/Collaboration resource owners remain resolved by `ResourceLocator`
directly this phase (WG-WM-001, "do not start with every WorkManagement resource").

## Change

### New: neutral SPI

`backend/src/Notrelix.Application/Common/Security/IResourceAuthorizationFactsProvider.cs`

```csharp
public interface IResourceAuthorizationFactsProvider
{
    Task<ResourceAuthorizationFacts?> ResolveAsync(ResourceRef resource, Guid actorUserId,
        CancellationToken cancellationToken);
}

public sealed record ResourceAuthorizationFacts(
    Guid ResourceId, Guid AccountId, Guid WorkspaceId, bool Exists,
    string? Audience, string? MemberRole);
```

Contracts:

- returns **facts only**, never a policy/Allow/Deny decision;
- no dependency on EF, Npgsql, HTTP, gRPC, or broker (transport/persistence neutral contract location).

### New: WorkManagement-owned adapter

`backend/src/Notrelix.Infrastructure/Data/ReadPorts/WorkManagement/WorkManagementResourceAuthorizationFactsProvider.cs`

- Owns `IWorkManagementDbContext` and the `Board`/`BoardMember`/`BoardRole`/`BoardVisibility` knowledge.
- `work-management.board` → full facts: existence/lifecycle (`Exists = !IsDeleted && !IsArchived`), audience
  (`Visibility.ToString()`), actor→Board role (member `Role.ToString()`; `FirstOrDefault` so no member row is
  never conflated with `Guest`).
- Other work-management kinds (`board-group/field/view/item`, `label`, `checklist`, `checklist-item`) →
  ownership-scope only (account/workspace) via `FirstScopedAsync<T> where T : Entity, IWorkspaceScoped`;
  `ChecklistItem` via a Checklist join.
- Uses `.IgnoreQueryFilters()` (same trust boundary as the locator it replaces) because AccessControl still
  enforces membership; it identifies the owning tenant before RLS context is fully established.

### Modified: shared authorization SQL

`backend/src/Notrelix.Infrastructure/Data/Authz/AccessFactsQuery.cs`

- Removed the `work-management.board` cases that read `work.boards` / `work.board_members`.
- Cols 7/8 (resource audience / resource member role) now `NULL::text`; governance permission col preserved.

### Modified: facts provider composition

`backend/src/Notrelix.Infrastructure/Data/Authz/PostgresAccessFactsProvider.cs`

- New ctor parameter `IResourceAuthorizationFactsProvider resourceFactsProvider`.
- Board facts are resolved from the SPI **before the facts reader is opened** (avoids a nested command on the
  same ADO connection → `NpgsqlOperationInProgressException`), then composed over the SQL snapshot.
- Non-Board kinds fall back to the query columns. Still returns `AccessFacts`, not a decision.

### Modified: resource locator

`backend/src/Notrelix.Infrastructure/Services/ResourceLocator.cs`

- Replaced the direct `_workDb` work-management lookup with the SPI for the 8 work-management kinds.
- Removed WorkManagement private-persistence dependency from shared Infrastructure.
- Other owners (Documents/Collaboration/Governance/Automation) unchanged.

### DI

`backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs`

- `services.AddScoped<IResourceAuthorizationFactsProvider, WorkManagementResourceAuthorizationFactsProvider>();`

## WG-WM verdicts

| ID | Verdict | Evidence |
|---|---|---|
| WG-WM-001 | DONE | Scoped to representative Board slice only |
| WG-WM-002 | DONE | `work-management.board` category; WorkManagement owns declaration; Governance consumes facts |
| WG-WM-003 | DONE | First-slice action exercised via existing `ArchiveBoard` / `ManageBoard`; no speculative actions |
| WG-WM-004 | DONE | Neutral SPI + WorkManagement adapter; no `work.*` SQL in shared authz (`SharedAuthzSql_MustNotReadWorkManagementTablesDirectly`) |
| WG-WM-005 | DONE | Allow + commit proof |
| WG-WM-006 | DONE | Deny + no-commit proof |
| WG-WM-007 | DONE | Cross-tenant deny + no-mutation proof |
| WG-WM-008 | DONE | Fact boundary; `HasExplicitResourcePermission` stays Governance-owned; no role-name check leak |
| WG-WM-009 | DONE | No per-entity ACL; other kinds are scope-only |

## Tests

### New integration proofs (real Postgres pipeline)

`backend/tests/Notrelix.Integration.Tests/Integration/BoardHandshakeAuthorizationIntegrationTests.cs`
(composed production graph: `ExecutionContextBehavior` + `DataSessionBehavior` + `AccessControlBehavior` +
real `PostgresAccessFactsProvider` + real `WorkManagementResourceAuthorizationFactsProvider` + real
`ResourceLocator` + `RlsSessionContext` + `EfRequestDataSession`):

- `ArchiveBoard_WorkspaceBoardOwner_AllowedThroughHandshake_AndCommitted` — WG-WM-005 allow + durable commit.
- `ArchiveBoard_WorkspaceMemberWithoutBoardAuthority_DeniedBeforeCommit` — WG-WM-006 deny, board unchanged.
- `ArchiveBoard_PrivateBoardNonMember_HiddenAsNotFound` — restricted/hidden: non-member on a Private board →
  `NotFoundException`, no commit (fail closed).
- `ArchiveBoard_CrossTenantBoard_DeniedWithoutMutation` — WG-WM-007 Account-A actor → Board under Account-B →
  `ForbiddenException`, foreign board unarchived.

Support: `backend/tests/Notrelix.Testing.Application/Fakes/FakeResourceAuthorizationFactsProvider.cs`.

### Updated tests (new provider ctor / SPI registration)

- `Governance/PostgresAccessFactsProviderTests.cs`
- `Workspaces/Invitations/AcceptInvitationByIdIntegrationTests.cs`
- `Data/ExpectedVersionConcurrencyIntegrationTests.cs`
- `Integration/PipelineTelemetryIntegrationTests.cs` — now registers the **real** adapter (this suite routes a
  `work-management.board-item` through the SPI-backed locator; the fake returns null by default and broke locate).
- `Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`

### New architecture guards

`backend/tests/Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs`:

- `SharedAuthzSql_MustNotReadWorkManagementTablesDirectly` — no `work.boards` / `work.board_members` in `AccessFactsQuery`.
- `ResourceAuthorizationSpi_MustRemainTransportAndPersistenceNeutral` — SPI free of EF/Npgsql/HTTP/gRPC/broker/policy.
- `WorkManagementFactsAdapter_MustOwnTheWorkManagementDbContext`.
- `PostgresAccessFactsProvider_MustNotEmitPolicyDecisions`.

`IgnoreQueryFilters` allowlists (3 files) classify the adapter as `InfrastructureBootstrap` — same boundary as
`ResourceLocator.cs`.

## Suite evidence

| Suite | Result | Note |
|---|---|---|
| Domain.Tests | 2576 green | unaffected |
| Application.Tests | 588 green | SPI lives here; engine unchanged |
| Integration.Tests | **361 green** (357 → 361) | +4 Board-handshake proofs |
| Architecture.Tests | **414 green** (410 → 414) | +4 guards |
| Platform.Tests / API.Tests / Infrastructure.Tests | 147 / 260 / 134 green | no regression |
| backend.slnx build | 0 errors | SDK 9.0.317 |

## Contracts

No change to: product semantics, REST/OpenAPI, events/messages, realtime, package export, schema/migration,
authorization/tenant scope of any existing resource. The WorkManagement Board facts path moved from raw SQL to a
WorkManagement-owned adapter; behavior is equivalent for existing resource-scoped requests. No `NRX-*` weakened.

## Decisions

### D10-A — Neutral resource-owner facts SPI — DECIDED

`Application.Common.Security.IResourceAuthorizationFactsProvider` is the sanctioned transport-neutral
facts boundary. It returns source-owner facts only, never a policy decision (WG-WM-004, WG-WM-008).

### D10-B — WorkManagement owns the Board adapter — DECIDED

The adapter owns `IWorkManagementDbContext` and Board knowledge. Shared Infrastructure no longer depends on
WorkManagement private persistence.

### D10-C — `HasExplicitResourcePermission` stays Governance-owned — DECIDED

Not moved into the adapter. Firmly a Governance policy fact.

### D10-D — SPI resolved before facts-reader open — DECIDED

Avoids a nested ADO command (fixes `NpgsqlOperationInProgressException`) while preserving a single active reader.

### D10-E — No per-entity ACL — DECIDED

BoardItem and friends are scope-only this phase (WG-WM-009); Board remains the independent contract.

## Findings

- The SPI adapter uses `IgnoreQueryFilters()` to identify the owning tenant/workspace before RLS context is set
  — the same trust boundary the original locator used; cross-tenant safety is preserved because AccessControl
  still resolves the actor's workspace membership in the located workspace and denies a foreign actor.
- `PipelineTelemetryIntegrationTests` needed the real adapter (not the default-null fake) because its resource
  scoped request actually routes a `work-management.board-item` through the SPI-backed locator.

## Phase 10 exit

WorkManagement handshake proven (PLAN §113). **Phase 10 CLOSED.** Phase 11 (P3-B protected-slice verification +
staged P3 handoff, PLAN §114–115) may proceed.
