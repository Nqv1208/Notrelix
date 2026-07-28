# Open Quality & Architectural Gates

The following gates must be 100% satisfied before creating tag `frontend-platform-v1.0`:

1. **API Client Instance Isolation (P0):** `refreshPromise` must be client-closure scoped with zero global/module-level mutable state.
2. **Deterministic Session Expiration (P0):** Event-driven single-flight expiration without time-based debounce timeouts or `window` CustomEvents.
3. **Realtime State Machine Transport (P0):** Injected socket factory, explicit state machine transitions, heartbeat pong timeout, LRU dedup with TTL, workspace filter isolation, and sequence gap recovery.
4. **Clean Feature Boundaries (P0):** Zero `@notrelix/runtime-web` imports inside `@notrelix/features-*` packages.
5. **Production E2E Pipeline (P0):** Playwright E2E running against production bundle preview in CI.
6. **AST Architecture Checker (P0/P1):** AST-based enforcement of layer boundaries, direct environment reads, client creation, and deep imports.
7. **Production Startup Smoke & Environment (P1):** Fail-fast startup validation with environment variable scanning on production bundle.
