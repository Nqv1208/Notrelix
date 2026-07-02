-- =============================================================================
-- 007_policies_platform.sql — Account/Workspace/Governance/Authz policies
-- =============================================================================

DO $$
BEGIN
    IF ops.table_exists('account', 'accounts') THEN
        PERFORM ops.drop_all_policies_for_table('account', 'accounts');
        PERFORM ops.enable_rls_for_table('account', 'accounts');
        PERFORM ops.create_policy('account', 'accounts', 'p_app_select', 'SELECT', 'notrelix_app', 'ops.has_account_access(id)', NULL);
        PERFORM ops.create_policy('account', 'accounts', 'p_app_update_admin', 'UPDATE', 'notrelix_app', 'ops.is_account_admin(id)', 'ops.is_account_admin(id)');
        PERFORM ops.create_policy('account', 'accounts', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('account', 'accounts', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;

SELECT ops.apply_scoped_business_policies('account', 'account_members', true);
SELECT ops.apply_scoped_business_policies('account', 'account_invitations', true);
SELECT ops.apply_scoped_business_policies('account', 'account_domains', true);
SELECT ops.apply_scoped_business_policies('account', 'account_settings', true);
SELECT ops.apply_scoped_business_policies('account', 'account_regions', true);
SELECT ops.apply_scoped_business_policies('account', 'account_identity_providers', true);
SELECT ops.apply_scoped_business_policies('account', 'scim_directories', true);
SELECT ops.apply_scoped_business_policies('account', 'scim_sync_runs', true);
SELECT ops.apply_scoped_business_policies('account', 'workspace_routes', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspaces', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspace_members', true);
SELECT ops.apply_scoped_business_policies('workspace', 'workspace_invitations', true);
SELECT ops.apply_scoped_business_policies('workspace', 'spaces', true);
SELECT ops.apply_scoped_business_policies('workspace', 'teams', true);
SELECT ops.apply_scoped_business_policies('workspace', 'team_members', true);
SELECT ops.apply_scoped_business_policies('governance', 'custom_roles', true);
SELECT ops.apply_scoped_business_policies('governance', 'custom_role_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'workspace_member_role_assignments', true);
SELECT ops.apply_scoped_business_policies('governance', 'resource_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'field_permissions', true);
SELECT ops.apply_scoped_business_policies('governance', 'permission_rules', true);
SELECT ops.apply_scoped_business_policies('governance', 'permission_templates', true);
SELECT ops.apply_scoped_business_policies('governance', 'workspace_policies', true);
SELECT ops.apply_scoped_business_policies('governance', 'share_links', true);
SELECT ops.apply_scoped_business_policies('governance', 'resource_permission_inheritance_cache', true);

DO $$
DECLARE
    v_own text;
BEGIN
    IF ops.table_exists('authz', 'access_grants') THEN
        PERFORM ops.drop_all_policies_for_table('authz', 'access_grants');
        PERFORM ops.enable_rls_for_table('authz', 'access_grants');

        IF ops.column_exists('authz', 'access_grants', 'user_id') THEN
            v_own := 'user_id = ops.current_user_id()';
        ELSE
            v_own := 'false';
        END IF;

        PERFORM ops.create_policy('authz', 'access_grants', 'p_app_select_own', 'SELECT', 'notrelix_app', v_own, NULL);
        PERFORM ops.create_policy('authz', 'access_grants', 'p_auth_select_own', 'SELECT', 'notrelix_auth', v_own, NULL);
        PERFORM ops.create_policy('authz', 'access_grants', 'p_worker_all', 'ALL', 'notrelix_worker', 'true', 'true');
        PERFORM ops.create_policy('authz', 'access_grants', 'p_support_select', 'SELECT', 'notrelix_support_readonly', 'true', NULL);
    END IF;
END $$;
