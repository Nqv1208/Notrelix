# Notrelix Documentation

> **Repository-level canonical index for Notrelix engineering, product, governance, quality, delivery, operations, infrastructure, decisions, templates, and generated documentation.**
>
> This directory owns **cross-stack and repository-wide knowledge**.
>
> It does **not** duplicate backend implementation architecture or frontend implementation architecture.

The documentation system follows:

> **one topic → one canonical normative owner**

The repository should be large where the system is semantically deep and small where a document only needs to route.

The objective is not to minimize Markdown.

The objective is:

- minimal authority ambiguity;
- sufficient implementation depth;
- explicit ownership;
- controlled change;
- executable evidence;
- reliable use by humans and Coding Agents.

---

# 1. Start here

Choose the path that matches your task.

## Product or business-semantic change

Read:

```text
../PRODUCT.md
product/README.md
product/product-model.md
product/contexts/<owning-context>.md
```

Then read affected backend/frontend/system owners.

---

## Repository-wide architecture change

Read:

```text
../RULE.md
../AGENTS.md
governance/documentation-authority.md
architecture/system-overview.md
```

Then the specific cross-stack architecture owner.

---

## Backend implementation change

Do not search this repository `docs/` tree for backend implementation rules first.

Start at:

```text
../backend/AGENTS.md
../backend/docs/README.md
```

Then use:

```text
../backend/docs/architecture/<concern>.md
```

---

## Frontend implementation change

Start at:

```text
../frontend/AGENTS.md
../frontend/docs/README.md
```

Then use:

```text
../frontend/docs/architecture/<concern>.md
```

---

## Breaking contract or migration

Read:

```text
delivery/change-classification.md
delivery/change-impact-and-migration.md
architecture/contract-boundaries.md
```

Then the affected producer/consumer/data owner.

---

## Security / tenancy

Start with:

```text
../RULE.md
    NRX-003
    NRX-004
    NRX-012

product/contexts/identity.md
product/contexts/workspaces.md
product/contexts/governance.md
quality/security-quality-standard.md
```

Then:

```text
../backend/docs/architecture/security-tenancy-authorization.md
```

and affected frontend/security UX owners.

---

## Documentation-core change

Read:

```text
governance/documentation-authority.md
governance/documentation-lifecycle.md
governance/topic-authority-map.md
governance/decision-and-exception-policy.md
governance/documentation-quality-gates.md
```

Documentation authority changes are architecture changes.

---

# 2. Documentation authority planes

Notrelix documentation is divided by semantic responsibility.

```text
Repository root
    constitution, orientation, current context, execution routing

docs/
    cross-stack system architecture
    product semantics
    repository governance
    repository quality
    delivery/change management
    operations
    infrastructure/deployment
    system decisions
    reusable documentation templates
    generated documentation evidence

backend/docs/
    backend implementation architecture
    backend operations
    backend decisions
    backend generated evidence

frontend/docs/
    frontend implementation architecture
    frontend decisions
    frontend generated evidence

source/tests/manifests/contracts/migrations/CI
    executable evidence
```

No plane may redefine a topic owned by another plane merely because the topic is related.

---

# 3. Root documentation relationship

The repository root owns high-level constitutions and routers.

| Root file | Role |
|---|---|
| [`../README.md`](../README.md) | Product/repository onboarding and operational entry point |
| [`../PRODUCT.md`](../PRODUCT.md) | Repository-level product constitution |
| [`../DESIGN.md`](../DESIGN.md) | Product design constitution |
| [`../RULE.md`](../RULE.md) | Repository-wide invariants |
| [`../AGENTS.md`](../AGENTS.md) | Coding Agent execution contract |
| [`../CONTEXT.md`](../CONTEXT.md) | Current repository facts and active transitions |
| [`../CONTEXT-MAP.md`](../CONTEXT-MAP.md) | Task → canonical authority routing |
| `../CLAUDE.md` | Provider compatibility router only |

This `docs/README.md` is the canonical index for the repository-level documentation tree.

It must not become a duplicate root README or root rulebook.

---

# 4. Target documentation tree

