# Notrelix Client — Dependency Boundary Matrix

**Version:** 5.0
**Status:** Active
**Enforcement:** `pnpm check:architecture` (closed-world manifest + docs drift check)

---

## Overview

This document describes the dependency boundary **principles** of the Notrelix
client workspace. The exact per-package allow-list is **generated** from the
executable architecture manifest and must never be hand-maintained here:

- Executable source of truth: `tooling/dependency-rules/src/architecture-manifest.ts`
- Generated exact table: [`package-boundaries.generated.md`](./package-boundaries.generated.md)
- Regenerate: `pnpm --filter @notrelix/dependency-rules docs:generate`
- Drift check (runs inside `pnpm check:architecture`): `pnpm check:architecture-docs`

Every directory under `frontend/apps/**` and `frontend/packages/**` that
contains a `package.json` is part of the closed-world package universe. A
package that exists but is not registered in the manifest fails the
architecture gate; a manifest entry with no matching package also fails.

---

## Layer Hierarchy

```txt
┌─────────────────────────────────────────────────────────┐
│  Apps Layer: apps/web, apps/marketing, apps/mobile      │
├─────────────────────────────────────────────────────────┤
│  Features Layer: packages/features/*                    │
├─────────────────────────────────────────────────────────┤
│  Product Layer: packages/product/{work-management,      │
│                 docs, automation}/*                     │
├─────────────────────────────────────────────────────────┤
│  UI Layer: packages/ui/{tokens, web, mobile, icons}     │
├─────────────────────────────────────────────────────────┤
│  Runtimes Layer: packages/runtimes/{web, mobile}        │
├─────────────────────────────────────────────────────────┤
│  Foundation Layer: packages/foundation/*                │
└─────────────────────────────────────────────────────────┘
```

Dependency direction is downward only; the manifest encodes the exact allowed
edge set per package.

---

## Boundary principles

1. **Foundation has no React, no product, no UI, no runtime, no app code.**
   Foundation packages may only depend on other foundation packages and must
   not read app environment directly.
2. **Runtimes bridge platform globals to foundation abstractions.**
   Browser-specific construction (WebSocket factory, storage, environment
   interpretation) lives in `@notrelix/runtime-web`, never in foundation or
   feature code.
3. **UI packages own presentation only.** No product APIs, no QueryClient, no
   transport clients, no auth/session singletons.
4. **Product cores stay platform neutral.** Product state packages must not
   import UI implementation packages or notification side-effect libraries;
   result/error data flows to the web adapter, which owns presentation.
5. **Product testing packages are verification-only.** No production app or
   web-production package may import a `*-testing` package.
6. **Features consume injected capabilities.** Features must not create global
   API clients, QueryClient instances, or WebSocket connections.
7. **Apps compose, not implement.** `apps/web` is the production composition
   root; explicit composition only, no DI container or service locator.
8. **Package public entry points are the boundary.** Deep imports
   (`@notrelix/foo/src/...`) are forbidden.

The generated table documents the exact allowed internal imports for each
package; the checker additionally enforces the forbidden-import matrix (see
`tooling/dependency-rules/src/forbidden-imports.ts`) and folder-level purity
rules (`check-folder-boundaries.ts`).

---

## Enforcement

Run the full closed-world architecture gate:

```bash
pnpm check:architecture
```

This executes, in order:

1. declared-dependency validation against the manifest;
2. closed-world preflight (package set equality, stable violation codes) plus
   AST import enforcement;
3. folder boundary purity checks;
4. architecture-doc drift validation against the generated table.

Stable preflight violation codes: `UNREGISTERED_PACKAGE`,
`STALE_PACKAGE_POLICY`, `MISSING_PACKAGE_PATH`, `PACKAGE_NAME_MISMATCH`,
`DUPLICATE_PACKAGE_NAME`, `DUPLICATE_PACKAGE_PATH`, `UNKNOWN_ALLOWED_IMPORT`,
`SELF_IMPORT_POLICY`, `DUPLICATE_ALLOWED_IMPORT`.

---

## Rationale

1. **Foundation has no React** — platform independence and testability.
2. **Core has no UI** — separates business logic from presentation.
3. **No cross-runtime imports** — web code cannot leak into mobile and vice versa.
4. **Features are isolated** — each feature is independently developable.
5. **Apps compose, not implement** — composition stays explicit and auditable.
6. **Closed-world governance** — a missing manifest entry is a build failure,
   so no package can silently escape dependency enforcement.

---

## Change process

Adding, removing, or renaming a workspace package requires updating
`architecture-manifest.ts` in the same change, then regenerating the boundary
table (`docs:generate`). Allow-list broadening is an architecture-change PR,
not an incidental edit.

See also:

- [`dependency-model.md`](./dependency-model.md)
- [`application-composition.md`](./application-composition.md)
- [`freeze-governance.md`](./freeze-governance.md)
