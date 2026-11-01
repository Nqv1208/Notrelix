-- =============================================================================
-- 020_support_role_no_content.sql — The support login holds no credential and no tenant content
-- =============================================================================
-- Removes access only; runs after 005, which re-grants SELECT on every apply.
-- =============================================================================

-- 2. Support is designed per case type (billing, account/login, feature errors, data recovery). Until each
-- is designed the login reads only the shared catalogs: nothing tenant-owned and never a credential.
DO $$
DECLARE
    s text;
    t record;
    keep text[] := ARRAY['billing.plans', 'billing.plan_prices', 'billing.plan_limits', 'automation.automation_templates'];
BEGIN
    FOR s IN SELECT nspname FROM pg_namespace WHERE nspname NOT LIKE 'pg\_%' AND nspname NOT IN ('information_schema', 'public')
    LOOP
        EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM notrelix_support_readonly', s);
    END LOOP;

    EXECUTE 'GRANT SELECT ON billing.plans, billing.plan_prices, billing.plan_limits, automation.automation_templates TO notrelix_support_readonly';

    -- The open "support can see every row" policy goes too, so a grant added by mistake later stays closed.
    FOR t IN
        SELECT schemaname, tablename FROM pg_policies
         WHERE policyname = 'p_support_select' AND (schemaname || '.' || tablename) <> ALL (keep)
    LOOP
        EXECUTE format('DROP POLICY p_support_select ON %I.%I', t.schemaname, t.tablename);
    END LOOP;
END $$;
