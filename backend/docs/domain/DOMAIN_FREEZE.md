# Notrelix Domain Freeze Certification

## Certified Commit

**HEAD SHA**: `b1bb88ffe2fff2a4b8015ec709258679bf91a1d8`
**Certification Date**: 2026-07-29

---

## Certification Gates

### Domain Build
| Command | Exit Code | Errors | Warnings |
|---|---|---|---|
| `dotnet build src/Notrelix.Domain/Notrelix.Domain.csproj -c Release -warnaserror` | 0 | 0 | 0 |

### Domain Tests
| Command | Exit Code | Passed | Failed | Skipped |
|---|---|---|---|---|
| `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj -c Release` | 0 | 2616 | 0 | 0 |

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
| 1 | Accounts Core | `Notrelix.Domain.Accounts` | `Account`, `AccountMember`, `AccountInvitation`, `AccountIdentityProvider`, `ScimDirectory`, `ScimSyncRun` |
| 2 | Identity | `Notrelix.Domain.Identity` | `User`, `UserLogin`, `UserSession`, `UserSecuritySettings`, `UserProfile`, `UserMfaMethod`, `ApiToken`, `EmailVerificationToken`, `PasswordResetToken` |
| 3 | Workspaces | `Notrelix.Domain.Workspaces` | `Workspace`, `WorkspaceMember`, `WorkspaceInvitation`, `Space`, `Team`, `TeamMember` |
| 4 | WorkManagement | `Notrelix.Domain.WorkManagement` | `Board`, `BoardField`, `BoardGroup`, `BoardItem`, `BoardView`, `BoardViewUserPreference`, `SavedFilter`, `Checklist`, `Form`, `Label`, `BoardRelation`, `BoardTemplate`, `ItemTemplate`, `TimeTrackingEntry` |
| 5 | Documents | `Notrelix.Domain.Documents` | `Page`, `Block` |
| 6 | Collaboration | `Notrelix.Domain.Collaboration` | `Comment`, `Reaction`, `Watcher`, `PresenceSession` |
| 7 | Automation Frozen | `Notrelix.Domain.Automation.Rules`, `.RulesEngine`, `.Scheduled`, `.Templates` | `AutomationRule`, `ScheduledJob`, `AutomationTemplate` |
| 8 | Integrations | `Notrelix.Domain.Integrations` | `IntegrationConnection`, `CalendarIntegration`, `WebhookDelivery`, `WebhookSubscription` |
| 9 | Billing | `Notrelix.Domain.Billing` | `Subscription`, `Plan`, `Invoice`, `PaymentMethod`, `BillingEvent`, `Entitlement` |
| 10 | Governance | `Notrelix.Domain.Governance` | `PermissionTemplate`, `PermissionTemplateDefinition`, `PermissionRule`, `ResourcePermission`, `CustomRole`, `ShareLink`, `AuditEntry` |
| 11 | Analytics | `Notrelix.Domain.Analytics` | `Dashboard`, `DashboardSource`, `DashboardWidget`, `ReportingSnapshot` |

### Experimental Capabilities

| # | Capability | Namespace Prefix | Notes |
|---|---|---|---|
| 1 | Automation Experimental | `Notrelix.Domain.Automation.Triggers`, `.Actions`, `.Conditions`, `.Executions`, `.Agents` | Isolated from Frozen snapshots |
| 2 | Collaboration Presence | `Notrelix.Domain.Collaboration` (PresenceSession) | Longest-prefix registry override |

### Stabilizing Capabilities

None. All capabilities classified as either `Frozen` or `Experimental`.

---

## Aggregate Scopes

| Scope | Aggregate Roots |
|---|---|
| **Global** | `User`, `ApiToken`, `EmailVerificationToken`, `PasswordResetToken`, `Plan` |
| **Account** | `Account`, `AccountMember`, `AccountInvitation`, `AccountIdentityProvider`, `ScimDirectory`, `ScimSyncRun`, `UserSecuritySettings`, `UserProfile`, `UserMfaMethod`, `UserLogin`, `UserSession`, `PermissionTemplate`, `AuditEntry`, `Dashboard`, `DashboardSource`, `DashboardWidget`, `ReportingSnapshot`, `Subscription`, `Invoice`, `PaymentMethod`, `BillingEvent`, `Entitlement` |
| **Workspace** | `Workspace`, `WorkspaceMember`, `WorkspaceInvitation`, `Space`, `Team`, `Board`, `BoardField`, `BoardGroup`, `BoardItem`, `BoardView`, `BoardViewUserPreference`, `SavedFilter`, `Checklist`, `Form`, `Label`, `BoardRelation`, `BoardTemplate`, `ItemTemplate`, `Page`, `Block`, `Comment`, `Reaction`, `Watcher`, `PresenceSession`, `IntegrationConnection`, `CalendarIntegration`, `WebhookDelivery`, `WebhookSubscription`, `PermissionRule`, `ResourcePermission`, `CustomRole`, `ShareLink`, `AutomationRule`, `ScheduledJob`, `AutomationTemplate` |

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

- 203 `[CoversMutation]` attributes across all 11 bounded contexts
- 4 mutation coverage tests enforce:
  - Every mutation method on every frozen aggregate has `[CoversMutation]`
  - Signatures reference methods that exist on the target type (fuzzy match: name + parameter count)
  - No duplicate signatures per aggregate
  - Attribute only on `[Fact]` or `[Theory]` methods
- 6 query methods excluded via `NonMutationMethodRegistry`

---

## Architecture Gates

**All 36 architecture tests pass:**

- 6 Determinism semantic tests (Roslyn-based source analysis)
- 8 DomainTypeGraphWalker tests (synthetic structural tests)
- 6 ArchitectureExclusionRegistry tests
- Cross-context reference tests (entity + frozen aggregate checks)
- State encapsulation tests
- Determinism reflection tests

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

---

## Certification Statement

> Notrelix Domain production surface is frozen at HEAD.
> All production capabilities are classified Frozen.
> Experimental capabilities are isolated and excluded from Frozen compatibility snapshots.
> Every Frozen aggregate mutation has explicit executable scenario coverage.
> Contract snapshots and architecture gates pass without regeneration or skipped tests.
