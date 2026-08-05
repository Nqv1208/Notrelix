# Deletion Policy Matrix

> **Status:** Frozen  
> **Last updated:** 2026-07-31  
> **Source of truth:** `backend/tests/Notrelix.Domain.Tests/Freeze/DeletionPolicyRegistry.cs`

This document classifies every aggregate's deletion policy in the Notrelix Domain.
The classification is authoritative and enforced by architecture tests.

---

## Policy Definitions

| Policy | Meaning |
|--------|---------|
| **NotSupported** | Resource cannot be deleted or restored. No `IsDeleted` field. |
| **RecoverableDelete** | Record is hidden from normal operations and can be restored without changing business state. |
| **ArchiveOnly** | Business archive replaces deletion. No generic `IsDeleted`. |
| **BusinessTerminationOnly** | Cancel/revoke/close is the terminal business state. No generic deletion. |
| **AppendOnly** | Audit/event/history/snapshot facts are immutable and never deleted through Domain. |
| **OwnedRemoval** | Child is removed from root collection. No independent soft-delete lifecycle. |
| **BusinessTombstone** | Deleted representation must remain loadable for business reasons (e.g., thread topology). |

---

## Key Architectural Rules

1. **Business status is independent from deletion state.**  
   `Delete()` and `Restore()` never modify business status (e.g., `Status`, `CommentStatus`).

2. **No `SoftDeleted` enum values.**  
   Enums do not contain `SoftDeleted` when the type exposes `IsDeleted`.

3. **No `_statusBeforeDeletion` recovery fields.**  
   Restore preserves the current business status; no patch fields exist.

4. **Restore only changes deletion availability.**  
   `IsDeleted` becomes `false`; business state remains unchanged.

---

## Accounts

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Account` | RecoverableDelete | `AccountStatus` preserved through delete/restore |
| `AccountMember` | OwnedRemoval | Removed from account collection |
| `WorkspaceRoute` | RecoverableDelete | Route can be restored |
| `AccountDomain` | BusinessTerminationOnly | Domain verification lifecycle |
| `AccountIdentityProvider` | BusinessTerminationOnly | Provider connection lifecycle |
| `ScimDirectory` | BusinessTerminationOnly | SCIM directory lifecycle |
| `AccountInvitation` | BusinessTerminationOnly | Accept/revoke/expire lifecycle |

---

## Identity

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `User` | RecoverableDelete | `UserStatus` preserved through delete/restore |
| `UserProfile` | RecoverableDelete | Profile availability |
| `UserSession` | BusinessTerminationOnly | Revoke/expire lifecycle |
| `UserSecuritySettings` | NotSupported | Security settings always available |
| `UserMfaMethod` | BusinessTerminationOnly | MFA method lifecycle |
| `ApiToken` | BusinessTerminationOnly | Token revoke/expire |
| `EmailVerificationToken` | BusinessTerminationOnly | Token consume/expire |
| `PasswordResetToken` | BusinessTerminationOnly | Token consume/expire |
| `UserLoginAttempt` | AppendOnly | Security audit trail |

---

## Workspaces

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Workspace` | RecoverableDelete | `WorkspaceStatus` (Active/Archived) preserved |
| `WorkspaceMember` | OwnedRemoval | Membership removal is business transition |
| `WorkspaceInvitation` | BusinessTerminationOnly | Accept/revoke/expire lifecycle |
| `Space` | RecoverableDelete | Space can be restored |
| `Team` | RecoverableDelete | Team can be restored |

---

## WorkManagement

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Board` | RecoverableDelete | Archive status preserved through delete/restore |
| `BoardField` | RecoverableDelete | System fields cannot be deleted |
| `BoardItem` | RecoverableDelete | Item availability independent from archive |
| `BoardGroup` | RecoverableDelete | Group availability |
| `BoardView` | RecoverableDelete | View availability |
| `BoardViewUserPreference` | RecoverableDelete | User preference availability |
| `SavedFilter` | RecoverableDelete | Filter availability |
| `BoardRelation` | RecoverableDelete | Relation availability |
| `Checklist` | RecoverableDelete | Checklist availability |
| `ApprovalRequest` | RecoverableDelete | Approval request availability |
| `Form` | RecoverableDelete | Form availability |
| `Label` | RecoverableDelete | Label availability |
| `BoardTemplate` | RecoverableDelete | Template availability |
| `ItemTemplate` | RecoverableDelete | Template availability |
| `TimeTrackingEntry` | AppendOnly | Time tracking is immutable audit data |

---

## Documents

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Page` | BusinessTombstone | Hierarchy topology requires loadable deleted nodes |
| `Block` | BusinessTombstone | Block tree topology requires loadable deleted nodes |
| `ResourceLink` | RecoverableDelete | Link availability |
| `DocumentVersion` | AppendOnly | Version history is immutable |
| `PageTemplate` | ArchiveOnly | Template archive lifecycle |

