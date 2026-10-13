namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// Existing-data step of migration <c>AddStatusOptionCategory</c>: gives every
/// option of a Status field a work-status category. The option name is compared
/// trimmed, whitespace-collapsed, case- and Vietnamese-diacritic-insensitive:
/// <list type="number">
/// <item>"hoàn thành", "done", "xong", "completed", "complete" → Done;</item>
/// <item>"chờ duyệt", "review", "in review", "đang duyệt" → Review;</item>
/// <item>of the field's remaining options, the lowest position (ordinal, then id) → Todo;</item>
/// <item>every other option → InProgress.</item>
/// </list>
/// Only rows whose category is still null are written, so a re-run changes
/// nothing. Options of other field types keep a null category. Migration history
/// is append-only once released: a later mapping change ships as a new migration.
/// <para>
/// Migration identity: the statements must see every tenant's rows, so the
/// migration runs as the owner of the <c>work</c> tables without
/// <c>FORCE ROW LEVEL SECURITY</c>, or as a <c>BYPASSRLS</c> role. Under any other
/// identity row-level security would silently hide rows, so the backfill first
/// fails with <see cref="BlockedPrefix"/>; it finally fails with
/// <see cref="IncompletePrefix"/> if any Status option is still uncategorized.
/// </para>
/// <para>
/// One transaction: the backfill runs inside the migration transaction. Status
/// options are a few per Status field (tens per board), so the update is small and
/// short; no batching is needed.
/// </para>
/// </summary>
public static class StatusOptionCategoryMigrationSql
{
    public const string BlockedPrefix = "STATUS_CATEGORY_BACKFILL_BLOCKED";
    public const string IncompletePrefix = "STATUS_CATEGORY_BACKFILL_INCOMPLETE";

    private const string Accented =
        "àáảãạăằắẳẵặâầấẩẫậÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬèéẻẽẹêềếểễệÈÉẺẼẸÊỀẾỂỄỆìíỉĩịÌÍỈĨỊòóỏõọôồốổỗộơờớởỡợÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢùúủũụưừứửữựÙÚỦŨỤƯỪỨỬỮỰỳýỷỹỵỲÝỶỸỴđĐ";

    private const string Plain =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaeeeeeeeeeeeeeeeeeeeeeeiiiiiiiiiioooooooooooooooooooooooooooooooooouuuuuuuuuuuuuuuuuuuuuuyyyyyyyyyydd";

    private const string NormalizedName =
        $"lower(regexp_replace(btrim(translate(normalize(o.name, NFC), '{Accented}', '{Plain}')), '\\s+', ' ', 'g'))";

    private const string PendingStatusOptions = """
        FROM work.board_fields f
        WHERE f.id = o.field_id
          AND f.type = 'Status'
          AND o.category IS NULL
        """;

    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('work.field_options') OR row_security_active('work.board_fields') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the work table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        UPDATE work.field_options o
           SET category = 'Done'
          {{PendingStatusOptions}}
          AND {{NormalizedName}} IN ('hoan thanh', 'done', 'xong', 'completed', 'complete');

        UPDATE work.field_options o
           SET category = 'Review'
          {{PendingStatusOptions}}
          AND {{NormalizedName}} IN ('cho duyet', 'review', 'in review', 'dang duyet');

        UPDATE work.field_options o
           SET category = 'Todo'
          FROM (
              SELECT DISTINCT ON (p.field_id) p.id
                FROM work.field_options p
                JOIN work.board_fields pf ON pf.id = p.field_id
               WHERE pf.type = 'Status'
                 AND p.category IS NULL
               ORDER BY p.field_id, p.position COLLATE "C", p.id
          ) lowest
         WHERE o.id = lowest.id
           AND o.category IS NULL;

        UPDATE work.field_options o
           SET category = 'InProgress'
          {{PendingStatusOptions}};

        DO $$
        DECLARE
            v_missing bigint;
        BEGIN
            SELECT count(*) INTO v_missing
              FROM work.field_options o
              JOIN work.board_fields f ON f.id = o.field_id
             WHERE f.type = 'Status' AND o.category IS NULL;
            IF v_missing > 0 THEN
                RAISE EXCEPTION '{{IncompletePrefix}}: % Status option(s) still have no category', v_missing;
            END IF;
        END $$;
        """;
}
