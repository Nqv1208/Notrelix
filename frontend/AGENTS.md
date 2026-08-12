---
document_id: FE-AGENTS
document_type: agent-contract
status: active
owner: frontend-platform
applies_to:
  - frontend
  - coding-agents
  - frontend-contributors
evidence:
  - frontend/README.md
  - frontend/docs/README.md
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/package.json
  - .github/workflows/fe-ci.yml
review_on:
  - frontend-agent-contract-change
  - frontend-authority-routing-change
  - frontend-architecture-change
  - frontend-validation-model-change
---

# Frontend Execution Contract

This file defines how a coding agent or contributor must reason before editing the Notrelix frontend.

It is an **execution contract**.

It is not:

```text
a second architecture handbook
a feature specification
a package inventory
a migration tracker
a prompt scratchpad
```

Read root governance first.

Then use this file to identify the correct frontend owner, required evidence, and stop conditions.

---

# 1. Required reading order

Before editing frontend source, read:

```text
1. /AGENTS.md
2. /RULE.md
3. /frontend/README.md
4. /frontend/AGENTS.md
5. /frontend/docs/README.md
6. canonical architecture doc for the changed concern
7. product/context docs when product semantics matter
8. relevant FE-ADR when historical rationale matters
9. executable source/tests/manifests
```

Do not infer architecture from one nearby source file.

---

# 2. Start from the change, not the file

Before coding, state:

```text
What behavior is changing?
Who owns it?
Which host(s) consume it?
Which server contract/state does it depend on?
Which package should own the reusable behavior?
What architecture rule protects that owner?
How will the change be proven?
```

Do not begin with:

```text
"I found this component, so I will implement everything here."
```

---

# 3. Owner classification

Classify the requested change into one or more:

```text
host composition
foundation
runtime
UI
product capability
cross-product feature
tooling/generator
generated contract
testing
documentation
```

Then route to the canonical owner.

---

# 4. Host classification

Determine impact on:

```text
web
mobile
marketing
```

Do not assume a web-only implementation is acceptable for a product capability intended to support mobile.

Do not force marketing into product runtime architecture.

---

# 5. Product/context classification

If the change affects product meaning, identify the owning product context.

Examples:

```text
Workspace membership
→ Workspaces/Governance

Board/Item
→ Work Management

Page/Block
→ Documents

comments/presence
→ Collaboration

workflow automation
→ Automation

plan/entitlement
→ Billing
```

Frontend packages consume product semantics.

They do not create new backend/domain truth independently.

---

# 6. Backend contract classification

For server-backed behavior identify:

```text
REST endpoint/operation
request/response contract
error contract
authorization expectation
realtime event
idempotency/concurrency expectation
pagination/filter/sort
version compatibility
```

Do not implement a client workflow around an assumed backend endpoint/event that does not exist.

---

# 7. Backend-authoritative rule

Server state remains backend-authoritative.

An agent MUST NOT solve a missing backend contract by creating a permanent client-only source of truth.

If the feature requires a backend decision/contract that is absent:

```text
stop
record unresolved dependency
do not invent backend semantics
```

---

# 8. Package architecture authority

The executable dependency authority is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Do not override it by personal preference.

If the manifest conflicts with intended architecture:

```text
classify the conflict
change architecture deliberately
do not merely expand allow-list
```

---

# 9. Closed-world package rule

Every governed source-bearing app/package must belong to the architecture manifest according to tooling rules.

Before creating/moving a package:

```text
check workspace discovery
check architecture manifest
check package exports
check generator/docs
check tests
```

No shadow packages.

---

# 10. Apps compose

Apps own host composition.

Reusable product behavior belongs in owning packages.

Do not place reusable behavior in:

```text
apps/web/src/routes
apps/web/src/providers
apps/mobile app shell
marketing page component
```

just because it is initially used there.

---

# 11. Foundation rule

Foundation remains product-agnostic unless a package is explicitly defined otherwise.

Before adding a symbol to foundation ask:

```text
Is this a generic client mechanism?
or
Is this actually Work Management/Documents/Billing/etc. behavior?
```

