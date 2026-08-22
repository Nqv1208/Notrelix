---
document_id: FE-ARCH-FRONTEND-OVERVIEW
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend
  - frontend-workspace
  - frontend-hosts
  - frontend-package-architecture
  - frontend-product-client-boundary
evidence:
  - frontend/package.json
  - frontend/pnpm-workspace.yaml
  - frontend/turbo.json
  - frontend/apps/
  - frontend/packages/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/docs/generated/package-boundaries.md
  - frontend/docs/decisions/
review_on:
  - frontend-host-model-change
  - frontend-package-layer-change
  - frontend-product-feature-ownership-change
  - frontend-runtime-boundary-change
  - frontend-state-authority-change
  - frontend-framework-foundation-change
---

# Frontend Overview

> **The Notrelix frontend is a governed multi-host client platform. Apps compose host/runtime concerns; reusable product behavior lives in explicit package owners; server state remains backend-authoritative; the exact package graph is closed-world and executable.**
>
> The architecture is optimized for parallel product development, web/mobile portability, generated contract fidelity, and later evolution without turning the app hosts or foundation packages into a new monolith.

This document is the canonical top-level owner for the Notrelix frontend architecture.

It defines:

- frontend versus backend/system responsibility;
- the three host applications;
- package-family responsibilities;
- architecture-layer meanings;
- high-level dependency direction;
- product package versus feature package ownership;
- host/runtime/UI separation;
- server-state authority;
- web/mobile/marketing isolation;
- freeze meaning;
- architecture growth rules.

Exact per-package import allow-lists are **not** maintained here.

They are executable in:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

and rendered into:

```text
frontend/docs/generated/package-boundaries.md
```

---

# 1. Architecture objective

The frontend architecture exists to let teams build product capability in parallel while preserving:

```text
clear semantic ownership
explicit package dependency direction
backend-authoritative server state
web/mobile runtime isolation
reusable product behavior
stable generated contracts
replaceable host/runtime mechanisms
testable boundaries
accessible UI foundations
```

The target is not the maximum possible number of packages.

The target is the minimum set of durable boundaries needed for independent product/runtime evolution.

---

# 2. FE-ARCH-001 — Frontend is a client platform, not a second backend

Frontend may own:

```text
presentation
interaction
routing/navigation
client state
query/cache integration
optimistic UX
realtime reconciliation
runtime adapters
design-system implementation
```

Frontend MUST NOT become authoritative for:

```text
durable business state
tenant ownership
resource authorization
billing entitlement truth
server-side invariants
persistence
```

when those facts are backend/product owned.

---

# 3. System boundary

Conceptually:

```text
Product/system semantics
        ↓
Backend public/realtime contracts
        ↓
Generated/normalized frontend contracts
        ↓
Frontend product/feature state
        ↓
Platform adapters
        ↓
Host composition
        ↓
UI interaction
```

The frontend transforms authoritative server/product facts into client behavior.

It does not redefine them independently.

---

# 4. FE-ARCH-002 — Backend contract precedes durable client behavior

If a client feature requires:

```text
new server mutation
new permission outcome
new event
new conflict semantic
new entitlement fact
```

and the backend/product contract does not exist, the frontend MUST NOT invent a permanent client-only substitute.

Record the dependency and change the owning contract.

---

# 5. Three-host model

Notrelix currently has three host applications:

```text
apps/web
apps/mobile
apps/marketing
```

They have intentionally different runtime/framework requirements.

---

# 6. Web host

Current web package:

```text
@notrelix/app-web
```

Current host technology:

```text
Vite + React
```

The web host is the main authenticated browser application.

It composes:

```text
web runtime
router
query/session/realtime providers
product web adapters
feature packages
web UI
shell
host environment
```

according to narrower architecture documents.

---

# 7. Mobile host

Current mobile package:

```text
@notrelix/app-mobile
```

Current host technology:

```text
Expo / React Native
```

The mobile host composes:

```text
mobile runtime
native navigation/router
native UI
mobile product adapters
query/state
mobile lifecycle/platform integration
```

The production mobile graph must remain native-safe.

---

# 8. Marketing host

Current marketing package:

```text
@notrelix/app-marketing
```

Current host technology:

```text
Next.js
```

Marketing is isolated from the authenticated application product graph.

Its approved shared internal surface is intentionally narrow and visual.

---

# 9. FE-ARCH-003 — Host framework split is intentional

Do not introduce a custom universal host abstraction merely to make:

```text
Vite
Expo
Next
```

look structurally identical.

Share stable product/runtime/UI contracts where useful.

Keep framework-specific construction at the host/runtime boundary.

---

# 10. Host responsibility

A host owns:

```text
application startup
top-level provider composition
routing/navigation bootstrap
runtime construction
host environment binding
shell
host-specific error/loading boundaries
```

A host is a composition root.

---

# 11. FE-ARCH-004 — Apps compose; apps do not own reusable product semantics

Reusable:

```text
Board state
Document state
Automation behavior
cross-route account/workspace feature behavior
realtime product reconciliation
```

SHOULD NOT live in an app merely because that host uses it first.

Move it to the semantic package owner.

---

# 12. Package families

The workspace is organized into major families:

```text
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/dev/*
packages/features/*
tooling/*
```

These families separate reusable client concerns.

