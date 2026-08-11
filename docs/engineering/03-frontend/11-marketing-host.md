---
title: "Marketing Host Contract"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Marketing Host Contract

The marketing app is a public-facing Next.js host with different runtime/performance/SEO concerns from the authenticated product hosts.

## FE-MKT-101 — Marketing is not authenticated product architecture

Public site pages, SEO, content/landing surfaces and acquisition flows remain isolated from workspace product state. Do not import product state/realtime/runtime-web internals into marketing simply to reuse a component.

## FE-MKT-102 — Shared UI is intentional

Marketing may consume stable shared design tokens/primitives when compatible with its rendering model. Product-specific authenticated UI should not become a marketing dependency. Duplicate small presentation code rather than pulling a large product dependency across the boundary.

## FE-MKT-103 — Server/client boundary stays explicit

Next.js server/client components and data fetching follow marketing needs. Browser-only code is marked/isolated appropriately; secrets/private service credentials never enter client bundles. Public API calls still use approved contracts and error handling.

## FE-MKT-104 — Performance and metadata are product requirements

Critical landing pages should minimize unnecessary client JavaScript, preserve metadata/canonical URL behavior, usable semantic HTML and accessibility. Third-party analytics/marketing scripts require privacy/security review and must not become a hidden dependency for core page rendering.
