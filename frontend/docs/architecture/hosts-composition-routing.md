---
document_id: FE-ARCH-HOSTS-COMPOSITION-ROUTING
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-hosts
  - web-composition
  - mobile-composition
  - marketing-composition
  - frontend-routing
  - frontend-session-bootstrap
  - frontend-service-lifecycle
evidence:
  - frontend/apps/web/
  - frontend/apps/mobile/
  - frontend/apps/marketing/
  - frontend/packages/runtimes/web/
  - frontend/packages/runtimes/mobile/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
review_on:
  - host-framework-change
  - routing-model-change
  - provider-composition-change
  - runtime-service-lifecycle-change
  - auth-session-composition-change
  - account-workspace-transition-change
  - marketing-host-boundary-change
---

# Hosts, Composition, and Routing

> **Hosts are outer composition roots.**
>
> They own startup, environment handoff, runtime/service construction, providers, route/navigation trees, shell boundaries, and host lifecycle. Reusable product behavior remains in package owners. Route guards improve UX but do not replace backend authorization.

This document is the canonical owner for:

- web/mobile/marketing host responsibilities;
- application startup/composition;
- runtime/service ownership and disposal;
- provider/bootstrap boundaries;
- host environment handoff;
- routing/navigation ownership;
- auth/session host composition;
- account/workspace navigation lifecycle;
- host error/loading/not-found boundaries;
- web/mobile/marketing differences.

It does not own:

- backend authorization;
- REST contract details;
- query/cache semantics;
- realtime protocol details;
- design-token values;
- product business semantics.

Those concerns are routed to their canonical owners.

---

# 1. Host architecture objective

A host should be understandable as:

```text
environment
        ↓
runtime construction
        ↓
application services
        ↓
providers
        ↓
router/navigation
        ↓
shell/routes/screens
        ↓
product/feature adapters
```

The host composes reusable packages.

It does not become the reusable package itself.

---

# 2. FE-HOST-001 — Apps are composition roots

Current host apps:

```text
apps/web
apps/mobile
apps/marketing
```

Each app owns its framework-specific startup.

Reusable product behavior SHOULD live outside the app when it must be shared or independently owned.

---

# 3. Three-host model

Current technologies:

```text
web
→ Vite + React + TanStack Router

mobile
→ Expo + React Native + Expo Router

marketing
→ Next.js
```

The framework split is intentional.

---

# 4. FE-HOST-002 — Host framework differences remain host-local

Do not build a universal custom framework abstraction solely to hide:

```text
Vite
Expo
Next
TanStack Router
Expo Router
```

Share stable inner contracts.

Keep host framework APIs at outer boundaries.

---

# 5. Web composition root

Current web startup lives in:

```text
apps/web/src/main.tsx
```

Current source demonstrates the intended composition shape:

```text
read runtime environment
        ↓
create web runtime
        ↓
create application services
        ↓
register lifecycle cleanup
        ↓
AppProviders
        ↓
RouterProvider
```

This is current implementation evidence.

The architecture rule is about ownership/lifecycle, not permanent file names.

---

# 6. FE-HOST-003 — Environment is normalized before runtime construction

Host environment input SHOULD be:

```text
read
validate/normalize
then construct runtime
```

Do not let every feature read raw:

```text
import.meta.env
process.env
Expo Constants
```

independently.

Host/runtime config owners provide typed normalized values.

---

# 7. Web environment handoff

Current web composition calls a web runtime-environment reader before `createAppRuntime`.

That pattern keeps raw Vite environment access in the host/config boundary.

---

# 8. FE-HOST-004 — Public frontend environment is not secret storage

Values delivered through:

```text
VITE_*
EXPO_PUBLIC_*
NEXT_PUBLIC_*
```

must be treated as client-visible.

Do not place backend/provider secrets there.

This rule applies regardless of host.

---

# 9. Runtime construction

Runtime packages construct/adapt host-specific reusable mechanisms.

Examples can include:

```text
API transport
storage
telemetry
realtime mechanism
host runtime environment
```

according to current runtime package contracts.

---

# 10. FE-HOST-005 — Runtime construction happens at an outer owner

Do not construct a second independent application runtime inside arbitrary:

```text
route
feature component
product package
```

Long-lived host runtime is normally created once by the composition boundary.

---

# 11. Application services

The host can compose several package-level services into an application-service object/context.

This object is a composition mechanism.

It must not become an untyped service locator.

---

# 12. FE-HOST-006 — Application services are typed composition, not global lookup

Prefer:

```text
explicit properties
typed providers
narrow package contracts
```

over:

```text
services.get("anything")
global registry
string-key service lookup
```

