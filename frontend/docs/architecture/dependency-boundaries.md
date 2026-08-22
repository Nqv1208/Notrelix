---
document_id: FE-ARCH-DEPENDENCY-BOUNDARIES
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-package-graph
  - frontend-import-boundaries
  - frontend-public-exports
  - frontend-mobile-purity
  - frontend-generated-architecture-evidence
evidence:
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/tooling/dependency-rules/src/
  - frontend/docs/generated/package-boundaries.md
  - frontend/pnpm-workspace.yaml
  - frontend/package.json
  - frontend/apps/
  - frontend/packages/
review_on:
  - architecture-manifest-change
  - package-layer-change
  - package-public-export-change
  - mobile-purity-rule-change
  - package-generator-change
  - dependency-checker-change
---

# Frontend Dependency Boundaries

> **The frontend package graph is closed-world, explicit, least-privilege, and executable.**
>
> Authored architecture defines what the layers mean. `architecture-manifest.ts` defines the exact package universe and the exact currently allowed internal package imports. Generated package-boundary documentation is evidence derived from that manifest, not a second handwritten dependency matrix.

This document is the canonical owner for frontend package dependency architecture.

It defines:

- package registration and closed-world rules;
- exact dependency authority;
- high-level layer direction;
- public export and deep-import rules;
- foundation/runtime/UI/product/feature dependency expectations;
- web/mobile/marketing dependency separation;
- package creation/removal/move rules;
- dependency architecture change handling;
- generated package-boundary evidence;
- executable checks and stop conditions.

It does **not** define:

- product business semantics;
- exact REST contracts;
- query key semantics;
- realtime reconciliation details;
- route trees;
- UI visual design;
- exact per-package allowed-import lists.

Those topics have separate owners.

---

# 1. Dependency architecture objective

The dependency architecture exists to prevent the monorepo from degrading into:

```text
many packages
+
unrestricted imports
=
one hidden monolith
```

A package boundary has value only when it creates a meaningful constraint.

The frontend should remain evolvable as:

```text
stable low-level mechanisms
        ↑
product/feature behavior
        ↑
platform adapters
        ↑
host composition
```

without lower-level packages depending back outward.

---

# 2. FE-DEP-001 — Package boundaries are architectural constraints

A package is not merely a folder with its own `package.json`.

A governed production package SHOULD have:

```text
clear responsibility
bounded public surface
least-privilege internal dependencies
runtime/platform contract
test ownership
```

If a package can import anything, its boundary is mostly cosmetic.

---

# 3. Closed-world authority

The executable package architecture is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

The manifest is intended to describe the complete governed package/app universe.

Every governed source-bearing app/package under the covered workspace must be represented according to the checker contract.

---

# 4. FE-DEP-002 — The manifest is closed-world

For governed source packages/apps:

```text
workspace/source inventory
=
architecture manifest inventory
```

according to the checker.

A new package MUST NOT become production architecture merely because pnpm discovers it.

It must be intentionally registered.

---

# 5. Manifest entry contract

Each architecture policy entry currently defines:

```text
packageName
relativePath
layer
freezeScope
allowedInternalImports
```

These are executable dependency facts.

---

# 6. FE-DEP-003 — Exact allowed internal imports come from the manifest

When determining whether:

```text
package A
→ may import
package B
```

the exact current answer comes from:

```text
allowedInternalImports
```

for package A.

Do not infer permission from:

```text
same folder family
same team
same product
same pnpm workspace
similar package name
existing transitive dependency
```

---

# 7. Manifest self-validation

The manifest currently validates defects including:

```text
unknown allowed-import target
self-import policy
duplicate allowed import
duplicate package name
duplicate package path
```

These checks protect manifest integrity before source import analysis.

---

# 8. FE-DEP-004 — Invalid policy data fails before import scanning

Architecture tooling SHOULD fail on a malformed manifest before using it as authority.

A checker that silently accepts:

```text
unknown package names
duplicate paths
self edges
```

cannot reliably enforce source dependencies.

---

# 9. Workspace discovery is different

`pnpm-workspace.yaml` defines workspace discovery globs.

Current families include:

```text
apps/*
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/features/*
packages/dev/*
tooling/*
tooling/storybook/web
```

This is package-manager participation.

It is not dependency permission.

---

# 10. FE-DEP-005 — Workspace membership does not imply import permission

Two packages MAY be part of the same workspace while remaining forbidden from importing each other.

The distinction is:

```text
pnpm-workspace.yaml
→ can the package participate in the workspace?

architecture-manifest.ts
→ may this governed package depend on that internal package?
```