```text
docs/
├── README.md
│
├── governance/
│   ├── documentation-authority.md
│   ├── documentation-lifecycle.md
│   ├── topic-authority-map.md
│   ├── decision-and-exception-policy.md
│   └── documentation-quality-gates.md
│
├── architecture/
│   ├── system-overview.md
│   ├── bounded-context-map.md
│   ├── contract-boundaries.md
│   ├── data-ownership-and-consistency.md
│   ├── events-realtime-and-delivery-boundary.md
│   └── capability-extraction-strategy.md
│
├── product/
│   ├── README.md
│   ├── product-model.md
│   ├── product-experience.md
│   └── contexts/
│       ├── accounts.md
│       ├── identity.md
│       ├── workspaces.md
│       ├── governance.md
│       ├── work-management.md
│       ├── documents.md
│       ├── collaboration.md
│       ├── automation.md
│       ├── integrations.md
│       ├── billing.md
│       └── analytics.md
│
├── quality/
│   ├── engineering-quality-standard.md
│   ├── testing-strategy.md
│   ├── security-quality-standard.md
│   ├── accessibility-standard.md
│   └── performance-and-scalability.md
│
├── delivery/
│   ├── change-classification.md
│   ├── change-impact-and-migration.md
│   ├── definition-of-done.md
│   └── release-rollout-and-recovery.md
│
├── operations/
│   ├── observability.md
│   ├── incident-readiness.md
│   ├── recovery-and-data-safety.md
│   └── service-degradation.md
│
├── infrastructure/
│   ├── environment-model.md
│   ├── deployment-runtime.md
│   └── containerization-and-local-services.md
│
├── decisions/
│   ├── README.md
│   └── SYS-ADR-*.md
│
├── templates/
│   ├── adr-template.md
│   ├── architecture-change-template.md
│   ├── feature-spec-template.md
│   ├── migration-plan-template.md
│   ├── incident-template.md
│   └── pr-checklist.md
│
└── generated/
    ├── document-index.md
    └── rule-index.md
```

This is the repository-level tree only.

Backend/frontend implementation architecture remains outside this tree.

---

# 5. Document classes

Every documentation file should have one primary class.

A file may reference another class.

It should not attempt to perform several conflicting roles.

---

## 5.1 Constitution

Constitutions define foundational repository/product constraints.

Primary examples:

```text
../PRODUCT.md
../DESIGN.md
../RULE.md
```

Properties:

- stable;
- normative;
- high semantic impact;
- changed deliberately;
- broad scope;
- not implementation inventories.

A constitution does not need to list every implementation detail.

---

## 5.2 Architecture

Architecture documents define current intended durable engineering contracts.

Examples:

```text
architecture/*.md
../backend/docs/architecture/*.md
../frontend/docs/architecture/*.md
```

Architecture documents answer:

- ownership;
- boundary;
- allowed/forbidden dependency/behavior;
- consistency model;
- failure model;
- change impact;
- evidence.

They are normative for their topic.

---

## 5.3 Product context

Product context documents define business meaning independently of implementation technology.

Examples:

```text
product/contexts/work-management.md
product/contexts/governance.md
product/contexts/billing.md
```

They answer:

- mission;
- vocabulary;
- ownership;
- lifecycle;
- invariants;
- cross-context responsibility;
- user-visible semantics.

Backend/frontend implement them.

Backend/frontend do not independently redefine them.

---

## 5.4 Governance

Governance documents define how documentation/decisions/exceptions are owned, evolved, verified, and retired.

Examples:

```text
governance/documentation-authority.md
governance/documentation-lifecycle.md
```

Governance should remain more stable than implementation architecture.

---

## 5.5 Standard

Quality standards define repository-wide expectations such as:

- testing philosophy;
- accessibility;
- security quality;
- engineering quality;
- performance/scalability.

They do not replace project-specific implementation/testing contracts.

---

## 5.6 Delivery policy

Delivery documents define how changes move safely through:

```text
classification
impact analysis
migration
definition of done
rollout
recovery
```

They are process/engineering contracts.

They are not project roadmaps.

---

## 5.7 Operations / runbook

Operations documents define:

- observation;
- incident behavior;
- recovery;
- degradation;
- runtime environment;
- deployment/runtime expectations.

A runbook describes action/procedure.

It does not redefine product architecture.

---

## 5.8 ADR

ADR means Architecture Decision Record.

An ADR records:

> Why did we make this consequential decision?

It does not replace the current architecture handbook.

Accepted ADR history is preserved.

Later decisions supersede rather than silently rewrite it.

---

## 5.9 Template

Templates provide reusable structure.

A template is not normative architecture merely because it contains normative-looking headings.

A template MAY reference canonical standards.

It MUST NOT define new architecture.

---

## 5.10 Generated evidence

Generated docs expose machine-derived exact inventory.

Examples:

```text
generated/document-index.md
generated/rule-index.md
../backend/docs/generated/project-map.md
../frontend/docs/generated/package-boundaries.md
```

Generated files:

- have producers;
- are reproducible;
- are drift-checked;
- are not manually edited;
- do not own design rationale.

---

## 5.11 Current-state context

Current-state facts belong primarily to:

```text
../CONTEXT.md
../backend/CONTEXT.md
```

Do not create current-state snapshots throughout `docs/` unless the scope truly requires a distinct operational current-state file.

---

# 6. Normative versus evidence

Notrelix distinguishes:

```text
Normative intent
vs
Executable evidence
```

## Normative intent

Owned by:

- PRODUCT;
- RULE;
- DESIGN;
- canonical product docs;
- canonical architecture docs;
- standards/policies;
- accepted current decisions.

## Executable evidence

Owned by:

- source;
- tests;
- project/package manifests;
- OpenAPI/contracts;
- migrations;
- generated outputs;
- CI.

Neither side silently wins when they disagree.

The mismatch must be classified.

See:

```text
../AGENTS.md
../CONTEXT.md
```

for:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

---

# 7. Do not read the whole documentation tree by default

Notrelix documentation is intentionally deep.

Depth is not a request to load every file for every task.

Use:

```text
../CONTEXT-MAP.md
```

to choose the minimum authoritative set.

Example:

```text
Task:
change frontend query key for a Workspace-scoped Board list

Read:
../RULE.md
    NRX-003
    NRX-014

product/contexts/work-management.md
product/contexts/workspaces.md

../frontend/AGENTS.md
../frontend/docs/architecture/state-query-mutations.md

Executable evidence:
frontend architecture/query source/tests
```

Do not also read Billing, Documents, deployment runtime, incident recovery, and every ADR unless the change actually touches them.

---

# 8. Governance documentation

Directory:

```text
governance/
```

Purpose:

> define how engineering/product/documentation truth is owned and evolved.

---

## 8.1 Documentation authority

File:

```text
governance/documentation-authority.md
```

Owns:

- authority planes;
- document classes;
- canonical owner;
- summary vs definition;
- generated evidence;
- conflict model;
- scoped-document admission;
- prohibited competing authorities.

Read this file first when:

- two documents appear canonical;
- a new documentation layer is proposed;
- backend/frontend rules are being moved;
- a root file is becoming too detailed;
- generated and authored docs overlap.

---

## 8.2 Documentation lifecycle

File:

```text
governance/documentation-lifecycle.md
```

Owns:

```text
DRAFT
ACTIVE
SUPERSEDED
GENERATED
```

and review/removal rules.

A document is not marked `FROZEN` merely because its architecture is stable.

Architecture can be protected while documentation remains maintainable.

---

## 8.3 Topic authority map

File:

```text
governance/topic-authority-map.md
```

Owns the auditable mapping:

```text
Topic
→ canonical owner
→ evidence
→ decision registry
→ owner/team
```

`CONTEXT-MAP.md` routes **tasks**.

`topic-authority-map.md` records **topic ownership**.

They must agree.

---

## 8.4 Decision and exception policy

File:

```text
governance/decision-and-exception-policy.md
```

Owns:

- ADR trigger;
- architecture decision threshold;
- temporary exception;
- owner;
- risk;
- compensating control;
- review/expiry trigger;
- removal/normalization.

An exception does not become precedent simply because source contains it.

---

## 8.5 Documentation quality gates

File:

```text
governance/documentation-quality-gates.md
```

Owns:

- required docs checks;
- link policy;
- authority check;
- metadata check;
- ID uniqueness;
- source inventory alignment;
- generated drift;
- CI behavior.

---

# 9. System architecture documentation

Directory:

```text
architecture/
```

These files own concerns crossing product/backend/frontend/runtime boundaries.

They MUST NOT become copies of backend/frontend implementation architecture.

---

## 9.1 System overview

File:

```text
architecture/system-overview.md
```

Owns:

- overall system boundary;
- modular monolith position;
- multi-host frontend;
- trust boundaries;
- external-system categories;
- server-authoritative product state;
- cross-stack integration shape;
- system non-goals.

References backend/frontend overviews instead of re-documenting their internals.

---

## 9.2 Bounded-context map

File:

```text
architecture/bounded-context-map.md
```

Owns:

- accepted business contexts;
- context ownership relation;
- upstream/downstream dependencies;
- extraction seams;
- technical-capability versus business-context distinction.

Detailed business meaning remains in:

```text
product/contexts/*.md
```

---

## 9.3 Contract boundaries

File:

```text
architecture/contract-boundaries.md
```

Owns cross-stack contract classes:

- REST/OpenAPI;
- realtime;
- integration/public events;
- message envelope;
- generated frontend client/types;
- package exports;
- provider/webhook compatibility where system-wide.

It defines:

- producer;
- consumer;
- compatibility;
- versioning;
- deprecation;
- migration responsibility.

---

## 9.4 Data ownership and consistency

File:

```text
architecture/data-ownership-and-consistency.md
```

Owns:

- authoritative owner;
- aggregate/local transaction;
- cross-context eventual consistency;
- projections/read models;
- cache as derived state;
- frontend cache as projection;
- saga/process-manager admission;
- idempotency/retry relationship;
- no accidental distributed transaction assumption.

---

## 9.5 Events, realtime, and delivery boundary

File:

```text
architecture/events-realtime-and-delivery-boundary.md
```

Owns taxonomy among:

```text
Domain event
Integration/public event
Outbox record
Message envelope
Realtime notification
Activity
Audit
```

Detailed backend delivery remains Platform-owned.

Detailed frontend reconciliation remains frontend realtime/state-owned.

---

## 9.6 Capability extraction strategy

File:

```text
architecture/capability-extraction-strategy.md
```

Owns:

- modular monolith now;
- bounded contexts as extraction seams;
- extraction trigger;
- contract/data/runtime prerequisites;
- anti-premature-microservice rule.

A bounded context does not automatically require a current service.

---

# 10. Product documentation

