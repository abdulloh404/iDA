using System.Globalization;
using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.SystemSettings;

public record IncomeDocSettingDetail(
    Guid Id,
    DocumentSendCycle PayslipCycle,
    int PayslipDay,
    PayslipArDetail PayslipArDetail,
    DocumentSendCycle Certificate406Cycle,
    int Certificate406Day,
    DocumentSendCycle Certificate50TawiCycle,
    int Certificate50TawiDay,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record IncomeDocSettingInput(
    DocumentSendCycle? PayslipCycle,
    int? PayslipDay,
    PayslipArDetail? PayslipArDetail,
    DocumentSendCycle? Certificate406Cycle,
    int? Certificate406Day,
    DocumentSendCycle? Certificate50TawiCycle,
    int? Certificate50TawiDay);

public sealed class IncomeDocSettingSpec
    : CrudSpec<SysIncomeDocSetting, IncomeDocSettingDetail, IncomeDocSettingDetail,
        IncomeDocSettingInput>
{
    public override string Resource => "income-doc-settings";
    public override string DisplayNameTh => "ตั้งค่าการส่งเอกสารรายได้";
    public override string Module => "system-settings";
    public override string DefaultSort => "updatedAt";
    public override bool IsSingleton => true;

    public override Expression<Func<SysIncomeDocSetting, IncomeDocSettingDetail>> ListProjection =>
        DetailProjection;

    public override Expression<Func<SysIncomeDocSetting, IncomeDocSettingDetail>> DetailProjection =>
        e => new IncomeDocSettingDetail(e.Id, e.PayslipCycle, e.PayslipDay, e.PayslipArDetail,
            e.Certificate406Cycle, e.Certificate406Day,
            e.Certificate50TawiCycle, e.Certificate50TawiDay,
            e.UpdatedAt, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysIncomeDocSetting, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysIncomeDocSetting, object?>>>
        {
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override void Apply(SysIncomeDocSetting e, IncomeDocSettingInput input, bool isCreate)
    {
        e.PayslipCycle = input.PayslipCycle ?? DocumentSendCycle.Monthly;
        e.PayslipDay = input.PayslipDay ?? 0;
        e.PayslipArDetail = input.PayslipArDetail ?? PayslipArDetail.Hide;
        e.Certificate406Cycle = input.Certificate406Cycle ?? DocumentSendCycle.Monthly;
        e.Certificate406Day = input.Certificate406Day ?? 0;
        e.Certificate50TawiCycle = input.Certificate50TawiCycle ?? DocumentSendCycle.Yearly;
        e.Certificate50TawiDay = input.Certificate50TawiDay ?? 0;
    }

    public override Task ValidateAsync(SysIncomeDocSetting e, IncomeDocSettingInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        CycleRule(errors, input.PayslipCycle, "payslipCycle");
        DayRule(errors, input.PayslipDay, "payslipDay");
        if (input.PayslipArDetail is null)
            errors.Required("payslipArDetail", "โปรดระบุรายละเอียดสลิปเงินเดือน");

        CycleRule(errors, input.Certificate406Cycle, "certificate406Cycle");
        DayRule(errors, input.Certificate406Day, "certificate406Day");
        CycleRule(errors, input.Certificate50TawiCycle, "certificate50TawiCycle");
        DayRule(errors, input.Certificate50TawiDay, "certificate50TawiDay");
        return Task.CompletedTask;
    }

    private static void CycleRule(ValidationFailure errors, DocumentSendCycle? value, string field)
    {
        if (value is null) errors.Required(field, "โปรดระบุรอบการส่ง");
    }

    private static void DayRule(ValidationFailure errors, int? value, string field)
    {
        if (value is null or 0)
            errors.Required(field, "โปรดระบุวันที่ส่งตามรอบ");
        else if (value is < 1 or > 31)
            errors.Add(field, "range", "วันที่ส่งตามรอบต้องอยู่ระหว่าง 1 ถึง 31");
    }
}

public record ExpiryAlertSettingDetail(
    Guid Id,
    int AlertDaysBefore,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record ExpiryAlertSettingInput(int? AlertDaysBefore);

public sealed class ExpiryAlertSettingSpec
    : CrudSpec<SysExpiryAlertSetting, ExpiryAlertSettingDetail, ExpiryAlertSettingDetail,
        ExpiryAlertSettingInput>
{
    public override string Resource => "expiry-alert-settings";
    public override string DisplayNameTh => "ตั้งค่าแจ้งเตือนรายการใกล้หมดอายุ";
    public override string Module => "system-settings";
    public override string DefaultSort => "updatedAt";
    public override bool IsSingleton => true;

    public override Expression<Func<SysExpiryAlertSetting, ExpiryAlertSettingDetail>> ListProjection =>
        DetailProjection;

    public override Expression<Func<SysExpiryAlertSetting, ExpiryAlertSettingDetail>> DetailProjection =>
        e => new ExpiryAlertSettingDetail(e.Id, e.AlertDaysBefore, e.UpdatedAt,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysExpiryAlertSetting, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysExpiryAlertSetting, object?>>>
        {
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override void Apply(SysExpiryAlertSetting e, ExpiryAlertSettingInput input, bool isCreate) =>
        e.AlertDaysBefore = input.AlertDaysBefore ?? 0;

    public override Task ValidateAsync(SysExpiryAlertSetting e, ExpiryAlertSettingInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        DayCount.Rule(errors, input.AlertDaysBefore, "alertDaysBefore",
            "จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ");
        return Task.CompletedTask;
    }
}

public record PasswordPolicyDetail(
    Guid Id,
    int ResetDays,
    int WarnDaysBefore,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record PasswordPolicyInput(int? ResetDays, int? WarnDaysBefore);

public sealed class PasswordPolicySpec
    : CrudSpec<SysPasswordPolicy, PasswordPolicyDetail, PasswordPolicyDetail, PasswordPolicyInput>
{
    public override string Resource => "password-policies";
    public override string DisplayNameTh => "ตั้งค่าการรีเซ็ตรหัสผ่าน";
    public override string Module => "system-settings";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "updatedAt";
    public override bool IsSingleton => true;

    public override Expression<Func<SysPasswordPolicy, PasswordPolicyDetail>> ListProjection =>
        DetailProjection;

    public override Expression<Func<SysPasswordPolicy, PasswordPolicyDetail>> DetailProjection =>
        e => new PasswordPolicyDetail(e.Id, e.ResetDays, e.WarnDaysBefore, e.UpdatedAt,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysPasswordPolicy, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysPasswordPolicy, object?>>>
        {
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override void Apply(SysPasswordPolicy e, PasswordPolicyInput input, bool isCreate)
    {
        e.ResetDays = input.ResetDays ?? 0;
        e.WarnDaysBefore = input.WarnDaysBefore ?? 0;
    }

    public override Task ValidateAsync(SysPasswordPolicy e, PasswordPolicyInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        DayCount.Rule(errors, input.ResetDays, "resetDays", "จำนวนวันที่ต้องรีเซ็ตรหัสผ่าน");
        DayCount.Rule(errors, input.WarnDaysBefore, "warnDaysBefore",
            "จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ");

        if (input.ResetDays is > 0 and var reset && input.WarnDaysBefore is > 0 and var warn &&
            warn >= reset)
            errors.Add("warnDaysBefore", "range",
                "โปรดระบุจำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ น้อยกว่าจำนวนวันที่ต้องรีเซ็ตรหัสผ่าน");
        return Task.CompletedTask;
    }
}

public record CheckinAreaDetail(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    int RadiusMeters,
    bool AllowOutside,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record CheckinAreaInput(
    string? Latitude,
    string? Longitude,
    int? RadiusMeters,
    bool AllowOutside);

public sealed class CheckinAreaSpec
    : CrudSpec<SysCheckinArea, CheckinAreaDetail, CheckinAreaDetail, CheckinAreaInput>
{
    public override string Resource => "checkin-areas";
    public override string DisplayNameTh => "ตั้งค่าพื้นที่การลงชื่อเข้าเวร";
    public override string Module => "system-settings";
    public override string DefaultSort => "updatedAt";
    public override bool IsSingleton => true;

    public override Expression<Func<SysCheckinArea, CheckinAreaDetail>> ListProjection =>
        DetailProjection;

    public override Expression<Func<SysCheckinArea, CheckinAreaDetail>> DetailProjection =>
        e => new CheckinAreaDetail(e.Id, e.Latitude, e.Longitude, e.RadiusMeters, e.AllowOutside,
            e.UpdatedAt, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysCheckinArea, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysCheckinArea, object?>>>
        {
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override void Apply(SysCheckinArea e, CheckinAreaInput input, bool isCreate)
    {
        e.Latitude = ParseCoordinate(input.Latitude) ?? 0;
        e.Longitude = ParseCoordinate(input.Longitude) ?? 0;
        e.RadiusMeters = input.RadiusMeters ?? 0;
        e.AllowOutside = input.AllowOutside;
    }

    public override Task ValidateAsync(SysCheckinArea e, CheckinAreaInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        CoordinateRule(errors, input.Latitude, "latitude", "Latitude", 90);
        CoordinateRule(errors, input.Longitude, "longitude", "Longitude", 180);

        if (input.RadiusMeters is null or 0)
            errors.Required("radiusMeters", "โปรดระบุระยะการลงชื่อเข้าเวร");
        else if (input.RadiusMeters is < 1 or > 99_999)
            errors.Add("radiusMeters", "range", "ระยะการลงชื่อเข้าเวรต้องอยู่ระหว่าง 1 ถึง 99,999 เมตร");
        return Task.CompletedTask;
    }

    private static void CoordinateRule(ValidationFailure errors, string? value, string field,
        string label, int limit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Required(field, $"โปรดระบุ{label}");
            return;
        }

        if (value.Trim().Length > 20 || ParseCoordinate(value) is not { } parsed)
            errors.Add(field, "invalid", $"{label} ต้องเป็นตัวเลข เช่น 13.765432");
        else if (Math.Abs(parsed) > limit)
            errors.Add(field, "range", $"{label} ต้องอยู่ระหว่าง -{limit} ถึง {limit}");
    }

    internal static decimal? ParseCoordinate(string? value) =>
        decimal.TryParse(value?.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var parsed)
            ? decimal.Round(parsed, 6)
            : null;
}

public record TermsDetail(
    Guid Id,
    string Content,
    int Version,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record TermsInput(string? Content);

public sealed class TermsSpec : CrudSpec<SysTerms, TermsDetail, TermsDetail, TermsInput>
{
    public const int MaxContentLength = 100_000;

    public override string Resource => "terms";
    public override string DisplayNameTh => "ตั้งค่าข้อกำหนดและเงื่อนไข";
    public override string Module => "system-settings";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "updatedAt";
    public override bool IsSingleton => true;

    public override Expression<Func<SysTerms, TermsDetail>> ListProjection => DetailProjection;

    public override Expression<Func<SysTerms, TermsDetail>> DetailProjection =>
        e => new TermsDetail(e.Id, e.Content, e.Version, e.UpdatedAt, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<SysTerms, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysTerms, object?>>>
        {
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override void Apply(SysTerms e, TermsInput input, bool isCreate)
    {
        var content = RichText.Sanitize(input.Content);

        if (!isCreate && content != e.Content) e.Version++;
        e.Content = content;
    }

    public override Task ValidateAsync(SysTerms e, TermsInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (RichText.IsBlank(input.Content))
            errors.Required("content", "โปรดระบุเนื้อหาข้อกำหนดและเงื่อนไข");
        else if (input.Content!.Length > MaxContentLength)
            errors.Add("content", "max_length", "เนื้อหาข้อกำหนดและเงื่อนไขยาวเกินไป");
        return Task.CompletedTask;
    }
}

internal static class DayCount
{

    public static void Rule(ValidationFailure errors, int? value, string field, string labelTh)
    {
        if (value is null or 0)
            errors.Required(field, $"โปรดระบุ{labelTh}");
        else if (value is < 1 or > 999)
            errors.Add(field, "range", $"{labelTh}ต้องอยู่ระหว่าง 1 ถึง 999 วัน");
    }
}