Their exact package inventory is generated/executable.

---

# 13. Architecture manifest

The executable closed-world authority is:

```text
tooling/dependency-rules/src/architecture-manifest.ts
```

Every governed app/package represented by the checker belongs to the manifest exactly once according to its contract.

Each entry defines:

```text
packageName
relativePath
layer
freezeScope
allowedInternalImports
```

---

# 14. FE-ARCH-005 — Exact dependency permission is executable

This document MAY explain architectural direction.

It MUST NOT replace the exact per-package allow-list.

When asking:

```text
May package X import package Y right now?
```

consult:

```text
architecture-manifest.ts
```

and the architecture checker.

---

# 15. Generated package evidence

Readable source-derived package architecture:

```text
docs/generated/package-boundaries.md
```

is generated from the manifest.

The current generated document reports the current package universe and exact allowed imports.

Its package count is current evidence, not an architecture constant.

---

# 16. Architecture layers

Current executable layer categories are:

```text
foundation
runtime
ui
product-core
product-state
product-collaboration
product-plugin
product-adapter
product-testing
feature
app
```

These categories describe client dependency/ownership roles.

They are not business bounded contexts.

---

# 17. FE-ARCH-006 — Architecture layer and product context are different dimensions

Example:

```text
Work Management
→ product context/capability

product-core / product-state / product-adapter
→ client architecture roles within that capability
```

Do not call every architecture package a bounded context.

Do not make every bounded context require the same package topology.

---

# 18. Freeze scopes

Current executable freeze scopes are:

```text
core-production
verification
marketing-isolated
```

Freeze scope means architecture coverage/stability.

It does not mean every feature is complete.

---

# 19. FE-ARCH-007 — Freeze means stable foundation, not finished product

A frozen package boundary MAY continue receiving:

```text
new feature behavior
new views
new endpoint consumption
new tests
```

provided the accepted architecture remains intact.

A feature gap alone is not evidence that the architecture is unfrozen.

---

# 20. Foundation family

Foundation contains reusable client mechanisms/contracts that should remain broadly product-agnostic.

Current examples include concepts such as:

```text
contracts
kernel
platform
query
realtime
observability
```

Exact membership remains executable evidence.

---

# 21. FE-ARCH-008 — Foundation is not a reuse dumping ground

Code does not belong in foundation merely because:

```text
two packages use it
it looks generic
moving it reduces duplicate lines
```

Foundation is for stable cross-product client mechanisms/contracts.

Product-specific semantics remain with product/feature owners.

---

# 22. Kernel

Kernel is the narrowest stable client primitive layer.

It should contain concepts that do not need knowledge of:

```text
Work Management
Documents
Billing
Governance
host framework
```

Broad import count alone does not make a type kernel-worthy.

---

# 23. FE-ARCH-009 — Kernel remains semantically small

If a proposed kernel type contains product names, host behavior, query semantics, or feature policy, reassess ownership.

Kernel expansion has high fan-out cost.

---

# 24. Contracts foundation

Contracts provide generated/normalized backend-facing client contract types and related stable contract utilities.

Wire shapes are not automatically the best internal semantic model.

Mapping is allowed where a product/state owner needs a different representation.

---

# 25. FE-ARCH-010 — Generated contract output is evidence, not hand-authored truth

If generated REST/realtime output is wrong:

```text
fix producer/input/generator
regenerate
```

Do not patch generated output and leave the producer inconsistent.

---

# 26. Platform foundation

Platform defines generic client capabilities and narrow contracts.

Examples can include abstract capabilities for:

```text
storage
navigation integration
environment/runtime services
```

as current source defines.

It is not a global untyped service locator.

---

# 27. FE-ARCH-011 — Platform abstraction is narrow and typed

Do not create:

```text
getService(name)
global dependency bag
magic feature registry
```

as the default mechanism for cross-package dependencies.

Prefer explicit imports/contracts/composition.

---

# 28. Query foundation

Query foundation owns generic server-state/query mechanics.

It does not own every resource's keys or mutation semantics.

Product/feature state owners define resource-specific behavior.

---

# 29. FE-ARCH-012 — Generic query mechanism and product query policy are separate

Example:

```text
generic QueryClient helpers
→ query foundation

Work Management item query keys/invalidation
→ Work Management state
```

Do not centralize every key in one global foundation registry.

---

# 30. Realtime foundation

Realtime foundation owns generic realtime mechanism/contract handling.

Product-specific event reconciliation belongs with the affected product/feature state owner.

---

# 31. FE-ARCH-013 — Realtime transport does not own product semantics

Generic realtime code SHOULD NOT know how:

```text
Board Item
Document Block
Billing entitlement
```

mutates client product state.

Delegate to product/feature reconciliation.

---

# 32. Observability foundation

Observability provides reusable client instrumentation contracts/mechanisms.

Vendor-specific bindings should remain replaceable where architecture requires.

Product packages should not acquire direct vendor dependency without an owner/reason.

---

# 33. Runtime family

Runtime packages bind host-specific capabilities to reusable inward contracts.

Current runtime split includes:

```text
runtime-web
runtime-mobile
```

according to the executable manifest.

---

# 34. FE-ARCH-014 — Runtime adapts platform; runtime does not own product behavior

Runtime MAY know:

```text
browser APIs
native APIs
host lifecycle
platform storage
platform networking
```

