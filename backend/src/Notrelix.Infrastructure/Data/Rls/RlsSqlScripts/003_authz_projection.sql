-- =============================================================================
-- 003_authz_projection.sql — Local authorization projection for RLS
-- =============================================================================
-- authz.workspace_access_grants is the local access projection that every
-- future bounded-context DB must carry so RLS never depends on cross-DB joins.
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS authz;

CREATE TABLE IF NOT EXISTS authz.workspace_access_grants (
    workspace_id        uuid NOT NULL,
    user_id             uuid NOT NULL,

    source_context      varchar(80) NOT NULL DEFAULT 'Workspace',
    membership_status   varchar(40) NOT NULL,

    role_codes          text[] NOT NULL DEFAULT '{}'::text[],
    permission_codes    text[] NOT NULL DEFAULT '{}'::text[],

    is_workspace_owner  boolean NOT NULL DEFAULT false,
    is_workspace_admin  boolean NOT NULL DEFAULT false,

    granted_at          timestamptz NOT NULL DEFAULT now(),
    revoked_at          timestamptz,
    updated_at          timestamptz NOT NULL DEFAULT now(),

    source_event_id     uuid,
    source_version      bigint,
    metadata_json       jsonb NOT NULL DEFAULT '{}'::jsonb,

    PRIMARY KEY (workspace_id, user_id),

    CONSTRAINT ck_authz_workspace_access_grants_status
        CHECK (membership_status IN ('Pending', 'Active', 'Suspended', 'Removed', 'Revoked')),
    CONSTRAINT ck_authz_workspace_access_grants_source_context_not_blank
        CHECK (btrim(source_context) <> '')
);

DROP TRIGGER IF EXISTS trg_authz_workspace_access_grants_updated_at ON authz.workspace_access_grants;
CREATE TRIGGER trg_authz_workspace_access_grants_updated_at
BEFORE UPDATE ON authz.workspace_access_grants
FOR EACH ROW EXECUTE FUNCTION ops.set_updated_at();

CREATE INDEX IF NOT EXISTS ix_authz_workspace_access_grants_user_active
ON authz.workspace_access_grants(user_id, workspace_id)
WHERE membership_status = 'Active' AND revoked_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_authz_workspace_access_grants_workspace_active
ON authz.workspace_access_grants(workspace_id, user_id)
WHERE membership_status = 'Active' AND revoked_at IS NULL;

-- Helper functions for RLS policy evaluation
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
          AND (
              is_workspace_owner = true
              OR is_workspace_admin = true
          )
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
        SELECT 1 FROM authz.workspace_access_grants
        WHERE workspace_id = p_workspace_id
          AND user_id = ops.current_user_id()
          AND membership_status = 'Active'
          AND revoked_at IS NULL
          AND (
              is_workspace_owner = true
              OR is_workspace_admin = true
              OR p_permission_code = ANY(permission_codes)
          )
    );
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- Grants for authz schema
GRANT USAGE ON SCHEMA authz TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT SELECT ON authz.workspace_access_grants TO notrelix_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON authz.workspace_access_grants TO notrelix_worker;
GRANT SELECT ON authz.workspace_access_grants TO notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_has_workspace_access(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_is_workspace_admin(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION authz.current_user_has_workspace_permission(uuid, text) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_user_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_workspace_id() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.current_request_scope() TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
