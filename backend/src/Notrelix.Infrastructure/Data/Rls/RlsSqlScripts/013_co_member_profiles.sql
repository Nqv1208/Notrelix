-- =============================================================================
-- 013_co_member_profiles.sql — Co-member display facts (name, avatar)
-- =============================================================================
-- identity.users has no row policy that exposes another person's row: the row carries the email, the
-- password hash and security state, and a row policy cannot hide columns. The only facts one person may
-- resolve about another are the display name and the avatar, and only when the two share a workspace
-- membership. These owner-defined functions return exactly those facts and nothing else.
--
-- Relationship: both users hold a workspace_members row in the same workspace, in ANY membership state
-- (active, suspended, removed), so historical attribution (a comment, an assignment) keeps resolving
-- after someone leaves. Guests are workspace members and resolve the same way. Which members a person may
-- LIST stays an Application authorization rule; these functions only resolve identity display facts for ids
-- the Application already decided to show.
--
-- The caller is the user the session context names (app.current_user_id). A person always resolves
-- themselves. The worker scope (already an unbounded background principal) resolves any requested id.
-- No other scope resolves anyone: a token or credential scope has no user relationship to share.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.shares_workspace_with_caller(p_user uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_user IS NOT NULL AND (
        ops.is_worker_scope()
        OR (
            ops.current_request_scope() IN ('app', 'user')
            AND ops.current_user_id() IS NOT NULL
            AND (
                p_user = ops.current_user_id()
                OR EXISTS (
                    SELECT 1
                      FROM workspace.workspace_members mine
                      JOIN workspace.workspace_members theirs ON theirs.workspace_id = mine.workspace_id
                     WHERE mine.user_id = ops.current_user_id()
                       AND theirs.user_id = p_user
                )
            )
        )
    );
$$;

CREATE OR REPLACE FUNCTION ops.co_member_profiles(p_user_ids uuid[])
RETURNS TABLE (user_id uuid, name text, avatar text)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT u.id, u.name::text, u.avatar::text
      FROM identity.users u
     WHERE u.id = ANY (p_user_ids)
       AND ops.shares_workspace_with_caller(u.id);
$$;

-- The object-store key of an uploaded profile photo, for the avatar endpoint. Same relationship rule.
CREATE OR REPLACE FUNCTION ops.co_member_avatar_storage_key(p_user uuid)
RETURNS text
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT u.avatar_storage_key::text
      FROM identity.users u
     WHERE u.id = p_user
       AND ops.shares_workspace_with_caller(u.id);
$$;

REVOKE ALL ON FUNCTION ops.shares_workspace_with_caller(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.co_member_profiles(uuid[]) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.co_member_avatar_storage_key(uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.shares_workspace_with_caller(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.co_member_profiles(uuid[]) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.co_member_avatar_storage_key(uuid) TO notrelix_app;
