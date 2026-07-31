# ADR-001: Frontend Web Platform Freeze Scope & Architecture Boundaries

**Status:** Accepted  
**Date:** 2026-07-30  
**Context:** Notrelix Web Application (`apps/web`) is preparing for multi-team module feature expansion.

---

## Context

Previous architecture documentation contained outdated references to Next.js/Bun and mixed mobile/web requirements. A clear boundary is needed to freeze the web platform foundation (`frontend-web-platform-v1.0.0`).

---

## Decision

1. **Web Platform Target:** `apps/web` is standardized on Vite + React 19 + TanStack Router + TanStack Query v5 + pnpm workspace.
2. **Explicit Exclusions:** Mobile (`apps/mobile`) and Marketing (`apps/marketing`) are excluded from this web freeze certificate.
3. **Single Composition Root:** System singletons (`NotrelixClient`, `QueryClient`, `RealtimeClient`) are created exclusively in `apps/web/src/composition` and `packages/runtimes/web`.
4. **Enforcement:** Architecture rules are enforced via TypeScript AST inspection during CI gates.

---

## Consequences

- Feature teams can expand bounded contexts independently without altering platform singletons.
- Mobile and Web progress independently without blocking Web platform freeze sign-off.
