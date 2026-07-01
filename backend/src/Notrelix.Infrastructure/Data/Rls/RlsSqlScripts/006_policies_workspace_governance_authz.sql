-- =============================================================================
-- 006_policies_workspace_governance_authz.sql
-- =============================================================================
-- Workspace ownership, membership, governance, and authz projection policies.
-- =============================================================================

-- workspace.workspaces
ALTER TABLE workspace.workspaces ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_workspaces_select_app ON workspace.workspaces;
CREATE POLICY p_workspaces_select_app ON workspace.workspaces
    FOR SELECT TO notrelix_app
    USING (id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(id) AND deleted_at IS NULL);

DROP POLICY IF EXISTS p_workspaces_insert_app ON workspace.workspaces;
CREATE POLICY p_workspaces_insert_app ON workspace.workspaces
    FOR INSERT TO notrelix_app
    WITH CHECK (created_by = ops.current_user_id());

DROP POLICY IF EXISTS p_workspaces_update_admin_app ON workspace.workspaces;
CREATE POLICY p_workspaces_update_admin_app ON workspace.workspaces
    FOR UPDATE TO notrelix_app
    USING (id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(id))
    WITH CHECK (id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(id));

DROP POLICY IF EXISTS p_workspaces_worker_all ON workspace.workspaces;
CREATE POLICY p_workspaces_worker_all ON workspace.workspaces
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_workspaces_support_read ON workspace.workspaces;
CREATE POLICY p_workspaces_support_read ON workspace.workspaces
    FOR SELECT TO notrelix_support_readonly USING (true);

-- workspace.workspace_members
ALTER TABLE workspace.workspace_members ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_workspace_members_select_app ON workspace.workspace_members;
CREATE POLICY p_workspace_members_select_app ON workspace.workspace_members
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_workspace_members_insert_admin_app ON workspace.workspace_members;
CREATE POLICY p_workspace_members_insert_admin_app ON workspace.workspace_members
    FOR INSERT TO notrelix_app
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(workspace_id));

DROP POLICY IF EXISTS p_workspace_members_update_admin_app ON workspace.workspace_members;
CREATE POLICY p_workspace_members_update_admin_app ON workspace.workspace_members
    FOR UPDATE TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(workspace_id))
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(workspace_id));

DROP POLICY IF EXISTS p_workspace_members_worker_all ON workspace.workspace_members;
CREATE POLICY p_workspace_members_worker_all ON workspace.workspace_members
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_workspace_members_support_read ON workspace.workspace_members;
CREATE POLICY p_workspace_members_support_read ON workspace.workspace_members
    FOR SELECT TO notrelix_support_readonly USING (true);

-- workspace.teams
ALTER TABLE workspace.teams ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_teams_select_app ON workspace.teams;
CREATE POLICY p_teams_select_app ON workspace.teams
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);

DROP POLICY IF EXISTS p_teams_insert_app ON workspace.teams;
CREATE POLICY p_teams_insert_app ON workspace.teams
    FOR INSERT TO notrelix_app
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_teams_update_app ON workspace.teams;
CREATE POLICY p_teams_update_app ON workspace.teams
    FOR UPDATE TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id))
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_teams_worker_all ON workspace.teams;
CREATE POLICY p_teams_worker_all ON workspace.teams
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_teams_support_read ON workspace.teams;
CREATE POLICY p_teams_support_read ON workspace.teams
    FOR SELECT TO notrelix_support_readonly USING (true);

-- workspace.team_members
ALTER TABLE workspace.team_members ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_team_members_select_app ON workspace.team_members;
CREATE POLICY p_team_members_select_app ON workspace.team_members
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_team_members_insert_app ON workspace.team_members;
CREATE POLICY p_team_members_insert_app ON workspace.team_members
    FOR INSERT TO notrelix_app
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_team_members_worker_all ON workspace.team_members;
CREATE POLICY p_team_members_worker_all ON workspace.team_members
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_team_members_support_read ON workspace.team_members;
CREATE POLICY p_team_members_support_read ON workspace.team_members
    FOR SELECT TO notrelix_support_readonly USING (true);

-- workspace.spaces
ALTER TABLE workspace.spaces ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_spaces_select_app ON workspace.spaces;
CREATE POLICY p_spaces_select_app ON workspace.spaces
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);

