namespace Notrelix.Application.Common.DTOs;

/// <summary>
/// Generic paginated result wrapper
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Data { get; }
    public int Total { get; }
    public int Page { get; }
    public int PageSize { get; }
    public bool HasMore => Page * PageSize < Total;
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    public PagedResult(IReadOnlyList<T> data, int total, int page, int pageSize)
    {
        Data = data;
        Total = total;
        Page = page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, page, pageSize);
    }
}
