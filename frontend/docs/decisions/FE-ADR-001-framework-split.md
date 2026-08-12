---
document_id: FE-ADR-001
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-hosts
  - frontend-framework-split
  - frontend-web
  - frontend-mobile
  - frontend-marketing
evidence:
  - frontend/apps/web/package.json
  - frontend/apps/mobile/package.json
  - frontend/apps/marketing/package.json
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/docs/architecture/frontend-overview.md
  - frontend/docs/architecture/hosts-composition-routing.md
review_on:
  - frontend-host-framework-change
  - frontend-host-consolidation
  - frontend-new-host
  - frontend-host-runtime-model-change
---

# FE-ADR-001 — Framework Split

## ID

`FE-ADR-001`

## Status

**Accepted**

## Date

**2026-07-12**

This date is preserved from the original ADR.

## Owners

**Current stewardship:** `frontend-architecture`

**Historical decision owner/authorship:** Not recorded explicitly in the original ADR.

Current stewardship does not imply historical authorship.

---

# Context

The original ADR stated that the Notrelix frontend needed to serve multiple client experiences with different runtime needs:

- a public marketing website where SEO is important;
- an authenticated product web application;
- a mobile application.

The original ADR described the mobile application as future-facing at the time of the decision.

The architectural problem was therefore not simply:

```text
Which JavaScript framework should the frontend use?
```

It was:

```text
Should marketing, authenticated web product, and mobile
share one host/framework architecture,
or should each use a host framework suited to its runtime needs
while sharing reusable packages underneath?
```

The decision chose separate hosts.

---

# Decision

Notrelix uses three separate frontend host applications:

| Host             | Accepted framework direction   | Accepted purpose           |
| ---------------- | ------------------------------ | -------------------------- |
| `apps/marketing` | Next.js App Router             | SEO/public marketing pages |
| `apps/web`       | Vite + React + TanStack Router | Authenticated product SPA  |
| `apps/mobile`    | Expo / React Native            | Native/mobile application  |

The durable decision is the **host/framework split**.

Conceptually:

```text
marketing
→ Next.js host

authenticated web product
→ Vite + React host

mobile product
→ Expo / React Native host
```

while reusable product/client capabilities are shared through governed packages rather than by forcing the host applications onto one framework.

---

# Decision boundaries

This ADR decides:

```text
marketing and authenticated product web are separate hosts
web product is not implemented as the marketing Next.js application
mobile is a separate native host
framework-specific host concerns remain at host boundaries
shared behavior is moved into reusable packages where appropriate
```

It does **not** decide every current package or implementation detail.

---

# Durable identity of the decision

The identity of `FE-ADR-001` is:

> Notrelix intentionally uses separate host/framework architectures for marketing, authenticated web product, and mobile, instead of forcing all three client experiences through one framework/runtime.

The decision identity is **not**:

```text
one exact Vite version
one exact Next.js version
one exact Expo version
one exact TanStack Router version
one exact host source-file layout
one exact build script
```

Those implementation details may evolve without superseding this ADR, provided the accepted host/framework separation remains intact.

---

# Current architecture interpretation

The current architecture documents refine the accepted decision as:

```text
apps
→ composition roots

foundation/product packages
→ reusable inward behavior

runtime packages
→ platform-specific mechanism adapters

web/mobile product adapters
→ platform-specific presentation/integration

marketing
→ isolated public marketing host
```

The exact package dependency graph is separately governed by:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

This ADR does not maintain that graph manually.

---

# Host 1 — Marketing

The accepted decision assigns:

```text
apps/marketing
→ Next.js
```

for public marketing/SEO needs.

The original ADR specifically named:

```text
Next.js App Router
```

and the consequences cited:

```text
SSG/SSR for SEO
```

as a benefit.

Current source still declares `next` in `@notrelix/app-marketing`.

---

# Marketing boundary

The accepted split does not mean:

```text
Next.js
→ general framework for every frontend package
```

That framework-contamination question is further governed by `FE-ADR-004`.

Current dependency architecture keeps the marketing host in:

```text
marketing-isolated
```

scope with a deliberately narrow shared internal dependency surface.

---

# Host 2 — Authenticated web product

The accepted decision assigns:

```text
apps/web
→ Vite + React + TanStack Router
```

for the authenticated product SPA.

The original ADR cited the fast SPA/development model as a consequence.

Current `@notrelix/app-web` still declares:

```text
vite
react
react-dom
@tanstack/react-router
```

and is built as a Vite application.

---

# Web boundary

The web host is an outer composition root.

The accepted framework decision does not authorize:

```text
Vite/TanStack Router imports
→ product core
→ foundation
→ mobile packages
```

Framework-specific APIs stay at appropriate host/adapter boundaries according to current architecture.

---

# Host 3 — Mobile

The accepted decision assigns:

```text
apps/mobile
→ Expo / React Native
```

The original ADR described mobile as a future app at the time of decision.

