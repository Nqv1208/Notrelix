---
document_id: WRK-BACKEND-ROADMAP
document_type: workstream-roadmap
status: active
owner: engineering-delivery
applies_to:
  - backend
  - platform-foundation
  - identity-accounts
  - workspace-governance
  - work-management
  - documents-collaboration
  - automation-integrations
  - billing-entitlements
  - analytics-reporting
  - backend-parallel-delivery
evidence:
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/capability-extraction-strategy.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/teams/workspace-governance.md
  - docs/workstreams/teams/work-management.md
  - docs/workstreams/teams/documents-collaboration.md
  - docs/workstreams/teams/automation-integrations.md
  - docs/workstreams/teams/billing-entitlements.md
  - docs/workstreams/teams/analytics-reporting.md
  - backend/docs/architecture/backend-overview.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
review_on:
  - backend-critical-path-change
  - bounded-context-dependency-change
  - team-topology-change
  - authorization-model-change
  - tenancy-model-change
  - event-contract-change
  - backend-service-extraction-decision
  - backend-foundation-freeze-change
---

# Backend Development Roadmap

## 1. Purpose

This document is the canonical execution roadmap for backend capability development after the backend foundation has been frozen.

It answers:

```text
What must be built first?
What may start in parallel?
What must wait?
What readiness level is required before a dependent team starts?
What contract must a producing team stabilize?
What does not block downstream work?
What evidence is required before a phase exits?
```

This roadmap does not redefine product semantics, bounded-context ownership, backend architecture, or team ownership.

Those remain owned by their canonical authorities.

This document owns:

```text
backend execution order
critical dependency spine
parallelization gates
phase entry/exit criteria
cross-team sequencing
```

## 2. Core roadmap principle

Backend development must follow dependency direction, not team preference.

The execution model is:

```text
critical dependency spine
+
parallel branches after contracts stabilize
```

It is NOT:

```text
Team A completes 100%
↓
Team B completes 100%
↓
Team C completes 100%
```

The purpose of readiness gates is to open safe parallelism as early as possible without forcing downstream teams to depend on unstable contracts.

## 3. Foundation freeze relationship

The backend foundation is already treated as frozen enough for feature development.

Therefore this roadmap MUST NOT reopen foundation architecture broadly.

Platform/Foundation work before business development is limited to blockers required by the next business capability.

The roadmap explicitly rejects:

```text
"finish every platform capability first"
```

because that creates foundation-first overengineering.

The correct rule is:

```text
close only foundation blockers required by the next critical business slice
```

## 4. Backend structural baseline

Production projects remain:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

The roadmap does not authorize:

- per-bounded-context production projects;
- new production projects;
- microservices;
- direct Domain → Infrastructure dependencies;
- direct context-to-context private persistence access.

A later service extraction requires an explicit architecture decision and extraction readiness evidence.

## 5. Business bounded-context baseline

The backend business capability map includes:

```text
Accounts
Identity
Workspaces
Governance
WorkManagement
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

Support capabilities such as Search/Operations are not used to reorder the business critical path unless a concrete dependency requires them.

## 6. Team execution topology

The roadmap assumes the following team ownership groups:

```text
Identity & Accounts
Workspace & Governance
Work Management
Documents & Collaboration
Automation & Integrations
Billing & Entitlements
Analytics & Reporting
Platform & Foundation
```

One team owning two bounded contexts does not merge those contexts.

## 7. Readiness model

This roadmap uses:

```text
D0 — UNDEFINED
D1 — DEFINED
D2 — DESIGNED
D3 — IMPLEMENTING
D4 — VERIFIED
D5 — STABLE
```

Interpretation:

### D0 — UNDEFINED

Capability/contract ownership is unclear.

No dependent implementation should begin.

### D1 — DEFINED

Owner and purpose known.

Insufficient for implementation dependency.

### D2 — DESIGNED

Contract/design exists.

Consumers may prepare local scaffolding but must not harden against the contract.

### D3 — IMPLEMENTING

Producer implementation in progress.

Consumers may build isolated work behind adapters/mocks where useful.

### D4 — VERIFIED

Contract implemented and tested.

Dependent teams may begin integration and product implementation.

### D5 — STABLE

Contract is accepted as foundation for broad parallel development.

Breaking changes require explicit cross-team coordination.

## 8. Backend critical dependency spine

The backend critical path is:

```text
Platform blockers
        ↓
Actor / Session / Account
        ↓
Account Context
        ↓
Workspace
        ↓
Workspace Membership
        ↓
Resource / Action / Permission
        ↓
Authorization Enforcement
        ↓
Board
        ↓
BoardItem
        ↓
BoardField / FieldValue
        ↓
