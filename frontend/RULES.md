# Frontend Development Rules & Hard Boundaries

> **Hard Rules for Frontend Monorepo Development**

Refer to [Client Architecture Specification](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/README.md) for full details.

---

## Hard Rules

1. **No Singleton Mutations:** `NotrelixClient`, `QueryClient`, and `RealtimeClient` are created ONLY in `apps/web/src/composition` or `runtimes/web`.
2. **Framework Purity:** `core` packages must NEVER import React, DOM, TanStack Query, or UI primitives.
3. **No Direct Env Reads:** Do not read `process.env` or `import.meta.env` inside packages; use host environment adapters.
4. **No Deep Imports:** Always import from package entrypoints (e.g. `@notrelix/kernel`), never from `@notrelix/kernel/src/*`.
5. **No Feature-to-Runtime Imports:** `@notrelix/features-*` must NEVER import `@notrelix/runtime-web`.
