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