Grouping / Ordering
```

This is the minimum spine that transforms the frozen backend foundation into a usable enterprise work-management SaaS core.

## 9. Top-level roadmap

```text
P0  Critical Platform blockers
        ↓
P1  Identity & Accounts core
        ↓
P2  Workspace & Governance core
        ↓
P3  WorkManagement transactional core
        ↓
        ┌───────────────────────┬──────────────────────┬───────────────────────┐
        ↓                       ↓                      ↓
P4A WorkManagement views   P4B Documents/Collab   P4C Billing/Entitlements
        └───────────────────────┴───────────┬──────────┘
                                            ↓
P5                                 Automation & Integrations
                                            ↓
P6                                 Analytics & Reporting
```

P4 lanes are intentionally parallel.

P5 may start incrementally as individual producer event contracts reach D4.

P6 may start projection infrastructure earlier, but business analytical implementation should follow source contract readiness.

# Priority 0 — Critical Platform blockers

## 10. Purpose of P0

P0 exists only to remove blockers preventing Identity/Accounts and Workspace/Governance from implementing their core safely.

P0 is not a complete Platform roadmap.

## 11. P0 required capabilities

The required minimum is:

```text
PF-01 Session / CSRF transport
PF-02 Actor / Account / Workspace technical context propagation
PF-03 Authorization enforcement pipeline
PF-11 Account-scoped frontend/server-state isolation contract
```

For backend-only execution, the frontend part of PF-11 is a cross-stack dependency but still affects safe Account transition semantics.

## 12. P0 backend scope

Required backend foundation:

- trusted Actor resolution;
- Account/Tenant context mechanism;
- Workspace context mechanism where needed;
- central Application authorization behavior;
- API/session/CSRF contract consistency;
- tests proving protected request behavior;
- no feature-local authorization bypass path.

## 13. P0 explicit non-blockers

The following Platform capabilities do NOT need to be fully complete before P1:

```text
full realtime recovery
full messaging optimization
all observability features
all packaging hardening
all UI foundation
all provider integration infrastructure
all performance optimization
```

They remain scheduled when consumers require them.

## 14. P0 exit gate

P1 may proceed when:

| Contract | Required readiness |
|---|---|
| Actor resolution | D5 |
| Session transport | D4+ |
| Account/Tenant technical context | D5 |
| Authorization pipeline registration | D5 |
| Protected request enforcement | D5 |
| CSRF/session contract for browser clients | D5 before protected frontend mutation delivery |

P0 does not need to wait for all Platform/Foundation work.

## 15. P0 evidence

Required:

- Application behavior tests;
- API authentication/authorization tests;
- integration tenant-isolation tests;
- CSRF/session tests;
- architecture gate for authorization ownership where applicable;
- production DI graph coverage.

# Priority 1 — Identity & Accounts

## 16. Why P1 is first business priority

All downstream contexts depend on stable answers to:

```text
Who is acting?
Which Account owns the request?
Which Account is active?
How is session identity represented?
```

If Identity & Accounts is unstable, downstream teams will create duplicate representations of:

- Actor;
- Account;
- current Account;
- tenant;
- session;
- account membership/identity relationship.

P1 therefore establishes the SaaS identity/tenant spine.

## 17. P1 critical core

The P1 critical path is:

```text
IA-CORE-01 Actor identity
        ↓
IA-CORE-02 Session lifecycle
        ↓
IA-CORE-03 Account lifecycle
        ↓
IA-CORE-04 Account resolution/current Account
        ↓
IA-CORE-05 Account context isolation
        ↓
