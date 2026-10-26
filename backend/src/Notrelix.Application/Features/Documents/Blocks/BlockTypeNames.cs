namespace Notrelix.Application.Features.Documents.Blocks;

/// <summary>
/// Reads a block type from the wire: the Domain name (<c>Heading1</c>) or the
/// editor's name (<c>heading_1</c>, <c>paragraph</c>).
/// </summary>
public static class BlockTypeNames
{
    private static readonly Dictionary<string, BlockType> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["paragraph"] = BlockType.Text,
        ["heading_1"] = BlockType.Heading1,
        ["heading_2"] = BlockType.Heading2,
        ["heading_3"] = BlockType.Heading3,
        ["bulleted_list"] = BlockType.BulletList,
        ["numbered_list"] = BlockType.NumberedList,
        ["todo"] = BlockType.ToDo,
        ["board_reference"] = BlockType.Board,
        ["page_reference"] = BlockType.PageReference,
        ["table_of_contents"] = BlockType.TableOfContents,
    };

    public static bool TryParse(string value, out BlockType type)
    {
        if (Aliases.TryGetValue(value, out type)) return true;
        return Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);
    }
}
