# CONTEXT-MAP.md — Notrelix Authority Router

> **Use this file when you know what kind of work you are doing but do not yet know which Notrelix document owns the decision.**
>
> This file is navigation only.
>
> It MUST NOT become a second product model, architecture handbook, rulebook, or current-state snapshot.

The routing principle is:

> **one topic → one canonical normative owner**

A change may legitimately affect several canonical owners when it crosses distinct decision surfaces.

Example:

```text
New Work Management public event

Product meaning
→ docs/product/contexts/work-management.md

Cross-stack event compatibility
→ docs/architecture/events-realtime-and-delivery-boundary.md
→ docs/architecture/contract-boundaries.md

Backend event/outbox implementation
→ backend/docs/architecture/domain-modeling.md
→ backend/docs/architecture/platform-and-messaging.md

Automation consumer semantics
→ docs/product/contexts/automation.md

Migration/rollout if breaking
→ docs/delivery/change-impact-and-migration.md
```

Those files own different questions.

They must not copy the same normative paragraph into several locations.

---

# 1. First routing decision

Start by identifying the **kind of question** you are trying to answer.

| Question | First owner |
|---|---|
| What is Notrelix? | [`PRODUCT.md`](PRODUCT.md) |
| What does this business concept mean? | `docs/product/contexts/<owner>.md` |
| What repository-wide invariant applies? | [`RULE.md`](RULE.md) |
| How should a Coding Agent execute this task? | [`AGENTS.md`](AGENTS.md) |
| What is true in the repository right now? | [`CONTEXT.md`](CONTEXT.md) |
| What should the product feel/behave like? | [`DESIGN.md`](DESIGN.md) |
| How is the overall system divided? | `docs/architecture/system-overview.md` |
| Which bounded context owns this fact? | `docs/architecture/bounded-context-map.md` + owning product context |
| How should a backend concern be implemented? | `backend/docs/architecture/<concern>.md` |
| How should a frontend concern be implemented? | `frontend/docs/architecture/<concern>.md` |
| Why was a consequential design chosen? | relevant ADR registry |
| What exact project/package inventory exists? | executable manifest/generated evidence |
| How must a breaking change migrate? | `docs/delivery/change-impact-and-migration.md` |
| How is documentation authority governed? | `docs/governance/documentation-authority.md` |

---

# 2. Required interpretation order

Do not use this map as a precedence override mechanism.

For a material change, reason through:

```text
explicit task intent
→ RULE.md constraints
→ AGENTS.md execution protocol
→ scoped AGENTS.md if applicable
→ canonical owner(s) from this map
→ relevant ADR rationale
→ source/tests/manifests/contracts/migrations/CI evidence
```

Current source may reveal debt or transition.

It does not automatically override the canonical owner.

See [`AGENTS.md`](AGENTS.md) for drift classification and stop conditions.

---

# 3. Root entry points

| Need | Read |
|---|---|
| Repository/product onboarding | [`README.md`](README.md) |
| Product constitution | [`PRODUCT.md`](PRODUCT.md) |
| Product design constitution | [`DESIGN.md`](DESIGN.md) |
| Repository invariants | [`RULE.md`](RULE.md) |
| Coding Agent execution contract | [`AGENTS.md`](AGENTS.md) |
| Current repository facts/transitions | [`CONTEXT.md`](CONTEXT.md) |
| Task → owner routing | [`CONTEXT-MAP.md`](CONTEXT-MAP.md) |

Root documents summarize or constrain repository-wide behavior.

They do not replace detailed backend/frontend/system/product owners.

---

# 4. Product-semantic routing

Use product-context documents when the question is:

- What does this business concept mean?
- Which capability owns the fact?
- What lifecycle is correct?
- What product invariant applies?
- What user-visible behavior should remain stable?
- What cross-context business relationship exists?

Do not answer those questions from:

- table names;
- frontend package names;
- route names;
- provider DTOs;
- historical roadmaps.

---

## 4.1 Accounts

Read:

```text
PRODUCT.md
docs/product/contexts/accounts.md
```

Use for:

- Account meaning;
- account lifecycle;
- account-level administration;
- account vs workspace distinction;
- account-scoped ownership.

Also read when relevant:

```text
docs/product/contexts/billing.md
docs/product/contexts/governance.md
backend/docs/architecture/security-tenancy-authorization.md
```

---

## 4.2 Identity

Read:

```text
PRODUCT.md
docs/product/contexts/identity.md
```

Use for:

- user identity;
- authentication;
- sessions;
- credentials;
- MFA;
- OAuth/SSO identity;
- API/service token identity/security lifecycle.

For authorization questions also read:

```text
docs/product/contexts/governance.md
backend/docs/architecture/security-tenancy-authorization.md
```

Authentication and authorization are not the same topic.

---

## 4.3 Workspaces

Read:

```text
PRODUCT.md
docs/product/contexts/workspaces.md
```

Use for:

- Workspace meaning/lifecycle;
- membership;
- invitations;
- collaboration tenancy;
- workspace-scoped product ownership.

Also read for protected resource work:

```text
docs/product/contexts/governance.md
backend/docs/architecture/security-tenancy-authorization.md
```

---

## 4.4 Governance

Read:

```text
PRODUCT.md
docs/product/contexts/governance.md
```

Use for:

- permissions;
- sharing;
- resource-access semantics;
- subject/resource policy;
- guests/share links;
- administrative/security audit semantics.

Backend enforcement:

```text
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/application-model.md
```

Frontend UX projection:

```text
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/ui-and-design-system.md
```

---

## 4.5 Work Management

Read:

```text
PRODUCT.md
docs/product/contexts/work-management.md
```

Use for:

- Board;
- BoardField;
- BoardItem;
- BoardGroup;
- BoardView;
- field/value semantics;
- ordering;
- Table;
- Kanban;
- Calendar;
- Timeline;
- Form;
- Dashboard;
- relations;
- formulas;
- rollups;
- work lifecycle.

