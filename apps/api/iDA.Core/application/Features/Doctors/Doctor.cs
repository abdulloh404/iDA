using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.Doctors;

public record DoctorListItem(
    Guid Id,
    string DoctorGlobalCode,
    string? TitleName,
    string FullNameTh,
    string? FullNameEn,
    GenderType Gender,
    string? NationalIdLast4,
    string? HomeHospitalId,
    bool IsCentralDoctor,
    ApprovalStatus ApprovalStatus,
    RecordStatus Status);

public record DoctorDetail(
    Guid Id,
    string DoctorGlobalCode,
    string? EpmsPersonId,
    Guid? TitleId,
    string FirstNameTh,
    string LastNameTh,
    string? FirstNameEn,
    string? LastNameEn,
    GenderType Gender,
    DateOnly? BirthDate,
    string? Nationality,
    IdDocType IdDocType,

    string? NationalIdLast4,
    DateOnly? IdDocExpiryDate,
    bool IdDocNoExpiry,
    string? TaxId,
    TaxEntityType TaxEntityType,
    bool IsCentralDoctor,
    string? HomeHospitalId,
    string? PhotoUrl,
    string? SpokenLanguages,
    bool TreatForeignPatient,
    DateOnly? FirstJoinDate,
    ApprovalStatus ApprovalStatus,
    RecordStatus Status,
    string? Remark,
    string SourceSystem,
    string RowVersion);

public record DoctorInput(
    string DoctorGlobalCode,
    Guid? TitleId,
    string FirstNameTh,
    string LastNameTh,
    string? FirstNameEn,
    string? LastNameEn,
    GenderType Gender,
    DateOnly? BirthDate,
    string? Nationality,
    IdDocType IdDocType,

    string? NationalId,
    string? PassportNo,
    DateOnly? IdDocExpiryDate,
    bool IdDocNoExpiry,
    string? TaxId,
    TaxEntityType TaxEntityType,
    bool IsCentralDoctor,
    string? HomeHospitalId,
    string? SpokenLanguages,
    bool TreatForeignPatient,
    DateOnly? FirstJoinDate,
    RecordStatus Status,
    string? Remark);

public sealed class DoctorSpec : CrudSpec<Doctor, DoctorListItem, DoctorDetail, DoctorInput>
{
    public override string Resource => "doctors";
    public override string DisplayNameTh => "ประวัติแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "doctorGlobalCode";

    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys =>
        ["gender", "approvalStatus", "homeHospitalId", "isCentralDoctor"];

    public override Expression<Func<Doctor, DoctorListItem>> ListProjection =>
        e => new DoctorListItem(e.Id, e.DoctorGlobalCode,
            e.Title == null ? null : e.Title.TitleNameTh,
            e.FirstNameTh + " " + e.LastNameTh,
            e.FirstNameEn == null ? null : e.FirstNameEn + " " + e.LastNameEn,
            e.Gender, e.NationalIdLast4, e.HomeHospitalId, e.IsCentralDoctor,
            e.ApprovalStatus, e.Status);

    public override Expression<Func<Doctor, DoctorDetail>> DetailProjection =>
        e => new DoctorDetail(e.Id, e.DoctorGlobalCode, e.EpmsPersonId, e.TitleId,
            e.FirstNameTh, e.LastNameTh, e.FirstNameEn, e.LastNameEn, e.Gender, e.BirthDate,
            e.Nationality, e.IdDocType, e.NationalIdLast4, e.IdDocExpiryDate, e.IdDocNoExpiry,
            e.TaxId, e.TaxEntityType, e.IsCentralDoctor, e.HomeHospitalId, e.PhotoUrl,
            e.SpokenLanguages, e.TreatForeignPatient, e.FirstJoinDate, e.ApprovalStatus,
            e.Status, e.Remark, e.SourceSystem, e.RowVersion.ToString());

    public override Expression<Func<Doctor, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.DoctorGlobalCode,
            e.FirstNameTh + " " + e.LastNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<Doctor, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<Doctor, object?>>>
        {
            ["doctorGlobalCode"] = e => e.DoctorGlobalCode,
            ["fullNameTh"] = e => e.FirstNameTh,
            ["gender"] = e => e.Gender,
            ["approvalStatus"] = e => e.ApprovalStatus,
            ["homeHospitalId"] = e => e.HomeHospitalId,
            ["status"] = e => e.Status,
        };