IA-CORE-06 Identity ↔ Account stable contract
```

## 18. P1 capability detail

### IA-CORE-01 Actor identity

Must establish:

- canonical user/actor identity;
- authenticated actor representation;
- stable ID contract;
- actor resolution for Application use cases;
- no API/HTTP primitive leakage into Domain.

### IA-CORE-02 Session lifecycle

Must establish:

- login session creation;
- current session/current identity;
- expiration;
- logout/invalidation;
- session failure semantics.

OAuth/MFA are not required for the basic session contract to stabilize.

### IA-CORE-03 Account lifecycle

Must establish:

- Account identity;
- create/read/update lifecycle according to product semantics;
- tenant boundary;
- account state;
- stable reference contract.

### IA-CORE-04 Account resolution

Must establish:

- how the current Account is determined;
- how Account scope enters Application;
- missing/invalid Account behavior;
- tenant spoofing prevention.

### IA-CORE-05 Account isolation

Must prove:

- Account A data cannot be accessed as Account B;
- current Account switch semantics are clear;
- background execution remains Account-scoped.

### IA-CORE-06 Identity ↔ Account contract

Must define stable cross-context identity for consumers such as Workspaces, Governance and Billing.

## 19. P1 secondary capabilities

These may begin after core contracts stabilize:

```text
OAuth provider linking
MFA
security settings
API tokens
advanced session/security controls
```

They MUST NOT block P2 unless P2 specifically requires them.

## 20. P1 non-blocking rule

The following do not block Workspace/Governance core:

```text
OAuth complete coverage
MFA
API tokens
advanced security settings
all profile UX
```

Workspace/Governance only needs the stable Actor + Account contract.

## 21. P1 exit gate to P2

P2 may begin when:

| Contract | Required readiness |
|---|---|
| Actor identity | D5 |
| Account identity | D5 |
| Account/Tenant boundary | D5 |
| Account context resolution | D5 |
| session identity | D4+ |
| Account isolation | D5 |
| Identity ↔ Account consumer contract | D5 |

## 22. P1 tests

Required categories:

- Domain Account/Identity invariants;
- Application session/account handlers;
- Infrastructure persistence;
- API session/account contracts;
- integration tenant isolation;
- authorization;
- session lifecycle;
- Account switch/isolation;
- architecture tests where relevant.

## 23. P1 execution package

Before implementation, create:

```text
docs/workstreams/executions/identity-accounts/
├── identity-accounts.spec.md
├── identity-accounts.plan.md
├── identity-accounts.tests.md
└── identity-accounts.certification.md
```

The package must cover core first, secondary security capabilities later.

# Priority 2 — Workspace & Governance

## 24. Why P2 follows P1

Workspace/Governance depends on stable:

```text
Actor
Account
```

and creates the authorization/resource backbone required by every major product context.

If WorkManagement starts before this backbone stabilizes, it risks embedding:

- role checks;
- owner checks;
- workspace checks;
- permission strings

directly inside product handlers.

## 25. P2 critical path

```text
WG-CORE-01 Workspace
        ↓
WG-CORE-02 WorkspaceMember
        ↓
WG-CORE-03 Invitation
        ↓
WG-CORE-04 Resource identity / ResourceKind
        ↓
WG-CORE-05 Action
        ↓
WG-CORE-06 Permission
        ↓
WG-CORE-07 Built-in roles
        ↓
WG-CORE-08 Authorization integration
```

## 26. Workspace core

Must establish:

- Workspace identity;
- Account containment;
- lifecycle;
- member relationship;
- workspace scoping.

## 27. Membership

Must establish:

- subject/actor membership;
- member state;
- add/remove/change lifecycle;
- last-admin/owner rules where product-defined;
- access revocation semantics.

## 28. Invitation

Must establish:

- invite target;
- expiry;
- acceptance;
- revoke/reject;
- role/access intent;
- duplicate/replay behavior.

Invitation completeness does not need to block Board if Workspace/member/resource policy is already stable, but basic membership should be D4+.

## 29. Resource / Action / Permission

This is the critical Governance contract.

It must allow downstream resource teams to declare:

```text
Actor
→ ResourceKind
→ ResourceId
→ Action
→ policy decision
```

without implementing feature-local permission systems.

## 30. Built-in roles

Built-in role semantics must be stable enough for initial product enforcement.

Custom roles may remain later.

## 31. P2 secondary capabilities

May follow core:

```text
custom roles
share policies
advanced governance administration
additional permission administration
```

These do not automatically block P3.

## 32. P2 exit gate to P3

P3 may begin when:

| Contract | Required readiness |
|---|---|
| Workspace identity | D5 |
| Account → Workspace containment | D5 |
| WorkspaceMember | D4+ |
| resource identity/resource kind | D5 |
| Action semantics | D5 |
| Permission semantics | D5 |
| built-in role policy | D4+ |
| central authorization integration | D5 |

## 33. P2 tests

Required:

- Workspace Domain invariants;
- membership transitions;
- invitation behavior;
- role/permission invariants;
- Application authorization declarations;
- API 401/403 behavior;
- integration cross-Account/Workspace isolation;
- architecture authorization gates;
- resource/action integration tests.

## 34. P2 execution package

```text
docs/workstreams/execution/workspace-governance/
├── workspace-governance.spec.md
├── workspace-governance.plan.md
├── workspace-governance.tests.md
└── workspace-governance.certification.md
```

# Priority 3 — WorkManagement transactional core

## 35. Why P3 is the first product transactional core

After P1/P2, the system can answer:

```text
Who?
Which Account?
Which Workspace?
Can they do it?
```

P3 introduces:

```text
What work are they managing?
```

WorkManagement is the primary transactional product spine.

## 36. P3 critical path

```text
WM-701 Board
        ↓
WM-702 BoardItem
        ↓
WM-703 BoardField
        ↓