Implementation routes depend on the change:

```text
Domain
→ backend/docs/architecture/domain-modeling.md

Use case
→ backend/docs/architecture/application-model.md

Persistence/indexing
→ backend/docs/architecture/infrastructure-and-data.md

API
→ backend/docs/architecture/api-and-contracts.md

Frontend state
→ frontend/docs/architecture/state-query-mutations.md

Realtime
→ frontend/docs/architecture/realtime.md

UI
→ frontend/docs/architecture/ui-and-design-system.md
```

---

## 4.6 Documents

Read:

```text
PRODUCT.md
docs/product/contexts/documents.md
```

Use for:

- Page;
- Block;
- hierarchy;
- document content lifecycle;
- resource links/embeds.

Cross-capability collaboration:

```text
docs/product/contexts/collaboration.md
```

Frontend implementation may involve Documents product packages but package shape does not redefine product semantics.

---

## 4.7 Collaboration

Read:

```text
PRODUCT.md
docs/product/contexts/collaboration.md
```

Use for:

- comments;
- threads;
- mentions;
- reactions;
- notifications;
- user-facing activity;
- attachment metadata where owned;
- presence/cursors;
- target-resource collaboration.

For resource access:

```text
docs/product/contexts/governance.md
backend/docs/architecture/security-tenancy-authorization.md
```

For realtime behavior:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
frontend/docs/architecture/realtime.md
```

---

## 4.8 Automation

Read:

```text
PRODUCT.md
docs/product/contexts/automation.md
```

Use for:

- trigger;
- conditions;
- actions;
- scheduling;
- execution identity;
- automation lifecycle;
- recursion policy;
- capability invocation semantics.

Reliability:

```text
backend/docs/architecture/platform-and-messaging.md
docs/architecture/data-ownership-and-consistency.md
```

Cross-context actions:

```text
affected product context
docs/architecture/contract-boundaries.md
```

---

## 4.9 Integrations

Read:

```text
PRODUCT.md
docs/product/contexts/integrations.md
```

Use for:

- provider connection lifecycle;
- webhook semantics;
- external mapping;
- synchronization;
- provider anti-corruption boundary;
- provider revision/conflict behavior.

Provider implementation:

```text
backend/docs/architecture/infrastructure-and-data.md
```

Retry/idempotency:

```text
backend/docs/architecture/platform-and-messaging.md
```

Secrets/security:

```text
backend/docs/architecture/security-tenancy-authorization.md
docs/quality/security-quality-standard.md
```

---

## 4.10 Billing

Read:

```text
PRODUCT.md
docs/product/contexts/billing.md
```

Use for:

- plan;
- subscription;
- entitlement;
- limit;
- usage;
- commercial lifecycle;
- downgrade/payment-failure product policy.

Do not infer Billing semantics from provider statuses alone.

Provider implementation may also require:

```text
docs/product/contexts/integrations.md
backend/docs/architecture/infrastructure-and-data.md
```

---

## 4.11 Analytics / Reporting

Read:

```text
PRODUCT.md
docs/product/contexts/analytics.md
```

Use for:

- metrics;
- dashboard/widget semantics;
- analytical projections;
- freshness;
- reporting;
- aggregation meaning.

Performance/queryability may require:

```text
docs/quality/performance-and-scalability.md
backend/docs/architecture/infrastructure-and-data.md
```

Analytics remains derived state unless a product decision explicitly says otherwise.

---

# 5. Search routing

Search/indexing is currently a technical/product capability but is **not automatically a business bounded context**.

For search behavior read:

```text
PRODUCT.md
owning source product context(s)
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/security-tenancy-authorization.md
```

Frontend search package/dependency questions:

```text
frontend/docs/architecture/dependency-boundaries.md
frontend/docs/architecture/state-query-mutations.md
```

If a task proposes making Search a bounded context, classify it as:

```text
Product semantic change
+
System architecture change
```

and read:

```text
docs/architecture/bounded-context-map.md
docs/architecture/capability-extraction-strategy.md
```

---

# 6. Cross-stack system architecture routing

Use `docs/architecture/` when a question crosses backend/frontend/product owners.

---

## 6.1 System boundary / overall architecture

Read:

```text
docs/architecture/system-overview.md
```

Use for:

- overall system context;
- modular monolith + multi-host frontend;
- external actors/systems;
- major trust boundaries;
- server-authoritative model;
- major system non-goals.

Do not use this document for low-level Domain/Application/package rules.

---

## 6.2 Bounded-context ownership

Read:

```text
docs/architecture/bounded-context-map.md
```

Then read the owning:

```text
docs/product/contexts/<context>.md
```

Use for:

- which context owns a fact;
- context upstream/downstream relationship;
- context extraction seam;
- technical capability vs business context distinction.

---

## 6.3 REST / realtime / event / generated contract boundary

Read:

```text
docs/architecture/contract-boundaries.md
```

Then the producer-specific owner:

```text
backend/docs/architecture/api-and-contracts.md
backend/docs/architecture/platform-and-messaging.md
frontend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/realtime.md
```

Use for:

- producer;
- consumers;
- compatibility;
- versioning;
- deprecation;
- generated contracts;
- breaking change.

---

## 6.4 Data ownership / consistency

Read:

```text
docs/architecture/data-ownership-and-consistency.md
```

Use for:

- authoritative owner;
- local transaction;
- cross-context eventual consistency;
- projections;
- cache;
- saga/process-manager admission;
- retry relationship.

Then read the affected backend/frontend owners.

---

## 6.5 Domain event / integration event / realtime / audit taxonomy

Read:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
```

Use to distinguish:

```text
Domain event
Integration/public event
Outbox record
Message envelope
Realtime notification
Activity event
Audit event
```

Then route implementation to:

```text
backend/docs/architecture/domain-modeling.md
backend/docs/architecture/platform-and-messaging.md
frontend/docs/architecture/realtime.md
```

---

## 6.6 Microservice/service extraction question

Read:

```text
docs/architecture/capability-extraction-strategy.md
docs/architecture/bounded-context-map.md
```

Use for:

- whether a bounded context should remain inside the modular monolith;
- extraction triggers;
- contract/data/operational prerequisites;
- service boundary.

Do not infer:

```text
bounded context = current microservice
```

---

# 7. Backend routing

Always start backend work with:

```text
backend/AGENTS.md
backend/docs/README.md
```

Then route by concern.

---

## 7.1 Backend project/layer topology

Canonical owner:

```text
backend/docs/architecture/backend-overview.md
```

Exact current project inventory:

```text
backend/backend.slnx
backend/**/*.csproj
backend/docs/generated/project-map.md
```

Use for:

- project responsibility;
- project reference direction;
- Platform vs Infrastructure;
- composition;
- bounded-context placement philosophy.

---

## 7.2 Domain aggregate / entity / value object / invariant

Canonical owner:

```text
backend/docs/architecture/domain-modeling.md
```

Also read the owning product context.

Use for:

- aggregate admission;
- entity/value semantics;
- mutation order;
- semantic no-op;
- failure atomicity;
- external facts;
- version;
- audit;
- lifecycle;
- typed IDs;
- SharedKernel;
- hierarchy/order;
- Domain events.

Evidence:

```text
backend/src/Notrelix.Domain/**
backend/tests/Notrelix.Domain.Tests/**
backend/tests/Notrelix.Architecture.Tests/**
```

---

## 7.3 Application use case / vertical slice

Canonical owner:

```text
backend/docs/architecture/application-model.md
```

Use for:

- commands/queries;
- use-case structure;
- request contracts;
- handlers;
- orchestration;
- external facts;
- cross-context use cases.

Evidence:

```text
backend/src/Notrelix.Application/**
backend/tests/Notrelix.Application.Tests/**
```

---

## 7.4 Application pipeline

Canonical owner:

```text
backend/docs/architecture/application-model.md
```

Use for:

- behavior order;
- marker contracts;
- authorization;
- tenant/resource resolution;
- transaction;
- expected version;
- idempotency;
- cache/post-commit behavior.

Actual order MUST be verified from source/registrations/tests.

Do not infer exact pipeline order from this router.

---

## 7.5 Transaction / SaveChanges ownership

Read:

```text
backend/docs/architecture/application-model.md
docs/architecture/data-ownership-and-consistency.md
```

If outbox/post-commit involved also read:

```text
backend/docs/architecture/platform-and-messaging.md
```

---

## 7.6 EF / DbContext / mapping / PostgreSQL

Canonical owner:

```text
backend/docs/architecture/infrastructure-and-data.md
```

For schema evolution also read:

```text
backend/docs/operations/migrations-and-data-change.md
```

Evidence:

```text
backend/src/Notrelix.Infrastructure/**
backend migrations
backend/tests/Notrelix.Infrastructure.Tests/**
backend/tests/Notrelix.Integration.Tests/**
```

---

## 7.7 RLS

Read:

```text
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/infrastructure-and-data.md
```

For migrations:

```text
backend/docs/operations/migrations-and-data-change.md
```

Evidence must include integration/RLS behavior, not only mocked tests.

---

## 7.8 Cache / Redis

Read:

```text
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/application-model.md
backend/docs/architecture/security-tenancy-authorization.md
```

Use for:

- cache implementation;
- cache ownership;
- permission-sensitive key scope;
- invalidation;
- post-commit timing.

Cache is derived state.

---

## 7.9 Provider / external service adapter

Read:

```text
backend/docs/architecture/infrastructure-and-data.md
owning product context
```

For integration providers:

```text
docs/product/contexts/integrations.md
```

For secrets/security:

```text
backend/docs/architecture/security-tenancy-authorization.md
```

For retryable effects:

```text
backend/docs/architecture/platform-and-messaging.md
```

---

## 7.10 Messaging / outbox / consumer

Canonical owner:

```text
backend/docs/architecture/platform-and-messaging.md
```

Also read:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/data-ownership-and-consistency.md
```

Use for:

- message identity;
- envelope;
- consumer identity;
- idempotency;
- delivery;
- retry;
- dead-letter;
- poison;
- ordering;
- tenant execution context.

---

## 7.11 Background jobs

Read:

```text
backend/docs/architecture/platform-and-messaging.md
```

Then affected product/security owner.

If job touches tenant data:

```text
backend/docs/architecture/security-tenancy-authorization.md
```

---

## 7.12 API endpoint / HTTP behavior

Canonical owner:

```text
backend/docs/architecture/api-and-contracts.md
```

Use for:

- endpoint conventions;
- auth integration;
- request/result mapping;
- OpenAPI;
- errors;
- pagination/filter/sort;
- idempotency input;
- version/deprecation.

Business authorization remains Application-owned.

---

## 7.13 Authentication

Read:

```text
docs/product/contexts/identity.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/api-and-contracts.md
```

Use for:

- JWT/session integration;
- OAuth/SSO;
- credential/security boundary.

---

## 7.14 Authorization

Read:

```text
docs/product/contexts/governance.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/application-model.md
```

Use for:

- subject;
- resource;
- permission;
- resource resolution;
- Application policy;
- fail-closed behavior.

---

## 7.15 Backend configuration/runtime

Canonical owner:

```text
backend/docs/operations/configuration-and-runtime.md
```

Exact current runtime evidence:

```text
backend configuration source
.env.example
docker-compose*.yml
Makefile
```

---

## 7.16 Migration / schema / backfill

Canonical owner:

```text
backend/docs/operations/migrations-and-data-change.md
```

Also read:

```text
docs/delivery/change-impact-and-migration.md
backend/docs/architecture/infrastructure-and-data.md
```

For destructive lifecycle:

```text
owning product context
RULE.md NRX-011
```

---

## 7.17 Backend test/gate change

Canonical owner:

```text
backend/docs/architecture/testing-and-quality-gates.md
```

Scoped agent:

```text
backend/tests/AGENTS.md
```

Exact project inventory:

```text
backend/backend.slnx
```

Use for:

- test responsibility;
- architecture tests;
- integration tests;
- OpenAPI drift;
- non-zero execution.

---

## 7.18 Historical backend rationale

Read:

```text
backend/docs/decisions/README.md
backend/docs/decisions/ADR-*.md
```

ADRs explain historical consequential choices.

They do not replace current architecture docs.

---

# 8. Frontend routing

Always start frontend work with:

```text
frontend/AGENTS.md
frontend/docs/README.md
```

Exact package ownership/dependency legality must be checked against the executable architecture manifest.

---

## 8.1 Frontend overall architecture / package families

Canonical owner:

```text
frontend/docs/architecture/frontend-overview.md
```

Exact current package workspace:

```text
frontend/pnpm-workspace.yaml
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

