---
document_id: FE-DEV-UI-CONSTRUCTION
document_type: development-guide
status: active
owner: frontend-platform
applies_to:
  - frontend-ui-development
  - pure-ui-verification
evidence:
  - frontend/tooling/testing/
  - frontend/tooling/storybook/web/
  - frontend/e2e/ui/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
review_on:
  - pure-ui-verification-change
  - storybook-evidence-change
  - ui-fixture-scenario-change
---

# UI Construction

Use this path when building governed Web presentation surfaces before application integration is ready:

```text
product semantics
-> pure presentation surface
-> deterministic fixture
-> deterministic scenario
-> local interaction controller
-> owner-local Storybook story
-> component interaction test
-> a11y/visual/network evidence
-> UI DONE
```

The pure surface receives typed props and callbacks. State/query/mutation hooks, auth/session state, router composition, API clients, mock backend transport, and realtime clients stay outside the registered pure entry.

Fixtures create one deterministic value or entity with explicit overrides. Scenarios compose complete renderable surface state from fixtures. Local interaction controllers clone scenario state and implement presentation callbacks only, such as create, move, rename, delete, open, or tab selection. They do not model HTTP status codes, retries, optimistic rollback, cache invalidation, auth refresh, persistence, RLS, or backend permission decisions.

Storybook is the shared renderer and discovery host. Product, feature, and UI stories live beside their owners and import owner-owned fixtures/scenarios/controllers. Story files are examples, not reusable data authority.

Use pure UI tests for presentation behavior. `renderPureUi` is the pure component harness; it must not install application providers. Registered pure entries are checked for forbidden transitive imports and story/component network access.

Application mock backend work is a separate integration lane. It can prove full-app offline flows, but it is not required evidence for UI DONE.

## Machine contract

Governed surfaces are registered in a `verification/ui-evidence.manifest.json` beside their owner. The manifest is the **manifest v2** contract: each surface declares `surfaceId`, one `pureEntry`, `coveredSources`, and `states`. Source classification, semantic state coverage, and state universes are enforced by the evidence schema; the enumerator and checks derive the inventory from this manifest.

Supported state universes are `data`, `form`, `navigation`, `shell`, `composition`, `feedback`, and `primitive` (see the UI evidence schema). A surface must account for every state its rules declare, and must not state a generic N/A without an explicit reason.

### Action contract

Enabled controls inside a registered pure entry must carry at least one of: `on*` callback, `submit`, `disabled`/`aria-disabled`, or link (`href`/`asChild`) semantics. `check:ui-actions` rejects dead enabled controls.

### FUI interaction case markers

Case-level interaction coverage is asserted from test names. Each declared interaction case must be satisfied by an exact passing marker; a zero-interaction surface is recorded with an explicit `N/A`.

### Responsive/theme target policy

Every visual story includes `desktop/light`. A `responsive` surface's `Default` story also includes `mobile/light` and `tablet/light`. A `themeAware` surface's `Default` story also includes `desktop/dark`. Snapshots are named `<storyId>--<viewport>--<theme>.png`; Linux Chromium is authoritative CI evidence. `check:ui-purity` and `check:ui-actions` are part of the `ui-foundation` CI job.

### Local UI validation

```bash
pnpm validate:ui
```

`validate:ui` is the UI-only lane. It runs architecture, purity, actions, fixture determinism, evidence, typecheck, lint, format, web tests, and the Storybook freeze. It deliberately does **not** run codegen, mock-contract, mock/real E2E, or backend/docker/database commands. Starting a backend/mock E2E is **not** required to reach UI DONE for a governed surface.
