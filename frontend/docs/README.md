---
document_id: FE-DOCS-INDEX
document_type: documentation-index
status: active
owner: frontend-platform
applies_to:
  - frontend-documentation
  - frontend-architecture
  - frontend-decisions
  - frontend-generated-evidence
evidence:
  - frontend/README.md
  - frontend/AGENTS.md
  - frontend/docs/architecture/
  - frontend/docs/decisions/
  - frontend/docs/generated/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
review_on:
  - frontend-documentation-tree-change
  - frontend-authority-routing-change
  - frontend-architecture-topic-change
  - frontend-generated-evidence-change
---

# Frontend Documentation

> **This directory routes frontend engineering knowledge to one authoritative owner per topic.**
>
> Use authored architecture for durable semantics, ADRs for historical consequential decisions, generated files for source-derived facts, and source/tests/CI for current executable evidence.

This index is the canonical navigation entrypoint for frontend documentation.

It does not duplicate the contents of every frontend architecture file.

---

# 1. Reading order

For normal frontend work:

```text
1. ../README.md
   → workspace orientation and commands

2. ../AGENTS.md
   → coding-agent execution contract

3. architecture/<topic>.md
   → current durable architecture for the concern

4. decisions/FE-ADR-*.md
   → historical rationale when needed

5. generated/*
   → source-derived package facts

6. source / tests / manifests / CI
   → current executable evidence
```

For product semantics, also read the relevant repository product/context documentation.

Do not infer product rules solely from frontend source.

---

# 2. Authority model

Frontend documentation has three distinct knowledge classes:

```text
Authored current architecture
→ architecture/

Historical decisions
→ decisions/

Generated current facts
→ generated/
```

They are intentionally different.

---

# 3. Current architecture

Current frontend architecture lives under:

```text
frontend/docs/architecture/
```

Use these documents to answer:

```text
How should this system work now?
Where should this behavior live?
What dependency direction is valid?
What state/runtime/UI boundary owns this behavior?
What proof is required when it changes?
```

---

# 4. Historical decisions

Frontend ADRs live under:

```text
frontend/docs/decisions/
```

Use ADRs to answer:

```text
Why was this consequential architecture choice made?
What alternatives were considered?
What compatibility consequence existed?
What later ADR superseded it?
```

Do not use an old ADR as the first description of current architecture.

Check status and supersession.

---

# 5. Generated evidence

Generated frontend documentation lives under:

```text
frontend/docs/generated/
```

Current package-boundary evidence:

```text
generated/package-boundaries.md
```

is generated from:

```text
tooling/dependency-rules/src/architecture-manifest.ts
```

Do not hand-edit generated evidence.

Change its producer/input, regenerate, review the diff, and commit both as required.

---

# 6. Exact package dependency authority

The exact governed package universe and allowed internal import graph is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

The generated readable representation is:

```text
frontend/docs/generated/package-boundaries.md
```

Architecture docs explain **why the layers exist and what they mean**.

The manifest defines **which exact package may import which exact package now**.

Do not maintain another handwritten package matrix here.

---

# 7. Architecture topics

## 7.1 Frontend overview

Read:

```text
architecture/frontend-overview.md
```

for:

```text
system/client boundary
three-host architecture
package-family model
layer meanings
apps-as-composition-roots
frontend versus backend authority
product packages versus feature packages
web/mobile/marketing separation
top-level dependency principles
freeze meaning
```

This is the first architecture document to read when ownership is unclear.

---

## 7.2 Dependency boundaries

Read:

```text
architecture/dependency-boundaries.md
```

for:

```text
closed-world manifest
allowed internal imports
public exports
deep imports
foundation purity
runtime/platform safety
mobile purity
package creation/removal/move
dependency-rule gates
generated boundary docs
```

Use this before changing the architecture manifest or cross-package imports.

---

## 7.3 Hosts, composition and routing

Read:

```text
architecture/hosts-composition-routing.md
```

for:

```text
Vite web host
Expo mobile host
Next marketing host
provider composition
routing/navigation
session/bootstrap
runtime adapter construction
host environment
host error boundaries
workspace/account transitions
```

Apps compose.

They do not become general product/business layers.

---

## 7.4 API and contracts

Read:

```text
architecture/api-and-contracts.md
```

for:

```text
OpenAPI/generated client contracts
REST request/response boundary
error mapping
auth/session transport
idempotency/concurrency contract
pagination/filter/sort
version compatibility
generated DTO ownership
```