The host may know many services because it is the outer composition root.

Inner packages should not.

---

# 13. Service creator/owner

Current web `main.tsx` creates runtime/application services and disposes them on HMR/pagehide.

Current mobile providers create mobile runtime/application services and dispose them on provider unmount.

These are two framework-specific realizations of the same lifecycle principle.

---

# 14. FE-HOST-007 — The lifecycle owner disposes what it creates

If a composition boundary creates a long-lived disposable service, it MUST have a cleanup path for the corresponding lifecycle.

Examples:

```text
web pagehide
HMR dispose
React provider unmount
native app/session lifecycle
```

as appropriate.

---

# 15. Hidden global singleton risk

A global singleton created during module import can escape host lifecycle.

It can retain:

```text
listeners
sockets
timers
credentials
workspace state
```

after the intended owner changes.

---

# 16. FE-HOST-008 — Avoid import-time runtime startup

Reusable module import SHOULD NOT automatically:

```text
connect realtime
attach global listeners
start interval
create query client
read mutable session
```

unless explicitly owned/documented.

Start through composition.

---

# 17. Web provider composition

Current web `AppProviders` composes concerns including:

```text
global error boundary
application services
web runtime provider
query client
auth provider
theme provider
Work Management services provider
toast surface
```

This is current evidence of host composition breadth.

---

# 18. FE-HOST-009 — Provider tree composes owners; it does not transfer ownership

Because a provider appears near the root does not mean:

```text
app
now owns query semantics
app owns auth business rules
app owns Work Management state
```

The provider exposes services owned by the corresponding package.

---

# 19. Provider ordering

Provider order can be semantically important when one provider consumes another.

Changes require understanding dependencies.

---

# 20. FE-HOST-010 — Provider order follows dependency prerequisites

Do not reorder root providers for aesthetics.

For each provider identify:

```text
what it creates
what it consumes
what lifetime it owns
what children require it
```

Then order explicitly.

---

# 21. Provider cleanup

Providers that create subscriptions/listeners/services should clean them up.

A pure context provider wrapping already-owned service need not duplicate disposal.

---

# 22. FE-HOST-011 — Disposal occurs once at the owning boundary

Avoid:

```text
host disposes service
+
nested provider also disposes same service
```

unless the service contract is explicitly reference-counted/idempotent.

Double-disposal is a lifecycle defect.

---

# 23. Web router ownership

Current web router is created in the app.

It uses TanStack Router with a route tree and application-service context.

Current router also configures global route:

```text
pending
error
not found
scroll restoration
```

behavior.

---

# 24. FE-HOST-012 — Router belongs to the host

Host owns:

```text
route tree
router instance
route context wiring
global route fallback behavior
navigation integration
```

Product core/state does not own the host router.

---

# 25. Route modules

Route modules adapt route parameters/search/navigation to product/feature packages.

They should remain thin composition adapters where possible.

---

# 26. FE-HOST-013 — Routes delegate reusable behavior

A route MAY own:

```text
path/search validation
route loader composition
screen assembly
navigation-specific decisions
```

It SHOULD NOT become the default home for:

```text
query-key definitions
product mutation semantics
realtime state machine
business calculations
```

when those belong to reusable owners.

---

# 27. Generated route trees

If the router/framework generates route-tree artifacts, generated output follows generator rules.

Do not hand-edit generated route artifacts when a producer owns them.

---

# 28. FE-HOST-014 — Route generation source and output remain synchronized

Change source route definitions/config.

Regenerate/check generated route output as required by current tooling.

---

# 29. Route context

Current web router context carries typed application services.

This allows route loaders/guards/components to access host-composed dependencies without importing global singleton instances.

---

# 30. FE-HOST-015 — Router context is typed host composition

Router context SHOULD contain intentionally composed host services/contracts.

It MUST NOT become:

```text
arbitrary mutable application state bag
duplicate query cache
business domain object registry
```

---

# 31. Navigation after service events

Current web runtime composition can inject navigation behavior such as moving to sign-in on signed-out session state.

This is an outer adapter:

```text
runtime/session event
→ host navigation callback
```

---

# 32. FE-HOST-016 — Inner runtime requests navigation through an outward adapter

Runtime/package code should not import the host router solely to perform navigation.

The host can inject a callback/adapter appropriate to the runtime contract.

This preserves inward dependency direction.

---

# 33. Return URL handling

Authentication flows often preserve an intended return route.

Current web source sanitizes internal return URLs before using them in sign-in navigation.

---

# 34. FE-HOST-017 — Redirect/return URLs are untrusted navigation input

Before navigating to a user/provider-controlled redirect target:

