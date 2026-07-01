-- =============================================================================
-- 001_roles.sql — Create PostgreSQL roles for Notrelix V5 RLS
-- =============================================================================
-- Idempotent. Safe to run multiple times.
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
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_admin') THEN
        CREATE ROLE notrelix_admin NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notrelix_owner') THEN
        CREATE ROLE notrelix_owner NOLOGIN;
    END IF;
END $$;
