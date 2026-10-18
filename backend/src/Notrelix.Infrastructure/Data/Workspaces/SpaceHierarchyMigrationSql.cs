namespace Notrelix.Infrastructure.Data.Workspaces;

/// <summary>
/// Existing-data steps of migration <c>AddSpaceHierarchyAndMembership</c>
/// (ADR-009, WSP-021..WSP-024). Applied in this order inside the migration
/// transaction; every statement is idempotent so a re-run after a partial
/// manual repair converges. Migration history is append-only: a later rule
/// change ships as a new migration instead of editing this SQL.
/// </summary>
public static class SpaceHierarchyMigrationSql
{
    /// <summary>Operator-readable error prefix raised by <see cref="Preflight"/>.</summary>
    public const string ErrorPrefix = "SPACE_HIERARCHY_PREFLIGHT_FAILED";

    /// <summary>
    /// Fail closed before any constraint is added: a Board whose (never written
    /// until now) <c>space_id</c> does not name a Space of its own Workspace
    /// cannot be given a guessed placement, so the migration aborts and leaves
    /// the schema unchanged. Pages have no placement column before this
    /// migration.
    /// </summary>
    public const string Preflight = $$"""
        DO $$
        DECLARE
            v_orphans bigint;
        BEGIN
            SELECT count(*) INTO v_orphans
              FROM work.boards b
             WHERE b.space_id IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1 FROM workspace.spaces s
                    WHERE s.id = b.space_id AND s.workspace_id = b.workspace_id);
            IF v_orphans > 0 THEN
                RAISE EXCEPTION '{{ErrorPrefix}}: % board(s) reference a space outside their workspace or a missing space', v_orphans;
            END IF;
        END $$;
        """;

    /// <summary>
    /// Every existing Space is a root (no hierarchy existed). Siblings get
    /// distinct canonical fractional keys in (lower(name), id) order: integer
    /// keys <c>a0..az</c>, then <c>b00..bzz</c>, then <c>c000..czzz</c>, which
    /// sort ordinally in rank order and satisfy the key grammar. Only rows still
    /// holding the column default are written.
    /// </summary>
    public const string BackfillPositions = """
        WITH digits AS (
            SELECT '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'::text AS d
        ),
        ranked AS (
            SELECT s.id,
                   (row_number() OVER (PARTITION BY s.workspace_id ORDER BY lower(s.name), s.id) - 1) AS n
              FROM workspace.spaces s
        )
        UPDATE workspace.spaces s
           SET position = CASE
                 WHEN r.n < 62 THEN 'a' || substr(d.d, r.n::int + 1, 1)
                 WHEN r.n < 3844 THEN 'b' || substr(d.d, (r.n / 62)::int + 1, 1) || substr(d.d, (r.n % 62)::int + 1, 1)
                 ELSE 'c' || substr(d.d, (r.n / 3844)::int + 1, 1)
                          || substr(d.d, ((r.n / 62) % 62)::int + 1, 1)
                          || substr(d.d, (r.n % 62)::int + 1, 1)
               END
          FROM ranked r, digits d
         WHERE s.id = r.id AND s.position = '';
        """;

    /// <summary>Closure self-rows for every existing (root) Space.</summary>
    public const string BackfillClosure = """
        INSERT INTO workspace.space_paths (ancestor_id, descendant_id, account_id, workspace_id, depth)
        SELECT s.id, s.id, s.account_id, s.workspace_id, 0
          FROM workspace.spaces s
        ON CONFLICT (ancestor_id, descendant_id) DO NOTHING;
        """;

    /// <summary>
    /// WSP-024: the creator of an existing Space becomes its Space Admin, so a
    /// Private Space keeps exactly the audience it had (Owners/Admins and its
    /// creator). Spaces without a recorded creator get no entry; workspace
    /// Owners/Admins still manage them.
    /// </summary>
    public const string BackfillCreatorAdmins = """
        INSERT INTO workspace.space_members
            (id, account_id, workspace_id, space_id, subject_type, subject_id, role, created_at, created_by)
        SELECT gen_random_uuid(), s.account_id, s.workspace_id, s.id, 'User', s.created_by, 'Admin', s.created_at, s.created_by
          FROM workspace.spaces s
         WHERE s.created_by IS NOT NULL
        ON CONFLICT (space_id, subject_type, subject_id) DO NOTHING;
        """;
}
