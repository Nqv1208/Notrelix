---
title: "Web Host Contract"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Web Host Contract

The web app is a Vite composition host. It integrates browser runtime, routing, providers and product/feature web slices without owning their business semantics.

## FE-WEB-101 — Browser capability enters through web runtime/host

URL/history, DOM, browser storage, visibility/connectivity and browser-only integrations stay in app-web or runtime-web. Product core/state must remain importable without DOM assumptions when defined as host-neutral.

## FE-WEB-102 — Route shell is thin

Route modules select screens, loaders/guards and host layout. They do not duplicate product queries/mutations merely because routing provides params. Convert params to typed capability inputs and delegate to owned state/adapters.

## FE-WEB-103 — Deep-link state is explicit

Resource identifiers encoded in URLs are validated and scoped through normal authorization/query paths. URL presence never proves access. Shareable filter/view state should have a versioned/validated representation when persisted in URLs; do not deserialize arbitrary JSON into trusted product configuration.

## FE-WEB-104 — SSR assumptions are not introduced accidentally

The current web host is Vite/client-oriented. Code must not quietly depend on server-rendering lifecycle or Node globals unless an explicit architecture decision introduces that runtime. Marketing Next.js concerns stay in the marketing host.

## Operational quality

Chunk boundaries/lazy loading should follow user journeys and product packages, not microscopic components. Error boundaries isolate recoverable host/product failures. Browser observability must preserve correlation context without logging secrets or sensitive payloads.
