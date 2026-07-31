# Dependency Model & Boundary Rules

> **Canonical Layering Rules for Notrelix Monorepo Packages**

---

## 1. Package Layers

1. **Foundation Layer (`packages/foundation/*`):**
   - Pure TypeScript, low-level platform primitives, HTTP client, protocol validator, generic query engine.
2. **Runtime Layer (`packages/runtimes/*`):**
   - Host platform adapters (browser WebSocket, connectivity listeners, session event bus, telemetry).
3. **UI Layer (`packages/ui/*`):**
   - Design tokens, CSS primitives, accessible UI components (Button, Modal, Input).
4. **Product Layer (`packages/product/*`):**
   - Large domain-specific modules (Work Management, Docs, Automation). Divided into `core`, `state`, `web`, `testing`.
5. **Feature Layer (`packages/features/*`):**
   - Focused business feature packages (Auth, Workspace, Billing, Governance, Notifications, etc.).
6. **Application Layer (`apps/*`):**
   - Composition root, router, pages, global providers.

---

## 2. Directional Dependency Matrix

```text
apps/web
  ├──> features/* (web/react)
  ├──> product/* (web/state)
  ├──> runtimes/web
  ├──> ui/web
  └──> foundation/*

packages/features/*
  ├──> foundation/*
  └──> ui/*
  (NEVER import from apps/* or runtimes/web)

packages/product/*
  ├──> foundation/*
  └──> ui/*

packages/runtimes/web
  └──> foundation/*

packages/foundation/*
  └──> foundation/kernel
```

---

## 3. Strict Prohibitions

- **No Reverse Dependencies:** Lower layers (`foundation`, `runtimes`, `core`) must NEVER import from higher layers (`apps`, `features`, `ui/web`).
- **No Feature to Runtime Imports:** `@notrelix/features-*` must NEVER import `@notrelix/runtime-web`.
- **No Deep Imports:** Packages must consume public exports via package entrypoints, never via `@notrelix/pkg/src/*`.