---

# 11. Generated dependency evidence

The readable package map is:

```text
frontend/docs/generated/package-boundaries.md
```

It is generated from the architecture manifest by dependency-rule tooling.

---

# 12. FE-DEP-006 — Generated dependency docs are not edited by hand

If generated package-boundary evidence is stale:

```text
change source manifest if intended
run producer
review generated diff
commit output
```

Do not patch the Markdown matrix directly.

---

# 13. Generated evidence purpose

Generated docs make executable policy reviewable.

They are useful for:

```text
code review
architecture audit
onboarding
drift review
```

without making the handwritten architecture file duplicate the current package inventory.

---

# 14. FE-DEP-007 — Volatile inventory remains generated

Do not copy into authored docs:

```text
complete current package list
complete exact allow-list
complete direct edge matrix
current package count
```

as manually synchronized architecture.

Those facts change frequently and should remain generated/executable.

---

# 15. Layer model

Current executable architecture layers include:

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

The layer is a dependency/ownership role.

It is not a bounded context.

---

# 16. FE-DEP-008 — Layer names encode responsibility, not prestige

A package should not be moved “down” into foundation/core to make it more reusable-looking.

Lower/foundation placement creates stronger reuse expectations and wider coupling.

Place code in the narrowest correct owner.

---

# 17. Conceptual dependency direction

High-level direction:

```text
app
  ↓
feature / product-adapter
  ↓
product-state / collaboration / plugin
  ↓
product-core
  ↓
foundation
```

Parallel platform direction:

```text
app-web
  ↓
runtime-web
  ↓
foundation

app-mobile
  ↓
runtime-mobile
  ↓
foundation
```

UI direction:

```text
ui-web
  ↓
ui-tokens

ui-mobile
  ↓
ui-tokens
```

Exact exceptions/edges remain manifest-owned.

---

# 18. FE-DEP-009 — Conceptual direction does not replace exact policy

The diagrams in this document explain intent.

They do not grant a dependency.

An edge must still appear in the executable manifest where required.

---

# 19. Foundation dependency posture

Foundation packages should remain low in the dependency graph.

Current foundation concepts include:

```text
kernel
contracts
platform
query
realtime
observability
```

according to the current manifest.

---

# 20. FE-DEP-010 — Foundation imports inward, not outward

Foundation MUST NOT depend on:

```text
features
product adapters
apps
marketing
web/mobile application internals
```

unless a deliberate architecture change redefines the layer.

---

# 21. Kernel dependency posture

Kernel should have minimal/no internal dependency breadth.

It is suitable for extremely stable client primitives.

---

# 22. FE-DEP-011 — Kernel may not absorb product/runtime concepts

Do not move into kernel:

```text
Board
Page
Automation
Billing
router
browser storage
native storage
query client
```

merely to make imports easier.

A high-fan-out package must stay semantically narrow.

---

# 23. Contracts dependency posture

Contracts can depend on narrow client primitives where approved.

Generated wire types should not acquire feature/UI/runtime dependencies.

---

# 24. FE-DEP-012 — Wire contracts do not depend on feature presentation

Generated/normalized contract packages MUST remain independent from:

```text
ui-web
ui-mobile
feature components
host routers
```

The direction is:

```text
client behavior
→ consumes contracts
```

not the reverse.

---

# 25. Platform dependency posture

Platform contains typed reusable capability contracts/mechanisms.

It may depend on lower foundation contracts according to manifest.

It should not import concrete host implementations.

---

# 26. FE-DEP-013 — Abstraction does not import its host implementation

If:

```text
platform
→ defines capability contract

runtime-web
→ implements it
```

then:

```text
platform
MUST NOT
→ import runtime-web
```

That would invert the dependency.

---

# 27. Query dependency posture

Generic query foundation may depend on lower primitives.

It does not depend on product-state packages.

---

# 28. FE-DEP-014 — Product query semantics depend on query foundation

Direction:

```text
product-state
→ query
```

not:

```text
query
→ all product states
```

This prevents a central generic package from becoming aware of every resource.

---

# 29. Realtime dependency posture

Generic realtime foundation owns transport/reusable mechanism.

Product-specific collaboration/state consumes it.

---

# 30. FE-DEP-015 — Realtime foundation cannot import product event consumers

Direction:

```text
product-state/collaboration
→ realtime
```

not:

```text
realtime
→ documents-state
→ work-management-state
```

Product reconciliation remains outside the generic mechanism.

---

# 31. Observability dependency posture

Observability should remain reusable and low-level.

Product/host code may instrument through approved contracts.

---

