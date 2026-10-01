using Ida.Api.Auth;
using Ida.Application.Features.IncomeDocuments;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SystemSettingsEndpoints
{
    public static void MapTenantSystemSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapBadDebtTiersEndpoints();

        app.MapSlipSettingsEndpoints();

        app.MapPost("/api/master-data/slip-settings/sync",
                async (ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SyncSlipSettingsCommand(), ct)))
            .WithTags("ตั้งค่าการออกสลิป")
            .WithName("slip_settings_sync")
            .WithDescription("สร้างการตั้งค่าให้รหัสแพทย์ที่ใช้งานอยู่และยังไม่มี โดยใช้อีเมลจาก" +
                "ประวัติแพทย์ — ไม่แตะแถวที่มีอยู่แล้ว เรียกซ้ำได้")
            .Produces<SyncSlipSettingsResult>()
            .RequirePermission("slip-settings.write");

        app.MapHisNotifyEmailsEndpoints();
        app.MapIncomeDocSettingsEndpoints();
        app.MapHisDoctorCodeMapsEndpoints();
        app.MapExpiryAlertSettingsEndpoints();
        app.MapCheckinAreasEndpoints();
    }
}

