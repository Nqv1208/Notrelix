# Notrelix Production Core Domain Freeze Certification

## Certified Commit

**Certified Code SHA**: `a0ed08194dd1c5232f755704070c4a8049f8e3a5`
**Certification Date**: 2026-08-01

---

## Certification Gates

### Domain Build
| Command | Exit Code | Errors | Warnings |
|---|---|---|---|
| `dotnet build src/Notrelix.Domain/Notrelix.Domain.csproj -c Release -warnaserror` | 0 | 0 | 0 |

### Domain Tests
| Command | Exit Code | Passed | Failed | Skipped |
|---|---|---|---|---|
| `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj -c Release` | 0 | 2639 | 0 | 0 |

### Full Solution Build
| Command | Exit Code | Projects | Errors | Warnings |
|---|---|---|---|---|
| `dotnet build backend/Notrelix.sln -c Release` | 0 | 16 | 0 | 0 |

> **Note**: Full solution build with `-warnaserror` fails due to pre-existing warnings in `Notrelix.Application` and `Notrelix.Application.Tests` (CS1998, CS8604, CS8602). These are outside the Domain layer and do not affect the freeze certification.

---

## Capability Status

### Frozen Production Core

| # | Capability | Namespace Prefix |
|---|---|---|
| 1 | Common | `Notrelix.Domain.Common` |
| 2 | SharedKernel | `Notrelix.Domain.SharedKernel` |
| 3 | Accounts | `Notrelix.Domain.Accounts` |
| 4 | Identity | `Notrelix.Domain.Identity` |
| 5 | Workspaces | `Notrelix.Domain.Workspaces` |
| 6 | WorkManagement.Boards | `Notrelix.Domain.WorkManagement.Boards` |
| 7 | WorkManagement.BoardGroups | `Notrelix.Domain.WorkManagement.BoardGroups` |
| 8 | WorkManagement.Fields | `Notrelix.Domain.WorkManagement.Fields` |
| 9 | WorkManagement.Items | `Notrelix.Domain.WorkManagement.Items` |
| 10 | WorkManagement.Views | `Notrelix.Domain.WorkManagement.Views` |
| 11 | WorkManagement.Checklists | `Notrelix.Domain.WorkManagement.Checklists` |
| 12 | WorkManagement.Labels | `Notrelix.Domain.WorkManagement.Labels` |
| 13 | WorkManagement.Forms | `Notrelix.Domain.WorkManagement.Forms` |
| 14 | WorkManagement.Relations | `Notrelix.Domain.WorkManagement.Relations` |
| 15 | WorkManagement.Templates | `Notrelix.Domain.WorkManagement.Templates` |
| 16 | Documents | `Notrelix.Domain.Documents` |
| 17 | Collaboration.Attachments | `Notrelix.Domain.Collaboration.Attachments` |
| 18 | Collaboration.Comments | `Notrelix.Domain.Collaboration.Comments` |
| 19 | Collaboration.Mentions | `Notrelix.Domain.Collaboration.Mentions` |
| 20 | Collaboration.ReadStates | `Notrelix.Domain.Collaboration.ReadStates` |
| 21 | Collaboration.Rules | `Notrelix.Domain.Collaboration.Rules` |
| 22 | Governance | `Notrelix.Domain.Governance` |
| 23 | Automation.Rules | `Notrelix.Domain.Automation.Rules` |
| 24 | Automation.RulesEngine | `Notrelix.Domain.Automation.RulesEngine` |
| 25 | Integrations.Connections | `Notrelix.Domain.Integrations.Connections` |
| 26 | Billing | `Notrelix.Domain.Billing` |
| 27 | Analytics | `Notrelix.Domain.Analytics` |

### Stabilizing Capabilities

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

### Experimental Capabilities

| # | Capability | Namespace Prefix |
|---|---|---|
| 1 | WorkManagement.Formulas | `Notrelix.Domain.WorkManagement.Formulas` |
| 2 | WorkManagement.Rollups | `Notrelix.Domain.WorkManagement.Rollups` |
| 3 | WorkManagement.Workload | `Notrelix.Domain.WorkManagement.Workload` |
| 4 | WorkManagement.Approvals | `Notrelix.Domain.WorkManagement.Approvals` |
| 5 | Automation.Triggers | `Notrelix.Domain.Automation.Triggers` |
| 6 | Automation.Actions | `Notrelix.Domain.Automation.Actions` |
| 7 | Automation.Conditions | `Notrelix.Domain.Automation.Conditions` |
| 8 | Automation.Executions | `Notrelix.Domain.Automation.Executions` |
| 9 | Automation.Agents | `Notrelix.Domain.Automation.Agents` |
| 10 | Collaboration.Presence | `Notrelix.Domain.Collaboration.Presence` |

---

## Snapshot Schemas

### Frozen Domain Public API
**Schema**: `FrozenApi|Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|Parameters`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/FrozenDomainPublicApi.approved.txt`

### Domain Events (Frozen only)
**Schema**: `DomainEvents|LogicalName|Version|ClrType|Scope|PropertyName|PropertyType|IsNullable`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/DomainEvents.approved.txt`

### Enums (Frozen only)
**Schema**: `Enums|EnumType|UnderlyingType|MemberName|NumericValue`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/Enums.approved.txt`

### Rule Codes
**Schema**: `RuleCodes|Code|OwnerContext|ConstantName`
**File**: `tests/Notrelix.Domain.Tests/Snapshots/RuleCodes.approved.txt`

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

> Notrelix production-core Domain is Frozen at `a0ed08194dd1c5232f755704070c4a8049f8e3a5`.
>
> **Frozen production core**: 27 capabilities certified.
> **Stabilizing**: 7 capabilities isolated with unresolved semantics.
> **Experimental**: 10 capabilities remain isolated.
>
> All production-core invariants are correct.
> Frozen public APIs, enums, rule codes, and event contracts are protected.
> Frozen capabilities have executable tests for their public mutations.
> Stabilizing capabilities are isolated and do not block the production-core freeze.
> Contract snapshots and architecture gates pass without regeneration or skipped tests.
> Business status is independent from deletion state.
> AutomationRule enforces activation invariant.
> BoardRelation provides pure Domain rules for duplicate and cardinality enforcement.
> Capability registry uses Exact/Subtree matching for fail-closed classification.
> Frozen snapshots use effective registry status (exclude Stabilizing).