# 32. FE-DEP-016 — Telemetry vendor dependency does not propagate by convenience

Do not require every product package to import a vendor SDK because the host uses that vendor.

Keep vendor binding at the approved observability/runtime boundary.

---

# 33. Runtime dependency posture

Current runtime packages:

```text
runtime-web
runtime-mobile
```

adapt platform-specific mechanisms.

They consume foundation.

They should not depend on product behavior.

---

# 34. FE-DEP-017 — Runtime packages remain product-agnostic

Runtime MUST NOT become the home for:

```text
Work Management mutations
Document state
Billing policy
Workspace business state
```

Runtime owns platform mechanisms, not product meaning.

---

# 35. Web runtime isolation

`runtime-web` may use browser-compatible implementation details.

It is web-specific.

---

# 36. FE-DEP-018 — Web runtime is not a mobile dependency

Production mobile graph MUST NOT depend on:

```text
@notrelix/runtime-web
```

to reuse browser behavior.

Move shared contracts/mechanics inward if truly platform-neutral.

---

# 37. Mobile runtime isolation

`runtime-mobile` owns native/Expo/React Native runtime adaptation.

It should not import browser-only mechanisms.

---

# 38. FE-DEP-019 — Mobile runtime remains DOM-free

Mobile runtime MUST NOT depend on:

```text
window/document assumptions
react-dom
DOM storage implementation
web router
```

unless isolated outside the production native graph by an explicit architecture change.

---

# 39. UI token dependency posture

`ui-tokens` is a low-level design-semantic package.

It should have minimal/no internal dependencies.

---

# 40. FE-DEP-020 — UI tokens do not depend on rendering implementation

Direction:

```text
ui-web
→ ui-tokens

ui-mobile
→ ui-tokens
```

not the reverse.

Tokens should be consumable by multiple rendering platforms.

---

# 41. Web UI dependency posture

`ui-web` owns generic web UI primitives/implementations.

It may consume tokens.

It should not depend on product state.

---

# 42. FE-DEP-021 — Generic web UI does not import product packages

If a component knows:

```text
Board Item
Document Page
Billing plan
```

as product semantics, it likely belongs to a feature/product adapter, not generic `ui-web`.

---

# 43. Mobile UI dependency posture

`ui-mobile` owns generic native UI primitives/implementations.

It may consume tokens.

It does not consume web UI.

---

# 44. FE-DEP-022 — Web/mobile UI implementations remain separate

Forbidden production direction:

```text
ui-mobile
→ ui-web

mobile product adapter
→ ui-web
```

Share token/semantic contracts instead.

---

# 45. Icons dependency posture

Icons should remain low-dependency.

They should not import product/runtime state.

---

# 46. FE-DEP-023 — Visual asset packages remain semantically neutral

Do not put route/product action logic into icon exports.

---

# 47. Product-core dependency posture

A product-core package can depend on approved foundation contracts/primitives.

It should remain free of host/runtime/UI implementation.

---

# 48. FE-DEP-024 — Product core remains host-neutral

Product core MUST NOT import:

```text
ui-web
ui-mobile
runtime-web
runtime-mobile
apps/*
```

unless the package ceases to be product-core by deliberate architecture change.

---

# 49. Product-state dependency posture

Product-state may consume:

```text
product core
contracts
query
realtime/platform
```

according to exact manifest.

It should not import host apps/UI rendering by default.

---

# 50. FE-DEP-025 — State owner does not depend on screen composition

Direction:

```text
screen/adapter
→ product-state
```

not:

```text
product-state
→ app route
```

This keeps state reusable across host surfaces.

---

# 51. Product collaboration dependency posture

Collaboration packages may consume product core + realtime primitives.

They do not own host UI/runtime.

---

# 52. FE-DEP-026 — Collaboration does not bypass product state ownership accidentally

If a realtime collaboration event mutates server-derived state, the collaboration/state boundary must be deliberate.

Do not create a second competing cache solely inside a collaboration package.

---

# 53. Product plugin dependency posture

Plugins depend on their product capability according to explicit plugin contract.

They do not gain general repository dependency access.

---

# 54. FE-DEP-027 — Plugin boundary is not an architecture escape hatch

A plugin MAY extend a declared capability.

It MUST NOT be used to:

```text
import forbidden host internals
reach unrelated product packages
bypass public exports
```

---

# 55. Product adapter dependency posture

Adapters combine product behavior with platform UI/runtime concerns.

Current families can have web and mobile adapters.

---

# 56. FE-DEP-028 — Adapter is the preferred platform-coupling boundary for reusable product capability

