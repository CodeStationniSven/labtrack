namespace LabTrack.Application.Common.Pagination;

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResponse(IReadOnlyCollection<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }

    public static PagedResponse<T> Create(IReadOnlyCollection<T> items, int count, int pageIndex, int pageSize)
    {
        return new PagedResponse<T>(items, count, pageIndex, pageSize);
    }
}