If it knows product-specific concepts, use the product/feature owner.

---

# 12. Runtime rule

Host-specific construction belongs in:

```text
runtime package
or
host app
```

according to scope.

Do not leak:

```text
window
document
localStorage
React Native APIs
Expo APIs
Next APIs
```

into runtime-neutral packages.

---

# 13. UI rule

Separate:

```text
tokens
web UI
mobile UI
icons
```

according to architecture.

Do not create a universal component implementation by wrapping DOM for mobile.

Share semantic tokens/contracts where appropriate.

---

# 14. Product package rule

Dedicated product packages own reusable product behavior.

Keep product semantics in:

```text
core/state/collaboration/plugin
```

as current family architecture defines.

Keep platform rendering/runtime concerns in adapters.

Do not add package layers just because another product family has them.

---

# 15. Feature package rule

Feature packages own cross-product vertical client features.

They must maintain least-privilege dependencies.

Do not turn a feature package into:

```text
"imports everything because the page needs everything"
```

If dependency pressure grows, reassess ownership.

---

# 16. Public exports

Cross-package imports must use supported public exports.

Do not deep import.

Before exporting new internal behavior, decide whether it is a stable package contract.

Do not expose internals merely to make one import compile.

---

# 17. Deep-import stop condition

If the easiest solution is:

```ts
import x from "@notrelix/pkg/src/internal/...";
```

stop.

Choose:

```text
move code
add legitimate public export
change owner
or
change architecture
```

Do not bypass the boundary.

---

# 18. Internal dependency rule

An import is allowed only when the executable architecture permits it.

If a dependency is not allowed:

```text
do not add it to manifest first
```

First answer:

```text
Should this package really own/consume this behavior?
```

Manifest change follows architecture decision.

---

# 19. No generic module registry by default

Do not invent:

```text
global ModuleRegistry
ServiceLocator
feature reflection discovery
dependency container
untyped plugin bag
```

to avoid explicit imports.

Use explicit composition unless a concrete need justifies new architecture.

---

# 20. Web rule

Web app-specific concerns stay in web host/runtime/adapters.

Do not leak web routing/browser APIs into product core.

Reusable state should be host-independent where the product contract allows it.

---

# 21. Mobile rule

Production mobile package graph must remain native-safe.

Before committing mobile-related work check for:

```text
react-dom
react-dom/*
DOM JSX tags
window/document
@notrelix/ui-web
@notrelix/runtime-web
web app internals
Next.js
```

If any appears, stop and reassess boundary.

---

# 22. Marketing rule

Marketing stays isolated.

Do not import authenticated application/product state into marketing for convenience.

Promote shared visual primitives to an approved UI owner instead.

Marketing-specific server/SEO/content behavior remains marketing-host owned.

---

# 23. Framework rule

Do not normalize Vite/Expo/Next into a custom universal framework layer without a concrete architecture problem.

The host split is intentional.

Use each framework where the host owns it.

---

# 24. Generated contracts

Generated contract code is not hand-authored.

If generated API/realtime type is wrong:

```text
inspect backend/source contract
inspect generator
change producer/input
regenerate
```

Do not patch generated output and leave producer unchanged.

---

# 25. REST contract work

Before modifying REST consumption identify:

```text
operation
version
request type
response type
error type
auth/session mode
idempotency/concurrency semantics
pagination/filter/sort
```

Use the API-and-contracts canonical doc.

---

# 26. Handwritten DTO rule

Do not duplicate backend wire DTOs across feature packages by default.

Use generated contracts.

Map to a client semantic model only when that separation is intentional.

Document the mapping owner.

---

# 27. Error handling

Use stable error category/code/contracts.

Do not branch business UX on arbitrary server error prose.

If server lacks a stable machine-readable distinction the client requires:

```text
record contract gap
do not string-match permanent behavior
```

---

# 28. Server state classification

For every new state variable decide whether it is:

```text
server state
URL/navigation
local UI
form draft
runtime/session
persisted preference
```

Do not create a global store until this classification is clear.

---