WM-704 FieldValue
        ↓
WM-705 Grouping
        ↓
WM-706 Ordering
        ↓
WM-707 Checklist
```

## 37. Board (WM-701)

Must establish:

- Workspace-contained Board;
- lifecycle;
- authorization resource mapping;
- stable identity;
- persistence.

## 38. BoardItem (WM-702)

Must establish:

- Item lifecycle;
- Board/group containment;
- mutation semantics;
- concurrency semantics where required;
- event production.

## 39. BoardField (WM-703)

Must establish:

- field definition;
- field type identity;
- validation/serialization contract;
- migration behavior.

## 40. FieldValue (WM-704)

Must establish:

- valid/null/empty semantics;
- value storage;
- update contract;
- sort/filter compatibility.

## 41. Grouping (WM-705)

Must establish whether grouping is:

- domain state;
- saved view state;
- derived query state.

No view may independently redefine it.

## 42. Ordering (WM-706)

Must use the hardened canonical ordering mechanism.

Ordering must cover:

- insertion;
- movement;
- dense insertion;
- deterministic comparison;
- invalid key handling;
- concurrency.

## 43. Checklists (WM-707)

Must establish checklist/item lifecycle and ordering without conflating them with Collaboration.

## 44. P3 explicit non-goals

Do not require before core exit:

```text
Table fully complete
Kanban fully complete
Calendar
Timeline
Dashboard
Form
Automation integration
Analytics reporting
all realtime optimization
```

The core must be stable before these parallel product surfaces expand.

## 45. P3 exit gate to P4

P4 product branches may begin when:

| Contract | Required readiness |
|---|---|
| Board | D5 |
| BoardItem | D5 |
| BoardField | D4+ |
| FieldValue | D4+ |
| Grouping semantics | D4+ |
| Ordering | D4+ / D5 for drag-heavy consumers |
| Workspace/Governance integration | D5 |
| WorkManagement resource/actions | D5 |
| WorkManagement event baseline | D3-D4 where consumers need it |

## 46. P3 tests

Required:

- Domain Board/Item/Field invariants;
- ordering;
- persistence;
- migrations;
- Application commands/queries;
- authorization;
- API contracts;
- integration workspace isolation;
- event production;
- concurrency/idempotency where applicable.

## 47. P3 execution package

```text
docs/workstreams/execution/work-management/
├── work-management.spec.md
├── work-management.plan.md
├── work-management.tests.md
└── work-management.certification.md
```

The master package must distinguish transactional core from view expansion.

# Priority 4 — Parallel product expansion

## 48. Purpose of P4

P4 is where meaningful parallel team development begins.

P4 has three main lanes:

```text
P4A WorkManagement Views
P4B Documents & Collaboration
P4C Billing & Entitlements
```

These lanes depend on different parts of the critical spine and therefore should not be serialized unnecessarily.

# P4A — WorkManagement views

## 49. P4A sequence

Recommended:

```text
WM-VIEW-01 Query/filter/sort
        ↓
WM-VIEW-02 Table
        ↓
WM-VIEW-03 Kanban
        ↓
WM-VIEW-04 Calendar
WM-VIEW-05 Timeline
WM-VIEW-06 Dashboard
WM-VIEW-07 Form
```

Table/Kanban should establish shared mutation/query consistency before later views.

## 50. P4A invariant

All views consume the same canonical:

```text
Board
BoardItem
BoardField
FieldValue
Grouping/Ordering
```

No independent transactional model per view.

## 51. P4A parallelization

After shared query/filter/sort is D4+:

```text
Calendar
Timeline
Dashboard
Form
```

may proceed in parallel if they do not redefine core state.

# P4B — Documents & Collaboration

## 52. P4B entry gate

Documents/Collaboration may begin once:

| Contract | Required readiness |
|---|---|
| Actor/Account | D5 |
| Workspace | D5 |
| Governance authz | D5 |
| Platform ordering primitive if shared | D4+ |
| realtime transport | D3+ for scaffolding, D4+ before hardening |

WorkManagement core is not strictly required for Documents itself.

WorkManagement target contracts are required only for Collaboration features targeting WorkManagement resources.

## 53. Documents core sequence

```text
Page
↓
Page hierarchy
↓
Block
↓
Block content contract
↓
Block ordering
↓
Document query/editor integration
↓
Realtime/recovery
```

## 54. Collaboration sequence

```text
Comment
↓
Comment target contract
↓
Comment authorization
↓
Comment realtime
↓
target deletion/retention
```

## 55. WorkManagement collaboration dependency

Comments on Board/BoardItem wait for WorkManagement target resource contract D4+.

Documents comments do not need to wait for WorkManagement.

# P4C — Billing & Entitlements

## 56. P4C entry gate

Billing may begin after:

| Contract | Required readiness |
|---|---|
| Account identity | D5 |
| billable Account semantics | D5 |
| billing-admin authorization | D4+ |
| Platform idempotency | D4+ before financial mutations |
| provider secret mechanism | D4+ before provider integration |

Billing does NOT need WorkManagement core to begin.

## 57. Billing core sequence

```text
Plan
↓
Subscription
↓
Entitlement
↓
Product entitlement contract
↓
Usage
↓
Provider/payment integration
↓
Billing administration
```

## 58. Billing parallelism

After entitlement contract reaches D4:

- WorkManagement can consume entitlement;
- Automation can consume entitlement;
- Documents can consume entitlement;
- Billing provider work can continue independently.

## 59. P4 completion condition

P4 is not a single gate requiring all three lanes to finish.

Each lane independently reaches D4/D5 contracts needed by P5.

# Priority 5 — Automation & Integrations

## 60. Why P5 comes after producer contracts

Automation is a downstream consumer of business facts.

If implemented too early, it will pressure source contexts to design events around Automation internals.

Correct dependency:

```text
source context owns business fact
↓
Automation consumes fact
↓
Automation orchestrates
↓
Integrations executes provider operation
```

## 61. P5 incremental entry rule

Automation does not need every producer context complete.

A trigger may be implemented when its producer event is D4+.

Examples:

```text
WorkManagement item events D4+
→ WorkManagement automation triggers may begin

