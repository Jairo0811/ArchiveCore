namespace ArchiveCore.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    /// <summary>Normalizes user-supplied paging values into a safe, bounded range.</summary>
    public static PageRequest From(int? page, int? pageSize, int defaultSize = DefaultPageSize, int maxSize = MaxPageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedSize = pageSize is null or < 1 ? defaultSize : Math.Min(pageSize.Value, maxSize);
        return new PageRequest(normalizedPage, normalizedSize);
    }
}
