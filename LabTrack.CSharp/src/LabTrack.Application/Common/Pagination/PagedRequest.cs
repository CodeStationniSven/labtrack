namespace LabTrack.Application.Common.Pagination;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;
    private const int DefaultPageIndex = 1;

    private int _pageSize = DefaultPageSize;
    private int _pageIndex = DefaultPageIndex;

    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? DefaultPageIndex : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool IsDescending { get; init; }
}