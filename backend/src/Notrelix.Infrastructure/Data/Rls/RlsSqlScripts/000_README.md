# Notrelix Enterprise Schema V2 — Verified RLS Policy Pack

This pack was regenerated against the uploaded baseline:

`notrelix-enterprise-schema-v2-clean-baseline(1).sql`

It is intentionally split by responsibility. Do **not** use the previous single `rls-policy-pack.sql` because it still references stale/removed tables such as:

- `governance.audit_retention_policies`
- `governance.member_role_assignments`
- `billing.workspace_feature_usages`
- `identity.api_tokens`

## Apply order

Use `000_apply_order.sql` or embed the files in the same order:

1. `001_roles.sql`
2. `002_context_helpers.sql`
3. `003_authz_access_helpers.sql`
4. `004_policy_runtime.sql`
5. `005_grants.sql`
6. `006_policies_identity.sql`
7. `007_policies_platform.sql`
8. `008_policies_workspace_scoped_domain.sql`
9. `009_policies_notifications_activity_search.sql`
10. `010_policies_events_messaging_audit_ops.sql`
11. `011_verification.sql`

## Important fixes

- No policy references `governance.audit_retention_policies`.
- No policy references legacy Schema V5 tables removed from Schema V2.
- `authz.access_grants.expires_at` is optional in helper logic. If your current migration does not have it, the helpers still work.
- `authz.access_grants.membership_status` is optional in helper logic. If present, only `Active` grants are accepted.
- This pack does not create or alter business tables.
- This pack does not add `uuid-ossp` or `uuid_generate_v4()`.

## Required runtime context

Application code must set these PostgreSQL settings per request/transaction:

```sql
SELECT set_config('app.current_user_id', '<uuid>', true);
SELECT set_config('app.current_account_id', '<uuid>', true);
SELECT set_config('app.current_workspace_id', '<uuid>', true);
SELECT set_config('app.request_scope', 'app', true);
```

Worker:

```sql
SELECT set_config('app.request_scope', 'worker', true);
```

Support readonly:

```sql
SELECT set_config('app.request_scope', 'support', true);
```
