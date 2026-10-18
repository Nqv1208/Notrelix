namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Fail-closed ordering preflight (WM-V3-MIG-ORDER-001, SPEC WMREQ100). Runs first
/// in the forward migration that changes the position columns to ordinal text and
/// installs the unique ordering indexes. It aborts the migration transaction with
/// <see cref="ErrorPrefix"/> when any row holds a grammar-invalid key or any active
/// scope holds a duplicate key; it never rewrites or reorders data. Pre-production
/// databases are reset before the migration (SPEC WM-V3-DEC-006).
/// Applied by migration <c>20261010065641_CanonicalizeWorkOrdering</c>; migration
/// history is append-only (WMREQ103), so a later rule change ships as a new
/// migration instead of editing this SQL.
/// </summary>
public static class WorkOrderingPreflight
{
    /// <summary>Operator-readable error prefix raised by the preflight.</summary>
    public const string ErrorPrefix = "WM_ORDERING_PREFLIGHT_FAILED";

    /// <summary>(table, scope parent column, active-row predicate) of the six P3 scopes.</summary>
    public static readonly IReadOnlyList<(string Table, string Parent, string Active)> Scopes =
    [
        ("board_items", "group_id", "deleted_at IS NULL AND parent_item_id IS NULL"),
        ("board_groups", "board_id", "deleted_at IS NULL"),
        ("board_fields", "board_id", "deleted_at IS NULL"),
        ("checklists", "item_id", "deleted_at IS NULL"),
        ("checklist_items", "checklist_id", "true"),
        ("field_options", "field_id", "true"),
    ];

    // Integer-part length encoded by the head character, mirroring
    // FractionalIndexGenerator: 'A'..'Z' => 27 - index, 'a'..'z' => ascii - 95.
    private const string IntegerLength =
        "(CASE WHEN ascii(left(position, 1)) BETWEEN 65 AND 90 THEN 27 - (ascii(left(position, 1)) - 65) ELSE ascii(left(position, 1)) - 95 END)";

    /// <summary>
    /// SQL predicate that holds exactly for keys <c>FractionalIndex.Create</c> accepts:
    /// base62 characters, a valid integer head and length, not the smallest integer,
    /// and no trailing zero in the fractional part.
    /// </summary>
    public static readonly string ValidKeyPredicate =
        "position IS NOT NULL"
        + " AND position COLLATE \"C\" ~ '^[A-Za-z][0-9A-Za-z]*$'"
        + $" AND {IntegerLength} <= length(position)"
        + " AND position <> 'A' || repeat('0', 26)"
        + $" AND NOT (length(position) > {IntegerLength} AND right(position, 1) = '0')";

    public static string Sql
    {
        get
        {
            var scopes = string.Join(",\n        ", Scopes.Select(s => $"('{s.Table}', '{s.Parent}', '{s.Active.Replace("'", "''")}')"));
            var valid = ValidKeyPredicate.Replace("'", "''");
            return $$"""
                DO $preflight$
                DECLARE
                    scope record;
                    problem_count bigint;
                    sample text;
                    problems text := '';
                BEGIN
                    FOR scope IN SELECT * FROM (VALUES
                        {{scopes}}
                    ) AS s(tbl, parent, active)
                    LOOP
                        EXECUTE format('SELECT count(*), min(id::text) FROM work.%I WHERE NOT (%s)', scope.tbl, '{{valid}}')
                            INTO problem_count, sample;
                        IF problem_count > 0 THEN
                            problems := problems || format(' work.%s: %s invalid key(s) (e.g. id %s);', scope.tbl, problem_count, sample);
                        END IF;

                        EXECUTE format(
                            'SELECT count(*), min(parent_id) FROM (SELECT %I::text AS parent_id FROM work.%I WHERE %s GROUP BY %I, position COLLATE "C" HAVING count(*) > 1) d',
                            scope.parent, scope.tbl, scope.active, scope.parent)
                            INTO problem_count, sample;
                        IF problem_count > 0 THEN
                            problems := problems || format(' work.%s: %s duplicate active position(s) (e.g. %s %s);', scope.tbl, problem_count, scope.parent, sample);
                        END IF;
                    END LOOP;

                    IF problems <> '' THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = '{{ErrorPrefix}}:' || problems,
                            HINT = 'Reset the pre-production database before this migration (WM-V3-DEC-006); ordering data is never reordered automatically.';
                    END IF;
                END
                $preflight$;
                """;
        }
    }
}
