---
document_id: BE-ACCESS-FACTS-EVIDENCE
document_type: operations
status: active
owner: backend-runtime-operations
applies_to:
  - backend
  - backend-access-control
  - backend-performance
evidence:
  - backend/src/Notrelix.Infrastructure/Data/Authz/PostgresAccessFactsProvider.cs
  - backend/src/Notrelix.Infrastructure/Data/Migrations/20260702093805_SchemaV2Baseline.cs
review_on:
  - access-facts-query-shape-change
  - schema-v3-baseline-change
---

# AccessFacts Query — EXPLAIN Evidence

## Scope

This record captures query-plan evidence for the canonical AccessFacts SQL used
by `PostgresAccessFactsProvider` on every permission-gated request. It supports
the pipeline-freeze decision that **no additional index is added** for this
query path.

## Method

```text
statement   : PostgresAccessFactsProvider.Sql (verbatim)
plan        : EXPLAIN (ANALYZE, BUFFERS)
schema      : consolidated migration baseline 20260702093805_SchemaV2Baseline
bindings    : representative UUIDs, resource_type = 'work-management.board-item',
              action = 'UpdateBoardItem', feature_code = NULL
environment : ephemeral PostgreSQL testcontainer (pipeline-freeze execution,
              IA-TST-X-PERF evidence capture)
```

## Observed plan

The planner collapses every scalar subselect into a single result node — the
whole statement executes as indexed point lookups with no sequential or bitmap
scan:

```text
Result  (cost=92.37..92.38 rows=1 width=458)
        (actual time=0.050..0.051 rows=1 loops=1)
```

Total execution time ≈ **0.05 ms** per evaluation on the migrated baseline.

## Supporting indexes (existing)

Every subselect is covered by an existing unique/btree index:

```text
identity.users                    pk_users (id); ux_users_normalized_email
account.accounts                  pk_accounts (id)
account.account_members           idx_account_members_account_user (account_id, user_id) UNIQUE
workspace.workspaces              pk_workspaces (id)
workspace.workspace_members       idx_workspace_members_workspace_user (workspace_id, user_id) UNIQUE
governance.resource_permissions   idx_resource_permissions_resource (resource_type, resource_id);
                                  idx_resource_permissions_subject_id (subject_id)
governance.permission_rules       idx_permission_rules_workspace_id (workspace_id);
                                  idx_permission_rules_scope_action (scope_type, action);
                                  idx_permission_rules_status (status)
billing.subscriptions             pk_subscriptions (id)
billing.entitlements              idx_entitlements_account_id (account_id)
billing.feature_usage_ledger      pk_feature_usage_ledger (id)
```

## Decision

No index is added for the AccessFacts path. The measured plan contains no scan
node to eliminate and per-request latency is dominated elsewhere in the
pipeline.

Re-evaluate when any of the following change:

```text
the facts SQL gains a non-point predicate (range/list scan)
a table above loses its covering unique/btree index
per-request p95 attribution shows this statement above noise
schema v3 changes tenant scoping of the probed tables
```


## Large-tenant evidence (freeze file 04 §8)

Captured from `PipelineFreezeEvidenceTests` on real PostgreSQL with a seeded
representative tenant:

```text
workspace_members   : 10,000 rows (single workspace)
permission_rules    : 10,000 rows (matching workspace)
plan                : Result  (cost=45.85..45.87) (actual time=12.145..12.224)
sequential scans    : none on workspace_members / permission_rules / resource_permissions
dominant cost       : jsonb aggregation of matching permission rules (bounded by
                      predicate selectivity, not table cardinality)
```

Conclusion: the one-command AccessFacts shape remains index-backed at 10k
cardinality; no additional index is justified by this evidence.