It SHOULD NOT own:

```text
Board rules
Document rules
Billing semantics
```

---

# 35. Web runtime

Web runtime may depend on browser-compatible mechanisms.

It can implement abstractions for:

```text
web storage
browser lifecycle
web realtime/auth integration
web observability
```

as current architecture defines.

Do not make mobile consume it.

---

# 36. Mobile runtime

Mobile runtime adapts native/Expo/React Native mechanisms.

It must remain independent from web-only runtime implementation.

---

# 37. FE-ARCH-015 — Runtime direction is host inward

Conceptually:

```text
app-web
→ runtime-web
→ foundation contracts/mechanisms

app-mobile
→ runtime-mobile
→ foundation contracts/mechanisms
```

Foundation does not import runtime packages.

---

# 38. UI family

UI is split into shared semantic visual foundations and platform implementations.

Current architecture includes concepts such as:

```text
ui-tokens
ui-web
ui-mobile
ui-icons
```

---

# 39. FE-ARCH-016 — Share design semantics, not incompatible rendering machinery

Web and mobile SHOULD share:

```text
semantic tokens
visual language
interaction intent
```

where appropriate.

They MUST NOT be forced to share DOM/native component implementation.

---

# 40. UI tokens

Tokens can own stable semantic values for:

```text
color
spacing
typography
radius
motion
density
```

according to the design-system architecture.

Tokens should not depend on product state.

---

# 41. FE-ARCH-017 — Tokens are semantic design inputs

A token is not a dumping ground for arbitrary CSS values.

Prefer semantic naming tied to system meaning.

Do not encode product business state into generic tokens.

---

# 42. Web UI

`ui-web` owns reusable web UI primitives/implementations.

It may use web-specific rendering technology.

It is not a production mobile dependency.

---

# 43. Mobile UI

`ui-mobile` owns native reusable UI primitives/implementations.

It uses shared tokens where appropriate.

It should not wrap/import web DOM components to simulate reuse.

---

# 44. Icons

The icon package is a narrow visual asset boundary.

It should remain low-dependency and product-agnostic.

---

# 45. Product package families

Dedicated product package families exist where a product capability needs reusable client architecture beyond one feature slice.

Current dedicated families include capabilities such as:

```text
Work Management
Documents
Automation
```

Exact package membership remains generated.

---

# 46. FE-ARCH-018 — Product family shape follows capability need, not symmetry

A product capability MAY have:

```text
core
state
collaboration
plugin
web adapter
mobile adapter
testing
```

only when each boundary has a real responsibility.

Do not create missing layers solely because another capability has them.

---

# 47. Product core

Product core owns reusable client-side product semantics that are safe outside a specific rendering/runtime host.

It can include:

```text
client semantic models
pure calculations
view/model transformation
stable capability contracts
```

within the limits of backend/product authority.

---

# 48. FE-ARCH-019 — Product core does not become backend Domain duplicate

Do not rebuild server aggregate/invariant logic in TypeScript as an independent source of truth.

Client product core may model/present behavior needed for interaction.

Server business acceptance remains authoritative.

---

# 49. Product state

Product-state packages own reusable server-state integration for a capability.

Typical concerns:

```text
query keys
query functions
mutation orchestration
cache updates
optimistic interaction
realtime reconciliation
```

They do not own host routing or platform rendering.

---

# 50. FE-ARCH-020 — Product state is derived from authoritative server state

Product state packages MAY cache and optimistically project.

They MUST reconcile to backend outcomes.

They MUST NOT become an independent durable business database by accident.

---

# 51. Product collaboration

Collaboration packages own capability-specific collaborative behavior where needed.

They can consume generic realtime primitives.

They do not own the generic connection/runtime.

---

# 52. FE-ARCH-021 — Collaboration semantics and realtime mechanism remain separate

Example:

```text
Document collaborative presence/block reconciliation
→ Documents collaboration owner

socket/reconnect/subscription primitive
→ realtime/runtime owner
```

---

# 53. Product plugin

A plugin package can own extensibility mechanisms specific to a product capability.

It should not become a repository-wide dynamic module system without an architecture decision.

---

# 54. FE-ARCH-022 — Product plugin scope is bounded

Plugin contracts should name:

```text
what can be extended
what the plugin may depend on
what authority remains in core/state
```

Do not use plugins to bypass dependency rules.

---

# 55. Product adapters

Web/mobile product adapters bind reusable product behavior to platform-specific UI/runtime behavior.

They can depend on the corresponding UI platform.

---

# 56. FE-ARCH-023 — Adapter direction is product semantics toward platform presentation

Conceptually:

```text
product core/state
        ↓
web adapter → ui-web

product core/state
        ↓
mobile adapter → ui-mobile
```

Do not make product core import web/mobile adapter.

---

# 57. Product testing

Testing packages support verification.

They may depend on production product packages according to the manifest.

Production packages MUST NOT depend on testing packages.

---

# 58. FE-ARCH-024 — Verification code is not production dependency

Test fixture/builder/helper convenience does not justify importing `product-testing` into a production app/package.

---

# 59. Feature packages

Feature packages own cross-product vertical client capabilities that do not need a dedicated multi-package product family.

Current examples include areas such as:

```text
auth
workspace
account
billing
integrations
notifications
activity
governance
search
collaboration
```

