namespace Notrelix.Infrastructure.Data.Documents;

/// <summary>
/// Existing-data step of migration <c>UpgradeBlocksToRichTextV1</c>: Blocks written before rich text v1 kept
/// their text, the format flags of the whole block (<c>bold</c>, <c>italic</c>, <c>underline</c>,
/// <c>strike</c>, <c>textColor</c>, <c>highlight</c>) and the type's settings in one flat content object
/// (or, for seeded blocks, a bare JSON string). Each such Block becomes:
/// <list type="bullet">
/// <item>text-like types: <c>{"v":1,"runs":[...]}</c> with one run holding the whole text and the flags as
/// marks over it (marks ordered by type; <c>code</c> blocks carry none; <c>default</c> text colour and
/// <c>none</c> highlight are no mark; the legacy <c>destructive</c> colour is <c>danger</c>);</item>
/// <item>properties: <c>align</c>, and per type <c>checked</c>, <c>language</c>, <c>tone</c>, <c>icon</c>,
/// <c>collapsed</c>, <c>attachmentId</c>, <c>width</c>, <c>alt</c>, <c>boardId</c>, <c>pageId</c> (from
/// <c>linkedPageId</c>) and table <c>rows</c> (each string cell becomes a cell of one run); every other
/// type keeps an empty content object.</item>
/// </list>
/// Nothing is discarded: a block that is rewritten keeps its previous content and properties in
/// <c>legacy_content</c> (<c>{"content":...,"properties":...}</c>), which is also where the members with no
/// v1 home (<c>mentionIds</c>, <c>pageMentionIds</c>, <c>title</c>, <c>items</c>, <c>fontFamily</c>,
/// <c>fontSize</c>, <c>linkedTaskId</c>) stay readable, and the down step restores from it. A value that
/// does not satisfy the v1 schema is left out of the properties instead of failing the upgrade.
/// <para>
/// Idempotent: a text-like block whose content already is <c>{"v":1,...}</c>, and a block of another type
/// whose content is already empty, is not touched; only rows whose upgraded form differs are written, and
/// each rewritten row's version moves forward so an editor holding the old shape re-reads before it writes.
/// </para>
/// <para>
/// Migration identity: the statements must see every tenant's rows, so the migration runs as the owner of
/// the <c>docs</c> tables without <c>FORCE ROW LEVEL SECURITY</c>, or as a <c>BYPASSRLS</c> role; under any
/// other identity the step first fails with <see cref="BlockedPrefix"/>.
/// </para>
/// </summary>
public static class BlockRichTextMigrationSql
{
    public const string BlockedPrefix = "BLOCK_RICH_TEXT_BACKFILL_BLOCKED";

    public const string AddLegacyColumn =
        "ALTER TABLE docs.blocks ADD COLUMN IF NOT EXISTS legacy_content jsonb NULL;";

    public const string DropLegacyColumn =
        "ALTER TABLE docs.blocks DROP COLUMN IF EXISTS legacy_content;";

