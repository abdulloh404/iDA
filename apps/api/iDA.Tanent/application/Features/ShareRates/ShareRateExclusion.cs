using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.ShareRates;

public record ShareExclusionRow(
    Guid Id, Guid RateId, string? DoctorCode, string? DoctorName, string? DoctorGroupCode,
    string? DoctorGroupName);

public record ShareExclusionDetail(
    Guid Id, Guid RateId, Guid? DoctorCodeId, Guid? DoctorGroupId, string RowVersion);

public record ShareExclusionInput(Guid RateId, Guid? DoctorCodeId, Guid? DoctorGroupId);

public sealed class ShareRateExclusionSpec
    : CrudSpec<ShareRateExclusion, ShareExclusionRow, ShareExclusionDetail,
        ShareExclusionInput>
{
    public override string Resource => "share-rate-exclusions";
    public override string DisplayNameTh => "รายการยกเว้นแพทย์";
    public override string Module => "share-rates";
    public override string DefaultSort => "doctorCode";

    public override IReadOnlyList<string> FilterKeys => ["rateId"];

    public override Expression<Func<ShareRateExclusion, ShareExclusionRow>> ListProjection =>
        e => new ShareExclusionRow(e.Id, e.RateId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.DoctorGroup == null ? null : e.DoctorGroup.Code,
            e.DoctorGroup == null ? null : e.DoctorGroup.NameTh);

    public override Expression<Func<ShareRateExclusion, ShareExclusionDetail>>
        DetailProjection =>
        e => new ShareExclusionDetail(e.Id, e.RateId, e.DoctorCodeId, e.DoctorGroupId,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<ShareRateExclusion, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<ShareRateExclusion, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["doctorGroupCode"] = e => e.DoctorGroup == null ? null : e.DoctorGroup.Code,
        };

    public override IQueryable<ShareRateExclusion> Search(
        IQueryable<ShareRateExclusion> q, ListRequest r) =>
        r.Filter("rateId") is { } id && Guid.TryParse(id, out var rateId)
            ? q.Where(e => e.RateId == rateId)
            : q;

    public override void Apply(ShareRateExclusion e, ShareExclusionInput input, bool isCreate)
    {
        if (isCreate) e.RateId = input.RateId;

        e.DoctorCodeId = input.DoctorCodeId;
        e.DoctorGroupId = input.DoctorGroupId;
    }

    public override Task ValidateAsync(ShareRateExclusion e, ShareExclusionInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.RateId, "rateId", "อัตราส่วนแบ่ง");

        if (input.DoctorCodeId is null && input.DoctorGroupId is null)
            errors.Required("doctorCodeId", "โปรดเลือกแพทย์หรือกลุ่มแพทย์ที่ต้องการยกเว้น");
        else if (input.DoctorCodeId is not null && input.DoctorGroupId is not null)
            errors.Add("doctorCodeId", "conflict",
                "เลือกได้อย่างเดียวระหว่างแพทย์รายคนกับกลุ่มแพทย์");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(ShareRateExclusion e,
        ShareExclusionInput input, bool isCreate, ValidationFailure errors,
        IRepository<ShareRateExclusion> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var rateId = input.RateId;
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.RateId == rateId &&
            o.DoctorCodeId == input.DoctorCodeId && o.DoctorGroupId == input.DoctorGroupId);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("doctorCodeId", "รายการยกเว้นนี้มีอยู่แล้วในอัตรานี้");
    }
}

public sealed class PatientRightSpec : SimpleMasterSpec<MstPatientRight>
{
    public override string Resource => "patient-rights";
    public override string DisplayNameTh => "สิทธิ์คนไข้";
    public override string Module => "share-rates";
}

