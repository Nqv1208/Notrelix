# ADR-001: Framework Split

**Date:** 2026-07-12
**Status:** Accepted

## Context

The Notrelix frontend needs to serve multiple clients:

- Public marketing website (SEO-critical)
- Authenticated product web app
- Future mobile app

## Decision

Split into three separate apps:

| App              | Framework                      | Purpose                |
| ---------------- | ------------------------------ | ---------------------- |
| `apps/marketing` | Next.js App Router             | SEO, SSG, public pages |
| `apps/web`       | Vite + React + TanStack Router | Product SPA            |
| `apps/mobile`    | Expo / React Native            | Future mobile          |

## Consequences

- Marketing gets SSG/SSR for SEO
- Product app gets fast SPA with Vite
- Mobile can share packages with web
- Each app has its own dependency tree