```text
validate/sanitize
restrict to allowed internal/approved targets
```

Avoid open redirect behavior.

---

# 35. Route parameter validation

Route/search params are strings/untrusted input at the navigation boundary.

Validate/normalize before product/service use.

---

# 36. FE-HOST-018 — Route IDs are inputs, not authorization

A route containing:

```text
workspaceId
boardId
pageId
```

does not prove access.

The backend still authorizes the requested resource.

Frontend guards only improve navigation UX.

---

# 37. Route guards

Route guards can check known session/workspace/permission state to avoid rendering impossible screens.

They are not the server security boundary.

---

# 38. FE-HOST-019 — Route guard does not replace backend authorization

Never justify a missing server authorization check with:

```text
"the user cannot reach the route"
```

The browser/native client can be modified.

---

# 39. Auth/session host composition

Authentication/session packages provide reusable client session behavior.

The app/runtime boundary composes them into the host lifecycle.

Current web creates the feature auth provider using runtime API/contracts.

---

# 40. FE-HOST-020 — Session composition is outer; session semantics remain package-owned

The host MAY:

```text
construct provider
supply API/runtime dependency
react to signed-out navigation
clear host-scoped state
```

It SHOULD NOT duplicate auth protocol/state logic across routes.

---

# 41. Auth/session transitions

Sign-in/sign-out/session invalidation can affect:

```text
query cache
realtime
workspace selection
routes
persisted client state
```

These transitions must be coordinated.

Detailed state/realtime semantics belong to their topic docs.

---

# 42. FE-HOST-021 — Principal change is a lifecycle boundary

After logout/user change, the old principal's tenant/resource data MUST NOT remain visible as if still valid.

Host composition participates in cleanup/reset according to state/runtime contracts.

---

# 43. Account/workspace navigation

Workspace/account selection affects navigation and server-state scope.

The host can own route selection/current navigation context.

Product/state packages own data semantics.

---

# 44. FE-HOST-022 — Workspace route transition is not merely URL replacement

A Workspace change can require:

```text
route update
query invalidation/clear
realtime resubscription
permission refresh
feature state reset
```

The host coordinates the transition; state/realtime owners define their part.

---

# 45. Stale workspace risk

A URL can change before old scoped services/cache/subscriptions are fully reconciled.

Avoid rendering old Workspace data under a new Workspace route.

---

# 46. FE-HOST-023 — Scope transition must prevent old-scope bleed

As applicable:

```text
suspend rendering
key providers/state by scope
clear/invalidate scoped cache
rebind subscriptions
```

according to state/realtime architecture.

---

# 47. Web default loading

Current web router config provides a global pending component.

Global loading fallback is appropriate for host-level navigation.

Product-specific loading remains screen/feature owned.

---

# 48. FE-HOST-024 — Global fallback is not a substitute for product loading UX

Host pending UI handles route-level transition.

Feature/product components still need correct:

```text
initial loading
refreshing
empty
partial
```

states where applicable.

---

# 49. Web route error

Current router config provides a default route error component.

Host-level route error is a containment boundary.

---

# 50. FE-HOST-025 — Host error boundary contains failures; it does not hide them

Error boundaries SHOULD:

```text
show safe recovery UX
report safe diagnostics
avoid exposing secrets/stacks to users
```

They SHOULD NOT silently swallow product failures and continue with corrupt state.

---

# 51. Global error boundary

Current web provider composition wraps the application in a global error boundary with telemetry/release information.

This is host-level containment/observability.

---

# 52. FE-HOST-026 — Error-boundary telemetry is diagnostic only

Telemetry failure MUST NOT block route/product recovery.

Do not turn observability into product correctness dependency.

---

# 53. Not-found handling

Current web router has a global not-found fallback.

Individual product “resource not found” states may differ from “route does not exist.”

---

# 54. FE-HOST-027 — Route 404 and resource 404 are different concerns

Distinguish:

```text
route pattern missing
→ host router not-found

known route, server resource missing/hidden
→ product/API state
```

Do not collapse security-sensitive resource-not-found semantics into router configuration.

---

# 55. Mobile composition root

Current mobile root layout uses:

```text
MobileAppProviders
→ Expo Router Stack
```

Current stack includes routes such as:

```text
index
sign-in
sign-up
workspaces/[workspaceId]
```

as current implementation evidence.

---

# 56. FE-HOST-028 — Mobile navigation is native-host owned

Expo Router/navigation files belong to the mobile host.

Product core/state MUST NOT depend on Expo Router.

Mobile adapters/screens translate navigation into product behavior.

---

# 57. Mobile providers

