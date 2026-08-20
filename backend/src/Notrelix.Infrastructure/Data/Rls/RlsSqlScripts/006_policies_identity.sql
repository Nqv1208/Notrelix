-- =============================================================================
-- 006_policies_identity.sql — Identity/Auth policies
-- =============================================================================

DO $$
BEGIN
    IF ops.table_exists('identity', 'users') THEN
        PERFORM ops.drop_all_policies_for_table('identity', 'users');
        PERFORM ops.enable_rls_for_table('identity', 'users');
        PERFORM ops.create_policy('identity', 'users', 'p_app_select_self', 'SELECT', 'notrelix_app', 'id = ops.current_user_id()', NULL);
        PERFORM ops.create_policy('identity', 'users', 'p_app_update_self', 'UPDATE', 'notrelix_app', 'id = ops.current_user_id()', 'id = ops.current_user_id()');
        PERFORM ops.create_policy('identity', 'users', 'p_auth_all', 'ALL', 'notrelix_auth', 'true', 'true');
        PERFORM ops.create_policy('identity', 'users', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('identity', 'users', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_user_owned_policies('identity', 'user_profiles', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_sessions', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'oauth_accounts', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_security_settings', 'user_id', true, true);
SELECT ops.apply_user_owned_policies('identity', 'user_mfa_methods', 'user_id', true, true);
-- API tokens are workspace credentials (account_id + workspace_id): workspace-scoped
-- policies. The auth handler performs its single-row digest lookup in the system
-- scope; every management/read path still flows through these policies.
SELECT ops.apply_scoped_business_policies('identity', 'api_tokens', true);
SELECT ops.apply_user_owned_policies('identity', 'user_login_attempts', 'user_id', true, false);
SELECT ops.apply_user_owned_policies('identity', 'email_verification_tokens', 'user_id', true, false);
SELECT ops.apply_user_owned_policies('identity', 'password_reset_tokens', 'user_id', true, false);
