namespace TeamFlow.Application.Common.Pagination;

public sealed record PaginationRequest(int Page = 1, int PageSize = 25)
{
    public const int MaxPageSize = 200;

    public int SafePage => Page <= 0 ? 1 : Page;
    public int SafePageSize =>
        PageSize switch
        {
            <= 0 => 25,
            > MaxPageSize => MaxPageSize,
            _ => PageSize,
        };
    public int Skip => (SafePage - 1) * SafePageSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Empty(PaginationRequest req) =>
        new(Array.Empty<T>(), req.SafePage, req.SafePageSize, 0);
}