Current `MobileAppProviders` creates:

```text
mobile runtime
mobile application services
query client provider
mobile runtime provider
```

and disposes services on provider unmount.

---

# 58. FE-HOST-029 — Mobile runtime/service construction is native-specific but lifecycle-equivalent

The web and mobile implementation do not need identical files/components.

They should preserve equivalent architecture:

```text
normalized env
→ runtime/services
→ providers
→ navigation
→ cleanup
```

---

# 59. Mobile route/screens

Expo Router route modules/screens are host adapters.

Reusable product behavior stays in native-safe packages.

---

# 60. FE-HOST-030 — Mobile route files do not become product state owners

Avoid creating a separate per-screen authoritative state model when product state package already owns the server state.

Screen local interaction state remains local.

---

# 61. Mobile lifecycle

Native apps have lifecycle states beyond browser page lifecycle.

Runtime services may need to react to:

```text
foreground/background
network changes
deep link
credential refresh
```

through approved runtime/host APIs.

---

# 62. FE-HOST-031 — Mobile lifecycle handling stays in host/runtime boundary

Do not add Expo AppState/Linking/secure-store dependencies to product core/state unless a deliberate runtime abstraction permits it.

---

# 63. Deep links

Deep links are untrusted navigation inputs.

Resolve/validate route/resource identifiers.

Backend remains authorization authority.

---

# 64. FE-HOST-032 — Deep link capability does not grant resource capability

Opening:

```text
notrelix://workspace/X/item/Y
```

does not prove the user can access X/Y.

Normal server authorization applies.

---

# 65. Marketing host

Marketing uses Next.js and lives in its own host.

Current source is organized under:

```text
apps/marketing/src/app
components
content
sections
styles
```

as implementation evidence.

---

# 66. FE-HOST-033 — Marketing owns content/SEO/marketing routing

Marketing MAY own:

```text
marketing pages
SEO metadata
public content composition
campaign/navigation content
marketing-only configuration
```

It MUST NOT become a second authenticated product application.

---

# 67. Marketing internal dependencies

Current manifest allows marketing to consume only approved shared visual packages:

```text
ui-tokens
ui-web
ui-icons
```

among internal packages.

---

# 68. FE-HOST-034 — Marketing remains product-runtime isolated

Do not import:

```text
features-auth
runtime-web authenticated services
work-management state
documents state
automation state
```

into marketing to reuse application logic.

Extract visual primitives/content-independent UI if truly shared.

---

# 69. Marketing server capability

Next.js can provide server-rendering/server-only capabilities for marketing.

That does not automatically authorize backend secrets/product mutations inside arbitrary client components.

---

# 70. FE-HOST-035 — Next server/client boundary must remain explicit

If marketing uses server-only secrets/services:

```text
keep them server-only
do not expose through NEXT_PUBLIC_*
do not import into client bundles
```

Marketing security follows repository environment/security standards.

---

# 71. Cross-host shared behavior

Share through inward packages:

```text
contracts
product core/state where safe
tokens
platform-neutral behavior
```

not app-to-app imports.

---

# 72. FE-HOST-036 — Apps do not import each other's internals

Forbidden conceptual edges:

```text
app-mobile → app-web/src/*
app-web → app-mobile/*
app-marketing → app-web/src/*
```

Move shared behavior into an appropriate package.

---

# 73. Host shell

A host shell can own:

```text
navigation chrome
top-level layout
workspace switcher composition
global surfaces
```

It can compose multiple product/feature packages.

---

# 74. FE-HOST-037 — Shell is outer composition, not product authority

A shell component SHOULD NOT own:

```text
Board mutation rules
Billing entitlement calculation
Document conflict resolution
```

Delegate to package owners.

---

# 75. Global surfaces

Examples:

```text
toast
modal root
command palette
global error
global loading
```

can be host-level when they span routes.

Their reusable primitives may belong in UI/feature packages.

---

# 76. FE-HOST-038 — Global surface ownership separates mechanism and semantics

Example:

```text
toast renderer
→ host/UI

"workspace deleted" message decision
→ feature/product outcome handling
```

Do not centralize every product notification decision in the host shell.

---

# 77. Theme composition

Current web provider tree composes a ThemeProvider and runtime storage.

Theme is cross-app/UI concern.

Detailed token/theme architecture belongs to UI docs.

---

# 78. FE-HOST-039 — Host wires theme runtime; UI system owns theme semantics

The app can provide storage/runtime dependency.

Do not define component-specific color policy in host startup.

---

# 79. Query client composition

Current web/mobile hosts provide query clients through host provider composition.

Detailed query policy belongs to state/query architecture.

---