Web-specific product presentation belongs in a web adapter.

Mobile-specific presentation belongs in a mobile adapter.

Do not push platform dependencies into product core merely to reduce adapter code.

---

# 57. Product testing dependency posture

Testing packages may consume production product packages.

Production packages do not consume testing packages.

---

# 58. FE-DEP-029 — Verification edges are one-way

Allowed conceptual direction:

```text
product-testing
→ product production packages
```

Forbidden:

```text
production
→ product-testing
```

Factories/fixtures needed at runtime belong in a production owner, not testing support.

---

# 59. Feature dependency posture

Feature packages currently use explicit least-privilege allow-lists.

The manifest intentionally does not grant every feature every UI/realtime/product dependency.

---

# 60. FE-DEP-030 — Feature dependencies are explicit, not inherited from a universal base architecture

Shared constants used by manifest tooling MAY reduce repetition internally.

But effective package permission remains exact.

A feature gets only what its responsibility requires.

---

# 61. Feature-to-feature dependencies

Feature packages should not casually depend on each other.

Cross-feature page composition generally belongs in the app/host.

If feature A repeatedly needs feature B's behavior, inspect semantic ownership.

---

# 62. FE-DEP-031 — Cross-feature dependency requires ownership analysis

Do not create:

```text
billing
→ workspace
→ governance
→ account
```

chains simply because screens are related.

Prefer stable lower product/foundation contracts or outer composition.

---

# 63. App dependency posture

Apps are outer composition roots and therefore have broader allowed dependency sets.

This breadth is intentional.

---

# 64. FE-DEP-032 — App breadth does not justify package breadth

Because `app-web` may import many packages, it does not follow that:

```text
feature-auth
```

may import every package the app imports.

Composition roots are allowed to be broader than reusable packages.

---

# 65. Web app dependencies

Current web app is allowed to compose:

```text
foundation
runtime-web
ui-web/tokens/icons
product web/state/core packages
feature packages
```

according to exact manifest.

It remains subject to public export rules.

---

# 66. FE-DEP-033 — Web app must not deep-import package internals

Even the composition root uses package public APIs.

Being the app does not grant:

```text
@notrelix/foo/src/internal/*
```

access.

---

# 67. Mobile app dependencies

Current mobile app has a narrow native-safe internal allow-list centered on:

```text
runtime-mobile
query
ui-mobile
ui-tokens
mobile product adapters
```

according to the manifest.

---

# 68. FE-DEP-034 — Mobile app does not mirror web app dependency breadth

The mobile graph SHOULD remain intentionally smaller/native-specific.

Do not add web features to mobile by importing their web package.

Create/reuse a native-safe product/state contract or mobile feature boundary.

---

# 69. Marketing dependencies

Current marketing app is `marketing-isolated`.

Its internal dependency allow-list is limited to shared visual packages:

```text
ui-tokens
ui-web
ui-icons
```

according to current manifest.

---

# 70. FE-DEP-035 — Marketing cannot import authenticated product runtime

Marketing MUST NOT depend on:

```text
product-state
features-auth
runtime-web product session
work-management state
documents state
```

merely to reuse UI/content.

Promote generic visual primitives to shared UI when appropriate.

---

# 71. Public exports

Packages define supported public imports through their package exports/entrypoints.

The public surface is part of the package contract.

---

# 72. FE-DEP-036 — Public entrypoints are the cross-package contract

Consumers SHOULD import from:

```ts
import { x } from "@notrelix/package";
```

or an explicitly supported subpath.

They MUST NOT bind to private file structure.

---

# 73. Deep imports

Example forbidden import:

```ts
import { x } from "@notrelix/package/src/internal/x";
```

It bypasses:

```text
public contract
package encapsulation
refactor safety
architecture scanning assumptions
```

---

# 74. FE-DEP-037 — Deep-import prohibition applies even when TypeScript resolves it

Compilation success is not architectural permission.

If the symbol should be public:

```text
export it intentionally
```

or move the consumer/behavior.

---

# 75. Public export expansion

Adding an export is a contract decision.

It can increase coupling and future compatibility obligations.

---

# 76. FE-DEP-038 — Export only stable package responsibility

Do not export:

```text
internal service instance
private cache object
test-only helper
implementation-specific class
```

solely because an external package wants convenient access.

---

# 77. Supported subpaths

A package MAY expose supported subpaths where beneficial.

They should be deliberate and stable.

Subpaths are still public contracts.

---

# 78. FE-DEP-039 — Supported subpath is not equivalent to arbitrary internal path

The contract must be declared by package exports and architecture.