# 29. Query ownership

Product/feature query keys belong near their state owner.

Generic query mechanics belong in query foundation.

Do not create a global registry containing every resource's keys by convenience.

---

# 30. Query key scope

Server-state keys must include sufficient scope to prevent cross-context reuse.

As applicable include:

```text
Account
Workspace
resource identity
filters/page/sort
```

according to canonical state architecture.

Do not cache Workspace A under a key that can be reused in Workspace B.

---

# 31. Mutation rule

For every mutation define:

```text
request
authoritative result
cache effect
optimistic behavior if any
rollback
conflict
realtime interaction
navigation effect
```

Do not implement only the network call.

---

# 32. Optimistic update rule

Use optimistic updates deliberately.

Required before using:

```text
predictable operation
reversible local effect
clear failure UX
conflict strategy
server reconciliation
```

Do not use optimistic success to hide uncertain provider/server outcomes.

---

# 33. Idempotency/concurrency

If backend operation uses:

```text
idempotency key
expected version
ETag/version
conflict result
```

client behavior must preserve the contract.

Do not blindly retry non-idempotent mutation.

---

# 34. Realtime classification

Identify whether change touches:

```text
connection
authentication
subscription
event normalization
reconciliation
gap/reconnect
product state
presence/collaboration
```

Do not put product event semantics into generic transport.

---

# 35. Duplicate realtime events

Assume at-least-once/duplicate delivery where backend contract allows it.

Reconciliation should be idempotent.

Do not append blindly if an event can be received twice.

---

# 36. Out-of-order realtime events

Do not assume arrival order equals authoritative resource order/version.

Use version/order/reload strategy defined by contract.

If contract lacks enough information:

```text
record backend/realtime contract gap
```

rather than invent sequence semantics.

---

# 37. Reconnect

Reconnect requires:

```text
re-authentication/re-establishment as needed
subscription restoration
gap detection/revalidation
query reconciliation
```

A socket connected state alone does not mean client state is current.

---

# 38. Workspace switch

Treat Workspace switch as a coordinated state boundary.

Review:

```text
route
query cache
realtime subscriptions
permissions
feature state
product state
pending mutation
```

Do not leave stale Workspace data visible.

---

# 39. Session transition

Login/logout/token refresh/account switch can affect:

```text
query cache
realtime
runtime credential
route
persisted state
```

Do not update only the header/token variable.

---

# 40. Authorization UX

Frontend can display/hide based on known permission data.

Do not treat hidden button as security.

Requests still go through backend authorization.

Do not duplicate the complete Governance permission engine in the client.

---

# 41. Entitlement UX

Plan/feature availability can affect UX.

Entitlement is separate from authorization.

Do not infer:

```text
paid plan → user is authorized
```

or:

```text
workspace admin → feature is entitled
```

---

# 42. Route authorization

Route guards provide UX/navigation enforcement.

Backend remains security authority.

A route guard can prevent obviously invalid navigation.

It must not be the only protection for server data/effects.

---

# 43. Resource scope

Do not trust route/body identifiers as tenant proof.

The client may send them as request inputs.

Backend resolves ownership/authorization.

Avoid client logic that assumes:

```text
because route includes Workspace X,
every loaded resource belongs to X
```

without backend contract.

---

# 44. UI primitive ownership

Before adding a reusable component determine:

```text
generic UI primitive?
product-specific presentation?
feature-specific component?
host shell?
```

Do not put a Board-specific component into `ui-web`.

---

# 45. Tokens first where semantic

For shared visual decisions use design tokens when appropriate.

Avoid hard-coded values duplicated across components when a semantic token exists.

Do not invent token for every one-off pixel.

---

# 46. Web/mobile UI parity

Preserve shared product semantics.

Platform interaction may differ.

Do not make component API identical at the cost of awkward/native-unsafe implementation.

---

# 47. Accessibility acceptance

For interactive UI consider:

```text
semantic element/role
keyboard
focus
label
disabled state
error association
screen-reader announcement
contrast
reduced motion
touch target
```

as applicable.

Do not defer basic accessibility as “polish”.

