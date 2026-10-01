using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

internal static class HospitalQuery
{
    public static ApiException NotFound() =>
        ApiException.NotFound("hospitals_not_found", "ไม่พบข้อมูลสาขาโรงพยาบาลที่ระบุ");

    public static readonly Expression<Func<Hospital, HospitalListItem>> ToListItem =
        e => new HospitalListItem(e.Id, e.HospitalNameTh, e.HospitalNameEn, e.ShortName,
            e.BranchNo, e.DoctorCodePrefix, e.IsHeadOffice, e.Status);

    public static readonly Expression<Func<Hospital, HospitalDetail>> ToDetail =
        e => new HospitalDetail(e.Id, e.HospitalNameTh, e.HospitalNameEn, e.ShortName,
            e.BranchNo, e.DoctorCodePrefix, e.GlPostCode, e.SetOfBooks, e.EkgTreatmentPrefix,
            e.TaxId, e.TaxAddrNo, e.TaxAddrBuilding, e.TaxAddrSoi, e.TaxAddrRoad,
            e.TaxAddrSubdistrict, e.TaxAddrDistrict, e.TaxAddrProvince, e.TaxAddrPostcode,
            e.TaxAddrCountry, e.ContactEmail, e.ContactPhone, e.IsHeadOffice, e.Status,
            e.Remark, e.RowVersion.ToString());

    public static IQueryable<Hospital> Search(IQueryable<Hospital> query, ListRequest r)
    {
        if (r.Filter("id") is { } id)
            query = query.Where(e => e.Id.Contains(id));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.HospitalNameTh.Contains(nameTh));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Id.Contains(q) ||
                e.HospitalNameTh.Contains(q) ||
                (e.HospitalNameEn != null && e.HospitalNameEn.Contains(q)) ||
                (e.ShortName != null && e.ShortName.Contains(q)));
        }

        return query;
    }

    public static IQueryable<Hospital> Sort(IQueryable<Hospital> query, ListRequest r)
    {
        var (key, descending) = r.ParseSort("id");

        Expression<Func<Hospital, object?>> selector = key switch
        {
            "id" => e => e.Id,
            "nameTh" => e => e.HospitalNameTh,
            "nameEn" => e => e.HospitalNameEn,
            "shortName" => e => e.ShortName,
            "branchNo" => e => e.BranchNo,
            "status" => e => e.Status,
            _ => throw ApiException.BadRequest("unknown_sort",
                $"ไม่รองรับการเรียงลำดับด้วยคอลัมน์ '{key}'",
                new { allowed = new[] { "id", "nameTh", "nameEn", "shortName", "branchNo", "status" } }),
        };

        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    public static void Apply(Hospital e, HospitalInput input)
    {
        e.HospitalNameTh = input.NameTh.Trim();
        e.HospitalNameEn = input.NameEn?.Trim();
        e.ShortName = input.ShortName?.Trim();
        e.BranchNo = input.BranchNo?.Trim();
        e.DoctorCodePrefix = input.DoctorCodePrefix?.Trim().ToUpperInvariant();
        e.GlPostCode = input.GlPostCode?.Trim();
        e.SetOfBooks = input.SetOfBooks?.Trim();
        e.EkgTreatmentPrefix = input.EkgTreatmentPrefix?.Trim();

        e.TaxId = input.TaxId?.Trim();
        e.TaxAddrNo = input.TaxAddrNo?.Trim();
        e.TaxAddrBuilding = input.TaxAddrBuilding?.Trim();
        e.TaxAddrSoi = input.TaxAddrSoi?.Trim();
        e.TaxAddrRoad = input.TaxAddrRoad?.Trim();
        e.TaxAddrSubdistrict = input.TaxAddrSubdistrict?.Trim();
        e.TaxAddrDistrict = input.TaxAddrDistrict?.Trim();
        e.TaxAddrProvince = input.TaxAddrProvince?.Trim();
        e.TaxAddrPostcode = input.TaxAddrPostcode?.Trim();
        e.TaxAddrCountry = input.TaxAddrCountry?.Trim();

        e.ContactEmail = input.ContactEmail?.Trim();
        e.ContactPhone = input.ContactPhone?.Trim();
        e.IsHeadOffice = input.IsHeadOffice;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public static void Validate(HospitalInput input, bool isCreate, ValidationFailure errors)
    {
        if (isCreate)
            MasterFieldRules.Code(errors, input.Id, "รหัสโรงพยาบาล", 20, field: "id");

        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อสาขาโรงพยาบาล (ภาษาไทย)");

        var taxId = input.TaxId?.Trim();
        if (!string.IsNullOrEmpty(taxId) &&
            (taxId.Length != 13 || !taxId.All(char.IsAsciiDigit)))
            errors.Add("taxId", "format", "เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก");

        var postcode = input.TaxAddrPostcode?.Trim();
        if (!string.IsNullOrEmpty(postcode) &&
            (postcode.Length != 5 || !postcode.All(char.IsAsciiDigit)))
            errors.Add("taxAddrPostcode", "format", "รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก");

        var email = input.ContactEmail?.Trim();
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
            errors.Add("contactEmail", "format", "รูปแบบอีเมลไม่ถูกต้อง");
    }

    public static async Task<HospitalDetail> LoadAsync(IRepository<Hospital> repo,
        IQueryExecutor exec, string id, CancellationToken ct) =>
        await exec.FirstOrDefaultAsync(repo.Query().Where(e => e.Id == id).Select(ToDetail), ct)
        ?? throw NotFound();

    public static readonly IReadOnlyList<ExcelColumn<HospitalListItem>> ExportColumns =
    [
        new("รหัสโรงพยาบาล", r => r.Id),
        new("ชื่อสาขาโรงพยาบาล (ไทย)", r => r.NameTh),
        new("ชื่อสาขาโรงพยาบาล (อังกฤษ)", r => r.NameEn),
        new("ชื่อย่อ", r => r.ShortName),
        new("เลขสาขา", r => r.BranchNo),
        new("คำนำหน้ารหัสแพทย์", r => r.DoctorCodePrefix),
        new("สำนักงานใหญ่", r => r.IsHeadOffice ? "ใช่" : "ไม่ใช่"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