---

## 8.2 Package dependency / import boundary

Canonical owner:

```text
frontend/docs/architecture/dependency-boundaries.md
```

Executable authority:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Generated evidence:

```text
frontend/docs/generated/package-boundaries.md
```

Do not edit generated boundary docs manually.

Do not add manifest edges only to make an import compile.

---

## 8.3 Add/move frontend package

Read:

```text
frontend/docs/architecture/dependency-boundaries.md
frontend/docs/architecture/frontend-overview.md
frontend/docs/architecture/architecture-change-policy.md
```

Then:

```text
architecture-manifest.ts
pnpm-workspace.yaml
package.json files
```

If the change introduces a new semantic owner, also read product/system ownership docs.

---

## 8.4 Host composition

Canonical owner:

```text
frontend/docs/architecture/hosts-composition-routing.md
```

Use for:

- web bootstrap;
- mobile bootstrap;
- marketing composition;
- providers;
- shell;
- environment;
- host runtime.

---

## 8.5 Routing/navigation

Read:

```text
frontend/docs/architecture/hosts-composition-routing.md
DESIGN.md
owning product context
```

Use for:

- web routes;
- mobile navigation;
- public marketing routes;
- route guards as UX;
- shell ownership.

Frontend routing does not replace backend authorization.

---

## 8.6 API/generated frontend contract

Canonical owner:

```text
frontend/docs/architecture/api-and-contracts.md
```

Also read producer:

```text
backend/docs/architecture/api-and-contracts.md
docs/architecture/contract-boundaries.md
```

Exact generated contract source/tooling must be inspected.

Do not hand-copy backend DTOs.

---

## 8.7 Query key / server-state ownership

Canonical owner:

```text
frontend/docs/architecture/state-query-mutations.md
```

Use for:

- query-key taxonomy;
- account/workspace/resource scope;
- query owner;
- invalidation;
- stale response protection;
- local versus server state.

---

## 8.8 Mutation / optimistic update

Read:

```text
frontend/docs/architecture/state-query-mutations.md
owning product context
```

If public API behavior changes:

```text
frontend/docs/architecture/api-and-contracts.md
backend/docs/architecture/api-and-contracts.md
```

Use for:

- optimistic admission;
- snapshot;
- patch;
- rollback;
- conflict;
- invalidate/refetch;
- realtime interaction.

---

## 8.9 Workspace/account transition

Read:

```text
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/hosts-composition-routing.md
```

Use for:

- request cancellation;
- old-scope cache;
- realtime teardown/resubscribe;
- permission state;
- route state.

---

## 8.10 Realtime

Canonical owner:

```text
frontend/docs/architecture/realtime.md
```