---

# 48. Loading states

Server-backed UI should not assume immediate data.

Define loading/skeleton/progressive state according to UX.

Avoid layout jumps and controls that become active before required data/permission exists.

---

# 49. Empty states

Empty is not necessarily error.

Distinguish:

```text
no data
no permission
not found
filtered to zero
loading
offline
```

where product UX needs it.

---

# 50. Error states

Provide recoverable action when possible.

Do not expose raw stack/provider error.

Use safe normalized client error handling.

---

# 51. Conflict states

For concurrency/conflict use explicit product UX.

Do not silently overwrite new server data because optimistic local state is older.

---

# 52. Offline/degraded state

If capability requires online server authority, show honest degraded/offline behavior.

Do not fake completion and lose the operation.

If offline queueing is supported, it needs explicit semantics.

---

# 53. Client storage

Classify stored data by:

```text
sensitivity
tenant scope
expiry
logout cleanup
account/workspace transition
host mechanism
```

Do not persist tokens/private server payloads casually.

---

# 54. Environment variables

Frontend public env values are public.

Never put secrets into:

```text
VITE_*
EXPO_PUBLIC_*
NEXT_PUBLIC_*
```

If a secret is required, the operation belongs on a trusted server boundary.

---

# 55. Security logging

Do not log:

```text
access token
refresh token
API key
password
private document body
full sensitive response
```

to console/telemetry.

Use safe IDs/categories.

---

# 56. HTML/rich content

Do not bypass React escaping casually.

For rich content use the approved editor/rendering/sanitization path.

Treat user/provider HTML as untrusted.

---

# 57. Dependency installation

Add dependencies to the narrowest owning package.

Do not add package at frontend root only to make import resolution easy.

Check:

```text
web/mobile compatibility
bundle size
license/security
existing package capability
architecture manifest
```

---

# 58. Framework dependency

Do not add framework dependency to a pure/core package unless its architecture is changing deliberately.

Examples:

```text
Next.js in foundation
React DOM in mobile
Expo in product core
```

are boundary red flags.

---

# 59. Package creation

A new package requires a real ownership/dependency reason.

Do not split every folder into a package.

If created:

```text
workspace discovery
package.json
exports
architecture manifest
allowed imports
tests
generated docs
```

must align.

---

# 60. Package removal

Before removing package:

```text
consumer inventory
public exports
generated manifest
tests
docs
migration
```

must be handled.

Do not leave stale architecture-manifest entry.

---

# 61. Package rename

Package names are internal contracts.

Rename requires:

```text
package manifest
imports
manifest
tooling
generated docs
CI/test filters if any
docs
```

Do not leave aliases indefinitely without a migration reason.

---

# 62. Move across architecture layer

Moving:

```text
feature → foundation
product-state → app
runtime → product-core
```

is an architecture change, not just refactor.

Evaluate dependency direction and semantic owner.

---

# 63. Public export expansion

Every export increases package contract surface.

Prefer narrow public API.

Do not export internal implementation object just because a test/consumer wants it.

Use proper test seam or abstraction.

---

# 64. Test placement

Choose by protected property.

Examples:

```text
pure package behavior
→ node unit

web component interaction
→ web test

cross-package integration
→ integration

native behavior
→ mobile

generator/architecture tooling
→ generator/tooling tests

user production flow
→ E2E
```

Use canonical testing doc.

---

# 65. Non-zero test evidence

Required guarded suites must prove they ran intended tests.

Do not remove count/category guards to make an empty suite green.

When renaming/reorganizing critical tests, update the guard intentionally.

---

# 66. Generator tests

Changes to:

```text
architecture manifest parser/checker
package generator
contract generator
generated docs
```

require generator/tooling proof.

Do not rely only on typecheck.

---

# 67. Architecture tests

For package/import rule change run:

```bash
pnpm check:architecture
pnpm check:architecture-docs
```

plus generator tests when tooling changes.

Do not weaken manifest allow-list merely to pass one feature import.

---

# 68. Codegen tests

Backend REST/realtime contract change requires:

```bash
pnpm codegen:check
```

and affected client tests.

If output changes, review semantic compatibility.

Do not treat regeneration alone as compatibility approval.

---

# 69. Typecheck

Use:

```bash
pnpm typecheck
```

for affected broad changes.

Package-scoped typecheck is useful during iteration.

Report scope accurately.

---

# 70. Lint/format

Run relevant lint/format checks.

Do not report:

```text
frontend verified
```

because formatting passed.

Keep evidence categories distinct.

---

# 71. UI tests

UI foundation changes can require:

```text
Storybook
accessibility Playwright
visual regression
component tests
```

depending on protected property.

Do not update visual snapshot blindly.

Review intended visual change.

---

# 72. E2E

Use E2E for production user-flow contracts.

Do not use E2E to compensate for missing package-level tests.

A failure should be diagnosable to a smaller seam when possible.

---

# 73. Build proof

Run affected host build when:

```text
host composition
framework/build config
public env
package bundling
routing
code splitting
```

changes.

Build success does not replace runtime/E2E.

---

# 74. Mobile build proof

Mobile changes may require Expo export/build-related proof in addition to unit tests.

A web build does not prove native safety.

---

# 75. Marketing build proof

Marketing host changes require its own Next build where appropriate.

Do not assume web app build proves marketing.

---

# 76. CI evidence

Current frontend CI is split across multiple required jobs.

Do not claim full CI pass from one job.

Use exact revision results.

---

# 77. Change classification

Before broad work classify affected dimensions:

```text
contract
architecture
state/realtime
UI
runtime
security
mobile
marketing
tooling
generated artifacts
```

Validation obligations accumulate.

A change can require multiple proof categories.

---

# 78. Architecture decision trigger

Consider FE ADR when changing durable foundations such as:

```text
framework split
package manager
package export model
reusable-package framework boundary
auth/session model
package architecture foundation
runtime ownership foundation
```

Routine feature work following current architecture does not need a new ADR.

---

# 79. Architecture-change policy

If a source change conflicts with canonical frontend architecture:

```text
do not "fix docs later"
```

Use:

```text
frontend/docs/architecture/architecture-change-policy.md
```

and repository decision governance.

---

# 80. Source-versus-doc conflict

Classify conflict before editing:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not automatically assume source wins.

Source is current evidence.

It is not automatic precedent.

---

# 81. Legacy docs

Do not restore deleted/retired authority such as:

```text
frontend/ARCHITECTURE.md
frontend/RULES.md
frontend/MIGRATION_TRACKER.md
freeze roadmap as architecture
active audit report as architecture
```

Migrate unique knowledge to the canonical owner.

Then retire the legacy artifact.

---

# 82. Generated docs

Never hand-edit:

```text
frontend/docs/generated/package-boundaries.md
generated backend contract code
other explicitly generated outputs
```

unless the generator contract explicitly allows hand-maintained regions.

Current generated package-boundary docs do not.

---

# 83. Documentation update trigger

Update canonical frontend docs when the actual durable architecture changes.

Do not update architecture docs for every internal refactor that preserves the same contract.

Update generated docs through their producer.

---

# 84. Documentation link discipline

Route to one owner.

Do not paste the same package matrix/rule text into:

```text
README
AGENTS
architecture docs
decision registry
package README
```

Use links/references.

---

# 85. Feature implementation workflow

Before implementation:

```text
1. locate product/feature owner
2. inspect backend contract
3. identify package/host impact
4. inspect manifest
5. inspect canonical architecture
6. write/confirm acceptance behavior
7. choose test seams
```

During implementation:

```text
8. keep changes inside owners
9. update exports/dependencies deliberately
10. regenerate generated artifacts
11. run focused tests
```

Before completion:

```text
12. run required broader gates
13. inspect generated diffs
14. report evidence
15. list unresolved external dependency honestly
```

---

# 86. New screen workflow

For a new web/mobile screen:

```text
route/navigation owner
product/feature package
server state owner
loading/error/empty
permission
mutation
realtime
responsive/native behavior
a11y
tests
```

