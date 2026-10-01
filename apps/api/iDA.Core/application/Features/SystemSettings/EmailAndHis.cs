using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.SystemSettings;

public record EmailTemplateListItem(
    Guid Id,
    string Code,
    string Name,
    string Subject,
    DateTimeOffset UpdatedAt);

public record EmailTemplateDetail(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Subject,
    string Body,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public record EmailTemplateInput(string? Subject, string? Body);

public sealed class EmailTemplateSpec
    : CrudSpec<SysEmailTemplate, EmailTemplateListItem, EmailTemplateDetail, EmailTemplateInput>
{
    public const int SubjectMax = 500;
    public const int BodyMax = 4000;

    public override string Resource => "email-templates";
    public override string DisplayNameTh => "รูปแบบอีเมล";
    public override string Module => "system-settings";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "name";
    public override bool CanCreate => false;

    public override Expression<Func<SysEmailTemplate, EmailTemplateListItem>> ListProjection =>
        e => new EmailTemplateListItem(e.Id, e.Code, e.Name, e.Subject, e.UpdatedAt);

    public override Expression<Func<SysEmailTemplate, EmailTemplateDetail>> DetailProjection =>
        e => new EmailTemplateDetail(e.Id, e.Code, e.Name, e.Description, e.Subject, e.Body,
            e.UpdatedAt, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysEmailTemplate, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysEmailTemplate, object?>>>
        {
            ["name"] = e => e.Name,
            ["subject"] = e => e.Subject,
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override IQueryable<SysEmailTemplate> Search(IQueryable<SysEmailTemplate> q,
        ListRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.Name.Contains(text) || e.Subject.Contains(text));
        }

        return q;
    }

    public override void Apply(SysEmailTemplate e, EmailTemplateInput input, bool isCreate)
    {
        e.Subject = input.Subject?.Trim() ?? string.Empty;
        e.Body = RichText.Sanitize(input.Body);
    }

    public override Task ValidateAsync(SysEmailTemplate e, EmailTemplateInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Required(errors, input.Subject, "subject", "หัวข้ออีเมล");
        MasterFieldRules.MaxLength(errors, input.Subject, "subject", "หัวข้ออีเมล", SubjectMax);

        if (RichText.IsBlank(input.Body))
            errors.Required("body", "โปรดระบุเนื้อหาอีเมล");

        else if (RichText.Sanitize(input.Body).Length > BodyMax)
            errors.Add("body", "max_length",
                $"เนื้อหาอีเมลต้องไม่เกิน {BodyMax:N0} ตัวอักษร (นับรวมการจัดรูปแบบ)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<EmailTemplateListItem>> ExportColumns =>
    [
        new("Template", r => r.Name),
        new("หัวข้ออีเมล", r => r.Subject),
        new("วันที่แก้ไขล่าสุด", r => r.UpdatedAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm")),
    ];
}