Exact current inventory remains generated.

---

# 60. FE-ARCH-025 — Feature package remains least privilege

Feature packages SHOULD consume only the internal packages required for their client responsibility.

Do not make a feature depend on every product/state package merely because a screen aggregates them.

Composition can remain in the app/host.

---

# 61. Feature versus product family

Use a dedicated product family when there is meaningful reusable capability architecture such as:

```text
shared core model
complex server-state owner
web/mobile adapters
collaboration/plugin boundary
```

Use a feature package when the client behavior is a narrower vertical slice.

---

# 62. FE-ARCH-026 — Package type follows ownership pressure, not folder size

A large folder does not automatically deserve a package.

A small but strongly isolated runtime/platform boundary can.

Choose based on:

```text
semantic ownership
dependency control
reuse
runtime separation
testing
team parallelism
```

---

# 63. High-level dependency direction

Conceptually, the architecture should trend inward:

```text
apps
        ↓
product adapters / feature composition
        ↓
product state / collaboration / plugin
        ↓
product core
        ↓
foundation contracts/mechanisms
```

Alongside:

```text
apps
→ runtime-web/runtime-mobile
→ foundation

web adapters
→ ui-web
→ ui-tokens

mobile adapters
→ ui-mobile
→ ui-tokens
```

This is conceptual guidance.

Exact package edges remain manifest-owned.

---

# 64. FE-ARCH-027 — Lower semantic layers do not depend on host composition

Foundation/product core/state MUST NOT import:

```text
apps/web
apps/mobile
apps/marketing
```

Apps are outer composition roots.

---

# 65. Public exports

Cross-package dependencies use approved public exports.

Package internal layout is not a public contract automatically.

Deep imports couple consumers to private structure.

---

# 66. FE-ARCH-028 — Cross-package deep imports are forbidden by default

Do not import:

```text
@notrelix/foo/src/internal/...
```

from another package.

If a consumer genuinely needs the symbol:

```text
make an intentional public export
move the behavior
or change the owner
```

according to dependency architecture.

---

# 67. Package export boundary

Public package exports are compatibility boundaries within the monorepo.

Removing/changing an export can affect many packages even if TypeScript can be fixed mechanically.

Review semantic consumer expectations.

---

# 68. FE-ARCH-029 — Public export expansion is deliberate

Do not export an internal class/function solely to satisfy one test or cross-package shortcut.

Expose stable package responsibility, not incidental implementation.

---

# 69. Workspace discovery

Current pnpm workspace globs include:

```text
apps/*
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/dev/*
packages/features/*
tooling/*
tooling/storybook/web
```

Workspace discovery tells pnpm what participates.

It does not define architecture permission.

---

# 70. FE-ARCH-030 — Workspace membership does not grant import permission

Two packages can be in the same pnpm workspace and still be forbidden from importing each other.

Use the architecture manifest.

---

# 71. Tooling

Tooling packages own:

```text
architecture checks
package/generator tooling
shared TS/ESLint config
testing setup
Storybook infrastructure
code generation
```

according to current source.

Tooling is an engineering mechanism plane.

---

# 72. FE-ARCH-031 — Tooling does not become product runtime dependency by default

Production product packages SHOULD NOT depend on generator/checker/build-tool internals.

If runtime behavior is needed, move a stable runtime contract/implementation to the proper production owner.

---

# 73. Build/task orchestration

Turborepo orchestrates workspace tasks.

Current root scripts expose tasks such as:

```text
dev
build
typecheck
lint
test
architecture checks
codegen checks
validation
```

Exact scripts belong to `package.json`/`turbo.json`.

Architecture docs do not freeze exact command chains.

---

# 74. FE-ARCH-032 — Build tooling is not runtime architecture

A package being built by Turbo does not make Turbo part of its product/runtime API.

Keep build orchestration concerns out of production semantics.

---

# 75. Contract generation

Frontend generated contracts consume backend/system contract artifacts.

Current codegen inputs include backend OpenAPI/realtime contract artifacts according to executable task configuration.

Generated outputs feed client contract packages.

---

# 76. FE-ARCH-033 — Contract direction is producer to client

Conceptually:

```text
backend/system contract producer
        ↓
generator
        ↓
frontend generated contract
        ↓
client adapters/state
```

Do not reverse this by editing frontend generated DTOs and expecting backend to conform.

---

# 77. Server state

Server state includes backend-owned resources such as:

```text
Workspace
Board/Item
Document/Page
Automation
Billing/entitlement
Governance permission state
```

Frontend holds representations/caches.

The backend owns the durable truth.

---

# 78. FE-ARCH-034 — Client cache is derived state

A query cache MAY:

```text
avoid refetch
show optimistic projection
support fast navigation
```

It MUST NOT be treated as proof of:

```text
current authorization
current tenant ownership
committed durable mutation
```

when backend contract owns those facts.

---

# 79. Local state

Local client state is valid for:

```text
open/closed UI
selection
draft input
local navigation
temporary composition
host runtime state
```

when it is not server truth.

Do not send every UI state into global/shared server-state stores.

---

# 80. FE-ARCH-035 — State ownership follows state class

Before creating shared state classify:

```text
server
URL/navigation
local UI
form draft
runtime/session
persisted preference
```

Choose owner/storage from the class.

