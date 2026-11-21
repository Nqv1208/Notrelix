-- =============================================================================
-- 036_sso_identity_scope.sql — The sign-in identities of enterprise single sign-on (identity.sso_identities)
-- =============================================================================
-- A person's identity at a company identity provider (Account, issuer, subject) is bound to exactly one user. The
-- callback of the company sign-in starts from the provider's subject, not from a known user, so it has the same
-- shape as the Google identity lookup: the database derives the one user a subject signs in as and enters the
-- ordinary credential scope for that user. Nothing here is a broad policy:
--
--   * ops.enter_sso_identity_scope(account, subject) — the ONLY way the callback finds the user of a subject. It
--       returns that user (nothing when the subject is unknown or the user is deleted) after entering the credential
--       scope for the user, exactly like ops.enter_credential_scope('email', ...) which it delegates to.
--   * identity.sso_identities — a person reads only their own rows. A row is written only in the credential scope
--       (an existing person being linked) or the registration scope (a new person), only for the very user the scope
--       was entered for. Never updated or deleted by the application role.
-- The client secret is not touched here (it is read first, in the SSO callback scope of 035).
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.enter_sso_identity_scope(p_account uuid, p_subject text)
RETURNS uuid
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_email text;
BEGIN
    IF p_account IS NULL OR p_subject IS NULL OR length(p_subject) = 0 THEN
        RETURN NULL;
    END IF;

    SELECT u.normalized_email INTO v_email
      FROM identity.sso_identities i
      JOIN identity.users u ON u.id = i.user_id AND u.deleted_at IS NULL
     WHERE i.account_id = p_account AND i.subject = p_subject;

    IF v_email IS NULL THEN
        RETURN NULL;
    END IF;

    RETURN ops.enter_credential_scope('email', v_email, 0);
END;
$$;

-- May this request write an identity row for this user? Only in the credential or registration scope the user was
-- entered for.
CREATE OR REPLACE FUNCTION ops.sso_identity_write_allowed(p_user uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_user IS NOT NULL
       AND ops.current_request_scope() IN ('credential', 'registration')
       AND p_user = ops.current_setting_uuid('app.scope_user_id');
$$;

REVOKE ALL ON FUNCTION ops.enter_sso_identity_scope(uuid, text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_identity_write_allowed(uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.enter_sso_identity_scope(uuid, text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_identity_write_allowed(uuid) TO notrelix_app;

-- Own rows readable; no application write from the pack (the narrow INSERT below is the only one).
SELECT ops.apply_user_owned_policies('identity', 'sso_identities', 'user_id', false, false);
DO $$
BEGIN
    IF ops.table_exists('identity', 'sso_identities') THEN
        PERFORM ops.create_policy('identity', 'sso_identities', 'p_app_insert_sso_scope', 'INSERT', 'notrelix_app',
            NULL, 'ops.sso_identity_write_allowed(user_id)');
        -- The user-owned pack creates the support SELECT policy that 020 removes from every tenant table.
        DROP POLICY IF EXISTS p_support_select ON identity.sso_identities;
    END IF;
END $$;
