# AGENTS.md — Notrelix Repository Execution Contract

> **Mandatory operating contract for Coding Agents working in the Notrelix repository.**
>
> This file defines how an agent must discover ownership, read architecture, inspect evidence, classify changes, implement safely, validate results, update documentation, and stop when a decision cannot be inferred without inventing product or architecture.

This file is procedural.

It does not replace:

- [`PRODUCT.md`](PRODUCT.md) for product meaning;
- [`RULE.md`](RULE.md) for repository-wide invariants;
- [`DESIGN.md`](DESIGN.md) for product design semantics;
- [`CONTEXT.md`](CONTEXT.md) for current repository state;
- canonical system/product/backend/frontend documents for detailed topic contracts;
- ADRs for historical decision rationale;
- source/tests/manifests/contracts/migrations/CI for executable evidence.

The purpose of this contract is simple:

> **A Coding Agent must not need to invent product semantics, architecture, security policy, consistency rules, or validation strategy merely because the repository is large.**

---

# 1. Operating model

## 1.1 The agent is an implementer and investigator, not an architecture oracle

The agent MAY:

- inspect source;
- inspect tests;
- inspect history/ADRs;
- compare current implementation to canonical intent;
- propose a decision when explicitly asked;
- implement an approved change;
- improve architecture within an already-defined contract;
- detect and report drift.

The agent MUST NOT:

- silently invent a new bounded context;
- silently choose a new aggregate boundary;
- silently weaken authorization;
- silently change tenant scope;
- silently choose a cross-context transaction model;
- silently choose a breaking public contract;
- silently reinterpret product vocabulary;
- silently change framework/package ownership;
- silently make a temporary exception permanent.

When architecture is already defined, follow it.

When architecture is changing, treat the task as an architecture/product change and execute the required decision process.

---

# 2. Authority model

Do not treat repository instructions as a simple “nearest file always wins” stack.

Use semantic authority.

## 2.1 Task intent

The explicit task defines the desired outcome.

If the task requests a change to an existing protected product/architecture contract, that is a **contract change request**.

The task does not silently bypass repository invariants.

---

## 2.2 Repository constitution

[`RULE.md`](RULE.md) constrains all implementation unless the task explicitly includes changing a repository invariant through the required product/architecture process.

A scoped instruction MUST NOT weaken or contradict a `NRX-*` invariant.

---

## 2.3 Root execution contract

This file defines repository-wide execution behavior:

- how to investigate;
- how to classify;
- when to stop;
- how to validate;
- how to report.

---

## 2.4 Scoped execution specialization

Applicable scoped `AGENTS.md` files may define local workflow.

Current intended scoped entry points include:

```text
backend/AGENTS.md
backend/tests/AGENTS.md
frontend/AGENTS.md
```

Do not assume every project/package has its own `AGENTS.md`.

Do not create scoped `AGENTS.md` files for symmetry.

A scoped file is justified only when local execution materially differs from its parent contract.

---

## 2.5 Canonical topic owner

For the concern being changed, read the canonical document that owns the topic.

Examples:

```text
product meaning
→ PRODUCT.md
→ docs/product/contexts/<owner>.md

Domain behavior
→ backend/docs/architecture/domain-modeling.md

Application use case/pipeline
→ backend/docs/architecture/application-model.md

frontend package boundary
→ frontend/docs/architecture/dependency-boundaries.md

frontend server state
→ frontend/docs/architecture/state-query-mutations.md
```

Use [`CONTEXT-MAP.md`](CONTEXT-MAP.md) when ownership is not obvious.

---

## 2.6 ADR

ADRs answer:

> Why was a consequential decision made?

They are evidence of design intent.

They do not replace the current canonical architecture document.

If current architecture intentionally changes, preserve historical ADR integrity and create/supersede decisions according to the decision policy.

---

## 2.7 Executable evidence

Source, tests, manifests, contracts, migrations, generated artifacts, and CI prove current behavior.

They can reveal:

- correct implementation;
- stale documentation;
- transitional code;
- architecture debt;
- incomplete migration.

Source alone does not automatically redefine intended architecture.

---

# 3. Mandatory task classification

Before editing code or canonical documentation, classify the task.

A task may belong to more than one class.

## Class A — Local implementation/refactor

Examples:

- internal rename;
- helper extraction;
- local performance improvement;
- test cleanup;
- private implementation replacement.

Expected impact:

- no product-semantic change;
- no public contract change;
- no persistence meaning change;
- no dependency-boundary change.

Required behavior:

- preserve applicable `NRX-*`;
- inspect local callers/tests;
- run focused proof;
- broaden validation when structure/architecture may be affected.

---

## Class B — Product semantic change

Examples:

- changing Work Management vocabulary;
- changing lifecycle;
- changing what a View owns;
- changing BoardGroup semantics;
- introducing a new bounded context;
- changing entitlement behavior;
- changing resource-sharing meaning.

Required reading:

```text
PRODUCT.md
docs/product/<owner>
RULE.md
affected backend/frontend canonical docs
```

Required handling:

- explicit semantic owner;
- affected consumers;
- lifecycle;
- security/tenant impact;
- migration/compatibility;
- tests;
- ADR when architecture consequences are significant.