    public override IQueryable<Doctor> Search(IQueryable<Doctor> query, ListRequest r)
    {
        if (r.Enum<GenderType>("gender") is { } parsedGender)
            query = query.Where(e => e.Gender == parsedGender);

        if (r.Enum<ApprovalStatus>("approvalStatus") is { } parsedApproval)
            query = query.Where(e => e.ApprovalStatus == parsedApproval);

        if (r.Filter("homeHospitalId") is { } hospital)
            query = query.Where(e => e.HomeHospitalId == hospital);

        if (r.Filter("isCentralDoctor") is { } central)
            query = query.Where(e => e.IsCentralDoctor == (central == "true"));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.DoctorGlobalCode.Contains(q) ||
                e.FirstNameTh.Contains(q) ||
                e.LastNameTh.Contains(q) ||
                (e.FirstNameEn != null && e.FirstNameEn.Contains(q)) ||
                (e.LastNameEn != null && e.LastNameEn.Contains(q)));
        }

        return query;
    }

    public override void Apply(Doctor e, DoctorInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorGlobalCode = input.DoctorGlobalCode.Trim();
            e.SourceSystem = "MANUAL";

            e.ApprovalStatus = ApprovalStatus.Draft;
        }

        e.TitleId = input.TitleId;
        e.FirstNameTh = input.FirstNameTh.Trim();
        e.LastNameTh = input.LastNameTh.Trim();
        e.FirstNameEn = input.FirstNameEn?.Trim();
        e.LastNameEn = input.LastNameEn?.Trim();
        e.Gender = input.Gender;
        e.BirthDate = input.BirthDate;
        e.Nationality = input.Nationality?.Trim();
        e.IdDocType = input.IdDocType;
        e.IdDocExpiryDate = input.IdDocNoExpiry ? null : input.IdDocExpiryDate;
        e.IdDocNoExpiry = input.IdDocNoExpiry;
        e.TaxId = input.TaxId?.Trim();
        e.TaxEntityType = input.TaxEntityType;
        e.IsCentralDoctor = input.IsCentralDoctor;
        e.HomeHospitalId = input.HomeHospitalId;
        e.SpokenLanguages = input.SpokenLanguages?.Trim();
        e.TreatForeignPatient = input.TreatForeignPatient;
        e.FirstJoinDate = input.FirstJoinDate;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();

        e.PendingSecrets[nameof(Doctor.NationalIdEnc)] = input.NationalId;
        e.PendingSecrets[nameof(Doctor.PassportNoEnc)] = input.PassportNo;
    }

    public override Task ValidateAsync(Doctor e, DoctorInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.DoctorGlobalCode, "รหัสแพทย์กลาง", 20,
            field: "doctorGlobalCode");
        MasterFieldRules.Required(errors, input.FirstNameTh, "firstNameTh", "ชื่อ (ภาษาไทย)");
        MasterFieldRules.Required(errors, input.LastNameTh, "lastNameTh", "นามสกุล (ภาษาไทย)");

        var nationalId = input.NationalId?.Trim();
        if (!string.IsNullOrEmpty(nationalId) &&
            (nationalId.Length != 13 || !nationalId.All(char.IsAsciiDigit)))
            errors.Add("nationalId", "format", "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก");

        if (input.IdDocType == IdDocType.NationalId && isCreate &&
            string.IsNullOrWhiteSpace(nationalId))
            errors.Required("nationalId", "โปรดระบุเลขบัตรประชาชน");

        var taxId = input.TaxId?.Trim();
        if (!string.IsNullOrEmpty(taxId) &&
            (taxId.Length != 13 || !taxId.All(char.IsAsciiDigit)))
            errors.Add("taxId", "format", "เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก");

        if (input.BirthDate is { } birth && birth > DateOnly.FromDateTime(DateTime.UtcNow))
            errors.Add("birthDate", "range", "วันเกิดต้องไม่เป็นวันในอนาคต");

        if (!input.IdDocNoExpiry && input.IdDocExpiryDate is null &&
            input.IdDocType == IdDocType.Passport)
            errors.Required("idDocExpiryDate", "โปรดระบุวันที่หมดอายุ หรือเลือกไม่มีวันหมดอายุ");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(Doctor e, DoctorInput input,
        bool isCreate, ValidationFailure errors, IRepository<Doctor> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var code = input.DoctorGlobalCode.Trim();
        if (code.Length == 0) return;

        if (await exec.AnyAsync(repo.Query().Where(o => o.Id != e.Id && o.DoctorGlobalCode == code), ct))
            errors.Duplicate("doctorGlobalCode", "รหัสแพทย์กลางนี้ถูกใช้งานแล้ว");

    }

    public override IReadOnlyList<ExcelColumn<DoctorListItem>> ExportColumns =>
    [
        new("รหัสแพทย์กลาง", r => r.DoctorGlobalCode),
        new("คำนำหน้า", r => r.TitleName),
        new("ชื่อ-นามสกุล (ไทย)", r => r.FullNameTh),
        new("ชื่อ-นามสกุล (อังกฤษ)", r => r.FullNameEn),
        new("เพศ", r => GenderTh(r.Gender)),
        new("เลขบัตร (4 ตัวท้าย)", r => r.NationalIdLast4),
        new("สังกัดโรงพยาบาล", r => r.HomeHospitalId),
        new("แพทย์กลาง", r => r.IsCentralDoctor ? "ใช่" : "ไม่ใช่"),
        new("สถานะการอนุมัติ", r => ApprovalStatusLabels.ToThai(r.ApprovalStatus)),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string GenderTh(GenderType gender) => gender switch
    {
        GenderType.M => "ชาย",
        GenderType.F => "หญิง",
        _ => "ไม่ระบุ",
    };

}