---

# 81. Realtime

Realtime can update/invalidate/refetch server state.

It must tolerate transport realities:

```text
duplicate
out-of-order
reconnect
gap
stale permission
workspace transition
```

according to the realtime architecture.

---

# 82. FE-ARCH-036 — Realtime is reconciliation, not independent truth

If realtime state conflicts with an authoritative refetch/version contract, resolve according to server-state semantics.

Do not maintain an indefinitely divergent parallel realtime model.

---

# 83. Authentication/session

Frontend owns client session lifecycle and credential transport integration.

Backend owns credential validation/security authorization.

Client route/visibility checks improve UX.

They are not the security boundary.

---

# 84. FE-ARCH-037 — Frontend authorization UX is non-authoritative

The client MAY:

```text
hide
disable
redirect
explain
```

based on permission data.

The server MUST still authorize protected data/effects.

Do not encode “hidden button means secure.”

---

# 85. Tenant/resource scope

Frontend sends resource/tenant IDs as request/navigation inputs.

The server resolves authoritative ownership/permission.

---

# 86. FE-ARCH-038 — Client scope identifiers are inputs, not authority

Knowing:

```text
workspaceId
boardId
pageId
```

does not grant access.

Do not build client architecture that assumes resource ownership from route shape alone.

---

# 87. Web/mobile semantic parity

Where both hosts implement the same capability, they should preserve the same product contract.

UI/navigation interaction can differ by platform.

---

# 88. FE-ARCH-039 — Product parity does not require component parity

Web and mobile can use different:

```text
component tree
navigation
gesture
layout
platform integration
```

while preserving:

```text
resource semantics
mutation meaning
permission outcome
server-state contract
```

---

# 89. Mobile purity

The mobile production dependency graph must remain free of inappropriate web-only mechanisms.

Examples of red flags:

```text
react-dom
DOM APIs
ui-web
runtime-web
Next.js
web app internals
```

unless the dependency is deliberately isolated outside the native production graph.

---

# 90. FE-ARCH-040 — Native-safe graph is architectural

Do not solve mobile reuse by importing web implementation.

Share:

```text
tokens
product core
product state where native-safe
generic contracts
```

then implement native adapters/UI.

---

# 91. Marketing isolation

Marketing uses the shared visual foundation needed for consistent brand/design.

It should not consume authenticated application product state simply because the code exists.

---

# 92. FE-ARCH-041 — Marketing remains isolated from product runtime

If marketing needs a visual element also used by the application:

```text
promote/reuse approved UI primitive/token
```

rather than importing authenticated product-state packages.

---

# 93. Product/business contexts

The frontend supports product capabilities defined by repository product architecture.

Do not make frontend package layout the new source for bounded-context ownership.

A context can be represented by:

```text
dedicated product family
feature package
host composition
combination
```

depending on client needs.

---

# 94. FE-ARCH-042 — Client packaging does not redefine bounded contexts

Example:

```text
Governance product context
```

may currently appear as a feature package because the frontend does not need a dedicated multi-package Governance family.

That does not make Governance less of a product bounded context.

---

# 95. Cross-context composition

A page/screen may compose data/features from multiple product contexts.

Cross-context UI composition belongs at an outer composition layer.

Do not merge semantic ownership into one “page package” merely because the screen displays them together.

---

# 96. FE-ARCH-043 — UI composition does not transfer write ownership

Example:

```text
Workspace settings screen
```

can show:

```text
Governance
Billing
Integrations
Workspace
```

but each mutation still routes through the owning feature/product/server contract.

---

# 97. Change locality

A well-owned feature change should touch the narrowest relevant package set.

If a routine feature requires edits across:

```text
kernel
runtime
UI primitives
many unrelated features
all hosts
```

inspect whether the boundary is wrong.

---

# 98. FE-ARCH-044 — Routine feature work should not require architecture-wide edits

High fan-out is a signal to inspect:

```text
missing abstraction
wrong owner
over-global state
bad public export
foundation leakage
```

not a reason to normalize broad edits.

---

# 99. Dependency pressure

When package A repeatedly needs package B but the manifest forbids it, do not immediately allow the edge.

Ask:

```text
Is A the correct owner?
Should behavior move?
Should a narrower contract be extracted?
Is the current architecture actually changing?
```

---

# 100. FE-ARCH-045 — Dependency allow-list change is architecture evidence

Changing `allowedInternalImports` is not a normal “fix import error” operation.

The edge should be justified by architecture.

Consequential changes follow architecture-change policy.

---

# 101. Package creation pressure

Create a package when it creates a useful stable boundary for:

```text
semantic ownership
runtime isolation
independent testing
public contract
team parallelism
```

Do not create packages to make folder trees symmetrical.

---

# 102. FE-ARCH-046 — Package count is not an architecture quality metric

More packages can increase:

```text
build/config overhead
public API surface
dependency coordination
mental load
```

Fewer packages can increase coupling.

Choose based on boundary value.

---

# 103. Package removal/merge

Removing/merging package is an architecture change if it changes ownership/dependency direction.

A package with little code can still encode an important boundary.

Do not measure value only by LOC.

---

# 104. FE-ARCH-047 — Structural simplification preserves semantic separation

When merging packages, ensure the target does not gain conflicting:

```text
host/runtime
product
test
UI
```

