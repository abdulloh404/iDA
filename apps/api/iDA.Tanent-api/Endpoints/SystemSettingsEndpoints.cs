using Ida.Api.Auth;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee406;
using Ida.Application.Features.IncomeDocuments;
using Ida.Application.Features.SystemSettings;
using Ida.Application.Features.Users;
using Ida.Domain.Auth;
using Ida.Domain.Bu;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SystemSettingsEndpoints
{
    public static void MapTenantSystemSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<DfBadDebtTier, BadDebtTierListItem, BadDebtTierDetail, BadDebtTierInput>(
            Resource("bad-debt-tiers"));

        app.MapCrud<DocSlipSetting, SlipSettingListItem, SlipSettingDetail, SlipSettingInput>(
            Resource("slip-settings"));

        app.MapPost("/api/master-data/slip-settings/sync",
                async (ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SyncSlipSettingsCommand(), ct)))
            .WithTags("ตั้งค่าการออกสลิป")
            .WithName("slip_settings_sync")
            .WithDescription("สร้างการตั้งค่าให้รหัสแพทย์ที่ใช้งานอยู่และยังไม่มี โดยใช้อีเมลจาก" +
                "ประวัติแพทย์ — ไม่แตะแถวที่มีอยู่แล้ว เรียกซ้ำได้")
            .Produces<SyncSlipSettingsResult>()
            .RequirePermission("slip-settings.write");

        app.MapCrud<SysHisNotifyEmail, HisNotifyEmailListItem, HisNotifyEmailDetail,
            HisNotifyEmailInput>(Resource("his-notify-emails"));
        app.MapCrud<SysIncomeDocSetting, IncomeDocSettingDetail, IncomeDocSettingDetail,
            IncomeDocSettingInput>(Resource("income-doc-settings"));
        app.MapCrud<SysHisDoctorCodeMap, HisDoctorCodeMapListItem, HisDoctorCodeMapDetail,
            HisDoctorCodeMapInput>(Resource("his-doctor-code-maps"));
        app.MapCrud<SysExpiryAlertSetting, ExpiryAlertSettingDetail, ExpiryAlertSettingDetail,
            ExpiryAlertSettingInput>(Resource("expiry-alert-settings"));
        app.MapCrud<SysCheckinArea, CheckinAreaDetail, CheckinAreaDetail, CheckinAreaInput>(
            Resource("checkin-areas"));
    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException($"No CrudSpec declares the resource '{name}'.");
}