The screen component itself should primarily compose these owners.

---

# 87. New API operation workflow

For consuming a new backend operation:

```text
backend contract exists
codegen updated
typed client operation
state owner
query/mutation key
error mapping
authorization UX
test
```

Do not start by writing `fetch()` directly in a page unless architecture explicitly assigns that host boundary.

---

# 88. New realtime event workflow

Confirm:

```text
backend event contract
generated/typed event representation
connection/subscription owner
product state owner
duplicate handling
ordering/gap handling
query reconciliation
test
```

Do not invent an event type client-only to patch missing backend behavior.

---

# 89. New product capability workflow

Decide whether it belongs in:

```text
existing feature package
new feature package
existing product family
new product family
host only
```

based on semantics/reuse/runtime needs.

Do not default to a new package family.

---

# 90. New UI primitive workflow

Confirm:

```text
reused across product features?
generic presentation?
token dependency?
web/mobile variants?
a11y contract?
storybook/test?
```

If product-specific, keep it product/feature-owned.

---

# 91. Refactor workflow

A refactor should state what contract remains unchanged.

If moving across packages changes:

```text
owner
exports
dependency graph
runtime availability
```

it is not purely internal.

Treat as architecture-affecting.

---

# 92. Bug-fix workflow

Reproduce the protected failure.

Fix the narrow owner.

Add regression proof at the cheapest reliable seam.

Do not restructure architecture incidentally unless the bug demonstrates an architecture defect requiring a separate decision.

---

# 93. Performance work

Measure before/after.

Identify whether bottleneck is:

```text
network
server
render
bundle
query/cache
list virtualization
realtime churn
layout
image
```

Do not memoize everything blindly.

Use repository performance standard.

---

# 94. Bundle work

Bundle optimization may affect:

```text
exports
lazy routes
dependency choice
host composition
tree shaking
```

Do not move code into global shared package solely because chunks look duplicated without ownership analysis.

---

# 95. Accessibility bug

Treat as product-quality defect.

Fix semantic cause.

Do not suppress accessibility test unless the test is demonstrably wrong and the canonical accessibility expectation is preserved.

---

# 96. Visual regression

When snapshot differs:

```text
intended design change?
token drift?
font/layout environment drift?
browser instability?
real regression?
```

Review before accepting baseline.

Do not update snapshot automatically.

---

# 97. Mobile-specific exception

A web feature unavailable on mobile may be a legitimate product scope difference.

Document/route the product decision.

Do not import web implementation into mobile to claim parity.

---

# 98. Marketing exception

Marketing can have intentionally different routing/rendering/SEO constraints.

Keep those differences host-local.

Do not use them as precedent for authenticated application architecture.

---

# 99. Third-party component code

Vendored/generated UI component code may have different formatting/style constraints.

Do not globally weaken project quality rules.

Use a narrow documented exception/configuration if necessary.

---

# 100. Security-sensitive change

For:

```text
auth/session
token storage
permission UX
OAuth redirect
CSRF client behavior
public env
HTML rendering
telemetry payload
```

read repository security standard and relevant backend contract/ADR.

Require negative/failure proof.

---

# 101. Auth/session change

Review:

```text
credential mode
refresh
logout
multi-tab/device
storage
query cache clear
realtime disconnect
route transition
cross-origin/CSRF behavior
```

Do not change only the login form.

---

# 102. Logout

Logout should clear/revoke client-side state according to contract.

Review:

```text
query cache
realtime
persisted storage
workspace/account state
sensitive forms
```

Do not leave previous tenant data visible after principal change.

---

# 103. OAuth callback

Treat callback parameters as untrusted input.

Use backend/provider contract.

Do not expose provider secrets in frontend.

Validate safe redirect behavior.

---

# 104. Public/share view

A public/share capability is not normal authenticated membership.

Do not load the full authenticated application graph if the capability only grants a bounded resource.

Respect backend share/public contract.

---

# 105. API key UI

Never display/recover stored API-key secret after the product contract says it is one-time visible.

Client should follow backend secret lifecycle.