Directory:

```text
product/
```

Product docs define business meaning independent of backend/frontend implementation.

This is one of the deepest canonical areas in the repository.

---

## 10.1 Product index

File:

```text
product/README.md
```

Owns:

- product-doc reading path;
- product-context index;
- product semantic change process;
- relationship to root PRODUCT.

---

## 10.2 Product model

File:

```text
product/product-model.md
```

Owns cross-context product-level vocabulary and capability relationships.

It should not duplicate each context's full invariants.

---

## 10.3 Product experience

File:

```text
product/product-experience.md
```

Owns product-experience semantics that are broader than visual design but more detailed than root PRODUCT.

Examples:

- long-session work behavior;
- coherence;
- product language;
- state integrity;
- enterprise interaction expectations.

Root DESIGN remains design constitution.

---

# 11. Product contexts

Each context document is the canonical product owner for its semantic area.

Required contexts:

```text
product/contexts/accounts.md
product/contexts/identity.md
product/contexts/workspaces.md
product/contexts/governance.md
product/contexts/work-management.md
product/contexts/documents.md
product/contexts/collaboration.md
product/contexts/automation.md
product/contexts/integrations.md
product/contexts/billing.md
product/contexts/analytics.md
```

Each context must define, as relevant:

```text
Mission
Owns
Does not own
Ubiquitous language
Core objects/capabilities
Invariants
Lifecycle
Tenant/authorization implications
Cross-context contracts
Events/facts exposed
User journeys
Deletion/archive semantics
Failure/conflict semantics
Frontend representation implications
Analytics implications
Forbidden shortcuts
Change/test expectations
```

The context may omit a section that genuinely does not apply.

It must not fill template headings with generic prose.

---

# 12. Product-context ownership versus implementation

Example:

```text
product/contexts/work-management.md
    owns Board / BoardField / BoardItem / BoardGroup / BoardView meaning

backend/docs/architecture/domain-modeling.md
    owns how Domain models invariants

backend/docs/architecture/application-model.md
    owns how use cases orchestrate

frontend/docs/architecture/state-query-mutations.md
    owns query/mutation/cache behavior

frontend/docs/architecture/ui-and-design-system.md
    owns frontend UI implementation
```

No implementation document may redefine:

```text
BoardGroup = status
```

if the owning product context says otherwise.

---

# 13. Quality documentation

Directory:

```text
quality/
```

These are repository-wide standards.

They do not contain exact backend/frontend suite inventories when those inventories are project-owned.

---

## 13.1 Engineering quality standard

File:

```text
quality/engineering-quality-standard.md
```

Owns shared expectations for:

- ownership clarity;
- complexity;
- dependency hygiene;
- error handling;
- dead compatibility;
- observability;
- architecture-aware review;
- documentation-as-code.

---

## 13.2 Testing strategy

File:

```text
quality/testing-strategy.md
```

Owns shared test philosophy:

- behavior vs implementation;
- Domain/unit;
- integration;
- architecture;
- contract;
- E2E;
- deterministic fixtures;
- flaky-test policy;
- non-zero-work rule.

Exact project/suite commands remain under backend/frontend docs.

---

## 13.3 Security quality standard

File:

```text
quality/security-quality-standard.md
```

Owns repository-wide security engineering expectations:

- secret safety;
- sensitive data;
- dependency/security hygiene;
- logging;
- auth/authz separation;
- secure testing;
- vulnerability handling.

Detailed backend auth/RLS lives under backend security docs.

---

## 13.4 Accessibility standard

File:

```text
quality/accessibility-standard.md
```

Owns repository-wide accessibility expectations.

Root DESIGN owns design semantics.

Frontend UI/testing docs own implementation/proof.

---

## 13.5 Performance and scalability

File:

```text
quality/performance-and-scalability.md
```

Owns shared principles:

- bounded queries;
- pagination;
- indexes/queryability;
- cache discipline;
- payload size;
- realtime fan-out;
- large-data frontend behavior;
- performance evidence.

Do not invent organization-level SLO numbers here unless they have been explicitly decided.

---

# 14. Delivery documentation

Directory:

```text
delivery/
```

Delivery docs describe how changes safely evolve the system.

They are not roadmaps.

---

## 14.1 Change classification

File:

```text
delivery/change-classification.md
```

Classifies:

```text
local refactor
behavior change
product semantic change
public contract change
schema/data change
architecture change
security change
operational change
```

Each class maps to required owners/evidence.

---

## 14.2 Change impact and migration

File:

```text
delivery/change-impact-and-migration.md
```

Owns:

- producer/consumer inventory;
- compatibility classification;
- expand/contract;
- staged rollout;
- backfill;
- deprecation;
- cleanup;
- migration proof.

---

## 14.3 Definition of done

File:

```text
delivery/definition-of-done.md
```

Owns repository-wide completion expectations:

- intended behavior;
- tests;
- architecture/security gates;
- docs;
- generated artifacts;
- migration;
- rollout;
- observability;
- cleanup.

