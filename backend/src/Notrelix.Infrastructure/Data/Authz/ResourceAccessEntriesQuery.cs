namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Explicit access entries of one resource for the Governance access summary.
/// Subjects are matched as <see cref="AccessFactsQuery"/> matches them: User
/// and Team resource permissions that are not deleted, Teams that are active
/// and in the Workspace, Team members that are active; every reached person is
/// an active member of the Workspace. The resource's own members are supplied
/// by the owning context (<c>@member_ids</c>); Work persistence is not read here.
/// </summary>
public static class ResourceAccessEntriesQuery
{
    private const string LevelRank = """
        CASE rp.permission_level
          WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
          WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END
        """;

    private const string ActiveGrants = """
        SELECT rp.id, rp.subject_type, rp.subject_id, rp.permission_level, rp.created_by, rp.created_at, rp.version, t.name AS team_name
          FROM governance.resource_permissions rp
          LEFT JOIN workspace.teams t
            ON rp.subject_type = 'Team' AND t.id = rp.subject_id
           AND t.workspace_id = @workspace_id AND t.deleted_at IS NULL AND t.status = 'Active'
         WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
           AND rp.resource_type = @resource_type AND rp.resource_id = @resource_id
           AND rp.deleted_at IS NULL
           AND (rp.subject_type = 'User' OR (rp.subject_type = 'Team' AND t.id IS NOT NULL))
        """;

    /// <summary>
    /// Columns: subject_type, subject_id, rank, created_by (nullable), team_name (nullable), permission id,
    /// created_at, team_people (active members of a Team subject; null for a User), version of the permission row.
    /// </summary>
    public const string GrantsSql = """
        SELECT rp.subject_type, rp.subject_id,
        """ + LevelRank + """
         AS rank, rp.created_by, rp.team_name, rp.id, rp.created_at,
               CASE WHEN rp.subject_type = 'Team' THEN
                 (SELECT count(*)::int FROM workspace.team_members tm
                   WHERE tm.workspace_id = @workspace_id AND tm.team_id = rp.subject_id AND tm.status = 'Active')
               END AS team_people,
               rp.version
          FROM (
        """ + ActiveGrants + """
          ) rp
        """;

    /// <summary>Column: user_id of every active Workspace Owner and Admin.</summary>
    public const string WorkspaceAdminsSql = """
        SELECT wm.user_id
          FROM workspace.workspace_members wm
         WHERE wm.account_id = @account_id AND wm.workspace_id = @workspace_id
           AND wm.status = 'Active' AND wm.role IN ('Owner', 'Admin')
        """;

    /// <summary>
    /// Columns: source (owner | member | user | team | space), team_id (nullable), user_id, is_guest.
    /// Space rows are produced only when <c>@space_id</c> is not null.
    /// </summary>
    public const string ReachSql = """
        WITH grants AS MATERIALIZED (
        """ + ActiveGrants + """
        ),
        active_team_members AS MATERIALIZED (
          SELECT tm.team_id, tm.user_id
            FROM workspace.team_members tm
            JOIN workspace.teams t ON t.id = tm.team_id
           WHERE tm.workspace_id = @workspace_id AND tm.status = 'Active'
             AND t.workspace_id = @workspace_id AND t.deleted_at IS NULL AND t.status = 'Active'),
        reach AS (
          SELECT 'owner' AS source, NULL::uuid AS team_id, o.user_id
            FROM workspace.workspace_members o
           WHERE o.account_id = @account_id AND o.workspace_id = @workspace_id
             AND o.role = 'Owner' AND o.status = 'Active'
          UNION ALL
          SELECT 'member', NULL::uuid, m.user_id
            FROM unnest(@member_ids) AS m(user_id)
          UNION ALL
          SELECT 'user', NULL::uuid, g.subject_id FROM grants g WHERE g.subject_type = 'User'
          UNION ALL
          SELECT 'team', g.subject_id, atm.user_id
            FROM grants g JOIN active_team_members atm ON atm.team_id = g.subject_id
           WHERE g.subject_type = 'Team'
          UNION ALL
          SELECT 'space', NULL::uuid, sm.subject_id
            FROM workspace.space_members sm
           WHERE @space_id IS NOT NULL AND sm.workspace_id = @workspace_id
             AND sm.space_id = @space_id AND sm.subject_type = 'User'
          UNION ALL
          SELECT 'space', NULL::uuid, atm.user_id
            FROM workspace.space_members sm
            JOIN active_team_members atm ON atm.team_id = sm.subject_id
           WHERE @space_id IS NOT NULL AND sm.workspace_id = @workspace_id
             AND sm.space_id = @space_id AND sm.subject_type = 'Team')
        SELECT DISTINCT r.source, r.team_id, r.user_id, wm.role = 'Guest' AS is_guest
          FROM reach r
          JOIN workspace.workspace_members wm
            ON wm.account_id = @account_id AND wm.workspace_id = @workspace_id
           AND wm.user_id = r.user_id AND wm.status = 'Active'
        """;

