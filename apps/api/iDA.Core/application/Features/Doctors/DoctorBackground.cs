using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.Doctors;

public record DoctorEducationRow(
    Guid Id, Guid DoctorId, short? StartYear, short? EndYear, string DegreeName,
    string? InstituteName, string? Country, RecordStatus Status);

public record DoctorEducationDetail(
    Guid Id, Guid DoctorId, short? StartYear, short? EndYear, string DegreeName,
    string? InstituteName, string? Country, string? Remark, RecordStatus Status,
    string RowVersion);

public record DoctorEducationInput(
    Guid DoctorId, short? StartYear, short? EndYear, string DegreeName,
    string? InstituteName, string? Country, string? Remark, RecordStatus Status);

public sealed class DoctorEducationSpec
    : DoctorChildSpec<DoctorEducation, DoctorEducationRow, DoctorEducationDetail,
        DoctorEducationInput>
{
    public override string Resource => "doctor-educations";
    public override string DisplayNameTh => "ประวัติการศึกษา";
    public override string DefaultSort => "-endYear";

    public override Expression<Func<DoctorEducation, DoctorEducationRow>> ListProjection =>
        e => new DoctorEducationRow(e.Id, e.DoctorId, e.StartYear, e.EndYear, e.DegreeName,
            e.InstituteName, e.Country, e.Status);

    public override Expression<Func<DoctorEducation, DoctorEducationDetail>> DetailProjection =>
        e => new DoctorEducationDetail(e.Id, e.DoctorId, e.StartYear, e.EndYear, e.DegreeName,
            e.InstituteName, e.Country, e.Remark, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorEducation, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorEducation, object?>>>
        {
            ["endYear"] = e => e.EndYear,
            ["degreeName"] = e => e.DegreeName,
            ["instituteName"] = e => e.InstituteName,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorEducation> Search(IQueryable<DoctorEducation> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorEducation e, DoctorEducationInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.StartYear = input.StartYear;
        e.EndYear = input.EndYear;
        e.DegreeName = input.DegreeName.Trim();
        e.InstituteName = input.InstituteName?.Trim();
        e.Country = input.Country?.Trim();
        e.Remark = input.Remark?.Trim();
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorEducation e, DoctorEducationInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.DegreeName, "degreeName", "วุฒิการศึกษา");
        DoctorYears.Check(errors, input.StartYear, input.EndYear);
        return Task.CompletedTask;
    }
}

internal static class DoctorYears
{
    public static void Check(ValidationFailure errors, short? start, short? end,
        string startField = "startYear", string endField = "endYear")
    {

        foreach (var (value, field) in new[] { (start, startField), (end, endField) })
        {
            if (value is { } year && year is < 1900 or > 2100)
                errors.Add(field, "range", "ปีต้องเป็น ค.ศ. ระหว่าง 1900 ถึง 2100");
        }

        if (start is { } s && end is { } t && t < s)
            errors.Add(endField, "range", "ปีที่จบต้องไม่ก่อนปีที่เริ่ม");
    }
}

public record DoctorTrainingRow(
    Guid Id, Guid DoctorId, string TrainingName, string? InstituteName, string? BudgetSource,
    string? BondContractNo, DateOnly? StartDate, DateOnly? EndDate, RecordStatus Status);

public record DoctorTrainingDetail(
    Guid Id, Guid DoctorId, string TrainingName, string? InstituteName, string? BudgetSource,
    string? BondContractNo, DateOnly? StartDate, DateOnly? EndDate, RecordStatus Status,
    string RowVersion);

public record DoctorTrainingInput(
    Guid DoctorId, string TrainingName, string? InstituteName, string? BudgetSource,
    string? BondContractNo, DateOnly? StartDate, DateOnly? EndDate, RecordStatus Status);

public sealed class DoctorTrainingSpec
    : DoctorChildSpec<DoctorTraining, DoctorTrainingRow, DoctorTrainingDetail, DoctorTrainingInput>
{
    public override string Resource => "doctor-trainings";
    public override string DisplayNameTh => "ประวัติการฝึกอบรม";
    public override string DefaultSort => "-startDate";

    public override Expression<Func<DoctorTraining, DoctorTrainingRow>> ListProjection =>
        e => new DoctorTrainingRow(e.Id, e.DoctorId, e.TrainingName, e.InstituteName,
            e.BudgetSource, e.BondContractNo, e.StartDate, e.EndDate, e.Status);

    public override Expression<Func<DoctorTraining, DoctorTrainingDetail>> DetailProjection =>
        e => new DoctorTrainingDetail(e.Id, e.DoctorId, e.TrainingName, e.InstituteName,
            e.BudgetSource, e.BondContractNo, e.StartDate, e.EndDate, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorTraining, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorTraining, object?>>>
        {
            ["startDate"] = e => e.StartDate,
            ["trainingName"] = e => e.TrainingName,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorTraining> Search(IQueryable<DoctorTraining> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorTraining e, DoctorTrainingInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.TrainingName = input.TrainingName.Trim();
        e.InstituteName = input.InstituteName?.Trim();
        e.BudgetSource = input.BudgetSource?.Trim();
        e.BondContractNo = input.BondContractNo?.Trim();
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorTraining e, DoctorTrainingInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.TrainingName, "trainingName", "หลักสูตรที่อบรม");

        if (input.StartDate is { } from && input.EndDate is { } to && to < from)
            errors.Add("endDate", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม");

        return Task.CompletedTask;
    }
}

public record DoctorWorkHistoryRow(
    Guid Id, Guid DoctorId, short? StartYear, short? EndYear, string? PositionName,
    string? Workplace, RecordStatus Status);

public record DoctorWorkHistoryDetail(
    Guid Id, Guid DoctorId, short? StartYear, short? EndYear, string? PositionName,
    string? Workplace, string? Remark, RecordStatus Status, string RowVersion);

public record DoctorWorkHistoryInput(
    Guid DoctorId, short? StartYear, short? EndYear, string? PositionName,
    string? Workplace, string? Remark, RecordStatus Status);

public sealed class DoctorWorkHistorySpec
    : DoctorChildSpec<DoctorWorkHistory, DoctorWorkHistoryRow, DoctorWorkHistoryDetail,
        DoctorWorkHistoryInput>
{
    public override string Resource => "doctor-work-histories";
    public override string DisplayNameTh => "ประวัติการทำงาน";
    public override string DefaultSort => "-endYear";

    public override Expression<Func<DoctorWorkHistory, DoctorWorkHistoryRow>> ListProjection =>
        e => new DoctorWorkHistoryRow(e.Id, e.DoctorId, e.StartYear, e.EndYear, e.PositionName,
            e.Workplace, e.Status);

    public override Expression<Func<DoctorWorkHistory, DoctorWorkHistoryDetail>> DetailProjection =>
        e => new DoctorWorkHistoryDetail(e.Id, e.DoctorId, e.StartYear, e.EndYear,
            e.PositionName, e.Workplace, e.Remark, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorWorkHistory, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorWorkHistory, object?>>>
        {
            ["endYear"] = e => e.EndYear,
            ["workplace"] = e => e.Workplace,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorWorkHistory> Search(
        IQueryable<DoctorWorkHistory> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorWorkHistory e, DoctorWorkHistoryInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.StartYear = input.StartYear;
        e.EndYear = input.EndYear;
        e.PositionName = input.PositionName?.Trim();
        e.Workplace = input.Workplace?.Trim();
        e.Remark = input.Remark?.Trim();
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorWorkHistory e, DoctorWorkHistoryInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.Workplace, "workplace", "สถานที่ทำงาน");
        DoctorYears.Check(errors, input.StartYear, input.EndYear);
        return Task.CompletedTask;
    }
}

