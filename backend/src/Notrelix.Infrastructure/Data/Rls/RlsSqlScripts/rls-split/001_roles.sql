-- =============================================================================
-- 001_roles.sql — Runtime roles for Notrelix Schema V2 RLS
-- =============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_app') THEN
        CREATE ROLE notrelix_app NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_auth') THEN
        CREATE ROLE notrelix_auth NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_worker') THEN
        CREATE ROLE notrelix_worker NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_support_readonly') THEN
        CREATE ROLE notrelix_support_readonly NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_migrator') THEN
        CREATE ROLE notrelix_migrator NOLOGIN;
    END IF;
END $$;
