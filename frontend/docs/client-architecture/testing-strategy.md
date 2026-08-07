# Testing Strategy Specification

> **Test Topology, Vitest Workspaces, AST Architecture Verification & Playwright E2E**

---

## 1. Test Topology

Kiểm thử frontend được chia làm 3 tầng:
1. **Node Unit Tests (`pnpm test:node`):**
   - Environment: Node.js.
   - Target: `packages/foundation/kernel`, `packages/foundation/realtime`, pure core models, DTO mappers.
2. **Web Component & Integration Tests (`pnpm test:web`):**
   - Environment: JSDOM.
   - Target: React hooks, UI web components, state controllers, provider integration.
3. **Production Playwright E2E (`pnpm e2e`):**
   - Environment: Desktop Chrome against Vite production preview build (`http://127.0.0.1:4173`).
   - Target: Auth flow, workspace isolation, session expiration, realtime connection, error boundaries.

---

## 2. Architecture Checker

`pnpm check:architecture` executes AST-based verification via TypeScript Compiler API to enforce:
- Zero deep imports (`@notrelix/*/src/*`).
- Zero direct environment reads outside approved host adapters.
- Zero unauthorized client creation calls (`createNotrelixClient`, `new WebSocket`, `new QueryClient`).
