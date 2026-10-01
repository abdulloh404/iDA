using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.General;

public record DoctorGroupListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? DoctorTypeCode,
    string? DoctorTypeNameTh,
    RecordStatus Status);

public record DoctorGroupDetail(
    Guid Id,
    string Code,
    Guid? DoctorTypeId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record DoctorGroupInput(
    string Code,
    Guid? DoctorTypeId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark);

public sealed class DoctorGroupSpec
    : CrudSpec<MstDoctorGroup, DoctorGroupListItem, DoctorGroupDetail, DoctorGroupInput>
{
    public override string Resource => "doctor-groups";
    public override string DisplayNameTh => "กลุ่มแพทย์";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "doctorTypeId"];

    public override Expression<Func<MstDoctorGroup, DoctorGroupListItem>> ListProjection =>
        e => new DoctorGroupListItem(e.Id, e.Code, e.NameTh, e.NameEn,
            e.DoctorType == null ? null : e.DoctorType.Code,
            e.DoctorType == null ? null : e.DoctorType.NameTh,
            e.Status);

    public override Expression<Func<MstDoctorGroup, DoctorGroupDetail>> DetailProjection =>
        e => new DoctorGroupDetail(e.Id, e.Code, e.DoctorTypeId, e.NameTh, e.NameEn, e.Status,
            e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstDoctorGroup, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstDoctorGroup, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstDoctorGroup, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["doctorTypeCode"] = e => e.DoctorType == null ? null : e.DoctorType.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstDoctorGroup> Search(IQueryable<MstDoctorGroup> query,
        ListRequest r)
    {

        if (r.Filter("doctorTypeId") is { } typeId && Guid.TryParse(typeId, out var id))
            query = query.Where(e => e.DoctorTypeId == id);

        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.NameTh.Contains(q) ||
                (e.NameEn != null && e.NameEn.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstDoctorGroup e, DoctorGroupInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.DoctorTypeId = input.DoctorTypeId;
        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstDoctorGroup e, DoctorGroupInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสกลุ่มแพทย์", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh",
            "รายละเอียดกลุ่มแพทย์ (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<DoctorGroupListItem>> ExportColumns =>
    [
        new("รหัสประเภทแพทย์", r => r.DoctorTypeCode),
        new("ประเภทแพทย์", r => r.DoctorTypeNameTh),
        new("รหัสกลุ่มแพทย์", r => r.Code),
        new("รายละเอียดกลุ่มแพทย์ (ไทย)", r => r.NameTh),
        new("รายละเอียดกลุ่มแพทย์ (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

