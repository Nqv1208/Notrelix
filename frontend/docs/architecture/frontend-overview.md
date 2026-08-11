# Frontend Overview

## Scope

Durable frontend architecture for the Notrelix pnpm/Turborepo workspace.

## Responsibility / Ownership

Frontend owns client composition, typed contract consumption, server-state
caching, realtime reconciliation, design-system implementation and host-specific
runtime integration.

## Current Architecture

The workspace contains Vite web (`apps/web`), Expo mobile (`apps/mobile`) and
Next marketing (`apps/marketing`). Packages are grouped under
`packages/foundation`, `packages/runtimes`, `packages/ui`, `packages/product`,
`packages/features` and `tooling`.

## Normative Contracts

- Apps are composition roots for providers, routing, host runtime and shell.
- Product packages own reusable capability behavior.
- Feature packages own cross-product vertical features.
- Foundation packages are framework/runtime-neutral unless explicitly scoped.
- Runtime packages adapt host-specific services.
- UI web and UI mobile are separate platform implementations over shared tokens.
- Source-generated architecture boundaries are the exact package authority.
- Frontend does not own backend business authorization or persistence truth.

## Allowed Design

Host-specific code belongs in apps or runtime packages. Product-owned state/query
modules belong under product packages. Generated contract clients are consumed
through approved foundation/runtime boundaries.

## Forbidden Design

Do not park business logic in apps, put web-only dependencies in mobile
production packages, import Next.js outside approved host/marketing boundaries,
or maintain manual package inventories that duplicate the manifest.

## Failure Modes

Route composition becomes product ownership; mobile/web package boundaries drift
from generator output; freeze/roadmap artifacts become current architecture.

## Change Impact Rules

Package graph, host composition, runtime ownership, product/feature ownership or
generated contract changes require architecture checks and focused tests.

## Executable Evidence / Tests / Gates

`package.json`, `pnpm-workspace.yaml`, `turbo.json`,
`tooling/dependency-rules/src/architecture-manifest.ts`, and
`pnpm check:architecture`.

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

The architecture manifest and package manifests.

## Explicit Non-responsibilities

This document does not define UI visual philosophy, backend contracts or product
semantics by itself.
