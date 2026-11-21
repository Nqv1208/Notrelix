-- =============================================================================
-- 035_sso_scope.sql — Bounded reads and the one write of enterprise single sign-on (OIDC)
-- =============================================================================
-- The sign-in page, the SSO start, the SSO callback and the password / social sign-in checks run before there
-- is a user, a session or an Account in the request. They never get a broad policy on account_domains,
-- account_identity_providers, account_settings, accounts or account_members. Everything they may know comes
-- through the owner-defined functions below, each keyed by something the server holds (the typed email's
-- domain, an account id the server derived itself) and each returning only what the caller needs:
--
--   * ops.sso_route(domain)            — who a VERIFIED domain signs in with: account id and name, provider display
--                                        name, whether SSO is required. A row only when the domain is Verified, the
--                                        Account is open and has an Active provider. No client id, issuer or secret.
--   * ops.sso_enforcement(domain, user) — whether a Verified domain's Account requires SSO, and whether the person
--                                        is an active Owner of it (the password break-glass). Answers nothing else.
--   * ops.sso_domain_verified(account, domain) — a boolean.
--   * ops.sso_domain_claimed_by_other(domain, account) — a boolean: another Account holds the domain Verified.
--   * ops.sso_provider_public(account) — issuer, client id, JIT flag, display name of the Active provider (no secret).
--   * ops.enter_sso_callback_scope(account) + ops.sso_callback_secret() — the ONLY read of the client secret: it
--                                        needs the token scope of purpose 'sso-callback' bound to that very account,
--                                        which the database enters only for an Account whose provider is Active.
--   * ops.sso_join_member(account, user, now) — the ONLY write: the person's membership of the Account as role
--                                        Member (and the Account-level access grant that goes with it), and only when
--                                        the person's confirmed email is on a Verified domain of that Account whose
--                                        provider is Active. It never reactivates an ended membership and never
--                                        writes another role.
-- A closed Account (and a deleted one) routes nothing. Entering the callback scope replaces any earlier scope of
-- the transaction (like every other enter_* function), so the client secret is read first and the credential or
-- registration scope entered afterwards.
-- =============================================================================