Backend producer contracts remain authoritative for the wire contract.

---

## 7.5 State, query and mutations

Read:

```text
architecture/state-query-mutations.md
```

for:

```text
server state
query keys
cache ownership
invalidations
mutations
optimistic updates
conflicts
workspace/account transitions
local state taxonomy
client persistence
```

Frontend cache is derived state, not backend persistence truth.

---

## 7.6 Realtime

Read:

```text
architecture/realtime.md
```

for:

```text
connection lifecycle
authentication/reconnect
subscription scope
duplicate/out-of-order delivery
gap handling
query reconciliation
product event adapters
permission/workspace transitions
```

Realtime supplements authoritative server state.

It does not become a second product database.

---

## 7.7 UI and design system

Read:

```text
architecture/ui-and-design-system.md
```

for:

```text
tokens
web/mobile UI split
icons
component ownership
accessibility
interaction state
motion
density
responsive behavior
theme
Storybook/visual evidence
```

Do not put product semantics into generic UI primitives.

---

## 7.8 Testing and quality gates

Read:

```text
architecture/testing-and-quality-gates.md
```

for:

```text
node/web/mobile/integration/tooling tests
guarded non-zero execution
architecture tests
codegen drift
UI accessibility/visual tests
host builds
E2E
CI evidence
exact-revision certification
```

Use the cheapest reliable test seam for the protected property.

---

## 7.9 Architecture change policy

Read:

```text
architecture/architecture-change-policy.md
```

before changing:

```text
package-layer ownership
allowed dependency direction
host framework boundary
runtime ownership
state authority
generated contract architecture
public package export foundation
mobile/web isolation
```

Do not edit the manifest until an architectural dependency is justified.

---

# 8. Frontend decisions

The frontend ADR registry is:

```text
decisions/README.md
```

Current historical decision IDs include:

```text
FE-ADR-001 — Framework split
FE-ADR-002 — Package manager
FE-ADR-003 — Package exports
FE-ADR-004 — No Next in packages
FE-ADR-005 — Auth session model
```

The registry is the authority for current ADR status.

If an accepted decision changes:

```text
create a new FE-ADR
mark the old ADR Superseded
link both directions
update current architecture
update source/tests/generated evidence
```

Do not rewrite accepted history silently.

---

# 9. Repository product docs

Frontend documentation does not redefine product semantics.

For product meaning use repository product authorities such as:

```text
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/*
```

Examples:

```text
role/permission semantics
→ Governance product docs

Workspace membership semantics
→ Workspaces product docs

Board/Item semantics
→ Work Management product docs

Page/Block semantics
→ Documents product docs

automation definition/execution semantics
→ Automation product docs
```

Frontend docs define the **client implementation architecture** for consuming those semantics.

---

# 10. Repository architecture docs

Frontend architecture also participates in system-wide contracts.

Read repository architecture when changing:

```text
bounded-context interaction
cross-system contract
data ownership
realtime/delivery boundary
service extraction
```

Relevant owners include:

```text
docs/architecture/system-overview.md
docs/architecture/bounded-context-map.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

Frontend-specific docs narrow those system rules into client architecture.

---

# 11. Quality docs

Repository-wide quality owners include:

```text
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/accessibility-standard.md
docs/quality/performance-and-scalability.md
```

Frontend architecture applies these standards to client-specific test/runtime/UI boundaries.

Do not create a competing frontend version of repository-wide policy unless frontend behavior genuinely differs.

---

# 12. Delivery docs

Use repository delivery docs for:

```text
change classification
definition of done
contract-first delivery
release/rollout
migration
team ownership
local development
```

Frontend architecture documents should state frontend-specific obligations, then route to these shared policies.

---

# 13. Generated file rule

A generated document must identify:

```text
producer
input/source authority
regeneration command
drift-check command or gate
```

If any of these are missing, treat the generated status as incomplete governance.

---

# 14. Generated package boundaries

Current generated package map is produced by:

```text
@notrelix/dependency-rules
```

from:

```text
tooling/dependency-rules/src/architecture-manifest.ts
```

Use:

```bash
pnpm --filter @notrelix/dependency-rules docs:generate
```

to regenerate according to current tooling.

Use the root architecture-doc check for CI/local verification.

---

# 15. Source evidence

Source is evidence of current implementation.

It is not automatically architectural precedent.

If source conflicts with current authored architecture/ADR:

```text
do not choose source automatically
```

Classify the conflict.

---

# 16. Drift classification

Use repository documentation governance categories:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Examples:

```text
docs say package A imports B but manifest no longer does
→ likely DOC_STALE if generated/authored doc was not refreshed

