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
/// is append-only: a later mapping change ships as a new migration.
/// </summary>
public static class StatusOptionCategoryMigrationSql
{
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
        """;
}