# 80. FE-HOST-040 — One host-scoped query runtime does not mean one global state owner

The host can provide the query mechanism.

Product/feature state packages still own:

```text
keys
fetch/mutation semantics
invalidation
```

according to state architecture.

---

# 81. Realtime lifecycle composition

Web current source has host provider/lifecycle files for realtime.

Host can start/stop/rebind generic realtime according to principal/workspace lifecycle.

Detailed protocol/reconciliation belongs to realtime docs.

---

# 82. FE-HOST-041 — Host coordinates realtime lifecycle; product packages own product reconciliation

Do not implement every event reducer in `apps/web/src/realtime`.

Use product/feature owners.

---

# 83. Provider keyed scope

Some providers/services may need to recreate when principal/account/workspace changes.

Others should remain host-lifetime stable.

---

# 84. FE-HOST-042 — Service lifetime matches scope

Classify service lifetime:

```text
host lifetime
principal lifetime
account lifetime
workspace lifetime
screen/route lifetime
```

Do not keep Workspace-scoped service alive across Workspace change unless it supports explicit rebind safely.

---

# 85. Host state versus product state

Host state examples:

```text
current route
navigation transition
runtime initialization
global theme
host error boundary
```

Product state examples:

```text
Board
Document
Automation
Workspace data
```

Keep owners distinct.

---

# 86. FE-HOST-043 — Host state does not absorb server resource state

Do not create a giant `AppContext` containing:

```text
all Boards
all Pages
Billing
Permissions
Notifications
```

because providers are convenient.

Use package state/query owners.

---

# 87. Route search state

Search/query-string state can be host navigation state.

Schemas/validation belong near route host boundary or reusable feature where appropriate.

Current web router exports route search schemas as current evidence.

---

# 88. FE-HOST-044 — Shareable navigation state prefers URL where product UX requires it

Examples:

```text
selected tab
filter
search query
view mode
```

can belong in route/search state when deep-link/share/back-forward semantics matter.

Do not duplicate it in global store without reason.

---

# 89. Route loader/data fetching

If router supports loaders, loaders should orchestrate approved package APIs/state.

They should not become a second data-access architecture.

---

# 90. FE-HOST-045 — Route loader delegates to state/contracts

Do not hand-build one-off fetch clients inside route files when canonical API/query owners exist.

---

# 91. Route code splitting

Host router can own lazy route composition.

Product packages can expose lazy-friendly entrypoints where needed.

Performance choices should preserve boundaries.

---

# 92. FE-HOST-046 — Lazy loading does not change semantic ownership

A lazily imported feature remains feature-owned.

Do not move code into host route solely for chunking.

---

# 93. Navigation permissions

Known permissions can influence whether a nav item is visible/enabled.

Backend still authorizes requests.

---

# 94. FE-HOST-047 — Navigation visibility is UX, not security

Treat permission-driven menus as:

```text
discoverability/UX
```

not proof that hidden resources are protected.

---

# 95. Authentication routes

Sign-in/sign-up/password flows can be host routes using auth feature behavior.

Keep auth feature protocols reusable where appropriate.

---

# 96. FE-HOST-048 — Auth route component is adapter around auth feature

Do not duplicate token/session logic separately in each route.

Host owns navigation/presentation integration.

Feature/runtime owns auth client behavior.

---

# 97. Post-auth navigation

After authentication, navigation can use an approved return target/default workspace route.

Sanitize external input.

Resolve authoritative workspace availability through server state.

---

# 98. FE-HOST-049 — Post-auth destination is not trusted client memory

Do not navigate into a Workspace/resource solely because it was previously stored without confirming current session/server access as required.

---

# 99. Logout navigation

Logout usually returns to a public/auth route after state cleanup.

Ordering matters.

---

# 100. FE-HOST-050 — Clear sensitive scoped state before exposing new principal/public shell

Avoid visible flashes of old tenant data after logout.

Coordinate state/runtime teardown and navigation.

---

# 101. Host error recovery

A recovery action can:

```text
retry
reload route
navigate safe home
sign out on invalid session
```

according to error classification.

Do not treat every error as full-page reload.

---

# 102. FE-HOST-051 — Recovery matches failure scope

Examples:

```text
route component failure
→ route retry

session invalid
→ session recovery/sign-in

workspace removed
→ workspace selection

runtime initialization fatal
→ host fatal boundary
```

Keep recovery behavior intentional.

---

# 103. Environment failure

Missing/invalid required runtime environment should fail clearly before partial app startup when possible.

Do not let every API call fail later with misleading network errors.

---

# 104. FE-HOST-052 — Critical host configuration validates early

For required public runtime config:

```text
parse
validate
fail with safe diagnostic
```

at host/runtime bootstrap.

Optional provider configuration may degrade according to approved policy.

---

# 105. Runtime initialization failure

If runtime cannot initialize safely, host should show a bounded fatal/degraded state.

Do not render product routes with unusable service placeholders.

---

# 106. FE-HOST-053 — Partially initialized runtime is not normal application state

Either:

```text
runtime ready
or
explicit degraded/failure contract
```

Do not pass undefined critical services deep into the tree and let consumers discover failure randomly.

---

# 107. Service replacement

Tests/storybook/dev may inject alternate service implementations where architecture allows.

Composition root is the correct seam.

---

# 108. FE-HOST-054 — Test substitution happens at composition contracts

Prefer injecting:

```text
fake API/runtime/service
```

through typed boundaries rather than monkey-patching package internals.

Production graph tests should state substitutions accurately.

---

# 109. Host-specific tests

Host changes should be tested at the cheapest relevant seam:

```text
web provider/router unit/integration
mobile provider/navigation tests
marketing route/build tests
E2E for cross-boundary user flow
```

Use testing architecture for exact gate requirements.

---

# 110. FE-HOST-055 — One host test does not prove another host

A green web test does not prove mobile.

A marketing build does not prove authenticated web.

Apply host-specific evidence.

---

# 111. Web host build

Changes to:

```text
Vite config
route tree
provider composition
runtime environment
web adapters
```

can require web build proof.

---

# 112. FE-HOST-056 — Build proves packaging, not user-flow correctness

A web build can prove compile/bundle integration.

It does not prove:

```text
auth flow
workspace switch
realtime reconnect
accessibility
```

unless those are separately executed.

---

# 113. Mobile host build/export

Native-safe dependency graph and Expo build/export are separate proof categories.

Architecture checks protect forbidden web dependencies.

Build/export protects bundling/native host integration.

---

# 114. FE-HOST-057 — Mobile architecture gate and mobile build are complementary

Do not remove one because the other passes.

A bundle can sometimes include conceptually wrong dependencies; an architecture gate can miss runtime packaging defects.

---

# 115. Marketing host build

Next build protects marketing route/server/client composition.

Packaging smoke and runtime smoke are distinct.

---

# 116. FE-HOST-058 — Marketing build does not prove deployment health

If CI/job calls something “smoke,” it should actually start/probe runtime or be named packaging/build.

Do not overclaim evidence.

---

# 117. Host-to-backend contract

All hosts consuming backend APIs use approved contract/runtime mechanisms.

Web/mobile can have different credential/storage mechanisms.

Backend API semantics remain shared.

---

# 118. FE-HOST-059 — Host transport differences do not fork product mutation meaning

Example:

```text
web and mobile may attach credentials differently
```

but:

```text
MoveBoardItem
```

has one backend operation meaning.

---

# 119. Browser-specific security

Web host must respect browser concerns such as:

```text
cookie credentials
CSRF contract
same-origin/cross-origin behavior
redirect safety
XSS
```

according to backend/security architecture.

---

# 120. FE-HOST-060 — Browser security mechanism belongs to web host/runtime boundary

Do not burden product core with:

```text
CSRF cookie reading
window location
browser header policy
```

unless exposed through neutral contract.

---

# 121. Mobile credential handling

Mobile may use native storage/session mechanisms.

Keep native secret/token storage inside approved runtime/auth boundaries.

Do not expose credentials to product packages.

---

# 122. FE-HOST-061 — Credential storage is runtime/session concern

Product state asks for authenticated API capability.

It should not know where/how the token is stored.

---

# 123. Marketing credentials

Marketing should usually not share authenticated product session runtime.

Public calls/server-side marketing services follow separate contract/security rules.

---

# 124. FE-HOST-062 — Marketing does not reuse browser app session by default

If a marketing/auth bridge is needed, define an explicit bounded contract rather than importing app-web auth providers.

---

# 125. Host observability

Hosts can attach:

```text
release identity
route
runtime initialization
global error
```

to observability.

Product telemetry events can originate from feature/product owners.

---

# 126. FE-HOST-063 — Host observability enriches; it does not centralize all product analytics semantics

Keep product event meaning in its owning product/analytics contract.

---

# 127. Browser page lifecycle

Web must handle:

```text
pagehide
HMR
visibility/network as applicable
```

through runtime/host lifecycle.

Current `main.tsx` explicitly disposes on HMR/pagehide.

---

# 128. FE-HOST-064 — Development HMR must not leak long-lived services

When HMR replaces composition modules, old:

```text
listeners
sockets
timers
```

