namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Canonical collection-visibility SQL authority (docs/product/governance.md
/// GOV-029, ADR-009). Each statement returns the ids of the resources one
/// principal may see in one Workspace, encoding exactly the decision
/// <c>AccessPolicyEngine</c> makes for a single read over the facts of
/// <see cref="AccessFactsQuery"/>:
/// <list type="number">
/// <item>no active Workspace membership → nothing;</item>
/// <item>Workspace Owner → every active resource;</item>
/// <item>explicit permission rules for the single-read action, lowest priority
///   value first, Deny wins at equal priority;</item>
/// <item>Private resource (or Guest principal) → board membership or an explicit
///   resource permission is required;</item>
/// <item>otherwise visible to the Workspace member.</item>
/// </list>
/// The PostgreSQL parity suite (CollectionVisibilityIntegrationTests) asserts
/// list membership equals the single-read decision; change both together.
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
        user_rules AS MATERIALIZED (
          SELECT pr.priority, lower(pr.effect) AS effect, pr.scope_type, pr.resource_type, pr.resource_id
            FROM governance.permission_rules pr JOIN ws ON ws.account_id = pr.account_id
           WHERE pr.workspace_id = @workspace_id
             AND pr.status = 'Active' AND pr.deleted_at IS NULL
             AND (pr.starts_at IS NULL OR pr.starts_at <= @now)
             AND (pr.expires_at IS NULL OR pr.expires_at > @now)
             AND pr.action = @action
             AND pr.subject_type = 'User' AND pr.subject_id = @user_id),
        user_permissions AS MATERIALIZED (
          SELECT rp.resource_id
            FROM governance.resource_permissions rp JOIN ws ON ws.account_id = rp.account_id
           WHERE rp.workspace_id = @workspace_id AND rp.resource_type = @resource_type
             AND rp.subject_type = 'User' AND rp.subject_id = @user_id
             AND rp.deleted_at IS NULL)
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
                      OR (res.visibility = 'Workspace' AND m.role <> 'Guest')
                      OR EXISTS (SELECT 1 FROM work.board_members bm
                                  WHERE bm.board_id = res.id AND bm.user_id = @user_id)
                      OR EXISTS (SELECT 1 FROM user_permissions up WHERE up.resource_id = res.id))))
        """;

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
                      OR (res.visibility = 'Workspace' AND m.role <> 'Guest')
                      OR EXISTS (SELECT 1 FROM user_permissions up WHERE up.resource_id = res.id))))
        """;

    /// <summary>
    /// Space visibility before Space membership exists: workspace Owners and
    /// Admins see every Space, other members see Workspace-visible Spaces (Guests
    /// none) and the Private Spaces they created.
    /// </summary>
    public const string VisibleSpacesSql = Principal + "\n" + """

        SELECT res.id
          FROM workspace.spaces res
          CROSS JOIN member m
         WHERE res.workspace_id = @workspace_id
           AND res.deleted_at IS NULL
           AND (
             m.role IN ('Owner', 'Admin')
             OR (res.visibility = 'Workspace' AND m.role <> 'Guest')
             OR res.created_by = @user_id)
        """;
}
