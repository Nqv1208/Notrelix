-- =============================================================================
-- 018_billing_usage.sql — Account-wide usage counts for Billing
-- =============================================================================
-- Seat and storage limits are account-scoped, so their usage sums across every workspace of the account,
-- while a caller's row visibility stops at the workspaces they hold. These functions return only counts (never
-- rows) and only for a workspace the caller already holds (or is invited to), after Application authorization
-- has run. They do not widen what any table exposes.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.billing_account_usage(p_account uuid, p_workspace uuid)
RETURNS TABLE (member_seats integer, guest_seats integer, storage_bytes bigint)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR NOT ops.has_workspace_access(p_account, p_workspace) THEN
        RETURN QUERY SELECT 0, 0, 0::bigint;
        RETURN;
    END IF;

    RETURN QUERY
    SELECT
        (SELECT COUNT(DISTINCT m.user_id)::integer
           FROM workspace.workspace_members m
           JOIN workspace.workspaces w ON w.id = m.workspace_id
          WHERE m.account_id = p_account AND w.deleted_at IS NULL
            AND m.status = 'Active' AND m.role <> 'Guest'),
        (SELECT COUNT(DISTINCT m.user_id)::integer
           FROM workspace.workspace_members m
           JOIN workspace.workspaces w ON w.id = m.workspace_id
          WHERE m.account_id = p_account AND w.deleted_at IS NULL
            AND m.status = 'Active' AND m.role = 'Guest'),
        (SELECT COALESCE(SUM(a.file_size), 0)::bigint
           FROM collab.attachments a
          WHERE a.account_id = p_account AND a.storage_key IS NOT NULL AND a.purged_at IS NULL);
END;
$$;

CREATE OR REPLACE FUNCTION ops.billing_has_seat(p_account uuid, p_workspace uuid, p_user uuid, p_guest boolean)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR p_user IS NULL
       OR NOT ops.has_workspace_access(p_account, p_workspace) THEN
        RETURN false;
    END IF;

    RETURN EXISTS (
        SELECT 1
          FROM workspace.workspace_members m
          JOIN workspace.workspaces w ON w.id = m.workspace_id
         WHERE m.account_id = p_account AND m.user_id = p_user AND w.deleted_at IS NULL
           AND m.status = 'Active'
           AND ((p_guest AND m.role = 'Guest') OR (NOT p_guest AND m.role <> 'Guest')));
END;
$$;

REVOKE ALL ON FUNCTION ops.billing_account_usage(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.billing_has_seat(uuid, uuid, uuid, boolean) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.billing_account_usage(uuid, uuid) TO notrelix_app, notrelix_worker;
GRANT EXECUTE ON FUNCTION ops.billing_has_seat(uuid, uuid, uuid, boolean) TO notrelix_app, notrelix_worker;
