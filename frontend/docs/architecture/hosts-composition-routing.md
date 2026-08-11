# Hosts, Composition, and Routing

## Scope

Host responsibilities, provider bootstrap, environment ownership, routing,
authentication/session composition, authorization UX and web/mobile/marketing
differences.

## Responsibility / Ownership

Apps compose runtime services and routes. Reusable business behavior stays in
foundation/runtime/product/feature packages according to the manifest.

## Current Architecture

Web uses Vite and TanStack Router. Mobile uses Expo/React Native. Marketing uses
Next and remains isolated from core product runtime.

## Normative Contracts

- Hosts own provider bootstrap, environment handoff and route trees.
- Runtime packages create host services; provider owners dispose services they create.
- Routing is host-owned and delegates reusable behavior to package owners.
- Authentication/session composition belongs at the app/runtime boundary.
- Frontend authorization UX does not replace backend Application authorization.
- Web, mobile and marketing may differ in runtime concerns but not product semantics.

## Allowed Design

Host-only adapters for storage, environment, navigation, auth/session and
platform APIs.

## Forbidden Design

No reusable product logic inside host routes, mobile imports of web runtime/UI,
marketing imports of authenticated product runtime, or route guards as security
proof.

## Failure Modes

Service lifecycle leaks, stale workspace cache/realtime subscriptions, host route
code becoming a product module.

## Change Impact Rules

Provider, route, auth/session, app-shell or workspace lifecycle changes require
host-specific tests and affected product/runtime tests.

## Executable Evidence / Tests / Gates

`apps/web`, `apps/mobile`, `apps/marketing`, runtime packages,
`pnpm test:web`, `pnpm test:mobile`, and E2E where relevant.

## Related ADRs

`FE-ADR-001`, `FE-ADR-005`.

## Related Source Manifests

Architecture manifest and host package manifests.

## Explicit Non-responsibilities

This document does not own backend authorization, product data semantics or UI
token design.
