---
document_id: FE-README
document_type: overview
status: active
owner: frontend-platform
applies_to:
  - frontend
  - frontend-onboarding
  - frontend-workspace
evidence:
  - frontend/package.json
  - frontend/pnpm-workspace.yaml
  - frontend/turbo.json
  - frontend/apps/
  - frontend/packages/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - .github/workflows/fe-ci.yml
review_on:
  - frontend-workspace-topology-change
  - frontend-host-change
  - frontend-toolchain-change
  - frontend-entrypoint-change
  - frontend-documentation-routing-change
---

# Notrelix Frontend

The Notrelix frontend is the client platform for the Notrelix enterprise work-management product.

It is a **pnpm/Turborepo workspace** with three host applications:

```text
apps/web
→ Vite web application

apps/mobile
→ Expo / React Native mobile application

apps/marketing
→ Next.js marketing application
```

Reusable client behavior is organized into explicit package families under:

```text
packages/foundation
packages/runtimes
packages/ui
packages/product
packages/features
tooling
```

The frontend is intentionally not one large application tree.

It is a governed client platform in which:

```text
apps compose
foundation provides product-agnostic primitives/contracts
runtime packages bind host-specific capabilities
UI packages own platform UI implementations
product packages own reusable product-capability behavior
feature packages own cross-product feature slices
tooling enforces and generates architecture evidence
```

This README is the onboarding and workspace-orientation entrypoint.

It is **not** the canonical owner of every frontend architecture rule.

Use the documentation routing sections below to reach the authoritative topic.

---

# 1. Start here

For a developer or coding agent working in the frontend, read in this order:

```text
1. root README.md
2. root RULE.md
3. root AGENTS.md
4. frontend/README.md
5. frontend/AGENTS.md
6. frontend/docs/README.md
7. relevant frontend architecture document
8. related FE-ADR if historical rationale matters
9. generated/package/source/test evidence
```

Do not start by reading random package README files and inferring the architecture from them.

The canonical frontend documentation index is:

```text
frontend/docs/README.md
```

---

# 2. What the frontend owns

The frontend owns client-side concerns including:

```text
host composition
routing
session/client principal state
typed backend contract consumption
server-state query/cache integration
optimistic client interaction behavior
realtime connection and reconciliation
client-side product adapters
design-system implementation
accessibility behavior
responsive behavior
platform-specific interaction
web/mobile/marketing runtime integration
frontend testing and architecture gates
```

The frontend does **not** own backend business truth.

Examples:

```text
frontend permission visibility
≠ backend authorization

cached entity state
≠ persistence authority

optimistic update
≠ committed server outcome

route parameter
≠ tenant/resource authority

frontend entitlement display
≠ Billing source of truth
```

The backend remains authoritative for durable server state, business authorization, tenant ownership, persistence, and server-side invariants.

---

# 3. Product relationship

Notrelix is a work-management/workspace platform.

The frontend should support the product model defined by repository product documentation rather than inventing a separate client product model.

Core product capabilities include bounded-context concepts such as:

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

The frontend may package these capabilities differently where client composition requires it.

A frontend package boundary is **not automatically a business bounded context**.

---

# 4. Host applications

## 4.1 Web

Path:

```text
frontend/apps/web
```

Package:

```text
@notrelix/app-web
```

The web host is the main authenticated browser application.

It is the composition root for web-specific concerns such as:

```text
Vite application startup
web routing
web runtime construction
provider composition
web shell
web-only environment integration
web adapters
```

Reusable product behavior should not be moved into the app merely because the web app uses it first.

---

## 4.2 Mobile

Path:

```text
frontend/apps/mobile
```

Package:

```text
@notrelix/app-mobile
```

The mobile host is the Expo/React Native application.

The mobile graph must remain native-safe.

Production mobile packages must not acquire accidental dependencies on:

```text
react-dom
DOM APIs
web-only UI implementations
web runtime adapters
web application internals
Next.js
```

Platform-specific adaptation belongs in:

```text
apps/mobile
packages/runtimes/mobile
packages/ui/mobile
product mobile adapters
```

according to ownership.

---

## 4.3 Marketing

Path:

```text
frontend/apps/marketing
```

Package:

```text
@notrelix/app-marketing
```

The marketing host is intentionally isolated from the authenticated product-application dependency graph.

It uses the shared visual foundation where approved, but it should not become a second product runtime.