Do not rely on filesystem reachability.

---

# 79. External dependencies

This architecture primarily governs internal package edges.

External dependencies still require ownership analysis.

---

# 80. FE-DEP-040 — External dependency belongs to the narrowest owner

Add a library where its runtime responsibility lives.

Do not install at root or foundation merely because multiple packages might use it later.

Consider:

```text
runtime compatibility
mobile safety
bundle impact
security/license
existing abstraction
```

---

# 81. Framework dependencies

React/Next/Vite/Expo/router/UI libraries are not neutral.

Their placement communicates coupling.

---

# 82. FE-DEP-041 — Host framework libraries stay at host/adapter/UI boundaries unless architecture explicitly permits otherwise

Examples of suspicious moves:

```text
Next.js
→ foundation

Expo
→ product core

react-dom
→ mobile state

TanStack Router
→ product core
```

---

# 83. Type-only imports

Type-only imports still represent conceptual coupling even when removed at runtime.

Architecture checker policy should define how they are treated.

Do not use `import type` solely to bypass a semantic boundary.

---

# 84. FE-DEP-042 — Type-only coupling can still violate ownership

If product core must know a host-owned type, the boundary may still be inverted.

Extract a neutral contract instead where appropriate.

---

# 85. Transitive dependencies

If:

```text
A → B
B → C
```

A does not automatically have permission to import C.

---

# 86. FE-DEP-043 — Internal dependency permission is direct and explicit

Add a direct edge only when A genuinely depends on C's contract.

Do not rely on accidental transitive availability or hoisting.

---

# 87. pnpm strictness

pnpm workspace behavior helps expose undeclared dependency assumptions.

Do not work around missing declaration by relying on node_modules layout.

---

# 88. FE-DEP-044 — Import permission and package declaration both matter

For an internal dependency to be healthy:

```text
architecture permits edge
+
package manifest declares edge when required
+
public export exists
+
source uses approved import
```

One alone is insufficient.

---

# 89. Circular dependencies

Cycles increase coupling and complicate initialization/migration.

The conceptual architecture should be acyclic inward.

---

# 90. FE-DEP-045 — New internal cycles are forbidden by default

If a proposed edge creates:

```text
A → B → A
```

stop and resolve ownership.

Typical fixes:

```text
move shared contract inward
split responsibility
move composition outward
```

Do not accept cycles as “monorepo convenience.”

---

# 91. Runtime cycles

Service/provider construction can create logical cycles even without package cycles.

Keep construction at host composition boundaries.

---

# 92. FE-DEP-046 — Composition resolves dependencies; low-level packages do not locate outer services

Avoid service locator patterns that recreate hidden circular dependency at runtime.

---

# 93. Mobile purity

Mobile safety is a first-class dependency property.

Current architecture explicitly rejects web-only imports/DOM concerns from production mobile graph.

---

# 94. FE-DEP-047 — Mobile production packages reject web-only packages and APIs

Examples include:

```text
react-dom
react-dom/*
@notrelix/ui-web
@notrelix/runtime-web
direct web-app imports
DOM JSX intrinsics
```

Reliable checks SHOULD be AST/package-graph based rather than fragile string scanning.

---

# 95. DOM JSX intrinsics

A mobile package using:

```tsx
<div />
<button />
```

is usually evidence of web rendering leakage.

Native UI should use native components/approved UI-mobile abstractions.

---

# 96. FE-DEP-048 — Native safety includes source syntax, not only package.json

A package can avoid a declared `react-dom` dependency and still contain DOM-specific code.

Architecture checks should protect both dependency graph and reliable source-level rules.

---

# 97. Shared React logic

Some React hooks can be runtime-neutral if they avoid platform APIs/rendering.

Do not assume all React code is web-only.

Classify by actual responsibility.

---

# 98. FE-DEP-049 — Share behavior, not incompatible platform implementation

Good sharing:

```text
query hook/state orchestration
pure product hook
typed contract
```

when native-safe.

Bad sharing:

```text
DOM component imported into mobile
browser navigation wrapper used in Expo
```

---

# 99. Marketing isolation checks

Marketing is allowed to use web UI because it is a web-rendered host.

It is still isolated from authenticated product runtime.

---

# 100. FE-DEP-050 — Web rendering compatibility does not erase marketing semantic isolation

`ui-web` is a visual dependency.

That does not authorize marketing to import:

```text
app-web
features-auth
product-state
```

---

# 101. Adding a package

Before adding a new package determine:

```text
semantic owner
architecture layer
freeze scope
runtime/platform
allowed imports
public exports
consumers
tests
```