Documents events D4+
→ Documents triggers may begin
```

## 62. Automation core sequence

```text
Rule
↓
Trigger
↓
Condition
↓
Action
↓
Enable/Disable
↓
Event matching
↓
Execution
↓
Retry/failure
```

## 63. Integrations core sequence

```text
Connector catalog
↓
Connection
↓
Provider authorization
↓
Configuration
↓
Credential reference
↓
Outbound operation
↓
Inbound webhook
↓
Health/reconciliation
```

## 64. Automation → Integration contract

Must remain:

```text
Automation action
→ Integration operation contract
→ provider adapter
```

Automation handlers must not directly instantiate provider SDKs.

## 65. P5 platform dependencies

Before hardening:

| Platform contract | Required readiness |
|---|---|
| messaging delivery | D5 |
| message identity | D5 |
| idempotency | D5 |
| ordered delivery where required | D4+ |
| poison handling | D4+ |
| observability | D4+ |
| secret mechanism | D5 for credentials |

## 66. P5 tests

Required:

- rule/trigger/action Domain tests;
- event idempotency;
- execution state tests;
- provider adapter tests;
- webhook signature/replay;
- integration source event → execution → provider operation;
- retry/terminal failure;
- tenant isolation;
- authorization.

## 67. P5 execution package

```text
docs/workstreams/execution/automation-integrations/
├── automation-integrations.spec.md
├── automation-integrations.plan.md
├── automation-integrations.tests.md
└── automation-integrations.certification.md
```

# Priority 6 — Analytics & Reporting

## 68. Why P6 is downstream

Analytics consumes stable source facts.

It should not define transactional semantics or force source persistence coupling.

Analytics business implementation therefore follows source contract stability.

## 69. Early work allowed before P6

Analytics team may prepare:

- projection mechanism;
- idempotent consumer framework;
- metric-definition format;
- backfill/rebuild tooling;
- storage proof of concept;

without hard-coding unstable business metrics.

## 70. P6 entry gate

A report/metric may begin when:

```text
source fact semantics → D5
source event/reporting contract → D4+
authorization scope → D4+
```

Cross-context reports require all participating source projections D4+.

## 71. Analytics sequence

```text
Source inventory
↓
Metric definitions
↓
Projection foundation
↓
Idempotency/replay
↓
Domain projections
↓
Cross-context projections
↓
Report API
↓
Reporting frontend
↓
Export/hardening
```

## 72. Analytics source rule

Analytics MUST NOT start from:

```text
which tables can we join?
```

It starts from:

```text
which source-owned business facts define this metric?
```

## 73. P6 tests

Required:

- projection idempotency;
- replay;
- late events;
- corrections;
- tenant isolation;
- authorization;
- source → projection → report integration;
- rebuild equivalence;
- query/report performance;
- data quality.

## 74. P6 execution package

```text
docs/workstreams/execution/analytics-reporting/
├── analytics-reporting.spec.md
├── analytics-reporting.plan.md
├── analytics-reporting.tests.md
└── analytics-reporting.certification.md
```

# Platform work across the roadmap

## 75. Platform is an enabling lane, not a serialized phase

Platform continues throughout all priorities.

It should pick up capabilities just before consumers require them.

Example:

```text
P0
→ session/context/authz

