using System.Net.Http.Json;
using System.Text.Json;
using Ida.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Services;

public sealed class ServiceApiClient(HttpClient client, IConfiguration configuration)
{
    public const string KeyHeader = "X-Ida-Service-Key";
    public const string KeySetting = "IDA_SERVICE_API_KEY";

    public async Task<T> PostAsync<T>(string baseUrl, string route, object input, CancellationToken ct)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var address)
            || address.Scheme is not ("http" or "https") || address.UserInfo.Length > 0 || address.Query.Length > 0 || address.Fragment.Length > 0)
            throw new InvalidOperationException("An absolute HTTP(S) API URL without credentials, query or fragment is required.");
        var key = configuration[KeySetting];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32) throw new InvalidOperationException($"Set {KeySetting} to the same secret (at least 32 characters) on Core and Tenant APIs.");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(address, route));
        request.Headers.Add(KeyHeader, key);
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
