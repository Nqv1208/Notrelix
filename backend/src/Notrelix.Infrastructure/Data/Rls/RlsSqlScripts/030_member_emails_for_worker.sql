-- The board import job resolves Person columns by e-mail while it runs on the worker login (ImportPeopleDirectory,
-- called from BoardTransferWorker), but ops.disclosable_member_emails was granted to the application role only, so the
-- job failed with "permission denied for function". The worker is the unbounded background principal: the function now
-- also answers it, still only for ACTIVE NON-GUEST members of the named Workspace, and the importer's own Owner/Admin
-- check stays in the caller. A forged 'worker' scope on any other login is still refused by ops.is_worker_scope().
CREATE OR REPLACE FUNCTION ops.disclosable_member_emails(p_workspace uuid, p_user_ids uuid[])
RETURNS TABLE (user_id uuid, email text)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT u.id, u.normalized_email::text
      FROM identity.users u
      JOIN workspace.workspace_members subject
        ON subject.user_id = u.id AND subject.workspace_id = p_workspace
     WHERE p_workspace IS NOT NULL
       AND u.id = ANY (p_user_ids)
       AND subject.status = 'Active'
       AND subject.role <> 'Guest'
       AND (ops.is_worker_scope()
            OR (ops.current_request_scope() IN ('app', 'user')
                AND EXISTS (
                    SELECT 1
                      FROM workspace.workspace_members reader
                     WHERE reader.workspace_id = p_workspace
                       AND reader.user_id = ops.current_user_id()
                       AND reader.status = 'Active'
                       AND reader.role IN ('Owner', 'Admin'))));
$$;

REVOKE ALL ON FUNCTION ops.disclosable_member_emails(uuid, uuid[]) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.disclosable_member_emails(uuid, uuid[]) TO notrelix_app, notrelix_worker;