Compilation is not definition of done.

---

## 14.4 Release, rollout, and recovery

File:

```text
delivery/release-rollout-and-recovery.md
```

Owns shared release-change principles.

Exact environment/deployment mechanics route to infrastructure/operations owners.

---

# 15. Operations documentation

Directory:

```text
operations/
```

These files define repository-level operational behavior.

---

## 15.1 Observability

File:

```text
operations/observability.md
```

Owns:

- logs;
- metrics;
- traces;
- correlation;
- operational identifiers;
- sensitive-data limits;
- observability ownership.

---

## 15.2 Incident readiness

File:

```text
operations/incident-readiness.md
```

Owns:

- incident classification;
- roles;
- diagnosis workflow;
- evidence;
- escalation;
- recovery decision flow.

---

## 15.3 Recovery and data safety

File:

```text
operations/recovery-and-data-safety.md
```

Owns:

- backup/restore principles;
- data corruption response;
- recovery verification;
- destructive-event handling.

Detailed DB migration mechanics remain backend-owned.

---

## 15.4 Service degradation

File:

```text
operations/service-degradation.md
```

Owns expected system behavior when dependencies degrade.

Examples:

- Redis unavailable;
- RabbitMQ/messaging unavailable;
- provider outage;
- realtime outage;
- read-only/degraded mode.

It does not invent product behavior that the owning product context has not approved.

---

# 16. Infrastructure documentation

Directory:

```text
infrastructure/
```

Repository infrastructure docs cover cross-stack runtime/deployment environment.

They must not duplicate backend Application/Infrastructure project architecture.

---

## 16.1 Environment model

File:

```text
infrastructure/environment-model.md
```

Owns:

- local/development/staging/production environment concepts;
- configuration/secrets flow at repository level;
- environment isolation;
- runtime expectations.

Exact values live in source/environment manifests.

---

## 16.2 Deployment runtime

File:

```text
infrastructure/deployment-runtime.md
```

Owns:

- high-level deployed runtime;
- gateway;
- service/container relationships;
- rollout topology;
- deployment dependencies.

---

## 16.3 Containers and local services

File:

```text
infrastructure/containerization-and-local-services.md
```

Owns:

- local container model;
- Compose role;
- local service dependencies;
- optional tooling profiles;
- development runtime boundaries.

Exact current commands remain Makefile/Compose-owned.

---

# 17. Decision registries

Repository-wide/system decisions live under:

```text
decisions/
```

Index:

```text
decisions/README.md
```

System ADR IDs:

```text
SYS-ADR-001
SYS-ADR-002
...
```

Backend decisions remain:

```text
../backend/docs/decisions/
```

Frontend decisions remain:

```text
../frontend/docs/decisions/
```

Do not move project-specific ADRs into system scope merely to have one ADR folder.

Decision scope should match the scope of the choice.

---

# 18. ADR behavior

An ADR should answer:

```text
What problem/context existed?
What did we decide?
What alternatives were considered?
Why?
What consequences follow?
What compatibility/migration is required?
What evidence supports the decision?
What supersedes what?
```

An ADR should not contain every current implementation rule.

Current architecture belongs in architecture documents.

---

# 19. Templates

Directory:

```text
templates/
```

Templates exist to make high-impact work consistent without duplicating architecture.

Target templates:

```text
adr-template.md
architecture-change-template.md
feature-spec-template.md
migration-plan-template.md
incident-template.md
pr-checklist.md
```

Templates must reference canonical owners.

They must not become a second rule system.

---

# 20. Generated documentation

Directory:

```text
generated/
```

Target repository-generated docs:

```text
document-index.md
rule-index.md
```

Project-generated docs remain in their project trees.

---

## 20.1 Document index

`generated/document-index.md` should be generated from canonical document metadata.

Suggested fields:

```text
Document ID
Type
Owner
Status
Path
Applies To
```

Do not hand-edit.

---

## 20.2 Rule index

`generated/rule-index.md` should be generated from stable rule IDs.

Suggested fields:

```text
Rule ID
Title
Owner document
```

Do not hand-edit.

---

# 21. Backend documentation relationship

Backend docs are not a subsection of this tree because their authority is project-specific.

Start:

```text
../backend/docs/README.md
```

Target backend structure:

```text
backend/docs/
├── README.md
├── architecture/
├── operations/
├── decisions/
└── generated/
```

Repository-level docs MAY reference backend implementation contracts.

They MUST NOT copy the same detailed backend rules.

Example:

```text
docs/architecture/data-ownership-and-consistency.md
    says cross-context eventual consistency is explicit

backend/docs/architecture/platform-and-messaging.md
    defines actual delivery/idempotency/order mechanisms
```

---

# 22. Frontend documentation relationship

Start:

```text
../frontend/docs/README.md
```

Target frontend structure:

```text
frontend/docs/
├── README.md
├── architecture/
├── decisions/
└── generated/
```

Repository docs own shared product/system semantics.

Frontend docs own client implementation architecture.

---

