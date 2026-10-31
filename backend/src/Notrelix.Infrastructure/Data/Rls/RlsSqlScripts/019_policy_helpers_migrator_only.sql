-- =============================================================================
-- 019_policy_helpers_migrator_only.sql — Policy-DDL helpers are callable by the migrator only
-- =============================================================================
-- Removes access only; runs after 004/005, which create (and re-grant) on every apply.
-- =============================================================================

-- The helpers below are SECURITY DEFINER and edit policies. A function carries EXECUTE for PUBLIC unless it
-- is revoked, so any runtime login could add a policy of its own (for example an unconditional SELECT on
-- identity.users). Only the migrator may run them; the apply itself runs as the owner.
DO $$
DECLARE
    f record;
BEGIN
    FOR f IN
        SELECT p.oid::regprocedure AS sig
          FROM pg_proc p
          JOIN pg_namespace n ON n.oid = p.pronamespace AND n.nspname = 'ops'
         WHERE p.proname IN ('create_policy', 'drop_all_policies_for_table', 'enable_rls_for_table')
            OR p.proname LIKE 'apply\_%'
    LOOP
        EXECUTE format(
            'REVOKE ALL ON FUNCTION %s FROM PUBLIC, notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly',
            f.sig);
        EXECUTE format('GRANT EXECUTE ON FUNCTION %s TO notrelix_migrator', f.sig);
    END LOOP;
END $$;