Cross-stack taxonomy:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
```

Backend producer:

```text
backend/docs/architecture/platform-and-messaging.md
```

Use for:

- connection state;
- subscription ownership;
- identity;
- duplicate;
- ordering;
- sequence gaps;
- reconnect;
- heartbeat;
- cache reconciliation.

---

## 8.11 UI primitive / design-system implementation

Read:

```text
DESIGN.md
frontend/docs/architecture/ui-and-design-system.md
```

Use for:

- token ownership;
- web/mobile primitives;
- accessibility implementation;
- component states;
- vendor/shadcn policy.

---

## 8.12 Product-specific frontend component

Read:

```text
owning product context
frontend/docs/architecture/frontend-overview.md
frontend/docs/architecture/ui-and-design-system.md
```

Then determine product/feature package owner from architecture manifest.

Do not promote a product component into generic UI merely because it is reused.

---

## 8.13 Web-only behavior

Read:

```text
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/dependency-boundaries.md
```

Verify that web-only dependencies remain outside native-safe paths.

---

## 8.14 Mobile/native behavior

Read:

```text
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/dependency-boundaries.md
DESIGN.md
```

Use for:

- Expo/React Native runtime;
- mobile-safe product adapter;
- touch/native navigation;
- no DOM/react-dom leakage.

---

## 8.15 Marketing behavior

Read:

```text
frontend/docs/architecture/hosts-composition-routing.md
DESIGN.md
PRODUCT.md
```

Use for:

- public acquisition;
- SEO;
- brand register;
- product marketing truth.

Marketing does not own authenticated application server state.

---

## 8.16 Frontend test/gate change

Canonical owner:

```text
frontend/docs/architecture/testing-and-quality-gates.md
```

Exact commands:

```text
frontend/package.json
```

Architecture:

```text
frontend/tooling/dependency-rules/**
```

Generated contract drift:

```text
frontend codegen tooling
```

---

## 8.17 Frontend architecture change

Canonical owner:

```text
frontend/docs/architecture/architecture-change-policy.md
```

Also read affected architecture topic and frontend decision registry.

Use for:

- package graph;
- host/runtime model;
- foundation admission;
- product/feature responsibility;
- public export changes.

---

## 8.18 Historical frontend rationale

Read:

```text
frontend/docs/decisions/README.md
frontend/docs/decisions/FE-ADR-*.md
```

---

# 9. Product design routing

Root owner:

```text
DESIGN.md
```

Use for:

- calm · focused · confident;
- authenticated vs marketing register;
- hierarchy;
- calm density;
- interaction grammar;
- application states;
- accessibility baseline;
- motion;
- responsive/mobile semantics.

Implementation owner:

```text
frontend/docs/architecture/ui-and-design-system.md
```

Exact literal tokens/primitives:

```text
frontend/packages/ui/tokens
frontend/packages/ui/web
frontend/packages/ui/mobile
```

---

## 9.1 Accessibility

Read:

```text
DESIGN.md
docs/quality/accessibility-standard.md
frontend/docs/architecture/ui-and-design-system.md
frontend/docs/architecture/testing-and-quality-gates.md
```

Host-specific accessibility may also require host composition docs.

---

## 9.2 Large-data UI / performance

Read:

```text
DESIGN.md
docs/quality/performance-and-scalability.md
frontend/docs/architecture/state-query-mutations.md
```

Backend query/indexing:

```text
backend/docs/architecture/infrastructure-and-data.md
```

---

## 9.3 Loading / empty / permission / conflict UX

Read:

```text
DESIGN.md
owning product context
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/ui-and-design-system.md
```

For realtime/offline/reconnecting:

```text
frontend/docs/architecture/realtime.md
```

---

# 10. Quality routing

Repository-wide quality docs own shared standards.

Backend/frontend exact test topology remains project-owned.

---

## 10.1 Engineering quality

Read:

```text
docs/quality/engineering-quality-standard.md
```

Use for:

- ownership clarity;
- complexity;
- dependency hygiene;
- dead compatibility;
- error handling;
- architecture-aware review.

---

## 10.2 Testing strategy

Read:

```text
docs/quality/testing-strategy.md
```

Then:

```text
backend/docs/architecture/testing-and-quality-gates.md
or
frontend/docs/architecture/testing-and-quality-gates.md
```

Use for:

- test type;
- architecture tests;
- integration/E2E;
- flaky policy;
- non-zero evidence;
- required merge gates.

---

## 10.3 Security quality

Read:

```text
docs/quality/security-quality-standard.md
```

Then affected backend/frontend security owner.

Use for:

- secrets;
- sensitive data;
- dependencies;
- logging;
- vulnerability handling;
- secure engineering.

---

## 10.4 Accessibility quality

Read:

```text
docs/quality/accessibility-standard.md
DESIGN.md
frontend/docs/architecture/ui-and-design-system.md
```

---

## 10.5 Performance/scalability

Read:

```text
docs/quality/performance-and-scalability.md
```

Then affected backend/frontend/data owner.

Use for:

- bounded queries;
- pagination;
- indexes;
- cache;
- payloads;
- realtime fan-out;
- frontend rendering/windowing.

---

# 11. Delivery/change routing

---

## 11.1 Classify a change

Read:

```text
docs/delivery/change-classification.md
```

Use for:

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

---

## 11.2 Breaking change / migration

Read:

```text
docs/delivery/change-impact-and-migration.md
```

Also read the affected contract/product/data owners.

Use for:

- consumer inventory;
- compatibility;
- expand/contract;
- rollout sequence;
- backfill;
- deprecation;
- cleanup.

---

## 11.3 Definition of done

Read:

```text
docs/delivery/definition-of-done.md
```

Then project-specific testing/gates.

Use for:

- behavior;
- tests;
- docs;
- migration;
- generated artifacts;
- rollout;
- observability;
- cleanup.

---

## 11.4 Release / rollout / recovery

Read:

```text
docs/delivery/release-rollout-and-recovery.md
```

Also read migration/operations docs.

---

# 12. Operations routing

---

## 12.1 Observability

Read:

```text
docs/operations/observability.md
```

Then affected runtime/backend/frontend owner.

Use for:

- logs;
- metrics;
- traces;
- correlation;
- sensitive-data limits;
- diagnostic ownership.

---

## 12.2 Incident readiness

Read:

```text
docs/operations/incident-readiness.md
```

Use for:

- incident roles;
- diagnosis;
- escalation;
- evidence;
- recovery decision flow.

---

## 12.3 Recovery / data safety

Read:

```text
docs/operations/recovery-and-data-safety.md
backend/docs/operations/migrations-and-data-change.md
```

Use for:

- backups;
- restore;
- data corruption;
- migration recovery;
- destructive event.

---

## 12.4 Degraded service

Read:

```text
docs/operations/service-degradation.md
```

Use for:

- Redis unavailable;
- messaging unavailable;
- provider degraded;
- realtime unavailable;
- read-only/degraded product mode.

Then read affected subsystem owners.

---

# 13. Infrastructure/deployment routing

---

## 13.1 Environment model

Read:

```text
docs/infrastructure/environment-model.md
```

Exact current environment evidence:

```text
.env.example
docker-compose*.yml
Makefile
```

---

## 13.2 Deployment/runtime

Read:

```text
docs/infrastructure/deployment-runtime.md
```

Use for:

- service/container topology;
- staging/production;
- gateway;
- rollout environment.

---

## 13.3 Containers/local services

Read:

```text
docs/infrastructure/containerization-and-local-services.md
```

Exact current source:

```text
docker-compose.yml
docker-compose.dev.yml
docker-compose.staging.yml
docker-compose.prod.yml
Makefile
```

---

# 14. Documentation-core routing

Documentation is a protected architecture subsystem.

---

## 14.1 Documentation authority

Read:

```text
docs/governance/documentation-authority.md
```

Use for:

- document classes;
- authority planes;
- canonical owner;
- summary versus definition;
- generated evidence;
- scoped-doc admission.

---

## 14.2 Documentation lifecycle

Read:

```text
docs/governance/documentation-lifecycle.md
```

Use for:

```text
draft
active
superseded
generated
```

and review triggers.

Do not use `FROZEN` as a generic immutable Markdown lifecycle state.

---

## 14.3 Topic owner

Read:

```text
docs/governance/topic-authority-map.md
```

This generated/auditable map should be consistent with this human task router.

`CONTEXT-MAP.md` routes by **task**.

`topic-authority-map.md` records canonical **topic ownership**.

They must not disagree.

---

## 14.4 Architecture decision / exception

Read:

```text
docs/governance/decision-and-exception-policy.md
```

Use for:

- ADR trigger;
- temporary exception;
- owner;
- risk;
- expiry/review trigger;
- removal.

---

## 14.5 Documentation CI/gates

Read:

```text
docs/governance/documentation-quality-gates.md
```

Current command entry point:

```bash
make docs-check
```

Exact implementation is script/CI-owned.

---

# 15. ADR routing

Choose ADR scope according to decision scope.

---

## 15.1 System/repository decision

Read/write:

```text
docs/decisions/
```

Namespace:

```text
SYS-ADR-*
```

Examples:

- cross-stack architecture;
- repository-wide contract strategy;
- major extraction model;
- system-wide deployment/consistency decision.

---

## 15.2 Backend decision

Read/write:

```text
backend/docs/decisions/
```

Namespace:

```text
ADR-*
```

Examples:

- pipeline boundary;
- RLS bootstrap lifecycle;
- backend security/runtime decision;
- backend-specific persistence/messaging architecture.

---

## 15.3 Frontend decision

Read/write:

```text
frontend/docs/decisions/
```

Namespace:

```text
FE-ADR-*
```

Examples:

- framework split;
- package manager;
- package exports;
- runtime/config model;
- frontend architecture boundary.

---

# 16. Generated evidence routing

When exact inventory is needed, prefer producer/generated evidence.

---

## 16.1 Backend project inventory

Producer/evidence:

```text
backend/backend.slnx
backend/**/*.csproj
```

Generated view:

```text
backend/docs/generated/project-map.md
```

---

## 16.2 Frontend package inventory

Executable authority:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Workspace discovery:

```text
frontend/pnpm-workspace.yaml
```

Generated evidence:

```text
frontend/docs/generated/package-boundaries.md
```

---

## 16.3 Public contracts

Use actual contract producers such as:

```text
backend OpenAPI/public-contract producer
artifacts/contracts/
frontend codegen tooling
```

Do not infer exact contract shape from prose.

---

## 16.4 Documentation index/rule index

Generated:

```text
docs/generated/document-index.md
docs/generated/rule-index.md
```

Do not hand-edit once generators are active.

---

# 17. Combined change routes

Many real changes cross owners.

Use these routes to avoid missing a dimension.

---

## 17.1 Add a new BoardField type

Read:

```text
PRODUCT.md
docs/product/contexts/work-management.md