---

## Class C — Backend business/use-case change

Examples:

- Domain invariant;
- command/query;
- authorization;
- transaction;
- expected version;
- event;
- persistence;
- API exposure.

Required reading:

```text
backend/AGENTS.md
owning product context
relevant backend architecture docs
```

Then inspect source/tests.

---

## Class D — Frontend capability change

Examples:

- product package behavior;
- query/mutation;
- realtime;
- routing;
- UI;
- web/mobile/marketing behavior.

Required reading:

```text
frontend/AGENTS.md
owning product context
relevant frontend architecture docs
```

Then inspect architecture manifest/source/tests.

---

## Class E — Public/cross-boundary contract change

Examples:

- REST/OpenAPI;
- realtime;
- event/message;
- generated frontend contract;
- package export;
- webhook;
- persisted external compatibility.

Required reading:

```text
RULE.md NRX-007
RULE.md NRX-008
docs/architecture/contract-boundaries.md
affected producer/consumer docs
```

Required output:

- producer/consumer inventory;
- compatibility classification;
- migration/rollout;
- generated artifacts;
- contract tests.

---

## Class F — Data/schema change

Required reading:

```text
backend/docs/architecture/infrastructure-and-data.md
backend/docs/operations/migrations-and-data-change.md
affected product/backend contract
```

Required analysis:

- authoritative owner;
- migration;
- persisted meaning;
- RLS;
- index/queryability;
- compatibility;
- deploy order;
- rollback/roll-forward;
- backfill;
- destructive risk.

---

## Class G — Architecture change

Examples:

- new project reference;
- new frontend dependency edge;
- new package family;
- moving consistency ownership;
- changing Platform/Infrastructure responsibility;
- changing server-state ownership;
- introducing a new shared abstraction.

Required behavior:

- identify affected architecture owner;
- explain why current architecture is insufficient;
- evaluate alternatives;
- update ADR when consequential;
- update architecture tests/manifests;
- migrate existing consumers where required.

---

## Class H — Security/tenant change

Examples:

- permission rule;
- share link;
- workspace scope;
- account scope;
- RLS;
- auth/session;
- permission-sensitive cache;
- background execution principal.

Required reading:

```text
RULE.md NRX-003
RULE.md NRX-004
RULE.md NRX-012
docs/product/contexts/governance.md
docs/product/contexts/workspaces.md
backend/docs/architecture/security-tenancy-authorization.md
```

Also read affected frontend/security UX docs when user-facing.

---

## Class I — Reliability/messaging change

Examples:

- outbox;
- delivery;
- consumer;
- ordering;
- retry;
- idempotency;
- poison detection;
- automation execution identity.

Required reading:

```text
RULE.md NRX-009
RULE.md NRX-010
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/platform-and-messaging.md
```

---

## Class J — Documentation-core change

Examples:

- canonical owner;
- authority map;
- rule;
- ADR registry;
- generated doc producer;
- docs CI;
- root constitution.

Required reading:

```text
RULE.md NRX-018
docs/governance/documentation-authority.md
docs/governance/documentation-lifecycle.md
docs/governance/topic-authority-map.md
```

Documentation-core changes MUST be treated with the same care as source architecture changes.

---

# 4. Mandatory preflight

Before editing a material change, perform the following.

## 4.1 Establish repository baseline

Inspect:

```bash
git status --short
git branch --show-current
git rev-parse HEAD
```

Purpose:

- know the exact source state;
- avoid deleting unrelated work;
- distinguish pre-existing modifications from your own changes.

Do not reset, clean, checkout, or overwrite unrelated user changes merely to obtain a clean worktree.

---

## 4.2 Identify the owner

Answer:

```text
What product capability/context owns this behavior?
What backend/frontend/system concern owns its implementation?
What durable fact is authoritative?
Who consumes it?
```

Do not start implementation until ownership is reasonably clear.

---

## 4.3 Identify scope

Determine whether the behavior is:

```text
global
account-scoped
workspace-scoped
resource-scoped
user-scoped
provider-scoped
```

Scope influences:

- authorization;
- cache identity;
- query identity;
- realtime;
- events;
- persistence;
- analytics;
- background jobs.

---

## 4.4 Identify invariants

List applicable `NRX-*`.

Typical examples:

```text
new protected workspace mutation
→ NRX-001
→ NRX-003
→ NRX-004
→ NRX-009

new retryable provider action
→ NRX-007
→ NRX-010
→ NRX-012

frontend optimistic mutation
→ NRX-003
→ NRX-014
→ NRX-016
```

The agent report SHOULD name applicable rule IDs for material changes.

---

## 4.5 Identify contracts

Inspect whether the change touches:

- product semantics;
- REST/OpenAPI;
- event/message;
- realtime;
- generated client/type;
- package export;
- schema;
- persisted values;
- lifecycle;
- authorization;
- tenant scope;
- provider mapping.

If yes, classify compatibility before implementation.

---

## 4.6 Identify evidence

Find existing:

- tests;
- architecture tests;
- contract tests;
- generated checks;
- migrations;
- ADRs;
- manifests;
- callers/consumers.

Do not create an implementation based only on one source file if a broader contract exists.

---

