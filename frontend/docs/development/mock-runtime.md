---
document_id: FE-DEV-MOCK-RUNTIME
document_type: development-guide
status: active
owner: frontend-platform
applies_to:
  - frontend-web-development
  - frontend-mock-runtime
evidence:
  - frontend/apps/web/src/dev/mock-runtime/
  - frontend/playwright.mock.config.ts
  - frontend/e2e/mock/
review_on:
  - mock-runtime-contract-change
  - mock-scenario-change
  - mock-browser-gate-change
---

# Web mock runtime

The WebApp can run without a backend by selecting an app-owned transport adapter at the host composition seam.

```text
UI -> hooks -> feature adapter -> NotrelixClient -> mock handler registry -> MockStore
```

Start it with `pnpm dev:web:mock`. Optional configuration is `VITE_MOCK_PERSONA`, `VITE_MOCK_SCENARIO`, and `VITE_MOCK_LATENCY_MS`. Invalid values fail at startup. Production environment validation rejects `VITE_MOCK_API=true`.

The transport is closed-world. An operation without a registered handler throws `MockUnhandledOperationError`; it never falls through to real HTTP. Realtime uses an in-process adapter and never creates a browser WebSocket.

The currently certified surface covers auth profile; workspace list/detail/create/update and official invitation reads/acceptance; Account profile; Notifications list/read/read-all; representative Work Management board/item operations; Documents page/block operations; global search composition; all six deterministic scenarios; protected workspace route reload; and HTTP/auth-refresh/WebSocket isolation.

The mock accessibility test remains enabled and currently reports the existing global primary-color contrast as `CERTIFICATION-PENDING`. The mock runtime does not alter product theme tokens to make that gate pass.

Workspace views/members/activity/invitation-management, account preferences/security, notification extensions, and Docs favorite behavior still marked pending-backend in their owning adapters are not promoted to invented transport contracts here. Their producer requirements are tracked in the v2 contract-gap register.

Run the browser property gate with `pnpm e2e:mock`.
