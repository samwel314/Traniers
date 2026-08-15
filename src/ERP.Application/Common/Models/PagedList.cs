namespace ERP.Application.Common.Models;

/// <summary>One page of results plus the metadata the client needs to page through the rest.</summary>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static PagedList<T> Empty(int pageNumber, int pageSize) => new([], pageNumber, pageSize, 0);

    public PagedList<TDestination> Map<TDestination>(Func<T, TDestination> projection)
        => new([.. Items.Select(projection)], PageNumber, PageSize, TotalCount);
}

/// <summary>Base for any paged/sorted/searched query. Values are clamped, never trusted.</summary>
public abstract record PagedRequest
{
    private const int MaxPageSize = 200;

    private readonly int _pageNumber = 1;
    private readonly int _pageSize = 20;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}