RULE.md
    NRX-001
    NRX-007
    NRX-009
    NRX-014

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/application-model.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/api-and-contracts.md

frontend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/ui-and-design-system.md

docs/quality/performance-and-scalability.md
```

If automation/analytics consume it:

```text
docs/product/contexts/automation.md
docs/product/contexts/analytics.md
```

---

## 17.2 Add a protected workspace-scoped query

Read:

```text
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md

RULE.md
    NRX-003
    NRX-004
    NRX-014

backend/docs/architecture/application-model.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/api-and-contracts.md

frontend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/state-query-mutations.md
```

---

## 17.3 Change a Domain lifecycle/deletion rule

Read:

```text
owning product context
RULE.md NRX-011

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/operations/migrations-and-data-change.md

docs/delivery/change-impact-and-migration.md
```

If cross-context references exist, read those product contexts too.

---

## 17.4 Change an API contract

Read:

```text
RULE.md NRX-007
RULE.md NRX-008

docs/architecture/contract-boundaries.md
backend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/api-and-contracts.md

docs/delivery/change-impact-and-migration.md
```

Then inspect:

```text
OpenAPI producer
artifacts/contracts
frontend codegen
all consumers
```

---

## 17.5 Change an event/message payload

Read:

```text
owning product context
RULE.md NRX-007
RULE.md NRX-008
RULE.md NRX-010

docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/contract-boundaries.md

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/platform-and-messaging.md

affected consumer product contexts
docs/delivery/change-impact-and-migration.md
```

---

## 17.6 Add a provider integration

Read:

```text
docs/product/contexts/integrations.md
affected source product context

RULE.md
    NRX-007
    NRX-010
    NRX-012

backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md

docs/delivery/change-impact-and-migration.md
```

---

## 17.7 Change billing entitlement behavior

Read:

```text
docs/product/contexts/billing.md
affected product context

RULE.md
    NRX-001
    NRX-004
    NRX-011

backend/docs/architecture/application-model.md
backend/docs/architecture/security-tenancy-authorization.md

frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/ui-and-design-system.md
```

If provider-facing:

```text
docs/product/contexts/integrations.md
```

---

## 17.8 Add/change optimistic frontend behavior

Read:

```text
owning product context
RULE.md NRX-014
DESIGN.md

frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/testing-and-quality-gates.md
```

If backend operation semantics/concurrency change:

```text
backend/docs/architecture/application-model.md
```

---

## 17.9 Change realtime subscription/update semantics

Read:

```text
RULE.md
    NRX-003
    NRX-007
    NRX-014

docs/architecture/events-realtime-and-delivery-boundary.md

backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md

frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md
```

---

## 17.10 Add/move a frontend package

Read:

```text
RULE.md NRX-002
RULE.md NRX-006

