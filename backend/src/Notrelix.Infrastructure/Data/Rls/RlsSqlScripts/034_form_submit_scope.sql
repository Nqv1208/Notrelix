-- =============================================================================
-- 034_form_submit_scope.sql — The per-form principal of an anonymous public form submission
-- =============================================================================
-- A visitor of a public form has no user, no session and no workspace. Instead of a broad policy or the
-- worker bypass, the request enters a scope bound to exactly ONE form: the database derives the form (and
-- its account, workspace and board) from the form's public key, and only when the form is currently open to
-- the public: published, not deleted, its public key set (the form is public), its board live and not
-- archived, its workspace active and not deleted, and the workspace's public-links switch on. Anything else
-- enters nothing, so a private, closed, deleted, archived or switched-off form is indistinguishable from an
-- unknown key (404). Switching the form back to private clears the key, which ends the principal at once.
--
-- What the scope can do, and nothing more:
--   * read the ONE form and its questions (the definition the visitor is shown anyway);
--   * read the option ids of the board fields those questions fill, through one bounded function
--     (the principal has no read of the board, its fields or its items);
--   * INSERT form submissions for that form, plus the outbox and domain-event rows that insert raises.
-- It never updates or deletes anything and never reads another form, board or workspace. It is a token-kind
-- scope (never a member of ops.is_worker_scope()), carries the purpose 'form-submit', and ends with the
-- transaction.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.has_form_submit_binding(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    -- The internal event log carries only a workspace, so the account may be NULL there; a non-NULL account must match.
    SELECT p_workspace IS NOT NULL
       AND ops.current_request_scope() = 'token'
       AND ops.current_setting_text('app.scope_token_purpose') = 'form-submit'
       AND (p_account IS NULL OR p_account = ops.current_setting_uuid('app.scope_account_id'))
       AND p_workspace = ops.current_setting_uuid('app.scope_workspace_id');
$$;

-- True when the row belongs to the one form the scope was entered for.
CREATE OR REPLACE FUNCTION ops.has_form_submit_form(p_account uuid, p_workspace uuid, p_form uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_form IS NOT NULL
       AND p_account IS NOT NULL
       AND ops.has_form_submit_binding(p_account, p_workspace)
       AND p_form = ops.current_setting_uuid('app.scope_form_id');
$$;

-- Enters the form-submit scope for the form with this public key. Returns the derived form, account,
-- workspace and board, or nothing (no scope entered) when no form is open to the public under the key.
-- Must run inside a transaction: the values are transaction-local.
CREATE OR REPLACE FUNCTION ops.enter_form_submit_scope(p_public_key text)
RETURNS TABLE (form_id uuid, account_id uuid, workspace_id uuid, board_id uuid)
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_form uuid;
    v_account uuid;
    v_workspace uuid;
    v_board uuid;
BEGIN
    IF p_public_key IS NULL OR length(p_public_key) < 22 OR length(p_public_key) > 64 THEN
        RETURN;
    END IF;

    SELECT f.id, f.account_id, f.workspace_id, f.board_id
      INTO v_form, v_account, v_workspace, v_board
      FROM work.forms f
      JOIN work.boards b ON b.id = f.board_id AND b.deleted_at IS NULL AND NOT b.is_archived
      JOIN workspace.workspaces w ON w.id = f.workspace_id AND w.account_id = f.account_id
                                 AND w.deleted_at IS NULL AND w.status = 'Active'
                                 AND w.settings_allow_public_sharing
     WHERE f.public_key = p_public_key
       AND f.deleted_at IS NULL
       AND f.status = 'Published';

    IF v_form IS NULL THEN
        RETURN;
    END IF;

    PERFORM set_config('app.request_scope', 'token', true);
    PERFORM set_config('app.scope_token_purpose', 'form-submit', true);
    PERFORM set_config('app.scope_token_hash', '', true);
    PERFORM set_config('app.scope_account_id', v_account::text, true);
    PERFORM set_config('app.scope_workspace_id', v_workspace::text, true);
    PERFORM set_config('app.scope_form_id', v_form::text, true);
    PERFORM set_config('app.scope_user_id', '', true);
    PERFORM set_config('app.scope_invitee_id', '', true);
    PERFORM set_config('app.current_user_id', '', true);
    PERFORM set_config('app.current_account_id', v_account::text, true);
    PERFORM set_config('app.current_workspace_id', v_workspace::text, true);

    RETURN QUERY SELECT v_form, v_account, v_workspace, v_board;
END;
$$;

-- The options a visitor may choose for the questions of the bound form that fill a board field. This is
-- the only thing the principal can learn about a board: the options the form itself offers (id and label),
-- never a field name, an item or any other field.
CREATE OR REPLACE FUNCTION ops.form_submit_field_options()
RETURNS TABLE (field_id uuid, option_id uuid, option_name text)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT o.field_id, o.id, o.name
      FROM work.form_questions q
      JOIN work.field_options o ON o.field_id = q.board_field_id
     WHERE ops.has_form_submit_form(q.account_id, q.workspace_id, q.form_id)
       AND q.form_id = ops.current_setting_uuid('app.scope_form_id');
$$;

REVOKE ALL ON FUNCTION ops.has_form_submit_binding(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.has_form_submit_form(uuid, uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_form_submit_scope(text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.form_submit_field_options() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_form_submit_binding(uuid, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.has_form_submit_form(uuid, uuid, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.enter_form_submit_scope(text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.form_submit_field_options() TO notrelix_app;

-- Read: the one form and its questions.
SELECT ops.create_policy(
    'work', 'forms', 'p_app_select_form_submit_scope', 'SELECT', 'notrelix_app',
    'ops.has_form_submit_form(account_id, workspace_id, id)', NULL);
SELECT ops.create_policy(
    'work', 'form_questions', 'p_app_select_form_submit_scope', 'SELECT', 'notrelix_app',
    'ops.has_form_submit_form(account_id, workspace_id, form_id)', NULL);

-- Write: the submission itself and the rows the insert raises, for the bound form's tenant only.
SELECT ops.create_policy(
    'work', 'form_submissions', 'p_app_insert_form_submit_scope', 'INSERT', 'notrelix_app', NULL,
    'ops.has_form_submit_form(account_id, workspace_id, form_id) AND submitter_user_id IS NULL AND created_item_id IS NULL AND status = ''Accepted''');
SELECT ops.create_policy(
    'messaging', 'outbox_messages', 'p_app_insert_form_submit_scope', 'INSERT', 'notrelix_app', NULL,
    'ops.has_form_submit_binding(account_id, workspace_id)');
SELECT ops.create_policy(
    'events', 'domain_event_logs', 'p_app_insert_form_submit_scope', 'INSERT', 'notrelix_app', NULL,
    'ops.has_form_submit_binding(NULL, workspace_id)');