responsibilities.

Simplify structure without collapsing authorities.

---

# 105. Architecture source versus docs

The manifest/source may reveal current drift.

Source is evidence.

It is not automatic precedent over accepted architecture/ADRs.

---

# 106. FE-ARCH-048 — Architecture drift is classified before repair

Use:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

according to documentation governance.

Do not silently edit the nearest artifact.

---

# 107. Architecture change

Changes to durable foundations can require:

```text
architecture-change-policy
FE ADR
manifest update
source migration
generated package docs
tests/gates
compatibility plan
```

Routine implementation following existing boundaries does not require an ADR.

---

# 108. FE-ARCH-049 — Manifest change and architecture doc change are atomic when semantics change

If the intended dependency architecture changes:

```text
canonical architecture
+
manifest
+
source
+
tests
+
generated evidence
```

should move together.

Do not land a new edge first and document it later.

---

# 109. Testing relationship

Architecture boundaries must be executable where reliable.

Frontend currently has tooling for:

```text
architecture graph
deep imports
mobile boundary
generated docs
codegen
test taxonomy
```

plus behavior/UI/E2E suites.

The testing architecture document owns the exact proof model.

---

# 110. FE-ARCH-050 — Machine-detectable foundation rules should be gated

Examples:

```text
package not in manifest
forbidden internal import
deep import
web dependency in mobile graph
generated boundary drift
```

SHOULD fail executable checks when detection is reliable.

Do not rely only on reviewer memory.

---

# 111. Architecture evidence hierarchy

For a current package question:

```text
semantic meaning
→ authored architecture

historical rationale
→ FE ADR

exact allowed edge
→ architecture manifest

human-readable exact edge
→ generated package-boundaries

actual dependency/import
→ package.json/source

proof
→ architecture tests/CI
```

Use the correct evidence class.

---

# 112. Architecture versus package manifest

A `package.json` dependency means the package currently declares a dependency.

It does not by itself prove the dependency is architecturally allowed.

The architecture checker/manifest provides that additional constraint.

---

# 113. FE-ARCH-051 — Existing dependency is not automatic precedent

If a current package dependency violates canonical architecture:

```text
classify source debt
```

rather than copying it into another package.

---

# 114. Host package manifests

Current host manifests demonstrate the intentional framework split:

```text
app-web
→ Vite/TanStack/React DOM/web product dependencies

app-mobile
→ Expo/React Native/mobile adapters

app-marketing
→ Next/React DOM/shared web visual primitives
```

These are current source evidence.

---

# 115. FE-ARCH-052 — Host dependency breadth is not reusable-package permission

The web app can depend on many packages because it is a composition root.

That does not mean a feature/product package may import the same breadth.

Outer composition is intentionally broader.

---

# 116. Marketing dependency breadth

Marketing's approved internal dependencies are intentionally narrower than web application composition.

Do not use Next/marketing dependencies as precedent for reusable package imports.

---

# 117. FE-ARCH-053 — Host isolation is enforced by dependency direction

Marketing/mobile/web should share through approved inward packages.

They SHOULD NOT import each other's app internals.

---

# 118. Runtime-neutral packages

A runtime-neutral package should avoid assumptions about:

```text
DOM
native view tree
host router
specific environment variable system
framework server component model
```

unless explicitly scoped.

---

# 119. FE-ARCH-054 — Neutrality is behavioral, not naming

A package called `core` is not automatically runtime-neutral.

Its imports/source must preserve the contract.

Architecture checks should detect reliable violations.

---

# 120. React usage

Some reusable frontend packages can legitimately use React.

The important boundary is not “React nowhere outside apps.”

The important boundary is whether the package's architecture permits UI/framework coupling.

Keep pure core/state layers appropriately narrow.

---

# 121. FE-ARCH-055 — Framework dependency follows package responsibility

Do not add React/UI framework to a pure semantic/state package solely for helper convenience.

Move rendering/hooks to the appropriate UI/adapter/feature owner when required.

---

# 122. State library choice

Libraries such as TanStack Query can support the state architecture.

The library is mechanism.

The architecture is:

```text
server state remains server-authoritative
keys are scoped
mutations reconcile
ownership is explicit
```

Do not confuse one library API with the whole state model.

---

# 123. FE-ARCH-056 — State architecture survives library replacement conceptually

Product state should expose behavior/ownership clear enough that replacing the underlying query mechanism does not require rewriting product semantics.

This does not require speculative abstraction around every library call.

---

# 124. Router choice

Router is host navigation mechanism.

Product core should not need router APIs to model product semantics.

Feature UI can interact with host navigation through approved boundaries.

---

# 125. FE-ARCH-057 — URL/navigation is host/client state, not product aggregate state

A route identifies navigation context.

It does not create a backend resource or tenant relationship.

Keep navigation semantics distinct.

---

# 126. Observability

Client observability can annotate:

```text
route
request
render
realtime
error
release
```

without becoming a vendor-coupled product dependency.

Use privacy/security standards for payloads.

---

# 127. FE-ARCH-058 — Observability failure does not change product correctness

Telemetry MUST NOT be required to authorize/commit a product mutation.

It is diagnostic/supporting unless a separate product analytics contract explicitly says otherwise.

---

# 128. Accessibility

Accessibility is a cross-cutting product quality property.