before WorkManagement realtime
→ realtime recovery

before Automation
→ durable messaging/idempotency/poison

before provider integrations
→ secret/observability foundation

continuously
→ architecture tests / CI / packaging
```

## 76. Platform just-in-time dependency schedule

### Before P1

```text
session
actor/account context
authz pipeline
```

### Before P3

```text
stable resource authz integration
persistence/migration mechanisms already frozen
```

### Before realtime-heavy P4

```text
realtime recovery D4+
```

### Before P5

```text
messaging D5
idempotency D5
message identity D5
poison/order D4+
secret handling D5
```

### Before P6

```text
event delivery/replay mechanism D5
observability D4+
```

# Cross-team dependency gates

## 77. P1 → P2 gate

Required:

```text
Actor D5
Account D5
Account boundary D5
Account context D5
Account isolation D5
```

Session advanced features are not required.

## 78. P2 → P3 gate

Required:

```text
Workspace D5
Account→Workspace containment D5
ResourceKind/Resource D5
Action D5
Permission D5
Authorization enforcement D5
```

Custom roles are not required.

## 79. P3 → P4A gate

Required:

```text
Board D5
BoardItem D5
BoardField D4+
FieldValue D4+
Grouping D4+
Ordering D4+
```

## 80. P2 → P4B gate

Documents may begin from:

```text
Workspace D5
Governance D5
```

without waiting for all WorkManagement core.

## 81. P2 → P4C gate

Billing may begin from:

```text
Account D5
Governance billing-admin contract D4+
```

without waiting for WorkManagement.

## 82. Producer → P5 gate

Per trigger/action:

```text
source event D4+
message delivery D5
```

P5 does not need all source contexts complete.

## 83. Producer → P6 gate

Per metric:

```text
source semantic D5
source reporting/event contract D4+
```

# Non-blocking matrix

## 84. Identity secondary features

These do not block Workspace core:

```text
MFA
API tokens
all OAuth providers
advanced security settings
```

## 85. Governance secondary features

These do not block Board core:

```text
custom roles
advanced share policy
full governance admin UX
```

## 86. WorkManagement views

These do not block Billing:

```text
Calendar
Timeline
Dashboard
Form
```

## 87. Documents

Documents core does not block WorkManagement views.

Collaboration targets on WorkManagement wait only for target contract, not all WorkManagement views.

## 88. Billing provider depth

Full invoice/payment feature depth does not block basic entitlement consumption if the internal Subscription/Entitlement contract is already stable.

## 89. Automation

Automation for WorkManagement does not wait for Documents if it only consumes WorkManagement events.

## 90. Analytics

Analytics WorkManagement reporting does not wait for Automation/Billing if the report does not use them.

The roadmap is therefore a dependency DAG, not a strict linear waterfall.

# Parallel team schedule

## 91. Stage A

Active:

```text
Platform blocker lane
Identity & Accounts
```

Workspace/Governance can prepare specs and source inventory.

## 92. Stage B

After P1 core D4-D5:

```text
Workspace & Governance — primary
Identity & Accounts — secondary security capabilities
Platform — targeted support
```

## 93. Stage C

After P2 core D4-D5:

```text
Work Management Core — primary

