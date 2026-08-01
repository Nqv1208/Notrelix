# Notrelix Production Core Domain Freeze Certification

## Certified Identity

- **Certified Code SHA**: `2dd46e6dd2e3c7accae1d1df93e12147edf99e7c`
- **Certified Domain Tree SHA**: `5c7e2e8f8a7775f437d805713387f76571314063`
- **Evidence CI Run ID**: `N/A` (local certification run; working tree not pushed)
- **Evidence Record**: `backend/docs/domain/evidence/2dd46e6d-domain-freeze.md`
- **Certification Date**: 2026-08-01

> **Note**: FZ79A (determinism hardening) and FZ79B (snapshot schema v2) are certified as an
> uncommitted working-tree delta on top of `2dd46e6d`. The certificate must be re-stamped
> with the exact commit SHA after the delta is committed.

---

## Certification Gates

### Domain Build
| Command | Exit Code | Errors | Warnings |
|---|---|---:|---:|
| `dotnet build src/Notrelix.Domain/Notrelix.Domain.csproj -c Release -warnaserror` | 0 | 0 | 0 |

### Domain Tests
| Command | Exit Code | Passed | Failed | Skipped |
|---|---|---:|---:|---:|
| `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj -c Release` | 0 | 2653 | 0 | 0 |

### Freeze Gates
| Gate | Exit Code | Passed | Failed | Skipped |
|---|---|---:|---:|---:|
| Architecture (`Freeze.Architecture`) | 0 | 62 | 0 | 0 |
| Snapshots (`FreezeSnapshotTests` + `FreezeSnapshotSchemaTests`) | 0 | 11 | 0 | 0 |
| Mutation coverage (`MutationCoverageTests`) | 0 | 4 | 0 | 0 |
| Friend assembly (`DomainFriendAssemblyTests`) | 0 | 4 | 0 | 0 |
| Determinism (`DeterminismSemanticTests` + `DomainProjectCompilationTests`) | 0 | 10 | 0 | 0 |

### Full Solution Build
| Command | Exit Code | Errors | Warnings |
|---|---|---:|---:|
| `dotnet build backend.slnx -c Release --no-restore` | 0 | 0 | 0 |

---

## Determinism Claim

The determinism gate loads the actual `Notrelix.Domain.csproj` through MSBuildWorkspace,
fails on project/workspace/compilation failure, and semantically scans regular Domain
source documents for the approved forbidden ambient APIs. It does not claim mathematical
determinism; it is a fail-closed real-project analysis.

---

## Mutation Claim

Every public mutation on every Frozen aggregate maps by exact overload identity to at
least one executable test. The claim does not assert that every scenario exists globally.

---

## Friend-Assembly Claim

Domain exposes internals only to `Notrelix.Domain.Tests`. Application, Infrastructure,
API, and Platform use the public Domain contract.

---

## Capability Status

Counts calculated from `DomainCapabilityRegistry.Capabilities` (58 registrations).

### Frozen Production Core (41)