DROP POLICY IF EXISTS p_spaces_insert_app ON workspace.spaces;
CREATE POLICY p_spaces_insert_app ON workspace.spaces
    FOR INSERT TO notrelix_app
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_spaces_update_app ON workspace.spaces;
CREATE POLICY p_spaces_update_app ON workspace.spaces
    FOR UPDATE TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id))
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_spaces_worker_all ON workspace.spaces;
CREATE POLICY p_spaces_worker_all ON workspace.spaces
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

-- workspace.workspace_invitations
ALTER TABLE workspace.workspace_invitations ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_workspace_invitations_select_app ON workspace.workspace_invitations;
CREATE POLICY p_workspace_invitations_select_app ON workspace.workspace_invitations
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));

DROP POLICY IF EXISTS p_workspace_invitations_insert_app ON workspace.workspace_invitations;
CREATE POLICY p_workspace_invitations_insert_app ON workspace.workspace_invitations
    FOR INSERT TO notrelix_app
    WITH CHECK (workspace_id = ops.current_workspace_id() AND authz.current_user_is_workspace_admin(workspace_id));

DROP POLICY IF EXISTS p_workspace_invitations_worker_all ON workspace.workspace_invitations;
CREATE POLICY p_workspace_invitations_worker_all ON workspace.workspace_invitations
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

-- authz.workspace_access_grants
ALTER TABLE authz.workspace_access_grants ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_authz_grants_app_select_own ON authz.workspace_access_grants;
CREATE POLICY p_authz_grants_app_select_own ON authz.workspace_access_grants
    FOR SELECT TO notrelix_app
    USING (user_id = ops.current_user_id());

DROP POLICY IF EXISTS p_authz_grants_worker_all ON authz.workspace_access_grants;
CREATE POLICY p_authz_grants_worker_all ON authz.workspace_access_grants
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

DROP POLICY IF EXISTS p_authz_grants_support_read ON authz.workspace_access_grants;
CREATE POLICY p_authz_grants_support_read ON authz.workspace_access_grants
    FOR SELECT TO notrelix_support_readonly USING (true);

-- governance tables (workspace-scoped, admin write)
ALTER TABLE governance.resource_permissions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS p_gov_resource_permissions_select_app ON governance.resource_permissions;
CREATE POLICY p_gov_resource_permissions_select_app ON governance.resource_permissions
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);
DROP POLICY IF EXISTS p_gov_resource_permissions_worker_all ON governance.resource_permissions;
CREATE POLICY p_gov_resource_permissions_worker_all ON governance.resource_permissions
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
DROP POLICY IF EXISTS p_gov_resource_permissions_support_read ON governance.resource_permissions;
CREATE POLICY p_gov_resource_permissions_support_read ON governance.resource_permissions
    FOR SELECT TO notrelix_support_readonly USING (true);

ALTER TABLE governance.custom_roles ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS p_gov_custom_roles_select_app ON governance.custom_roles;
CREATE POLICY p_gov_custom_roles_select_app ON governance.custom_roles
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);
DROP POLICY IF EXISTS p_gov_custom_roles_worker_all ON governance.custom_roles;
CREATE POLICY p_gov_custom_roles_worker_all ON governance.custom_roles
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

ALTER TABLE governance.share_links ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS p_gov_share_links_select_app ON governance.share_links;
CREATE POLICY p_gov_share_links_select_app ON governance.share_links
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);
DROP POLICY IF EXISTS p_gov_share_links_worker_all ON governance.share_links;
CREATE POLICY p_gov_share_links_worker_all ON governance.share_links
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

ALTER TABLE governance.workspace_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS p_gov_workspace_policies_select_app ON governance.workspace_policies;
CREATE POLICY p_gov_workspace_policies_select_app ON governance.workspace_policies
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id));
DROP POLICY IF EXISTS p_gov_workspace_policies_worker_all ON governance.workspace_policies;
CREATE POLICY p_gov_workspace_policies_worker_all ON governance.workspace_policies
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);

ALTER TABLE governance.permission_rules ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS p_gov_permission_rules_select_app ON governance.permission_rules;
CREATE POLICY p_gov_permission_rules_select_app ON governance.permission_rules
    FOR SELECT TO notrelix_app
    USING (workspace_id = ops.current_workspace_id() AND authz.current_user_has_workspace_access(workspace_id) AND deleted_at IS NULL);
DROP POLICY IF EXISTS p_gov_permission_rules_worker_all ON governance.permission_rules;
CREATE POLICY p_gov_permission_rules_worker_all ON governance.permission_rules
    FOR ALL TO notrelix_worker USING (true) WITH CHECK (true);
