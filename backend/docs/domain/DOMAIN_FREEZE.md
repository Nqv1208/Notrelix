# Notrelix Domain Freeze

> **Baseline:** `78df8c9b` → Domain Freeze PRs D01–D08
> **Date:** 2026-07-25
> **Domain build:** Release 0 warnings 0 errors
> **Domain tests:** 1464+ pass

---

## Frozen Capabilities

All production Domain capabilities are **Frozen** unless listed under Experimental below.

### Common / SharedKernel

| Component | Status | Notes |
|-----------|--------|-------|
| Entity, AuditableEntity, AggregateRoot | Frozen | Optimistic concurrency via Version, audit guards |
| SoftDeletableEntity | Frozen | Protected MarkDeleted/MarkRestored, concrete aggregates own lifecycle |
| DomainEvent, GlobalDomainEvent | Frozen | No UtcNow, occurredAt required |
| AccountScopedDomainEvent | Frozen | Validates AccountId, implements IAccountScoped |
| WorkspaceScopedDomainEvent | Frozen | Inherits AccountScoped, validates both IDs |
| ValueObject | Frozen | Exact-type equality, SequenceEqual |
| Guard | Frozen | Stable rule codes, CallerArgumentExpression |
| BusinessRuleException | Frozen | Only exception type, requires RuleCode |
| BusinessRuleCodes | Frozen | ~460 stable codes, Context_Aggregate_RuleName pattern |
| EventNameAttribute | Frozen | Name + Version, applied to all 381 events |
| Color | Frozen | #RGB→#RRGGBB normalization, uppercase |
| DateRange | Frozen | Start inclusive, End inclusive/null, validates default |
| Email | Frozen | Trim→lowercase, max 254, regex validation |
| FractionalIndex | Frozen | Official fractional-indexing v4.0.0 port (CC0-1.0) |
| FractionalIndexGenerator | Frozen | GenerateKeyBetween + GenerateNKeysBetween, postconditions |
| Icon | Frozen | FromEmoji/FromName, no Default |
| JsonValue | Frozen | Compact serialization, no raw parser messages |
| Money | Frozen | 3-letter ISO currency, trim→uppercase |
| ResourceRef | Frozen | Non-empty ResourceId, EnsureSameWorkspace |
| ResourceType | Frozen | Explicit numeric values, snapshot tested |
| SecretRef | Frozen | ToString masks value |
| Slug | Frozen | Unicode normalization (FormD), Create validates |
| Url | Frozen | HTTP/HTTPS only, scheme/host lowercase |

### Bounded Contexts

| Context | Status | Aggregates |
|---------|--------|------------|
| **Accounts** | Frozen | Account, AccountMember, AccountInvitation, AccountDomain, AccountIdentityProvider, ScimDirectory, AccountSettings, WorkspaceRoute |
| **Identity** | Frozen | User, UserSession, UserMfaMethod, ApiToken, UserSecuritySettings, UserProfile, UserLoginAttempt |
| **Workspaces** | Frozen | Workspace, WorkspaceMember, WorkspaceInvitation, Space, Team |
| **WorkManagement** | Frozen | Board, BoardField, BoardItem, BoardGroup, BoardView, SavedFilter, Checklist, Label, Form, ApprovalRequest, BoardRelation, BoardTemplate, ItemTemplate, TimeTrackingEntry |
| **Documents** | Frozen | Page, Block, DocumentVersion, ResourceLink, PageTemplate |
| **Collaboration** | Frozen | Comment, Reaction, Attachment, ResourceWatcher |
| **Governance** | Frozen | ResourcePermission, PermissionRule, CustomRole, ShareLink, PermissionTemplate, WorkspacePolicy |
| **Automation** | Frozen | AutomationRule, AutomationExecution, ScheduledJob, AiAgent, AiAgentRun, AutomationTemplate |
| **Integrations** | Frozen | IntegrationConnection, CalendarIntegration, WebhookSubscription, WebhookDelivery |
| **Billing** | Frozen | Plan, Subscription, Entitlement, Invoice, PaymentMethod, UsageMetric, WorkspaceFeatureUsage, BillingCustomer, BillingEvent |
| **Analytics** | Frozen | Dashboard, DashboardWidget, DashboardSource, ReportingSnapshot |

---

## Experimental Capabilities

These capabilities are **NOT Frozen**. They lack sufficient business rules for production use. **Frozen code must not depend on Experimental code.**

| Capability | Location | Reason |
|------------|----------|--------|
| **Formula** | `WorkManagement/Formulas/` | Stub only. No parser/evaluator. FormulaExpression is a string VO. |
| **Rollup** | `WorkManagement/Rollups/` | Stub only. RollupFunction enum, no evaluation logic. |
| **Workload** | `WorkManagement/Workload/` | Minimal. WorkloadAllocation entity, no capacity/overlap rules. |
| **Approval workflow** | `WorkManagement/Approvals/` | Basic CRUD + step management. Missing: sequential enforcement, approver authorization, self-approval policy, delegation, expiry, resubmission. |

---

## Key Invariants Locked

### Foundation
- Entity rejects `Guid.Empty`
- DomainEvent requires `occurredAt`, no UtcNow in Domain
- AggregateRoot: Version starts at 1, incremented on persistent mutation
- SoftDeletableEntity: protected MarkDeleted/MarkRestored, no public generic lifecycle
- AuditableEntity: CreatedAt set once, UpdatedAt ≥ CreatedAt, BusinessRuleException
- No public setters, no public mutable collections
- No DateTime.UtcNow, Random.Shared, CultureInfo.CurrentCulture, Environment.*

### Events
- All 381 concrete events: sealed, [EventName], correct scope hierarchy
- Workspace events carry AccountId + WorkspaceId
- Event names unique, format: `context.action`
- No broken inheritance, no property shadowing

### Multi-tenant
- IWorkspaceScoped aggregates have AccountId + WorkspaceId
- IAccountScoped aggregates have AccountId
- Global query filters enforce tenant isolation
- Cross-scope operations validate matching scope

### WorkManagement
- Board: archived restrictions on all mutations
- BoardField: ReorderOptions validates all IDs before mutation, GenerateNKeysBetween
- BoardItem: archived restrictions, Formula/Rollup write rejection
- FieldValue: Date parse validation, MultiSelect uniqueness
- FieldSettings: Status requires transitions
- Label: Update bumps version, SoftDelete has audit+version
- Template: Restore calls base.Restore(), Create has audit

---

## Definition of Done (Verified)

- [x] Domain Release build 0 warnings 0 errors
- [x] Domain.Tests all green (1464+ tests)
- [x] No stale or broken common base classes
- [x] No duplicate exception semantics (only DomainException + BusinessRuleException)
- [x] All business failures have stable rule codes
- [x] Common does not depend on bounded contexts
- [x] SharedKernel contains only cross-context types
- [x] All SharedKernel factories protect invariants
- [x] No aggregate/entity creatable with empty ID
- [x] No Domain event reads current clock
- [x] All workspace events have AccountId and WorkspaceId
- [x] All tenant aggregates have correct scope
- [x] No public mutable business collections
- [x] No public business-state setters
- [x] No-op and failed mutations don't change state/audit/version/event
- [x] Formula, Rollup, Workload, Approval isolated as Experimental
- [x] Event names/versions unique and attributed
- [x] Rule codes unique and locked
- [x] Enum numeric values snapshot tested (ResourceType)
- [x] Architecture tests enforce Domain purity