must be disposed where services are recreated.

---

# 129. StrictMode

React StrictMode can expose side-effect/lifecycle assumptions during development.

Services should not be created repeatedly by render unintentionally.

---

# 130. FE-HOST-065 — Long-lived service creation is outside unstable render paths or memoized/owned deliberately

Current web creates services before root render.

Current mobile memoizes service creation in root provider.

Both avoid recreating services on ordinary child render.

---

# 131. Provider render purity

Provider render should not perform uncontrolled side effects each render.

Use lifecycle hooks/owned factories.

---

# 132. FE-HOST-066 — Construction, render and side-effect phases remain explicit

Do not hide:

```text
network connect
storage mutation
event registration
```

inside arbitrary render expressions.

---

# 133. Host composition evolution

Adding/removing providers/services can alter lifecycle/ordering across the app.

Treat broad root changes carefully.

---

# 134. FE-HOST-067 — Root provider change has high fan-out

Review:

```text
dependency prerequisites
lifecycle/disposal
principal/workspace scope
test substitution
error behavior
host build
```

before modifying composition order.

---

# 135. Adding a route

A route addition should identify:

```text
host
path/navigation semantics
product/feature owner
required session/permission UX
state owner
loading/error
tests
```

It should not require new product architecture by default.

---

# 136. FE-HOST-068 — Route count growth does not imply route-layer business ownership

Keep route modules compositional as the product grows.

If route files accumulate reusable business logic, extract to owners.

---

# 137. Adding a mobile screen

Use Expo Router/native host patterns.

Consume native-safe feature/product adapters.

Do not port the web route component directly if it carries DOM/web dependencies.

---

# 138. FE-HOST-069 — Mobile parity is semantic, not route-file parity

The same capability may use different route hierarchy/navigation patterns on mobile.

Backend operation and product meaning remain shared.

---

# 139. Adding a marketing page

Marketing pages can use marketing content/sections/shared UI.

Do not import product runtime to render marketing examples.

Use static/mock marketing representations when appropriate.

---

# 140. FE-HOST-070 — Marketing demo visuals are not production product state

A landing-page screenshot/demo component MAY represent product concepts visually without connecting to authenticated state.

Do not confuse demo data with product state owner.

---

# 141. Account switch

Account switch can affect Workspace inventory, billing, integrations, governance, query cache, realtime.

The host coordinates navigation/lifecycle.

Detailed data policies remain in state/realtime docs.

---

# 142. FE-HOST-071 — Account switch is a principal-scope transition

Do not keep child Workspace/resource state from the old Account active under the new Account.

---

# 143. Workspace switch

Workspace switch is narrower than Account switch but still broad.

It can affect:

```text
routes
queries
permissions
realtime
feature state
```

---

# 144. FE-HOST-072 — Workspace switch has one coordinated transition contract

Avoid each route/component independently detecting Workspace change and performing unrelated cleanup.

Use shared host/state/runtime lifecycle owners.

---

# 145. Feature flag/entitlement routing

A route can hide/redirect if a feature is unavailable.

That is UX.

Backend still validates entitlements/authorization.

---

# 146. FE-HOST-073 — Route availability does not replace server feature/entitlement enforcement

Do not expose protected operation solely through route control.

---

# 147. Public routes

Public/authenticated routes can coexist.

Host routing owns route classification.

Security still belongs to backend/auth contracts.

---

# 148. FE-HOST-074 — Public route does not automatically mean public backend data

A public page can call no backend, public backend capability, or authenticated capability depending on contract.

Do not infer data authorization from route visibility.

---

# 149. Share routes

Share/public-token route may have bounded resource capability.

Keep its provider/dependency graph bounded where practical.

---

# 150. FE-HOST-075 — Share route does not bootstrap full authenticated Workspace authority by default

Use the minimum package/state/runtime required for the share capability.

Backend enforces capability scope.

---

# 151. Host composition and microfrontends

Current apps are composition roots in a monorepo, not independent product microfrontends per package.

Do not introduce runtime federation solely due package modularity.

---

# 152. FE-HOST-076 — Host composition remains explicit static dependency composition by default

A move to:

```text
microfrontend/runtime federation
```

would be a consequential architecture decision requiring a new design/ADR.

---

# 153. Host replacement/evolution

A future host framework change should preserve inner package contracts where practical.

Do not speculate abstractions prematurely.

---

# 154. FE-HOST-077 — Replaceability is achieved through correct boundaries, not universal wrappers

Keep:

```text
router framework
host env
DOM/native APIs
```

outward.

Then product core/state can survive host evolution without wrapping every framework API today.

---

# 155. Architecture drift

