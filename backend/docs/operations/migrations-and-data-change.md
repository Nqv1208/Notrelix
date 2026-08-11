# Migrations and Data Change

## Scope

EF migrations, pending-model-change policy, expand/contract deployment,
destructive changes, backfills, RLS changes, indexes, data lifecycle, rollback
and verification.

## Responsibility / Ownership

Infrastructure owns schema migrations and persistence shape. Domain/Application
own the business meaning being persisted.

## Current Architecture

The EF model and migration chain are the schema authority. Manual baseline SQL
documents are historical only.

## Normative Contracts

- Change Domain/Application contract first, then persistence mapping/migration.
- Pending model changes must be resolved intentionally, not suppressed.
- Prefer expand/contract for incompatible public or persisted changes.
- Destructive changes require preflight counts, deterministic mapping,
  rollback/roll-forward plan and explicit approval.
- Backfills fail on unknown legacy state instead of guessing defaults.
- RLS changes are reviewed with tenant-scope tests and session context behavior.
- Index changes must match query/access patterns and tenant predicates.
- Data lifecycle/retention follows product/security policy.
- Deployment ordering must preserve old/new reader and writer compatibility when
  staged rollout is required.

## Allowed Design

- Additive columns/tables/indexes with explicit follow-up cleanup.
- Deterministic data migration scripts tied to reviewed migrations.
- Integration tests for converters, JSON versioning and RLS policies.

## Forbidden Design

- Dropping or repurposing persisted state before all readers/writers are known.
- Guessing migration values for unknown legacy state.
- Treating database constraints as the only business invariant.
- RLS policy generation based solely on matching column names.

## Failure Modes

- Schema drift hidden until deploy.
- Backfill corrupts tenant scope or version semantics.
- Rollback cannot read newly written data.

## Change Impact Rules

Any persisted meaning change requires migration review, affected reader/writer
inventory, data compatibility plan and tests appropriate to the blast radius.

## Executable Evidence / Tests / Gates

- EF migrations/configuration source
- Infrastructure and integration tests
- Pending-model-change checks where configured

## Related ADRs

- `../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md`

## Related Source Manifests

Infrastructure project and migration files.

## Non-responsibilities

This document does not decide product lifecycle or API compatibility by itself.