# 23. Generated frontend evidence

Exact package architecture is not manually maintained in repository-level docs.

Use:

```text
../frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

and generated:

```text
../frontend/docs/generated/package-boundaries.md
```

for exact package-boundary evidence.

---

# 24. Generated backend evidence

Exact backend project inventory should be generated from:

```text
../backend/backend.slnx
../backend/**/*.csproj
```

Target:

```text
../backend/docs/generated/project-map.md
```

Repository prose should not hard-code exact project-reference tables when generated evidence can serve that role.

---

# 25. Reading paths by role

## 25.1 New contributor

Recommended:

```text
../README.md
→ ../PRODUCT.md
→ ../RULE.md
→ ../backend/README.md or ../frontend/README.md
→ relevant canonical docs
→ source/tests
```

Read `DESIGN.md` for user-facing work.

## 25.2 Coding Agent

Recommended:

```text
../RULE.md
→ ../AGENTS.md
→ scoped AGENTS.md
→ ../CONTEXT-MAP.md
→ owning product/topic docs
→ source/tests/manifests/contracts
→ required evidence/gates
```

## 25.3 Product/architecture reviewer

Recommended:

```text
../PRODUCT.md
→ ../RULE.md
→ architecture/
→ product/
→ relevant project architecture
→ ADRs
→ executable evidence
```

## 25.4 Security reviewer

Recommended:

```text
../RULE.md
→ quality/security-quality-standard.md
→ product/contexts/identity.md
→ product/contexts/workspaces.md
→ product/contexts/governance.md
→ backend security docs
→ affected frontend/docs/runtime evidence
```

## 25.5 Release/migration reviewer

Recommended:

```text
delivery/change-classification.md
→ delivery/change-impact-and-migration.md
→ affected contract/data owner
→ release-rollout-and-recovery.md
→ operations/infrastructure as required
```

---

# 26. Reading paths by change type

## Domain invariant

```text
../PRODUCT.md
→ product/contexts/<owner>.md
→ ../RULE.md
→ ../backend/AGENTS.md
→ ../backend/docs/architecture/domain-modeling.md
→ source/tests
```

## Backend use case / pipeline

```text
product owner
→ ../RULE.md
→ ../backend/AGENTS.md
→ ../backend/docs/architecture/application-model.md
→ security/consistency owner if affected
→ source/tests
```

## Data/schema change

```text
product owner
→ delivery/change-impact-and-migration.md
→ ../backend/docs/architecture/infrastructure-and-data.md
→ ../backend/docs/operations/migrations-and-data-change.md
→ migrations/tests
```

## Message/retry/outbox change

```text
product owner
→ architecture/data-ownership-and-consistency.md
→ architecture/events-realtime-and-delivery-boundary.md
→ ../backend/docs/architecture/platform-and-messaging.md
→ source/tests/integration evidence
```

## Frontend server-state change

```text
product owner
→ ../RULE.md
→ ../frontend/AGENTS.md
→ ../frontend/docs/architecture/state-query-mutations.md
→ realtime if affected
→ source/tests
```

## Frontend UI change

```text
product owner
→ ../DESIGN.md
→ ../frontend/docs/architecture/ui-and-design-system.md
→ host/state docs if relevant
→ source/tests/a11y
```

## Package-boundary change

```text
../RULE.md
→ ../frontend/AGENTS.md
→ ../frontend/docs/architecture/dependency-boundaries.md
→ architecture-change-policy.md
→ executable architecture manifest
→ architecture checks
```

## Product semantic change

```text
../PRODUCT.md
→ product/contexts/<owner>.md
→ architecture/bounded-context-map.md if ownership changes
→ affected backend/frontend docs
→ delivery/change-impact-and-migration.md
→ ADR if consequential
```

---

# 27. Documentation metadata

Canonical authored docs under this tree should use lightweight metadata where governance requires it.

Recommended schema:

```yaml
---
document_id: SYS-DATA-CONSISTENCY
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
evidence:
  - backend/src
  - backend/tests
  - frontend/packages
review_on:
  - cross-context-consistency-change
  - contract-boundary-change