Do not persist raw secret in telemetry/local storage.

---

# 106. Billing UI

Billing/entitlement information is backend-authoritative.

Do not compute plan entitlement from display price/plan name in client.

Use stable backend contract.

---

# 107. Governance UI

Role/permission display should consume stable Governance permission/role contracts.

Do not hard-code one-off `"Admin"` checks across unrelated components.

UX checks remain non-security.

---

# 108. Search UI

Search result cache/route must preserve tenant/resource scope.

Do not merge results from old Workspace after Workspace switch.

Backend remains authorization authority.

---

# 109. Collaboration UI

Presence/comment/editor behavior can have realtime and optimistic state.

Separate:

```text
ephemeral presence
durable comment/document state
```

Do not persist presence as durable product truth by accident.

---

# 110. Automation UI

Automation editor/execution views may involve long-running backend state.

Do not equate:

```text
request accepted
```

with:

```text
automation completed
```

Use backend status/event contract.

---

# 111. Documents UI

Rich document state may combine:

```text
server document state
collaboration/realtime
editor-local selection/composition
```

Keep these state classes separate.

Do not serialize ephemeral editor selection as product document truth unless contract says so.

---

# 112. Work Management UI

Board/Table/Calendar/Timeline/etc. are views over shared work-management data.

Do not create separate authoritative item models per view.

Shared product state belongs in Work Management owners.

---

# 113. View adapter rule

A view can adapt:

```text
layout
interaction
projection
sorting/presentation
```

without forking the core server-state model.

If a view needs a new server projection/query, add it through product state/contracts intentionally.

---

# 114. Testing report

When completing work report:

```text
files changed
owning package/topic
backend contracts touched
architecture-manifest changes
generated files
focused tests
broad gates
host builds
remaining unresolved items
```

Do not report “all good” without execution evidence.

---

# 115. Evidence precision

Say:

```text
pnpm test:web:guarded passed
```

not:

```text
frontend CI passed
```

unless the actual full CI for the exact revision passed.

---

# 116. Partial completion

If an external backend/product decision blocks part of work:

```text
complete independent safe work
report exact blocked contract
stop before inventing semantics
```

Do not fill the gap with speculative permanent code.

---

# 117. Stop: backend contract missing

Stop if the requested client behavior requires an endpoint/event/error/permission contract that is missing or contradictory.

Record:

```text
needed contract
current evidence
affected owner
why client cannot decide safely
```

---

# 118. Stop: manifest contradiction

Stop if:

```text
canonical dependency intent
≠ architecture manifest
```

and resolving it changes ownership/dependency architecture.

Classify rather than guessing.

---

# 119. Stop: web/mobile owner unclear

Stop if reusable behavior could belong to:

```text
core
state
web adapter
mobile adapter
runtime
feature
```

and the distinction materially affects dependencies.

Do not choose based on shortest import path.

---

# 120. Stop: product semantics unresolved

Stop when a frontend request requires deciding:

```text
permission meaning
billing rule
lifecycle state
ownership
deletion semantics
```

that belongs to backend/product authority.

---

# 121. Stop: ADR conflict

If an accepted FE ADR conflicts with current source and there is no superseding decision:

```text
do not silently rewrite ADR
do not silently bless source
```

classify the drift and escalate through architecture-change policy.

---

# 122. Stop: generated output requested to be edited

If task says to change generated file manually:

```text
find producer
```

unless the task explicitly concerns correcting a generation defect and the generated file will be regenerated.

---

# 123. Stop: secret in frontend

Stop immediately if a proposed implementation places secret material in client-delivered code/env/storage.

Move the trusted operation to backend/server boundary.

---

# 124. Stop: mobile imports web

Stop if production mobile graph gains web-only runtime/UI/DOM dependency.

Fix the ownership/adaptation boundary.

---

# 125. Stop: foundation becomes product-aware

Stop if foundation begins importing/encoding:

```text
Board
Page
Billing
Governance role
Automation workflow
```

as product behavior rather than neutral contract/mechanism.

Move to owner.

---

# 126. Stop: app becomes business layer

