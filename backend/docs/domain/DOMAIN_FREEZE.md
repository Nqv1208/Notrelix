# Notrelix Domain Freeze Certification

## Certified Commit

**HEAD SHA**: `eb0e4a2d828a9fdb7eff4306aeaa373f7efe010b`
**Certification Date**: 2026-07-31

---

## Certification Gates

### Domain Build
| Command | Exit Code | Errors | Warnings |
|---|---|---|---|
| `dotnet build src/Notrelix.Domain/Notrelix.Domain.csproj -c Release -warnaserror` | 0 | 0 | 0 |

### Domain Tests
| Command | Exit Code | Passed | Failed | Skipped |
|---|---|---|---|---|
| `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj -c Release` | 0 | 2610 | 0 | 0 |

### Full Solution Build
| Command | Exit Code | Projects | Errors | Warnings |
|---|---|---|---|---|
| `dotnet build backend/Notrelix.sln -c Release` | 0 | 16 | 0 | 0 |

> **Note**: Full solution build with `-warnaserror` fails due to pre-existing warnings in `Notrelix.Application` and `Notrelix.Application.Tests` (CS1998, CS8604, CS8602). These are outside the Domain layer and do not affect the freeze certification.

---

## Capability Status

### Frozen Capabilities

| # | Capability | Namespace Prefix | Aggregate Roots |
|---|---|---|---|
| 1 | Accounts Core | `Notrelix.Domain.Accounts` | `Account`, `AccountMember`, `AccountDomain`, `AccountIdentityProvider`, `AccountInvitation`, `ScimDirectory`, `WorkspaceRoute` |
| 2 | Identity | `Notrelix.Domain.Identity` | `User`, `UserProfile`, `UserSession`, `UserSecuritySettings`, `UserMfaMethod`, `ApiToken`, `EmailVerificationToken`, `PasswordResetToken`, `UserLoginAttempt` |
| 3 | Workspaces | `Notrelix.Domain.Workspaces` | `Workspace`, `WorkspaceMember`, `WorkspaceInvitation`, `Space`, `Team` |
| 4 | WorkManagement | `Notrelix.Domain.WorkManagement` | `Board`, `BoardField`, `BoardGroup`, `BoardItem`, `BoardView`, `BoardViewUserPreference`, `SavedFilter`, `Checklist`, `Form`, `Label`, `BoardRelation`, `ApprovalRequest`, `BoardTemplate`, `ItemTemplate`, `TimeTrackingEntry` |
| 5 | Documents | `Notrelix.Domain.Documents` | `Page`, `Block`, `ResourceLink`, `DocumentVersion`, `PageTemplate` |
| 6 | Collaboration | `Notrelix.Domain.Collaboration` | `Comment`, `Reaction`, `ResourceWatcher`, `Attachment` |
| 7 | Automation Frozen | `Notrelix.Domain.Automation.Rules`, `.RulesEngine`, `.Scheduled`, `.Templates` | `AutomationRule`, `ScheduledJob`, `AutomationTemplate` |
| 8 | Integrations | `Notrelix.Domain.Integrations` | `IntegrationConnection`, `CalendarIntegration`, `WebhookSubscription`, `WebhookDelivery`, `InboundWebhookEvent` |
| 9 | Billing | `Notrelix.Domain.Billing` | `Plan`, `BillingCustomer`, `Subscription`, `Invoice`, `PaymentMethod`, `Entitlement`, `UsageMetric`, `WorkspaceFeatureUsage`, `BillingEvent` |
| 10 | Governance | `Notrelix.Domain.Governance` | `PermissionTemplate`, `PermissionRule`, `ResourcePermission`, `CustomRole`, `ShareLink` |
| 11 | Analytics | `Notrelix.Domain.Analytics` | `Dashboard`, `DashboardSource`, `ReportingSnapshot` |

### Experimental Capabilities

| # | Capability | Namespace Prefix | Aggregate Roots |
|---|---|---|---|
| 1 | Automation Experimental | `Notrelix.Domain.Automation.Triggers`, `.Actions`, `.Conditions`, `.Executions`, `.Agents` | `AiAgent`, `AiAgentRun`, `AutomationExecution` |
| 2 | Collaboration Presence | `Notrelix.Domain.Collaboration.Presence` | `PresenceSession` (entity, not agg. root) |

### Stabilizing Capabilities

None. All capabilities classified as either `Frozen` or `Experimental`.

---

## Aggregate Scopes

