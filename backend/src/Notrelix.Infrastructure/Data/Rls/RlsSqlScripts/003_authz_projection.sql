-- =============================================================================
-- 003_authz_projection.sql — Local authorization projection for RLS
-- =============================================================================
-- authz.access_grants is the local access projection that every
-- future bounded-context DB must carry so RLS never depends on cross-DB joins.
-- This is an account-aware projection: every grant is scoped to an account,
-- with an optional workspace_id for workspace-level access.
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS authz;

CREATE TABLE IF NOT EXISTS authz.access_grants (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id          uuid NOT NULL,
    workspace_id        uuid,
    user_id             uuid NOT NULL,

    source_context      varchar(80) NOT NULL DEFAULT 'Workspace',
    membership_status   varchar(40) NOT NULL,

    role_codes          text[] NOT NULL DEFAULT '{}'::text[],
    permission_codes    text[] NOT NULL DEFAULT '{}'::text[],

    is_account_admin    boolean NOT NULL DEFAULT false,
    is_workspace_admin  boolean NOT NULL DEFAULT false,

    granted_at          timestamptz NOT NULL DEFAULT now(),
    revoked_at          timestamptz,
    updated_at          timestamptz NOT NULL DEFAULT now(),

    source_event_id     uuid,
    source_version      bigint NOT NULL DEFAULT 1,
    metadata_json       jsonb NOT NULL DEFAULT '{}'::jsonb,

    CONSTRAINT ck_authz_access_grants_status
        CHECK (membership_status IN ('Pending', 'Active', 'Suspended', 'Removed', 'Revoked')),
    CONSTRAINT ck_authz_access_grants_source_context_not_blank
        CHECK (btrim(source_context) <> '')
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_access_grants_account_workspace_user
    ON authz.access_grants(account_id, COALESCE(workspace_id, '00000000-0000-0000-0000-000000000000'::uuid), user_id);

DROP TRIGGER IF EXISTS trg_authz_access_grants_updated_at ON authz.access_grants;
CREATE TRIGGER trg_authz_access_grants_updated_at
BEFORE UPDATE ON authz.access_grants
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE INDEX IF NOT EXISTS ix_access_grants_user_account_active
    ON authz.access_grants(user_id, account_id)
    WHERE membership_status = 'Active' AND revoked_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_access_grants_workspace_user_active
    ON authz.access_grants(workspace_id, user_id)
    WHERE workspace_id IS NOT NULL AND membership_status = 'Active' AND revoked_at IS NULL;

-- Helper functions for RLS policy evaluation

CREATE OR REPLACE FUNCTION authz.current_user_has_workspace_access(p_workspace_id uuid)
RETURNS boolean AS $$
BEGIN
    IF p_workspace_id IS NULL OR ops.current_user_id() IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM authz.access_grants
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
        SELECT 1 FROM authz.access_grants
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
    IF p_workspace_id IS NULL OR ops.current_user_id() IS NULL OR p_permission_code IS NULL THEN
        RETURN false;
    END IF;
    RETURN EXISTS (
        SELECT 1 FROM authz.access_grants
        WHERE workspace_id = p_workspace_id
          AND user_id = ops.current_user_id()
          AND membership_status = 'Active'
          AND revoked_at IS NULL
          AND (
              is_workspace_admin = true
              OR p_permission_code = ANY(permission_codes)
          )
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- Grants for authz schema
GRANT USAGE ON SCHEMA authz TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT SELECT ON authz.access_grants TO notrelix_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON authz.access_grants TO notrelix_worker;
GRANT SELECT ON authz.access_grants TO notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_has_workspace_access(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_is_workspace_admin(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_has_workspace_permission(uuid, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_user_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_workspace_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_request_scope() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
