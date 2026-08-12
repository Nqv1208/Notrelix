---
document_id: FE-ADR-003
document_type: architecture-decision
status: Accepted
owner: frontend-architecture
applies_to:
  - frontend-package-exports
  - frontend-public-package-api
  - frontend-cross-package-imports
  - frontend-deep-import-policy
  - frontend-package-encapsulation
evidence:
  - frontend/packages/ui/web/package.json
  - frontend/packages/foundation/contracts/package.json
  - frontend/tooling/dependency-rules/src/check-frontend-dependencies.ts
  - frontend/tooling/dependency-rules/src/forbidden-source-patterns.ts
  - frontend/docs/architecture/dependency-boundaries.md
review_on:
  - frontend-package-export-model-change
  - frontend-deep-import-policy-change
  - frontend-public-subpath-change
  - frontend-package-encapsulation-change
  - frontend-dependency-checker-change
---

# FE-ADR-003 — Package Exports

## ID

`FE-ADR-003`

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

The original ADR recorded a simple but consequential monorepo problem:

> Packages need to expose public APIs without allowing deep imports into internals.

Without a public package boundary, consumers can couple directly to:

```text
src/
internal folder layout
private implementation files
generated source paths
```

and turn every internal refactor into a cross-repository breaking change.

The decision therefore established package exports as the supported cross-package API boundary.

---

# Decision

Use the `exports` field in each package's `package.json` to define supported package entrypoints.

The original ADR gave an example equivalent to:

```json
{
  "exports": {
    ".": "./src/index.ts",
    "./ui/button": "./src/components/ui/button.tsx"
  }
}
```

Supported consumers import through:

```ts
import { Button } from "@notrelix/ui-web";
```

or an explicitly exported subpath such as:

```ts
import { Button } from "@notrelix/ui-web/ui/button";
```

Consumers do **not** import arbitrary package internals.

---

# Durable identity of the decision

The identity of `FE-ADR-003` is:

> Cross-package consumers use declared package public entrypoints/subpaths; internal source layout is not a public API.

The identity is not:

```text
every package must expose only "."
every package must use wildcard exports
every source file needs a public subpath
all package exports must point to src forever
```

The exact export map can evolve while the public-boundary model remains accepted.

---

# Decision boundaries

This ADR decides:

```text
cross-package API
→ package exports

public root
→ declared "."

public subpath
→ explicitly declared export/subpath pattern

internal source
→ private by default

consumer import
→ package name or supported package subpath
```

It does not decide:

```text
which package may depend on which package
which symbols should be public
which package owns a product semantic
which source folder naming convention is permanent
```

Those are separate dependency/ownership questions.

---

# FE-ADR-003-I1 — Package public API is declared

A reusable package SHOULD expose supported consumer surfaces through:

```text
package.json exports
```

rather than relying on consumers knowing filesystem structure.

---

# FE-ADR-003-I2 — Internal `src` path is not public API

A consumer MUST NOT import:

```text
@notrelix/some-package/src/...
```

as a normal cross-package contract.

Current architecture tooling explicitly rejects this named-package `src` deep-import form.

---

# FE-ADR-003-I3 — Supported subpaths are public contracts

An export such as:

```text
@notrelix/ui-web/theme
```

is part of the package API.

Removing/renaming it can affect consumers even when internal implementation remains unchanged.

---

# FE-ADR-003-I4 — Export permission does not grant dependency permission

Even when package B exports a symbol:

```text
package A
```

may consume it only if the dependency architecture also permits:

```text
A → B
```

The two checks are independent.

---

# FE-ADR-003-I5 — Architecture permission does not force export

If the manifest allows:

```text
A → B
```

but B does not intentionally export an internal symbol, A must not bypass the package API.

Choose:

```text
intentional new public export
move behavior
or
different owner
```

---

# Relationship to dependency architecture

The frontend uses two complementary boundaries:

```text
architecture-manifest.ts
→ may package A depend on package B?

package B exports
→ what may A consume from B?
```

Both must be true.

Conceptually:

```text
dependency allowed?
        ↓ yes
public entrypoint exported?
        ↓ yes
cross-package import valid
```

---

# Relationship to FE-ADR-002

`FE-ADR-002` selects:

```text
pnpm workspace/package-management model
```

`FE-ADR-003` selects:

```text
public package API/encapsulation model
```

