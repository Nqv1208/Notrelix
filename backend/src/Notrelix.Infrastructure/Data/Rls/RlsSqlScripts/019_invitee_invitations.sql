-- =============================================================================
-- 019_invitee_invitations.sql — A signed-in person reads the invitations addressed to them
-- =============================================================================
-- The person's own list of pending invitations and the answer to one of them (decline or accept by id)
-- are Global requests: they run in the user scope, which carries the verified user and no workspace, so
-- the tenant policies of workspace.workspace_invitations admit nothing.
--
-- This script adds ONE read policy, bound to ONE fact: the invitation is pending and addressed to the
-- confirmed email address of the signed-in person.
--
--   workspace.workspace_invitations   SELECT   a pending invitation addressed to the caller
--
-- Nothing else is widened: no other table, no other scope, no other status, never an address that is
-- not confirmed, and no write. The invited workspace's name and the inviter's display name are not read
-- through this policy: the person's request enters the existing invitation token scope (014) from the
-- digest of an invitation they were allowed to read, and the policies of that scope (the invited
-- workspace row, the inviter's display facts, the invitation row, the events of an answer) apply
-- exactly as they do when the invitation is answered from its link. The invitee scope then proves the
-- address again inside the database (enter_invitee_scope).
-- =============================================================================

-- True when the invitation address is the signed-in person's own confirmed address. Only the user scope
-- of an authenticated person holds it; no token, registration, credential, worker or tenant scope does.
CREATE OR REPLACE FUNCTION ops.is_invited_caller(p_email text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_email IS NOT NULL
       AND ops.current_request_scope() = 'user'
       AND ops.current_user_id() IS NOT NULL
       AND ops.has_user_scope(ops.current_user_id())
       AND EXISTS (
            SELECT 1
              FROM identity.users u
             WHERE u.id = ops.current_user_id()
               AND u.email_confirmed
               AND u.deleted_at IS NULL
               AND u.normalized_email = lower(btrim(p_email)));
$$;

REVOKE ALL ON FUNCTION ops.is_invited_caller(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.is_invited_caller(text) TO notrelix_app;

SELECT ops.create_policy(
    'workspace', 'workspace_invitations', 'p_app_select_invitee_pending', 'SELECT', 'notrelix_app',
    'status = ''Pending'' AND ops.is_invited_caller(email)', NULL);
