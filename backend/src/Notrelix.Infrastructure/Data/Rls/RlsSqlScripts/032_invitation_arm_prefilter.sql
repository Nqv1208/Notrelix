-- =============================================================================
-- 032_invitation_arm_prefilter.sql — The invitation arm of boards and pages runs only inside an invitation scope
-- =============================================================================
-- Why: work.boards and docs.pages carry the policy p_app_select_invitation_resource (014):
--     ops.invitation_names_resource(kind, account_id, workspace_id, id)
-- It is SECURITY DEFINER and is evaluated for every row a statement touches, in every session, first in the combined
-- OR (about 17 microseconds per row), although it can only be true inside an invitation scope.
--
-- Equivalence, read from 014: ops.invitation_names_resource is
--     ops.has_invitation_binding(p_account, p_workspace) AND EXISTS (...)
-- and ops.has_invitation_binding starts with
--     ops.current_request_scope() IN ('token', 'registration') AND ...
-- ops.current_request_scope() is COALESCE(setting, 'app'), never NULL, so outside the token and registration scopes
-- the first conjunct is false and the whole function is false. Inside those scopes the function is called unchanged.
--
-- What: the policy expression becomes
--     CASE WHEN (SELECT ops.current_request_scope() IN ('token', 'registration')) THEN <original call> ELSE false END
-- The scalar subquery has no outer reference, so it is evaluated once per statement, and CASE fixes the order, so a
-- normal session never calls the function. Same policy name, command and roles; only the expression changes.
--
-- Guard: only a policy of notrelix_app alone whose current expression is exactly one plain call of the function is
-- rewritten (any table that has one, not just the two above); anything else, including an already rewritten policy, is
-- left as it is. 014 recreates the plain policy on every apply, then this script rewrites it again.
-- =============================================================================

DO $$
DECLARE
    pol record;
BEGIN
    -- Deparsed policy text is schema-qualified only when the schema is off the path; pin it so the comparison is stable.
    PERFORM set_config('search_path', 'pg_catalog, pg_temp', true);

    FOR pol IN
        SELECT p.polrelid::regclass AS tbl, p.polname, pg_get_expr(p.polqual, p.polrelid) AS using_expr
          FROM pg_policy p
         WHERE p.polroles = ARRAY[(SELECT oid FROM pg_roles WHERE rolname = 'notrelix_app')]
           AND p.polwithcheck IS NULL
           AND pg_get_expr(p.polqual, p.polrelid) ~ '^ops\.invitation_names_resource\([^()]*\)$'
    LOOP
        EXECUTE format(
            'ALTER POLICY %I ON %s USING (CASE WHEN (SELECT ops.current_request_scope() IN (''token'', ''registration'')) THEN %s ELSE false END)',
            pol.polname, pol.tbl, pol.using_expr);
    END LOOP;
END
$$;