    /// <summary>The pure upgrade of one block; created for the migration and dropped after it.</summary>
    public const string CreateUpgradeFunction = """
        CREATE OR REPLACE FUNCTION docs.block_upgrade_v1(p_type text, p_content jsonb, p_properties jsonb)
        RETURNS TABLE (new_content jsonb, new_properties jsonb)
        LANGUAGE plpgsql IMMUTABLE AS $fn$
        DECLARE
            v_text_like boolean := p_type IN ('Text','Heading1','Heading2','Heading3','BulletList','NumberedList','ToDo','Toggle','Quote','Callout','Code');
            v_guid constant text := '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            v_legacy jsonb;
            v_props jsonb := '{}'::jsonb;
            v_marks jsonb := '[]'::jsonb;
            v_text text;
            v_color text;
            v_value text;
        BEGIN
            IF v_text_like AND jsonb_typeof(p_content) = 'object' AND p_content -> 'v' = '1'::jsonb THEN
                new_content := p_content; new_properties := p_properties; RETURN NEXT; RETURN;
            END IF;
            IF NOT v_text_like AND p_content = '{}'::jsonb THEN
                new_content := p_content; new_properties := p_properties; RETURN NEXT; RETURN;
            END IF;

            v_legacy := CASE jsonb_typeof(p_content)
                WHEN 'object' THEN p_content
                WHEN 'string' THEN jsonb_build_object('text', p_content #>> '{}')
                ELSE '{}'::jsonb END;

            IF v_text_like THEN
                v_text := CASE WHEN jsonb_typeof(v_legacy -> 'text') = 'string' THEN left(v_legacy ->> 'text', 20000) ELSE '' END;

                IF p_type <> 'Code' THEN
                    IF v_legacy -> 'bold' = 'true'::jsonb THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'bold'));
                    END IF;
                    v_color := CASE WHEN jsonb_typeof(v_legacy -> 'textColor') = 'string' THEN v_legacy ->> 'textColor' END;
                    v_color := CASE v_color WHEN 'destructive' THEN 'danger' ELSE v_color END;
                    IF v_color IN ('muted','primary','accent','success','warning','danger') THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'color', 'token', v_color));
                    END IF;
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'highlight') = 'string' THEN v_legacy ->> 'highlight' END;
                    IF v_value IN ('muted','accent','primary','success','warning','danger') THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'highlight', 'token', v_value));
                    END IF;
                    IF v_legacy -> 'italic' = 'true'::jsonb THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'italic'));
                    END IF;
                    IF v_legacy -> 'strike' = 'true'::jsonb THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'strike'));
                    END IF;
                    IF v_legacy -> 'underline' = 'true'::jsonb THEN
                        v_marks := v_marks || jsonb_build_array(jsonb_build_object('type', 'underline'));
                    END IF;
                END IF;

                new_content := jsonb_build_object('v', 1, 'runs', CASE WHEN v_text = '' THEN '[]'::jsonb
                    ELSE jsonb_build_array(jsonb_build_object('text', v_text, 'marks', v_marks)) END);

                v_value := CASE WHEN jsonb_typeof(v_legacy -> 'align') = 'string' THEN v_legacy ->> 'align' END;
                IF v_value IN ('left','center','right','justify') THEN
                    v_props := v_props || jsonb_build_object('align', v_value);
                END IF;
                IF p_type = 'ToDo' AND jsonb_typeof(v_legacy -> 'checked') = 'boolean' THEN
                    v_props := v_props || jsonb_build_object('checked', v_legacy -> 'checked');
                END IF;
                IF p_type = 'Toggle' AND jsonb_typeof(v_legacy -> 'collapsed') = 'boolean' THEN
                    v_props := v_props || jsonb_build_object('collapsed', v_legacy -> 'collapsed');
                END IF;
                IF p_type = 'Code' AND jsonb_typeof(v_legacy -> 'language') = 'string' THEN
                    v_value := lower(v_legacy ->> 'language');
                    IF v_value ~ '^[a-z0-9+#-]{1,32}$' THEN
                        v_props := v_props || jsonb_build_object('language', v_value);
                    END IF;
                END IF;
                IF p_type = 'Callout' THEN
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'tone') = 'string' THEN v_legacy ->> 'tone' END;
                    IF v_value IN ('info','success','warning','danger','neutral') THEN
                        v_props := v_props || jsonb_build_object('tone', v_value);
                    END IF;
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'icon') = 'string' THEN v_legacy ->> 'icon' END;
                    IF v_value IS NOT NULL AND char_length(v_value) BETWEEN 1 AND 16 AND v_value !~ '[[:cntrl:]]' THEN
                        v_props := v_props || jsonb_build_object('icon', v_value);
                    END IF;
                END IF;
            ELSE
                new_content := '{}'::jsonb;

                IF p_type IN ('Image','File','Video') THEN
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'attachmentId') = 'string' THEN v_legacy ->> 'attachmentId' END;
                    IF v_value ~ v_guid AND v_value <> '00000000-0000-0000-0000-000000000000' THEN
                        v_props := v_props || jsonb_build_object('attachmentId', lower(v_value));
                    END IF;
                END IF;
                IF p_type = 'Image' THEN
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'width') = 'number' THEN v_legacy ->> 'width' END;
                    IF v_value ~ '^[0-9]{2,4}$' AND v_value::int BETWEEN 80 AND 2000 THEN
                        v_props := v_props || jsonb_build_object('width', v_value::int);
                    END IF;
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'alt') = 'string' THEN v_legacy ->> 'alt' END;
                    IF v_value IS NOT NULL AND char_length(v_value) <= 500 AND v_value !~ '[[:cntrl:]]' THEN
                        v_props := v_props || jsonb_build_object('alt', v_value);
                    END IF;
                END IF;
                IF p_type = 'Board' THEN
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'linkedBoardId') = 'string' THEN v_legacy ->> 'linkedBoardId' END;
                    IF v_value ~ v_guid AND v_value <> '00000000-0000-0000-0000-000000000000' THEN
                        v_props := v_props || jsonb_build_object('boardId', lower(v_value));
                    END IF;
                END IF;
                IF p_type = 'PageReference' THEN
                    v_value := CASE WHEN jsonb_typeof(v_legacy -> 'linkedPageId') = 'string' THEN v_legacy ->> 'linkedPageId' END;
                    IF v_value ~ v_guid AND v_value <> '00000000-0000-0000-0000-000000000000' THEN
                        v_props := v_props || jsonb_build_object('pageId', lower(v_value));
                    END IF;
                END IF;
                IF p_type = 'Table'
                   AND jsonb_typeof(v_legacy -> 'rows') = 'array'
                   AND jsonb_array_length(v_legacy -> 'rows') <= 500
                   AND length(v_legacy ->> 'rows') <= 200000
                   AND NOT EXISTS (
                        SELECT 1 FROM jsonb_array_elements(v_legacy -> 'rows') AS r(rw)
                         WHERE jsonb_typeof(r.rw) <> 'array'
                            OR jsonb_array_length(r.rw) > 50
                            OR EXISTS (SELECT 1 FROM jsonb_array_elements(r.rw) AS c(cell) WHERE jsonb_typeof(c.cell) <> 'string'
                                                                                                OR char_length(c.cell #>> '{}') > 20000))
                THEN
                    v_props := v_props || jsonb_build_object('rows', (
                        SELECT coalesce(jsonb_agg(
                            (SELECT coalesce(jsonb_agg(jsonb_build_object('runs',
                                        CASE WHEN cell.txt = '' THEN '[]'::jsonb
                                             ELSE jsonb_build_array(jsonb_build_object('text', cell.txt, 'marks', '[]'::jsonb)) END)
                                    ORDER BY cell.n), '[]'::jsonb)
                               FROM jsonb_array_elements_text(r.rw) WITH ORDINALITY AS cell(txt, n))
                            ORDER BY r.i), '[]'::jsonb)
                          FROM jsonb_array_elements(v_legacy -> 'rows') WITH ORDINALITY AS r(rw, i)));
                END IF;
            END IF;

            new_properties := v_props;
            RETURN NEXT;
        END
        $fn$;
        """;

