using Ida.Application.Common;

namespace Ida.Api.Endpoints;

public static class ListQueryString
{
    public static ListRequest Read(HttpRequest http)
    {
        var query = http.Query;
        var request = new ListRequest
        {
            Page = ParseInt(query["page"], 1),
            PageSize = ParseInt(query["pageSize"], ListRequest.DefaultPageSize),
            Sort = query["sort"],
            Q = query["q"],
            Status = ParseStatus(query["status"]),
        };

        foreach (var pair in query)
        {
            if (pair.Key is "page" or "pageSize" or "sort" or "q") continue;
            request.Filters[pair.Key] = pair.Value.ToString();
        }

        return request;
    }

    public static string? RowVersion(HttpRequest http) =>
        http.Query["rowVersion"].FirstOrDefault()
        ?? http.Headers["If-Match"].FirstOrDefault();

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) ? parsed : fallback;

    private static StatusFilter ParseStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "active" => StatusFilter.Active,
        "inactive" => StatusFilter.Inactive,
        _ => StatusFilter.All,
    };
}