frontend/docs/architecture/dependency-boundaries.md
frontend/docs/architecture/frontend-overview.md
frontend/docs/architecture/architecture-change-policy.md

architecture-manifest.ts
pnpm-workspace.yaml
```

If semantic ownership changes, also read the relevant product context and system bounded-context map.

---

## 17.11 Add a backend production project

Read:

```text
RULE.md NRX-002
backend/docs/architecture/backend-overview.md
docs/architecture/capability-extraction-strategy.md
docs/governance/decision-and-exception-policy.md
```

Then:

```text
backend/backend.slnx
affected csproj
Architecture.Tests
backend docs generated project map
```

Adding a project is an architecture change, not normal feature organization.

---

## 17.12 Extract a bounded context into a service

Read:

```text
PRODUCT.md
owning product context

docs/architecture/bounded-context-map.md
docs/architecture/capability-extraction-strategy.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md

docs/delivery/change-impact-and-migration.md
docs/infrastructure/deployment-runtime.md
docs/operations/observability.md
docs/operations/recovery-and-data-safety.md
```

This is a system architecture change.

Do not treat project splitting as equivalent to service extraction.

---

## 17.13 Change documentation authority

Read:

```text
RULE.md NRX-018

docs/governance/documentation-authority.md
docs/governance/topic-authority-map.md
docs/governance/documentation-lifecycle.md
docs/governance/documentation-quality-gates.md
```

Then update:

```text
CONTEXT-MAP.md
generated document/rule indices
references
CI/tooling
```

Do not leave old and new canonical owners active together.

---

# 18. Current-state versus target-intent routing

When the question is:

> What exists in the code right now?

Read:

```text
CONTEXT.md
source
tests
manifests
generated evidence
```

When the question is:

> What should new code follow?

Read:

```text
RULE.md
canonical topic owner
accepted ADR
```

When they disagree:

```text
AGENTS.md drift protocol
```

Do not use CONTEXT as a permanent architecture rule.

Do not use architecture prose as proof that current source already complies.

---

# 19. Current implementation inventory routing

---

## Backend exact project set

```text
backend/backend.slnx
```

---

## Backend exact references/packages

```text
backend/**/*.csproj
backend/Directory.Packages.props
```

---

## Backend SDK/framework

```text
backend/global.json
backend/Directory.Build.props
```

---

## Frontend exact workspace families

```text
frontend/pnpm-workspace.yaml
```

---

## Frontend exact package dependency architecture

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

---

## Frontend exact commands

```text
frontend/package.json
```

---

## Frontend exact resolved dependency versions

```text
frontend/pnpm-lock.yaml
```

---

## Runtime/container state

```text
docker-compose*.yml
Makefile
.env.example
```

---

## Current documentation governance

```text
scripts/docs/**
```

During migration, current legacy checker may still be:

```text
scripts/check-documentation.mjs
```

Use the actual checked-in script/Makefile as execution evidence.

---

# 20. Migration routing

A change is likely a migration when it affects any existing durable/consumer assumption.

Examples:

```text
schema
persisted enum/value
public API
event/message
realtime payload
package export
generated contract
product lifecycle meaning
authorization scope
tenant identity
provider synchronization
```

Read:

```text
docs/delivery/change-impact-and-migration.md
```

Then the relevant producer/consumer/data owner.

For backend persisted data:

```text
backend/docs/operations/migrations-and-data-change.md
```

---

# 21. Security routing

For any protected behavior, identify all relevant dimensions.

```text
authentication
authorization
tenant scope
resource scope
RLS
cache scope
search scope
realtime scope
background execution
logging/secrets
```

Primary product owners:

```text
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
```

Primary backend owner:

```text
backend/docs/architecture/security-tenancy-authorization.md
```

Repository quality:

```text
docs/quality/security-quality-standard.md
```

Frontend permission behavior:

```text
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/ui-and-design-system.md
```

---

# 22. Reliability routing

For retry, duplicate delivery, async failure, ordering, or provider uncertainty:

```text
RULE.md NRX-009
RULE.md NRX-010

docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/platform-and-messaging.md
```

Then read the owning product context.

For frontend convergence:

```text
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

---

# 23. Lifecycle/destructive-operation routing

Read:

```text
owning product context
RULE.md NRX-011
```

Then:

```text
backend/docs/architecture/domain-modeling.md
backend/docs/operations/migrations-and-data-change.md
```

For cross-context references:

```text
docs/architecture/data-ownership-and-consistency.md
affected product contexts
```

For user-facing behavior:

```text
DESIGN.md
frontend/docs/architecture/ui-and-design-system.md
```

---

# 24. Performance routing

Start with:

```text
docs/quality/performance-and-scalability.md
```

Then choose:

```text
backend query/index/persistence
→ backend/docs/architecture/infrastructure-and-data.md

Application query shape
→ backend/docs/architecture/application-model.md

frontend server state
→ frontend/docs/architecture/state-query-mutations.md

large-data UI
→ DESIGN.md
→ frontend/docs/architecture/ui-and-design-system.md

realtime fan-out
→ backend/docs/architecture/platform-and-messaging.md
→ frontend/docs/architecture/realtime.md
```

---

# 25. Documentation reference rules

This map uses target canonical paths.

When integrating documentation incrementally, install/update this map only when the referenced target owners exist, or perform the documentation migration atomically enough that docs-link governance remains green.

All canonical repository references MUST use repository-relative paths.

Do not add:

```text
file:///...
/Users/<name>/...
C:\...
```

as documentation authority references.

---

# 26. What this map must not contain

Do not add:

- detailed Domain mutation algorithms;
- exact Application behavior order;
- exact event dedup keys;
- literal token values;
- exact frontend package allow-list;
- exact API route list;
- package/project counts;
- migration status;
- freeze percentage;
- historical audit results.

Those belong to canonical owners, executable manifests, generated evidence, or history.

---

# 27. How to add a new routing entry

Before adding a row/section:

1. prove the topic has a distinct owner;
2. identify the canonical document;
3. identify executable evidence;
4. confirm the new entry does not duplicate an existing route under another name;
5. update `docs/governance/topic-authority-map.md` if topic ownership itself changes;
6. run documentation governance.

Do not create a new canonical document merely because CONTEXT-MAP lacks a convenient destination.

First decide whether the topic belongs in an existing owner.

---

# 28. Router maintenance triggers

Update this file when:

- a canonical document path changes;
- a new product bounded context is approved;
- a topic owner moves;
- backend/frontend canonical architecture is restructured;
- an ADR registry scope changes;
- a new repository-wide quality/delivery/operations owner is introduced;
- generated evidence producer/path changes;
- a common task repeatedly lacks deterministic routing.

Do not update for private source refactors.

---

# 29. Router validation

After changing this file or canonical documentation paths:

```bash
make docs-check
```

The target documentation-core governance should validate:

- links;
- canonical path existence;
- authority conflicts;
- topic-owner coherence;
- generated indices.

If this router and `docs/governance/topic-authority-map.md` disagree, documentation governance is broken and must be corrected before certification.

---

# 30. Compact task lookup

Use this as the fastest high-level lookup after understanding the authority model.

| Task | Primary owner |
|---|---|
| Product model | `PRODUCT.md` |
| Product context semantic | `docs/product/contexts/<context>.md` |
| Repository invariant | `RULE.md` |
| Product design | `DESIGN.md` |
| Current repository fact | `CONTEXT.md` |
| System overview | `docs/architecture/system-overview.md` |
| Bounded-context ownership | `docs/architecture/bounded-context-map.md` |
| Cross-stack contract | `docs/architecture/contract-boundaries.md` |
| Cross-context consistency | `docs/architecture/data-ownership-and-consistency.md` |
| Event/realtime taxonomy | `docs/architecture/events-realtime-and-delivery-boundary.md` |
| Service extraction | `docs/architecture/capability-extraction-strategy.md` |
| Backend project architecture | `backend/docs/architecture/backend-overview.md` |
| Domain | `backend/docs/architecture/domain-modeling.md` |
| Application | `backend/docs/architecture/application-model.md` |
| Persistence/RLS/providers | `backend/docs/architecture/infrastructure-and-data.md` |
| Platform/messaging | `backend/docs/architecture/platform-and-messaging.md` |
| API/OpenAPI | `backend/docs/architecture/api-and-contracts.md` |
| Backend security/tenant/authz | `backend/docs/architecture/security-tenancy-authorization.md` |
| Backend tests/gates | `backend/docs/architecture/testing-and-quality-gates.md` |
| Backend config/runtime | `backend/docs/operations/configuration-and-runtime.md` |
| Backend migrations | `backend/docs/operations/migrations-and-data-change.md` |
| Frontend overview | `frontend/docs/architecture/frontend-overview.md` |
| Frontend dependencies | `frontend/docs/architecture/dependency-boundaries.md` |
| Hosts/routing | `frontend/docs/architecture/hosts-composition-routing.md` |
| Frontend API/contracts | `frontend/docs/architecture/api-and-contracts.md` |
| Query/state/mutations | `frontend/docs/architecture/state-query-mutations.md` |
| Frontend realtime | `frontend/docs/architecture/realtime.md` |
| UI/design system | `frontend/docs/architecture/ui-and-design-system.md` |
| Frontend tests/gates | `frontend/docs/architecture/testing-and-quality-gates.md` |
| Frontend architecture change | `frontend/docs/architecture/architecture-change-policy.md` |
| Engineering quality | `docs/quality/engineering-quality-standard.md` |
| Testing strategy | `docs/quality/testing-strategy.md` |
| Security standard | `docs/quality/security-quality-standard.md` |
| Accessibility standard | `docs/quality/accessibility-standard.md` |
| Performance/scalability | `docs/quality/performance-and-scalability.md` |
| Change classification | `docs/delivery/change-classification.md` |
| Migration/change impact | `docs/delivery/change-impact-and-migration.md` |
| Definition of done | `docs/delivery/definition-of-done.md` |
| Release/rollout | `docs/delivery/release-rollout-and-recovery.md` |
| Observability | `docs/operations/observability.md` |
| Incident readiness | `docs/operations/incident-readiness.md` |
| Recovery/data safety | `docs/operations/recovery-and-data-safety.md` |
| Service degradation | `docs/operations/service-degradation.md` |
| Environment model | `docs/infrastructure/environment-model.md` |
| Deployment runtime | `docs/infrastructure/deployment-runtime.md` |
| Containers/local services | `docs/infrastructure/containerization-and-local-services.md` |
| Documentation authority | `docs/governance/documentation-authority.md` |
| Documentation lifecycle | `docs/governance/documentation-lifecycle.md` |
| Topic authority | `docs/governance/topic-authority-map.md` |
| ADR/exception policy | `docs/governance/decision-and-exception-policy.md` |
| Documentation gates | `docs/governance/documentation-quality-gates.md` |

---

# 31. Final routing rule

If you still cannot identify the canonical owner after using this map:

1. do **not** create a new owner immediately;
2. inspect [`PRODUCT.md`](PRODUCT.md), [`RULE.md`](RULE.md), and [`CONTEXT.md`](CONTEXT.md);
3. inspect related source/tests/manifests;
4. determine whether the problem is:
   - missing routing;
   - missing canonical architecture;
   - unclear product ownership;
   - active transition;
   - architecture conflict;
5. follow [`AGENTS.md`](AGENTS.md) stop/decision protocol.

The correct outcome is not:

> “Put the rule in the nearest convenient Markdown.”

The correct outcome is:

> **Identify the semantic owner, make that owner canonical, and route everything else to it.**