---

## Collaboration

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Comment` | BusinessTombstone | Thread topology requires loadable deleted nodes; resolution status preserved |
| `Reaction` | AppendOnly | Reactions are immutable facts |
| `ResourceWatcher` | AppendOnly | Watch subscriptions are immutable |
| `Attachment` | RecoverableDelete | Attachment availability |

---

## Governance

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `ResourcePermission` | RecoverableDelete | Permission availability |
| `PermissionRule` | RecoverableDelete | Rule availability |
| `CustomRole` | ArchiveOnly | Role archive lifecycle |
| `ShareLink` | BusinessTerminationOnly | Revoke/expire lifecycle |
| `PermissionTemplate` | ArchiveOnly | Template archive lifecycle |

---

## Automation

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `AutomationRule` | RecoverableDelete | Rule availability |
| `AutomationTemplate` | RecoverableDelete | Template availability |
| `ScheduledJob` | RecoverableDelete | Job availability |
| `AiAgent` | RecoverableDelete | Agent availability |
| `AiAgentRun` | AppendOnly | Run history is immutable |
| `AutomationExecution` | AppendOnly | Execution history is immutable |

---

## Integrations

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `IntegrationConnection` | RecoverableDelete | Connection status (Active/Error/Expired/Revoked) preserved |
| `WebhookSubscription` | RecoverableDelete | Subscription availability |
| `CalendarIntegration` | RecoverableDelete | Calendar integration availability |
| `WebhookDelivery` | AppendOnly | Delivery log is immutable |
| `InboundWebhookEvent` | AppendOnly | Inbound event log is immutable |

---

## Billing

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Plan` | NotSupported | Global catalog, never deleted |
| `Entitlement` | NotSupported | Entitlement definitions are permanent |
| `BillingCustomer` | BusinessTerminationOnly | Customer lifecycle |
| `Subscription` | BusinessTerminationOnly | Cancel/expire lifecycle, no soft delete |
| `Invoice` | AppendOnly | Financial records are immutable |
| `PaymentMethod` | BusinessTerminationOnly | Payment method lifecycle |
| `UsageMetric` | AppendOnly | Usage data is immutable |
| `WorkspaceFeatureUsage` | AppendOnly | Feature usage is immutable |
| `BillingEvent` | AppendOnly | Billing events are immutable |

---

## Analytics

| Aggregate | Policy | Notes |
|-----------|--------|-------|
| `Dashboard` | ArchiveOnly | Dashboard archive lifecycle |
| `DashboardSource` | NotSupported | Data source configuration is permanent |
| `ReportingSnapshot` | AppendOnly | Snapshots are immutable point-in-time facts |

---

## Unique Key Restore Policies

For aggregates with unique keys, the restore collision policy must be explicitly chosen:

| Key | Policy | Implementation |
|-----|--------|----------------|
| Account slug | Reserved after deletion | Normal unique index |
| Workspace slug | Reserved after deletion | Normal unique index |
| Account domain | Reserved after deletion | Normal unique index |

> **Note:** Restore collision handling is an Application/Infrastructure concern.
> Domain `Restore()` does not query for collisions. Application handlers must
> check for active collisions and translate unique constraint violations to
> stable conflict results.

---

## Query Behavior

Infrastructure owns query filtering:

- **RecoverableDelete:** Normal queries exclude `IsDeleted = true`. Admin/restore queries explicitly include deleted.
- **BusinessTombstone:** Thread topology queries may include deleted nodes. Normal content projection hides or masks deleted content per policy.
- **AppendOnly:** No deletion query filters needed; records are never deleted.

---

## Retention and Purge

Retention and physical purge belong to Application/Infrastructure:

- Retention duration is policy-driven
- Purge batch size is operational
- Legal hold check precedes purge
- Outbox/audit preservation is required
- Purge observability is mandatory

No Domain `Purge()` method exists unless purge eligibility is a real business invariant.