Typical shared dependencies are:

```text
UI tokens
web UI primitives where approved
icons
```

Marketing content/navigation/SEO/runtime concerns remain host-owned.

---

# 5. Workspace requirements

Current workspace requirements are defined by `frontend/package.json`.

The current minimums are:

```text
Node >= 22
pnpm >= 10
```

The repository currently pins:

```text
packageManager = pnpm@10.0.0
```

Use Corepack/pnpm according to repository setup.

Do not introduce a second package manager.

Do not maintain another lockfile.

---

# 6. Install dependencies

From:

```text
frontend/
```

run:

```bash
pnpm install --frozen-lockfile
```

CI requires lockfile consistency.

If installation reports that the lockfile does not match a package manifest:

```text
fix/update the workspace lockfile intentionally
```

Do not make CI silently use:

```text
--no-frozen-lockfile
```

as the normal solution.

---

# 7. Workspace structure

Current workspace globs are defined by:

```text
frontend/pnpm-workspace.yaml
```

They include:

```text
apps/*
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/features/*
tooling/*
tooling/storybook/web
```

This file defines pnpm workspace discovery.

It does not define the complete allowed dependency graph.

That authority belongs to the architecture manifest.

---

# 8. Executable architecture authority

The exact closed-world frontend package architecture is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Every source-bearing app/package under the governed workspace must be represented according to the manifest/checker contract.

The manifest defines for each registered unit:

```text
package name
relative path
architecture layer
freeze scope
allowed internal imports
```

It is executable authority.

Do not hand-maintain a competing dependency matrix in README files.

---

# 9. Architecture layers

The current manifest recognizes architecture layers including:

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

These are dependency-governance layers.

They are not automatically organizational team names or business contexts.

---

# 10. Freeze scopes

The current architecture manifest distinguishes:

```text
core-production
verification
marketing-isolated
```

Freeze scope describes architecture coverage.

It does **not** mean:

```text
every feature is finished
```

A package can belong to a frozen architecture while product functionality continues to evolve.

---

# 11. Foundation packages

Current foundation package family includes concepts such as:

```text
contracts
kernel
platform
query
realtime
observability
```

Foundation must remain narrowly reusable.

The key rule is:

```text
foundation
→ product-agnostic client primitives/contracts

NOT

foundation
→ convenient dumping ground for reusable-looking product logic
```

Before moving behavior into foundation, ask:

```text
Would this behavior still make sense if Work Management,
Documents, Billing and Governance did not exist?
```

If not, it is probably not foundation.

---

# 12. Runtime packages

Current runtime packages include:

```text
@notrelix/runtime-web
@notrelix/runtime-mobile
```

Runtime packages adapt host-specific mechanisms to reusable inward contracts.

Examples:

```text
browser storage
web navigation/runtime services
mobile storage
mobile linking
platform lifecycle
host-specific realtime construction
host observability binding
```

according to current architecture.

A runtime package does not own product semantics.

---

# 13. UI packages

Current UI family includes:

```text
@notrelix/ui-tokens
@notrelix/ui-web
@notrelix/ui-mobile
@notrelix/ui-icons
```

The architecture intentionally separates:

```text
shared design tokens
web UI implementation
mobile UI implementation
icons
```

Do not make mobile depend on `ui-web` merely to reuse a component implementation.

Share semantic tokens/primitives at the correct layer instead.

---

# 14. Product packages

Dedicated product package families currently exist for capabilities including:

```text
Work Management
Documents
Automation
```

These families may contain combinations of:

```text
core
state
collaboration
plugins
web adapter
mobile adapter
testing support
```

depending on the capability.

Do not force every capability to have every package type for symmetry.

Depth follows product/architecture need.

---

# 15. Feature packages

Current cross-product feature packages include areas such as:

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

Feature packages own client-facing vertical behavior that crosses or does not warrant a dedicated product package family.

They do not have unrestricted access to every internal package.

Their exact allowed imports are executable in the architecture manifest.

---

# 16. Apps compose

A central frontend rule is:

```text
apps compose
```

Apps may own:

```text
routing tables
host providers
shell composition
runtime construction
top-level error boundaries
host startup
host environment
```

Apps should not become the default home for reusable:

```text
product calculations
query/mutation semantics
realtime reconciliation
cross-route feature logic
design-system primitives
```

Move reusable behavior to the correct owner.

---

# 17. No generic client DI/module framework by default