| Scope | Aggregate Roots |
|---|---|
| **Global** | `User`, `UserProfile`, `UserSession`, `UserSecuritySettings`, `UserMfaMethod`, `EmailVerificationToken`, `PasswordResetToken`, `UserLoginAttempt`, `Plan`, `BillingEvent`, `BoardTemplate`, `PageTemplate`, `InboundWebhookEvent`, `AutomationTemplate` |
| **Hybrid** | `PermissionTemplate` |
| **Account** | `Account`, `AccountMember`, `AccountDomain`, `AccountIdentityProvider`, `AccountInvitation`, `ScimDirectory`, `WorkspaceRoute`, `BillingCustomer`, `Subscription`, `Invoice`, `Entitlement` |
| **Workspace** | `Workspace`, `WorkspaceMember`, `WorkspaceInvitation`, `Space`, `Team`, `Board`, `BoardField`, `BoardGroup`, `BoardItem`, `BoardView`, `BoardViewUserPreference`, `SavedFilter`, `Checklist`, `Form`, `Label`, `BoardRelation`, `ApprovalRequest`, `ItemTemplate`, `TimeTrackingEntry`, `Page`, `Block`, `ResourceLink`, `DocumentVersion`, `Comment`, `Reaction`, `ResourceWatcher`, `Attachment`, `IntegrationConnection`, `CalendarIntegration`, `WebhookSubscription`, `WebhookDelivery`, `PermissionRule`, `ResourcePermission`, `CustomRole`, `ShareLink`, `AutomationRule`, `ScheduledJob`, `AiAgent`, `AiAgentRun`, `AutomationExecution`, `UsageMetric`, `WorkspaceFeatureUsage`, `PaymentMethod`, `ApiToken`, `Dashboard`, `DashboardSource`, `ReportingSnapshot` |

---

## Snapshot Schemas

### Frozen Domain Public API
**Schema**: `FrozenApi|Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|ReturnType|Parameters`
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

**Status**: PASSING (unskipped)

- 4 mutation coverage tests enforce:
  - Every mutation method on every frozen aggregate has `[CoversMutation]`
  - Signatures reference methods that exist on the target type (fuzzy match: name + parameter count)
  - No duplicate signatures per aggregate
  - Attribute only on `[Fact]` or `[Theory]` methods
- All Frozen aggregates have full mutation coverage with Event, Invalid, NoOp, Valid, and Audit variants
- Non-mutation query methods excluded via `NonMutationMethodRegistry`

---

## Deletion Policy Architecture Gate

**All 7 DeletionPolicyArchitectureTests pass:**

- `DeletionPolicy_EveryAggregateRoot_HasExactlyOnePolicy` — all 73 aggregate roots classified
- `DeletionPolicy_NonRecoverable_MustNotDeriveSoftDeletable` — non-recoverable types use `AggregateRoot`
- `DeletionPolicy_RecoverableDelete_MustDeriveSoftDeletable` — recoverable types use `SoftDeletableAggregateRoot`
- `DeletionPolicy_OwnedRemoval_MustNotExposePublicDeleteOrRestore` — owned-removal types use business status
- `DeletionPolicy_AppendOnly_MustNotDeriveSoftDeletable` — append-only types are plain `AggregateRoot`
- `DeletionPolicy_BusinessTombstone_MustDeriveSoftDeletable` — tombstone types use `SoftDeletableAggregateRoot`
- `DeletionPolicy_ConsistentInheritance_AcrossAllPolicies` — all policies consistent with base type

---

## Architecture Gates

**All architecture tests pass:**

- Domain capability registry validation (every public type resolves to a capability)
- Aggregate scope resolution (every aggregate maps to exactly one scope)
- Frozen type snapshot determinism (public API, domain events, enums, rule codes)
- Cross-context reference constraints
- State encapsulation tests
- System-actor policy tests

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

> Notrelix Domain production surface is frozen at HEAD.
> All production capabilities are classified Frozen.
> Experimental capabilities are isolated and excluded from Frozen compatibility snapshots.
> Every Frozen aggregate mutation has explicit executable scenario coverage.
> Contract snapshots and architecture gates pass without regeneration or skipped tests.
> All 73 aggregate roots have explicit deletion policies enforced by architecture tests.
> Non-recoverable types have been cleaned of `SoftDeletableAggregateRoot` inheritance and use business-status lifecycle.
> Business status is independent from deletion state; Delete/Restore never modify business status.
> AutomationRule enforces activation invariant: Active ⇒ valid configuration ⇒ valid trigger ⇒ valid action.
> BoardRelation provides pure Domain rules for duplicate and cardinality enforcement.
> Capability registry uses Exact/Subtree matching for fail-closed classification.
