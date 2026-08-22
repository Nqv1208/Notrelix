---
document_id: WRK-README
document_type: workstream-router
status: active
owner: engineering-delivery
applies_to:
  - team-workstreams
  - capability-coordination
  - cross-team-dependencies
  - parallel-feature-delivery
evidence:
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
review_on:
  - team-topology-change
  - capability-ownership-change
  - cross-team-dependency-change
  - workstream-lifecycle-change
---

# Workstreams

## 1. Purpose

`docs/workstreams/` contains the mutable coordination knowledge required to execute the current product and architecture safely across multiple teams.

This directory answers questions such as:

- which capabilities are grouped into current workstreams;
- which team is expected to deliver a capability;
- which teams depend on one another;
- which dependency must be ready before another team can continue;
- which execution sequence is safe;
- which architectural blocker affects current delivery.

This directory does **not** own product semantics, system architecture, backend architecture, frontend architecture, or durable engineering policy.

## 2. Authority position

The authority order is:

1. `docs/product/*` — product and bounded-context semantics;
2. `docs/architecture/*` — system boundaries and cross-context architecture;
3. `backend/docs/architecture/*` — backend implementation architecture;
4. `frontend/docs/architecture/*` — frontend implementation architecture;
5. ADRs — accepted durable decisions;
6. `docs/delivery/*` — durable delivery policy and ownership rules;
7. `docs/workstreams/*` — mutable execution decomposition and coordination;
8. source/tests/generated evidence/CI — executable proof.

A workstream document MUST NOT override a higher authority.

If a workstream requires a behavior not supported by current product or architecture authority, the workstream is blocked until the relevant authority is changed through the normal decision process.

## 3. Directory contract

Canonical structure:

```text
docs/workstreams/
├── README.md
├── capability-map.md
├── cross-team-dependencies.md
└── teams/
    ├── identity-accounts.md
    ├── workspace-governance.md
    ├── work-management.md
    ├── documents-collaboration.md
    ├── automation-integrations.md
    ├── billing-entitlements.md
    ├── analytics-reporting.md
    └── platform-foundation.md
```

The `teams/` documents are execution specifications, not permanent architecture documents.

They may evolve as capabilities move from planning to implementation and hardening.

## 4. What belongs here

A workstream document may contain:

- current capability grouping;
- current team assignment;
- delivery order;
- producer/consumer dependency;
- dependency readiness;
- integration handoff;
- known architectural blocker;
- capability-level implementation surfaces;
- test/evidence expectations;
- ownership escalation path;
- service-extraction readiness observations.

A workstream document may say:

```text
Work Management depends on Governance resource authorization at D2.
```

It may also say:

```text
Realtime-heavy Work Management slices are blocked until Platform gap recovery reaches D4.
```

These are execution facts.

## 5. What does not belong here

Do not put the following in `docs/workstreams/`:

- product definition;
- aggregate invariant authority;
- system architecture rules;
- package dependency rules;
- backend layer rules;
- coding conventions;
- long-lived security policy;
- release policy;
- migration policy;
- incident procedure;
- sprint task status;
- individual developer assignments;
- percentage complete;
- temporary daily TODO lists.

Use the appropriate canonical owner instead.

Examples:

```text
What is a BoardItem?
→ docs/product/work-management.md

Can WorkManagement read Governance private tables?
→ docs/architecture/data-ownership-and-consistency.md

Can a frontend feature deep-import another package?
→ frontend/docs/architecture/dependency-boundaries.md

Who owns WorkManagement today?
→ docs/delivery/team-ownership.md

Which WorkManagement slices are being coordinated now?
→ docs/workstreams/teams/work-management.md
```

## 6. Workstreams are intentionally mutable

Unlike architecture and durable delivery policy, workstream documents are expected to change as execution changes.

Examples of legitimate changes:

- a dependency advances from `D2 CONTRACTED` to `D4 VERIFIED`;
- a capability moves from `DEFINED` to `IMPLEMENTING`;
- a workstream is split because delivery scope becomes too large;
- a team takes over a capability from another team;
- an architectural blocker is resolved;
- an integration order changes while preserving authority boundaries.

A workstream edit MUST NOT be used to silently change:

- bounded-context ownership;
- data ownership;
- event ownership;
- API compatibility policy;
- security boundaries;
- deployment architecture;
- frontend dependency architecture.

Those require changes in their actual authority documents.

## 7. Stable ownership versus mutable workstream state

`docs/delivery/team-ownership.md` owns stable organizational responsibility.

`docs/workstreams/*` owns current execution decomposition.

Example:

```text
team-ownership.md
  Work Management Team owns the WorkManagement bounded context.

workstreams/teams/work-management.md
  Current execution order:
  board lifecycle
  → board items
  → fields
  → views
  → realtime hardening
```

Changing the order does not change bounded-context ownership.

Changing the team that owns WorkManagement requires updating `team-ownership.md`.

## 8. Workstream lifecycle

Recommended lifecycle:

```text
DEFINED
→ CONTRACTED
→ IMPLEMENTING
→ INTEGRATING
→ HARDENING
→ DONE
```

Alternative state:

```text
BLOCKED
```

A workstream may contain several capability slices in different states.

The workstream document should not attempt to be a sprint board.

It records only state required to understand execution dependencies.

## 9. Dependency readiness

Cross-team dependencies use:

| Level | Meaning |
|---|---|
| `D0 UNKNOWN` | dependency is suspected but not defined |
| `D1 IDENTIFIED` | producer and consumer are known |
| `D2 CONTRACTED` | contract/event/resource semantics are defined |
| `D3 IMPLEMENTED` | producer implementation exists |
| `D4 VERIFIED` | producer/consumer integration evidence passes |
| `D5 STABLE` | safe for parallel downstream delivery without active redesign |

The detailed rules live in `cross-team-dependencies.md`.

## 10. Required team execution specifications

The following team specs are expected:

### Identity & Accounts

```text
docs/workstreams/teams/identity-accounts.md
```

Owns execution decomposition for:

- Accounts;
- Identity.

### Workspace & Governance

```text
docs/workstreams/teams/workspace-governance.md
```

Owns execution decomposition for:

- Workspaces;
- Governance.

### Work Management

```text
docs/workstreams/teams/work-management.md
```

Owns execution decomposition for:

- WorkManagement.

### Documents & Collaboration

```text
docs/workstreams/teams/documents-collaboration.md
```

Owns execution decomposition for:

- Documents;
- Collaboration.

### Automation & Integrations

```text
docs/workstreams/teams/automation-integrations.md
```

Owns execution decomposition for:

- Automation;
- Integrations.

### Billing & Entitlements

```text
docs/workstreams/teams/billing-entitlements.md
```

Owns execution decomposition for:

- Billing.

### Analytics & Reporting

```text
docs/workstreams/teams/analytics-reporting.md
```

Owns execution decomposition for:

- Analytics / Reporting.

### Platform & Foundation

```text
docs/workstreams/teams/platform-foundation.md
```

Owns execution decomposition for:

- cross-cutting backend mechanisms;
- cross-cutting frontend foundation/runtime/UI mechanisms;
- foundation debt lanes that block product teams.

Platform/Foundation is not a business bounded context.

## 11. Required team spec sections

Each `teams/*.md` file should include:

1. scope;
2. canonical authorities;
3. owned bounded contexts;
4. capabilities;
5. backend ownership surfaces;
6. frontend ownership surfaces;
7. producer dependencies;
8. consumer dependencies;
9. authorization model;
10. data ownership;
11. API/contracts;
12. events/realtime;
13. migrations;
14. test/evidence matrix;
15. delivery sequence;
16. blockers;
17. decisions the team may make locally;
18. decisions requiring escalation;
19. Definition of Done;
20. service-extraction readiness.

The goal is to let a team or coding agent execute without inventing architectural choices.

## 12. No duplicated source inventory

Workstream files SHOULD reference generated inventories instead of copying volatile inventory.

Examples:

```text
backend/docs/generated/project-map.md
frontend/docs/generated/package-boundaries.md
```

Do not manually maintain exact frontend package graphs or complete backend project reference graphs in a workstream file.

## 13. No duplicate product model

A workstream may summarize the capability being delivered, but it MUST link to the canonical product owner.

Bad:

```text
work-management.md defines what a Board means independently.
```

Correct:

```text
work-management.md says the current board-lifecycle implementation slice
is governed by docs/product/work-management.md.
```

## 14. No duplicate architecture rules

A workstream may state:

```text
This slice must use the Application authorization pipeline.
```

But the rule itself remains owned by backend architecture/ADR authority.

A workstream should not create a new competing `BE-*`, `FE-*`, `SYS-*`, or `DOC-*` rule namespace merely to restate existing architecture.

## 15. Relationship to issue/project tracking

Workstream documentation is not an issue tracker.

Issues/project boards own:

- assignee;
- due date;
- sprint;
- task status;
- review status;
- merge status.

Workstream docs own:

- why a capability is grouped;
- ownership;
- sequencing;
- dependencies;
- execution boundary;
- architectural blockers;
- completion evidence.

Do not add daily task churn to this directory.

## 16. Relationship to service extraction

A team spec may identify a bounded context as an extraction candidate.

It MUST NOT create a new deployable service merely because:

- the team is large;
- the context has many files;
- parallel delivery is difficult;
- a microservice sounds cleaner.

Extraction requires the architecture criteria defined by the system extraction strategy and an accepted decision when required.

## 17. Review behavior

Review a workstream document when:

- a team ownership change occurs;
- a capability is split across teams;
- a new producer/consumer dependency appears;
- a dependency changes integration style;
- a major capability reaches `D5 STABLE`;
- a foundation blocker is resolved;
- an extraction proposal appears.

Do not schedule review merely because a calendar interval elapsed if the execution topology did not change.

## 18. Retirement behavior

Workstream documents are expected to be retired or rewritten when their coordination purpose ends.

Do not preserve historical execution plans as active authority.

Historical delivery evidence may live in issue/PR history or an explicitly non-authoritative archive if required.

A completed workstream SHOULD be reduced to durable knowledge elsewhere when that knowledge remains useful.

## 19. Entry points

For stable team ownership:

```text
../delivery/team-ownership.md
```

For capability decomposition:

```text
./capability-map.md
```

For cross-team dependencies:

```text
./cross-team-dependencies.md
```

For team execution:

```text
./teams/
```

## 20. Phase 2 objective

The purpose of this directory during Phase 2 is to make parallel delivery safe.

Phase 2 is successful when:

- every bounded context has an accountable team;
- every major capability has a workstream owner;
- cross-team dependencies are explicit;
- teams know which contracts they produce and consume;
- architecture decisions are not delegated to coding agents;
- Platform/Foundation blockers are visible;
- teams can begin independent feature delivery without changing the frozen foundation by accident.