# 5. Repository mental model

A Coding Agent working in Notrelix MUST use the following high-level model.

## 5.1 Product

Notrelix is an enterprise work-management workspace operating system.

Product semantics are divided into stable business ownership areas such as:

```text
Accounts
Identity
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

A technical module is not automatically a bounded context.

See [`PRODUCT.md`](PRODUCT.md).

---

## 5.2 Backend

The backend is a modular monolith with five production projects:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Conceptual dependency direction:

```text
API ───────────────→ Application ─────────→ Domain
Infrastructure ────→ Application ─────────→ Domain
Platform ──────────→ Application ─────────→ Domain
```

Do not infer a simple vertical “Domain → Infrastructure” stack.

The projects separate responsibility.

A complete business use case may legitimately require changes across several projects.

---

## 5.3 Frontend

The frontend is a pnpm/Turborepo multi-host workspace with:

```text
apps
packages/foundation
packages/runtimes
packages/ui
packages/product
packages/features
tooling
```

Hosts include:

```text
web
mobile
marketing
```

Exact package/dependency authority belongs to the executable architecture manifest.

Apps compose.

Reusable product capability behavior belongs in the owning package family rather than being hidden in a host.

---

## 5.4 Cross-stack

Backend is authoritative for durable protected business state.

Frontend consumes explicit public contracts and maintains projections/caches.

Realtime accelerates convergence.

It does not become a second durable source of truth.

Generated frontend contracts must derive from approved producers.

---

# 6. Product reasoning protocol

When changing product behavior, reason in this order.

## 6.1 Vocabulary

What nouns and verbs does the product use?

Do not invent synonyms that create parallel meanings.

Examples:

```text
BoardItem
not legacy Card as foundational noun

BoardGroup
not universal StatusColumn

