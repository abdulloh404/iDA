using Ida.Application.Common.Crud;
using Ida.Application.Features.ShareRates;
using Ida.Domain.Bu;

namespace Ida.Api.Endpoints;

public static class ShareRateEndpoints
{
    public static void MapShareRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<ShareRate, ShareRateListItem, ShareRateDetail,
            ShareRateInput>(Resource("share-rates"));

        app.MapCrud<ShareRateExclusion, ShareExclusionRow, ShareExclusionDetail,
            ShareExclusionInput>(Resource("share-rate-exclusions"));

        app.MapCrud<MstPatientRight, MasterListItem, MasterDetail, MasterInput>(
            Resource("patient-rights"));
    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/ShareRates/, or remove the route.");
}