manifest allows dependency contrary to accepted architecture
→ possible SOURCE_DEBT or architecture change

old/new package path coexist during migration
→ TRANSITION

backend OpenAPI contract changed intentionally
→ CONTRACT_CHANGE

product ownership unclear
→ UNRESOLVED
```

Do not hide drift by editing whichever file is easiest.

---

# 17. Freeze artifacts

Freeze plans/certificates/audits are evidence of a point-in-time review.

They are not the current architecture owner.

Do not move:

```text
roadmap
freeze checklist
migration tracker
audit report
```

into architecture authority.

Extract durable knowledge first, then retire the temporary artifact.

---

# 18. Package README files

A package-local README MAY explain:

```text
local public API
local setup
package-specific examples
package-specific constraints
```

only when genuinely useful.

It MUST NOT create a competing repository architecture.

If the package rule is already determined by:

```text
architecture docs
manifest
AGENTS
```

route to those owners rather than copy them.

---

# 19. App README files

Host-local README files are optional.

Create one only if the host has operational/development knowledge that cannot be represented cleanly in:

```text
frontend/README.md
architecture/hosts-composition-routing.md
repository infrastructure/local-development docs
```

Do not add documentation files merely for symmetry.

---

# 20. Tooling docs

Tooling packages may document:

```text
generator contract
checker usage
configuration
extension mechanism
```

when developers need local operational guidance.

The tooling README must route back to canonical architecture when describing why a rule exists.

Tool implementation does not become architectural authority by documentation volume.

---

# 21. Read by change type

## New feature

Read:

```text
frontend-overview
relevant product docs
dependency-boundaries
state-query-mutations
API/realtime/UI docs as affected
testing-and-quality-gates
```

## New package

Read:

```text
frontend-overview
dependency-boundaries
architecture-change-policy
testing-and-quality-gates
```

## New endpoint consumption

Read:

```text
api-and-contracts
state-query-mutations
backend API contract
testing-and-quality-gates
```

## Realtime change

Read:

```text
realtime
state-query-mutations
system realtime/delivery architecture
testing-and-quality-gates
```

## New reusable UI primitive

Read:

```text
ui-and-design-system
dependency-boundaries
accessibility standard
testing-and-quality-gates
```

## Host/router/provider change

Read:

```text
hosts-composition-routing
frontend-overview
state/query or realtime docs if lifecycle changes
```

---

# 22. Read by failure type

## Architecture checker failure

Read:

```text
dependency-boundaries.md
architecture-change-policy.md
architecture-manifest.ts
generated/package-boundaries.md
```

## Contract codegen drift

Read:

```text
api-and-contracts.md
backend producer contract
codegen tooling
```

## Mobile dependency failure

Read:

```text
dependency-boundaries.md
hosts-composition-routing.md
frontend-overview.md
```

## Realtime state inconsistency

Read:

```text
realtime.md
state-query-mutations.md
backend realtime contract
```

## Visual/accessibility regression

Read:

```text
ui-and-design-system.md
testing-and-quality-gates.md
repository accessibility standard
```

---

# 23. Documentation change workflow

When changing frontend architecture:

```text
1. identify canonical topic
2. identify whether an ADR is required
3. update canonical authored doc
4. update source/manifest
5. update tests/gates
6. regenerate generated evidence
7. update decision registry if needed
8. run documentation/architecture checks
```

Do not update generated docs by hand.

---

# 24. Documentation-only correction

If only docs are stale and architecture/source are already correct:

```text
classify DOC_STALE
update the one canonical owner
regenerate generated docs if applicable
run docs checks
```

Do not create an ADR for a typo/stale explanation.

---

# 25. Source-only correction

If source violates an accepted architecture:

```text
classify SOURCE_DEBT
repair source
add/repair executable gate
leave accepted architecture intact
```

Do not rewrite docs to bless accidental source drift.

---

# 26. Consequential architecture change

If intended architecture itself changes:

```text
classify change
use architecture-change-policy
create/supersede FE ADR if required
update canonical architecture
update manifest/source
update tests/generated evidence
```

The ADR explains why.

The architecture document explains how the system works now.

---

# 27. Documentation metadata

Canonical frontend docs follow repository metadata conventions:

```text
document_id
document_type
status
owner
applies_to
evidence
review_on
```

Generated docs use:

```text
status: generated
```

when the producer supports the governed metadata contract.

Do not invent another frontend metadata format.

---

# 28. Normative language

Use:

```text
MUST
MUST NOT
SHOULD
SHOULD NOT
MAY
```

for actual durable rules.

Avoid making every explanatory sentence normative.

A rule ID should protect a real decision/boundary.

---

# 29. Rule namespaces

Frontend rule namespaces include:

```text
FE-ARCH
FE-DEP
FE-STATE
FE-RT
FE-UI
FE-TST
```

Use the namespace belonging to the canonical topic.

Do not reuse the same rule ID for different rules.

---

# 30. Documentation depth

Depth follows risk.

Examples:

```text
dependency architecture
state/query/realtime
UI design-system foundation
testing/gates
```

deserve deeper authored docs.

A small local package does not automatically need an equally large README.

---

# 31. Duplication rule

If two docs state the same durable rule, one should normally become:

```text
owner
```

and the other:

```text
route/reference
```

Do not keep both copies “synchronized manually.”

---

# 32. Exact inventories

Do not hand-maintain changing lists such as:

```text
all current package dependencies
all package paths
all current tests
all current exports
```

when the information can be generated reliably.

Use generated evidence/tooling.

---

# 33. Semantic inventories

Human-authored docs MAY maintain a stable semantic classification such as:

```text
foundation
runtime
UI
product
feature
app
```

because the meaning of those categories is architecture.

The exact package membership remains generated/executable.

---

# 34. Current generated package count

The generated package-boundary document currently reports a package count.

That number is **current evidence**, not an architectural constant.

Do not copy it into multiple canonical docs as a requirement.

---

# 35. Decisions versus generated facts

Example:

```text
FE-ADR-004
→ why Next.js is constrained to the intended host boundary

