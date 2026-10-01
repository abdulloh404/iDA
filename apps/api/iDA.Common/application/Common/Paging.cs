namespace Ida.Application.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public enum StatusFilter
{
    All,
    Active,
    Inactive,
}

public class ListRequest
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 200;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    public string? Sort { get; set; }

    public string? Q { get; set; }

    public StatusFilter Status { get; set; } = StatusFilter.All;

    public Dictionary<string, string> Filters { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? Filter(string key) =>
        Filters.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    public T? Enum<T>(string key) where T : struct, Enum
    {
        if (Filter(key) is not { } raw) return null;

        var wanted = raw.Replace("_", string.Empty);
        foreach (var value in System.Enum.GetValues<T>())
            if (string.Equals(value.ToString()!.Replace("_", string.Empty), wanted,
                    StringComparison.OrdinalIgnoreCase))
                return value;

        return null;
    }

    public int Skip => (Page - 1) * PageSize;

    public (string Key, bool Descending) ParseSort(string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(Sort) ? fallback : Sort.Trim();
        return raw.StartsWith('-') ? (raw[1..], true) : (raw, false);
    }
}