Current source now contains a real `@notrelix/app-mobile` application using:

```text
Expo
Expo Router
React Native
```

so the formerly future-facing branch of the decision is now implemented.

---

# Mobile boundary

Mobile shares reusable client/product packages where those packages are native-safe.

It does not share web rendering implementation merely because the product capability is shared.

Current architecture therefore distinguishes:

```text
runtime-web
runtime-mobile

ui-web
ui-mobile

product-web adapters
product-mobile adapters
```

where required.

---

# Shared package model

The original consequence stated:

```text
Mobile can share packages with web.
```

The current architecture interprets this as:

```text
share platform-neutral contracts/product semantics/state where safe
+
use platform-specific runtime/UI/adapters where required
```

not:

```text
mobile imports the web app
or
mobile imports DOM/web implementation
```

---

# Separate dependency trees

The original ADR stated:

```text
Each app has its own dependency tree.
```

The durable meaning is that each host can declare the framework/runtime dependencies it requires.

In the current pnpm workspace, all hosts still participate in one monorepo and share one workspace dependency-management system.

Therefore:

```text
separate host dependency manifests/graphs
≠ separate repository/package-manager universes
```

---

# Current source alignment

Current source remains aligned with the accepted host split:

```text
@app-web
→ Vite/React/TanStack Router dependencies

@app-mobile
→ Expo/React Native/Expo Router dependencies

@app-marketing
→ Next.js/React DOM dependencies
```

The architecture manifest also registers all three as separate `app` units with different freeze/dependency scopes.

This is current implementation evidence, not additional historical rationale.

---

# Alternatives Considered

## Alternative A — One framework/runtime for all three hosts

The original ADR does not contain a dedicated alternatives section.

However, the decision context and the explicit choice to “split into three separate apps” make the rejected direction of a single shared host/framework model recoverable at a high level.

That alternative would have meant treating:

```text
marketing
authenticated web product
mobile
```

as one host/framework architecture rather than three separate host applications.

The original ADR did not record a detailed evaluation of this alternative, so no additional historical advantages/disadvantages are invented here.

## Other alternatives

**Not recorded in the original ADR.**

The original record did not document evaluation of specific alternatives such as:

```text
all Next.js
all React Native Web
other SPA frameworks
other native frameworks
microfrontends
```

Those possibilities must not be inserted retroactively as historical facts.

---

# Consequences

The original ADR recorded the following consequences.

## Marketing SEO/rendering capability

The separate marketing host can use Next.js rendering capabilities appropriate to SEO/public pages.

## Product SPA independence

The authenticated product web application can use Vite/React/TanStack Router without being forced through the marketing host's framework/runtime model.

## Mobile sharing

The mobile application can share reusable packages with the web product where those packages are platform-safe.

## Per-host dependency graph

Each host can own dependencies appropriate to its runtime.

---

# Additional current consequences

These are current architectural implications of the accepted decision, not claims about what the original authors explicitly wrote.

## Framework knowledge stays outward

Reusable packages should not require a universal host framework.

This enables:

```text
product core/state
```

to remain reusable while:

```text
web/mobile/marketing
```

adapt differently.

## Host builds remain independent evidence

Web, mobile, and marketing require separate packaging/build evidence.

A successful Vite build does not prove Next.js or Expo host compatibility.

## Host-specific routing is allowed

Web can use TanStack Router.

Mobile can use Expo Router.

The product model does not require one shared router implementation.

---

# Compatibility / Migration

## Historical migration plan

**Not recorded in the original ADR.**

The original ADR recorded the target split but did not provide a staged migration plan, compatibility timeline, or old-host removal procedure.

## Current compatibility contract

The accepted architecture expects reusable packages to avoid accidental host-framework coupling so that:

```text
web
mobile
marketing
```

can evolve independently.

Current compatibility depends on:

```text
public package exports
closed-world dependency rules
web/mobile runtime separation
web/mobile UI separation
host-specific build/test gates
```

## Mixed-host development

The hosts coexist in one monorepo.

They do not need to deploy or render through the same framework.

A change to shared packages must preserve every consuming host's compatibility or be migrated deliberately.

---

# What does not require superseding this ADR

Examples:

```text
Vite upgrade
Next.js upgrade
Expo upgrade
TanStack Router upgrade
Expo Router upgrade
host source-file reorganization
new marketing page
new product web route
new mobile screen
new shared package
```

provided the host/framework split remains the same and other architecture rules are preserved.

---

# What can require superseding this ADR

A new decision is likely required if Notrelix intentionally changes the accepted host model, for example:

```text
merge marketing and authenticated product web into one Next.js host
move authenticated product web from Vite SPA to Next.js as the host foundation
replace native mobile host with a fundamentally different host architecture
introduce one universal runtime/framework that replaces the separate host split
adopt a deployment/runtime federation model that changes host ownership materially
```

The exact future decision should be evaluated when proposed.

