-- =============================================================================
-- 005_policies_identity.sql — Identity/Auth RLS policies
-- =============================================================================
-- Identity is NOT workspace-scoped. Policies enforce self-access only.
-- Auth role can read/insert for login/register flows.
-- =============================================================================

-- identity.users
ALTER TABLE identity.users ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_users_select_self_app ON identity.users;
CREATE POLICY p_identity_users_select_self_app ON identity.users
    FOR SELECT TO notrelix_app
    USING (id = ops.current_user_id() AND deleted_at IS NULL);

DROP POLICY IF EXISTS p_identity_users_update_self_app ON identity.users;
CREATE POLICY p_identity_users_update_self_app ON identity.users
    FOR UPDATE TO notrelix_app
    USING (id = ops.current_user_id())
    WITH CHECK (id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_users_auth_select ON identity.users;
CREATE POLICY p_identity_users_auth_select ON identity.users
    FOR SELECT TO notrelix_auth
    USING (true);

DROP POLICY IF EXISTS p_identity_users_auth_insert ON identity.users;
CREATE POLICY p_identity_users_auth_insert ON identity.users
    FOR INSERT TO notrelix_auth
    WITH CHECK (true);

DROP POLICY IF EXISTS p_identity_users_worker_read ON identity.users;
CREATE POLICY p_identity_users_worker_read ON identity.users
    FOR SELECT TO notrelix_worker
    USING (true);

DROP POLICY IF EXISTS p_identity_users_support_read ON identity.users;
CREATE POLICY p_identity_users_support_read ON identity.users
    FOR SELECT TO notrelix_support_readonly
    USING (true);

-- identity.user_profiles
ALTER TABLE identity.user_profiles ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_user_profiles_select_self_app ON identity.user_profiles;
CREATE POLICY p_identity_user_profiles_select_self_app ON identity.user_profiles
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_user_profiles_update_self_app ON identity.user_profiles;
CREATE POLICY p_identity_user_profiles_update_self_app ON identity.user_profiles
    FOR UPDATE TO notrelix_app
    USING (user_id = ops.current_user_id())
    WITH CHECK (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_user_profiles_auth_all ON identity.user_profiles;
CREATE POLICY p_identity_user_profiles_auth_all ON identity.user_profiles
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_identity_user_profiles_worker_read ON identity.user_profiles;
CREATE POLICY p_identity_user_profiles_worker_read ON identity.user_profiles
    FOR SELECT TO notrelix_worker
    USING (true);

-- identity.user_sessions
ALTER TABLE identity.user_sessions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_user_sessions_select_self_app ON identity.user_sessions;
CREATE POLICY p_identity_user_sessions_select_self_app ON identity.user_sessions
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_user_sessions_auth_all ON identity.user_sessions;
CREATE POLICY p_identity_user_sessions_auth_all ON identity.user_sessions
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_identity_user_sessions_worker_read ON identity.user_sessions;
CREATE POLICY p_identity_user_sessions_worker_read ON identity.user_sessions
    FOR SELECT TO notrelix_worker
    USING (true);

-- identity.user_security_settings
ALTER TABLE identity.user_security_settings ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_user_security_settings_select_self_app ON identity.user_security_settings;
CREATE POLICY p_identity_user_security_settings_select_self_app ON identity.user_security_settings
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_user_security_settings_auth_all ON identity.user_security_settings;
CREATE POLICY p_identity_user_security_settings_auth_all ON identity.user_security_settings
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

-- identity.user_mfa_methods
ALTER TABLE identity.user_mfa_methods ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_user_mfa_methods_select_self_app ON identity.user_mfa_methods;
CREATE POLICY p_identity_user_mfa_methods_select_self_app ON identity.user_mfa_methods
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_user_mfa_methods_auth_all ON identity.user_mfa_methods;
CREATE POLICY p_identity_user_mfa_methods_auth_all ON identity.user_mfa_methods
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

-- identity.oauth_accounts
ALTER TABLE identity.oauth_accounts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_oauth_accounts_select_self_app ON identity.oauth_accounts;
CREATE POLICY p_identity_oauth_accounts_select_self_app ON identity.oauth_accounts
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_identity_oauth_accounts_auth_all ON identity.oauth_accounts;
CREATE POLICY p_identity_oauth_accounts_auth_all ON identity.oauth_accounts
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

-- identity.api_tokens
ALTER TABLE identity.api_tokens ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_api_tokens_select_self_app ON identity.api_tokens;
CREATE POLICY p_identity_api_tokens_select_self_app ON identity.api_tokens
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id() OR user_id IS NULL);

DROP POLICY IF EXISTS p_identity_api_tokens_auth_all ON identity.api_tokens;
CREATE POLICY p_identity_api_tokens_auth_all ON identity.api_tokens
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_identity_api_tokens_worker_read ON identity.api_tokens;
CREATE POLICY p_identity_api_tokens_worker_read ON identity.api_tokens
    FOR SELECT TO notrelix_worker
    USING (true);

-- identity.email_verification_tokens (auth only)
ALTER TABLE identity.email_verification_tokens ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_email_verification_tokens_auth_all ON identity.email_verification_tokens;
CREATE POLICY p_identity_email_verification_tokens_auth_all ON identity.email_verification_tokens
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

-- identity.password_reset_tokens (auth only)
ALTER TABLE identity.password_reset_tokens ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_password_reset_tokens_auth_all ON identity.password_reset_tokens;
CREATE POLICY p_identity_password_reset_tokens_auth_all ON identity.password_reset_tokens
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);

-- identity.user_login_attempts (auth only)
ALTER TABLE identity.user_login_attempts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_identity_user_login_attempts_auth_all ON identity.user_login_attempts;
CREATE POLICY p_identity_user_login_attempts_auth_all ON identity.user_login_attempts
    FOR ALL TO notrelix_auth
    USING (true) WITH CHECK (true);