---
```

Do not add metadata fields that have no automated/governance purpose.

Do not use a stale source SHA as an automatic proof that every sentence is current.

Exact source alignment should be checked through evidence/generators where practical.

---

# 28. Document IDs

Canonical authored docs SHOULD have stable IDs once governance tooling depends on them.

IDs should describe ownership, not filename generation.

Examples:

```text
DOC-AUTHORITY
SYS-OVERVIEW
SYS-CONTEXT-MAP
PROD-WORK-MANAGEMENT
QLT-TESTING
DEL-MIGRATION
OPS-OBSERVABILITY
INFRA-ENVIRONMENT
```

Do not rename an ID casually after references/tooling depend on it.

---

# 29. Rule IDs

Stable rule IDs are used only where cross-reference value justifies them.

Repository constitution:

```text
NRX-*
```

Documentation governance:

```text
DOC-*
```

System architecture:

```text
SYS-*
```

Product:

```text
PROD-*
```

Backend:

```text
BE-*
```

Frontend:

```text
FE-*
```

Quality/delivery/operations/infrastructure have their own prefixes.

Do not assign IDs to every paragraph.

---

# 30. Relative links

Authored repository docs MUST use repository-relative links.

Forbidden:

```text
file:///Users/...
/home/<developer>/...
C:\Users\...
```

Do not link to a developer workstation as documentation authority.

---

# 31. Source references

Canonical architecture docs should reference evidence using repository paths and meaningful symbols/types where useful.

Example:

```text
Evidence:
- backend/src/Notrelix.Application/...
- backend/tests/Notrelix.Application.Tests/...
```

A source reference does not mean every source detail is normative architecture.

It proves current implementation/evidence for the relevant claim.

---

# 32. Generated references

A generated document must state:

```text
Producer:
Command:
Do not edit:
Drift check:
```

Example concept:

```text
Producer:
frontend/tooling/dependency-rules/src/architecture-manifest.ts

Command:
pnpm --filter @notrelix/dependency-rules docs:generate

