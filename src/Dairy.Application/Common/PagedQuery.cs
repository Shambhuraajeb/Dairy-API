namespace Dairy.Application.Common;

/// <summary>Standard list query. Derive (e.g. FarmerQuery : PagedQuery) to add module filters.</summary>
public class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : Math.Min(value, 200);
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }

    public int Offset => (Page - 1) * PageSize;

    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// LIKE pattern ("%text%", wildcards escaped) or null when no search text. Use as
    /// <c>(@Search IS NULL OR col LIKE @Search)</c> with <c>new { Search = query.SearchPattern }</c>.
    /// </summary>
    public string? SearchPattern
    {
        get
        {
            var s = Search?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return "%" + s + "%";
        }
    }
}

public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, long total, int page, int pageSize)
    {
        Items = items;
        Total = total;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }
    public long Total { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