pnpm may successfully resolve a workspace package even if the source import violates the package export architecture.

Installation success is not API-boundary approval.

---

# Current `ui-web` evidence

Current `@notrelix/ui-web` declares an export map including:

```text
.
./ui/*
./components/*
./theme
./assets/logo.svg
```

This demonstrates the current package using both:

```text
root exports
+
supported subpaths
```

under the accepted model.

The exact current subpaths are implementation evidence.

They are not all immutable ADR clauses.

---

# Current contracts-package evidence

Current `@notrelix/contracts` declares supported exports including:

```text
.
./client
./endpoints
./types
./generated/rest
```

This allows consumers to select intentional contract surfaces without importing:

```text
src/client/...
src/generated/...
```

directly.

---

# Root export

A package root export:

```text
"."
```

is useful for the most stable/common public surface.

It should not become a barrel containing every implementation symbol solely for convenience.

---

# FE-ADR-003-I6 — Root barrel remains intentional

Do not export every internal implementation from:

```text
src/index.ts
```

just to make deep-import failures disappear.

Public surface breadth is an architecture/compatibility choice.

---

# Supported subpaths

Subpaths are appropriate when they create meaningful stable API partitions.

Examples can include:

```text
/client
/types
/theme
/ui/button
```

depending on package responsibility.

---

# FE-ADR-003-I7 — Subpath exists for stable consumer need, not filesystem mirroring

Avoid publishing:

```text
./src/*
./internal/*
./components/internal/*
```

as blanket exports that simply expose the entire source tree.

That would technically use `exports` while defeating the decision.

---

# Wildcard exports

A wildcard export such as:

```text
"./ui/*"
```

can be valid when the exported family itself is a supported public contract.

It should be reviewed for API breadth.

---

# FE-ADR-003-I8 — Wildcard export is still public API

Wildcard does not mean:

```text
private files stay private automatically
```

Anything matched by the supported export pattern can become consumer-coupled.

Use narrow patterns where boundary stability matters.

---

# Internal refactoring

The original ADR recorded:

```text
Internal refactoring doesn't break consumers
```

as a consequence.

The intended meaning is:

```text
internal file/folder structure can change
while supported package entrypoint remains compatible
```

This benefit only holds when consumers respect the public entrypoint.

---

# TypeScript resolution

The original ADR also recorded:

```text
TypeScript resolves correctly via package.json exports
```

as a consequence.

Current packages use TypeScript source entrypoints directly inside the private monorepo, so export maps participate in module resolution during development/typechecking.

---

# Current deep-import enforcement

Current architecture checker parses source imports and explicitly reports:

```text
[DEEP_IMPORT]
```

when:

```text
isDeepSrcImport(imported)
```

matches an import of the form:

```text
@notrelix/<package>/src/...
```

This is executable evidence aligned with the accepted decision.

---

# Current `isDeepSrcImport` scope

Current implementation matches:

```regex
^@notrelix/[^/]+/src/
```

Therefore it directly proves rejection of:

```text
@notrelix/foo/src/private
```

style imports.

It does not, by itself, prove every possible physical cross-package traversal is rejected.

---

# FE-ADR-003-I9 — Relative cross-package filesystem traversal is also architecturally forbidden

The original ADR explicitly gave an invalid example conceptually equivalent to:

```text
../../packages/ui/web/src/components/ui/button
```

The accepted decision therefore covers both:

```text
named-package src deep import
and
physical relative traversal into another package's internals
```

A consumer should cross a package boundary through the package API.

---

# Current enforcement completeness

From the architecture-checker source reviewed during normalization:

```text
@notrelix/foo/src/*
```

deep imports are explicitly detected.

The reviewed checker does not visibly resolve every relative import to determine whether it crosses into another package directory.

Another ESLint/tooling rule may protect that case, but that equivalent enforcement has not been established by the evidence used for this normalization.

Classification:

```text
UNRESOLVED
```

for **complete executable enforcement of relative cross-package filesystem traversal**.

This does not weaken the architecture rule.

It means the rule is stronger than the currently proven gate coverage.

---

# FE-ADR-003-I10 — Missing gate proof does not convert forbidden import into allowed behavior

Until enforcement is proven/extended:

```text
relative cross-package import into another package's internals
```

remains architecturally forbidden.

Coding agents/reviewers MUST NOT use the checker gap as permission.

