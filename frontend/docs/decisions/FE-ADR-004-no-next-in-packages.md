---
document_id: FE-ADR-004
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-framework-boundaries
  - frontend-nextjs-boundary
  - frontend-reusable-packages
  - frontend-web
  - frontend-mobile
  - frontend-marketing
evidence:
  - frontend/apps/marketing/package.json
  - frontend/apps/web/package.json
  - frontend/apps/mobile/package.json
  - frontend/tooling/dependency-rules/src/forbidden-imports.ts
  - frontend/tooling/dependency-rules/src/check-package-manifests.ts
  - frontend/tooling/dependency-rules/src/check-frontend-dependencies.ts
  - frontend/tooling/dependency-rules/src/check-folder-boundaries.ts
  - frontend/docs/architecture/dependency-boundaries.md
  - frontend/docs/architecture/hosts-composition-routing.md
review_on:
  - frontend-nextjs-boundary-change
  - frontend-framework-neutrality-change
  - frontend-marketing-host-change
  - frontend-framework-contamination-rule-change
---

# FE-ADR-004 — No Next.js in Reusable Packages

## ID

`FE-ADR-004`

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

The original ADR identified a framework-contamination risk.

Notrelix intentionally uses different frontend hosts:

```text
marketing
→ Next.js

authenticated web product
→ Vite + React

mobile
→ Expo / React Native
```

The reusable package graph must remain usable by the non-Next hosts where intended.

The original context therefore stated that:

> Next.js is a framework-specific dependency. Packages should be framework-neutral to be reusable across web (Vite) and mobile (Expo).

The problem was not that Next.js is intrinsically bad.

The problem was:

```text
Next-specific APIs imported into reusable packages
→ reusable package becomes coupled to the marketing framework
→ Vite/mobile reuse becomes harder or impossible
→ framework boundary collapses
```

---

# Decision

The original accepted decision is:

> **Reusable frontend packages must not depend on `next`.**

and:

> **Only `apps/marketing` may use Next.js.**

The original rule set explicitly recorded:

```text
packages/* cannot have `next` in dependencies
packages/* cannot import `next/*`
apps/web cannot import `next/*`
apps/mobile cannot import `next/*`
routing/navigation must go through platform adapters
```

The durable identity of this ADR is the **Next.js framework-contamination boundary**.

---

# Durable decision identity

`FE-ADR-004` means:

```text
Next.js
→ marketing host concern

reusable packages
→ no Next framework dependency

app-web
→ no Next framework dependency

app-mobile
→ no Next framework dependency
```

It does **not** mean:

```text
all packages must be completely platform-neutral
all web packages must be framework-free
ui-web cannot use React DOM/web libraries
runtime-web cannot use browser APIs
```

Those are broader dependency/runtime rules governed elsewhere.

---

# Relationship to FE-ADR-001

`FE-ADR-001` decided:

```text
three separate host/framework architectures
```

`FE-ADR-004` protects that split from one specific contamination vector:

```text
Next.js leaking out of apps/marketing
```

Conceptually:

```text
FE-ADR-001
→ split the hosts

FE-ADR-004
→ keep Next.js inside its approved host boundary
```

---

# Why this decision matters

Without this rule, a package might introduce:

```ts
import { cookies } from "next/headers";
import Link from "next/link";
import { useRouter } from "next/navigation";
```

and then be consumed by:

```text
Vite web
or
Expo/native
```

creating framework incompatibility.

The accepted boundary keeps shared package contracts independent from the marketing host framework.

---

# FE-ADR-004-I1 — `next` is not a reusable-package dependency

Production reusable packages under:

```text
frontend/packages/**
```

MUST NOT declare `next` as a normal dependency under the accepted decision.

A future exception would require explicit bounded governance and must not become precedent.

---

# FE-ADR-004-I2 — `next/*` imports remain marketing-host local

A reusable package MUST NOT import:

```text
next/*
```

to implement:

```text
navigation
cookies
headers
server actions
metadata
images
routing
```

for general frontend behavior.

---

# FE-ADR-004-I3 — `apps/web` remains non-Next

The authenticated web application is a Vite/React host under the accepted architecture.

It MUST NOT depend on Next.js application/runtime APIs unless a superseding architecture decision changes the host model.

---

# FE-ADR-004-I4 — `apps/mobile` remains non-Next

The native mobile application MUST NOT depend on:

```text
next
next/*
next-themes
```

or other Next-specific runtime behavior.

---

# FE-ADR-004-I5 — Marketing may use Next.js

`apps/marketing` is the approved Next.js host.

This ADR does not prohibit:

```text
next
next/link
next/image
Next App Router
server/client component APIs
```

inside the marketing host where appropriate.

---

# Marketing boundary

Marketing using Next.js does not imply:

```text
shared UI
→ may import next

features
→ may import next

product packages
→ may import next
```

If marketing needs reusable behavior:

```text
extract framework-neutral visual/product-independent primitive
```

where appropriate.

Keep the Next-specific adapter/composition in the marketing app.

---

# Current source alignment — hosts

Current source remains aligned at the three host level:

```text
apps/marketing
→ Next.js

apps/web
→ Vite/React

apps/mobile
→ Expo/React Native
```

No reviewed evidence indicates that the host model itself has been superseded.

---

# Current executable enforcement

The current dependency-rule tooling contains several relevant layers of enforcement.

## Explicit forbidden package declarations/imports

`forbidden-imports.ts` includes explicit `next` / `next-themes` prohibitions for several packages and for:

```text
@notrelix/app-web
@notrelix/app-mobile
```

The architecture checker normalizes:

```text
next/*
→ base package "next"
```

before testing forbidden dependencies/imports.

---

# Package-manifest enforcement

`check-package-manifests.ts` scans:

```text
dependencies
devDependencies
peerDependencies
```

for package-specific forbidden dependencies.

If a declared dependency matches the package's forbidden list, it emits:

```text
[DECLARED_FORBIDDEN_DEPENDENCY]
```

This is current executable evidence.

---

# Source-import enforcement

`check-frontend-dependencies.ts` scans TypeScript import declarations.

For:

```text
next/*
```

it normalizes the base package to:

```text
next
```

and rejects the import when the current package's forbidden policy includes Next.

This is also current executable evidence.

---

# Core-layer enforcement

`check-folder-boundaries.ts` independently rejects framework imports from files classified as `core`.

Its current forbidden core imports include:

```text
next
```

and:

```text
next/*
```

This protects product/core purity even where package-level forbidden lists are not identical.

---

# Current enforcement coverage caveat

The original ADR rule was broad:

```text
packages/* cannot have next
packages/* cannot import next/*
```

The current `FORBIDDEN_IMPORTS` table is package-specific rather than generated from one global “all packages except marketing” rule.

Some packages visibly include `next` in their forbidden lists, while some package entries focus on other constraints.

Core files receive an additional generic folder-boundary prohibition.

Therefore, from the reviewed source alone, the following distinction is necessary:

```text
architectural rule
→ global across reusable packages

current executable enforcement
→ strong, but implemented through several package/layer-specific mechanisms
```

A future audit/tooling test SHOULD prove complete package coverage against the ADR rule rather than assuming the table is exhaustive.

Classification for **complete global enforcement proof**:

```text
UNRESOLVED
```

until a deterministic test demonstrates that every current/future non-marketing governed package cannot declare/import Next.js.

---

# FE-ADR-004-I6 — Enforcement gap does not weaken the decision

If one package is not currently covered by a forbidden-import list:

```text
that is not permission to add next
```

It is:

```text
possible gate coverage debt
```

The accepted architecture remains the authority.

---

# Current routing rule relationship

The original ADR also recorded:

> Routing/navigation must go through `@notrelix/platform` adapters.

That statement is broader than the core Next.js contamination decision.

Current architecture has since refined navigation ownership into:

```text
host router
→ app/host-owned

inner runtime/product code
→ callbacks/adapters/contracts
```

The current canonical owner is:

```text
frontend/docs/architecture/hosts-composition-routing.md
```

This ADR preserves the historical rule but does not expand its meaning beyond the recorded decision.

---

# Current routing source alignment caveat

Current frontend source includes some feature-level TanStack Router usage.

That does not violate the **No Next.js** decision directly.

It may violate broader router-independence rules, which are handled under:

```text
FE-ADR-005
and
current host/dependency architecture
```

Do not conflate:

```text
Next.js framework contamination
```

with:

```text
all router-library coupling
```

They are related but distinct concerns.

---

# Framework neutrality

The original consequences used the phrase:

```text
Packages are framework-neutral
```

This must be interpreted carefully.

Today the frontend package architecture intentionally contains platform-specific reusable packages such as:

```text
ui-web
runtime-web
ui-mobile
runtime-mobile
product web adapters
product mobile adapters
```

These are not “framework-neutral” in an absolute sense.

The durable ADR decision is specifically:

```text
No Next.js contamination outside marketing.
```

---

# FE-ADR-004-I7 — Historical broad wording does not erase later explicit package roles

Do not reinterpret:

```text
"packages are framework-neutral"
```

to prohibit all platform-specific package layers.

Current architecture explicitly permits platform-specific adapters/runtime/UI packages.

What remains forbidden under this ADR is:

```text
Next.js as a reusable/non-marketing framework dependency
```

unless superseded.

---

# Alternatives Considered

## Historical alternative

The original ADR does not contain a formal alternatives section.

The rejected direction recoverable from the decision is:

```text
allow Next.js dependencies/imports in reusable packages
```

because the accepted decision explicitly forbids that.

The original record does not document a detailed comparison of:

```text
all-Next monorepo
React Server Components in packages
framework-specific package variants
Next.js as authenticated web host
```

No such historical evaluation is invented.

---

# Consequences

The original ADR recorded:

- packages remain framework-neutral;
- web app can use Vite without Next.js contamination;
- mobile can share packages without web-framework dependencies;
- boundary is enforced by `tooling/dependency-rules/`.

These consequences are preserved as historical record.

---

# Current consequences

## Marketing framework freedom

Marketing can use Next.js-specific rendering/routing features without forcing them into the authenticated web/mobile clients.

## Vite isolation

The authenticated web app can evolve under its Vite/React host without needing Next runtime conventions.

## Mobile safety

The mobile graph does not need to carry Next.js server/browser assumptions.

## Package design pressure

If reusable code requires navigation/configuration behavior:

```text
define a neutral contract
inject adapter/callback
or
place framework-specific behavior in the correct outer host
```

instead of importing `next/*`.

---

# Compatibility / Migration

## Historical migration plan

**Not recorded in the original ADR.**

No chronology of existing Next.js dependency removal is documented.

## Current migration model for an accidental Next dependency

If a reusable package currently imported Next.js:

```text
1. identify the actual framework-specific need
2. move Next-specific implementation to apps/marketing where appropriate
3. extract a framework-neutral contract if reusable behavior remains
4. migrate consumers
5. remove the `next` dependency/import
6. update architecture checker coverage if the defect escaped
7. run architecture/type/build tests
```

---

# What does not require superseding this ADR

Examples:

```text
Next.js version upgrade in marketing
new marketing route
new marketing server component
new shared UI primitive used by marketing
new Vite route
new Expo screen
```

provided Next.js remains confined to its accepted host boundary.

---

# What can require superseding this ADR

Likely examples:

```text
authenticated product web intentionally moves to Next.js
reusable packages intentionally adopt Next.js runtime APIs
Next.js becomes a repository-wide framework foundation
marketing/product host architecture is merged
```

Such a change should also review `FE-ADR-001`.

---

# Evidence

## Original ADR evidence

The legacy ADR explicitly recorded:

```text
Date: 2026-07-12
Status: Accepted
Only apps/marketing may use Next.js
packages cannot depend on next
packages cannot import next/*
apps/web cannot import next/*
apps/mobile cannot import next/*
boundary enforced by tooling/dependency-rules
```

## Current tooling evidence

Current:

```text
frontend/tooling/dependency-rules/src/forbidden-imports.ts
frontend/tooling/dependency-rules/src/check-package-manifests.ts
frontend/tooling/dependency-rules/src/check-frontend-dependencies.ts
frontend/tooling/dependency-rules/src/check-folder-boundaries.ts
```

all contain relevant framework-boundary enforcement.

## Current host evidence

Current source still implements:

```text
marketing → Next.js
web → Vite
mobile → Expo
```

as separate host applications.

---

# Evidence interpretation

Current evidence supports:

```text
decision status
→ Accepted

host boundary
→ aligned

app-web/app-mobile Next prohibition
→ explicitly gated

many reusable package/core Next prohibitions
→ explicitly gated

complete all-package future-proof coverage
→ UNRESOLVED until directly proven
```

---

# Historical fidelity notes

This normalization does not claim:

- original decision owner;
- detailed alternatives;
- that every current package type existed in July 2026;
- that every current forbidden-import entry existed on the decision date;
- that “framework-neutral” means all packages are platform-neutral;
- that current tooling coverage is perfect.

---

# Review triggers

Review this ADR when a proposal would:

```text
move authenticated web to Next.js
allow next in reusable packages
allow next/* imports outside marketing
collapse marketing and product web host boundaries
replace the current host framework strategy
```

---

# Supersedes

**None.**

No earlier frontend ADR is recorded as superseded by `FE-ADR-004`.

---

# Superseded By

**None.**

At normalization time, no recorded frontend ADR supersedes `FE-ADR-004`.

---

# Normalization note

This normalization preserves:

```text
Date
Status
Context
No-Next decision
marketing-only Next ownership
recorded rules
recorded consequences
```

It adds:

```text
Owners
decision identity
relationship to FE-ADR-001
current enforcement evidence
enforcement-coverage caveat
Alternatives Considered
Compatibility / Migration
Supersedes
Superseded By
```

The accepted decision itself has not been changed.
