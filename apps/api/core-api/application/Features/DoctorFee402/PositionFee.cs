using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

public record PositionFeeListItem(
    Guid Id,
    string? DoctorCode,
    string? DoctorName,
    string? ClinicName,
    string PositionName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyAmount,
    RecordStatus Status);

public record PositionFeeDetail(
    Guid Id,
    Guid DoctorCodeId,
    Guid? ClinicId,
    string PositionName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyAmount,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record PositionFeeInput(
    Guid DoctorCodeId,
    Guid? ClinicId,
    string PositionName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal MonthlyAmount,
    RecordStatus Status,
    string? Remark);

public sealed class PositionFeeSpec
    : CrudSpec<DfPositionFee, PositionFeeListItem, PositionFeeDetail, PositionFeeInput>
{
    public override string Resource => "position-fees";
    public override string DisplayNameTh => "ค่าบริหาร / ตำแหน่ง";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "-startDate";

    public override IReadOnlyList<string> FilterKeys =>
        ["doctorCodeId", "startDate", "endDate", "clinicId"];

    public override Expression<Func<DfPositionFee, PositionFeeListItem>> ListProjection =>
        e => new PositionFeeListItem(e.Id,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Clinic == null ? null : e.Clinic.NameTh,
            e.PositionName, e.StartDate, e.EndDate, e.MonthlyAmount, e.Status);

    public override Expression<Func<DfPositionFee, PositionFeeDetail>> DetailProjection =>
        e => new PositionFeeDetail(e.Id, e.DoctorCodeId, e.ClinicId, e.PositionName,
            e.StartDate, e.EndDate, e.MonthlyAmount, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DfPositionFee, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfPositionFee, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["positionName"] = e => e.PositionName,
            ["startDate"] = e => e.StartDate,
            ["endDate"] = e => e.EndDate,
            ["monthlyAmount"] = e => e.MonthlyAmount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfPositionFee> Search(IQueryable<DfPositionFee> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        if (r.Filter("clinicId") is { } cid && Guid.TryParse(cid, out var clinicId))
            q = q.Where(e => e.ClinicId == clinicId);

        if (r.Filter("startDate") is { } from && DateOnly.TryParse(from, out var fromDate))
            q = q.Where(e => e.EndDate >= fromDate);

        if (r.Filter("endDate") is { } to && DateOnly.TryParse(to, out var toDate))
            q = q.Where(e => e.StartDate <= toDate);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.PositionName.Contains(text) ||
                (e.DoctorCode != null && (e.DoctorCode.Code.Contains(text) ||
                    e.DoctorCode.DisplayNameTh.Contains(text))));
        }

        return q;
    }

    public override void Apply(DfPositionFee e, PositionFeeInput input, bool isCreate)
    {
        e.DoctorCodeId = input.DoctorCodeId;
        e.ClinicId = input.ClinicId;
        e.PositionName = input.PositionName?.Trim() ?? string.Empty;
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;
        e.MonthlyAmount = input.MonthlyAmount;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfPositionFee e, PositionFeeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {

        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        MasterFieldRules.Required(errors, input.PositionName, "positionName", "ตำแหน่ง");

        if (input.StartDate == default)
            errors.Required("startDate", "โปรดระบุวันที่เริ่มต้น");
        if (input.EndDate == default)
            errors.Required("endDate", "โปรดระบุวันที่สิ้นสุด");
        else if (input.StartDate != default && input.StartDate > input.EndDate)
            errors.Add("endDate", "invalid_range", "โปรดระบุวันที่เริ่มต้น น้อยกว่าวันที่สิ้นสุด");

        if (input.MonthlyAmount <= 0)
            errors.Required("monthlyAmount", "โปรดระบุรายได้ (บาท/เดือน)");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfPositionFee e, PositionFeeInput input,
        bool isCreate, ValidationFailure errors, IRepository<DfPositionFee> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        if (input.StartDate == default || input.EndDate == default) return;

        var position = input.PositionName?.Trim() ?? string.Empty;
        var clash = repo.Query().Where(o =>
            o.Id != e.Id &&
            o.DoctorCodeId == input.DoctorCodeId &&
            o.PositionName == position &&
            o.Status == RecordStatus.Active &&
            o.StartDate <= input.EndDate &&
            o.EndDate >= input.StartDate);

        if (await exec.AnyAsync(clash, ct))
            errors.Add("startDate", "overlap",
                "แพทย์ท่านนี้มีค่าบริหารตำแหน่งเดียวกันในช่วงวันที่ที่ทับกันอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<PositionFeeListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("คลินิก", r => r.ClinicName),
        new("ตำแหน่ง", r => r.PositionName),
        new("วันที่เริ่มต้น", r => r.StartDate),
        new("วันที่สิ้นสุด", r => r.EndDate),
        new("รายได้ (บาท/เดือน)", r => r.MonthlyAmount),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