---

# Package export versus relative import

Within one package, normal relative imports are internal implementation.

Example:

```ts
import { helper } from "../internal/helper";
```

inside the same package can be valid.

The ADR governs:

```text
cross-package consumers
```

not every intra-package relative import.

---

# FE-ADR-003-I11 — Package boundary, not slash count, determines the rule

Do not ban all relative imports globally.

Determine whether the import stays inside the same package owner or crosses into another package's private source.

---

# Public export expansion

Adding a new export creates a new supported consumer seam.

---

# FE-ADR-003-I12 — Export expansion is deliberate compatibility surface

Before adding:

```text
"./internal-x"
```

ask:

```text
Is this actually stable package responsibility?
Does the consumer belong here?
Could behavior move?
Will mobile/runtime constraints remain valid?
```

Do not expose internals solely to make one import compile.

---

# Public export removal

Removing an export can break current monorepo consumers.

Even though packages are developed in one repository, the export is a stable internal contract.

---

# FE-ADR-003-I13 — Export removal migrates consumers atomically

For a removed/renamed export:

```text
identify consumers
provide replacement if needed
migrate
remove old export
run architecture/type/build tests
```

Do not leave consumers deep-importing the old source path.

---

# Export aliases

Temporary aliases can support migration.

---

# FE-ADR-003-I14 — Compatibility export alias has removal condition

A compatibility subpath should not become a permanent duplicate authority unless intentionally retained as public API.

---

# Generated code exports

Generated contract outputs can be exposed through stable package subpaths.

The generated files themselves remain generator-owned.

---

# FE-ADR-003-I15 — Public generated entrypoint does not make generated internals hand-editable

Example:

```text
@notrelix/contracts/generated/rest
```

can be public.

But:

```text
src/generated/rest/*
```

remains generated source controlled by its producer.

---

# Test helpers

Test-only helpers should not be exported through production root solely for test convenience.

Use testing packages/test-local seams where appropriate.

---

# FE-ADR-003-I16 — Test convenience does not widen production package API by default

If a test needs a private implementation symbol:

```text
test through public behavior
move test helper
or
create a deliberate seam
```

before exporting implementation detail globally.

---

# App packages

Apps are composition roots and are generally not intended as reusable internal libraries.

Other packages should not import app internals.

---

# FE-ADR-003-I17 — App source is not a reusable package API

Do not solve reuse by exporting:

```text
apps/web/src/...
```

to product/feature packages.

Move reusable behavior inward to the correct package.

---

# Framework package boundary

Export maps also help keep framework-specific implementation behind:

```text
ui-web
runtime-web
ui-mobile
runtime-mobile
```

public APIs.

They complement FE-ADR-001/004.

---

# Alternatives Considered

## Historical alternatives

The original ADR does not contain a formal alternatives section.

Its context identifies the rejected architectural condition:

```text
allow consumers to import package internals directly
```

because the accepted decision explicitly introduces public exports to prevent that.

The original record does not document a detailed comparison among:

```text
TypeScript path aliases
barrel files without package exports
Nx boundary tooling
ESLint-only boundaries
build-time package compilation
```

Those are not retroactively inserted as historical alternatives.

---

# Consequences

The original ADR recorded three consequences.

## Clear public API boundary

Consumers know which package surfaces are supported.

## Internal refactoring does not break consumers

Internal layout can evolve behind a compatible export map.

## TypeScript resolves via package exports

Package entrypoints participate in TypeScript/module resolution.

---

# Additional current consequences

These are current architectural implications, not claims that the original ADR listed them.

## Export surface is compatibility surface

Adding/removing a subpath affects cross-package consumers.

## Dependency and export checks are complementary

A consumer requires:

```text
allowed dependency
+
supported export
```

not one or the other.

## Deep-import violations are partly executable

The current architecture checker explicitly catches named-package `/src/` deep imports.

## Gate completeness still needs proof for relative traversal

The architecture rule forbids physical cross-package traversal, while complete automated coverage for that form remains `UNRESOLVED` from the source reviewed here.

---

# Compatibility / Migration

## Historical migration plan

**Not recorded in the original ADR.**

The original decision does not document how existing deep imports were migrated when the rule was introduced.

No migration chronology is invented.

## Current migration model

When replacing a private/deep import:

```text
1. identify correct owner
2. decide whether symbol should be public
3. add intentional export if appropriate
4. migrate consumer to package entrypoint/subpath
5. remove obsolete compatibility path
6. run architecture/type/tests/build evidence
```

## Export-shape evolution

A package can evolve from:

```text
root-only exports
```

to:

```text
root + supported subpaths
```

without superseding this ADR, because both still follow the public-export model.

---

# What does not require superseding this ADR

Examples:

```text
add a supported public subpath
remove an obsolete subpath with consumer migration
change internal folder structure
change root barrel composition
replace wildcard export with explicit subpaths
change export target from source file to built file
```

provided the package-export/public-API model remains intact.

---

# What can require superseding this ADR

A new decision may be required if Notrelix intentionally adopts a fundamentally different cross-package API model, for example:

```text
allow arbitrary deep imports as supported architecture
remove package export encapsulation entirely
treat source filesystem as public API
adopt a different module-boundary architecture that replaces package exports
```

No such change is currently recorded.

---

# Evidence

## Original ADR

The original record explicitly contains:

```text
Date: 2026-07-12
Status: Accepted

Context:
packages need public APIs without deep imports

Decision:
use package.json exports

Rules:
import via package name
or approved subpath
never import deep package source paths

Consequences:
clear API
internal refactoring safety
TypeScript export resolution
```

## Current `ui-web` evidence

`frontend/packages/ui/web/package.json` currently exposes:

```text
.
./ui/*
./components/*
./theme
./assets/logo.svg
```

## Current contracts evidence

`frontend/packages/foundation/contracts/package.json` currently exposes:

```text
.
./client
./endpoints
./types
./generated/rest
```

## Current architecture-checker evidence

`frontend/tooling/dependency-rules/src/check-frontend-dependencies.ts`:

```text
parses ImportDeclaration nodes
checks isDeepSrcImport()
reports [DEEP_IMPORT]
checks allowed internal @notrelix package dependencies
```

## Current deep-import matcher evidence

`frontend/tooling/dependency-rules/src/forbidden-source-patterns.ts` defines:

```text
isDeepSrcImport(importPath)
→ /^@notrelix\/[^/]+\/src\//
```

This proves current enforcement for named package `/src/` imports.

---

# Evidence interpretation

Current evidence demonstrates that the package-export architecture remains implemented.

However, exact checker evidence reviewed here proves only a subset of all possible physical deep-import forms.

Therefore:

```text
decision status
→ Accepted

public package-export model
→ aligned

named @notrelix/.../src deep-import gate
→ proven

relative cross-package traversal gate
→ UNRESOLVED
```

---

# Current known alignment

No reviewed evidence indicates that the package-export decision has been superseded.

Current package manifests actively use `exports`.

Current architecture tooling explicitly detects named deep imports and enforces internal package dependency permissions.

Status remains:

```text
Accepted
```

---

# Historical fidelity notes

This normalization does not claim:

- who originally approved the export model;
- which alternative module-boundary systems were formally evaluated;
- that current wildcard export patterns existed on 2026-07-12;
- that every current package export is ideal;
- that current tooling already blocks every possible filesystem traversal;
- that package exports alone enforce dependency architecture.

---

# Relationship to current architecture

Read:

```text
../architecture/dependency-boundaries.md
../architecture/architecture-change-policy.md
../architecture/testing-and-quality-gates.md
```

for the current operating rules.

This ADR explains why package public exports are part of the frontend foundation.

---

# Review triggers

Review this ADR when proposing to:

```text
remove package export maps as the public API boundary
permit arbitrary cross-package internals
change the fundamental cross-package module contract
replace package exports with a different repository-wide encapsulation model
```

Routine export additions/removals under the same model do not automatically reopen the decision.

---

# Supersedes

**None.**

No earlier frontend ADR is recorded as superseded by `FE-ADR-003`.

---

# Superseded By

**None.**

At normalization time, no recorded frontend ADR supersedes `FE-ADR-003`.

---

# Normalization note

This normalization preserves:

```text
Date
Status
Context
package.json exports decision
root/subpath import model
deep-import prohibition
recorded consequences
```

It adds:

```text
Owners
decision identity
current package evidence
current checker evidence
Alternatives Considered
Compatibility / Migration
Supersedes
Superseded By
enforcement-gap classification
```

Historical alternatives/owners are not invented.

The accepted decision itself has not been changed.