Notrelix does not require an abstract enterprise module registry simply because the workspace is modular.

Prefer:

```text
explicit package imports
explicit host composition
typed contracts
small runtime adapters
```

over:

```text
global service locator
magic module discovery
reflection-style registration
generic feature registry
untyped dependency bag
```

unless a concrete problem justifies an architecture decision.

---

# 18. Public package boundaries

A package's public exports are part of its contract.

Consume other packages through approved public entrypoints.

Do not deep-import another package's internals.

Forbidden style:

```ts
import { something } from "@notrelix/foo/src/internal/thing";
```

Preferred:

```ts
import { something } from "@notrelix/foo";
```

or an explicitly exported supported subpath.

Dependency checks enforce this architecture.

---

# 19. Backend contracts

Generated backend contracts are preferred over handwritten duplication.

The frontend codegen task currently consumes contract inputs including:

```text
backend/contracts/openapi/notrelix.v1.json
artifacts/contracts/realtime.v1.json
```

and generates frontend contract code under the foundation contracts package.

Do not manually patch generated contract output to disagree with backend producer contracts.

---

# 20. Code generation

Run:

```bash
pnpm codegen
```

To verify generated REST/client contracts are current:

```bash
pnpm codegen:check
```

The current check regenerates and fails when generated contract output differs from committed output.

Generated files are evidence/artifacts.

Change their producer/input instead of hand-editing them.

---

# 21. Server state

Server state remains backend-authoritative.

Frontend server-state architecture should preserve:

```text
tenant/workspace/resource-scoped query keys
stable cache ownership
explicit invalidation
mutation outcome handling
workspace transition safety
permission-sensitive visibility
```

Do not copy backend persistence into client stores as a second source of truth.

---

# 22. Query/cache distinction

Client query cache is:

```text
derived server state
```

It is not:

```text
offline authoritative database by default
```

Optimistic state can improve interaction latency but must reconcile with the server result.

---

# 23. Realtime

Realtime updates supplement server-state reconciliation.

The frontend must handle realities such as:

```text
duplicate messages
out-of-order delivery
reconnect
gaps
stale query cache
permission changes
workspace transition
```

The realtime layer should not create a second independent product state model.

Use the canonical realtime architecture document for details.

---

# 24. Authentication and authorization

Frontend authentication/session state is a client concern.

Backend authorization remains server-authoritative.

The frontend can:

```text
hide unavailable action
disable control
redirect route
show permission state
```

for UX.

It cannot use those UX checks as the security boundary.

---

# 25. Tenant/resource identifiers

A frontend route can carry:

```text
accountId
workspaceId
boardId
pageId
```

as navigation/request inputs.

The frontend must not treat these IDs as security proof.

Backend scope resolution and authorization remain authoritative.

---

# 26. Web/mobile behavioral parity

Web and mobile do not need identical component implementations.

They should preserve the same product contract where the same capability exists.

Platform-specific differences are valid for:

```text
navigation
gesture
input
layout
native OS integration
offline/lifecycle handling
```

Do not create different product semantics accidentally because one host is harder to implement.

---

# 27. Marketing isolation

Marketing is not the place to import authenticated product state packages simply to reuse a visual element.

If a marketing visual pattern belongs in shared UI:

```text
promote the primitive/token to the correct UI owner
```

rather than:

```text
marketing → product package
```

unless executable architecture explicitly permits the dependency.

---

# 28. Development commands

From `frontend/`:

## All development tasks

```bash
pnpm dev
```

## Web

```bash
pnpm dev:web
```

## Mobile

```bash
pnpm dev:mobile
```

## Marketing

```bash
pnpm dev:marketing
```

Use filtered package commands when working on one package where appropriate.

---

# 29. Build commands

Full workspace:

```bash
pnpm build
```

Specific hosts:

```bash
pnpm --filter @notrelix/app-web build
pnpm --filter @notrelix/app-mobile build
pnpm --filter @notrelix/app-marketing build
```

A package/host build proves build/package compatibility.

It does not replace architecture/tests/E2E evidence.

---

# 30. Typecheck

Run:

```bash
pnpm typecheck
```

Turborepo executes participating package typechecks according to workspace tasks.

Type correctness does not prove runtime or product semantics.

---

# 31. Lint

Run:

```bash
pnpm lint
```

Lint protects static code-quality rules.

Do not weaken lint globally because one imported third-party/generated component is inconvenient.

