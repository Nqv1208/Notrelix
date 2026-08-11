---
title: "Database Migrations and RLS"
document_class: handbook
normative: true
owner: backend-data
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/data
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Database Migrations and RLS

## BE-MIG-101 — EF model drift is resolved with a migration or intentional model correction

`PendingModelChangesWarning` is evidence that model and migration history differ. Do not suppress the warning as a normal fix.

## Safe schema evolution

For breaking/deployed changes prefer:

```text
expand schema
→ deploy compatible writer/reader
→ backfill/migrate
→ switch authoritative reads/writes
→ verify
→ contract/remove old schema later
```

Direct destructive rename/drop is allowed only when deployment/rollback/data-loss analysis proves it safe.

## Migration contents

Review tables/columns/defaults/nullability, constraints, indexes, FK delete behavior, concurrency tokens, RLS policies/session functions, backfill cost and lock behavior.

## BE-RLS-101 — Tenant schema and RLS agree

Workspace/account-scoped rows expose the scope required by the RLS policy/query model. Do not rely on an indirect join for every security boundary when the established model requires direct scope columns.

## Consumer transactions

When an integration consumer uses database RLS, apply tenant/RLS session **inside the same transaction** that performs idempotency claim and consumer business work. Setting only in-memory tenant context is not sufficient.

## Migration testing

At minimum for material migrations:

- migrate empty/current schema;
- upgrade representative prior schema if supported;
- verify RLS policy after migration;
- verify data backfill/constraints/indexes;
- confirm EF model has no pending drift;
- document rollback/forward recovery if destructive.
