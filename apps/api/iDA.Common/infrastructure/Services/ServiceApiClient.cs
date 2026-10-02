using System.Net.Http.Json;
using System.Text.Json;
using Ida.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Services;

public sealed class ServiceApiClient(HttpClient client)
{
    public const string KeyHeader = "X-Ida-Service-Key";
    public const string KeySetting = "IDA_SERVICE_API_KEY";

    public static string? ReadKey(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration["Api:ServiceKey"]) ? configuration[KeySetting] : configuration["Api:ServiceKey"];

    public static string ReadDestinationKey(IConfiguration configuration, string path)
    {
        var key = configuration[path];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32) throw new InvalidOperationException($"{path} must contain the destination API service key (at least 32 characters).");
        return key;
    }

    public static string WithPathBase(string baseUrl, string? pathBase)
    {
        var prefix = pathBase?.TrimEnd('/');
        if (string.IsNullOrEmpty(prefix)) return baseUrl.TrimEnd('/');
        if (!prefix.StartsWith('/') || prefix.Contains('?') || prefix.Contains('#') || prefix.Contains('\\') || prefix.Contains("//") || prefix.Split('/').Any(part => part is "." or ".."))
            throw new InvalidOperationException("API PathBase must be a path such as /core or /pt1.");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var address) || address.Scheme is not ("http" or "https")
            || address.UserInfo.Length > 0 || address.Query.Length > 0 || address.Fragment.Length > 0)
            throw new InvalidOperationException("An absolute HTTP(S) API URL without credentials, query or fragment is required.");
        var existingPath = address.AbsolutePath.TrimEnd('/');
        if (existingPath.Length > 0 && !string.Equals(existingPath, prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("The path in the API URL must match its configured PathBase.");
        return new UriBuilder(address) { Path = prefix }.Uri.AbsoluteUri.TrimEnd('/');
    }

    public async Task<T> PostAsync<T>(string baseUrl, string route, object input, string serviceKey, CancellationToken ct)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var address)
            || address.Scheme is not ("http" or "https") || address.UserInfo.Length > 0 || address.Query.Length > 0 || address.Fragment.Length > 0)
            throw new InvalidOperationException("An absolute HTTP(S) API URL without credentials, query or fragment is required.");
        if (string.IsNullOrWhiteSpace(serviceKey) || serviceKey.Length < 32) throw new InvalidOperationException("The destination API service key must be at least 32 characters.");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(address, route));
        request.Headers.Add(KeyHeader, serviceKey);
        request.Content = JsonContent.Create(input);
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new ApiException(503, "service_unavailable", "ไม่สามารถอ่านข้อมูลจากบริการที่เกี่ยวข้องได้ กรุณาลองใหม่");
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
                ?? throw new ApiException(503, "invalid_service_response", "บริการที่เกี่ยวข้องส่งข้อมูลไม่ครบถ้วน");
        }
        catch (Exception error) when (error is HttpRequestException or JsonException || error is OperationCanceledException && !ct.IsCancellationRequested)
        {
            throw new ApiException(503, "service_unavailable", "ไม่สามารถอ่านข้อมูลจากบริการที่เกี่ยวข้องได้ กรุณาลองใหม่");
        }
    }
}