parallel:
Billing core
Documents core preparation/implementation
Identity advanced security
Governance secondary capabilities
Platform realtime/messaging preparation
```

## 94. Stage D

After WorkManagement core D4-D5:

```text
WorkManagement Views
Documents & Collaboration
Billing & Entitlements
```

all active in parallel.

Platform closes realtime/messaging/provider blockers just-in-time.

## 95. Stage E

As producer events stabilize:

```text
Automation & Integrations
```

starts per producer contract.

P4 teams continue.

## 96. Stage F

As source/reporting contracts stabilize:

```text
Analytics & Reporting
```

starts per metric/report.

Other teams continue hardening.

# Backend execution artifact model

## 97. Required execution package per team

Every team execution area must eventually have:

```text
<team>.spec.md
<team>.plan.md
<team>.tests.md
<team>.certification.md
```

## 98. SPEC role

The SPEC owns:

- target capability;
- functional/non-functional requirements;
- contracts;
- data ownership;
- security;
- compatibility;
- acceptance criteria;
- non-goals;
- stop conditions.

## 99. PLAN role

The PLAN owns:

- current-state inventory;
- execution order;
- work units;
- affected layers;
- migrations;
- cross-team sequencing;
- PR decomposition;
- commands;
- implementation stop conditions.

## 100. TESTS role

The TESTS document owns:

- requirement-to-test traceability;
- test layers;
- scenario matrix;
- negative cases;
- integration/E2E;
- migration/security/performance testing;
- CI gate mapping.

A requirement without verification evidence is incomplete.

## 101. CERTIFICATION role

The CERTIFICATION document owns the evidence required before the team/capability is declared complete.

It must not pre-fill successful results before execution.

It defines required proof.

# Required test layers

## 102. Domain

Used where Domain invariants exist.

Examples:

- Account;
- Workspace;
- Board;
- Subscription;
- Automation rule.

## 103. Application

Used for:

- command/query orchestration;
- validation;
- authorization declaration;
- idempotency behavior;
- entitlement checks.

## 104. Infrastructure

Used for:

- EF mappings;
- persistence;
- migrations;
- provider adapters;
- secret/reference storage;
- message storage.

## 105. Platform

Used for:

- messaging;
- ordering;
- poison handling;
- idempotency infrastructure;
- realtime mechanisms.

## 106. API

Used for:

- OpenAPI;
- transport;
- auth/authz;
- CSRF;
- validation/error mapping.

## 107. Architecture tests

Used for:

- Domain purity;
- layer dependency;
- bounded-context isolation;
- authorization pipeline invariants;
- project/package architecture.

## 108. Integration

Used for:

- tenant isolation;
- production DI graph;
- DB behavior;
- messaging;
- cross-context contracts;
- provider/webhook integration.

## 109. Security

Used for:

- tenant isolation;
- permission denial;
- CSRF/session;
- secret exposure;
- webhook verification;
- provider credential safety.

## 110. Migration

Used for:

- clean DB;
- upgrade DB;
- backfill;
- compatibility;
- rollback/forward-fix where relevant.

## 111. Performance

Used when capability sits on a critical/high-volume path.

Examples:

- Board loading;
- entitlement evaluation;
- messaging throughput;
- analytics projections.

# CI relationship

## 112. CI is certification evidence, not architecture authority

The roadmap assumes CI verifies accepted architecture and capability tests.

It must not determine business architecture.

## 113. Expected backend gate chain

Conceptually:

```text
quality
↓
architecture-tests
↓
core-tests
↓
platform-tests
↓
api-tests
↓
integration-tests
↓
docker-build
↓
final certification
```

Exact jobs remain governed by CI configuration.

## 114. Non-zero execution

Critical suites must prove actual execution.

A green job with zero relevant tests is not certification evidence.

## 115. Exact SHA rule

A phase/team certification requires required CI green on the exact candidate SHA.

Local success alone is insufficient if CI exercises additional production behavior.

# Cross-team contract change policy

## 116. Producer owns semantic change

When a producer contract changes:

```text
producer
→ owns semantic rationale and compatibility

consumer
→ owns consumption migration