Then update workspace/manifest/generator evidence.

---

# 102. FE-DEP-051 — New package is an architecture action

A package MUST NOT be added solely to:

```text
reduce folder size
copy another product topology
avoid one relative import
```

without a stable boundary reason.

---

# 103. Package generator

If a repository package generator exists for the package type, prefer it.

The generator should encode current architecture defaults.

---

# 104. FE-DEP-052 — Generator does not authorize architecture

A generated package still needs correct:

```text
owner
layer
allow-list
public API
```

Do not use generator output as evidence that the package should exist.

---

# 105. Removing a package

Removal requires consumer/reference inventory.

Update:

```text
workspace
manifest
package manifests
imports
generated docs
tests
tooling
docs
```

as applicable.

---

# 106. FE-DEP-053 — Remove authority, not only files

Do not leave:

```text
stale manifest entry
compatibility alias
duplicate old/new package owner
```

without an explicit transition.

---

# 107. Renaming a package

Package name/path is an internal contract.

Rename requires broad migration.

---

# 108. FE-DEP-054 — Package rename preserves architecture identity deliberately

Decide whether rename is:

```text
same owner/new name
or
new owner/architecture migration
```

Do not hide ownership change as mechanical rename.

---

# 109. Moving code between packages

Moving code changes ownership when the source/target have different responsibilities.

---

# 110. FE-DEP-055 — Cross-package move requires semantic review

Before move inspect:

```text
old owner
new owner
public API
new dependency edges
runtime safety
test owner
consumer migration
```

Do not copy first and reason later.

---

# 111. Layer promotion/demotion

Moving a package from:

```text
feature → foundation
product-state → core
adapter → state
```

is architecture-significant.

---

# 112. FE-DEP-056 — Lower-layer promotion increases reuse obligations

Before moving code inward, prove that it is:

```text
product-neutral where required
runtime-neutral where required
stable enough for broader consumers
```

Do not lower it only to resolve a forbidden import.

---

# 113. Dependency pressure as signal

Repeated forbidden edges can indicate:

```text
wrong package owner
missing inward contract
over-fragmentation
real architecture evolution
```

The answer is not automatically allow-list expansion.

---

# 114. FE-DEP-057 — Repeated dependency friction triggers architecture review

Use:

```text
architecture-change-policy.md
```

when dependency pressure indicates the current topology no longer fits the product/runtime boundary.

---

# 115. Editing allowedInternalImports

`allowedInternalImports` is executable architecture.

Changing it is evidence of a dependency architecture change.

---

# 116. FE-DEP-058 — Allow-list edits require rationale

A code review changing the manifest SHOULD explain:

```text
consumer responsibility
provider responsibility
why the edge is needed
why moving behavior inward/outward is worse
runtime/mobile impact
```

Do not accept:

```text
"needed for compile"
```

as sufficient architecture rationale.

---

# 117. Generated docs synchronization

After intended manifest change:

```text
run architecture docs generator
run architecture-doc drift check
review generated diff
```

The generated diff should match the intended dependency change.

---

# 118. FE-DEP-059 — Manifest and generated package map move atomically

A manifest change that changes generated output SHOULD land with regenerated evidence.

A stale generated map is `DOC_STALE`/generated drift, not a second truth.

---

# 119. Architecture checks

Current root commands include:

```bash
pnpm check:architecture
pnpm check:architecture-docs
```

They protect package/source and generated-doc architecture respectively.

Exact implementation belongs to tooling.

---

# 120. FE-DEP-060 — Architecture checker failure is not solved by weakening the checker first

When a gate fails determine:

```text
source wrong?
manifest wrong?
generated docs stale?
architecture changed intentionally?
checker defect?
```

Then fix the correct owner.

---

# 121. Generator/tooling tests

Changes to dependency checker/manifest parser/doc generator require tooling/generator tests.

Do not rely only on project typecheck.

---

# 122. FE-DEP-061 — Architecture tooling is production-governance code

Although it is not shipped to end users, it protects production architecture.

Changes require review/test discipline comparable to other critical infrastructure.

---

# 123. Package manifest versus architecture manifest

A `package.json` may declare a dependency that architecture disallows.

That is source debt.

Conversely the architecture manifest may allow an edge not declared/used.

That is permission, not proof of active dependency.

---

# 124. FE-DEP-062 — Allowed edge is maximum permission, not required edge

Do not create imports just because they are allowed.

Least privilege means packages should use only what they actually need.

---

# 125. Unused allow-list permission

An unused permission can become architecture drift/over-broad policy over time.

