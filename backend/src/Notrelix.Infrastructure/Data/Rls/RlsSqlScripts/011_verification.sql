-- =============================================================================
-- 011_verification.sql — RLS verification queries
-- =============================================================================

-- 1. Legacy/stale tables must not exist in Schema V2.
WITH forbidden(schema_name, table_name) AS (
    VALUES
        ('collab','notifications'),
        ('collab','notification_preferences'),
        ('collab','notification_deliveries'),
        ('collab','unread_counters'),
        ('collab','activity_logs'),
        ('audit','activity_logs'),
        ('governance','audit_logs'),
        ('governance','security_events'),
        ('governance','audit_retention_policies'),
        ('governance','member_role_assignments'),
        ('automation','outbox_messages'),
        ('ops','processed_events'),
        ('billing','workspace_feature_usages'),
        ('identity','api_tokens')
)
SELECT f.schema_name, f.table_name, 'FORBIDDEN_TABLE_EXISTS' AS issue
FROM forbidden f
JOIN information_schema.tables t
  ON t.table_schema = f.schema_name
 AND t.table_name = f.table_name
WHERE t.table_type = 'BASE TABLE'
ORDER BY f.schema_name, f.table_name;

-- 2. Tables in target schemas with RLS disabled.
SELECT n.nspname AS schema_name, c.relname AS table_name, 'RLS_DISABLED' AS issue
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'r'
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND c.relrowsecurity = false
ORDER BY n.nspname, c.relname;

-- 3. RLS-enabled tables without policies.
SELECT n.nspname AS schema_name, c.relname AS table_name, 'NO_POLICY' AS issue
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
LEFT JOIN pg_policies p
  ON p.schemaname = n.nspname
 AND p.tablename = c.relname
WHERE c.relkind = 'r'
  AND n.nspname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
  AND c.relrowsecurity = true
GROUP BY n.nspname, c.relname
HAVING count(p.policyname) = 0
ORDER BY n.nspname, c.relname;

-- 4. Policy count by schema.
SELECT schemaname, count(*) AS policy_count
FROM pg_policies
WHERE schemaname IN ('account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops')
GROUP BY schemaname
ORDER BY schemaname;
