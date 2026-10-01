using System.Linq.Expressions;
using Ida.Domain.Common;

namespace Ida.Application.Common.Crud;

public record MasterListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status);

public record MasterDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record MasterInput(
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark);

public abstract class SimpleMasterSpec<TEntity>
    : CrudSpec<TEntity, MasterListItem, MasterDetail, MasterInput>
    where TEntity : TenantMasterEntity, new()
{
    public override string DefaultSort => "code";

    public virtual string CodeLabelTh => $"รหัส{DisplayNameTh}";

    public virtual int CodeMaxLength => 20;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn"];

    public override Expression<Func<TEntity, MasterListItem>> ListProjection =>
        e => new MasterListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.Status);

    public override Expression<Func<TEntity, MasterDetail>> DetailProjection =>
        e => new MasterDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override Expression<Func<TEntity, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<TEntity, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<TEntity, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["status"] = e => e.Status,
        };

    public override IQueryable<TEntity> Search(IQueryable<TEntity> query, ListRequest r)
    {
        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (r.Filter("nameEn") is { } nameEn)
            query = query.Where(e => e.NameEn != null && e.NameEn.Contains(nameEn));

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

    public override void Apply(TEntity e, MasterInput input, bool isCreate)
    {

        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(TEntity e, MasterInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, CodeLabelTh, CodeMaxLength);

        if (string.IsNullOrWhiteSpace(input.NameTh))
            errors.Required("nameTh", $"โปรดระบุชื่อ{DisplayNameTh} (ภาษาไทย)");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<MasterListItem>> ExportColumns =>
    [
        new(CodeLabelTh, r => r.Code),
        new($"{DisplayNameTh} (ไทย)", r => r.NameTh),
        new($"{DisplayNameTh} (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

public static class MasterFieldRules
{
    public static void Code(ValidationFailure errors, string? value, string labelTh,
        int maxLength = 20, string field = "code")
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Required(field, $"โปรดระบุ{labelTh}");
        else if (value.Trim().Length > maxLength)
            errors.Add(field, "max_length", $"{labelTh}ต้องไม่เกิน {maxLength} ตัวอักษร");
    }

    public static void Required(ValidationFailure errors, string? value, string field,
        string labelTh)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Required(field, $"โปรดระบุ{labelTh}");
    }

    public static void RequiredId(ValidationFailure errors, Guid? value, string field,
        string labelTh)
    {
        if (value is null || value == Guid.Empty) errors.Required(field, $"โปรดเลือก{labelTh}");
    }

    public static void MaxLength(ValidationFailure errors, string? value, string field,
        string labelTh, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
            errors.Add(field, "max_length", $"{labelTh}ต้องไม่เกิน {maxLength} ตัวอักษร");
    }

    public static void Email(ValidationFailure errors, string? value, string field,
        string labelTh, bool required, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) errors.Required(field, $"โปรดระบุ{labelTh}");
            return;
        }

        var text = value.Trim();
        if (text.Length > maxLength)
            errors.Add(field, "max_length", $"{labelTh}ต้องไม่เกิน {maxLength} ตัวอักษร");
        else if (!EmailPattern.IsMatch(text))
            errors.Add(field, "invalid_email", $"รูปแบบ{labelTh}ไม่ถูกต้อง");
    }

    private static readonly System.Text.RegularExpressions.Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string StatusTh(RecordStatus status) =>
        status == RecordStatus.Active ? "ใช้งาน" : "ไม่ใช้งาน";
}

