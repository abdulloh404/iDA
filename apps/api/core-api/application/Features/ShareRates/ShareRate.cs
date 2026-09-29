using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.ShareRates;

public record ShareRateListItem(
    Guid Id,
    ShareRateLevel Level,
    SocialKind? SocialKind,
    string? PrivateCaseCode,
    string? PackageCode,
    string? Detail,
    string? Location,
    string? ArCode,
    string? PatientRightName,
    string? DoctorCode,
    string? TreatmentCode,
    string? TreatmentCategoryCode,
    string? DepartmentName,
    string? ActivityCode,
    ShareTaxKind TaxKind,
    AdmissionType AdmissionType,
    ShareMode ShareMode,
    decimal? SharePercent,
    decimal? FixPriceFrom,
    decimal? FixPriceTo,
    decimal? DoctorPayAmount,
    bool ExcludeXrayEkg,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    RecordStatus Status);

public record ShareRateDetail(
    Guid Id,
    ShareRateLevel Level,
    SocialKind? SocialKind,
    string? PrivateCaseCode,
    string? PackageCode,
    string? Detail,
    string? Location,
    Guid? ArCodeId,
    Guid? PatientRightId,
    Guid? DoctorCodeId,
    Guid? TreatmentId,
    Guid? TreatmentCategoryId,
    Guid? DepartmentId,
    string? ActivityCode,
    ShareTaxKind TaxKind,
    ShareTaxBase TaxBase,
    AdmissionType AdmissionType,
    ShareMode ShareMode,
    decimal? SharePercent,
    decimal? FixPriceFrom,
    decimal? FixPriceTo,
    decimal? DoctorPayAmount,
    bool ExcludeXrayEkg,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool NoExpiry,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ShareRateInput(
    ShareRateLevel Level,
    SocialKind? SocialKind,
    string? PrivateCaseCode,
    string? PackageCode,
    string? Detail,
    string? Location,
    Guid? ArCodeId,
    Guid? PatientRightId,
    Guid? DoctorCodeId,
    Guid? TreatmentId,
    Guid? TreatmentCategoryId,
    Guid? DepartmentId,
    string? ActivityCode,
    ShareTaxKind TaxKind,
    ShareTaxBase TaxBase,
    AdmissionType AdmissionType,
    ShareMode ShareMode,
    decimal? SharePercent,
    decimal? FixPriceFrom,
    decimal? FixPriceTo,
    decimal? DoctorPayAmount,
    bool ExcludeXrayEkg,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool NoExpiry,
    RecordStatus Status,
    string? Remark);

public sealed class ShareRateSpec
    : CrudSpec<ShareRate, ShareRateListItem, ShareRateDetail,
        ShareRateInput>
{
    public override string Resource => "share-rates";
    public override string DisplayNameTh => "ส่วนแบ่งค่าแพทย์";
    public override string Module => "share-rates";
    public override string DefaultSort => "-effectiveFrom";

    public override IReadOnlyList<string> FilterKeys =>
        ["level", "socialKind", "taxKind", "admissionType", "doctorCodeId", "treatmentId",
         "treatmentCategoryId", "arCodeId", "departmentId", "excludeXrayEkg"];

    public override Expression<Func<ShareRate, ShareRateListItem>> ListProjection =>
        e => new ShareRateListItem(e.Id, e.Level, e.SocialKind, e.PrivateCaseCode, e.PackageCode,
            e.Detail, e.Location,
            e.ArCode == null ? null : e.ArCode.Code,
            e.PatientRight == null ? null : e.PatientRight.NameTh,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.Treatment == null ? null : e.Treatment.Code,
            e.TreatmentCategory == null ? null : e.TreatmentCategory.Code,
            e.Department == null ? null : e.Department.NameTh,
            e.ActivityCode,
            e.TaxKind, e.AdmissionType, e.ShareMode, e.SharePercent, e.FixPriceFrom,
            e.FixPriceTo, e.DoctorPayAmount, e.ExcludeXrayEkg, e.EffectiveFrom, e.EffectiveTo,
            e.Status);

    public override Expression<Func<ShareRate, ShareRateDetail>> DetailProjection =>
        e => new ShareRateDetail(e.Id, e.Level, e.SocialKind, e.PrivateCaseCode, e.PackageCode,
            e.Detail, e.Location, e.ArCodeId, e.PatientRightId, e.DoctorCodeId, e.TreatmentId,
            e.TreatmentCategoryId, e.DepartmentId, e.ActivityCode, e.TaxKind, e.TaxBase, e.AdmissionType, e.ShareMode,
            e.SharePercent, e.FixPriceFrom, e.FixPriceTo, e.DoctorPayAmount, e.ExcludeXrayEkg,
            e.EffectiveFrom, e.EffectiveTo, e.NoExpiry, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<ShareRate, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<ShareRate, object?>>>
        {
            ["effectiveFrom"] = e => e.EffectiveFrom,
            ["sharePercent"] = e => e.SharePercent,
            ["privateCaseCode"] = e => e.PrivateCaseCode,
            ["packageCode"] = e => e.PackageCode,
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["treatmentCode"] = e => e.Treatment == null ? null : e.Treatment.Code,

            ["treatmentCategoryCode"] =
                e => e.TreatmentCategory == null ? null : e.TreatmentCategory.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<ShareRate> Search(
        IQueryable<ShareRate> q, ListRequest r)
    {

        if (r.Enum<ShareRateLevel>("level") is { } parsedLevel)
            q = q.Where(e => e.Level == parsedLevel);

        if (r.Enum<SocialKind>("socialKind") is { } parsedSocial)
            q = q.Where(e => e.SocialKind == parsedSocial);

        if (r.Enum<ShareTaxKind>("taxKind") is { } parsedTax)
            q = q.Where(e => e.TaxKind == parsedTax);

        if (r.Enum<AdmissionType>("admissionType") is { } parsedAdmission)
            q = q.Where(e => e.AdmissionType == parsedAdmission);

        if (r.Filter("excludeXrayEkg") is { } exclude)
            q = q.Where(e => e.ExcludeXrayEkg == (exclude == "true"));

        if (r.Filter("doctorCodeId") is { } doctorId && Guid.TryParse(doctorId, out var dId))
            q = q.Where(e => e.DoctorCodeId == dId);

        if (r.Filter("treatmentId") is { } treatmentId && Guid.TryParse(treatmentId, out var tId))
            q = q.Where(e => e.TreatmentId == tId);

        if (r.Filter("treatmentCategoryId") is { } categoryId &&
            Guid.TryParse(categoryId, out var cId))
            q = q.Where(e => e.TreatmentCategoryId == cId);

        if (r.Filter("arCodeId") is { } arCodeId && Guid.TryParse(arCodeId, out var aId))
            q = q.Where(e => e.ArCodeId == aId);

        if (r.Filter("departmentId") is { } departmentId &&
            Guid.TryParse(departmentId, out var depId))
            q = q.Where(e => e.DepartmentId == depId);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e =>
                (e.PrivateCaseCode != null && e.PrivateCaseCode.Contains(text)) ||
                (e.PackageCode != null && e.PackageCode.Contains(text)) ||
                (e.Detail != null && e.Detail.Contains(text)) ||
                (e.Location != null && e.Location.Contains(text)) ||
                (e.ActivityCode != null && e.ActivityCode.Contains(text)) ||
                (e.DoctorCode != null && e.DoctorCode.Code.Contains(text)) ||
                (e.Treatment != null && e.Treatment.Code.Contains(text)));
        }

        return q;
    }

    public override void Apply(ShareRate e, ShareRateInput input, bool isCreate)
    {

        if (isCreate) e.Level = input.Level;

        e.SocialKind = ShareRate.SchemeOf(e.Level) == ShareRateScheme.SocialSecurity
            ? input.SocialKind ?? Ida.Domain.Common.SocialKind.Pure
            : null;

        e.PrivateCaseCode = input.PrivateCaseCode?.Trim();
        e.PackageCode = input.PackageCode?.Trim();
        e.Detail = input.Detail?.Trim();
        e.Location = input.Location?.Trim();
        e.ArCodeId = input.ArCodeId;
        e.PatientRightId = input.PatientRightId;
        e.DoctorCodeId = input.DoctorCodeId;
        e.TreatmentId = input.TreatmentId;
        e.TreatmentCategoryId = input.TreatmentCategoryId;
        e.DepartmentId = input.DepartmentId;
        e.ActivityCode = input.ActivityCode?.Trim();
        e.TaxKind = input.TaxKind;
        e.TaxBase = input.TaxBase;
        e.AdmissionType = input.AdmissionType;
        e.ShareMode = input.ShareMode;

        if (input.ShareMode == ShareMode.Percent)
        {
            e.SharePercent = input.SharePercent;
            e.FixPriceFrom = null;
            e.FixPriceTo = null;
            e.DoctorPayAmount = null;
        }
        else
        {
            e.SharePercent = null;
            e.FixPriceFrom = input.FixPriceFrom;
            e.FixPriceTo = input.FixPriceTo;
            e.DoctorPayAmount = input.DoctorPayAmount;
        }

        e.ExcludeXrayEkg = input.ExcludeXrayEkg;
        e.EffectiveFrom = input.EffectiveFrom;
        e.NoExpiry = input.NoExpiry;
        e.EffectiveTo = input.NoExpiry ? null : input.EffectiveTo;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(ShareRate e, ShareRateInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {

        switch (input.Level)
        {
            case ShareRateLevel.PrivateCase:
                MasterFieldRules.Required(errors, input.PrivateCaseCode, "privateCaseCode",
                    "รหัส Private Case");
                break;

            case ShareRateLevel.Package:
                MasterFieldRules.Required(errors, input.PackageCode, "packageCode",
                    "รหัส Package");
                break;

            case ShareRateLevel.PatientRightArCode:
                MasterFieldRules.RequiredId(errors, input.ArCodeId, "arCodeId", "AR Code");
                break;

            case ShareRateLevel.DoctorTreatmentDepartment:
                MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
                MasterFieldRules.RequiredId(errors, input.TreatmentId, "treatmentId", "Treatment");
                MasterFieldRules.Required(errors, input.Location, "location", "Location");
                break;

            case ShareRateLevel.DoctorTreatment:
                MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");

                if (input.TreatmentId is null && input.TreatmentCategoryId is null)
                    errors.Required("treatmentId", "โปรดระบุ Treatment หรือ Treatment Category");
                else if (input.TreatmentId is not null && input.TreatmentCategoryId is not null)
                    errors.Add("treatmentId", "conflict",
                        "เลือกได้อย่างเดียวระหว่าง Treatment กับ Treatment Category");
                break;

            case ShareRateLevel.CategoryTreatment:
                MasterFieldRules.RequiredId(errors, input.TreatmentCategoryId,
                    "treatmentCategoryId", "Category");
                MasterFieldRules.RequiredId(errors, input.TreatmentId, "treatmentId", "Treatment");
                break;

            case ShareRateLevel.Treatment:
                MasterFieldRules.RequiredId(errors, input.TreatmentId, "treatmentId", "Treatment");
                break;

            case ShareRateLevel.Category:
                MasterFieldRules.RequiredId(errors, input.TreatmentCategoryId,
                    "treatmentCategoryId", "Category");
                break;

            case ShareRateLevel.SocialArCode:
                MasterFieldRules.RequiredId(errors, input.ArCodeId, "arCodeId", "AR Code");
                break;

            case ShareRateLevel.SocialDoctorTreatment:
                MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
                break;

            case ShareRateLevel.SocialDepartmentTreatment:
                MasterFieldRules.RequiredId(errors, input.DepartmentId, "departmentId", "แผนก");
                break;

            case ShareRateLevel.SocialDoctorActivity:
                MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
                MasterFieldRules.Required(errors, input.ActivityCode, "activityCode", "รหัส Activity");
                break;

            case ShareRateLevel.SocialTreatment:
                MasterFieldRules.RequiredId(errors, input.TreatmentId, "treatmentId", "Treatment");
                break;

            case ShareRateLevel.SocialActivity:
                MasterFieldRules.Required(errors, input.ActivityCode, "activityCode", "รหัส Activity");
                break;

            case ShareRateLevel.SocialBase:
                break;
        }

        if (ShareRate.SchemeOf(input.Level) == ShareRateScheme.SocialSecurity &&
            input.SocialKind is null)
            errors.Required("socialKind", "โปรดระบุรหัสประกันสังคม");

        if (input.EffectiveFrom == default)
            errors.Required("effectiveFrom", "โปรดระบุวันที่เริ่มใช้");

        if (!input.NoExpiry && input.EffectiveTo is null)
            errors.Required("effectiveTo", "โปรดระบุวันที่สิ้นสุด หรือเลือกไม่มีวันหมดอายุ");

        if (input.EffectiveTo is { } to && to < input.EffectiveFrom)
            errors.Add("effectiveTo", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้");

        if (input.ShareMode == ShareMode.Percent)
        {
            if (input.SharePercent is null)
                errors.Required("sharePercent", "โปรดระบุส่วนแบ่งแบบเปอร์เซ็นต์");
            else if (input.SharePercent is < 0 or > 100)
                errors.Add("sharePercent", "range", "ส่วนแบ่งต้องอยู่ระหว่าง 0 ถึง 100");
        }
        else
        {
            if (input.DoctorPayAmount is null)
                errors.Required("doctorPayAmount", "โปรดระบุส่วนที่ทำจ่ายแพทย์");
            else if (input.DoctorPayAmount is < 0)
                errors.Add("doctorPayAmount", "range", "ส่วนที่ทำจ่ายแพทย์ต้องไม่ติดลบ");

            if (input.FixPriceFrom is { } from && input.FixPriceTo is { } priceTo &&
                priceTo < from)
                errors.Add("fixPriceTo", "range", "ราคาสิ้นสุดต้องไม่น้อยกว่าราคาเริ่มต้น");
        }

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(ShareRate e,
        ShareRateInput input, bool isCreate, ValidationFailure errors,
        IRepository<ShareRate> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (input.EffectiveFrom == default) return;

        var level = input.Level;
        var from = input.EffectiveFrom;
        var to = input.NoExpiry ? null : input.EffectiveTo;

        var clash = repo.Query().Where(o =>
            o.Id != e.Id &&
            o.Level == level &&
            o.AdmissionType == input.AdmissionType &&
            o.PrivateCaseCode == input.PrivateCaseCode &&
            o.PackageCode == input.PackageCode &&
            o.Location == input.Location &&
            o.ArCodeId == input.ArCodeId &&
            o.PatientRightId == input.PatientRightId &&
            o.DoctorCodeId == input.DoctorCodeId &&
            o.TreatmentId == input.TreatmentId &&
            o.TreatmentCategoryId == input.TreatmentCategoryId &&
            o.DepartmentId == input.DepartmentId &&
            o.ActivityCode == input.ActivityCode &&
            o.SocialKind == input.SocialKind &&
            (to == null || o.EffectiveFrom <= to) &&
            (o.EffectiveTo == null || o.EffectiveTo >= from));

        if (await exec.AnyAsync(clash, ct))
            errors.Add("effectiveFrom", "overlap",
                "มีอัตราที่เงื่อนไขเดียวกันและช่วงวันที่ทับกันอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<ShareRateListItem>> ExportColumns =>
    [
        new("ระดับ", r => LevelTh(r.Level)),
        new("รหัสประกันสังคม", r => r.SocialKind is { } k ? SocialKindTh(k) : null),
        new("รหัส Private Case", r => r.PrivateCaseCode),
        new("รหัส Package", r => r.PackageCode),
        new("รายละเอียด", r => r.Detail),
        new("AR Code", r => r.ArCode),
        new("สิทธิ์คนไข้", r => r.PatientRightName),
        new("แพทย์", r => r.DoctorCode),
        new("Treatment", r => r.TreatmentCode),
        new("Category", r => r.TreatmentCategoryCode),
        new("แผนก", r => r.DepartmentName),
        new("Activity", r => r.ActivityCode),
        new("Location", r => r.Location),
        new("ประเภทภาษี", r => TaxKindTh(r.TaxKind)),
        new("Admission Type", r => AdmissionTh(r.AdmissionType)),
        new("ส่วนแบ่ง (%)", r => r.SharePercent, "#,##0.00"),
        new("Fix Price ตั้งแต่", r => r.FixPriceFrom, "#,##0.00"),
        new("Fix Price ถึง", r => r.FixPriceTo, "#,##0.00"),
        new("ส่วนที่ทำจ่ายแพทย์", r => r.DoctorPayAmount, "#,##0.00"),
        new("ยกเว้น X-ray & EKG", r => r.ExcludeXrayEkg ? "ยกเว้น" : "คำนวณปกติ"),
        new("วันที่เริ่มใช้", r => r.EffectiveFrom.ToString("dd/MM/yyyy")),
        new("วันที่สิ้นสุด", r => r.EffectiveTo?.ToString("dd/MM/yyyy") ?? "ไม่มีวันหมดอายุ"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string LevelTh(ShareRateLevel level) => level switch
    {
        ShareRateLevel.PrivateCase => "ระดับ Private Case",
        ShareRateLevel.PatientRightArCode => "ระดับ สิทธิ์ & AR Code",
        ShareRateLevel.Package => "ระดับ Package",
        ShareRateLevel.DoctorTreatmentDepartment => "ระดับ Doctor Treatment Department",
        ShareRateLevel.DoctorTreatment => "ระดับ Doctor Treatment",
        ShareRateLevel.CategoryTreatment => "ระดับ Category Treatment",
        ShareRateLevel.Treatment => "ระดับ Treatment",
        ShareRateLevel.Category => "ระดับ Category",
        ShareRateLevel.SocialArCode => "ระดับ AR Code",
        ShareRateLevel.SocialDoctorTreatment => "ระดับ Doctor Treatment",
        ShareRateLevel.SocialDepartmentTreatment => "ระดับ Department Treatment",
        ShareRateLevel.SocialDoctorActivity => "ระดับ Doctor Activity",
        ShareRateLevel.SocialTreatment => "ระดับ Treatment",
        ShareRateLevel.SocialActivity => "ระดับ Activity",
        ShareRateLevel.SocialBase => "ส่วนแบ่งหลัก",
        _ => level.ToString(),
    };

    public static string SocialKindTh(SocialKind kind) =>
        kind == SocialKind.Pure ? "ประกันสังคมเพียว" : "ประกันสังคมสิทธิ์ร่วม";

    public static string TaxKindTh(ShareTaxKind kind) =>
        kind == ShareTaxKind.Tax406 ? "ภาษี 40(6)" : "ไม่กระทบฐานภาษี";

    public static string AdmissionTh(AdmissionType type) => type switch
    {
        AdmissionType.Ipd => "IPD",
        AdmissionType.Opd => "OPD",
        _ => "ทั้งหมด",
    };
}

