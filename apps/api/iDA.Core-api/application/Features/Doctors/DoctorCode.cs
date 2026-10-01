using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.Doctors;

public record DoctorCodeListItem(
    Guid Id,
    Guid DoctorId,
    string Code,
    string? OldDoctorCode,
    string DisplayNameTh,
    bool IsDutyDoctor,
    string? DepartmentCode,
    string? DoctorTypeCode,
    EmploymentStatus EmploymentStatus,
    ApprovalStatus ApprovalStatus,
    RecordStatus Status);

public record DoctorCodeDetail(
    Guid Id,
    Guid DoctorId,
    string Code,
    string? OldDoctorCode,
    bool IsCentralCode,
    string DisplayNameTh,
    string? DisplayNameEn,
    string? TaxInvoiceNameTh,
    string? TaxInvoiceNameEn,
    string? BoardStatus,
    string? DefaultDfDoctorCode,
    bool IsDutyDoctor,
    string? WhtFormType,
    bool CanCheckinAnywhere,
    bool ArNoWaitPayment,
    bool CalcToHospitalNoPay,
    Guid? DoctorTypeId,
    Guid? DoctorGroupId,
    Guid? DepartmentId,
    Guid? ClinicId,
    Guid? PrivilegeTypeId,
    Guid? StatusPrivilegeId,
    string? EmployeeCode,
    Guid? PaymentTypeId,
    DateOnly? StartWorkDate,
    EmploymentStatus EmploymentStatus,
    DateOnly? ResignDate,
    string? PaymentCondition,
    bool HospitalAbsorbCcFee,
    decimal? CcFeePercent,
    string? TaxId,
    bool UseHomeTaxAddress,
    string? TaxAddrNo,
    string? TaxAddrBuilding,
    string? TaxAddrSoi,
    string? TaxAddrRoad,
    string? TaxAddrSubdistrict,
    string? TaxAddrDistrict,
    string? TaxAddrProvince,
    string? TaxAddrPostcode,
    string[] PublishChannels,
    ApprovalStatus ApprovalStatus,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record DoctorCodeInput(
    Guid DoctorId,
    string Code,
    string? OldDoctorCode,
    bool IsCentralCode,
    string DisplayNameTh,
    string? DisplayNameEn,
    string? TaxInvoiceNameTh,
    string? TaxInvoiceNameEn,
    string? BoardStatus,
    string? DefaultDfDoctorCode,
    bool IsDutyDoctor,
    string? WhtFormType,
    bool CanCheckinAnywhere,
    bool ArNoWaitPayment,
    bool CalcToHospitalNoPay,
    Guid? DoctorTypeId,
    Guid? DoctorGroupId,
    Guid? DepartmentId,
    Guid? ClinicId,
    Guid? PrivilegeTypeId,
    Guid? StatusPrivilegeId,
    string? EmployeeCode,
    Guid? PaymentTypeId,
    DateOnly? StartWorkDate,
    EmploymentStatus EmploymentStatus,
    DateOnly? ResignDate,
    string? PaymentCondition,
    bool HospitalAbsorbCcFee,
    decimal? CcFeePercent,
    string? TaxId,
    bool UseHomeTaxAddress,
    string? TaxAddrNo,
    string? TaxAddrBuilding,
    string? TaxAddrSoi,
    string? TaxAddrRoad,
    string? TaxAddrSubdistrict,
    string? TaxAddrDistrict,
    string? TaxAddrProvince,
    string? TaxAddrPostcode,
    string[] PublishChannels,
    RecordStatus Status,
    string? Remark);

public sealed class DoctorCodeSpec
    : CrudSpec<DoctorCode, DoctorCodeListItem, DoctorCodeDetail, DoctorCodeInput>
{
    public override string Resource => "doctor-codes";
    public override string DisplayNameTh => "รหัสแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["doctorId", "departmentId", "isDutyDoctor", "approvalStatus", "employmentStatus"];

    public override Expression<Func<DoctorCode, DoctorCodeListItem>> ListProjection =>
        e => new DoctorCodeListItem(e.Id, e.DoctorId, e.Code, e.OldDoctorCode, e.DisplayNameTh,
            e.IsDutyDoctor,
            e.Department == null ? null : e.Department.Code,
            e.DoctorType == null ? null : e.DoctorType.Code,
            e.EmploymentStatus, e.ApprovalStatus, e.Status);

    public override Expression<Func<DoctorCode, DoctorCodeDetail>> DetailProjection =>
        e => new DoctorCodeDetail(e.Id, e.DoctorId, e.Code, e.OldDoctorCode, e.IsCentralCode,
            e.DisplayNameTh, e.DisplayNameEn, e.TaxInvoiceNameTh, e.TaxInvoiceNameEn,
            e.BoardStatus, e.DefaultDfDoctorCode, e.IsDutyDoctor, e.WhtFormType,
            e.CanCheckinAnywhere, e.ArNoWaitPayment, e.CalcToHospitalNoPay, e.DoctorTypeId,
            e.DoctorGroupId, e.DepartmentId, e.ClinicId, e.PrivilegeTypeId, e.StatusPrivilegeId,
            e.EmployeeCode, e.PaymentTypeId, e.StartWorkDate, e.EmploymentStatus, e.ResignDate,
            e.PaymentCondition, e.HospitalAbsorbCcFee, e.CcFeePercent, e.TaxId,
            e.UseHomeTaxAddress, e.TaxAddrNo, e.TaxAddrBuilding, e.TaxAddrSoi, e.TaxAddrRoad,
            e.TaxAddrSubdistrict, e.TaxAddrDistrict, e.TaxAddrProvince, e.TaxAddrPostcode,
            e.PublishChannels, e.ApprovalStatus, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<DoctorCode, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.DisplayNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<DoctorCode, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorCode, object?>>>
        {
            ["code"] = e => e.Code,
            ["displayNameTh"] = e => e.DisplayNameTh,
            ["oldDoctorCode"] = e => e.OldDoctorCode,
            ["departmentCode"] = e => e.Department == null ? null : e.Department.Code,
            ["employmentStatus"] = e => e.EmploymentStatus,
            ["approvalStatus"] = e => e.ApprovalStatus,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorCode> Search(IQueryable<DoctorCode> query, ListRequest r)
    {
        if (r.Filter("doctorId") is { } doctorId && Guid.TryParse(doctorId, out var dId))
            query = query.Where(e => e.DoctorId == dId);

        if (r.Filter("departmentId") is { } departmentId &&
            Guid.TryParse(departmentId, out var deptId))
            query = query.Where(e => e.DepartmentId == deptId);

        if (r.Filter("isDutyDoctor") is { } duty)
            query = query.Where(e => e.IsDutyDoctor == (duty == "true"));

        if (r.Enum<ApprovalStatus>("approvalStatus") is { } parsedApproval)
            query = query.Where(e => e.ApprovalStatus == parsedApproval);

        if (r.Enum<EmploymentStatus>("employmentStatus") is { } parsedEmp)
            query = query.Where(e => e.EmploymentStatus == parsedEmp);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.DisplayNameTh.Contains(q) ||
                (e.OldDoctorCode != null && e.OldDoctorCode.Contains(q)));
        }

        return query;
    }

    public override void Apply(DoctorCode e, DoctorCodeInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.Code = input.Code.Trim();
            e.DoctorId = input.DoctorId;
            e.ApprovalStatus = ApprovalStatus.Draft;
        }

        e.OldDoctorCode = input.OldDoctorCode?.Trim();
        e.IsCentralCode = input.IsCentralCode;
        e.DisplayNameTh = input.DisplayNameTh.Trim();
        e.DisplayNameEn = input.DisplayNameEn?.Trim();
        e.TaxInvoiceNameTh = input.TaxInvoiceNameTh?.Trim();
        e.TaxInvoiceNameEn = input.TaxInvoiceNameEn?.Trim();
        e.BoardStatus = input.BoardStatus?.Trim();
        e.DefaultDfDoctorCode = input.DefaultDfDoctorCode?.Trim();
        e.IsDutyDoctor = input.IsDutyDoctor;
        e.WhtFormType = input.WhtFormType?.Trim();
        e.CanCheckinAnywhere = input.CanCheckinAnywhere;
        e.ArNoWaitPayment = input.ArNoWaitPayment;
        e.CalcToHospitalNoPay = input.CalcToHospitalNoPay;
        e.DoctorTypeId = input.DoctorTypeId;
        e.DoctorGroupId = input.DoctorGroupId;
        e.DepartmentId = input.DepartmentId;
        e.ClinicId = input.ClinicId;
        e.PrivilegeTypeId = input.PrivilegeTypeId;
        e.StatusPrivilegeId = input.StatusPrivilegeId;
        e.EmployeeCode = input.EmployeeCode?.Trim();
        e.PaymentTypeId = input.PaymentTypeId;
        e.StartWorkDate = input.StartWorkDate;
        e.EmploymentStatus = input.EmploymentStatus;

        e.ResignDate = input.EmploymentStatus == EmploymentStatus.Working ? null : input.ResignDate;
        e.PaymentCondition = input.PaymentCondition?.Trim();
        e.HospitalAbsorbCcFee = input.HospitalAbsorbCcFee;
        e.CcFeePercent = input.HospitalAbsorbCcFee ? input.CcFeePercent : null;
        e.TaxId = input.TaxId?.Trim();
        e.UseHomeTaxAddress = input.UseHomeTaxAddress;
        e.TaxAddrNo = input.TaxAddrNo?.Trim();
        e.TaxAddrBuilding = input.TaxAddrBuilding?.Trim();
        e.TaxAddrSoi = input.TaxAddrSoi?.Trim();
        e.TaxAddrRoad = input.TaxAddrRoad?.Trim();
        e.TaxAddrSubdistrict = input.TaxAddrSubdistrict?.Trim();
        e.TaxAddrDistrict = input.TaxAddrDistrict?.Trim();
        e.TaxAddrProvince = input.TaxAddrProvince?.Trim();
        e.TaxAddrPostcode = input.TaxAddrPostcode?.Trim();
        e.PublishChannels = input.PublishChannels;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DoctorCode e, DoctorCodeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "ประวัติแพทย์");
        MasterFieldRules.Code(errors, input.Code, "รหัสแพทย์", 20);
        MasterFieldRules.Required(errors, input.DisplayNameTh, "displayNameTh",
            "ชื่อกลุ่ม/แพทย์ (ภาษาไทย)");

        if (input.EmploymentStatus != EmploymentStatus.Working && input.ResignDate is null)
            errors.Required("resignDate", "แพทย์ที่ไม่ได้ปฏิบัติงานแล้วต้องระบุวันที่ลาออก");

        if (input.ResignDate is { } resign && input.StartWorkDate is { } start && resign < start)
            errors.Add("resignDate", "range", "วันที่ลาออกต้องไม่ก่อนวันที่เริ่มงาน");

        if (input.HospitalAbsorbCcFee && input.CcFeePercent is null)
            errors.Required("ccFeePercent", "โปรดระบุค่าธรรมเนียมบัตรเครดิตที่โรงพยาบาลออกให้");

        if (input.CcFeePercent is < 0 or > 100)
            errors.Add("ccFeePercent", "range", "ค่าธรรมเนียมต้องอยู่ระหว่าง 0 ถึง 100");

        var taxId = input.TaxId?.Trim();
        if (!string.IsNullOrEmpty(taxId) &&
            (taxId.Length != 13 || !taxId.All(char.IsAsciiDigit)))
            errors.Add("taxId", "format", "เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก");

        if (input.WhtFormType is { } wht && wht.Length > 0 && wht is not ("PND3" or "PND53"))
            errors.Add("whtFormType", "invalid", "ประเภทหักภาษี ณ ที่จ่ายต้องเป็น PND3 หรือ PND53");

        foreach (var channel in input.PublishChannels)
        {
            if (channel is not ("WEBSITE" or "HEALTHUP" or "TELECARE" or "INTRANET"))
                errors.Add("publishChannels", "invalid",
                    $"ช่องทางแสดงข้อมูล '{channel}' ไม่ถูกต้อง");
        }

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<DoctorCodeListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.Code),
        new("รหัสแพทย์เก่า", r => r.OldDoctorCode),
        new("ชื่อกลุ่ม/แพทย์", r => r.DisplayNameTh),
        new("แพทย์เวร", r => r.IsDutyDoctor ? "อยู่เวร" : "ไม่อยู่เวร"),
        new("แผนก", r => r.DepartmentCode),
        new("ประเภทแพทย์", r => r.DoctorTypeCode),
        new("สถานะการปฏิบัติงาน", r => EmploymentStatusTh(r.EmploymentStatus)),
        new("สถานะการอนุมัติ", r => DoctorSpec.ApprovalStatusTh(r.ApprovalStatus)),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string EmploymentStatusTh(EmploymentStatus status) => status switch
    {
        EmploymentStatus.Working => "ปฏิบัติงานอยู่",
        EmploymentStatus.Resigned => "ลาออก",
        EmploymentStatus.Suspended => "พักงาน",
        _ => status.ToString(),
    };
}