    /// <summary>
    /// Column: whether an active permission rule for <c>@action</c> (User or Team
    /// subject) applies to the resource, matched as <see cref="AccessFactsQuery"/>
    /// matches rules for one principal.
    /// </summary>
    public const string HasReadRulesSql = """
        SELECT EXISTS (
          SELECT 1 FROM governance.permission_rules pr
           WHERE pr.account_id = @account_id AND pr.workspace_id = @workspace_id
             AND pr.status = 'Active' AND pr.deleted_at IS NULL
             AND (pr.starts_at IS NULL OR pr.starts_at <= @now)
             AND (pr.expires_at IS NULL OR pr.expires_at > @now)
             AND pr.action = @action
             AND pr.subject_type IN ('User', 'Team')
             AND (pr.scope_type = 'Workspace'
                  OR ((pr.resource_type IS NULL OR pr.resource_type = @resource_type)
                      AND (pr.resource_id IS NULL OR pr.resource_id = @resource_id))))
        """;

    /// <summary>Column: name of <c>@space_id</c> when it exists and is not deleted.</summary>
    public const string SpaceNameSql = """
        SELECT s.name FROM workspace.spaces s
         WHERE s.id = @space_id AND s.workspace_id = @workspace_id AND s.deleted_at IS NULL
        """;

    /// <summary>
    /// Columns: user_id, role_rank (Viewer=1, Member=2, Admin=3): the highest role of any direct or Team
    /// entry of <c>@space_id</c> that reaches the person.
    /// </summary>
    public const string SpaceRolesSql = """
        WITH entries AS (
          SELECT sm.subject_id AS user_id,
                 CASE sm.role WHEN 'Admin' THEN 3 WHEN 'Member' THEN 2 ELSE 1 END AS role_rank
            FROM workspace.space_members sm
           WHERE @space_id IS NOT NULL AND sm.workspace_id = @workspace_id
             AND sm.space_id = @space_id AND sm.subject_type = 'User'
          UNION ALL
          SELECT tm.user_id,
                 CASE sm.role WHEN 'Admin' THEN 3 WHEN 'Member' THEN 2 ELSE 1 END
            FROM workspace.space_members sm
            JOIN workspace.team_members tm
              ON tm.team_id = sm.subject_id AND tm.workspace_id = @workspace_id AND tm.status = 'Active'
            JOIN workspace.teams t
              ON t.id = tm.team_id AND t.workspace_id = @workspace_id AND t.deleted_at IS NULL AND t.status = 'Active'
           WHERE @space_id IS NOT NULL AND sm.workspace_id = @workspace_id
             AND sm.space_id = @space_id AND sm.subject_type = 'Team')
        SELECT user_id, max(role_rank) FROM entries GROUP BY user_id
        """;
}