Use narrowly justified configuration/exclusion where required.

---

# 32. Format

Check:

```bash
pnpm format:check
```

Apply package/repository-approved formatting through existing tooling.

Formatting is quality hygiene.

It is not architecture proof.

---

# 33. Architecture checks

Run:

```bash
pnpm check:architecture
```

This verifies the executable package/import architecture.

Run:

```bash
pnpm check:architecture-docs
```

This verifies the generated architecture documentation associated with dependency rules.

If these fail, do not immediately weaken the checker.

First determine whether:

```text
source is wrong
manifest is wrong
architecture decision changed
generated docs are stale
```

---

# 34. Generated package-boundary evidence

The generated package architecture document is:

```text
frontend/docs/generated/package-boundaries.md
```

Its producer is owned by the dependency-rules tooling.

Do not edit it manually.

Current package-boundary generation/check commands are exposed by:

```text
@notrelix/dependency-rules
```

through:

```text
docs:generate
docs:check
```

and the frontend root scripts route through the check.

---

# 35. Test taxonomy

Current root testing commands include:

```text
test:node
test:web
test:integration
test:mobile
test:generators
```

The guarded variants verify non-zero execution/evidence.

Use:

```bash
pnpm test
```

for the regular combined suite.

Use:

```bash
pnpm validate
```

for the broader guarded validation set.

---

# 36. Guarded test commands

Current guarded commands include:

```bash
pnpm test:node:guarded
pnpm test:web:guarded
pnpm test:integration:guarded
pnpm test:mobile:guarded
pnpm test:generators:guarded
```

These commands produce machine-readable result evidence and verify that expected work actually ran.

A successful test command that selected no intended tests is not acceptable proof for a required gate.

---

# 37. UI foundation tests

Storybook/Playwright currently supports frontend UI foundation checks.

Root scripts include:

```bash
pnpm test:ui:a11y
pnpm test:ui:visual
pnpm test:ui:freeze
```

Use the canonical UI/testing documents for expected scope.

Visual snapshots should protect intentional UI contracts, not make every pixel impossible to evolve.

---

# 38. End-to-end tests

Current production-oriented web E2E command:

```bash
pnpm e2e
```

E2E is appropriate for cross-boundary user flows.

Do not duplicate every small package invariant through Playwright.

Use the cheapest reliable seam.

---

# 39. Validation tiers

Current root scripts expose:

```bash
pnpm validate:fast
pnpm validate
```

`validate:fast` currently combines high-value generated/architecture/static/core checks.

`validate` adds the broader integration/mobile/generator guarded suites.

The exact script body is executable in `frontend/package.json`.

Do not duplicate the full command chain in docs as a second authority.

---

# 40. Current CI shape

Frontend CI currently protects categories including:

```text
quality
core tests
mobile tests
tooling/generator tests
UI foundation
web build
marketing build
mobile build
production E2E
final frontend gate
```

The exact workflow is:

```text
.github/workflows/fe-ci.yml
```

CI topology may evolve.

The protected properties should not silently disappear.

---

# 41. CI contract inputs

Frontend CI is also affected by shared contract paths such as:

```text
backend/contracts/openapi/**
artifacts/contracts/**
```

because frontend generated clients/realtime contracts depend on those producers.

A backend contract change can therefore be a frontend-relevant change even when no frontend source file was edited.

---

# 42. Web build-time environment

The web host uses public Vite environment configuration for client-known values.

Client-visible variables are not secret storage.

Never put a secret into:

```text
VITE_*
```

because it can become part of browser-delivered output.

---

# 43. Mobile public environment

Expo public variables are likewise client-visible.

Do not place backend/provider secret material in:

```text
EXPO_PUBLIC_*
```

Treat them as public application configuration.

---

# 44. Marketing environment

Next.js distinguishes server-only and public variables.

Do not move secrets into `NEXT_PUBLIC_*`.

Marketing deployment configuration belongs to the marketing host and repository environment model.

---

# 45. Dependency addition

Before adding a dependency ask:

```text
Which package owns this need?
Is the dependency runtime-specific?
Is it safe for mobile?
Does the manifest allow the internal dependency?
Does the package already expose an abstraction?
Does this create a framework dependency in foundation/core?
```

Do not add a dependency at root simply because multiple packages might eventually need it.

---

# 46. Internal dependency addition

Adding an internal package dependency normally requires:

```text
package manifest change
architecture-manifest allow-list change
architecture check
generated architecture-doc refresh/check
affected tests
```

A manifest edit is architecture evidence.

It is not a workaround for an arbitrary import.

---

# 47. Adding a package

A new governed source package/app must be intentionally added to:

```text
pnpm workspace discovery
architecture manifest
package exports
tooling/test taxonomy as applicable
generated package-boundary evidence
```

Do not create an unregistered package directory and rely on TypeScript path resolution.

---

# 48. Package generator

If repository tooling provides a package/module generator for the desired package family, use it.

The generator must encode current architecture.

Do not copy an old package directory and preserve stale exports/dependencies blindly.

---

# 49. Moving code

When moving code across packages, treat it as an ownership change.

Review:

```text
semantic owner
public exports
internal imports
runtime/platform safety
query/cache owner
backend contract dependencies
tests
generated docs
```

Do not perform a mechanical move that turns an adapter into a new authority.

---

# 50. Cross-product features

A feature package should not become a hidden product-monolith package.

If one feature starts accumulating:

```text
Work Management domain model
Documents state
Billing policy
Governance semantics
```

reassess ownership instead of expanding its allow-list indefinitely.

---

# 51. Product state packages

Product state packages may own reusable client state/query/reconciliation for their product capability.

They should not own:

```text
host router
browser-only storage implementation
mobile UI
backend business authorization
```

unless explicitly scoped by architecture.

---

# 52. Product adapters

Web/mobile adapters translate product behavior into platform-specific presentation/runtime integration.

Keep:

```text
core/state
```

separate from:

```text
web/mobile rendering and host APIs
```

where the current product family architecture defines that split.

---

# 53. Collaboration/realtime packages

Collaboration packages can own collaborative client behavior for their product capability.

Do not put generic realtime transport/session construction there if foundation/runtime owns it.

Distinguish:

```text
realtime mechanism
from
collaboration product semantics
```

---

# 54. Testing packages

Product testing packages are verification support.

They do not become production dependency shortcuts.

Production packages should not import test helpers.

The architecture manifest/freeze scope helps keep this distinction explicit.

---

# 55. Foundation contracts

`@notrelix/contracts` is the client contract boundary for generated/normalized backend-facing contracts.

Do not hand-create duplicate REST DTO types inside feature/product packages when generated contract types are suitable.

When an internal semantic type should differ from wire representation:

```text
map intentionally at the contract boundary
```

rather than leak transport shape everywhere.

---

# 56. Kernel

The kernel should contain only very stable product-agnostic client primitives.

A type is not kernel-worthy merely because many packages import it.

Broad usage can indicate a missing owner just as easily as a shared primitive.

---

# 57. Platform foundation

Platform abstractions should describe reusable client capabilities.

They should not become an untyped service container.

Prefer explicit narrow contracts.

---

# 58. Query foundation

Query foundation owns generic query/cache mechanics.

Product query keys and resource-specific invalidation should stay with their product/feature state owner.

Avoid a central file containing every application's query key.

---

# 59. Realtime foundation

Realtime foundation owns reusable transport/reconciliation primitives.

Product-specific event-to-state behavior belongs with the affected product/feature state owner.

Do not place Board/Page/Billing semantics in generic realtime transport.

---

# 60. Observability foundation

Frontend observability primitives should remain vendor-adaptable according to architecture.

Do not make product packages depend directly on one vendor SDK unless the architecture explicitly assigns that dependency.

---

# 61. UI tokens

Tokens define visual system values such as:

```text
color
spacing
typography
radius
motion
density
```

according to design-system architecture.

Tokens are semantic design inputs.

Do not put arbitrary component business behavior into token packages.

---

# 62. UI web

`ui-web` owns reusable web UI primitives/implementations.

It can depend on web UI technology.

It should not be imported by mobile production packages.

---

# 63. UI mobile

`ui-mobile` owns reusable native UI primitives/implementations.

Do not wrap web components/DOM simply to simulate reuse.

Share tokens/semantics, not incompatible rendering machinery.

---

# 64. Icons

Icons are a narrow shared visual asset boundary.

Keep icon package exports predictable.

Avoid product state dependencies.

---

# 65. Framework split

The three hosts intentionally use different frameworks/runtime stacks.

This is not a defect to “normalize” through a universal framework abstraction.

Use:

```text
Vite
→ authenticated web product host

Expo
→ native mobile host

Next.js
→ marketing host
```

according to the accepted frontend decisions/current architecture.

---

# 66. Next.js boundary

Next.js belongs to the marketing host unless an accepted architecture decision expands its scope.

Do not import:

```text
next/*
```

into foundation/product/reusable client packages merely because marketing already depends on Next.

---

# 67. React dependency

Reusable packages should expose React dependencies only where their architecture requires React.

Do not force pure core/state/data packages to become React component packages accidentally.

---

# 68. TanStack/query/router dependencies

Use generic query/router libraries at the owner designated by package architecture.

Do not spread router-specific APIs into product core.

Routing is primarily host composition.

Server-state query integration belongs in state/query owners.

---

# 69. State taxonomy

Before creating state, classify it:

```text
server state
URL/navigation state
local interaction state
form draft
ephemeral UI state
runtime/session state
persisted client preference
```

Do not put all state into one global store.

The storage/owner should follow the state class.

---

# 70. URL state

Shareable/navigation-relevant state often belongs in the route/URL.

Do not duplicate it into a global store without a reason.

Keep route parsing/validation explicit.

---

# 71. Local UI state

Local component interaction state should stay local when possible.

Do not centralize:

```text
open popover
hover state
temporary input focus
```

into product-wide stores.

---

# 72. Form state

Form draft state is not committed server state.

Mutation success/failure should reconcile the draft with authoritative server outcome according to UX contract.

---

# 73. Workspace transition

Changing Workspace can invalidate:

```text
query cache
realtime subscription
permissions
navigation
product state
```

Treat workspace transition as a coordinated boundary.

Do not keep old-Workspace server state visible by accident.

---

# 74. Account transition

Account-level transition can have even broader effect:

```text
workspace list
billing
integrations
governance
realtime
queries
```

Use canonical state/routing contracts.

---

# 75. Loading/error/empty/permission/conflict

A feature is not complete when it only renders the success state.

Consider:

```text
loading
empty
error
permission denied
not found
conflict
offline/degraded
stale/reconnecting
```

as applicable.

The exact UI belongs to feature/product/design-system owners.

---

# 76. Accessibility

Accessibility is a required product-quality property.

Frontend work should preserve:

```text
keyboard access
focus management
semantic roles
labels
contrast
reduced motion
screen-reader behavior
touch target suitability
```

as applicable.

Use repository accessibility standard and UI architecture.

---

# 77. Responsive behavior

Responsive behavior belongs to product/UI design.

Do not assume desktop-first DOM can simply be shrunk for mobile.

Web responsive and native mobile can use different presentation while preserving product semantics.

---

# 78. Error contracts

Do not parse arbitrary backend error strings to decide client behavior when a stable error contract/category exists.

Use typed/normalized API error handling according to API-contract architecture.

Human-readable backend detail is not a stable machine protocol by default.

---

# 79. Optimistic update

Use optimistic update only when:

```text
the user benefit is meaningful
rollback/reconciliation is defined
conflict semantics are understood
mutation identity is stable
```

Do not optimistically fake an operation whose server outcome is highly uncertain/destructive without recovery UX.

---

# 80. Offline behavior

Offline support is capability-specific.

Do not make a package an offline-first database by accident.

Explicitly define:

```text
what can be viewed
what can be queued
what conflicts
what requires online authority
```

before implementing durable offline mutation.

---

# 81. Client persistence

Browser/mobile persistence can store:

```text
non-secret preferences
bounded runtime/session material
approved caches
```

according to security/runtime architecture.

Do not persist secrets or tenant data indefinitely by convenience.

---

# 82. Security

Never treat client code as confidential.

Browser/mobile bundles can be inspected.

Do not ship:

```text
backend secret
provider secret
private signing key
database credential
```

in frontend code or public environment variables.

---

# 83. XSS/HTML

Avoid unsafe HTML injection.

If rich content requires HTML/rendering, sanitize and use the approved content model.

Do not bypass framework escaping casually.

---

# 84. Logging

Frontend logs/telemetry should avoid:

```text
access token
refresh token
API key
password
private content payload
sensitive personal information
```

unless an explicitly approved privacy/security contract permits a safe transformed form.

---

# 85. Releases

Build/release identity should be available where observability/runtime needs it.

Do not use runtime release SHA as product version semantics.

Deployment/release process belongs to repository Delivery/Infrastructure docs.