architecture/dependency-boundaries.md
→ current normative Next/package rule

architecture-manifest.ts
→ exact packages/imports currently allowed

generated/package-boundaries.md
→ readable generated view
```

These layers complement each other.

---

# 36. Documentation CI

Repository documentation governance is expected to verify:

```text
links
metadata
authority
rule IDs
source inventory
generated drift
```

Frontend-specific generated/architecture checks also remain part of frontend tooling/CI.

A green markdown link check alone is not enough to prove architecture docs are current.

---

# 37. Stop conditions

Stop documentation work if:

- you are about to create a second package-dependency matrix;
- you are copying a product rule into frontend architecture instead of routing to its owner;
- you are rewriting an Accepted FE ADR to match current source without supersession;
- you are hand-editing `generated/package-boundaries.md`;
- you are creating `frontend/RULES.md`, `frontend/ARCHITECTURE.md`, or a migration tracker as new authority;
- current source and canonical architecture disagree and the drift has not been classified;
- a new package-local doc would exist only for symmetry;
- generated evidence has no producer or regeneration path;
- the current architecture owner for the topic is unclear.

---

# 38. Frontend documentation tree

Target canonical structure:

```text
frontend/
├── README.md
├── AGENTS.md
└── docs/
    ├── README.md
    ├── architecture/
    │   ├── frontend-overview.md
    │   ├── dependency-boundaries.md
    │   ├── hosts-composition-routing.md
    │   ├── api-and-contracts.md
    │   ├── state-query-mutations.md
    │   ├── realtime.md
    │   ├── ui-and-design-system.md
    │   ├── testing-and-quality-gates.md
    │   └── architecture-change-policy.md
    ├── decisions/
    │   ├── README.md
    │   └── FE-ADR-*.md
    └── generated/
        └── package-boundaries.md
```

This is deliberately smaller than a package-by-package documentation tree.

---

# 39. Final routing rule

Use:

```text
workspace orientation
→ frontend/README.md

agent execution
→ frontend/AGENTS.md

current frontend architecture
→ frontend/docs/architecture/

historical frontend decisions
→ frontend/docs/decisions/

exact generated package facts
→ frontend/docs/generated/

exact package graph authority
→ architecture-manifest.ts

current implementation
→ source/tests/config

CI proof
→ current workflow result for exact revision
```

The purpose of this index is to make the correct owner easy to find so that documentation does not become a second, manually synchronized implementation of the frontend.