    public const string DropUpgradeFunction = "DROP FUNCTION IF EXISTS docs.block_upgrade_v1(text, jsonb, jsonb);";

    /// <summary>Upgrades every legacy block; a re-run changes nothing.</summary>
    public const string Backfill = $$"""
        DO $$
        BEGIN
            IF row_security_active('docs.blocks') THEN
                RAISE EXCEPTION '{{BlockedPrefix}}: row-level security applies to role %; run migrations as the docs table owner without FORCE ROW LEVEL SECURITY or as a BYPASSRLS role', current_user;
            END IF;
        END $$;

        UPDATE docs.blocks b
           SET legacy_content = CASE
                   WHEN b.legacy_content IS NOT NULL THEN b.legacy_content
                   WHEN b.content = '{}'::jsonb AND b.properties = '{}'::jsonb THEN NULL
                   ELSE jsonb_build_object('content', b.content, 'properties', b.properties) END,
               content = u.new_content,
               properties = u.new_properties,
               version = b.version + 1
          FROM (SELECT x.id, up.new_content, up.new_properties
                  FROM docs.blocks x
                 CROSS JOIN LATERAL docs.block_upgrade_v1(x.type, x.content, x.properties) AS up) AS u
         WHERE u.id = b.id
           AND (u.new_content IS DISTINCT FROM b.content
                OR u.new_properties IS DISTINCT FROM b.properties);
        """;

    /// <summary>Puts back the content (and properties) of every block the backfill rewrote.</summary>
    public const string Restore = """
        UPDATE docs.blocks
           SET content = legacy_content -> 'content',
               properties = legacy_content -> 'properties',
               version = version + 1
         WHERE legacy_content ? 'content'
           AND legacy_content ? 'properties';
        """;
}
