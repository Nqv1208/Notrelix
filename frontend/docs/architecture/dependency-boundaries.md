# Dependency Boundaries

## Scope

Closed-world package architecture, public exports, deep import policy, core and
runtime purity, mobile-native safety and generated package-boundary evidence.

## Responsibility / Ownership

`tooling/dependency-rules/src/architecture-manifest.ts` is the executable source
of truth for the package universe and allowed internal imports.

## Current Architecture

Every source-bearing app/package directory with a package manifest must be
registered exactly once in the architecture manifest. The generated inventory is
`../generated/package-boundaries.md`.

## Normative Contracts

- Closed-world set equality is required.
- Internal imports must follow the manifest allow-list.
- Public exports protect package contracts; deep imports are forbidden.
- Foundation/core packages stay framework-neutral unless named otherwise.
- Runtime-specific APIs stay inside host/runtime packages.
- Mobile production packages reject `react-dom`, `react-dom/*`,
  `@notrelix/ui-web`, `@notrelix/runtime-web`, direct web-app imports and DOM
  JSX intrinsics.
- Generated package boundaries are evidence, not design rationale.

## Allowed Design

Add packages through generator/manifest updates plus docs drift checks. Keep
public exports narrow and runtime adapters platform-specific.

## Forbidden Design

No deep imports, duplicated matrices, foundation dumping-ground moves, or
hard-coded mobile package lists.

## Failure Modes

Unregistered packages, stale generated docs or non-AST mobile DOM checks.

## Change Impact Rules

Manifest, generator, package exports or dependency-rule changes require
`check:architecture`, `check:architecture-docs`, generator tests and relevant
package tests.

## Executable Evidence / Tests / Gates

- `tooling/dependency-rules/src/architecture-manifest.ts`
- `tooling/dependency-rules/src/generate-architecture-docs.ts`
- `docs/generated/package-boundaries.md`
- `pnpm check:architecture`
- `pnpm check:architecture-docs`

## Related ADRs

`FE-ADR-002`, `FE-ADR-003`, `FE-ADR-004`.

## Related Source Manifests

`pnpm-workspace.yaml`, package manifests and architecture manifest.

## Explicit Non-responsibilities

This document does not decide product ownership or backend API compatibility.
