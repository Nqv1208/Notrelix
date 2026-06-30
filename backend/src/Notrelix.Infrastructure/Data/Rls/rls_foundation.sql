-- =============================================================================
-- Notrelix Enterprise RLS Foundation SQL
-- Provisioned separately from EF migrations to keep migration clean and avoid
-- hand-editing generated migration files.
--
-- This script is idempotent and safe to run on every startup after migration.
-- It provisions:
--   1. PostgreSQL roles (notrelix_app, _auth, _worker, _support_readonly, _migrator, _admin)
--   2. ops.set_updated_at() trigger function
--   3. ops.current_user_id / current_workspace_id / current_request_scope / current_correlation_id
--   4. authz.current_user_has_workspace_access / is_workspace_admin / has_workspace_permission
--   5. GRANT permissions for notrelix_app on new schemas
--   6. updated_at triggers on messaging/notifications/authz tables
--
-- RLS policies (ENABLE ROW LEVEL SECURITY + CREATE POLICY) are applied at runtime
-- by RlsPolicyApplier, not by this script.
-- =============================================================================

-- 1. Create PostgreSQL roles (idempotent)
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
END $$;

-- 2. updated_at trigger function
CREATE OR REPLACE FUNCTION ops.set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 3. RLS session context helper functions
CREATE OR REPLACE FUNCTION ops.current_user_id() RETURNS uuid AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', true), '')::uuid;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION ops.current_workspace_id() RETURNS uuid AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_workspace_id', true), '')::uuid;
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION ops.current_request_scope() RETURNS text AS $$
BEGIN
    RETURN NULLIF(current_setting('app.request_scope', true), '');
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION ops.current_correlation_id() RETURNS text AS $$
BEGIN
    RETURN current_setting('app.correlation_id', true);
EXCEPTION WHEN others THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- 4. authz RLS helper functions
CREATE OR REPLACE FUNCTION authz.current_user_has_workspace_access(p_workspace_id uuid)
RETURNS boolean AS $$
BEGIN
    IF p_workspace_id IS NULL OR ops.current_user_id() IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM authz.workspace_access_grants
        WHERE workspace_id = p_workspace_id
          AND user_id = ops.current_user_id()
          AND membership_status = 'Active'
          AND revoked_at IS NULL
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION authz.current_user_is_workspace_admin(p_workspace_id uuid)
RETURNS boolean AS $$
BEGIN
    IF p_workspace_id IS NULL OR ops.current_user_id() IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM authz.workspace_access_grants
        WHERE workspace_id = p_workspace_id
          AND user_id = ops.current_user_id()
          AND membership_status = 'Active'
          AND revoked_at IS NULL
          AND is_workspace_admin = true
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

CREATE OR REPLACE FUNCTION authz.current_user_has_workspace_permission(
    p_workspace_id uuid,
    p_permission_code text
)
RETURNS boolean AS $$
BEGIN
    IF p_workspace_id IS NULL OR ops.current_user_id() IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM authz.workspace_access_grants
        WHERE workspace_id = p_workspace_id
          AND user_id = ops.current_user_id()
          AND membership_status = 'Active'
          AND revoked_at IS NULL
          AND (is_workspace_admin = true OR p_permission_code = ANY(permission_codes))
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- 5. Grant permissions for notrelix_app on new schemas
GRANT USAGE ON SCHEMA events, messaging, notifications, audit, analytics TO notrelix_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA events, messaging, notifications, audit, analytics TO notrelix_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA events, messaging, notifications, audit, analytics TO notrelix_app;
GRANT USAGE ON SCHEMA authz TO notrelix_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA authz TO notrelix_app;

-- 6. updated_at triggers on new tables (idempotent via DROP IF EXISTS)
DROP TRIGGER IF EXISTS trg_messaging_outbox_messages_updated_at ON messaging.outbox_messages;
CREATE TRIGGER trg_messaging_outbox_messages_updated_at
BEFORE UPDATE ON messaging.outbox_messages
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

DROP TRIGGER IF EXISTS trg_notifications_email_outbox_updated_at ON notifications.email_outbox;
CREATE TRIGGER trg_notifications_email_outbox_updated_at
BEFORE UPDATE ON notifications.email_outbox
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

DROP TRIGGER IF EXISTS trg_authz_workspace_access_grants_updated_at ON authz.workspace_access_grants;
CREATE TRIGGER trg_authz_workspace_access_grants_updated_at
BEFORE UPDATE ON authz.workspace_access_grants
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();
