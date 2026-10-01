using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.SystemSettings;

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

