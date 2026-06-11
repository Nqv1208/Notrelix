namespace Notrelix.Application.Common.Models;

public class CursorPage<T>
{
    public T[] Items { get; set; } = Array.Empty<T>();
    public string? NextCursor { get; set; }
    public bool HasNextPage => NextCursor != null;

    public CursorPage() { }

    public CursorPage(IEnumerable<T> items, string? nextCursor)
    {
        Items = items.ToArray();
        NextCursor = nextCursor;
    }
}