---

# 86. Local environment

Use repository environment templates/current host documentation.

Do not add new root-level `.env` conventions without updating environment governance.

Public frontend variables are not secret channels.

---

# 87. Docker/container behavior

Web/marketing container builds belong to packaging/deployment concerns.

A successful container build proves packaging.

It does not prove:

```text
runtime health
E2E
accessibility
backend compatibility
```

unless those checks actually execute.

---

# 88. Documentation

Frontend canonical docs are organized under:

```text
frontend/docs/architecture
frontend/docs/decisions
frontend/docs/generated
```

Do not create:

```text
frontend/ARCHITECTURE.md
frontend/RULES.md
frontend/MIGRATION_TRACKER.md
```

as competing authority planes.

Route to the canonical owner instead.

---

# 89. Architecture docs

Current target frontend architecture topics are:

```text
frontend-overview.md
dependency-boundaries.md
hosts-composition-routing.md
api-and-contracts.md
state-query-mutations.md
realtime.md
ui-and-design-system.md
testing-and-quality-gates.md
architecture-change-policy.md
```

Each topic has one normative owner.

---

# 90. Decisions

Historical frontend architecture decisions live under:

```text
frontend/docs/decisions/
```

with IDs:

```text
FE-ADR-NNN
```

Current architecture docs remain the first place to learn how the system should work now.

ADRs explain why consequential choices were made.

---

# 91. Generated docs

Generated evidence includes:

```text
frontend/docs/generated/package-boundaries.md
```

Generated docs must name their producer.

Do not edit generated facts by hand.

---

# 92. When architecture and source disagree

Do not guess.

Classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

according to repository documentation governance.

Then repair the correct owner.

---

# 93. Common architecture mistakes

Avoid:

```text
business logic in app routes
deep imports
web dependencies in mobile
Next.js in reusable packages
manual contract DTO duplication
manual package matrix duplication
global state for all state classes
cache treated as source of truth
frontend permission check treated as security
foundation used as dumping ground
unbounded feature package dependency allow-list
```

---

# 94. Before coding

Identify:

```text
product/feature owner
host impact
backend contract
query/cache owner
realtime impact
web/mobile differences
architecture-manifest dependency
public export impact
tests/gates
migration/compatibility
```

before changing files.

Do not start from “which component file can I edit fastest?”

---

# 95. Before adding a dependency

Check:

```text
correct owning package
runtime safety
mobile safety
manifest allow-list
bundle/runtime impact
license/security
existing abstraction
test/tooling impact
```

Then update manifests/gates atomically.

---

# 96. Before changing generated code

Stop.

Identify the generator/input.

Examples:

```text
OpenAPI
→ backend producer contract
→ frontend codegen

package boundaries
→ architecture manifest
→ dependency-rules generator
```

Fix producer/input and regenerate.

---

# 97. Before changing public exports

Treat public exports as package contract.

Review:

```text
current consumers
deep import migration
host compatibility
mobile/web compatibility
tree-shaking/bundle impact
```

and tests.

---

# 98. Before changing host composition

Review:

```text
provider order
auth/session
router
query client
realtime
observability
error boundary
environment
runtime adapter
```

Host composition changes can affect the entire client even if one file changes.

---

# 99. Before changing query behavior

Review:

```text
query key
tenant/workspace scope
stale/cache policy
mutation invalidation
optimistic update
realtime reconciliation
workspace transition
permission visibility
```

---

# 100. Before changing realtime behavior

Review:

```text
connection owner
auth/re-auth
subscription scope
duplicate
out-of-order
gap/reconnect
cache reconciliation
tenant transition
permission revocation
```

---

# 101. Before changing UI primitives

Review:

```text
tokens
web/mobile ownership
accessibility
keyboard/focus
loading/error states
visual regression
consumer migration
```

Do not put product-specific semantics into a primitive.

---

# 102. Before changing mobile packages

Run the mobile architecture check mentally and executable:

```text
DOM?
react-dom?
ui-web?
runtime-web?
web app import?
Next.js?
```

If yes, stop unless the package is explicitly web-scoped.

---

# 103. Before changing marketing

Keep marketing isolated.

Ask whether the requested reuse should be:

```text
shared token/UI primitive
```

instead of:

```text
marketing importing product application package
```

---

# 104. Validation baseline

For a broad frontend change, useful baseline commands are:

```bash
pnpm codegen:check
pnpm check:architecture
pnpm check:architecture-docs
pnpm check:test-taxonomy
pnpm typecheck
pnpm lint
pnpm format:check
pnpm test
```

Use narrower focused commands during development and broader required gates before completion according to change class.

---

# 105. Fast validation

Current convenience command:

```bash
pnpm validate:fast
```

Use it for broad fast feedback.

Do not claim it includes every mobile/integration/generator/UI/E2E gate without checking the current script/workflow.

---

# 106. Full root validation

Current root:

```bash
pnpm validate
```

adds broader guarded suites.

CI still has additional host builds/UI/E2E gates.

Local `pnpm validate` is not automatically identical to all CI jobs.

---

# 107. CI final authority

For merge/release evidence, use the exact current CI workflow/result for the exact revision.

Do not cite an older green SHA after source changes.

---

# 108. Adding a new host

A new host is an architecture change.

It requires deliberate decisions about:

```text
runtime
UI platform
contracts
routing/navigation
state
realtime
testing
packaging
dependency manifest
```

Do not create `apps/foo` and let it import everything.

---

# 109. Adding a new product family

A dedicated product package family is justified by meaningful reusable product capability complexity.

Do not create:

```text
core
state
web
mobile
testing
plugins
```

for every feature merely for symmetry.

Create only the boundaries that solve actual ownership/runtime/dependency needs.

---

# 110. Adding a new feature package

A new cross-product feature package should have:

```text
clear owner
clear public surface
least-privilege internal imports
host consumers
test owner
```

Do not create a package just to reduce file count in `apps/web`.

---

# 111. Changing architecture

Architecture change is not “edit manifest until import passes”.

Use:

```text
frontend/docs/architecture/architecture-change-policy.md
```

and FE ADR policy when the change is consequential.

Update:

```text
canonical docs
manifest
source
tests
generated evidence
```

together.

---

# 112. Documentation authority rule

The shortest route is:

```text
orientation
→ this README

agent execution
→ AGENTS.md

architecture
→ docs/architecture/<topic>.md

decision history
→ docs/decisions/FE-ADR-*.md

exact package dependency facts
→ architecture-manifest.ts
→ docs/generated/package-boundaries.md

exact commands
→ package.json / Makefile / CI
```

Do not duplicate one topic across all of them.

---

# 113. Repository navigation

From repository root:

```text
frontend/
├── apps/
├── packages/
├── tooling/
├── docs/
├── package.json
├── pnpm-workspace.yaml
└── turbo.json
```

Use package manifests/source as current inventory evidence.

Do not rely on a manually curated complete package list in this README.

---

# 114. Useful source authorities

```text
frontend/package.json
→ scripts, engine/package-manager requirements

frontend/pnpm-workspace.yaml
→ workspace discovery

frontend/turbo.json
→ task graph/cache/output behavior

frontend/tooling/dependency-rules/src/architecture-manifest.ts
→ package universe + allowed internal dependencies

frontend/docs/generated/package-boundaries.md
→ generated readable boundary evidence

.github/workflows/fe-ci.yml
→ CI protected execution
```

---

# 115. Where to read next

Use:

```text
frontend/docs/README.md
```

and route by concern.

Typical routes:

```text
package/dependency question
→ dependency-boundaries.md

routing/provider/session question
→ hosts-composition-routing.md

REST/OpenAPI/client question
→ api-and-contracts.md

query/mutation/cache question
→ state-query-mutations.md

realtime question
→ realtime.md

component/token/a11y question
→ ui-and-design-system.md

test/gate question
→ testing-and-quality-gates.md

architecture modification question
→ architecture-change-policy.md
```

---

# 116. Final frontend orientation

The frontend should remain understandable as:

```text
backend/product contracts
        ↓
generated/normalized frontend contracts
        ↓
foundation mechanisms
        ↓
product/feature behavior
        ↓
web/mobile adapters
        ↓
host composition
        ↓
user experience
```

with the package graph constrained by:

```text
architecture-manifest.ts
```

and verified by:

```text
architecture checks
generated-doc drift checks
type/lint/format
guarded tests
UI gates
host builds
E2E
CI final gate
```

The goal is not to maximize package count.

The goal is to preserve **clear ownership, explicit runtime boundaries, backend-authoritative state, web/mobile safety, generated contract fidelity, and parallel feature development without architectural guessing**.