delivery roadmap
→ owns sequencing
```

## 117. Breaking change

A breaking cross-team contract requires:

- impacted consumers;
- migration window;
- compatibility strategy;
- test updates;
- rollout order.

## 118. No hidden coupling

A team MUST NOT introduce:

- private DB reads;
- private EF entity dependencies;
- handler-to-handler cross-context calls;
- provider-specific source-domain fields

merely to avoid defining a contract.

# Data ownership roadmap invariant

## 119. Transactional data

Each bounded context remains owner of its transactional state.

Same database does not imply shared ownership.

## 120. Cross-context consistency

Prefer:

- events;
- explicit Application contracts;
- approved read models;
- orchestration with clear ownership.

## 121. Lifecycle cascades

Account/Workspace/Board/Page deletion effects across contexts must be explicit.

Database cascades must not silently define business behavior.

# Event roadmap invariant

## 122. Event producer stability

Events become dependencies only when:

- meaning is owned;
- payload is stable enough;
- tenant/resource identity exists;
- compatibility is understood.

## 123. Consumer timing

Automation/Analytics consumers may scaffold at D2-D3, but integration hardening waits for D4+ producer contracts.

## 124. Event payload rule

A producer event should express a producer-owned business fact.

It MUST NOT be shaped around one consumer's internal implementation.

# Migration sequencing

## 125. Migration is first-class roadmap work

Migration includes:

```text
database schema
API contract
event contract
stored configuration
provider mapping
frontend generated contracts
read-model rebuild
```

## 126. Database migration

Every schema-affecting capability must include migration evidence before D5.

## 127. Event migration

Breaking events require producer/consumer rollout sequencing.

## 128. Stored configuration migration

Examples:

- Automation rule schema;
- WorkManagement saved filters;
- Block content schema;
- Billing provider config.

## 129. Analytical rebuild

Analytics semantic/schema changes may require rebuild instead of in-place migration.

# Freeze and stability rules

## 130. D5 does not mean immutable forever

D5 means:

```text
stable enough for downstream dependency
```

A later change is possible but requires explicit compatibility handling.

## 131. Freeze after phase exit

Once a critical producer contract reaches D5 and downstream work begins, arbitrary redesign is prohibited.

## 132. Architecture change

If a required feature truly invalidates architecture:

```text
stop
→ architecture decision
→ update authorities
→ migration plan
→ then implementation
```

Do not smuggle architecture redesign inside a feature PR.

# Service extraction sequencing

## 133. No extraction during normal feature sequencing

The roadmap assumes modular monolith delivery.

## 134. Extraction triggers

Consider extraction only with evidence such as:

- independent scaling;
- deployment cadence conflict;
- failure isolation;
- regulatory/security boundary;
- operational ownership;
- stable contracts;
- independent data ownership.

## 135. Team boundary is not service boundary

One team may own two bounded contexts.

One bounded context may remain inside the monolith for years.

No direct equivalence exists between:

```text
team
bounded context
service
```

# Roadmap status tracking

## 136. Allowed roadmap status

Each phase/capability may be marked:

```text
NOT_STARTED
READY
IN_PROGRESS
BLOCKED
VERIFIED
STABLE
```

Do not add daily progress percentages to this canonical roadmap.

## 137. What belongs elsewhere

Do not place:

- sprint tasks;
- individual assignees;
- due dates;
- daily status;
- PR URLs for transient execution

inside this roadmap.

Those belong to issue/project tracking or mutable work items.

# Phase certification summaries

## 138. P0 certification

Required outcome:

```text
secure trusted request context exists
```

Evidence:

- session/CSRF;
- context;
- authz;
- tenant isolation.

## 139. P1 certification

Required outcome:

```text
stable Actor + Account backbone
```

## 140. P2 certification

Required outcome:

```text
stable Workspace + resource authorization backbone
```

## 141. P3 certification

Required outcome:

```text
stable WorkManagement transactional model
```

## 142. P4 certification

Per-lane, not global:

```text
views stable
documents/collaboration stable
billing stable
```

according to their independent contracts.

## 143. P5 certification

Required outcome:

```text
producer-owned events
→ idempotent Automation
→ isolated provider integrations
```

## 144. P6 certification

Required outcome:

```text
source-owned facts
→ rebuildable analytical projections
→ authorized reports
```

# Master backend exit condition

## 145. Backend roadmap considered broadly delivered when

The following are true:

### SaaS core

- Identity/Account D5;
- Workspace/Governance D5;
- tenant isolation D5;
- central authorization D5.

### Work product core

- Board/BoardItem D5;
- Field/value contracts D5;
- ordering/grouping D4-D5;
- core product APIs stable.

### Product expansion

- major views consume canonical WorkManagement state;
- Documents/Collaboration context boundaries stable;
- Billing entitlement contract stable.

### Orchestration/external

- Automation rule/execution stable;
- Integrations connection/provider boundary stable;
- delivery/idempotency/security verified.

### Derived intelligence

- Analytics source contracts stable;
- projections rebuildable;
- report authz/tenant isolation stable.

### Quality

- architecture gates green;
- required core/platform/API/integration tests green;
- migrations safe;
- observability sufficient;
- CI exact SHA green.

## 146. What this roadmap deliberately does not guarantee

This roadmap does not mean:

- every product feature is complete;
- every provider is integrated;
- every analytics report exists;
- every view variant exists;
- microservices are required;
- no future architecture change can occur.

It means:

```text
the backend has a dependency-safe delivery order
and each capability has a clear path to stable parallel development.
```

# Immediate next action

## 147. First execution package to build

The next full execution package should be:

```text
docs/workstreams/executions/identity-accounts/
├── identity-accounts.spec.md
├── identity-accounts.plan.md
├── identity-accounts.tests.md
└── identity-accounts.certification.md
```

## 148. Why this package comes first

Because P1 is the first business capability on the critical dependency spine.

The existing Platform Wave 0 artifacts are prerequisites, not the master backend roadmap and not the first business-team package.

## 149. Package construction order

Recommended documentation construction order:

```text
1. Identity & Accounts
2. Workspace & Governance
3. WorkManagement
4. Documents & Collaboration
5. Billing & Entitlements
6. Automation & Integrations
7. Analytics & Reporting
```

Platform master package may be completed in parallel, but Platform implementation should remain just-in-time against consumer blockers.

## 150. Final operating principle

Use this question before starting any backend capability:

```text
What stable contract does this capability consume?
```

If the answer is unknown:

```text
do not implement yet
```

If the producer contract is D4:

```text
integration may begin
```

If the producer contract is D5:

```text
broad dependent development may proceed
```

If there is no real dependency:

```text
run in parallel
```

This is the roadmap's primary rule.
