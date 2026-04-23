namespace Notrelix.Domain.Enums;

// Loại block trong Notion-like document editor
public enum BlockType
{
    // Text
    Paragraph = 0,
    Heading1 = 1,
    Heading2 = 2,
    Heading3 = 3,
    BulletedList = 4,
    NumberedList = 5,
    Toggle = 6,
    Quote = 7,
    Callout = 8,
    Code = 9,
    Divider = 10,

    // Media
    Image = 20,
    Video = 21,
    File = 22,
    Embed = 23,
    Bookmark = 24,

    // Data
    Table = 30,
    TableRow = 31,
    Todo = 32,

    // Reference
    CardRef = 40,
    ChildPage = 41,

    // Layout
    ColumnList = 50,
    Column = 51
}
