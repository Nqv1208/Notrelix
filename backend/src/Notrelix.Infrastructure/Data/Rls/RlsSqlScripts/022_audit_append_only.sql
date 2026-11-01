-- =============================================================================
-- 022_audit_append_only.sql — The audit trail is append-only for the runtime logins
-- =============================================================================
-- Removes access only; runs after 005, which re-grants writes on every apply.
-- =============================================================================

-- 5. The audit trail is written once. The worker keeps its retention sweep.
DO $$
DECLARE
    t record;
BEGIN
    FOR t IN SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
              WHERE n.nspname = 'audit' AND c.relkind IN ('r', 'p')
    LOOP
        EXECUTE format('REVOKE UPDATE, DELETE, TRUNCATE ON audit.%I FROM notrelix_app, notrelix_auth, notrelix_support_readonly', t.relname);
    END LOOP;
END $$;
