using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.Doctors;

public record WelfarePlanListItem(
    Guid Id, string Code, string NameTh, string? NameEn, WelfareScope WelfareScope,
    decimal AnnualLimit, RecordStatus Status);

public record WelfarePlanDetail(
    Guid Id, string Code, string NameTh, string? NameEn, WelfareScope WelfareScope,
    decimal AnnualLimit, RecordStatus Status, string? Remark, string RowVersion);

public record WelfarePlanInput(
    string Code, string NameTh, string? NameEn, WelfareScope WelfareScope, decimal AnnualLimit,
    RecordStatus Status, string? Remark);

public sealed class WelfarePlanSpec
    : CrudSpec<MstWelfarePlan, WelfarePlanListItem, WelfarePlanDetail, WelfarePlanInput>
{
    public override string Resource => "welfare-plans";
    public override string DisplayNameTh => "แผนสวัสดิการแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "welfareScope"];

    public override Expression<Func<MstWelfarePlan, WelfarePlanListItem>> ListProjection =>
        e => new WelfarePlanListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.WelfareScope,
            e.AnnualLimit, e.Status);

    public override Expression<Func<MstWelfarePlan, WelfarePlanDetail>> DetailProjection =>
        e => new WelfarePlanDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.WelfareScope,
            e.AnnualLimit, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstWelfarePlan, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstWelfarePlan, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstWelfarePlan, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["welfareScope"] = e => e.WelfareScope,
            ["annualLimit"] = e => e.AnnualLimit,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstWelfarePlan> Search(IQueryable<MstWelfarePlan> q, ListRequest r)
    {
        if (r.Enum<WelfareScope>("welfareScope") is { } parsed)
            q = q.Where(e => e.WelfareScope == parsed);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.Code.Contains(text) || e.NameTh.Contains(text));
        }

        return q;
    }

    public override void Apply(MstWelfarePlan e, WelfarePlanInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.WelfareScope = input.WelfareScope;

        e.AnnualLimit = input.WelfareScope == WelfareScope.None ? 0m : input.AnnualLimit;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstWelfarePlan e, WelfarePlanInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสแผนสวัสดิการ", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อแผนสวัสดิการ");

        if (input.AnnualLimit < 0)
            errors.Add("annualLimit", "range", "วงเงินต่อปีต้องไม่ติดลบ");

        if (input.WelfareScope != WelfareScope.None && input.AnnualLimit <= 0)
            errors.Add("annualLimit", "range", "แผนที่มีสวัสดิการต้องระบุวงเงินมากกว่าศูนย์");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<WelfarePlanListItem>> ExportColumns =>
    [
        new("รหัสแผน", r => r.Code),
        new("ชื่อแผนสวัสดิการ", r => r.NameTh),
        new("ขอบเขต", r => WelfareScopeTh(r.WelfareScope)),
        new("วงเงินต่อปี (บาท)", r => r.AnnualLimit, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string WelfareScopeTh(WelfareScope scope) => scope switch
    {
        WelfareScope.None => "ไม่มีสวัสดิการ",
        WelfareScope.DoctorOnly => "สำหรับแพทย์เท่านั้น",
        WelfareScope.DoctorAndFamily => "สำหรับแพทย์และครอบครัว",
        _ => scope.ToString(),
    };
}

public record DoctorWelfareListItem(
    Guid Id, Guid DoctorId, string? DoctorName, string? DoctorGlobalCode, string? PlanName,
    WelfareScope WelfareScope, short WelfareYear, decimal AnnualLimit, decimal UsedAmount,
    decimal? RemainingAmount, RecordStatus Status);

public record DoctorWelfareDetail(
    Guid Id, Guid DoctorId, Guid WelfarePlanId, short WelfareYear, decimal AnnualLimit,
    decimal UsedAmount, decimal? RemainingAmount, string? DocumentUrl, RecordStatus Status,
    string? Remark, string RowVersion);

public record DoctorWelfareInput(
    Guid DoctorId, Guid WelfarePlanId, short WelfareYear, decimal AnnualLimit,
    string? DocumentUrl, RecordStatus Status, string? Remark);

public sealed class DoctorWelfareSpec
    : CrudSpec<DoctorWelfare, DoctorWelfareListItem, DoctorWelfareDetail, DoctorWelfareInput>
{
    public override string Resource => "doctor-welfares";
    public override string DisplayNameTh => "สวัสดิการแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "-welfareYear";

    public override IReadOnlyList<string> FilterKeys => ["doctorId", "welfareYear", "welfareScope"];

    public override Expression<Func<DoctorWelfare, DoctorWelfareListItem>> ListProjection =>
        e => new DoctorWelfareListItem(e.Id, e.DoctorId,
            null, null,
            e.WelfarePlan == null ? null : e.WelfarePlan.NameTh,
            e.WelfarePlan == null ? WelfareScope.None : e.WelfarePlan.WelfareScope,
            e.WelfareYear, e.AnnualLimit, e.UsedAmount, e.RemainingAmount, e.Status);

    public override async Task<IReadOnlyList<DoctorWelfareListItem>> EnrichListAsync(
        IReadOnlyList<DoctorWelfareListItem> items, ICrudRelatedData related, CancellationToken ct)
    {
        var doctorIds = items.Select(e => e.DoctorId).Distinct().ToArray();
        var doctors = await related.Core.DoctorsAsync(doctorIds, ct);
        var byId = doctors.ToDictionary(e => e.Id);
        return items.Select(e => byId.TryGetValue(e.DoctorId, out var doctor)
            ? e with { DoctorName = doctor.Name, DoctorGlobalCode = doctor.DoctorGlobalCode }
            : e).ToList();
    }

    public override Expression<Func<DoctorWelfare, DoctorWelfareDetail>> DetailProjection =>
        e => new DoctorWelfareDetail(e.Id, e.DoctorId, e.WelfarePlanId, e.WelfareYear,
            e.AnnualLimit, e.UsedAmount, e.RemainingAmount, e.DocumentUrl, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorWelfare, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorWelfare, object?>>>
        {
            ["welfareYear"] = e => e.WelfareYear,
            ["doctorGlobalCode"] = e => e.DoctorId,
            ["annualLimit"] = e => e.AnnualLimit,
            ["remainingAmount"] = e => e.RemainingAmount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorWelfare> Search(IQueryable<DoctorWelfare> q, ListRequest r)
    {
        if (r.Filter("doctorId") is { } id && Guid.TryParse(id, out var doctorId))
            q = q.Where(e => e.DoctorId == doctorId);

        if (r.Filter("welfareYear") is { } year && short.TryParse(year, out var welfareYear))
            q = q.Where(e => e.WelfareYear == welfareYear);

        if (r.Enum<WelfareScope>("welfareScope") is { } parsed)
            q = q.Where(e => e.WelfarePlan != null && e.WelfarePlan.WelfareScope == parsed);

        return q;
    }

    public override async Task<IQueryable<DoctorWelfare>> PrepareListQueryAsync(
        IQueryable<DoctorWelfare> query, ListRequest request, ICrudRelatedData related,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Q)) return query;
        var text = request.Q.Trim();
        var doctorIds = await related.Core.FindDoctorIdsAsync(new CoreDoctorSearch(text, Name: true, GlobalCode: true), ct);
        return query.Where(e => doctorIds.Contains(e.DoctorId));
    }

    public override async Task<IQueryable<DoctorWelfare>> ApplySortAsync(
        IQueryable<DoctorWelfare> query, ListRequest request, ICrudRelatedData related,
        CancellationToken ct)
    {
        var (key, descending) = request.ParseSort(DefaultSort);
        if (key != "doctorGlobalCode")
            return await base.ApplySortAsync(query, request, related, ct);

        var candidateIds = await related.ToListAsync(query.Select(e => e.DoctorId).Distinct(), ct);
        var orderedIds = await related.Core.OrderDoctorIdsAsync(candidateIds.ToArray(), descending, ct);
        var ranks = orderedIds.ToArray();
        return query.OrderBy(e => ranks.Contains(e.DoctorId)
            ? Array.IndexOf(ranks, e.DoctorId)
            : descending ? int.MinValue : int.MaxValue).ThenBy(e => e.Id);
    }

    public override void Apply(DoctorWelfare e, DoctorWelfareInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.WelfareYear = input.WelfareYear;
        }

        e.WelfarePlanId = input.WelfarePlanId;
        e.AnnualLimit = input.AnnualLimit;
        e.DocumentUrl = input.DocumentUrl?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DoctorWelfare e, DoctorWelfareInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.RequiredId(errors, input.WelfarePlanId, "welfarePlanId", "แผนสวัสดิการ");

        if (input.WelfareYear is < 2000 or > 2100)
            errors.Add("welfareYear", "range", "ปีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100");

        if (input.AnnualLimit < 0)
            errors.Add("annualLimit", "range", "วงเงินการรักษาต้องไม่ติดลบ");

        if (!isCreate && input.AnnualLimit < e.UsedAmount)
            errors.Add("annualLimit", "range",
                $"วงเงินต้องไม่น้อยกว่ายอดที่ใช้ไปแล้ว ({e.UsedAmount:#,##0.00} บาท)");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorWelfare e, DoctorWelfareInput input,
        bool isCreate, ValidationFailure errors, IRepository<DoctorWelfare> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var doctorId = input.DoctorId;
        var year = input.WelfareYear;

        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.DoctorId == doctorId && o.WelfareYear == year);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("welfareYear", $"แพทย์รายนี้มีวงเงินสวัสดิการของปี {year} อยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<DoctorWelfareListItem>> ExportColumns =>
    [
        new("รหัสแพทย์กลาง", r => r.DoctorGlobalCode),
        new("แพทย์", r => r.DoctorName),
        new("แผนสวัสดิการ", r => r.PlanName),
        new("ขอบเขต", r => WelfarePlanSpec.WelfareScopeTh(r.WelfareScope)),
        new("ปี (ค.ศ.)", r => (int)r.WelfareYear),
        new("วงเงินการรักษา (บาท)", r => r.AnnualLimit, "#,##0.00"),
        new("ใช้ไปแล้ว (บาท)", r => r.UsedAmount, "#,##0.00"),
        new("คงเหลือ (บาท)", r => r.RemainingAmount, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}