-- True when the account is open to new sign-ins: not deleted, Active or Trialing.
CREATE OR REPLACE FUNCTION ops.sso_account_open(p_account uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_account IS NOT NULL
       AND EXISTS (SELECT 1 FROM account.accounts a
                    WHERE a.id = p_account AND a.deleted_at IS NULL AND a.status IN ('Active', 'Trialing'));
$$;

CREATE OR REPLACE FUNCTION ops.sso_route(p_domain text)
RETURNS TABLE (account_id uuid, account_name text, provider_name text, enforced boolean)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT a.id, a.name::text, p.name::text, COALESCE(s.setting_value = 'true', false)
      FROM account.account_domains d
      JOIN account.accounts a ON a.id = d.account_id AND ops.sso_account_open(a.id)
      JOIN account.account_identity_providers p
        ON p.account_id = a.id AND p.provider_type = 'Oidc' AND p.status = 'Active'
      LEFT JOIN account.account_settings s
        ON s.account_id = a.id AND s.setting_key = 'security.require-sso'
     WHERE p_domain IS NOT NULL
       AND d.domain = lower(btrim(p_domain))
       AND d.verification_status = 'Verified';
$$;

CREATE OR REPLACE FUNCTION ops.sso_enforcement(p_domain text, p_user uuid)
RETURNS TABLE (account_id uuid, is_owner boolean)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT a.id,
           (p_user IS NOT NULL AND EXISTS (
                SELECT 1 FROM account.account_members m
                 WHERE m.account_id = a.id AND m.user_id = p_user
                   AND m.role = 'Owner' AND m.status = 'Active' AND m.deleted_at IS NULL))
      FROM account.account_domains d
      JOIN account.accounts a ON a.id = d.account_id AND a.deleted_at IS NULL AND a.status <> 'Closed'
      JOIN account.account_settings s
        ON s.account_id = a.id AND s.setting_key = 'security.require-sso' AND s.setting_value = 'true'
     WHERE p_domain IS NOT NULL
       AND d.domain = lower(btrim(p_domain))
       AND d.verification_status = 'Verified';
$$;

CREATE OR REPLACE FUNCTION ops.sso_domain_verified(p_account uuid, p_domain text)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_account IS NOT NULL AND p_domain IS NOT NULL
       AND EXISTS (SELECT 1 FROM account.account_domains d
                    WHERE d.account_id = p_account AND d.domain = lower(btrim(p_domain))
                      AND d.verification_status = 'Verified');
$$;

-- True when ANOTHER Account holds the domain Verified: the one cross-tenant fact an Owner's claim needs (a boolean).
CREATE OR REPLACE FUNCTION ops.sso_domain_claimed_by_other(p_domain text, p_account uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_domain IS NOT NULL AND p_account IS NOT NULL
       AND EXISTS (SELECT 1 FROM account.account_domains d
                    WHERE d.domain = lower(btrim(p_domain)) AND d.verification_status = 'Verified'
                      AND d.account_id <> p_account);
$$;

CREATE OR REPLACE FUNCTION ops.sso_provider_public(p_account uuid)
RETURNS TABLE (name text, issuer text, client_id text, jit_provisioning boolean)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p.name::text, p.issuer::text, p.client_id::text, p.jit_provisioning_enabled
      FROM account.account_identity_providers p
     WHERE ops.sso_account_open(p_account)
       AND p.account_id = p_account AND p.provider_type = 'Oidc' AND p.status = 'Active'
       AND p.client_id IS NOT NULL;
$$;

-- Enters the callback scope for an Account whose OIDC provider is Active. Returns false (nothing entered)
-- otherwise. Must run inside a transaction: the values are transaction-local.
CREATE OR REPLACE FUNCTION ops.enter_sso_callback_scope(p_account uuid)
RETURNS boolean
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
BEGIN
    IF NOT ops.sso_account_open(p_account)
       OR NOT EXISTS (SELECT 1 FROM account.account_identity_providers p
                       WHERE p.account_id = p_account AND p.provider_type = 'Oidc' AND p.status = 'Active') THEN
        RETURN false;
    END IF;

    PERFORM set_config('app.request_scope', 'token', true);
    PERFORM set_config('app.scope_token_purpose', 'sso-callback', true);
    PERFORM set_config('app.scope_token_hash', '', true);
    PERFORM set_config('app.scope_account_id', p_account::text, true);
    PERFORM set_config('app.scope_workspace_id', '', true);
    PERFORM set_config('app.scope_form_id', '', true);
    PERFORM set_config('app.scope_user_id', '', true);
    PERFORM set_config('app.scope_invitee_id', '', true);
    PERFORM set_config('app.current_user_id', '', true);
    PERFORM set_config('app.current_account_id', '', true);
    PERFORM set_config('app.current_workspace_id', '', true);
    RETURN true;
END;
$$;

-- The protected client secret of the Account the callback scope was entered for; nothing outside that scope.
CREATE OR REPLACE FUNCTION ops.sso_callback_secret()
RETURNS text
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p.client_secret_protected::text
      FROM account.account_identity_providers p
     WHERE ops.current_request_scope() = 'token'
       AND ops.current_setting_text('app.scope_token_purpose') = 'sso-callback'
       AND p.account_id = ops.current_setting_uuid('app.scope_account_id')
       AND p.provider_type = 'Oidc' AND p.status = 'Active';
$$;

-- Joins the person to the Account as a Member. True when the person is an active Member afterwards (newly
-- joined or already); false when the conditions do not hold or the existing membership is not active.
CREATE OR REPLACE FUNCTION ops.sso_join_member(p_account uuid, p_user uuid, p_now timestamptz)
RETURNS boolean
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_domain text;
    v_status text;
    v_inserted integer;
BEGIN
    IF p_account IS NULL OR p_user IS NULL OR p_now IS NULL OR NOT ops.sso_account_open(p_account)
       OR NOT EXISTS (SELECT 1 FROM account.account_identity_providers p
                       WHERE p.account_id = p_account AND p.provider_type = 'Oidc' AND p.status = 'Active') THEN
        RETURN false;
    END IF;

    SELECT regexp_replace(u.normalized_email, '^.*@', '') INTO v_domain
      FROM identity.users u
     WHERE u.id = p_user AND u.deleted_at IS NULL AND u.email_confirmed;
    IF v_domain IS NULL OR NOT ops.sso_domain_verified(p_account, v_domain) THEN
        RETURN false;
    END IF;

    SELECT m.status INTO v_status
      FROM account.account_members m
     WHERE m.account_id = p_account AND m.user_id = p_user;
    IF v_status IS NOT NULL THEN
        RETURN v_status = 'Active';
    END IF;

    INSERT INTO account.account_members (id, account_id, user_id, role, status, created_at, created_by)
    VALUES (gen_random_uuid(), p_account, p_user, 'Member', 'Active', p_now, p_user)
    ON CONFLICT (account_id, user_id) DO NOTHING;
    GET DIAGNOSTICS v_inserted = ROW_COUNT;
    IF v_inserted = 0 THEN
        RETURN false;
    END IF;

    INSERT INTO authz.access_grants AS g (
        id, account_id, workspace_id, user_id, source_context, membership_status, role_codes,
        permission_codes, is_account_admin, is_workspace_admin, granted_at, updated_at)
    VALUES (
        gen_random_uuid(), p_account, NULL, p_user, 'Account', 'Active', ARRAY['Member']::text[],
        ARRAY[]::text[], false, false, p_now, p_now)
    ON CONFLICT (account_id, user_id) WHERE workspace_id IS NULL DO NOTHING;
    RETURN true;
END;
$$;

REVOKE ALL ON FUNCTION ops.sso_account_open(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_route(text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_enforcement(text, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_domain_verified(uuid, text) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_domain_claimed_by_other(text, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_provider_public(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_sso_callback_scope(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_callback_secret() FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.sso_join_member(uuid, uuid, timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.sso_account_open(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_route(text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_enforcement(text, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_domain_verified(uuid, text) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_domain_claimed_by_other(text, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_provider_public(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.enter_sso_callback_scope(uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_callback_secret() TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.sso_join_member(uuid, uuid, timestamptz) TO notrelix_app;
