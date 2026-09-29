using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutyRates;

public record HolidayRateListItem(
    Guid Id,
    string HolidayName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? SpecialHours,
    HolidayPayMode PayMode,
    decimal? PayMultiplier,
    decimal? BoardHourlyAmount,
    decimal? NonBoardHourlyAmount,
    string? ClinicName,
    RecordStatus Status);

public record HolidayRateDetail(
    Guid Id,
    string HolidayName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? SpecialHours,
    HolidayPayMode PayMode,
    decimal? PayMultiplier,
    decimal? BoardHourlyAmount,
    decimal? NonBoardHourlyAmount,
    Guid? ClinicId,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record HolidayRateInput(
    string HolidayName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? SpecialHours,
    HolidayPayMode PayMode,
    decimal? PayMultiplier,
    decimal? BoardHourlyAmount,
    decimal? NonBoardHourlyAmount,
    Guid? ClinicId,
    RecordStatus Status,
    string? Remark);

public sealed class DutyHolidayRateSpec
    : CrudSpec<DutyHolidayRate, HolidayRateListItem, HolidayRateDetail, HolidayRateInput>
{
    public override string Resource => "holiday-duty-rates";
    public override string DisplayNameTh => "อัตราค่าเวรวันหยุดเทศกาล";
    public override string Module => "duty-rates";
    public override string DefaultSort => "-startDate";

    public override IReadOnlyList<string> FilterKeys => ["payMode", "clinicId", "from", "to"];

    public override Expression<Func<DutyHolidayRate, HolidayRateListItem>> ListProjection =>
        e => new HolidayRateListItem(e.Id, e.HolidayName, e.StartDate, e.EndDate,
            e.SpecialHours, e.PayMode, e.PayMultiplier, e.BoardHourlyAmount,
            e.NonBoardHourlyAmount,
            e.Clinic == null ? null : e.Clinic.NameTh,
            e.Status);

    public override Expression<Func<DutyHolidayRate, HolidayRateDetail>> DetailProjection =>
        e => new HolidayRateDetail(e.Id, e.HolidayName, e.StartDate, e.EndDate, e.SpecialHours,
            e.PayMode, e.PayMultiplier, e.BoardHourlyAmount, e.NonBoardHourlyAmount,
            e.ClinicId, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DutyHolidayRate, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyHolidayRate, object?>>>
        {
            ["holidayName"] = e => e.HolidayName,
            ["startDate"] = e => e.StartDate,
            ["endDate"] = e => e.EndDate,
            ["payMultiplier"] = e => e.PayMultiplier,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DutyHolidayRate> Search(
        IQueryable<DutyHolidayRate> q, ListRequest r)
    {
        if (r.Enum<HolidayPayMode>("payMode") is { } mode)
            q = q.Where(e => e.PayMode == mode);

        if (r.Filter("clinicId") is { } clinicId && Guid.TryParse(clinicId, out var cId))
            q = q.Where(e => e.ClinicId == cId);

        if (r.Filter("from") is { } from && DateOnly.TryParse(from, out var fromDate))
            q = q.Where(e => e.EndDate >= fromDate);

        if (r.Filter("to") is { } to && DateOnly.TryParse(to, out var toDate))
            q = q.Where(e => e.StartDate <= toDate);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.HolidayName.Contains(text));
        }

        return q;
    }

    public override void Apply(DutyHolidayRate e, HolidayRateInput input, bool isCreate)
    {
        e.HolidayName = input.HolidayName.Trim();
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;
        e.SpecialHours = input.SpecialHours;
        e.PayMode = input.PayMode;
        e.ClinicId = input.ClinicId;

        if (input.PayMode == HolidayPayMode.Multiplier)
        {
            e.PayMultiplier = input.PayMultiplier;
            e.BoardHourlyAmount = null;
            e.NonBoardHourlyAmount = null;
        }
        else
        {
            e.PayMultiplier = null;
            e.BoardHourlyAmount = input.BoardHourlyAmount;
            e.NonBoardHourlyAmount = input.NonBoardHourlyAmount;
        }

        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DutyHolidayRate e, HolidayRateInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Required(errors, input.HolidayName, "holidayName", "ชื่อวันหยุด");

        if (input.StartDate == default)
            errors.Required("startDate", "โปรดระบุวันที่เริ่มต้น");
        if (input.EndDate == default)
            errors.Required("endDate", "โปรดระบุวันที่สิ้นสุด");
        else if (input.StartDate != default && input.EndDate < input.StartDate)
            errors.Add("endDate", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มต้น");

        if (input.SpecialHours is < 0)
            errors.Add("specialHours", "range", "ค่าเวรพิเศษต้องไม่ติดลบ");

        if (input.PayMode == HolidayPayMode.Multiplier)
        {
            if (input.PayMultiplier is null)
                errors.Required("payMultiplier", "โปรดระบุอัตราจ่าย (เท่า)");
            else if (input.PayMultiplier <= 0)
                errors.Add("payMultiplier", "range", "อัตราจ่ายต้องมากกว่า 0");
        }
        else if (input.BoardHourlyAmount is null && input.NonBoardHourlyAmount is null)
        {
            errors.Required("boardHourlyAmount",
                "โปรดระบุอัตราจ่ายรายชั่วโมงอย่างน้อยหนึ่งช่อง");
        }

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DutyHolidayRate e,
        HolidayRateInput input, bool isCreate, ValidationFailure errors,
        IRepository<DutyHolidayRate> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (input.StartDate == default || input.EndDate == default) return;

        var clash = repo.Query().Where(o =>
            o.Id != e.Id &&
            o.ClinicId == input.ClinicId &&
            o.StartDate <= input.EndDate &&
            o.EndDate >= input.StartDate);

        if (await exec.AnyAsync(clash, ct))
            errors.Add("startDate", "overlap",
                "มีอัตราวันหยุดของคลินิกเดียวกันที่ช่วงวันที่ทับกันอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<HolidayRateListItem>> ExportColumns =>
    [
        new("ชื่อวันหยุด", r => r.HolidayName),
        new("วันที่เริ่มต้น", r => r.StartDate.ToString("dd/MM/yyyy")),
        new("วันที่สิ้นสุด", r => r.EndDate.ToString("dd/MM/yyyy")),
        new("ค่าเวรพิเศษ (ชั่วโมง)", r => r.SpecialHours, "#,##0.00"),
        new("ประเภทการจ่าย", r => PayModeTh(r.PayMode)),
        new("อัตราจ่าย (เท่า)", r => r.PayMultiplier, "#,##0.00"),
        new("อัตราแพทย์จบ Board (บาท/ชม.)", r => r.BoardHourlyAmount, "#,##0.00"),
        new("อัตราแพทย์ไม่จบ Board (บาท/ชม.)", r => r.NonBoardHourlyAmount, "#,##0.00"),
        new("คลินิก", r => r.ClinicName ?? "ทุกคลินิก"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string PayModeTh(HolidayPayMode mode) =>
        mode == HolidayPayMode.Multiplier ? "อัตราจ่าย (เท่า)" : "อัตราจ่ายรายชั่วโมง";
}

public record HolidayExclusionRow(
    Guid Id, Guid HolidayRateId, string? DoctorCode, string? DoctorName);

public record HolidayExclusionDetail(
    Guid Id, Guid HolidayRateId, Guid DoctorCodeId, string RowVersion);

public record HolidayExclusionInput(Guid HolidayRateId, Guid DoctorCodeId);

public sealed class DutyHolidayExclusionSpec
    : CrudSpec<DutyHolidayExclusion, HolidayExclusionRow, HolidayExclusionDetail,
        HolidayExclusionInput>
{
    public override string Resource => "holiday-duty-exclusions";
    public override string DisplayNameTh => "รายการยกเว้นแพทย์ (วันหยุด)";
    public override string Module => "duty-rates";
    public override string DefaultSort => "doctorCode";

    public override IReadOnlyList<string> FilterKeys => ["holidayRateId"];

    public override Expression<Func<DutyHolidayExclusion, HolidayExclusionRow>> ListProjection =>
        e => new HolidayExclusionRow(e.Id, e.HolidayRateId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh);

    public override Expression<Func<DutyHolidayExclusion, HolidayExclusionDetail>>
        DetailProjection =>
        e => new HolidayExclusionDetail(e.Id, e.HolidayRateId, e.DoctorCodeId,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DutyHolidayExclusion, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyHolidayExclusion, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
        };

    public override IQueryable<DutyHolidayExclusion> Search(
        IQueryable<DutyHolidayExclusion> q, ListRequest r) =>
        r.Filter("holidayRateId") is { } id && Guid.TryParse(id, out var rateId)
            ? q.Where(e => e.HolidayRateId == rateId)
            : q;

    public override void Apply(DutyHolidayExclusion e, HolidayExclusionInput input, bool isCreate)
    {
        if (isCreate) e.HolidayRateId = input.HolidayRateId;
        e.DoctorCodeId = input.DoctorCodeId;
    }

    public override Task ValidateAsync(DutyHolidayExclusion e, HolidayExclusionInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.HolidayRateId, "holidayRateId", "อัตราวันหยุด");
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DutyHolidayExclusion e,
        HolidayExclusionInput input, bool isCreate, ValidationFailure errors,
        IRepository<DutyHolidayExclusion> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.HolidayRateId == input.HolidayRateId &&
            o.DoctorCodeId == input.DoctorCodeId);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("doctorCodeId", "แพทย์ท่านนี้ถูกยกเว้นไว้แล้ว");
    }
}

