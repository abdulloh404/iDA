using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.Doctors;

public record DoctorLicenseRow(
    Guid Id, Guid DoctorId, string LicenseType, string LicenseNo, string? IssuedPlace,
    DateOnly? IssuedDate, DateOnly? ExpiryDate, bool IsNoExpiry, string? Detail,
    DateOnly? ApprovedDate, RecordStatus Status);

public record DoctorLicenseDetail(
    Guid Id, Guid DoctorId, string LicenseType, string LicenseNo, string? IssuedPlace,
    DateOnly? IssuedDate, DateOnly? ExpiryDate, bool IsNoExpiry, string? Detail,
    DateOnly? ApprovedDate, RecordStatus Status, string RowVersion);

public record DoctorLicenseInput(
    Guid DoctorId, string LicenseType, string LicenseNo, string? IssuedPlace,
    DateOnly? IssuedDate, DateOnly? ExpiryDate, bool IsNoExpiry, string? Detail,
    DateOnly? ApprovedDate, RecordStatus Status);

public sealed class DoctorLicenseSpec
    : DoctorChildSpec<DoctorLicense, DoctorLicenseRow, DoctorLicenseDetail, DoctorLicenseInput>
{
    public override string Resource => "doctor-licenses";
    public override string DisplayNameTh => "ใบประกอบวิชาชีพ";
    public override string DefaultSort => "licenseNo";

    public override Expression<Func<DoctorLicense, DoctorLicenseRow>> ListProjection =>
        e => new DoctorLicenseRow(e.Id, e.DoctorId, e.LicenseType, e.LicenseNo, e.IssuedPlace,
            e.IssuedDate, e.ExpiryDate, e.IsNoExpiry, e.Detail, e.ApprovedDate, e.Status);

    public override Expression<Func<DoctorLicense, DoctorLicenseDetail>> DetailProjection =>
        e => new DoctorLicenseDetail(e.Id, e.DoctorId, e.LicenseType, e.LicenseNo, e.IssuedPlace,
            e.IssuedDate, e.ExpiryDate, e.IsNoExpiry, e.Detail, e.ApprovedDate, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DoctorLicense, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorLicense, object?>>>
        {
            ["licenseNo"] = e => e.LicenseNo,
            ["licenseType"] = e => e.LicenseType,
            ["expiryDate"] = e => e.ExpiryDate,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorLicense> Search(IQueryable<DoctorLicense> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorLicense e, DoctorLicenseInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.LicenseType = input.LicenseType;
        e.LicenseNo = input.LicenseNo.Trim();
        e.IssuedPlace = input.IssuedPlace?.Trim();
        e.IssuedDate = input.IssuedDate;

        e.ExpiryDate = input.IsNoExpiry ? null : input.ExpiryDate;
        e.IsNoExpiry = input.IsNoExpiry;
        e.Detail = input.Detail?.Trim();
        e.ApprovedDate = input.ApprovedDate;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorLicense e, DoctorLicenseInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.LicenseNo, "licenseNo", "เลขที่ใบอนุญาต");

        if (!input.IsNoExpiry && input.ExpiryDate is null)
            errors.Required("expiryDate", "โปรดระบุวันหมดอายุ หรือเลือกไม่มีวันหมดอายุ");

        if (input.ExpiryDate is { } to && input.IssuedDate is { } from && to < from)
            errors.Add("expiryDate", "range", "วันหมดอายุต้องไม่ก่อนวันที่ออกเอกสาร");

        return Task.CompletedTask;
    }
}

public record DoctorContactRow(
    Guid Id, Guid DoctorId, string ContactType, string ContactValue, bool IsPrimary,
    bool IsVerified, RecordStatus Status);

public record DoctorContactDetail(
    Guid Id, Guid DoctorId, string ContactType, string ContactValue, bool IsPrimary,
    bool IsVerified, RecordStatus Status, string RowVersion);

public record DoctorContactInput(
    Guid DoctorId, string ContactType, string ContactValue, bool IsPrimary,
    RecordStatus Status);

public sealed class DoctorContactSpec
    : DoctorChildSpec<DoctorContact, DoctorContactRow, DoctorContactDetail, DoctorContactInput>
{
    public override string Resource => "doctor-contacts";
    public override string DisplayNameTh => "ช่องทางติดต่อ";
    public override string DefaultSort => "contactType";

    public override Expression<Func<DoctorContact, DoctorContactRow>> ListProjection =>
        e => new DoctorContactRow(e.Id, e.DoctorId, e.ContactType, e.ContactValue, e.IsPrimary,
            e.IsVerified, e.Status);

    public override Expression<Func<DoctorContact, DoctorContactDetail>> DetailProjection =>
        e => new DoctorContactDetail(e.Id, e.DoctorId, e.ContactType, e.ContactValue,
            e.IsPrimary, e.IsVerified, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DoctorContact, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorContact, object?>>>
        {
            ["contactType"] = e => e.ContactType,
            ["contactValue"] = e => e.ContactValue,
            ["isPrimary"] = e => e.IsPrimary,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorContact> Search(IQueryable<DoctorContact> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorContact e, DoctorContactInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.ContactType = input.ContactType;
        e.ContactValue = input.ContactValue.Trim();
        e.IsPrimary = input.IsPrimary;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorContact e, DoctorContactInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.ContactValue, "contactValue", "ข้อมูลติดต่อ");

        var value = input.ContactValue?.Trim() ?? string.Empty;
        if (input.ContactType is "EMAIL" or "EMAIL_ALT" && value.Length > 0 && !value.Contains('@'))
            errors.Add("contactValue", "format", "รูปแบบอีเมลไม่ถูกต้อง");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorContact e, DoctorContactInput input,
        bool isCreate, ValidationFailure errors, IRepository<DoctorContact> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var value = input.ContactValue?.Trim();
        if (string.IsNullOrEmpty(value)) return;

        var type = input.ContactType;
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.ContactType == type && o.ContactValue == value);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("contactValue", "ข้อมูลติดต่อนี้ถูกใช้กับแพทย์รายอื่นแล้ว");
    }
}

public record DoctorAddressRow(
    Guid Id, Guid DoctorId, string AddressType, string? AddrNo, string? Road,
    string? Subdistrict, string? District, string? Province, string? Postcode,
    RecordStatus Status);

public record DoctorAddressDetail(
    Guid Id, Guid DoctorId, string AddressType, string? AddrNo, string? Building, string? Soi,
    string? Road, string? Subdistrict, string? District, string? Province, string? Postcode,
    string? Country, bool SameAsHome, RecordStatus Status, string RowVersion);

public record DoctorAddressInput(
    Guid DoctorId, string AddressType, string? AddrNo, string? Building, string? Soi,
    string? Road, string? Subdistrict, string? District, string? Province, string? Postcode,
    string? Country, bool SameAsHome, RecordStatus Status);

public sealed class DoctorAddressSpec
    : DoctorChildSpec<DoctorAddress, DoctorAddressRow, DoctorAddressDetail, DoctorAddressInput>
{
    public override string Resource => "doctor-addresses";
    public override string DisplayNameTh => "ที่อยู่แพทย์";
    public override string DefaultSort => "addressType";

    public override Expression<Func<DoctorAddress, DoctorAddressRow>> ListProjection =>
        e => new DoctorAddressRow(e.Id, e.DoctorId, e.AddressType, e.AddrNo, e.Road,
            e.Subdistrict, e.District, e.Province, e.Postcode, e.Status);

    public override Expression<Func<DoctorAddress, DoctorAddressDetail>> DetailProjection =>
        e => new DoctorAddressDetail(e.Id, e.DoctorId, e.AddressType, e.AddrNo, e.Building,
            e.Soi, e.Road, e.Subdistrict, e.District, e.Province, e.Postcode, e.Country,
            e.SameAsHome, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DoctorAddress, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorAddress, object?>>>
        {
            ["addressType"] = e => e.AddressType,
            ["province"] = e => e.Province,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorAddress> Search(IQueryable<DoctorAddress> q, ListRequest r) =>
        DoctorIdFilter(r) is { } id ? q.Where(e => e.DoctorId == id) : q;

    public override void Apply(DoctorAddress e, DoctorAddressInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.AddressType = input.AddressType;
        }

        e.AddrNo = input.AddrNo?.Trim();
        e.Building = input.Building?.Trim();
        e.Soi = input.Soi?.Trim();
        e.Road = input.Road?.Trim();
        e.Subdistrict = input.Subdistrict?.Trim();
        e.District = input.District?.Trim();
        e.Province = input.Province?.Trim();
        e.Postcode = input.Postcode?.Trim();
        e.Country = input.Country?.Trim();
        e.SameAsHome = input.SameAsHome;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(DoctorAddress e, DoctorAddressInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");

        var postcode = input.Postcode?.Trim();
        if (!string.IsNullOrEmpty(postcode) &&
            (postcode.Length != 5 || !postcode.All(char.IsAsciiDigit)))
            errors.Add("postcode", "format", "รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorAddress e, DoctorAddressInput input,
        bool isCreate, ValidationFailure errors, IRepository<DoctorAddress> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var doctorId = input.DoctorId;
        var type = input.AddressType;
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.DoctorId == doctorId && o.AddressType == type);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("addressType", "ที่อยู่ชนิดนี้ของแพทย์รายนี้มีอยู่แล้ว");
    }
}

