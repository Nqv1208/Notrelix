# ADR-002: Package Manager

**Date:** 2026-07-12
**Status:** Accepted

## Context

The monorepo needs a package manager that supports:

- Workspace protocol (`workspace:*`)
- Strict dependency boundaries
- Frozen lockfile for CI
- Turborepo integration

## Decision

Use **pnpm** as the package manager.

- Version: pnpm@10.0.0
- Single lockfile at root
- No app-level lockfiles

## Consequences

- Strict dependency isolation by default
- Faster installs than npm/yarn
- Better monorepo support
- CI uses `pnpm install --frozen-lockfile`