This ADR does not pre-approve any of those changes.

---

# Relationship to FE-ADR-004

`FE-ADR-001` decides that:

```text
marketing
web
mobile
```

use separate framework/host architectures.

`FE-ADR-004` further decides that Next.js must not contaminate reusable packages/non-marketing hosts.

They are related but not duplicates.

Conceptually:

```text
FE-ADR-001
→ why hosts/frameworks are split

FE-ADR-004
→ why Next.js stays within its approved framework boundary
```

---

# Relationship to current dependency architecture

The current architecture manifest contains separate entries for:

```text
@notrelix/app-web
@notrelix/app-mobile
@notrelix/app-marketing
```

with different allowed internal dependencies.

This is executable current evidence supporting the accepted split.

The manifest is not historical decision rationale.

---

# Relationship to current host architecture

Current canonical operating documents are:

```text
../architecture/frontend-overview.md
../architecture/hosts-composition-routing.md
../architecture/dependency-boundaries.md
```

Read those documents for current implementation rules.

Use this ADR when the reason for the host split matters.

---

# Evidence

## Original decision record

The pre-normalization ADR recorded:

```text
Date: 2026-07-12
Status: Accepted
Context: marketing + web + future mobile
Decision: split into three apps
Consequences: SEO/SSG, Vite SPA, package sharing, separate dependency trees
```

The normalized ADR preserves that meaning.

## Current web evidence

```text
frontend/apps/web/package.json
```

declares:

```text
Vite
React
React DOM
TanStack Router
```

and current web build scripts remain Vite-based.

## Current mobile evidence

```text
frontend/apps/mobile/package.json
```

declares:

```text
Expo
Expo Router
React Native
```

and the application entry is Expo Router based.

## Current marketing evidence

```text
frontend/apps/marketing/package.json
```

declares:

```text
Next.js
React
React DOM
```

with Next development/build/start scripts.

## Current dependency-governance evidence

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

registers all three apps separately:

```text
app-web
→ core-production

app-mobile
→ core-production

app-marketing
→ marketing-isolated
```

with different allowed internal dependency sets.

---

# Evidence interpretation

The current evidence demonstrates that the accepted decision remains implemented.

It does not prove that every framework-specific implementation detail in 2026 is immutable.

For example:

```text
Vite version
Expo version
Next version
router version
```

can change without changing the decision identity.

---

# Current known alignment

At normalization time, no evidence reviewed indicates that the host/framework split itself has been superseded.

Current source remains consistent with:

```text
web = Vite/React
mobile = Expo/React Native
marketing = Next.js
```

Therefore status remains:

```text
Accepted
```

---

# Historical fidelity notes

This normalization intentionally does not claim:

- who originally approved/authored the decision;
- what detailed alternatives were considered;
- what staged migration plan was used;
- that microfrontends or other frameworks were explicitly rejected;
- that today's exact package graph existed on 2026-07-12;
- that today's exact router/build versions are part of the decision.

Those facts were not recorded by the original ADR.

---

# Decision invariants

The accepted decision can be summarized by the following historical/current interpretation.

## FE-ADR-001-I1 — Marketing has a dedicated host

Marketing remains a distinct host from the authenticated product web application.

## FE-ADR-001-I2 — Authenticated web uses its own SPA host architecture

The product web application is not merely a route subtree of the marketing Next.js host under the accepted decision.

## FE-ADR-001-I3 — Mobile has its own native host architecture

Mobile is not implemented by importing the web application and shrinking it to a mobile viewport.

## FE-ADR-001-I4 — Reuse happens through packages

Cross-host reuse should occur through appropriately owned reusable packages, not app-to-app imports.

## FE-ADR-001-I5 — Host-specific frameworks stay outward

Framework-specific host details remain at host/runtime/UI/adapter boundaries according to current architecture.

These invariants describe the accepted decision's architectural meaning.

They do not replace exact package dependency rules.

---

# Review triggers

Review this ADR when a proposal would:

```text
merge host applications
change the authenticated web host foundation
change the mobile host foundation materially
move Next.js into the product host architecture
introduce a universal cross-host runtime framework
add a new host whose existence changes the original split
```

Routine framework version upgrades do not automatically reopen the decision.

---

# Supersedes

**None.**

No earlier frontend ADR is recorded as being superseded by `FE-ADR-001`.

---

# Superseded By

**None.**

At normalization time, no recorded frontend ADR supersedes `FE-ADR-001`.

---

# Normalization note

This file is a structural normalization of the original Accepted ADR.

The normalization:

```text
preserves
→ Date
→ Status
→ Context
→ Decision
→ recorded Consequences

adds
→ Owners structure
→ Alternatives section
→ Compatibility/Migration section
→ Evidence section
→ Supersedes/Superseded By
→ current architecture routing
```

Missing historical details are explicitly marked as not recorded rather than reconstructed from present-day preference.

The accepted decision itself has not been changed.