If host source evolves beyond this architecture, classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not automatically treat current app code as new precedent.

---

# 156. FE-HOST-078 — Composition drift requires ownership review

A route/provider importing product internals can be source debt.

A deliberate host model change can require docs/ADR.

Classify before repair.

---

# 157. Related FE ADRs

Host architecture historical decisions include:

```text
FE-ADR-001 — framework split
FE-ADR-005 — auth session model
```

Check registry/status.

---

# 158. FE-HOST-079 — ADR rationale is historical; this file describes current desired host architecture

Do not rewrite an Accepted ADR simply because host implementation evolves.

Supersede consequential decisions correctly.

---

# 159. Host change checklist

Before changing root composition:

```text
[ ] host owner
[ ] environment impact
[ ] runtime/service construction
[ ] provider prerequisites/order
[ ] service lifecycle/disposal
[ ] auth/session impact
[ ] account/workspace scope
[ ] router/navigation impact
[ ] query/realtime coordination
[ ] error/loading fallback
[ ] host tests
[ ] host build/E2E as required
```

---

# 160. Route change checklist

```text
[ ] route/search schema
[ ] untrusted params validated
[ ] product/feature owner
[ ] server contract
[ ] route guard is UX only
[ ] loading/error/not-found
[ ] return URL/redirect safety
[ ] query/realtime state owner
[ ] tests
```

---

# 161. Provider change checklist

```text
[ ] what provider creates
[ ] dependencies consumed
[ ] provider order
[ ] lifecycle scope
[ ] cleanup owner
[ ] principal/workspace transition behavior
[ ] test seam
[ ] duplicate service/global state risk
```

---

# 162. Mobile host checklist

```text
[ ] Expo/native owner
[ ] no web/DOM package
[ ] mobile runtime
[ ] native UI
[ ] deep-link validation
[ ] lifecycle/background behavior
[ ] secure credential boundary
[ ] mobile tests/build
```

---

# 163. Marketing host checklist

```text
[ ] marketing-only responsibility
[ ] shared UI dependency allowed
[ ] no authenticated product-state dependency
[ ] Next server/client boundary explicit
[ ] no secret in NEXT_PUBLIC
[ ] SEO/content ownership
[ ] build/deployment evidence
```

---

# 164. Stop conditions

Stop before implementation if:

- a route guard is being used as the only authorization control;
- a host route is becoming the reusable product-state owner;
- product core/state needs TanStack Router or Expo Router;
- mobile composition requires web runtime/UI/DOM;
- marketing requires authenticated product state;
- long-lived services have no clear owner/disposal;
- a principal/workspace switch can leave old scoped state/subscriptions alive with no contract;
- runtime is being created repeatedly inside ordinary component render;
- an app imports another app's internals;
- a global service locator is being introduced;
- a raw external return URL is used for navigation;
- a host framework change is being treated as routine refactor without architecture review.

---

# 165. Executable evidence

Current host evidence includes:

```text
frontend/apps/web/src/main.tsx
frontend/apps/web/src/providers/
frontend/apps/web/src/router/
frontend/apps/mobile/app/
frontend/apps/mobile/src/providers/
frontend/apps/marketing/src/
frontend/packages/runtimes/web/
frontend/packages/runtimes/mobile/
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Use current source for implementation facts.

Use this document for durable host/composition rules.

---

# 166. Related architecture

Read:

```text
frontend-overview.md
dependency-boundaries.md
api-and-contracts.md
state-query-mutations.md
realtime.md
ui-and-design-system.md
testing-and-quality-gates.md
architecture-change-policy.md
```

Host docs intentionally route detailed state/realtime/UI semantics outward.

---

# 167. Explicit non-responsibilities

This document does not define:

```text
backend resource authorization
exact API DTOs
exact query keys
realtime ordering/gap algorithm
design token values
product lifecycle/business rules
```

It defines how hosts construct and compose the client around those owners.

---

# 168. Final host model

The three hosts should remain understandable as:

```text
WEB
Vite env
  ↓
web runtime/services
  ↓
web providers
  ↓
TanStack Router
  ↓
routes/shell
  ↓
web product adapters/features

MOBILE
Expo env
  ↓
mobile runtime/services
  ↓
mobile providers
  ↓
Expo Router
  ↓
screens
  ↓
mobile product adapters

MARKETING
Next runtime
  ↓
marketing app/content/SEO
  ↓
shared approved UI
```

with no app-to-app dependency.

The host layer succeeds when framework/runtime complexity stays outward, product/state ownership stays reusable, lifecycle cleanup is explicit, navigation input remains untrusted, and server authorization remains authoritative.
