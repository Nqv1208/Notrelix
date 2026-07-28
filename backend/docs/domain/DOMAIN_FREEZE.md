# Notrelix Domain Freeze

> **Certified SHA:** `d94f5f2250b01a96cea086993a7d6c97180896b5`
> **Date:** 2026-07-28
> **Domain build:** Release 0 warnings 0 errors (warnaserror)
> **Domain tests:** 2145 pass, 1 informational (mutation coverage)
> **Full solution tests:** 3344 pass
> **Frozen capabilities:** 35 | **Experimental:** 12 | **Stabilizing:** 0

---

## Frozen Capabilities

All production Domain capabilities are **Frozen** unless listed under Experimental below.

### Bounded Contexts

| Context | Status | Aggregates |
|---------|--------|------------|
| **Common** | Frozen | Entity, AuditableEntity, AggregateRoot, SoftDeletableEntity, DomainEvent, GlobalDomainEvent, AccountScopedDomainEvent, WorkspaceScopedDomainEvent, ValueObject, Guard, BusinessRuleException, EventNameAttribute, Color, DateRange, Email, FractionalIndex, FractionalIndexGenerator, Icon, JsonValue, Money, ResourceRef, ResourceType, SecretRef, Slug, Url |
| **SharedKernel** | Frozen | (cross-context types) |
| **Accounts** | Frozen | Account, AccountMember, AccountInvitation, AccountDomain, AccountIdentityProvider, ScimDirectory, WorkspaceRoute |
| **Identity** | Frozen | User, UserSession, UserMfaMethod, ApiToken, UserSecuritySettings, UserProfile, UserLoginAttempt |
| **Workspaces** | Frozen | Workspace, WorkspaceMember, WorkspaceInvitation, Space, Team |
| **WorkManagement** | Frozen | Board, BoardField, BoardItem, BoardGroup, BoardView, SavedFilter, Checklist, Label, Form, ApprovalRequest, BoardRelation, BoardTemplate, ItemTemplate, TimeTrackingEntry, BoardViewUserPreference |
| **Documents** | Frozen | Page, Block, DocumentVersion, ResourceLink, PageTemplate |
| **Collaboration** | Frozen | Attachment, Comment, Mention, Reaction, ResourceReadState, ResourceWatcher |
| **Governance** | Frozen | ResourcePermission, PermissionRule, CustomRole, ShareLink, PermissionTemplate, WorkspacePolicy |
| **Automation** | Frozen | AutomationRule, AutomationTemplate, ScheduledJob |
| **Integrations** | Frozen | IntegrationConnection, CalendarIntegration, WebhookSubscription, WebhookDelivery, InboundWebhookEvent |
| **Billing** | Frozen | Plan, Subscription, Entitlement, Invoice, PaymentMethod, UsageMetric, WorkspaceFeatureUsage, BillingCustomer, BillingEvent |
| **Analytics** | Frozen | Dashboard, DashboardWidget, DashboardSource |

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
| **Triggers** | `Automation/Triggers/` | Runtime trigger evaluation. |
| **Actions** | `Automation/Actions/` | Runtime action execution. |
| **Conditions** | `Automation/Conditions/` | Runtime condition evaluation. |
| **Executions** | `Automation/Executions/` | Runtime state machine for automation execution. |
| **Agents** | `Automation/Agents/` | AI agent orchestration. |

---

## Key Invariants Locked

### Foundation
- Entity rejects `Guid.Empty`
- DomainEvent requires `occurredAt`, no UtcNow in Domain
- AggregateRoot: Version starts at 1, incremented on persistent mutation
- SoftDeletableAggregateRoot: protected lifecycle methods
- AuditableEntity: CreatedAt set once, UpdatedAt ≥ CreatedAt
- Two-phase audit: `PrepareAuditUpdate` validates, `ApplyAuditUpdate` mutates
- BusinessRuleException with stable RuleCode per context
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
| Domain Events | `DomainEvents.approved.txt` | Deterministic, schema v1 |
| Rule Codes | `RuleCodes.approved.txt` | Deterministic, schema v1 |
| Enums | `Enums.approved.txt` | Deterministic, schema v1 |
| Public API | `FrozenDomainPublicApi.approved.txt` | Deterministic, schema v1 |

Regeneration requires `UPDATE_DOMAIN_FREEZE_SNAPSHOTS=1` env var. Forbidden in CI.

---

## Architecture Freeze Gates

All architecture gates use `DomainTypeGraphWalker` for full recursive type graph traversal:

| Gate | Description |
|------|-------------|
| `CommonSharedKernelIsolationTests` | Common/SharedKernel depends on no bounded contexts |
| `CrossContextReferenceTests` | No aggregate references concrete entity from another context |
| `ExperimentalIsolationTests` | Frozen types do not reference experimental types |
| `FrameworkDependencyTests` | Domain references no infrastructure namespaces/types |
| `DeterminismTests` | No DateTime.UtcNow, Random.Shared, CultureInfo, Environment.* |
| `StateEncapsulationTests` | No public mutable collections |
| `TenantScopeTests` | Scope interfaces match registry scope |
| `DomainCapabilityRegistryTests` | Registry consistency, no overlaps, all aggregates mapped |
| `MutationCoverageTests` | Every mutation on frozen aggregate has [CoversMutation] coverage |

---

## Mutation Coverage Infrastructure

- `CoversMutationAttribute`: `[AllowMultiple]`, documents scenario per mutation
- `MutationSignatureFormatter`: canonical `Method(Type1,Type2)` format with fully qualified types
- `MutationCoverageTests`: discovers all public mutations, validates coverage exists

---

## Definition of Done (Verified)

- [x] Domain Release build 0 warnings 0 errors (warnaserror)
- [x] Domain.Tests 2145 pass (1 informational: mutation coverage reporting)
- [x] Full solution tests 3344 pass
- [x] 4 contract snapshots deterministic and tested
- [x] DomainCapabilityRegistry single source of truth (35 Frozen, 12 Experimental, 0 Stabilizing)
- [x] 66 concrete AggregateRoot subclasses with `[CoversAggregate]` on real behavior tests
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
- [x] Experimental (Formula, Rollup, Workload, Approval, Presence, Triggers, Actions, Conditions, Executions, Agents) isolated
- [x] Event names/versions unique and attributed
- [x] Rule codes unique and locked
- [x] Enum numeric values snapshot tested
- [x] Architecture tests enforce Domain purity via full type graph walker
- [x] Mutation contract compliance enforced
- [x] Construction contract compliance enforced
- [x] Two-phase audit protocol (PrepareAuditUpdate + ApplyAuditUpdate)
- [x] Fail-closed capability/scope registry
- [x] Snapshot regeneration requires explicit env var gate