Do not edit:
yes
```

Use the actual current producer/command.

Do not copy example commands blindly.

---

# 33. Status lifecycle

Canonical authored documentation should use lifecycle statuses defined by:

```text
governance/documentation-lifecycle.md
```

Target statuses:

```text
draft
active
superseded
```

Generated files use:

```text
generated
```

Do not use:

```text
final
v4-final
enterprise-final
freeze-v3
canonical-final
```

as filename lifecycle management.

---

# 34. “Frozen” architecture

A foundation can be protected/frozen as an engineering contract.

That does not mean its Markdown file cannot be edited.

When a protected architecture changes:

- classify the change;
- update ADR/contract if required;
- migrate consumers;
- update tests/gates;
- update canonical docs.

Documentation lifecycle and architecture maturity are different dimensions.

---

# 35. What must not live in the active canonical tree

The active canonical tree MUST NOT contain roadmap/audit/migration snapshots as architecture owners.

Examples that should live in issue/project/Git history rather than canonical docs after completion:

```text
roadmap
freeze plan
wave plan
migration tracker
readiness audit
one-time code audit
historical baseline report
implementation progress percentage
```

Durable knowledge extracted from them belongs in canonical docs/ADR.

Unresolved work belongs in an issue/project tracker.

---

# 36. No active archive tree by default

Do not create:

```text
docs/archive/
docs/legacy/
docs/old/
```

merely to keep obsolete Markdown visible.

Git history is the default archive.

Keep historical docs active only when they have ongoing operational/legal/documentation value.

---

# 37. No duplicate backend/frontend handbooks here

Do not create:

```text
docs/backend/
docs/frontend/
```

as repository-level implementation architecture when these already exist:

```text
backend/docs/
frontend/docs/
```

Cross-stack docs may discuss backend/frontend interaction.

They must route detailed implementation to the project owner.

---

# 38. Do not create files by symmetry

Not every directory needs:

```text
README.md
RULE.md
AGENTS.md
CONTEXT.md
```

A document exists because it has:

- distinct semantic ownership;
- distinct lifecycle;
- distinct evidence/enforcement;
- enough depth to justify discoverability.

Not because another sibling directory has one.

---

# 39. Documentation depth

Notrelix does not optimize documentation by line count.

Deep files are expected where the system is deep.

Examples likely to be deep:

```text
product/contexts/work-management.md
../backend/docs/architecture/domain-modeling.md
../backend/docs/architecture/application-model.md
../backend/docs/architecture/platform-and-messaging.md
../frontend/docs/architecture/state-query-mutations.md
../frontend/docs/architecture/realtime.md
```

Index/router files should remain focused.

`docs/README.md` is an index and taxonomy/authority explanation.

It should not copy those deep contracts.

---

# 40. Architecture document minimum contract

Every canonical architecture file should answer, as applicable:

```text
Scope
Owns
Does not own
Why boundary exists
Current intended architecture
Normative invariants
Allowed
Forbidden
Failure modes
Consistency/concurrency/retry if relevant
Contract surfaces
Change impact
Evidence
Related decisions
Non-responsibilities
```

Do not fill irrelevant sections with generic text.

---

# 41. Product-context minimum contract

Every product-context file should answer, as applicable:

```text
Mission
Owns
Does not own
Ubiquitous language
Core concepts
Invariants
Lifecycle
Scope/tenant implications
Authorization meaning
Cross-context dependencies
Facts/events exposed
User journeys
Failure/conflict semantics
Deletion/archive semantics
Analytics implications
Frontend representation implications
Forbidden semantic shortcuts
Change/test expectations
```

---

# 42. Quality/standard minimum contract

A standard should answer:

```text
Scope
Why it matters
Required standard
Prohibited behavior
Evidence
Exceptions
Project-specific owners
```

Do not duplicate project-level exact scripts in repository standard unless needed for repository integration.

---

# 43. Runbook minimum contract

A runbook should answer:

```text
Trigger
Impact
Preconditions
Diagnosis
Actions
Validation
Escalation
Recovery
Post-incident follow-up
Safety constraints
```

Do not turn a runbook into architecture rationale.

---

# 44. Template minimum contract

A template should:

- make required decisions visible;
- link to canonical rules;
- distinguish required/optional sections;
- include stop/unknown sections where appropriate;
- avoid generic enterprise filler.

Templates should improve review quality.

They should not increase ceremony for ordinary local changes.

---

# 45. Documentation review expectations

A canonical documentation change should be reviewed for:

- correct semantic owner;
- no duplicate authority;
- source/test evidence;
- link integrity;
- terminology consistency;
- compatibility/migration implications;
- stale references;
- generated artifact impact;
- ADR requirement;
- lifecycle status.

Documentation review is architecture review when the file is normative.

---

# 46. Documentation change categories

## Local editorial correction

Examples:

- grammar;
- typo;
- link fix;
- wording without semantic change.

Usually no ADR/migration.

## Current-fact update

Examples:

- project set;
- package family;
- command;
- runtime port;
- toolchain version family.

Update current context/index/evidence owner.

Do not alter durable architecture unless meaning changed.

## Normative contract clarification

Clarifies existing intended semantics.

Requires:

- evidence;
- check for implementation drift;
- affected references.

## Normative contract change

Changes product/architecture behavior.

Requires:

- change classification;
- ADR when consequential;
- migration/compatibility when needed;
- source/tests/gates;
- downstream docs.

## Documentation authority change

Moves canonical ownership.

Requires:

- governance review;
- topic-authority map update;
- router update;
- references;
- removal of old owner;
- docs governance.

---

# 47. Documentation governance entry point

The canonical governance command should be exposed through:

```bash
make docs-check
```

The implementation may be composed from multiple scripts.

The command should validate the complete documentation authority model.

Current/future implementation details belong in:

```text
governance/documentation-quality-gates.md
scripts/docs/
.github/workflows/
```

---

# 48. Required documentation checks

Target governance should enforce at minimum:

```text
broken relative links = 0
absolute workstation/file links = 0
duplicate rule IDs = 0
duplicate ADR IDs = 0
forbidden legacy authority paths = 0
required canonical paths exist
backend source/project inventory alignment
frontend workspace/architecture alignment
generated documentation drift = 0
no active duplicate backend/frontend architecture tree
no branch/freeze/version filename authority generations
```

A required check that cannot execute must fail or report unrun—not silently pass.

---

# 49. Documentation generator ownership

Generators should be under a stable repository tooling location such as:

```text
scripts/docs/
```

Target responsibilities may include:

```text
check-links
check-authority
check-metadata
check-rule-ids
check-source-inventory
check-generated
generate-document-index
generate-rule-index
generate-backend-project-map
```

Exact script structure may evolve if the same governance contracts remain explicit.

---

# 50. Documentation completion criteria

The repository-level documentation system is healthy when:

- one canonical owner exists per mapped topic;
- root docs have distinct roles;
- repository `docs/` contains only cross-stack/product/governance/shared concerns;
- backend implementation architecture exists only under backend docs;
- frontend implementation architecture exists only under frontend docs;
- product contexts own business meaning;
- generated exact inventories have producers;
- ADRs preserve rationale;
- roadmaps/audits are not active architecture;
- current-state snapshots do not masquerade as durable intent;
- documentation CI enforces authority and drift;
- Coding Agents can route ordinary changes without guessing architecture.

---

# 51. Quick reference

## Product

```text
../PRODUCT.md
product/
```

## Design

```text
../DESIGN.md
../frontend/docs/architecture/ui-and-design-system.md
```

## Repository rules

```text
../RULE.md
```

## Coding Agent execution

```text
../AGENTS.md
```

## Current state

```text
../CONTEXT.md
```

## Task routing

```text
../CONTEXT-MAP.md
```

## System architecture

```text
architecture/
```

## Backend architecture

```text
../backend/docs/
```

## Frontend architecture

```text
../frontend/docs/
```

## Quality

```text
quality/
```

## Delivery

```text
delivery/
```

## Operations

```text
operations/
```

## Infrastructure

```text
infrastructure/
```

## Decisions

```text
decisions/
../backend/docs/decisions/
../frontend/docs/decisions/
```

## Generated

```text
generated/
../backend/docs/generated/
../frontend/docs/generated/
```

---

# 52. Final documentation rule

When unsure where a new statement belongs, do not place it in the nearest convenient Markdown file.

Ask:

```text
What question does this statement answer?

Who owns that question?

Is this:
    product meaning?
    repository invariant?
    current fact?
    architecture?
    quality standard?
    delivery policy?
    operational procedure?
    historical rationale?
    generated inventory?

What executable evidence supports it?
```

Then place it in the single correct owner and reference it elsewhere.

The documentation system succeeds when a contributor can ask:

> **“Where is this decision owned?”**

and receive one unambiguous answer.