| # | Capability | Namespace Prefix |
|---|---|---|
| 1 | Common | `Notrelix.Domain.Common` |
| 2 | SharedKernel | `Notrelix.Domain.SharedKernel` |
| 3 | Accounts | `Notrelix.Domain.Accounts` |
| 4 | Identity | `Notrelix.Domain.Identity` |
| 5 | Workspaces | `Notrelix.Domain.Workspaces` |
| 6 | WorkManagement | `Notrelix.Domain.WorkManagement` |
| 7 | WorkManagement.Boards | `Notrelix.Domain.WorkManagement.Boards` |
| 8 | WorkManagement.BoardGroups | `Notrelix.Domain.WorkManagement.BoardGroups` |
| 9 | WorkManagement.Fields | `Notrelix.Domain.WorkManagement.Fields` |
| 10 | WorkManagement.Items | `Notrelix.Domain.WorkManagement.Items` |
| 11 | WorkManagement.Views | `Notrelix.Domain.WorkManagement.Views` |
| 12 | WorkManagement.Checklists | `Notrelix.Domain.WorkManagement.Checklists` |
| 13 | WorkManagement.Labels | `Notrelix.Domain.WorkManagement.Labels` |
| 14 | WorkManagement.Forms | `Notrelix.Domain.WorkManagement.Forms` |
| 15 | WorkManagement.Relations | `Notrelix.Domain.WorkManagement.Relations` |
| 16 | WorkManagement.Templates | `Notrelix.Domain.WorkManagement.Templates` |
| 17 | Documents | `Notrelix.Domain.Documents` |
| 18 | Collaboration | `Notrelix.Domain.Collaboration` |
| 19 | Collaboration.Attachments | `Notrelix.Domain.Collaboration.Attachments` |
| 20 | Collaboration.Comments | `Notrelix.Domain.Collaboration.Comments` |
| 21 | Collaboration.Mentions | `Notrelix.Domain.Collaboration.Mentions` |
| 22 | Collaboration.ReadStates | `Notrelix.Domain.Collaboration.ReadStates` |
| 23 | Collaboration.Rules | `Notrelix.Domain.Collaboration.Rules` |
| 24 | Governance | `Notrelix.Domain.Governance` |
| 25 | Governance.Permissions | `Notrelix.Domain.Governance.Permissions` |
| 26 | Governance.Policies | `Notrelix.Domain.Governance.Policies` |
| 27 | Governance.Roles | `Notrelix.Domain.Governance.Roles` |
| 28 | Governance.ShareLinks | `Notrelix.Domain.Governance.ShareLinks` |
| 29 | Governance.Templates | `Notrelix.Domain.Governance.Templates` |
| 30 | Automation | `Notrelix.Domain.Automation` |
| 31 | Automation.RulesEngine | `Notrelix.Domain.Automation.RulesEngine` |
| 32 | Automation.Rules | `Notrelix.Domain.Automation.Rules` |
| 33 | Integrations | `Notrelix.Domain.Integrations` |
| 34 | Integrations.Rules | `Notrelix.Domain.Integrations.Rules` |
| 35 | Integrations.Connections | `Notrelix.Domain.Integrations.Connections` |
| 36 | Billing | `Notrelix.Domain.Billing` |
| 37 | Analytics | `Notrelix.Domain.Analytics` |
| 38 | Analytics.Dashboards | `Notrelix.Domain.Analytics.Dashboards` |
| 39 | Analytics.Widgets | `Notrelix.Domain.Analytics.Widgets` |
| 40 | Analytics.Snapshots | `Notrelix.Domain.Analytics.Snapshots` |
| 41 | Analytics.Rules | `Notrelix.Domain.Analytics.Rules` |

### Stabilizing Capabilities (7)

These capabilities have unresolved product semantics and do not block the production-core freeze.

| # | Capability | Namespace Prefix | Reason |
|---|---|---|---|
| 1 | Automation.Scheduled | `Notrelix.Domain.Automation.Scheduled` | Inconsistent Version increments, missing Resume/Cancel events |
| 2 | Automation.Templates | `Notrelix.Domain.Automation.Templates` | Missing no-op detection, Version increments |
| 3 | Collaboration.Reactions | `Notrelix.Domain.Collaboration.Reactions` | Query callback in Domain, unclear removal contract |
| 4 | Collaboration.Watchers | `Notrelix.Domain.Collaboration.Watchers` | Unwatch not idempotent, unclear lifecycle |
| 5 | Integrations.Calendar | `Notrelix.Domain.Integrations.Calendar` | Delete/Restore state mismatch |
| 6 | Integrations.Webhooks | `Notrelix.Domain.Integrations.Webhooks` | Delete deactivates, no restore lifecycle |
| 7 | Integrations.Sync | `Notrelix.Domain.Integrations.Sync` | Related to Calendar sync |

### Experimental Capabilities (10)

| # | Capability | Namespace Prefix |
|---|---|---|
| 1 | WorkManagement.Formulas | `Notrelix.Domain.WorkManagement.Formulas` |
| 2 | WorkManagement.Rollups | `Notrelix.Domain.WorkManagement.Rollups` |
| 3 | WorkManagement.Workload | `Notrelix.Domain.WorkManagement.Workload` |
| 4 | WorkManagement.Approvals | `Notrelix.Domain.WorkManagement.Approvals` |
| 5 | Collaboration.Presence | `Notrelix.Domain.Collaboration.Presence` |
| 6 | Automation.Triggers | `Notrelix.Domain.Automation.Triggers` |
| 7 | Automation.Actions | `Notrelix.Domain.Automation.Actions` |
| 8 | Automation.Conditions | `Notrelix.Domain.Automation.Conditions` |
| 9 | Automation.Executions | `Notrelix.Domain.Automation.Executions` |
| 10 | Automation.Agents | `Notrelix.Domain.Automation.Agents` |

---

## Snapshot Schemas

### Frozen Domain Public API
**Schema version**: `2`

