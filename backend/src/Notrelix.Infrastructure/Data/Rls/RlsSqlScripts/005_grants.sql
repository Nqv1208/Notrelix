-- =============================================================================
-- 005_grants.sql — Schema/table/function privileges
-- =============================================================================

DO $$
DECLARE
    s text;
BEGIN
    FOREACH s IN ARRAY ARRAY['account', 'identity', 'workspace', 'governance', 'authz', 'work', 'docs', 'collab', 'automation', 'integration', 'billing', 'reporting', 'search', 'notifications', 'activity', 'analytics', 'events', 'messaging', 'audit', 'ops']
    LOOP
        IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = s) THEN
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly', s);
            EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO notrelix_migrator', s);
            EXECUTE format('GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I TO notrelix_migrator', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_app', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_auth', s);
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO notrelix_worker', s);
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO notrelix_support_readonly', s);
        END IF;
    END LOOP;
END $$;
