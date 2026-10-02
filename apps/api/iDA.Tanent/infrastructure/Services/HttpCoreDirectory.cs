using Ida.Application.Common;
using Ida.Application.Features.IngestConfiguration;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Services;

public sealed class HttpCoreDirectory(ServiceApiClient client, IConfiguration configuration) : ICoreDirectory
{
    private string CoreUrl
    {
        get
        {
            var url = configuration["Api:Core:Url"] is { Length: > 0 } configuredUrl ? configuredUrl : configuration["Api:CoreUrl"] ?? configuration["CORE_API_URL"] ?? throw new InvalidOperationException("Set Api:Core:Url for the Tenant API.");
            return ServiceApiClient.WithPathBase(url, configuration["Api:Core:PathBase"]);
        }
    }

    private Task<T> ReadAsync<T>(string resource, object input, CancellationToken ct) =>
        client.PostAsync<T>(CoreUrl, "api/internal/directory/" + resource, input, ServiceApiClient.ReadDestinationKey(configuration, "Api:Core:ServiceKey"), ct);

    private async Task<List<T>> ReadIdsAsync<T>(string resource, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var result = new List<T>();
        foreach (var batch in ids.Distinct().Chunk(500))
            result.AddRange(await ReadAsync<List<T>>(resource, new { Ids = batch }, ct));
        return result;
    }

    public Task<List<CoreDoctor>> DoctorsAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreDoctor>("doctors", ids, ct);
    public Task<List<Guid>> FindDoctorIdsAsync(CoreDoctorSearch search, CancellationToken ct) => ReadAsync<List<Guid>>("doctor-search", search, ct);
    public Task<List<Guid>> OrderDoctorIdsAsync(Guid[] ids, bool descending, CancellationToken ct) => ReadAsync<List<Guid>>("doctor-order", new { Ids = ids, Descending = descending }, ct);
    public Task<List<Guid>> OrderTaxAllowanceIdsAsync(Guid[] ids, bool descending, CancellationToken ct) => ReadAsync<List<Guid>>("tax-allowance-order", new { Ids = ids, Descending = descending }, ct);
    public Task<List<CoreDoctorEmail>> DoctorEmailsAsync(IEnumerable<Guid> doctorIds, CancellationToken ct) => ReadIdsAsync<CoreDoctorEmail>("doctor-emails", doctorIds, ct);
    public Task<List<CoreBank>> BanksAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreBank>("banks", ids, ct);
    public Task<List<CoreBankBranch>> BankBranchesAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreBankBranch>("bank-branches", ids, ct);
    public Task<List<CoreSpecialty>> SpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreSpecialty>("specialties", ids, ct);
    public Task<List<CoreSubSpecialty>> SubSpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreSubSpecialty>("sub-specialties", ids, ct);
    public Task<List<CoreDocumentType>> DocumentTypesAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreDocumentType>("document-types", ids, ct);
    public Task<List<CoreTaxAllowance>> TaxAllowancesAsync(IEnumerable<Guid> ids, CancellationToken ct) => ReadIdsAsync<CoreTaxAllowance>("tax-allowances", ids, ct);
    public Task<List<InterfaceDto>> IngestDefinitionsAsync(CancellationToken ct) => ReadAsync<List<InterfaceDto>>("ingest-definitions", new { }, ct);
}