BoardView
not copied item collection
```

---

## 6.2 Ownership

Which context owns the fact?

A useful test:

> If this fact becomes incorrect, which context is responsible for restoring correctness?

That context is likely the owner.

---

## 6.3 Lifecycle

Determine:

- creation;
- mutation;
- archive/disable/revoke/etc.;
- deletion;
- restoration if applicable;
- retention;
- cross-context references.

Do not implement generic technical soft delete before product lifecycle is defined.

---

## 6.4 Scope and authorization

Who owns the resource?

Who may see/change it?

Is access inherited, explicit, shared, guest-based, or public?

Does the operation require Governance facts?

---

## 6.5 Consistency

Which facts must change atomically?

Which facts may converge asynchronously?

Do not widen an aggregate/transaction solely to avoid designing an explicit cross-context contract.

---

## 6.6 Representation

Only after meaning/ownership are established should you choose:

- table;
- DTO;
- event;
- route;
- query key;
- component;
- view;
- provider mapping.

---

# 7. Backend reasoning protocol

For backend work, read [`backend/AGENTS.md`](backend/AGENTS.md) and the relevant canonical backend document.

The following repository-level reasoning applies.

---

## 7.1 Domain changes

Start from the invariant.

Do not start from:

- table;
- endpoint;
- service class;
- DTO;
- frontend request.

Ask:

```text
What business fact changes?
Which aggregate/consistency boundary owns it?
What inputs are authoritative?
What external facts are required?
What lifecycle state permits the operation?
What is a semantic no-op?
What failures must leave state unchanged?
What event is justified after success?
```

### Domain mutation safety

For a protected mutation, reason about:

```text
lifecycle eligibility
→ validate identity/input
→ validate business invariants
→ normalize
→ detect semantic no-op
→ prepare prospective state
→ commit in-memory change
→ audit
→ version
→ event
```

Do not mechanically force this exact sequence when a documented domain contract requires a different valid sequence, but failure atomicity and semantic correctness MUST still hold.

### External facts

Domain MUST receive external facts from Application.

Examples:

- current actor;
- time;
- referenced user existence;
- hierarchy;
- provider status;
- cross-aggregate fact.

Do not add async repository/provider lookup inside aggregate/entities to “simplify” a rule.

### Aggregate admission

Do not create an aggregate root solely because:

- a table exists;
- the entity has a GUID;
- there is a repository;
- the UI edits it independently.

Aggregate roots own consistency/lifecycle.

---

## 7.2 Application changes

Application owns use-case orchestration.

Before changing a handler/behavior, identify:

```text
command/query semantics
authorization requirement
tenant/resource scope
external facts
transaction owner
expected version
idempotency
post-commit work
cache/realtime consequence
```

### Handlers

Handlers SHOULD:

- orchestrate;
- load required facts;
- invoke Domain;
- coordinate ports;
- map results.

Handlers SHOULD NOT become:

- business-rule dumps;
- authorization bypasses;
- direct provider orchestration when an adapter/port owns it;
- arbitrary transaction owners;
- duplicate Domain validation.

### Pipeline

When pipeline behavior is involved, inspect actual pipeline registrations/order and source marker contracts.

Do not invent behavior ordering from architecture prose alone.

### Transaction

Determine who owns:

- begin;
- commit;
- rollback;
- `SaveChanges`;
- outbox enrollment;
- post-commit actions.

If pipeline owns commit, handler must not independently commit unless explicitly designed.

### Expected version

Concurrency protection must fail closed when required.

Do not silently skip expected-version validation because a lookup path is unsupported.

---

## 7.3 Infrastructure/data changes

Infrastructure implements persistence/provider/runtime adapters.

Before editing:

```text
What application/domain contract is being implemented?
What database/provider facts are infrastructure-specific?
Does the change affect migration/RLS/indexes?
Does it accidentally introduce business ownership?
```

### Persistence

EF mapping does not define product semantics.

A persistence workaround MUST NOT silently change:

- aggregate ownership;
- lifecycle;
- authorization;
- tenant scope.

### RLS

RLS is defense in depth.

Inspect:

- tenant context setup;
- connection/transaction lifecycle;
- background consumers/jobs;
- migrations;
- integration tests.

### Cache

Cache is derived state.

Determine:

- scope;
- authorization sensitivity;
- invalidation owner;
- version/freshness;
- post-commit timing.

---

## 7.4 Platform/messaging changes

Platform owns reusable runtime mechanisms, not business semantics.

Before editing:

```text
What is the message identity?
What is the consumer identity?
What is the ordering stream?
When is dedup claimed/completed?
When does ordering advance?
What is poison identity?
How are retries classified?
When does dead-letter happen?
What tenant/RLS context is required?
```

Do not make a generic delivery mechanism depend on one bounded context's business vocabulary.

Do not advance success state before the protected handler/commit has succeeded.

---

## 7.5 API changes

API is a public/host boundary.

Before editing:

```text
What Application use case owns behavior?
What auth/session information enters?
What resource identity is exposed?
What request/result mapping is required?
What OpenAPI/generated contract changes?
What compatibility impact exists?
```

API endpoint code SHOULD remain thin.

Do not implement business authorization twice in API and Application.

Do not hand-copy contract changes into frontend.

---

## 7.6 Security changes

For any protected backend change, inspect both:

```text
Application authorization
and
RLS / tenant defense
```

Also inspect:

- caches;
- exports;
- search;
- realtime;
- background jobs;
- provider sync;
- audit.

A secure HTTP path does not prove the asynchronous path is secure.

---

# 8. Frontend reasoning protocol

For frontend work, read [`frontend/AGENTS.md`](frontend/AGENTS.md) and the relevant frontend architecture document.

Do not start from a component file and invent architecture outward.

Start from ownership.

---

## 8.1 Determine frontend owner

Classify the change as:

```text
host composition
foundation
runtime
UI
product capability
cross-product feature
tooling
```

Ask:

- Is behavior reusable?
- Is it business-capability-specific?
- Is it host-specific?
- Is it framework/runtime-specific?
- Is it presentation-only?
- Is it developer tooling?

Use the architecture manifest for exact package/dependency decisions.

---

## 8.2 Server-state reasoning

For server data, identify:

```text
backend authoritative resource
query owner
query key scope
cache owner
mutation owner
realtime owner
workspace/account transition behavior
```

Do not duplicate durable server entities into a local state store merely to make component access easier.

---

## 8.3 Query keys

Before introducing a key, answer:

- global/account/workspace/resource scope?
- product capability owner?
- stable resource identity?
- invalidation relationships?
- permission/tenant dimension?
- list/detail relationship?

Do not create component-local ad-hoc key conventions.

---

## 8.4 Mutations

For each material mutation, define:

```text
precondition
optimistic eligibility
snapshot
optimistic patch
authoritative request
success reconciliation
failure rollback
conflict handling
invalidate/refetch
realtime interaction
```

Not every mutation should be optimistic.

Do not optimize latency by creating ambiguous business state.

---

## 8.5 Workspace/account transitions

Treat scope transitions as state ownership changes.

Review:

- active requests;
- query cache;
- realtime subscription;
- local derived state;
- route state;
- permission state.

A late response/event from the previous scope MUST NOT overwrite new-scope state.

---

## 8.6 Realtime

Realtime messages are completed/authoritative facts or notifications according to their contract.

Before handling an event, determine:

```text
identity
scope
resource
version/sequence
duplicate behavior
out-of-order behavior
gap behavior
patch/invalidate/refetch strategy
```

If ordering/version certainty is lost, recover from authoritative data instead of guessing.

---

## 8.7 Host reasoning

### Web

Browser-rich authenticated application.

Web-specific runtime behavior belongs in web runtime/host-safe owners.

### Mobile

Native environment.

Do not import DOM/react-dom/web-only production dependencies into native-safe paths.

### Marketing

Public acquisition/brand/SEO surface.

Do not make marketing the owner of authenticated application server state.

---

## 8.8 UI/design-system reasoning

Before creating a primitive/component, determine:

- product-wide primitive?
- host-specific primitive?
- product-capability component?
- workflow component?
- third-party/vendor derivative?

Do not hide business workflows in generic primitives.

Accessibility is part of behavior.

Consider:

- keyboard;
- focus;
- pointer;
- touch;
- screen reader;
- loading;
- empty;
- error;
- permission;
- conflict;
- reduced motion.

---

# 9. Cross-stack contract protocol

A change crossing backend/frontend or async boundaries requires explicit producer/consumer analysis.

## 9.1 Identify the producer

Examples:

```text
API/OpenAPI producer
integration event producer
realtime producer
package export producer
schema producer
```

---

## 9.2 Identify all consumers

Do not stop at the first obvious frontend call site.

Inspect:

- web;
- mobile;
- marketing where relevant;
- automation;
- integrations;
- analytics;
- background consumers;
- generated clients;
- tests;
- external clients where documented.

---

## 9.3 Classify compatibility

```text
internal/private
additive compatible
behavior-compatible
breaking semantic
breaking shape
breaking persistence
breaking authorization/scope
```

If breaking, apply `NRX-008`.

---

## 9.4 Change producer first only when rollout is compatible

Deployment sequencing may require:

```text
producer supports old + new
→ consumers migrate
→ old compatibility removed
```

Do not assume one-shot backend/frontend deploy unless operational architecture explicitly guarantees it.

---

## 9.5 Generated artifacts

If a contract has a generator:

- modify producer;
- regenerate;
- verify generated output;
- never hand-edit generated consumer artifact.

---

# 10. Investigation protocol

Agents MUST investigate enough surrounding behavior to avoid local fixes that violate larger contracts.

## 10.1 Read the owner

Read canonical topic docs before editing.

Do not rely on filenames alone.

---

## 10.2 Inspect source neighborhood

Inspect:

- target type/module;
- interfaces/contracts;
- callers;
- consumers;
- tests;
- registration/composition;
- generated outputs;
- migrations;
- architecture tests.

---

## 10.3 Search for semantics, not only symbol names

When names have evolved, search:

- old and new product nouns;
- public contract identities;
- event names;
- schema columns;
- package exports.

A semantic migration may leave transitional names.

---

## 10.4 Inspect negative behavior

Look for tests/guards showing what MUST NOT happen.

Examples:

- rejection leaves version unchanged;
- architecture import forbidden;
- unauthorized query denied;
- duplicate event ignored;
- stale response rejected.

Negative tests often reveal stronger architecture than happy-path implementation.

---

## 10.5 Inspect generated producers

Before moving/deleting generated docs/types, locate the producer.

Unknown producer is a stop condition.

---

# 11. Evidence hierarchy

Use evidence according to the question.

There is no universal “source always wins”.

## 11.1 Product meaning

Strongest evidence:

```text
PRODUCT.md
owning product context
accepted product/system decision
behavior tests
```

Current incidental schema/source shape is weaker for product meaning.

---

## 11.2 Current implementation

Strongest evidence:

```text
source
tests
manifests
migrations
generated artifacts
CI
```

---

## 11.3 Architecture intent

Strongest evidence:

```text
RULE.md
canonical architecture owner
accepted ADR
architecture tests/manifest
```

---

## 11.4 Public contract

Strongest evidence:

```text
contract producer
OpenAPI/schema/event contract
generated client
contract tests
```

---

## 11.5 Historical rationale

Strongest evidence:

```text
ADR
commit/PR history
```

Roadmaps/audits are historical evidence only when still present for migration review.

---

# 12. Drift classification

When documentation and source disagree, classify before editing.

## `DOC_STALE`

Implementation and evidence consistently demonstrate accepted current behavior; canonical docs were not updated.

Action:

- update canonical docs;
- update references/generated indices;
- verify no semantic change is being hidden.

---

## `SOURCE_DEBT`

Canonical architecture/product intent is clear; source violates it.

Action:

- implement toward canonical target;
- update/keep explicit exception if migration cannot finish;
- do not update docs to bless accidental debt.

---

## `TRANSITION`

Both old and target structures intentionally coexist.

Action:

- identify transition owner;
- identify new-code rule;
- identify completion/removal condition;
- do not use legacy pattern as precedent.

---

## `CONTRACT_CHANGE`

The intended contract itself is changing.

Action:

- update owner;
- migration/consumer impact;
- ADR where needed;
- update tests/gates/generated evidence.

---

## `UNRESOLVED`

Evidence is insufficient or conflicting.

Action:

- stop;
- record the exact decision needed;
- do not guess.

---

# 13. Stop conditions

Stop implementation and surface the unresolved decision when any of the following applies.

## Product

- two contexts plausibly own the same authoritative fact;
- requested behavior changes product meaning but no product decision exists;
- a new bounded context/service is being inferred from folder/team/table structure.

## Security

- authorization/tenant behavior cannot be proven safely;
- read/list/search/export/realtime scope is ambiguous;
- a background/system principal model is undefined;
- destructive data policy is unclear.

## Domain/consistency

- aggregate boundary is ambiguous and affects invariants;
- transaction owner is unclear;
- expected-version behavior is unsupported/contradictory;
- a failure can leave partial business mutation with no approved semantics.

## Contracts

- API/event/realtime/persisted contract has multiple active meanings;
- consumer inventory cannot be established for a breaking change;
- generated artifact producer is unknown.

## Reliability

- message identity/consumer identity is ambiguous;
- retry can duplicate business/provider effects with no idempotency/reconciliation contract;
- ordering semantics conflict across callers/tests.

## Frontend

- architecture manifest conflicts with intended package ownership;
- web/mobile runtime ownership is unclear;
- query/server-state owner is ambiguous;
- realtime protocol lacks enough identity/scope/version to reconcile safely.

## Decisions

- accepted ADR conflicts with source and there is no superseding decision;
- implementing the task would permanently weaken a `NRX-*` rule without an explicit rule/architecture change request.

---

# 14. No-guess policy

The following are not acceptable substitutes for a decision:

- “this seems cleaner”;
- “this is common in enterprise systems”;
- “the existing code does it here”;
- “the framework recommends it”;
- “another project uses it”;
- “the database table exists”;
- “the UI needs it”;
- “we can refactor later”.

Use general engineering knowledge to evaluate options.

Do not use it to invent Notrelix-specific product/security/ownership semantics without evidence.

---

# 15. Implementation contract

Once ownership and contract are clear, implementation must be the smallest **complete** change.

Small does not mean “one file”.

A complete vertical change may include:

```text
product/domain semantics
Application use case
persistence/migration
Platform/event
API contract
generated client
frontend capability
tests/gates
documentation
```

If all are required for correctness, omitting half of them is not a smaller correct change.

---

## 15.1 Preserve unrelated work

Do not:

- reset unrelated files;
- delete user changes;
- reformat unrelated modules;
- rewrite large areas without need;
- update generated outputs unrelated to the changed producer.

Keep the diff focused.

---

## 15.2 Avoid speculative abstraction

Do not introduce:

- generic helper;
- shared service;
- shared enum;
- framework abstraction;
- new package/project

solely because it might be useful later.

Use `NRX-006`.

---

## 15.3 Remove obsolete transitional code when the migration completes

Do not preserve:

- aliases;
- adapters;
- compatibility exports;
- allow-list exceptions;
- duplicate handlers

without an active consumer/transition reason.

A completed migration should reduce debt.

---

# 16. Generated-file protocol

Before editing a file suspected to be generated:

1. inspect its header;
2. find generator;
3. find generation command;
4. modify producer;
5. regenerate;
6. run drift check.

Never hand-edit generated exact inventories as the primary fix.

Known examples include frontend package boundaries and future generated documentation/project maps.

---

# 17. Migration protocol

For schema/public/product contract changes, determine whether an expand/contract or staged rollout is required.

Typical sequence:

```text
introduce compatible producer/schema
→ deploy
→ migrate/backfill consumers/data
→ verify
→ remove old compatibility
```

Do not:

- drop before consumers migrate;
- rewrite historical migrations casually;
- assume old messages/clients disappear instantly;
- remove old event/API values without durable-data analysis.

Read the migration canonical owner for detailed procedure.

---

# 18. Documentation update protocol

Documentation is part of the change when the contract changes.

## Update canonical docs when

- product semantics change;
- bounded-context ownership changes;
- architecture boundary changes;
- pipeline/transaction contract changes;
- public contract evolution changes;
- lifecycle/deletion meaning changes;
- state/realtime ownership changes;
- test/gate expectations change;
- operational/config/migration contract changes.

## Do not update canonical docs merely because

- a private helper moved;
- formatting changed;
- an implementation detail changed without contract consequence.

## When moving authority

If a topic's canonical owner changes:

1. update the new owner;
2. update topic authority map;
3. update routers/summaries;
4. update generated indices;
5. remove old competing definition;
6. run docs governance.

Never leave both old and new canonical generations active.

---

# 19. ADR protocol

Create/supersede an ADR for consequential choices such as:

- architecture boundary;
- framework/runtime split;
- persistence/security mechanism with durable implications;
- significant contract evolution policy;
- new execution/consistency model;
- major exception becoming permanent architecture.

Do not create ADRs for trivial local implementation choices.

Do not rewrite accepted ADR history to pretend a later decision was always the original decision.

---

# 20. Test and validation selection

Validation must match change risk.

Do not run only the easiest command.

Do not run every repository command blindly when a focused gate is sufficient during iteration.

Use focused → broader progression.

---

## 20.1 Domain behavior

During implementation:

```text
Domain focused tests
```

Before completion as relevant:

```text
Domain suite
Architecture tests
affected Application/integration tests
```

Required proof should include success/reject/no-op/failure-atomicity/version/event/lifecycle cases when those semantics changed.

---

## 20.2 Application/pipeline

Run:

```text
Application focused tests
Application suite
Architecture tests
affected integration tests
```

Add/verify:

- authorization;
- transaction;
- expected version;
- pipeline order;
- post-commit;
- idempotency

according to changed contract.

---

## 20.3 Infrastructure/data

Run relevant:

```text
Infrastructure tests
Integration tests
migration checks
RLS tests
Architecture tests
```

Do not rely only on mocked persistence tests for schema/RLS behavior.

---

## 20.4 Platform/messaging

Run relevant:

```text
Platform tests
integration/production-graph tests
```

Verify:

- duplicate;
- retry;
- failure;
- ordering;
- poison;
- outbox;
- recovery;
- tenant context.

---

## 20.5 API/public contract

Run relevant:

```text
API tests
OpenAPI drift
codegen/contract checks
integration tests
```

If frontend generated contract changes, run affected frontend contract/type tests.

---

## 20.6 Frontend capability

Iterate with focused workspace tests.

Before completion choose relevant:

```bash
pnpm typecheck
pnpm lint
pnpm test
pnpm check:architecture
pnpm check:architecture-docs
pnpm codegen:check
pnpm validate:fast
pnpm validate
```

Use actual current `frontend/package.json` as command authority.

Do not invent script names from documentation.

---

## 20.7 Documentation

For documentation/core authority changes, run:

```bash
make docs-check
```

Once the documentation-core tooling target is implemented, also run the required generated/authority/source-alignment checks.

A documentation gate that cannot execute its required producer is a failed/unrun gate, not a successful skip.

---

# 21. Non-zero evidence requirement

Comply with `NRX-016`.

When a test/gate is required:

- confirm it executed relevant work;
- confirm intended project/package scope;
- report test/gate count when the wrapper exposes it;
- do not hide skips.

If a command returns success but zero protected tests ran, treat the proof as insufficient.

---

# 22. Worktree and Git safety

Coding Agents MUST preserve user/repository work.

## Never by default

```text
git reset --hard
git clean -fd
git checkout -- .
git restore .
force push
history rewrite
mass delete
```

unless the explicit task requires it and the consequences are understood.

## Before destructive migration

- inventory files;
- confirm generated producer;
- confirm knowledge/consumer migration;
- confirm no unrelated worktree changes.

## Commits

When asked to commit:

- keep commit logically scoped;
- do not hide unrelated changes;
- separate large migration stages when reviewability benefits.

---

# 23. External dependency/provider protocol

When behavior depends on another context or provider, do not duplicate its authority.

Examples:

### Work Management references Identity principal

Application obtains/validates required Identity fact.

Domain consumes explicit fact/stable ID.

Work Management does not query Identity infrastructure from Domain.

### Integration invokes provider

Integration adapter owns provider-specific request.

Product context owns product intent.

Provider response is translated back into stable product/integration contract.

### Billing entitlement affects product operation

Billing owns entitlement.

Product/Application consumes entitlement decision/fact.

Billing does not directly mutate Work Management persistence.

---

# 24. Observability protocol

Observability must help diagnose behavior without weakening security.

For material distributed/async flows, preserve stable identifiers where safe:

- request/operation ID;
- message/event ID;
- consumer/execution ID;
- tenant/resource scope identifiers according to logging policy;
- correlation/trace ID.

Do not log:

- secrets;
- raw credentials;
- unnecessary sensitive payloads.

Operational logs are not product/audit truth unless an explicit audit contract says otherwise.

---

# 25. Performance/scalability protocol

Agents MUST consider scale when changing query/state models.

Red flags:

- loading all BoardItems then filtering in memory;
- loading all workspace data for one resource check;
- unbounded event/realtime history;
- tenant-blind cache;
- unindexed dynamic field query;
- N+1 provider/database calls;
- rendering unbounded dense lists without virtualization/windowing when scale requires it.

Do not invent performance complexity prematurely.

But do not choose an architecture known to require full-tenant/full-board scans for routine operations.

Read repository quality/performance docs when the change affects scale.

---

# 26. Example reasoning flows

These examples illustrate process, not hard-coded implementation.

---

## Example A — Add a new BoardField type

Classify:

```text
Product semantic change
Backend Domain/Application/API
Frontend product/UI
Contract/data/indexing
possibly Automation/Analytics
```

Read:

```text
PRODUCT.md
docs/product/contexts/work-management.md
RULE.md
backend Domain/Application/API docs
frontend state/UI/API docs
```

Investigate:

- field-type registry;
- settings/value representation;
- validation;
- equality/no-op;
- filter/sort/group;
- persistence/indexing;
- API/OpenAPI;
- generated frontend contract;
- editor/renderer;
- automation;
- analytics;
- tests.

Do not implement only:

```text
enum member + React component
```

and call the field type complete.

---

## Example B — Add a protected workspace query

Classify:

```text
Backend use case
Security/tenant
API
Frontend query
```

Apply:

```text
NRX-003
NRX-004
NRX-014
```

Investigate:

- authoritative workspace/resource scope;
- Application authorization marker/pipeline;
- query result contract;
- RLS;
- frontend query-key scope;
- permission error UI;
- tests.

Do not assume reads need less authorization because they do not mutate.

---

## Example C — Change an event payload

Classify:

```text
Cross-boundary contract
Reliability/messaging
possibly migration
```

Apply:

```text
NRX-007
NRX-008
NRX-010
```

Investigate:

- logical event identity;
- producer;
- outbox;
- all consumers;
- durable in-flight/old messages;
- automation/realtime/integration consumers;
- schema/version strategy.

Do not rename/break because the C# record changed.

---

## Example D — Add a frontend package dependency

Classify:

```text
Frontend architecture change
```

Read:

```text
frontend/AGENTS.md
frontend/docs/architecture/dependency-boundaries.md
frontend architecture manifest
```

Ask:

- what semantic ownership requires this dependency?
- is the dependency direction permitted?
- should behavior move instead?
- is this actually a shared abstraction problem?

Do not edit manifest solely to make an import compile.

---

## Example E — Change delete behavior

Classify:

```text
Product lifecycle
Domain
Data migration
Cross-context impact
```

Apply:

```text
NRX-001
NRX-008
NRX-011
```

Investigate:

- owning lifecycle;
- references;
- retention;
- audit;
- analytics;
- integrations;
- provider effects;
- restore/purge;
- historical records;
- schema/migration.

Do not add `IsDeleted` as the first design step.

---

## Example F — Fix duplicate background processing

Classify:

```text
Platform/reliability
possibly product side-effect
```

Apply:

```text
NRX-009
NRX-010
```

Investigate:

- message identity;
- consumer identity;
- dedup key;
- claim/commit timing;
- crash point;
- retry;
- provider idempotency;
- ordering;
- tests.

Do not solve only by increasing retry delay.

---

# 27. Documentation-specific agent behavior

Documentation work is not exempt from source verification.

When writing canonical docs:

1. identify the topic owner;
2. read current source/tests/manifests;
3. read retained legacy knowledge only as evidence;
4. classify conflict;
5. write current intended contract;
6. add source/evidence references;
7. avoid copying machine-derived inventories manually;
8. update authority map/index;
9. run docs governance.

Do not generate generic architecture prose merely because a template has a section.

If a section is not relevant, state non-responsibility or omit it according to the documentation contract.

---

# 28. Forbidden agent behaviors

The following are explicitly prohibited unless the task intentionally changes the underlying contract.

## Product

- infer a bounded context from folder/table/team;
- redefine product vocabulary locally;
- model one view as independent authoritative data.

## Backend

- Domain → Infrastructure/provider dependency;
- business rules in API;
- ad-hoc authorization bypass;
- arbitrary handler-owned commit when pipeline owns transaction;
- external lookup inside Domain mutation;
- publish completed fact before durable success.

## Frontend

- deep imports across package boundaries;
- app-internal reusable business logic;
- web/DOM dependency in native-safe path;
- tenant-blind server-state key;
- permanent local duplicate of server entity;
- realtime-only durable truth.

## Contracts/data

- breaking change without migration;
- hand-copy generated contract;
- edit historical migration casually;
- remove compatibility before consumer migration.

## Reliability

- retry non-idempotent effect without identity/reconciliation;
- dedup by overly broad event type;
- mark success before protected work completes.

## Documentation

- create competing canonical owner;
- resurrect roadmap/freeze/audit as architecture;
- create `*-final-vN` authority generation;
- add scoped docs for symmetry;
- leave absolute workstation links;
- hand-edit generated inventory.

## Validation

- claim tests passed when not run;
- accept required zero-test success;
- silence a failed generator/gate;
- weaken architecture tests to make implementation pass.

---

# 29. Documentation change triggers

Update documentation in the same change when any of these occur.

## Root/product

- product thesis changes;
- bounded-context ownership changes;
- repository invariant changes;
- product design constitution changes.

## Cross-stack

- contract/versioning model changes;
- consistency ownership changes;
- event/realtime taxonomy changes;
- extraction strategy changes.

## Backend

- project responsibility/reference changes;
- Domain mutation/lifecycle/event contract changes;
- Application pipeline/transaction/auth contract changes;
- Infrastructure/RLS/data architecture changes;
- Platform delivery/idempotency/ordering changes;
- API/OpenAPI contract changes;
- test/gate topology changes.

## Frontend

- package family/dependency architecture changes;
- host responsibility changes;
- query/realtime/state ownership changes;
- UI implementation contract changes;
- test/gate topology changes.

## Operations

- configuration precedence;
- migration process;
- deployment/runtime;
- recovery/incident contract.

---

# 30. Completion report contract

For a material task, the final report MUST be concise but complete.

Include:

## Baseline

```text
branch
baseline commit/SHA when relevant
pre-existing worktree changes relevant to safety
```

## Ownership

```text
product context/capability
technical owner
applicable NRX rules
```

## Change

```text
what changed
why
files/areas
```

## Contracts

State whether the change touched:

- product semantics;
- API;
- event/message;
- realtime;
- package export;
- schema/data;
- authorization/tenant;
- migration.

## Evidence

List exact:

- tests;
- architecture gates;
- generated checks;
- build/typecheck/lint;
- docs checks.

Do not say “all tests pass” when only a focused suite was run.

## Unrun checks

List material checks that were not run and why.

## Remaining risks/decisions

Report unresolved external dependencies, migrations, rollout requirements, or known limitations.

---

# 31. Definition of complete agent work

A Coding Agent task is complete only when, as applicable:

- product owner is correct;
- repository invariants are preserved;
- architecture placement is correct;
- tenant/security behavior is safe;
- consistency/transaction semantics are complete;
- public/persisted contracts are compatible or migrated;
- retry/idempotency is handled where required;
- frontend state converges to authoritative state;
- accessibility/host requirements are addressed;
- focused tests prove behavior;
- broader required gates prove architecture/contracts;
- generated artifacts are synchronized;
- documentation is updated when contract changed;
- transitional compatibility/debt introduced by the task is explicit;
- completion report accurately states evidence.

Compilation alone is not completion.

A green UI alone is not completion.

A passing happy-path test alone is not completion.

The standard is:

> **the smallest complete change that preserves Notrelix product semantics and protected architecture under success, rejection, retry, concurrency, scope transition, and failure conditions relevant to that change.**
