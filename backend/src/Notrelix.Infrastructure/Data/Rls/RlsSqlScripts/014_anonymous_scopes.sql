-- =============================================================================
-- 014_anonymous_scopes.sql — Bounded scopes of the anonymous credential flows
-- =============================================================================
-- Invitation preview/decline/accept, registration and the credential family (login, refresh, verification,
-- reset) run before the caller has a tenant, and often before the caller is known at all. None of them is
-- allowed a broad policy or the worker bypass. Each enters a scope through an owner-defined function that
-- derives its bound from a presented secret (a token digest) or from the rows it is about to create, and
-- every scope ends with the transaction (all values are transaction-local).
--
--   token         one invitation (account + workspace + digest)          012: enter_token_scope
--   invitee       token scope + the person the invitation is for        014: enter_invitee_scope
--   registration  one brand-new user (and the one account it creates)    014: enter_registration_scope
--   credential    one existing user derived from a normalized email, a   014: enter_credential_scope
--                 session refresh digest or an email-verification digest
--
-- None of these values is a member of ops.is_worker_scope().
-- =============================================================================

-- True when the current token or registration scope is bound to this invitation account/workspace.
CREATE OR REPLACE FUNCTION ops.has_invitation_binding(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() IN ('token', 'registration')
       AND ops.current_setting_text('app.scope_token_hash') IS NOT NULL
       AND ops.current_setting_text('app.scope_token_purpose') = 'workspace-invitation'
       AND (p_account IS NULL OR p_account = ops.current_setting_uuid('app.scope_account_id'))
       AND (p_workspace IS NULL OR p_workspace = ops.current_setting_uuid('app.scope_workspace_id'))
       AND COALESCE(p_account, p_workspace) IS NOT NULL;
$$;

-- True while the request accepts a valid invitation for the identified person: the person is provisionally
-- inside the invited account (and workspace) so the acceptance can read what it must validate and write the
-- membership rows. Bound by enter_invitee_scope / enter_registration_scope; never true for an anonymous
-- preview or decline.
CREATE OR REPLACE FUNCTION ops.has_invitee_scope(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() IN ('token', 'registration')
       AND ops.current_setting_uuid('app.scope_invitee_id') IS NOT NULL
       AND ops.current_setting_uuid('app.scope_invitee_id') = ops.current_user_id()
       AND ops.has_invitation_binding(p_account, p_workspace);
$$;

-- True while the request accepts a valid invitation for the identified person, whatever tenant the row
-- being written carries. Only INSERT-only internal-event policies use it (a tenantless event raised
-- while the acceptance runs); it grants no read.
CREATE OR REPLACE FUNCTION ops.has_invitee_scope_any()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() IN ('token', 'registration')
       AND ops.current_setting_uuid('app.scope_invitee_id') IS NOT NULL
       AND ops.current_setting_uuid('app.scope_invitee_id') = ops.current_user_id()
       AND ops.current_setting_text('app.scope_token_hash') IS NOT NULL;
$$;

-- Identifies the invitation of the current token/registration scope by its digest and bound ids, and
-- returns its pending-ness, role and address. The function owner bypasses row-level security for this one
-- lookup; it never returns a row for another invitation.
CREATE OR REPLACE FUNCTION ops.scope_invitation(
    OUT o_pending boolean, OUT o_role text, OUT o_email text, OUT o_invited_by uuid)
RETURNS record
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT (i.status = 'Pending' AND i.expires_at > now()),
           i.role::text,
           lower(btrim(i.email)),
           i.invited_by
      FROM workspace.workspace_invitations i
     WHERE i.token_hash = ops.current_setting_text('app.scope_token_hash')
       AND i.account_id = ops.current_setting_uuid('app.scope_account_id')
       AND i.workspace_id = ops.current_setting_uuid('app.scope_workspace_id');
$$;

-- Binds the person an invitation is accepted by. The person must be the authenticated user of the request,
-- the invitation must be pending and unexpired, and the invited address must be the person's own address.
-- Returns false (and binds nothing) otherwise.
CREATE OR REPLACE FUNCTION ops.enter_invitee_scope(p_user uuid)
RETURNS boolean
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_pending boolean;
    v_role text;
    v_email text;
    v_invited_by uuid;
    v_user_email text;
BEGIN
    IF p_user IS NULL
       OR ops.current_request_scope() <> 'token'
       OR ops.current_setting_text('app.scope_token_hash') IS NULL
       OR ops.current_user_id() IS DISTINCT FROM p_user THEN
        RETURN false;
    END IF;

    SELECT s.o_pending, s.o_role, s.o_email, s.o_invited_by
      INTO v_pending, v_role, v_email, v_invited_by
      FROM ops.scope_invitation() s;

    IF v_pending IS NOT TRUE THEN
        RETURN false;
    END IF;

    SELECT u.normalized_email INTO v_user_email FROM identity.users u WHERE u.id = p_user;
    IF v_user_email IS NULL OR v_user_email <> v_email THEN
        RETURN false;
    END IF;

    PERFORM set_config('app.scope_invitee_id', p_user::text, true);
    PERFORM set_config('app.scope_invitation_role', v_role, true);
    PERFORM set_config('app.scope_invitation_email', v_email, true);
    RETURN true;
END;
$$;

-- Enters the registration scope for ONE brand-new user: the id must not belong to an existing user. When
-- the request already holds a token scope for a pending invitation, the registration keeps that binding
-- (an invited person registering and accepting in one transaction); the new user's address is then
-- constrained to the invited address at insert time (registration_user_allowed).
CREATE OR REPLACE FUNCTION ops.enter_registration_scope(p_user uuid)
RETURNS boolean
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_with_token boolean := false;
    v_pending boolean;
    v_role text;
    v_email text;
BEGIN
    IF p_user IS NULL OR EXISTS (SELECT 1 FROM identity.users u WHERE u.id = p_user) THEN
        RETURN false;
    END IF;

    IF ops.current_request_scope() = 'token' AND ops.current_setting_text('app.scope_token_hash') IS NOT NULL THEN
        SELECT s.o_pending, s.o_role, s.o_email INTO v_pending, v_role, v_email FROM ops.scope_invitation() s;
        IF v_pending IS NOT TRUE THEN
            RETURN false;
        END IF;
        v_with_token := true;
    END IF;

    PERFORM set_config('app.request_scope', 'registration', true);
    PERFORM set_config('app.scope_user_id', p_user::text, true);
    PERFORM set_config('app.current_user_id', p_user::text, true);
    PERFORM set_config('app.scope_registration_account_id', '', true);
    IF v_with_token THEN
        PERFORM set_config('app.scope_invitee_id', p_user::text, true);
        PERFORM set_config('app.scope_invitation_role', v_role, true);
        PERFORM set_config('app.scope_invitation_email', v_email, true);
    ELSE
        PERFORM set_config('app.scope_invitee_id', '', true);
        PERFORM set_config('app.scope_invitation_role', '', true);
        PERFORM set_config('app.scope_invitation_email', '', true);
        PERFORM set_config('app.scope_token_hash', '', true);
        PERFORM set_config('app.scope_token_purpose', '', true);
        PERFORM set_config('app.scope_account_id', '', true);
        PERFORM set_config('app.scope_workspace_id', '', true);
    END IF;
    RETURN true;
END;
$$;

-- Registration: may the registration scope insert this user row? Only the user the scope was entered for,
-- and, for an invited registration, only with the invited address.
CREATE OR REPLACE FUNCTION ops.registration_user_allowed(p_id uuid, p_normalized_email text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_id IS NOT NULL
       AND ops.current_request_scope() = 'registration'
       AND p_id = ops.current_setting_uuid('app.scope_user_id')
       AND (ops.current_setting_text('app.scope_invitation_email') IS NULL
            OR ops.current_setting_text('app.scope_invitation_email') = p_normalized_email);
$$;

-- Registration: may the registration scope insert this account row? Only the one account the scope's
-- first grant named (the grant write registers it), so a registration can create nothing else.
CREATE OR REPLACE FUNCTION ops.registration_account_allowed(p_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_id IS NOT NULL
       AND ops.current_request_scope() = 'registration'
       AND p_id = ops.current_setting_uuid('app.scope_registration_account_id');
$$;

-- Whether an identity already exists for a normalized address. Returns only the boolean (no row), so a
-- registration or an invitation preview can answer "already registered" without reading any user row. The
-- unique index on the address still decides a race.
CREATE OR REPLACE FUNCTION ops.email_registered(p_normalized_email text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_normalized_email IS NOT NULL
       AND EXISTS (SELECT 1 FROM identity.users u WHERE u.normalized_email = p_normalized_email AND u.deleted_at IS NULL);
$$;

-- The only way the application role writes an authz.access_grants row. The row is written immediately (not at
-- SaveChanges) so every grant-derived policy of the rest of the request sees it. The role set and the admin
-- flags are derived here, never supplied:
--   registration scope: the owner grant of the ONE new account for the new user (the account must not exist);
--   invitee scope:      the invited person's account-level Member grant, or the invitation's own role in
--                       the invitation's own workspace;
--   tenant (app) scope: only inside the account and workspace the request is scoped to -
--                       the account-level Member grant that accompanies adding someone to a workspace of
--                       the account (never over an account administrator's grant);
--                       a Member/Guest/Admin grant of the scoped workspace;
--                       an Owner grant of the scoped workspace only when the caller is that workspace's
--                       current Owner (the ownership transfer);
--                       an Owner grant of a NEW workspace of the scoped account for the caller who
--                       creates it (the workspace must not exist yet and the caller must be in the account).
--                       Application authorization stays the authority that decides whether the caller may
--                       do this; the function only bounds where the write can land.
-- Anything else is refused.
CREATE OR REPLACE FUNCTION ops.upsert_access_grant(
    p_account uuid, p_workspace uuid, p_user uuid, p_role text, p_granted_at timestamptz)
RETURNS void
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
DECLARE
    v_scope text := ops.current_request_scope();
    v_actor uuid := ops.current_user_id();
    v_allowed boolean := false;
    v_invitation_role text;
    v_is_admin boolean := p_role IN ('Owner', 'Admin');
BEGIN
    IF p_account IS NULL OR p_user IS NULL OR p_role NOT IN ('Owner', 'Admin', 'Member', 'Guest') THEN
        RAISE EXCEPTION 'Invalid access grant' USING ERRCODE = '22023';
    END IF;

    IF v_scope = 'registration'
       AND p_user = ops.current_setting_uuid('app.scope_user_id')
       AND p_workspace IS NULL
       AND p_role = 'Owner'
       AND NOT EXISTS (SELECT 1 FROM account.accounts a WHERE a.id = p_account)
       AND COALESCE(ops.current_setting_uuid('app.scope_registration_account_id'), p_account) = p_account THEN
        PERFORM set_config('app.scope_registration_account_id', p_account::text, true);
        v_allowed := true;
    END IF;

    IF NOT v_allowed
       AND ops.has_invitee_scope(p_account, p_workspace)
       AND p_user = ops.current_setting_uuid('app.scope_invitee_id') THEN
        IF p_workspace IS NULL THEN
            v_allowed := p_role = 'Member';
        ELSE
            v_invitation_role := ops.current_setting_text('app.scope_invitation_role');
            v_allowed := v_invitation_role IS NOT NULL AND v_invitation_role = p_role;
        END IF;
    END IF;

    IF NOT v_allowed
       AND v_scope = 'app'
       AND v_actor IS NOT NULL
       AND p_account = ops.current_account_id() THEN
        IF p_workspace IS NULL THEN
            v_allowed := p_role = 'Member';
        ELSIF p_workspace = ops.current_workspace_id() THEN
            IF p_role = 'Owner' THEN
                v_allowed := EXISTS (
                    SELECT 1
                      FROM authz.access_grants g
                     WHERE g.account_id = p_account
                       AND g.workspace_id = p_workspace
                       AND g.user_id = v_actor
                       AND g.revoked_at IS NULL
                       AND g.membership_status = 'Active'
                       AND 'Owner' = ANY (g.role_codes));
            ELSE
                v_allowed := true;
            END IF;
        ELSIF ops.current_workspace_id() IS NULL
              AND p_role = 'Owner'
              AND p_user = v_actor
              AND ops.has_account_access(p_account)
              AND NOT EXISTS (SELECT 1 FROM workspace.workspaces w WHERE w.id = p_workspace) THEN
            v_allowed := true;
        END IF;
    END IF;

    IF NOT v_allowed THEN
        RAISE EXCEPTION 'Access grant write denied for the current scope' USING ERRCODE = '42501';
    END IF;

    IF p_workspace IS NULL THEN
        INSERT INTO authz.access_grants AS g (
            id, account_id, workspace_id, user_id, source_context, membership_status, role_codes,
            permission_codes, is_account_admin, is_workspace_admin, granted_at, updated_at)
        VALUES (
            gen_random_uuid(), p_account, NULL, p_user, 'Account', 'Active', ARRAY[p_role]::text[],
            ARRAY[]::text[], v_is_admin, false, p_granted_at, p_granted_at)
        ON CONFLICT (account_id, user_id) WHERE workspace_id IS NULL DO UPDATE
           SET membership_status = 'Active', role_codes = EXCLUDED.role_codes, permission_codes = ARRAY[]::text[],
               is_account_admin = EXCLUDED.is_account_admin, is_workspace_admin = false,
               revoked_at = NULL, updated_at = EXCLUDED.updated_at
         -- a Member write never replaces an account administrator's grant
         WHERE g.is_account_admin = false OR EXCLUDED.is_account_admin;
    ELSE
        INSERT INTO authz.access_grants AS g (
            id, account_id, workspace_id, user_id, source_context, membership_status, role_codes,
            permission_codes, is_account_admin, is_workspace_admin, granted_at, updated_at)
        VALUES (
            gen_random_uuid(), p_account, p_workspace, p_user, 'Workspace', 'Active', ARRAY[p_role]::text[],
            ARRAY[]::text[], false, v_is_admin, p_granted_at, p_granted_at)
        ON CONFLICT (account_id, workspace_id, user_id) DO UPDATE
           SET membership_status = 'Active', role_codes = EXCLUDED.role_codes, permission_codes = ARRAY[]::text[],
               is_account_admin = false, is_workspace_admin = EXCLUDED.is_workspace_admin,
               revoked_at = NULL, updated_at = EXCLUDED.updated_at;
    END IF;
END;
$$;

-- Ends a person's workspace grant. Valid only in the tenant (app) scope of that very workspace and account:
-- the member removal and suspension of the scoped workspace. (A person leaving ends their OWN grant through
-- the narrow own-grant update policy instead.) A grant that does not exist is left alone.
CREATE OR REPLACE FUNCTION ops.revoke_access_grant(
    p_account uuid, p_workspace uuid, p_user uuid, p_revoked_at timestamptz)
RETURNS void
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
BEGIN
    IF p_account IS NULL OR p_workspace IS NULL OR p_user IS NULL
       OR ops.current_request_scope() <> 'app'
       OR ops.current_user_id() IS NULL
       OR p_account IS DISTINCT FROM ops.current_account_id()
       OR p_workspace IS DISTINCT FROM ops.current_workspace_id() THEN
        RAISE EXCEPTION 'Access grant revocation denied for the current scope' USING ERRCODE = '42501';
    END IF;

    UPDATE authz.access_grants g
       SET revoked_at = p_revoked_at, updated_at = p_revoked_at
     WHERE g.account_id = p_account AND g.workspace_id = p_workspace AND g.user_id = p_user;
END;
$$;

REVOKE ALL ON FUNCTION ops.has_invitation_binding(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.has_invitee_scope(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.has_invitee_scope_any() FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.scope_invitation() FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_invitee_scope(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_registration_scope(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.registration_user_allowed(uuid, text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.registration_account_allowed(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.email_registered(text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.upsert_access_grant(uuid, uuid, uuid, text, timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.revoke_access_grant(uuid, uuid, uuid, timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_invitation_binding(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_invitee_scope(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_invitee_scope_any() TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.registration_user_allowed(uuid, text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.registration_account_allowed(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.enter_invitee_scope(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.enter_registration_scope(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.email_registered(text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.upsert_access_grant(uuid, uuid, uuid, text, timestamptz) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.revoke_access_grant(uuid, uuid, uuid, timestamptz) TO notrelix_app;

-- -----------------------------------------------------------------------------
-- Policies of the invitation token scope. Allow-list only; every other table keeps its own policies.
-- -----------------------------------------------------------------------------
-- The invitation row itself: read and update only the one row the scope was entered for.
SELECT ops.create_policy(
    'workspace', 'workspace_invitations', 'p_app_select_invitation_binding', 'SELECT', 'notrelix_app',
    'ops.has_invitation_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')',
    NULL);
SELECT ops.create_policy(
    'workspace', 'workspace_invitations', 'p_app_update_invitation_binding', 'UPDATE', 'notrelix_app',
    'ops.has_invitation_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')',
    'ops.has_invitation_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')');
-- The invited workspace's own row, for the preview (its name).
SELECT ops.create_policy(
    'workspace', 'workspaces', 'p_app_select_invitation_binding', 'SELECT', 'notrelix_app',
    'ops.has_invitation_binding(account_id, id)', NULL);

-- -----------------------------------------------------------------------------
-- Policies of the registration scope: INSERT only, bound to the one new identity.
-- -----------------------------------------------------------------------------
SELECT ops.create_policy(
    'identity', 'users', 'p_app_insert_registration', 'INSERT', 'notrelix_app', NULL,
    'ops.registration_user_allowed(id, normalized_email)');
SELECT ops.create_policy(
    'account', 'accounts', 'p_app_insert_registration', 'INSERT', 'notrelix_app', NULL,
    'ops.registration_account_allowed(id)');

-- The one Board or Page a resource invitation names, for the preview title (WSP-026): SELECT of exactly
-- that row, only while the invitation scope is bound to the invitation that names it.
CREATE OR REPLACE FUNCTION ops.invitation_names_resource(p_kind text, p_account uuid, p_workspace uuid, p_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.has_invitation_binding(p_account, p_workspace)
       AND EXISTS (
            SELECT 1
              FROM workspace.workspace_invitations i
             WHERE i.token_hash = ops.current_setting_text('app.scope_token_hash')
               AND i.account_id = p_account
               AND i.workspace_id = p_workspace
               AND i.resource_kind = p_kind
               AND i.resource_id = p_id);
$$;
REVOKE ALL ON FUNCTION ops.invitation_names_resource(text, uuid, uuid, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.invitation_names_resource(text, uuid, uuid, uuid) TO notrelix_app;
SELECT ops.create_policy(
    'work', 'boards', 'p_app_select_invitation_resource', 'SELECT', 'notrelix_app',
    'ops.invitation_names_resource(''work-management.board'', account_id, workspace_id, id)', NULL);
SELECT ops.create_policy(
    'docs', 'pages', 'p_app_select_invitation_resource', 'SELECT', 'notrelix_app',
    'ops.invitation_names_resource(''documents.page'', account_id, workspace_id, id)', NULL);

-- -----------------------------------------------------------------------------
-- One-time credential rows of one user: written in the registration scope (the new user's first
-- verification token), the credential scope (a reset or verification issued/consumed for the user the
-- scope identified) and the user scope (the caller's own). The user-owned pack allows the owner to read
-- them; these add the INSERT/UPDATE the owning flow needs, bound to the single scope user.
-- -----------------------------------------------------------------------------
SELECT ops.create_policy('identity', 'email_verification_tokens', 'p_app_insert_user_scope', 'INSERT', 'notrelix_app', NULL, 'ops.has_user_scope(user_id)');
SELECT ops.create_policy('identity', 'email_verification_tokens', 'p_app_update_user_scope', 'UPDATE', 'notrelix_app', 'ops.has_user_scope(user_id)', 'ops.has_user_scope(user_id)');
SELECT ops.create_policy('identity', 'email_verification_tokens', 'p_app_select_user_scope', 'SELECT', 'notrelix_app', 'ops.has_user_scope(user_id)', NULL);
SELECT ops.create_policy('identity', 'password_reset_tokens', 'p_app_insert_user_scope', 'INSERT', 'notrelix_app', NULL, 'ops.has_user_scope(user_id)');
SELECT ops.create_policy('identity', 'password_reset_tokens', 'p_app_update_user_scope', 'UPDATE', 'notrelix_app', 'ops.has_user_scope(user_id)', 'ops.has_user_scope(user_id)');
SELECT ops.create_policy('identity', 'password_reset_tokens', 'p_app_select_user_scope', 'SELECT', 'notrelix_app', 'ops.has_user_scope(user_id)', NULL);

-- -----------------------------------------------------------------------------
-- Internal event rows raised while an invitation is declined or accepted. INSERT only: the tables stay
-- unreadable to the application role. A row is admitted when it belongs to the bound invitation's
-- account/workspace, or (accepting person only) when it carries no tenant at all.
-- -----------------------------------------------------------------------------
SELECT ops.create_policy(
    'events', 'domain_event_logs', 'p_app_insert_invitation_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_invitation_binding(NULL, workspace_id) OR (workspace_id IS NULL AND ops.has_invitee_scope_any())');
SELECT ops.create_policy(
    'messaging', 'outbox_messages', 'p_app_insert_invitation_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_invitation_binding(account_id, workspace_id) OR (account_id IS NULL AND workspace_id IS NULL AND ops.has_invitee_scope_any())');
SELECT ops.create_policy(
    'audit', 'audit_logs', 'p_app_insert_invitation_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_invitation_binding(NULL, workspace_id)');

-- -----------------------------------------------------------------------------
-- Credential scope: one EXISTING user, derived by the database from the credential the request presents.
-- -----------------------------------------------------------------------------
-- Login, password reset, email verification and session refresh start from a credential, not from a
-- known user. The application passes the credential (a normalized address, or a digest) and the database
-- finds the one user it names; the scope is then bound to that user alone. No broad SELECT policy on
-- identity.users exists for this: the lookup is this function, the row access afterwards is the ordinary
-- own-row policy. The function returns the derived user id (NULL when nothing matches; nothing is entered
-- then).
--   email               p_credential = normalized email address
--   refresh-token       p_credential = refresh token digest        (session of that digest)
--   email-verification  p_credential = token digest + hash version (token of that digest)
--   oauth-identity      p_credential = provider || chr(31) || provider subject (linked sign-in identity)
CREATE OR REPLACE FUNCTION ops.enter_credential_scope(p_kind text, p_credential text, p_hash_version integer)
RETURNS uuid
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_user uuid;
BEGIN
    IF p_credential IS NULL OR length(p_credential) = 0 THEN
        RETURN NULL;
    END IF;

    IF p_kind = 'email' THEN
        SELECT u.id INTO v_user
          FROM identity.users u
         WHERE u.normalized_email = p_credential AND u.deleted_at IS NULL;
    ELSIF p_kind = 'refresh-token' THEN
        SELECT s.user_id INTO v_user
          FROM identity.user_sessions s
         WHERE s.refresh_token_hash = p_credential;
    ELSIF p_kind = 'email-verification' THEN
        SELECT t.user_id INTO v_user
          FROM identity.email_verification_tokens t
         WHERE t.token_hash = p_credential AND t.hash_version = p_hash_version;
    ELSIF p_kind = 'oauth-identity' THEN
        SELECT a.user_id INTO v_user
          FROM identity.oauth_accounts a
         WHERE a.provider = split_part(p_credential, E'\x1f', 1)
           AND a.provider_id = split_part(p_credential, E'\x1f', 2);
    ELSE
        RAISE EXCEPTION 'Unknown credential scope kind' USING ERRCODE = '22023';
    END IF;

    IF v_user IS NULL THEN
        RETURN NULL;
    END IF;

    PERFORM set_config('app.request_scope', 'credential', true);
    PERFORM set_config('app.scope_user_id', v_user::text, true);
    PERFORM set_config('app.current_user_id', v_user::text, true);
    PERFORM set_config('app.current_account_id', '', true);
    PERFORM set_config('app.current_workspace_id', '', true);
    PERFORM set_config('app.scope_account_id', '', true);
    PERFORM set_config('app.scope_workspace_id', '', true);
    PERFORM set_config('app.scope_token_hash', '', true);
    PERFORM set_config('app.scope_token_purpose', '', true);
    PERFORM set_config('app.scope_invitee_id', '', true);
    PERFORM set_config('app.scope_invitation_role', '', true);
    PERFORM set_config('app.scope_invitation_email', '', true);
    PERFORM set_config('app.scope_registration_account_id', '', true);
    RETURN v_user;
END;
$$;
REVOKE ALL ON FUNCTION ops.enter_credential_scope(text, text, integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.enter_credential_scope(text, text, integer) TO notrelix_app;

-- Whether a provider identity is already linked to ANY account, as a boolean (no row, no owner). Used by the
-- link flow of a signed-in user to refuse an identity that belongs to someone else; the user's own link is
-- visible through the ordinary own-row policy.
CREATE OR REPLACE FUNCTION ops.oauth_identity_linked(p_provider text, p_subject text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_provider IS NOT NULL AND p_subject IS NOT NULL
       AND EXISTS (SELECT 1 FROM identity.oauth_accounts a WHERE a.provider = p_provider AND a.provider_id = p_subject);
$$;
REVOKE ALL ON FUNCTION ops.oauth_identity_linked(text, text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.oauth_identity_linked(text, text) TO notrelix_app;

-- -----------------------------------------------------------------------------
-- Public workspace resolution (account id + slug), for the anonymous resolve endpoint.
-- -----------------------------------------------------------------------------
-- The endpoint is public by product design: it answers one active, non-deleted workspace by (account, slug)
-- with the fields of the workspace card. It is an exact-row definer read, not a scope: no policy is widened,
-- and nothing but these columns and the member count is returned.
CREATE OR REPLACE FUNCTION ops.resolve_workspace_slug(p_account uuid, p_slug text)
RETURNS TABLE (
    workspace_id uuid, name text, slug text, description text, is_personal boolean,
    created_at timestamptz, version bigint, size text, language text, member_count bigint)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT w.id, w.name::text, w.slug::text, w.description::text, w.is_personal,
           w.created_at, w.version, w.settings_size::text, w.settings_language::text,
           (SELECT count(*) FROM workspace.workspace_members m WHERE m.workspace_id = w.id AND m.status <> 'Removed')
      FROM workspace.workspaces w
     WHERE p_account IS NOT NULL AND p_slug IS NOT NULL
       AND w.account_id = p_account AND w.slug = p_slug AND w.status = 'Active' AND w.deleted_at IS NULL;
$$;
REVOKE ALL ON FUNCTION ops.resolve_workspace_slug(uuid, text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.resolve_workspace_slug(uuid, text) TO notrelix_app;
