# ADR-004: No Next.js in Packages

**Date:** 2026-07-12
**Status:** Accepted

## Context

Next.js is a framework-specific dependency. Packages should be framework-neutral to be reusable across web (Vite) and mobile (Expo).

## Decision

**Packages must not depend on `next`.**

Only `apps/marketing` may use Next.js.

## Rules

- `packages/*` cannot have `next` in dependencies
- `packages/*` cannot import `next/*`
- `apps/web` cannot import `next/*`
- `apps/mobile` cannot import `next/*`
- Routing/navigation must go through `@notrelix/platform` adapters

## Consequences

- Packages are framework-neutral
- Web app can use Vite without Next.js contamination
- Mobile app can share packages without web framework dependencies
- Boundary enforced by `tooling/dependency-rules/`