**Columns**: `FrozenApi|Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|ReturnOrPropertyType|ParametersOrAccessor`

- constructor → `System.Void` + parameters
- method → return type + parameters
- property → property type + readonly/readwrite

**File**: `tests/Notrelix.Domain.Tests/Snapshots/FrozenDomainPublicApi.approved.txt`

### Domain Events (Frozen only)
**Schema version**: `1`
**Columns**: `DomainEvents|LogicalName|Version|ClrType|Scope|PropertyName|PropertyType|IsNullable`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/DomainEvents.approved.txt`

### Enums (Frozen only)
**Schema version**: `2`
**Columns**: `Enums|EnumType|UnderlyingType|MemberName|NumericValue`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/Enums.approved.txt`

### Rule Codes
**Schema version**: `1`
**Columns**: `RuleCodes|Code|OwnerContext|ConstantName`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/RuleCodes.approved.txt`

---

## Negative Proofs

Performed temporarily and reverted before delivery; full table in the evidence record:

- Ambient `DateTimeOffset.UtcNow` in a scratch Domain file → determinism gate fails with
  relative path, line, and `System.DateTimeOffset.UtcNow`.
- Missing backend root → locator throws listing inspected paths (permanent test).
- Invalid in-memory syntax → `EnsureCompilationHasNoErrors` throws with the diagnostic
  (permanent test).
- Removed output column → schema test fails and prints the malformed row.
- Altered approved row → snapshot comparison fails and the approved file is not rewritten.

---

## Mutation Coverage Gate

**Status**: PASSING

- Exact mutation-to-test mapping enforced via `[CoversMutation]` with nameof + Type[]
- Every public mutation on every Frozen aggregate maps to at least one executable test
- Compiler-safe method resolution via reflection

---

## Deletion Policy Architecture Gate

**All DeletionPolicyArchitectureTests pass:**

- Every aggregate root has exactly one deletion policy
- Non-recoverable types use `AggregateRoot`
- Recoverable types use `SoftDeletableAggregateRoot`
- Business status is independent from deletion state
- Delete/Restore never modify business status

---

## Critical Invariant Suites

The following high-risk operations have direct scenario tests:

- **AutomationRule Delete/Restore**: Status preserved through deletion lifecycle
- **BoardRelation Delete/Restore**: Status preserved, Deleted enum retired
- **BoardField SetDefaultValue**: Canonical validation always runs first

---

## Known Non-Domain Responsibilities

These are complementary Infrastructure controls, NOT Domain proof:

| Concern | Infrastructure Mechanism |
|---|---|
| Soft-delete global query filter | EF Core `HasQueryFilter` |
| Tenant isolation | EF Core global query filters |
| Concurrency | EF Core `Timestamp` / row version |
| Outbox dispatch | Background worker + outbox table |
| Search indexing | Background jobs (projection) |
| Event publishing | Message broker integration |
| API authorization | ASP.NET Core policies |
| Cache invalidation | Redis + cache-aside |

---

## Compatibility Change Procedure

A change to a Frozen contract requires:

1. Intentional design review
2. Behavior test update
3. Mutation coverage update (`[CoversMutation]`)
4. Snapshot diff review (`FreezeSnapshotTests`)
5. Event version review (if domain event changed)
6. Caller migration (Application / Infrastructure)
7. Full certification gate (build + test + snapshots)

Adding a new capability:
- Starts `Stabilizing` or `Experimental`
- Must never inherit `Frozen` by namespace default
- Register in `DomainCapabilityRegistry`
- Register deletion policy in `DeletionPolicyRegistry`

---

## Certification Statement

> Notrelix production-core Domain is Frozen at `2dd46e6dd2e3c7accae1d1df93e12147edf99e7c`.
>
> The immutable Domain tree is `5c7e2e8f8a7775f437d805713387f76571314063`.
>
> Frozen contracts are protected by behavior tests, exact mutation-to-test mapping,
> effective-maturity snapshots, production-friend-assembly restrictions, and fail-closed
> real-project determinism analysis.
>
> **Frozen production core**: 41 capabilities certified.
> **Stabilizing**: 7 capabilities isolated with unresolved semantics.
> **Experimental**: 10 capabilities remain isolated.
>
> Stabilizing and Experimental capabilities remain outside the Frozen compatibility
> commitment. Contract snapshots and architecture gates pass without regeneration or
> skipped tests. Negative proofs confirm the fail-closed gates fail on ambient
> nondeterminism, malformed rows, and snapshot drift.
