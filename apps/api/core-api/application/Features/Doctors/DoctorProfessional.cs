using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.Doctors;

public record DoctorAffiliationRow(
    Guid Id, Guid DoctorId, string AffiliationName, string? PositionName, bool IsPrimary,
    RecordStatus Status);

public record DoctorAffiliationDetail(
    Guid Id, Guid DoctorId, string AffiliationName, string? PositionName, bool IsPrimary,
    RecordStatus Status, string RowVersion);

public record DoctorAffiliationInput(
    Guid DoctorId, string AffiliationName, string? PositionName, bool IsPrimary,
    RecordStatus Status);

public sealed class DoctorAffiliationSpec
    : DoctorChildSpec<DoctorAffiliation, DoctorAffiliationRow, DoctorAffiliationDetail,
        DoctorAffiliationInput>
{
    public override string Resource => "doctor-affiliations";
    public override string DisplayNameTh => "ต้นสังกัด";
    public override string DefaultSort => "affiliationName";

    public override Expression<Func<DoctorAffiliation, DoctorAffiliationRow>> ListProjection =>
        e => new DoctorAffiliationRow(e.Id, e.DoctorId, e.AffiliationName, e.PositionName,
            e.IsPrimary, e.Status);

    public override Expression<Func<DoctorAffiliation, DoctorAffiliationDetail>> DetailProjection =>
        e => new DoctorAffiliationDetail(e.Id, e.DoctorId, e.AffiliationName, e.PositionName,
            e.IsPrimary, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorAffiliation, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorAffiliation, object?>>>
        {
            ["affiliationName"] = e => e.AffiliationName,
            ["isPrimary"] = e => e.IsPrimary,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorAffiliation> Search(
        IQueryable<DoctorAffiliation> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorAffiliation e, DoctorAffiliationInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.AffiliationName = input.AffiliationName.Trim();
        e.PositionName = input.PositionName?.Trim();
        e.IsPrimary = input.IsPrimary;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorAffiliation e, DoctorAffiliationInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.AffiliationName, "affiliationName", "ต้นสังกัด");
        return Task.CompletedTask;
    }
}

public record DoctorFamilyRow(
    Guid Id, Guid DoctorId, string FullNameTh, RelationGroup RelationGroup, string? RelationName,
    string? NationalIdLast4, string? Hn, bool IsWelfareEligible, RecordStatus Status);

public record DoctorFamilyDetail(
    Guid Id, Guid DoctorId, string FullNameTh, string? FullNameEn, RelationGroup RelationGroup,
    string? RelationName, string? NationalIdLast4, string? Hn, bool IsWelfareEligible,
    RecordStatus Status, string RowVersion);

public record DoctorFamilyInput(
    Guid DoctorId, string FullNameTh, string? FullNameEn, RelationGroup RelationGroup,
    string? RelationName, string? NationalId, string? Hn, bool IsWelfareEligible,
    RecordStatus Status);

public sealed class DoctorFamilySpec
    : DoctorChildSpec<DoctorFamily, DoctorFamilyRow, DoctorFamilyDetail, DoctorFamilyInput>
{
    public override string Resource => "doctor-families";
    public override string DisplayNameTh => "ครอบครัวและบุคคลอ้างอิง";
    public override string DefaultSort => "fullNameTh";

    public override Expression<Func<DoctorFamily, DoctorFamilyRow>> ListProjection =>
        e => new DoctorFamilyRow(e.Id, e.DoctorId, e.FullNameTh, e.RelationGroup, e.RelationName,
            e.NationalIdLast4, e.Hn, e.IsWelfareEligible, e.Status);

    public override Expression<Func<DoctorFamily, DoctorFamilyDetail>> DetailProjection =>
        e => new DoctorFamilyDetail(e.Id, e.DoctorId, e.FullNameTh, e.FullNameEn, e.RelationGroup,
            e.RelationName, e.NationalIdLast4, e.Hn, e.IsWelfareEligible, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DoctorFamily, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorFamily, object?>>>
        {
            ["fullNameTh"] = e => e.FullNameTh,
            ["relationGroup"] = e => e.RelationGroup,
            ["isWelfareEligible"] = e => e.IsWelfareEligible,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorFamily> Search(IQueryable<DoctorFamily> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorFamily e, DoctorFamilyInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.FullNameTh = input.FullNameTh.Trim();
        e.FullNameEn = input.FullNameEn?.Trim();
        e.RelationGroup = input.RelationGroup;
        e.RelationName = input.RelationName?.Trim();
        e.Hn = input.Hn?.Trim();
        e.IsWelfareEligible = input.IsWelfareEligible;
        e.Status = input.Status;

        e.PendingSecrets[nameof(DoctorFamily.NationalIdEnc)] = input.NationalId;
    }

    public override Task ValidateAsync(DoctorFamily e, DoctorFamilyInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.FullNameTh, "fullNameTh", "ชื่อ-นามสกุล");

        var nationalId = input.NationalId?.Trim();
        if (!string.IsNullOrEmpty(nationalId) &&
            (nationalId.Length != 13 || !nationalId.All(char.IsAsciiDigit)))
            errors.Add("nationalId", "format", "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก");

        if (input.IsWelfareEligible && string.IsNullOrWhiteSpace(input.RelationName))
            errors.Required("relationName", "ผู้มีสิทธิ์สวัสดิการต้องระบุความสัมพันธ์");

        return Task.CompletedTask;
    }
}

public record DoctorProfessionalRecordRow(
    Guid Id, Guid DoctorId, string? RecordNo, DateOnly? RecordDate, string? HospitalName,
    string? Subject, string? RecordType, RecordStatus Status);

public record DoctorProfessionalRecordDetail(
    Guid Id, Guid DoctorId, string? RecordNo, DateOnly? RecordDate, string? HospitalName,
    string? Subject, string? RecordType, string? Conclusion, RecordStatus Status,
    string RowVersion);

public record DoctorProfessionalRecordInput(
    Guid DoctorId, string? RecordNo, DateOnly? RecordDate, string? HospitalName,
    string? Subject, string? RecordType, string? Conclusion, RecordStatus Status);

public sealed class DoctorProfessionalRecordSpec
    : DoctorChildSpec<DoctorProfessionalRecord, DoctorProfessionalRecordRow,
        DoctorProfessionalRecordDetail, DoctorProfessionalRecordInput>
{
    public override string Resource => "doctor-professional-records";
    public override string DisplayNameTh => "ประวัติวิชาชีพ";
    public override string DefaultSort => "-recordDate";

    public override Expression<Func<DoctorProfessionalRecord, DoctorProfessionalRecordRow>>
        ListProjection =>
        e => new DoctorProfessionalRecordRow(e.Id, e.DoctorId, e.RecordNo, e.RecordDate,
            e.HospitalName, e.Subject, e.RecordType, e.Status);

    public override Expression<Func<DoctorProfessionalRecord, DoctorProfessionalRecordDetail>>
        DetailProjection =>
        e => new DoctorProfessionalRecordDetail(e.Id, e.DoctorId, e.RecordNo, e.RecordDate,
            e.HospitalName, e.Subject, e.RecordType, e.Conclusion, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorProfessionalRecord, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorProfessionalRecord, object?>>>
        {
            ["recordDate"] = e => e.RecordDate,
            ["recordNo"] = e => e.RecordNo,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorProfessionalRecord> Search(
        IQueryable<DoctorProfessionalRecord> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorProfessionalRecord e, DoctorProfessionalRecordInput input,
        bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.RecordNo = input.RecordNo?.Trim();
        e.RecordDate = input.RecordDate;
        e.HospitalName = input.HospitalName?.Trim();
        e.Subject = input.Subject?.Trim();
        e.RecordType = input.RecordType?.Trim();
        e.Conclusion = input.Conclusion?.Trim();
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorProfessionalRecord e,
        DoctorProfessionalRecordInput input, bool isCreate, ValidationFailure errors,
        CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.Subject, "subject", "เรื่อง");
        return Task.CompletedTask;
    }
}

public record DoctorInsuranceRow(
    Guid Id, Guid DoctorId, string InsurerName, string? PolicyNo, short? PolicyYear,
    decimal? CoverageAmount, DateOnly? StartDate, DateOnly? EndDate, RecordStatus Status);

public record DoctorInsuranceDetail(
    Guid Id, Guid DoctorId, string InsurerName, string? PolicyNo, short? PolicyYear,
    decimal? CoverageAmount, DateOnly? StartDate, DateOnly? EndDate, string? DocumentUrl,
    RecordStatus Status, string RowVersion);

public record DoctorInsuranceInput(
    Guid DoctorId, string InsurerName, string? PolicyNo, short? PolicyYear,
    decimal? CoverageAmount, DateOnly? StartDate, DateOnly? EndDate, string? DocumentUrl,
    RecordStatus Status);

public sealed class DoctorInsuranceSpec
    : DoctorChildSpec<DoctorInsurance, DoctorInsuranceRow, DoctorInsuranceDetail,
        DoctorInsuranceInput>
{
    public override string Resource => "doctor-insurances";
    public override string DisplayNameTh => "ประกันความรับผิดทางวิชาชีพ";
    public override string DefaultSort => "-policyYear";

    public override Expression<Func<DoctorInsurance, DoctorInsuranceRow>> ListProjection =>
        e => new DoctorInsuranceRow(e.Id, e.DoctorId, e.InsurerName, e.PolicyNo, e.PolicyYear,
            e.CoverageAmount, e.StartDate, e.EndDate, e.Status);

    public override Expression<Func<DoctorInsurance, DoctorInsuranceDetail>> DetailProjection =>
        e => new DoctorInsuranceDetail(e.Id, e.DoctorId, e.InsurerName, e.PolicyNo, e.PolicyYear,
            e.CoverageAmount, e.StartDate, e.EndDate, e.DocumentUrl, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorInsurance, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorInsurance, object?>>>
        {
            ["policyYear"] = e => e.PolicyYear,
            ["insurerName"] = e => e.InsurerName,
            ["endDate"] = e => e.EndDate,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorInsurance> Search(
        IQueryable<DoctorInsurance> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorInsurance e, DoctorInsuranceInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.InsurerName = input.InsurerName.Trim();
        e.PolicyNo = input.PolicyNo?.Trim();
        e.PolicyYear = input.PolicyYear;
        e.CoverageAmount = input.CoverageAmount;
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;
        e.DocumentUrl = input.DocumentUrl?.Trim();
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorInsurance e, DoctorInsuranceInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.InsurerName, "insurerName", "บริษัทประกัน");

        if (input.CoverageAmount is < 0)
            errors.Add("coverageAmount", "range", "วงเงินคุ้มครองต้องไม่ติดลบ");

        if (input.StartDate is { } from && input.EndDate is { } to && to < from)
            errors.Add("endDate", "range", "วันที่สิ้นสุดความคุ้มครองต้องไม่ก่อนวันที่เริ่ม");

        return Task.CompletedTask;
    }
}

public record DoctorDocumentRow(
    Guid Id, Guid DoctorId, string? DocTypeName, string DocumentName, bool HasExpiry,
    DateOnly? ExpiryDate, string? UploadedBy, DateTimeOffset UploadedAt, RecordStatus Status);

public record DoctorDocumentDetail(
    Guid Id, Guid DoctorId, Guid DocTypeId, string DocumentName, string FileUrl,
    int? FileSizeKb, string? MimeType, bool HasExpiry, DateOnly? ExpiryDate,
    string? UploadedBy, DateTimeOffset UploadedAt, RecordStatus Status, string RowVersion);

public record DoctorDocumentInput(
    Guid DoctorId, Guid DocTypeId, string DocumentName, string FileUrl, bool HasExpiry,
    DateOnly? ExpiryDate, RecordStatus Status);

public sealed class DoctorDocumentSpec
    : DoctorChildSpec<DoctorDocument, DoctorDocumentRow, DoctorDocumentDetail, DoctorDocumentInput>
{
    public override string Resource => "doctor-documents";
    public override string DisplayNameTh => "เอกสารแนบ";
    public override string DefaultSort => "-uploadedAt";

    public override Expression<Func<DoctorDocument, DoctorDocumentRow>> ListProjection =>
        e => new DoctorDocumentRow(e.Id, e.DoctorId,
            e.DocType == null ? null : e.DocType.DocTypeNameTh,
            e.DocumentName, e.HasExpiry, e.ExpiryDate, e.UploadedBy, e.UploadedAt, e.Status);

    public override Expression<Func<DoctorDocument, DoctorDocumentDetail>> DetailProjection =>
        e => new DoctorDocumentDetail(e.Id, e.DoctorId, e.DocTypeId, e.DocumentName, e.FileUrl,
            e.FileSizeKb, e.MimeType, e.HasExpiry, e.ExpiryDate, e.UploadedBy, e.UploadedAt,
            e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorDocument, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorDocument, object?>>>
        {
            ["uploadedAt"] = e => e.UploadedAt,
            ["documentName"] = e => e.DocumentName,
            ["expiryDate"] = e => e.ExpiryDate,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorDocument> Search(
        IQueryable<DoctorDocument> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorDocument e, DoctorDocumentInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.UploadedAt = DateTimeOffset.UtcNow;
        }

        e.DocTypeId = input.DocTypeId;
        e.DocumentName = input.DocumentName.Trim();
        e.FileUrl = input.FileUrl.Trim();
        e.HasExpiry = input.HasExpiry;

        e.ExpiryDate = input.HasExpiry ? input.ExpiryDate : null;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorDocument e, DoctorDocumentInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.RequiredId(errors, input.DocTypeId, "docTypeId", "ประเภทเอกสาร");
        MasterFieldRules.Required(errors, input.DocumentName, "documentName", "ชื่อเอกสาร");
        MasterFieldRules.Required(errors, input.FileUrl, "fileUrl", "ไฟล์เอกสาร");

        if (input.HasExpiry && input.ExpiryDate is null)
            errors.Required("expiryDate", "เอกสารที่มีวันหมดอายุต้องระบุวันที่หมดอายุ");

        return Task.CompletedTask;
    }
}

public record DoctorHospitalLinkRow(
    Guid Id, Guid DoctorId, string HospitalId, string? HospitalName, bool IsHome,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, RecordStatus Status);

public record DoctorHospitalLinkDetail(
    Guid Id, Guid DoctorId, string HospitalId, bool IsHome, DateOnly EffectiveFrom,
    DateOnly? EffectiveTo, RecordStatus Status, string RowVersion);

public record DoctorHospitalLinkInput(
    Guid DoctorId, string HospitalId, bool IsHome, DateOnly EffectiveFrom,
    DateOnly? EffectiveTo, RecordStatus Status);

public sealed class DoctorHospitalLinkSpec
    : DoctorChildSpec<DoctorHospitalLink, DoctorHospitalLinkRow, DoctorHospitalLinkDetail,
        DoctorHospitalLinkInput>
{
    public override string Resource => "doctor-hospital-links";
    public override string DisplayNameTh => "โรงพยาบาลที่แพทย์มีข้อมูล";
    public override string DefaultSort => "hospitalId";

    public override Expression<Func<DoctorHospitalLink, DoctorHospitalLinkRow>> ListProjection =>
        e => new DoctorHospitalLinkRow(e.Id, e.DoctorId, e.HospitalId,
            e.Hospital == null ? null : e.Hospital.HospitalNameTh,
            e.IsHome, e.EffectiveFrom, e.EffectiveTo, e.Status);

    public override Expression<Func<DoctorHospitalLink, DoctorHospitalLinkDetail>>
        DetailProjection =>
        e => new DoctorHospitalLinkDetail(e.Id, e.DoctorId, e.HospitalId, e.IsHome,
            e.EffectiveFrom, e.EffectiveTo, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorHospitalLink, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorHospitalLink, object?>>>
        {
            ["hospitalId"] = e => e.HospitalId,
            ["effectiveFrom"] = e => e.EffectiveFrom,
            ["isHome"] = e => e.IsHome,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorHospitalLink> Search(
        IQueryable<DoctorHospitalLink> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorHospitalLink e, DoctorHospitalLinkInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.HospitalId = input.HospitalId;
        }

        e.IsHome = input.IsHome;
        e.EffectiveFrom = input.EffectiveFrom;
        e.EffectiveTo = input.EffectiveTo;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorHospitalLink e, DoctorHospitalLinkInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.HospitalId, "hospitalId", "โรงพยาบาล");

        if (input.EffectiveFrom == default)
            errors.Required("effectiveFrom", "โปรดระบุวันที่เริ่มมีข้อมูล");

        if (input.EffectiveTo is { } to && to < input.EffectiveFrom)
            errors.Add("effectiveTo", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorHospitalLink e,
        DoctorHospitalLinkInput input, bool isCreate, ValidationFailure errors,
        IRepository<DoctorHospitalLink> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var doctorId = input.DoctorId;
        var hospitalId = input.HospitalId;

        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorId == doctorId && o.HospitalId == hospitalId), ct))
            errors.Duplicate("hospitalId", "แพทย์รายนี้ผูกกับโรงพยาบาลแห่งนี้อยู่แล้ว");

        if (input.IsHome && await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorId == doctorId && o.IsHome), ct))
            errors.Add("isHome", "duplicate", "แพทย์รายนี้มีโรงพยาบาลต้นสังกัดอยู่แล้ว");
    }
}