Review overly broad feature/app permissions when architecture evolves.

---

# 126. FE-DEP-063 — Permission surface should not grow monotonically without review

Remove stale allow-list edges when no longer part of the intended package contract, provided migration is complete.

---

# 127. Test-only imports

Test files may require special rules depending on checker design.

Do not use tests to normalize production boundary violations.

---

# 128. FE-DEP-064 — Test convenience does not redefine production dependency architecture

Prefer test helpers/testing packages/public seams.

Do not export production internals globally solely for one unit test.

---

# 129. Architecture exceptions

A temporary exception, if governance allows one, is not a second architecture.

It must have:

```text
owner
scope
reason
removal condition
evidence
```

Do not encode temporary debt as permanent broad allow-list if an exception model is more truthful.

---

# 130. FE-DEP-065 — Exception is temporary permission, not precedent

Another package MUST NOT copy an exception edge as justification.

---

# 131. Dependency/security relationship

Dependencies affect security through:

```text
secret exposure
browser/native boundary
supply-chain surface
untrusted rendering
credential handling
```

Architecture review can require security review for high-risk dependencies.

---

# 132. FE-DEP-066 — Client secret handling cannot be solved by package placement

Moving secret-bearing code from app to foundation does not make it safe.

Client-delivered code is inspectable.

Secrets belong on trusted server boundaries.

---

# 133. Dependency/performance relationship

A broad dependency can increase:

```text
bundle size
startup
native binary size
tree-shaking complexity
```

But performance optimization must preserve ownership.

---

# 134. FE-DEP-067 — Bundle optimization does not justify boundary bypass

Measure first.

Prefer:

```text
lazy composition
better public exports
dependency replacement
```

over deep-import/private-copy hacks.

---

# 135. Dependency/team relationship

Stable package ownership enables parallel work.

Over-centralized foundation creates coordination bottlenecks.

Over-fragmentation creates manifest/public-export churn.

---

# 136. FE-DEP-068 — Architecture optimizes ownership clarity, not package count

Evaluate whether a boundary reduces accidental coupling enough to justify its operational cost.

---

# 137. Product family asymmetry

Current Work Management, Documents, and Automation families do not have identical package shapes.

That is intentional.

---

# 138. FE-DEP-069 — Do not normalize product topology for visual symmetry

If Documents needs collaboration but Automation does not, do not add an Automation collaboration package merely to match the tree.

Dependency topology follows behavior.

---

# 139. Feature asymmetry

Some feature packages need realtime, some do not.

The manifest currently uses different least-privilege allow-lists.

---

# 140. FE-DEP-070 — Feature capability determines dependency capability

Do not give all feature packages realtime/UI access by default.

Grant the exact inward capability required.

---

# 141. Architecture drift classification

When docs, manifest, source, or generated evidence disagree classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

using repository governance.

---

# 142. FE-DEP-071 — Do not assume the newest source edge is the intended architecture

An accidental import can compile and land.

If it conflicts with canonical architecture/ADR, treat it as potential source debt.

---

# 143. Accepted ADR relationship

Frontend ADRs include historical decisions relevant to dependencies:

```text
FE-ADR-002 package manager
FE-ADR-003 package exports
FE-ADR-004 no Next in reusable packages
```

Check status/supersession.

---

# 144. FE-DEP-072 — Current architecture describes now; ADR explains why

Do not paste ADR rationale into every dependency rule.

Route historical rationale to the ADR.

---

# 145. Next.js boundary

Next.js belongs to the marketing host according to current architecture/ADR intent.

Reusable packages should not gain `next/*` imports without a consequential architecture decision.

---

# 146. FE-DEP-073 — Next.js is not a general reusable-package framework

A shared package used by marketing should expose framework-neutral/web UI contracts where practical rather than importing Next application APIs.

---

# 147. Router dependency boundary

Host routers belong to host composition.

Product core/state should not depend on TanStack Router/Expo Router simply to navigate after a mutation.

---

# 148. FE-DEP-074 — Navigation dependencies stay outer

Use host/adapter callbacks/contracts or composition where navigation is required.

Do not make inward product semantics own host route mechanics.

---

# 149. Query library boundary

TanStack Query can be used in query/state layers.

Do not leak query-client internals into pure product core.

---

# 150. FE-DEP-075 — Mechanism APIs stay in mechanism/state owners

Pure product calculations should remain testable without constructing a host query client.

---

# 151. React context boundary

Providers are host/UI composition mechanisms.

Do not use one global React context as an untyped cross-package dependency bus.

---

# 152. FE-DEP-076 — Provider composition does not erase package ownership

