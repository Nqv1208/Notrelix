# Notrelix Enterprise Schema V2 — Verified RLS Policy Pack

It is intentionally split by responsibility. Do **not** use the previous single `rls-policy-pack.sql` because it still references stale/removed tables such as:

- `governance.audit_retention_policies`
- `governance.member_role_assignments`
- `billing.workspace_feature_usages`
- `identity.api_tokens`

## Apply order

`000_apply_order.sql` is the authoritative apply order (001 to 029). Use it, or embed the files in exactly that order.

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