Stop if route/app shell accumulates reusable product/state logic.

Extract to product/feature owner.

---

# 127. Stop: allow-list expansion without owner analysis

Do not resolve architecture check by adding:

```text
allowedInternalImports.push(...)
```

before proving the dependency is architecturally correct.

---

# 128. Stop: cache becomes truth

Stop if implementation relies on client cache to decide irreversible server behavior or authorization.

Re-anchor to backend contract.

---

# 129. Stop: test weakened to fit implementation

Do not:

```text
remove negative assertion
remove mobile category guard
disable architecture checker
update snapshot blindly
```

just because implementation fails.

Determine whether source or test is wrong.

---

# 130. Stop: UI component abstraction for symmetry

Do not create a shared abstraction only because web/mobile have similarly named controls.

Share only when semantic API is truly reusable.

Platform-specific components are allowed.

---

# 131. Authority routes

Use these topic owners.

## Workspace/package topology

```text
docs/architecture/frontend-overview.md
```

## Dependencies/exports/mobile purity

```text
docs/architecture/dependency-boundaries.md
```

## Host composition/routing/session

```text
docs/architecture/hosts-composition-routing.md
```

## REST/generated API contracts

```text
docs/architecture/api-and-contracts.md
```

## Server state/query/mutations

```text
docs/architecture/state-query-mutations.md
```

## Realtime

```text
docs/architecture/realtime.md
```

## UI/design system/accessibility

```text
docs/architecture/ui-and-design-system.md
```

## Tests/gates

```text
docs/architecture/testing-and-quality-gates.md
```

## Architecture change

```text
docs/architecture/architecture-change-policy.md
```

---

# 132. Executable authorities

Use:

```text
package.json
→ current scripts/tool requirements

pnpm-workspace.yaml
→ workspace discovery

turbo.json
→ task graph

architecture-manifest.ts
→ exact allowed package universe/imports

generated/package-boundaries.md
→ generated readable architecture evidence

FE ADR files
→ historical consequential decisions

source/tests
→ current implementation evidence

fe-ci.yml
→ current CI execution
```

---

# 133. Do not create competing authority

Do not add new:

```text
RULES.md
ARCHITECTURE.md
MIGRATION_TRACKER.md
package matrix in AGENTS.md
manual current dependency list
```

because one task feels easier with a local note.

Use the existing canonical owner.

---

# 134. Completion checklist

Before finalizing a normal frontend change:

```text
[ ] owner identified
[ ] backend/product contract identified
[ ] host impact identified
[ ] package dependency valid
[ ] public exports valid
[ ] web/mobile/runtime safety valid
[ ] state class identified
[ ] loading/error/empty/conflict considered
[ ] accessibility considered
[ ] generated artifacts regenerated
[ ] focused tests passed
[ ] required architecture checks passed
[ ] required host build passed
[ ] broader gates passed as required
[ ] evidence reported accurately
[ ] unresolved dependency listed
```

---

# 135. Architecture-changing completion checklist

Additionally:

```text
[ ] architecture-change policy followed
[ ] FE ADR created/superseded if required
[ ] canonical architecture updated
[ ] manifest updated
[ ] generated package boundaries updated
[ ] architecture tests/gates updated
[ ] migration/compatibility considered
[ ] old path removed or removal condition recorded
```

---

# 136. Final execution rule

A frontend change is well placed when the reasoning can be stated as:

```text
product/backend contract
        ↓
correct client semantic owner
        ↓
allowed package dependency
        ↓
shared product/state behavior
        ↓
platform adapter
        ↓
host composition
        ↓
accessible user experience
```

and the evidence can be stated as:

```text
generated contracts current
architecture manifest satisfied
no deep-import/runtime leak
focused behavior tests pass
required host/UI/mobile/E2E gates pass
CI evidence belongs to the exact revision
```

The agent's job is not to make the nearest file compile.

The agent's job is to preserve **ownership, package boundaries, contract fidelity, server-state authority, runtime safety, accessibility, and executable proof** while implementing the requested behavior without inventing missing architecture.
