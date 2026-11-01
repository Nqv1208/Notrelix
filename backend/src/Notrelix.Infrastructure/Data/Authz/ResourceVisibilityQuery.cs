namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Canonical collection-visibility SQL authority (docs/product/governance.md
/// GOV-029, ADR-009). Each statement returns the ids of the resources one
/// principal may see in one Workspace, encoding exactly the decision
/// <c>AccessPolicyEngine</c> makes for a single read over the facts of
/// <see cref="AccessFactsQuery"/> (GOV-025 order):
/// <list type="number">
/// <item>no active Workspace membership → nothing;</item>
/// <item>Workspace Owner → every active resource;</item>
/// <item>explicit permission rules for the single-read action (User or active
///   Team subject), lowest priority value first, Deny wins at equal priority;</item>
/// <item>Private resource → board membership or an explicit resource permission
///   (User or active Team) is required; Space roles never reach it;</item>
/// <item>Workspace-visible resource placed in a Space → the same, or a Space
///   level of at least Viewer (workspace Owner/Admin → Manager; Space role
///   Admin/Member/Viewer through User or Team entries on the Space or an
///   ancestor, Member/Viewer stopped by a Private Space in between; open path →
///   Editor for workspace Members);</item>
/// <item>unplaced Workspace-visible resource → visible to non-Guest members,
///   Guests need explicit access.</item>
/// </list>
/// A Space is visible when the principal's Space level on it is above zero.
/// An archived Board is reached by the same single read (GOV-025 step 1: only a missing or
/// deleted resource is NotFound), so the archived list applies these rules to archived Boards.
/// The PostgreSQL parity suites (CollectionVisibilityIntegrationTests,
/// SpaceAccessMatrixIntegrationTests) assert list membership equals the
/// single-read decision; change both authorities together.
/// </summary>
public static class ResourceVisibilityQuery
{
    /// <summary>The action a single Board read is authorized with (GetBoardQuery).</summary>
    public const string BoardReadAction = "ViewBoard";

    /// <summary>The action a single Page read is authorized with (GetPageQuery).</summary>
    public const string PageReadAction = "ViewPage";

    private const string Principal = """
        WITH ws AS MATERIALIZED (
          SELECT w.account_id FROM workspace.workspaces w
           WHERE w.id = @workspace_id AND w.deleted_at IS NULL),
        member AS MATERIALIZED (
          SELECT wm.role FROM workspace.workspace_members wm JOIN ws ON ws.account_id = wm.account_id
           WHERE wm.workspace_id = @workspace_id AND wm.user_id = @user_id AND wm.status = 'Active'
           LIMIT 1),
        user_teams AS MATERIALIZED (
          SELECT tm.team_id FROM workspace.team_members tm
            JOIN workspace.teams t ON t.id = tm.team_id
           WHERE tm.workspace_id = @workspace_id AND tm.user_id = @user_id AND tm.status = 'Active'
             AND t.workspace_id = @workspace_id AND t.deleted_at IS NULL AND t.status = 'Active'),
        user_rules AS MATERIALIZED (
          SELECT pr.priority, lower(pr.effect) AS effect, pr.scope_type, pr.resource_type, pr.resource_id
            FROM governance.permission_rules pr JOIN ws ON ws.account_id = pr.account_id
           WHERE pr.workspace_id = @workspace_id
             AND pr.status = 'Active' AND pr.deleted_at IS NULL
             AND (pr.starts_at IS NULL OR pr.starts_at <= @now)
             AND (pr.expires_at IS NULL OR pr.expires_at > @now)
             AND pr.action = @action
             AND ((pr.subject_type = 'User' AND pr.subject_id = @user_id)
                  OR (pr.subject_type = 'Team' AND pr.subject_id IN (SELECT team_id FROM user_teams)))),
        user_permissions AS MATERIALIZED (
          SELECT rp.resource_id
            FROM governance.resource_permissions rp JOIN ws ON ws.account_id = rp.account_id
           WHERE rp.workspace_id = @workspace_id AND rp.resource_type = @resource_type
             AND ((rp.subject_type = 'User' AND rp.subject_id = @user_id)
                  OR (rp.subject_type = 'Team' AND rp.subject_id IN (SELECT team_id FROM user_teams)))
             AND rp.deleted_at IS NULL),
        user_entries AS MATERIALIZED (
          SELECT m.space_id,
                 max(CASE m.role WHEN 'Admin' THEN 3 WHEN 'Member' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END) AS role_rank
            FROM workspace.space_members m
           WHERE m.workspace_id = @workspace_id
             AND ((m.subject_type = 'User' AND m.subject_id = @user_id)
                  OR (m.subject_type = 'Team' AND m.subject_id IN (SELECT team_id FROM user_teams)))
           GROUP BY m.space_id),
        space_access AS MATERIALIZED (
          SELECT s.id AS space_id,
                 -- Open only when the closure knows the Space (AccessFactsQuery reads
                 -- SpaceIsOpen as EXISTS(path) AND NOT EXISTS(Private ancestor)): a
                 -- Space without closure rows must not read as open here while the
                 -- single read denies it.
                 (EXISTS (
                   SELECT 1 FROM workspace.space_paths p0 WHERE p0.descendant_id = s.id)
                  AND NOT EXISTS (
                   SELECT 1 FROM workspace.space_paths p
                     JOIN workspace.spaces ps ON ps.id = p.ancestor_id
                    WHERE p.descendant_id = s.id AND ps.visibility = 'Private')) AS is_open,
                 (SELECT max(e.role_rank)
                    FROM workspace.space_paths a
                    JOIN user_entries e ON e.space_id = a.ancestor_id
                   WHERE a.descendant_id = s.id
                     AND (e.role_rank = 3
                          OR NOT EXISTS (
                            SELECT 1 FROM workspace.space_paths pp
                              JOIN workspace.spaces ps ON ps.id = pp.ancestor_id
                             WHERE pp.descendant_id = s.id AND pp.depth < a.depth
                               AND ps.visibility = 'Private'))) AS role_rank
            FROM workspace.spaces s
           WHERE s.workspace_id = @workspace_id AND s.deleted_at IS NULL),
        space_level AS MATERIALIZED (
          SELECT sa.space_id,
                 CASE WHEN m.role IN ('Owner', 'Admin') THEN 4
                      ELSE GREATEST(
                        CASE sa.role_rank WHEN 3 THEN 4 WHEN 2 THEN 3 WHEN 1 THEN 1 ELSE 0 END,
                        CASE WHEN sa.is_open AND m.role = 'Member' THEN 3 ELSE 0 END)
                 END AS level
            FROM space_access sa CROSS JOIN member m)
        """;

    // First-priority rule decision for one resource id (r.resource_id matched as
    // the engine's facts query matches it).
    private const string RuleDecision = """
        LEFT JOIN LATERAL (
          SELECT bool_or(r.effect = 'deny') AS has_deny, bool_or(r.effect = 'allow') AS has_allow
            FROM user_rules r
           WHERE (r.scope_type = 'Workspace'
                  OR ((r.resource_type IS NULL OR r.resource_type = @resource_type)
                      AND (r.resource_id IS NULL OR r.resource_id = res.id)))
             AND r.priority = (
               SELECT min(r2.priority) FROM user_rules r2
                WHERE r2.scope_type = 'Workspace'
                   OR ((r2.resource_type IS NULL OR r2.resource_type = @resource_type)
                       AND (r2.resource_id IS NULL OR r2.resource_id = res.id)))
        ) rd ON true
        LEFT JOIN space_level sl ON sl.space_id = res.space_id
        """;

    public const string VisibleBoardsSql = Principal + "\n" + """

        SELECT res.id
          FROM work.boards res
          CROSS JOIN member m
        """ + "\n" + RuleDecision + "\n" + """

         WHERE res.workspace_id = @workspace_id
           AND res.deleted_at IS NULL AND res.is_archived = false
           AND (
             m.role = 'Owner'
             OR (NOT COALESCE(rd.has_deny, false)
                 AND (COALESCE(rd.has_allow, false)
                      OR EXISTS (SELECT 1 FROM work.board_members bm
                                  WHERE bm.board_id = res.id AND bm.user_id = @user_id)
                      OR EXISTS (SELECT 1 FROM user_permissions up WHERE up.resource_id = res.id)
                      OR (res.visibility = 'Workspace' AND res.space_id IS NULL AND m.role <> 'Guest')
                      OR (res.visibility = 'Workspace' AND res.space_id IS NOT NULL AND COALESCE(sl.level, 0) >= 1))))
        """;

    /// <summary>
    /// The archived (not deleted) Boards the principal may see: the <see cref="VisibleBoardsSql"/>
    /// rules, unchanged, over archived Boards. An archived Board is read by the same decision as
    /// an active one.
    /// </summary>
    public static readonly string VisibleArchivedBoardsSql =
        VisibleBoardsSql.Replace("res.is_archived = false", "res.is_archived = true", StringComparison.Ordinal);

    public const string VisiblePagesSql = Principal + "\n" + """

        SELECT res.id
          FROM docs.pages res
          CROSS JOIN member m
        """ + "\n" + RuleDecision + "\n" + """

         WHERE res.workspace_id = @workspace_id
           AND res.deleted_at IS NULL AND res.status <> 'Archived'
           AND (
             m.role = 'Owner'
             OR (NOT COALESCE(rd.has_deny, false)
                 AND (COALESCE(rd.has_allow, false)
                      OR EXISTS (SELECT 1 FROM user_permissions up WHERE up.resource_id = res.id)
                      OR (res.visibility = 'Workspace' AND res.space_id IS NULL AND m.role <> 'Guest')
                      OR (res.visibility = 'Workspace' AND res.space_id IS NOT NULL AND COALESCE(sl.level, 0) >= 1))))
        """;

    /// <summary>
    /// The pages in the trash (archived, not deleted) the principal may see: the
    /// <see cref="VisiblePagesSql"/> rules, unchanged, over archived pages.
    /// </summary>
    public static readonly string VisibleTrashedPagesSql =
        VisiblePagesSql.Replace("res.status <> 'Archived'", "res.status = 'Archived'", StringComparison.Ordinal);

    /// <summary>
    /// Non-deleted Spaces with the principal's effective level on each (1 =
    /// Viewer, 3 = Editor, 4 = Manager); a Space is visible when the level is
    /// above zero (GOV-028/GOV-029).
    /// </summary>
    public const string VisibleSpacesSql = Principal + "\n" + """

        SELECT sl.space_id, sl.level
          FROM space_level sl
         WHERE sl.level > 0
        """;
}
