using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.Accounting;

public record BankListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? SwiftCode,
    RecordStatus Status);

public record BankDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? SwiftCode,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record BankInput(
    string Code,
    string NameTh,
    string? NameEn,
    string? SwiftCode,
    RecordStatus Status,
    string? Remark);

public sealed class BankSpec : CrudSpec<MstBank, BankListItem, BankDetail, BankInput>
{
    public override string Resource => "banks";
    public override string DisplayNameTh => "ธนาคาร";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn"];

    public override Expression<Func<MstBank, BankListItem>> ListProjection =>
        e => new BankListItem(e.Id, e.Code, e.BankNameTh, e.BankNameEn, e.SwiftCode, e.Status);

    public override Expression<Func<MstBank, BankDetail>> DetailProjection =>
        e => new BankDetail(e.Id, e.Code, e.BankNameTh, e.BankNameEn, e.SwiftCode, e.Status,
            e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstBank, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.BankNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstBank, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstBank, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.BankNameTh,
            ["nameEn"] = e => e.BankNameEn,
            ["swiftCode"] = e => e.SwiftCode,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstBank> Search(IQueryable<MstBank> query, ListRequest r)
    {
        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.BankNameTh.Contains(nameTh));

        if (r.Filter("nameEn") is { } nameEn)
            query = query.Where(e => e.BankNameEn != null && e.BankNameEn.Contains(nameEn));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.BankNameTh.Contains(q) ||
                (e.BankNameEn != null && e.BankNameEn.Contains(q)) ||
                (e.SwiftCode != null && e.SwiftCode.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstBank e, BankInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.BankNameTh = input.NameTh.Trim();
        e.BankNameEn = input.NameEn?.Trim();

        e.SwiftCode = input.SwiftCode?.Trim().ToUpperInvariant();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstBank e, BankInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสธนาคาร", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อธนาคาร (ภาษาไทย)");

        var swift = input.SwiftCode?.Trim();
        if (!string.IsNullOrEmpty(swift) && swift.Length is not (8 or 11))
            errors.Add("swiftCode", "format", "SWIFT Code ต้องมี 8 หรือ 11 ตัวอักษร");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<BankListItem>> ExportColumns =>
    [
        new("รหัสธนาคาร", r => r.Code),
        new("ชื่อธนาคาร (ไทย)", r => r.NameTh),
        new("ชื่อธนาคาร (อังกฤษ)", r => r.NameEn),
        new("SWIFT Code", r => r.SwiftCode),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