UI architecture should make correct:

```text
semantic role
keyboard/focus
labels
contrast
motion
touch target
```

possible at primitives and product components.

The detailed standard lives in repository quality docs/UI architecture.

---

# 129. FE-ARCH-059 — Accessibility belongs in architecture boundaries, not late visual polish

Reusable UI primitives SHOULD encode accessible defaults where feasible.

Feature/product UI remains responsible for semantic use.

---

# 130. Responsive and native adaptation

Web responsive layout and native mobile UI can diverge in presentation.

Shared product semantics should not.

---

# 131. FE-ARCH-060 — Platform adaptation may differ while operation meaning remains stable

If both hosts invoke:

```text
move Board Item
submit Form
edit Page
```

the interaction patterns can differ.

The server mutation meaning remains the same contract.

---

# 132. Security boundary

Frontend is untrusted from the server's perspective.

Any client state/input can be manipulated.

Backend authentication/authorization validates protected operations.

---

# 133. FE-ARCH-061 — Client-delivered code contains no server secret

Do not place:

```text
database credential
provider secret
JWT signing key
backend API secret
```

into browser/mobile/marketing public bundles or public env variables.

If the client needs an operation requiring a secret, route it through a trusted server boundary.

---

# 134. Public/share capability

A public/share client experience can have a deliberately smaller package/permission graph than authenticated app.

Do not load full authenticated product authority by default for a bounded public capability.

---

# 135. FE-ARCH-062 — Capability-scoped public UI remains bounded

Share/public token for resource A does not imply client access to:

```text
Workspace administration
Billing
Integrations
sibling resources
```

Backend enforces this; frontend architecture should not over-fetch/over-compose it.

---

# 136. Performance boundary

Client architecture affects:

```text
bundle size
render cost
query fan-out
realtime churn
list virtualization
host startup
```

Performance work should preserve owners rather than merge everything into apps.

---

# 137. FE-ARCH-063 — Performance optimization does not bypass architecture silently

If a measured performance issue requires a new dependency/caching/host boundary:

```text
measure
justify
change architecture deliberately if needed
```

Do not deep-import/internal-copy as an invisible optimization.

---

# 138. Team parallelism

Packages can help teams work independently when ownership is clear.

They can hurt parallelism when every feature requires central package/manifest edits.

Review dependency pressure as the product grows.

---

# 139. FE-ARCH-064 — Shared foundation changes have higher coordination cost

A foundation/kernel/public-export change SHOULD have stronger justification because it can affect many packages/hosts.

Prefer product-local changes for product-local behavior.

---

# 140. Extraction/evolution

Frontend architecture should allow host/runtime/product packages to evolve without massive rewrite.

Possible future evolution may include:

```text
new product family
new native capability
runtime implementation replacement
host routing change
package consolidation
```

No future topology is pre-approved merely because it is possible.

---

# 141. FE-ARCH-065 — Future flexibility does not justify speculative abstraction

Do not add:

```text
generic microfrontend platform
universal plugin framework
dynamic module loader
cross-host service locator
```

without a current concrete requirement and architecture decision.

---

# 142. Microfrontend posture

Notrelix is a modular monorepo client platform.

It is not automatically a microfrontend architecture.

Package modularity does not require independent browser deployment per package.

---

# 143. FE-ARCH-066 — Package boundary and deployment boundary are different

A package can have strong semantic ownership while shipping in one web application bundle.

Do not introduce deployment fragmentation merely to mirror package structure.

---

# 144. Product extraction

A product family may become larger/more independent over time.

Before creating a new package family or split, inspect:

```text
semantic owner
shared state
host adapters
contract dependencies
team ownership
build/runtime cost
```

Use architecture-change policy.

---

# 145. FE-ARCH-067 — Product extraction preserves source-of-truth boundaries

Splitting client packages MUST NOT duplicate:

```text
server state authority
query cache authority
mutation owner
realtime reconciliation
```

across two competing packages.

---

# 146. Feature-to-product promotion

A feature package can be promoted into a dedicated product family if complexity/ownership warrants it.

Migration should declare:

```text
new owner
public exports
state owner
host adapters
old feature consumers
removal
```

---

# 147. FE-ARCH-068 — Promotion is ownership migration, not copy-and-leave

Do not copy feature code into a new product package and keep both active indefinitely.

Define transition and remove old authority.

---

# 148. Product-to-feature simplification

A product family can be simplified if dedicated boundaries no longer provide value.

Preserve semantic ownership and runtime safety.

---

# 149. FE-ARCH-069 — Simplification does not erase capability semantics

Collapsing package count does not mean the product context/contract disappears.

Keep the semantic owner clear.

---

# 150. Architecture change impact

Changes to these areas are architecture-significant:

```text
new app/host
new architecture layer
new dedicated product family
dependency direction
runtime split
UI platform split
state authority
contract-generation boundary
auth/session foundation
package export foundation
```

Use `architecture-change-policy.md`.

---

# 151. FE-ARCH-070 — Architecture significance follows durable coupling

A change is architecture-significant when it alters:

```text
who owns behavior
who may depend on whom
where runtime/platform knowledge lives
what state is authoritative
what contracts are stable
```

not merely because many lines changed.

---

# 152. Architecture tests

The exact architecture checker should enforce reliable structural rules.

