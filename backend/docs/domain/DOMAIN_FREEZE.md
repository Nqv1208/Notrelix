# Notrelix Domain Freeze

> **Baseline:** `49459fc` → DF01–DF25
> **Date:** 2026-07-27
> **Domain build:** Release 0 warnings 0 errors (warnaserror)
> **Domain tests:** 2080 pass
> **Frozen capabilities:** 28 | **Experimental:** 9
> **Frozen aggregates:** 66 concrete subclasses | **Snapshots:** 4 (deterministic)

---

## Frozen Capabilities

All production Domain capabilities are **Frozen** unless listed under Experimental below.

### Bounded Contexts

| Context | Status | Aggregates |
|---------|--------|------------|
| **Common** | Frozen | Entity, AuditableEntity, AggregateRoot, SoftDeletableEntity, DomainEvent, GlobalDomainEvent, AccountScopedDomainEvent, WorkspaceScopedDomainEvent, ValueObject, Guard, BusinessRuleException, EventNameAttribute, Color, DateRange, Email, FractionalIndex, FractionalIndexGenerator, Icon, JsonValue, Money, ResourceRef, ResourceType, SecretRef, Slug, Url |
| **SharedKernel** | Frozen | (cross-context types) |
| **Accounts** | Frozen | Account, AccountMember, AccountInvitation, AccountDomain, AccountIdentityProvider, ScimDirectory, AccountSettings, WorkspaceRoute |
| **Identity** | Frozen | User, UserSession, UserMfaMethod, ApiToken, UserSecuritySettings, UserProfile, UserLoginAttempt |
| **Workspaces** | Frozen | Workspace, WorkspaceMember, WorkspaceInvitation, Space, Team |
| **WorkManagement** | Frozen | Board, BoardField, BoardItem, BoardGroup, BoardView, SavedFilter, Checklist, Label, Form, ApprovalRequest, BoardRelation, BoardTemplate, ItemTemplate, TimeTrackingEntry |
| **WorkManagement.Formulas** | Experimental | FormulaExpression (stub only) |
| **WorkManagement.Rollups** | Experimental | RollupFunction (stub only) |
| **WorkManagement.Workload** | Experimental | WorkloadAllocation (minimal) |
| **WorkManagement.Approvals** | Experimental | ApprovalStep (missing sequential enforcement, approver authorization) |
| **Documents** | Frozen | Page, Block, DocumentVersion, ResourceLink, PageTemplate |
| **Collaboration.Attachments** | Frozen | Attachment |
| **Collaboration.Comments** | Frozen | Comment |
| **Collaboration.Mentions** | Frozen | Mention |
| **Collaboration.Reactions** | Frozen | Reaction |
| **Collaboration.ReadStates** | Frozen | ResourceReadState |
| **Collaboration.Rules** | Frozen | (rules only) |
| **Collaboration.Watchers** | Frozen | ResourceWatcher |
| **Collaboration.Presence** | Experimental | (presence tracking) |
| **Governance** | Frozen | ResourcePermission, PermissionRule, CustomRole, ShareLink, PermissionTemplate, WorkspacePolicy |
| **Automation.RulesEngine** | Frozen | RulesEngine (deterministic evaluation) |
| **Automation.Scheduled** | Frozen | ScheduledJob |
| **Automation.Rules** | Frozen | AutomationRule |
| **Automation.Templates** | Frozen | AutomationTemplate |
| **Automation.Triggers** | Experimental | (runtime triggers) |
| **Automation.Actions** | Experimental | (runtime actions) |
| **Automation.Conditions** | Experimental | (runtime conditions) |
| **Automation.Executions** | Experimental | AutomationExecution (runtime state machine) |
| **Automation.Agents** | Experimental | AiAgent, AiAgentRun |
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
| **Presence** | `Collaboration/Presence/` | Real-time presence tracking. No business rules, no invariants. |

---

## Key Invariants Locked

### Foundation
- Entity rejects `Guid.Empty`
- DomainEvent requires `occurredAt`, no UtcNow in Domain
- AggregateRoot: Version starts at 1, incremented on persistent mutation
- SoftDeletableAggregateRoot: protected lifecycle methods
- AuditableEntity: CreatedAt set once, UpdatedAt ≥ CreatedAt, BusinessRuleException
- No public setters, no public mutable collections
- No DateTime.UtcNow, Random.Shared, CultureInfo.CurrentCulture, Environment.*

### Events
- All concrete events: sealed, [EventName], correct scope hierarchy
- Workspace events carry AccountId + WorkspaceId
- Event names unique, format: `context.action`
- No broken inheritance, no property shadowing
- Domain events minimal: only EventId + OccurredAt

### Multi-tenant
- IWorkspaceScoped aggregates have AccountId + WorkspaceId
- IAccountScoped aggregates have AccountId
- Global query filters enforce tenant isolation
- Cross-scope operations validate matching scope

### Mutation Contract (Validated)
- Validate → change state → audit once → version once → event per contract
- No-op and failed mutations don't change state/audit/version/event

### Construction Contract (Validated)
- Guid.Empty rejected at construction
- Public factory creates valid state
- No public setters for business state

### Tenant Contract (Validated)
- AccountId/WorkspaceId immutable after construction
- No cross-context concrete entity references

---

## Snapshot Gates

| Gate | File | Status |
|------|------|--------|
| Domain Events | `DomainEvents.approved.txt` | Deterministic |
| Rule Codes | `RuleCodes.approved.txt` | Deterministic |
| Enums | `Enums.approved.txt` | Deterministic |
| Public API | `FrozenDomainPublicApi.approved.txt` | Deterministic |

---

## Architecture Freeze Gates

All 10 architecture gates are enforced via test:

| Gate | Description |
|------|-------------|
| `DomainTests` | All aggregates have >= 1 test |
| `NoExternalDependencies` | Domain references no infrastructure |
| `NoDateTimeUtcNow` | No UtcNow in Domain code |
| `NoSystemDependencies` | No Environment.*, Random.Shared |
| `EventContractCompliance` | All events have [EventName] |
| `AggregatePublicApi` | Public API surface frozen |
| `RuleCodeUniqueness` | All rule codes unique |
| `EnumValuesStable` | Enum numeric values frozen |
| `MutationContractCompliance` | All mutations follow validate-audit-version-event |
| `ConstructionContractCompliance` | All aggregates valid at construction |

---

## Definition of Done (Verified)

- [x] Domain Release build 0 warnings 0 errors (warnaserror)
- [x] Domain.Tests all green (2080 tests)
- [x] 4 contract snapshots deterministic and tested
- [x] DomainCapabilityRegistry single source of truth (28 capabilities, 13 bounded contexts)
- [x] 66 concrete AggregateRoot subclasses with `[CoversAggregate]` fixtures
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
- [x] Experimental (Formula, Rollup, Workload, Approval, Presence) isolated
- [x] Event names/versions unique and attributed
- [x] Rule codes unique and locked
- [x] Enum numeric values snapshot tested
- [x] Architecture tests enforce Domain purity
- [x] Mutation contract compliance enforced
- [x] Construction contract compliance enforced