Even when all services are available in the app provider tree, package code should depend only on its approved contracts.

---

# 153. Service lifetime boundary

A package creating a disposable runtime/service must define ownership/lifecycle.

Host composition commonly owns long-lived application service construction/disposal.

---

# 154. FE-DEP-077 — Creator owns disposal unless a narrower lifecycle contract says otherwise

Do not create hidden global singletons in lower packages with no cleanup path.

---

# 155. Package side effects

Reusable packages should minimize import-time side effects.

Registration/startup should occur through explicit composition where possible.

---

# 156. FE-DEP-078 — Importing a package should not secretly start the application

Avoid package import side effects such as:

```text
open socket
attach global listener
create global query client
start timer
```

unless explicitly documented/owned.

---

# 157. FE-DEP-079 — Architecture permission does not transfer semantic ownership

An allowed edge means:

```text
the dependency is permitted
```

It does **not** mean:

```text
the consumer now owns the provider's semantics
the provider may push behavior outward into the consumer
the edge should be copied to sibling packages
```

Example:

```text
app-web
→ may consume Work Management state
```

does not mean:

```text
app-web
→ becomes the Work Management state owner
```

Dependency permission and semantic ownership are related but distinct governance dimensions.

---

# 159. Dependency validation checklist

Before adding an internal dependency:

```text
[ ] correct consumer owner
[ ] correct provider owner
[ ] exact manifest permission
[ ] package.json declaration
[ ] public export
[ ] no deep import
[ ] no circular edge
[ ] runtime/platform compatibility
[ ] mobile safety
[ ] marketing isolation
[ ] tests/gates
[ ] generated docs if manifest changes
```

---

# 159. Package creation checklist

```text
[ ] semantic boundary justified
[ ] path fits workspace discovery
[ ] unique package name/path
[ ] architecture layer chosen
[ ] freeze scope chosen
[ ] least-privilege allow-list
[ ] public API designed
[ ] no unnecessary framework coupling
[ ] tests
[ ] architecture docs/generator updated
```

---

# 160. Package move checklist

```text
[ ] old owner documented
[ ] new owner justified
[ ] consumers inventoried
[ ] public API migration
[ ] dependency edges recalculated
[ ] runtime/mobile safety
[ ] tests migrated
[ ] old path removed
[ ] generated evidence current
```

---

# 161. Stop conditions

Stop before coding if:

- the only way forward is a deep import;
- a product package needs an app/router import;
- mobile needs `ui-web`, `runtime-web`, React DOM or DOM JSX;
- marketing needs authenticated product-state packages;
- foundation needs product-specific behavior;
- a feature needs broad cross-feature imports;
- an allow-list edit is justified only by “compile error”;
- a new package exists only for symmetry;
- a package cycle would be introduced;
- generated package docs are being hand-edited;
- an accepted ADR conflicts with a proposed dependency and no superseding decision exists;
- ownership is unclear enough that moving the code would materially change the dependency graph.

---

# 162. Executable evidence

Primary executable sources/gates:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/tooling/dependency-rules/src/
frontend/docs/generated/package-boundaries.md
frontend/pnpm-workspace.yaml
frontend/**/package.json
pnpm check:architecture
pnpm check:architecture-docs
generator/tooling tests
```

Use current source/tooling as evidence of the current graph.

Use this document as the semantic owner of dependency rules.

---

# 163. Related architecture

Read:

```text
frontend-overview.md
hosts-composition-routing.md
testing-and-quality-gates.md
architecture-change-policy.md
```

For package-specific state/realtime/UI concerns also read their topic owners.

---

# 164. Related decisions

Relevant frontend decision history:

```text
../decisions/README.md
FE-ADR-002
FE-ADR-003
FE-ADR-004
```

Do not assume status; check registry.

---

# 165. Explicit non-responsibilities

This document does not decide:

```text
backend API compatibility
resource authorization semantics
query-key details
realtime ordering semantics
route names
visual token values
feature product behavior
```

It defines who may depend on whom and how that boundary is governed.

---

# 166. Final dependency model

The frontend package graph should remain understandable as:

```text
host apps
        ↓
platform-specific adapters + features
        ↓
product state/collaboration/plugins
        ↓
product core
        ↓
foundation
```

with:

```text
web runtime
and
mobile runtime
```

kept separate,

and:

```text
ui-web
and
ui-mobile
```

sharing semantic tokens rather than rendering implementations.

The exact graph is executable.

The authored architecture explains why that graph is constrained.

The boundary is successful when a developer cannot accidentally turn package modularity into unrestricted monolithic coupling.
