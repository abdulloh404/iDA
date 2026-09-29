using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public record TitleListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status);

public record TitleDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TitleInput(
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark);

public sealed class TitleSpec : CrudSpec<MstTitle, TitleListItem, TitleDetail, TitleInput>
{
    public override string Resource => "titles";
    public override string DisplayNameTh => "คำนำหน้าชื่อ";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn"];

    public override Expression<Func<MstTitle, TitleListItem>> ListProjection =>
        e => new TitleListItem(e.Id, e.Code, e.TitleNameTh, e.TitleNameEn, e.Status);

    public override Expression<Func<MstTitle, TitleDetail>> DetailProjection =>
        e => new TitleDetail(e.Id, e.Code, e.TitleNameTh, e.TitleNameEn, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override Expression<Func<MstTitle, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.TitleNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstTitle, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstTitle, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.TitleNameTh,
            ["nameEn"] = e => e.TitleNameEn,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstTitle> Search(IQueryable<MstTitle> query, ListRequest r)
    {
        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.TitleNameTh.Contains(nameTh));

        if (r.Filter("nameEn") is { } nameEn)
            query = query.Where(e => e.TitleNameEn != null && e.TitleNameEn.Contains(nameEn));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.TitleNameTh.Contains(q) ||
                (e.TitleNameEn != null && e.TitleNameEn.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstTitle e, TitleInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.TitleNameTh = input.NameTh.Trim();
        e.TitleNameEn = input.NameEn?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstTitle e, TitleInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสคำนำหน้าชื่อ", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "คำนำหน้าชื่อ (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<TitleListItem>> ExportColumns =>
    [
        new("รหัสคำนำหน้าชื่อ", r => r.Code),
        new("คำนำหน้าชื่อ (ไทย)", r => r.NameTh),
        new("คำนำหน้าชื่อ (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