Examples:

```text
closed-world manifest
allowed internal import
deep import
mobile purity
generated package-boundary drift
```

The detailed gate ownership belongs to testing/dependency docs.

---

# 153. FE-ARCH-071 — Executable gate and authored rationale stay aligned

When a canonical dependency rule changes intentionally:

```text
authored architecture
manifest/checker
generated evidence
tests
```

must agree.

Do not leave a checker enforcing a retired rule or docs describing an unguarded critical boundary.

---

# 154. Current source evidence

Current source shows:

```text
Vite web host
Expo mobile host
Next marketing host
pnpm workspace families
closed-world architecture manifest
generated package-boundary document
frontend ADR registry
```

These facts demonstrate the present architecture implementation.

Exact versions/package counts are current evidence, not permanent architecture.

---

# 155. FE-ARCH-072 — Architecture docs avoid volatile inventory duplication

This document should not list:

```text
every package
every exact dependency
every library version
every current command
```

unless the value itself is architecture.

Use source/generated evidence for volatile facts.

---

# 156. Architecture review checklist

Before approving a broad frontend architecture change:

```text
[ ] product/client owner
[ ] backend contract authority
[ ] host impact
[ ] architecture layer
[ ] package owner
[ ] exact manifest edge
[ ] public export
[ ] web/mobile/marketing safety
[ ] server-state authority
[ ] realtime impact
[ ] UI/accessibility impact
[ ] migration/compatibility
[ ] tests/gates
[ ] FE ADR if consequential
[ ] generated boundary update
```

---

# 157. New package checklist

```text
[ ] real ownership reason
[ ] workspace discovery
[ ] package name/path
[ ] architecture layer
[ ] freeze scope
[ ] least-privilege imports
[ ] public exports
[ ] runtime/platform safety
[ ] tests
[ ] manifest entry
[ ] generated docs
```

---

# 158. New product family checklist

```text
[ ] capability is product-significant
[ ] feature package no longer sufficient
[ ] core responsibility
[ ] state responsibility
[ ] collaboration/plugin need
[ ] web adapter need
[ ] mobile adapter need
[ ] testing support need
[ ] backend contract owner
[ ] migration from old owner
```

Create only needed packages.

---

# 159. New host checklist

```text
[ ] product reason
[ ] framework/runtime decision
[ ] runtime adapter
[ ] UI platform
[ ] contract/codegen reuse
[ ] state/query
[ ] realtime
[ ] auth/session
[ ] navigation
[ ] testing/build/deployment
[ ] architecture manifest
[ ] ADR
```

A new host is a consequential architecture decision.

---

# 160. Dependency-change checklist

```text
[ ] consumer is correct owner
[ ] provider is correct owner
[ ] public export exists
[ ] edge does not invert layer
[ ] mobile/runtime safety
[ ] no deep import
[ ] manifest change justified
[ ] generated docs updated
[ ] architecture gate passes
```

---

# 161. Stop conditions

Stop implementation/architecture work if:

- a backend/product semantic decision is missing;
- the proposed solution makes frontend authoritative for server business truth;
- the easiest solution is a deep import;
- a feature wants broad allow-list expansion without owner analysis;
- foundation is gaining product-specific behavior;
- product core is gaining host/runtime APIs;
- mobile is gaining web/DOM dependencies;
- marketing is importing authenticated product state;
- generated contracts/package docs are being edited by hand;
- a routine feature requires architecture-wide edits and the reason is unclear;
- source conflicts with an Accepted FE ADR and no superseding decision exists;
- a new package/layer exists only for symmetry;
- a universal abstraction is being introduced for hypothetical future flexibility.

---

# 162. Related canonical owners

Frontend:

```text
dependency-boundaries.md
hosts-composition-routing.md
api-and-contracts.md
state-query-mutations.md
realtime.md
ui-and-design-system.md
testing-and-quality-gates.md
architecture-change-policy.md
```

Repository:

```text
../../../docs/architecture/system-overview.md
../../../docs/architecture/contract-boundaries.md
../../../docs/architecture/data-ownership-and-consistency.md
../../../docs/product/product-model.md
../../../docs/quality/engineering-quality-standard.md
```

---

# 163. Related decisions

Frontend decision history:

```text
../decisions/README.md
```

Current registry contains historical decisions for areas including:

```text
framework split
package manager
package exports
Next.js package boundary
auth session model
```

Check ADR status before using historical rationale.

---

# 164. Explicit non-responsibilities

This overview does not define:

```text
exact current package allow-list
exact route tree
exact API operation list
exact query keys
exact realtime event map
component-level visual specification
exact CI job names
product permission/lifecycle semantics
```

Use the narrower owner/source/generated evidence.

---

# 165. Final architecture model

The frontend should remain understandable as:

```text
backend + product authority
        ↓
generated/normalized client contracts
        ↓
foundation mechanisms
        ↓
product core / product state / feature owners
        ↓
product collaboration/plugins where needed
        ↓
web/mobile product adapters
        ↓
host runtime + UI composition
        ↓
accessible user experience
```

with a separate isolated marketing host sharing only approved visual foundations.

The exact package graph is executable.

The product/server authority remains external to the client.

The architecture succeeds when teams can add product capability without turning:

```text
apps
foundation
global state
or one shared feature package
```

into the new monolith.
