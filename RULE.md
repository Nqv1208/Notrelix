# RULE.md — Notrelix Repository Constitution

This file defines repository-wide invariants. Backend-specific rules live under
`backend/docs/`; frontend-specific rules live under `frontend/docs/`. Product
semantics live in `PRODUCT.md`; visual/product design principles live in
`DESIGN.md`.

## NRX-001 Product Semantics Outrank Representation

Ownership, lifecycle, invariants, tenant scope, authorization meaning and
user-visible behavior must be defined from the owning product capability before
storage, transport, event, route or UI representation is chosen.

Evidence: product docs, behavior tests, contract/migration review when public or
persisted meaning changes.

## NRX-002 Architecture Boundaries Are Executable Contracts

Project/package dependency direction, public exports, bounded-context ownership,
runtime separation, composition boundaries and generated contracts must be
respected by production code.

Evidence: backend architecture tests, frontend dependency-rules checks,
compilation, generated contract checks and package/project manifests.

## NRX-003 Tenant Isolation Is Correctness and Security

Account/workspace-scoped reads, writes, cache entries, query keys, search/index
records, realtime subscriptions, events/messages, jobs, persistence operations,
audit facts and projections must carry enough immutable scope to prevent
cross-tenant observation or mutation.

Evidence: authorization tests, RLS/integration tests, cache/query/realtime scope
tests and security review for cross-tenant workflows.

## NRX-004 Backend Authorization Is Authoritative

Every protected command and query must be authorized server-side at the
Application/public-use-case boundary. Frontend guards improve UX only.

Evidence: Application/API authorization tests, tenant/resource resolution tests
and frontend permission tests for UX behavior only.

## NRX-005 Pure Business Layers Stay Deterministic and Provider-free

Domain and framework-neutral foundation code must receive external facts through
explicit boundaries. They must not reach outward for time, current user, random
input, providers, network, filesystem or runtime environment.

Evidence: pure unit tests and dependency/determinism gates.

## NRX-006 Shared Code Requires Stable Ownership

An abstraction may move to shared/foundation/common only when meaning,
lifecycle, dependency direction and change pressure are compatible across
consumers.

Evidence: ownership review, dependency/public-export gates and canonical docs
when a shared abstraction becomes architectural.

## NRX-007 Source, Generated Evidence and Docs Must Stay Coherent

Generated inventories are evidence, not rationale. Authored docs explain durable
architecture. ADRs explain historical decisions. Roadmaps, freeze specs, audits
and migration trackers are not active architecture authorities.

Evidence:

```bash
make docs-check
```

## Change Rule

A repository-wide invariant may change only through an intentional
product/architecture decision that updates the owning canonical docs, affected
source/consumers, tests/gates, compatibility or migration plan, and ADR when the
decision is consequential.
