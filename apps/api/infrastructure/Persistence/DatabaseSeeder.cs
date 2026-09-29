using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Auth;
using Ida.Domain.Common;
using Ida.Domain.Core;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ida.Infrastructure.Persistence;

public class DatabaseSeeder(
    DatabaseContexts contexts,
    DatabaseRegistry registry,
    IPasswordHasher passwords,
    IConfiguration config,
    ILogger<DatabaseSeeder> log)
{
    private IdaDbContext db => contexts.Core;

    private static readonly (string Code, string NameTh, bool GroupLevel)[] SystemRoles =
    [
        ("GROUP_ADMIN", "ผู้ดูแลระบบส่วนกลาง", true),
        ("HOSPITAL_ADMIN", "ผู้ดูแลระบบโรงพยาบาล", false),
        ("ACCOUNTING", "เจ้าหน้าที่บัญชีแพทย์", false),
        ("VIEWER", "ผู้ใช้งานทั่วไป (ดูอย่างเดียว)", false),

        ("MD_OFFICE", "สำนักผู้อำนวยการแพทย์", false),
        ("DOCTOR_ACCOUNTING", "บัญชีแพทย์ (ผู้อนุมัติ)", false),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedHospitalsAsync(ct);
        var newPermissions = await SeedPermissionsAsync(ct);
        await SeedRolesAsync(newPermissions, ct);
        await SeedAdminAsync(ct);
        await SeedEmailTemplatesAsync(ct);
        await VerifyRowLevelSecurityAsync(ct);
    }

    private async Task SeedHospitalsAsync(CancellationToken ct)
    {
        var endpoints = await registry.ListBranchesAsync(ct);
        var branches = endpoints.Select(CreateHospital).ToList();
        if (branches.Count > 0 && branches.All(branch => !branch.IsHeadOffice))
            branches[0].IsHeadOffice = true;

        var existing = await db.Hospitals.ToListAsync(ct);
        var existingById = existing.ToDictionary(hospital => hospital.Id, StringComparer.Ordinal);
        var missing = new List<Hospital>();
        var changed = false;

        foreach (var branch in branches)
        {
            if (!existingById.TryGetValue(branch.Id, out var stored))
            {
                missing.Add(branch);
                continue;
            }

            if (stored.TenantDbName != branch.TenantDbName)
            {
                stored.TenantDbName = branch.TenantDbName;
                changed = true;
            }
            if (stored.TenantDbHost != branch.TenantDbHost)
            {
                stored.TenantDbHost = branch.TenantDbHost;
                changed = true;
            }
        }

        if (missing.Count == 0 && !changed) return;

        if (missing.Count > 0) db.Hospitals.AddRange(missing);
        await db.SaveChangesAsync(ct);
        if (missing.Count > 0)
            log.LogInformation("Seeded {Count} hospitals: {Ids}",
                missing.Count, string.Join(", ", missing.Select(h => h.Id)));
    }

    private static Hospital CreateHospital(DatabaseEndpoint endpoint)
    {
        var id = endpoint.HospitalId!;
        var hospital = id switch
        {
            "PT1" => new Hospital
            {
                Id = id,
                HospitalNameTh = "โรงพยาบาลพญาไท 1",
                HospitalNameEn = "Phyathai Hospital 1",
                ShortName = "PT1",
                DoctorCodePrefix = "P1",
                IsHeadOffice = true,
            },
            "PTW" => new Hospital
            {
                Id = id,
                HospitalNameTh = "โรงพยาบาลพญาไทบ่อวิน",
                HospitalNameEn = "Phyathai Bowin Hospital",
                ShortName = "PTW",
                DoctorCodePrefix = "PW",
            },
            _ => new Hospital
            {
                Id = id,
                HospitalNameTh = id,
                HospitalNameEn = id,
                ShortName = endpoint.ConnectionKey,
            },
        };

        hospital.TenantDbName = endpoint.DatabaseName;
        hospital.TenantDbHost = endpoint.Host;
        return hospital;
    }

    private async Task<HashSet<string>> SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var known = existing.ToHashSet(StringComparer.Ordinal);
        var added = new HashSet<string>(StringComparer.Ordinal);

        var valid = new HashSet<string>(StringComparer.Ordinal);

        foreach (var resource in CrudRegistry.Resources)
        {
            foreach (var action in CrudResource.Actions)
            {
                var code = resource.Permission(action);
                valid.Add(code);
                if (!known.Add(code)) continue;

                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Resource = resource.Name,
                    Action = action,
                    Module = resource.Module,
                    NameTh = $"{ActionNameTh(action)}{resource.DisplayNameTh}",
                    IsGroupLevel = resource.IsGroupLevel,
                });
                added.Add(code);
            }
        }

        foreach (var (code, nameTh, module, groupLevel) in HandWrittenPermissions)
        {
            valid.Add(code);
            if (!known.Add(code)) continue;
            db.Permissions.Add(new Permission
            {
                Code = code,
                Resource = code.Split('.')[0],
                Action = code.Split('.')[1],
                Module = module,
                NameTh = nameTh,
                IsGroupLevel = groupLevel,
            });
            added.Add(code);
        }

        if (added.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation("Seeded {Count} new permissions", added.Count);
        }

        await PruneOrphanPermissionsAsync(valid, ct);
        return added;
    }

    private async Task PruneOrphanPermissionsAsync(IReadOnlySet<string> valid,
        CancellationToken ct)
    {
        var orphans = await db.Permissions
            .Where(p => !valid.Contains(p.Code))
            .ToListAsync(ct);

        if (orphans.Count == 0) return;

        var total = await db.Permissions.CountAsync(ct);
        if (orphans.Count * 4 > total)
        {
            log.LogWarning(
                "Skipped pruning {Count} of {Total} permissions — that is too large a " +
                "share to be a rename, and looks like CrudRegistry failed to load. " +
                "Nothing was removed.", orphans.Count, total);
            return;
        }

        db.Permissions.RemoveRange(orphans);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Pruned {Count} permissions with no endpoint: {Codes}",
            orphans.Count, string.Join(", ", orphans.Select(o => o.Code)));
    }

    private static readonly (string Code, string NameTh, string Module, bool IsGroupLevel)[]
        HandWrittenPermissions =
    [
        ("hospitals.read", "ดูข้อมูลสาขาโรงพยาบาล", "master-data-general", true),
        ("hospitals.write", "บันทึกข้อมูลสาขาโรงพยาบาล", "master-data-general", true),
        ("hospitals.export", "Export ข้อมูลสาขาโรงพยาบาล", "master-data-general", true),

        ("approvals.read", "ดูคำขอและประวัติคำขอ", "approvals", false),
        ("approvals.approve", "อนุมัติหรือไม่อนุมัติคำขอ", "approvals", false),

        ("duty-schedules.submit", "ส่งตารางเวรให้บัญชี", "duty-schedules", false),
        ("duty-schedules.reject", "ตีกลับตารางเวร", "duty-schedules", false),

        ("doctor-fee-402.approve", "อนุมัติรายการค่าแพทย์ 40(2)", "doctor-fee-402", false),
        ("ingest-config.read", "ดูการตั้งค่านำเข้าข้อมูล", "integration", false),
        ("ingest-config.write", "บันทึกการตั้งค่านำเข้าข้อมูล", "integration", false),
        ("ingest.read", "ดูประวัติและสถานะการนำเข้าข้อมูล", "integration", false),
        ("ingest.raw.read", "ดู raw response ของการนำเข้าข้อมูล", "integration", false),
        ("ingest.trigger", "สั่งนำเข้าข้อมูลจำลอง", "integration", false),
    ];

    private static string ActionNameTh(string action) => action switch
    {
        "read" => "ดูข้อมูล",
        "write" => "บันทึกข้อมูล",
        "delete" => "ลบข้อมูล",
        "export" => "Export ",
        _ => action,
    };

    private async Task SeedRolesAsync(IReadOnlySet<string> newPermissions, CancellationToken ct)
    {
        var permissions = await db.Permissions.ToListAsync(ct);
        var roles = await db.Roles.Include(r => r.Permissions).ToListAsync(ct);

        foreach (var (code, nameTh, groupLevel) in SystemRoles)
        {
            var role = roles.FirstOrDefault(r => r.Code == code);
            if (role is null)
            {
                role = new Role
                {
                    Code = code,
                    NameTh = nameTh,
                    IsSystem = true,
                    IsGroupLevel = groupLevel,
                };
                db.Roles.Add(role);
                roles.Add(role);
            }

            var isNewRole = role.Permissions.Count == 0;
            var candidates = permissions.Where(p =>
                Grants(code, p) && (isNewRole || newPermissions.Contains(p.Code)));

            foreach (var permission in candidates)
            {
                if (role.Permissions.Any(rp => rp.PermissionId == permission.Id)) continue;
                role.Permissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} system roles", SystemRoles.Length);
    }

    private static bool Grants(string roleCode, Permission permission) => roleCode switch
    {
        "GROUP_ADMIN" => true,
        _ when permission.Resource == "ingest-config" => roleCode == "HOSPITAL_ADMIN",
        _ when permission.Resource == "ingest" => roleCode is "HOSPITAL_ADMIN" or "ACCOUNTING",

        _ when permission.Code == "duty-schedules.submit" => roleCode == "MD_OFFICE",
        _ when permission.Code == "duty-schedules.reject" =>
            roleCode is "ACCOUNTING" or "DOCTOR_ACCOUNTING",

        _ when permission.Code == "doctor-fee-402.approve" => roleCode == "DOCTOR_ACCOUNTING",

        "MD_OFFICE" or "DOCTOR_ACCOUNTING" =>
            permission.Resource == "approvals" || permission.Action == "read",

        "HOSPITAL_ADMIN" => !permission.IsGroupLevel || permission.Action == "read",
        "ACCOUNTING" => permission.Module != "admin" &&
                        (!permission.IsGroupLevel || permission.Action == "read") &&
                        permission.Action != "delete",
        "VIEWER" => permission.Action is "read" or "export",
        _ => false,
    };

    private async Task SeedAdminAsync(CancellationToken ct)
    {

        var password = config["Seed:AdminPassword"];
        var username = config["Seed:AdminUsername"] ?? "admin";

        var user = await db.Users
            .Include(u => u.HospitalRoles)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                log.LogWarning(
                    "No admin user created: set Seed:AdminPassword (appsettings.Local.json " +
                    "or the Seed__AdminPassword environment variable) and run the seeder again.");
                return;
            }

            user = new AppUser
            {
                Username = username,
                PasswordHash = passwords.Hash(password),
                DisplayName = "ผู้ดูแลระบบ",
                AuthType = "LOCAL",
                UserType = "ADMIN",
                Status = RecordStatus.Active,
            };
            db.Users.Add(user);
            log.LogInformation("Seeded admin user '{Username}'", username);
        }

        var role = await db.Roles.FirstAsync(r => r.Code == "GROUP_ADMIN", ct);
        var hospitals = await db.Hospitals.OrderBy(h => h.Id).ToListAsync(ct);
        var reachable = user.HospitalRoles.Select(r => r.HospitalId).ToHashSet(StringComparer.Ordinal);
        var hasDefault = user.HospitalRoles.Any(r => r.IsDefault);

        foreach (var hospital in hospitals.Where(h => !reachable.Contains(h.Id)))
        {

            db.UserHospitalRoles.Add(new UserHospitalRole
            {
                UserId = user.Id,
                HospitalId = hospital.Id,
                RoleId = role.Id,

                IsDefault = !hasDefault && hospital.IsHeadOffice,
            });
            if (!hasDefault && hospital.IsHeadOffice) hasDefault = true;
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Admin '{Username}' can reach {Count} hospitals",
            username, hospitals.Count);
    }

    private static readonly (string Code, string Name, string Description, string Subject, string Body)[]
        EmailTemplates =
    [
        ("PAYSLIP", "สลิปเงินเดือน",
            "ส่งให้แพทย์ทุกรอบตามหน้าจอ 99 ใช้ได้: {doctorName} {period} {hospitalName}",
            "สลิปค่าแพทย์ประจำเดือน {period} — {hospitalName}",
            "<p>เรียน {doctorName}</p><p>{hospitalName} ขอนำส่งสลิปค่าแพทย์ประจำเดือน {period} " +
            "ตามไฟล์แนบ ไฟล์ถูกป้องกันด้วยรหัสผ่านที่ท่านตั้งไว้</p><p>ฝ่ายบัญชีแพทย์</p>"),
        ("TAX_CERTIFICATE_406", "หนังสือรับรองภาษีเงินได้ 40(6)",
            "ส่งให้แพทย์ตามรอบในหน้าจอ 99 ใช้ได้: {doctorName} {period} {hospitalName}",
            "หนังสือรับรองภาษีเงินได้ 40(6) ประจำเดือน {period}",
            "<p>เรียน {doctorName}</p><p>{hospitalName} ขอนำส่งหนังสือรับรองภาษีเงินได้ 40(6) " +
            "ประจำเดือน {period} ตามไฟล์แนบ</p><p>ฝ่ายบัญชีแพทย์</p>"),
        ("TAX_CERTIFICATE_50_TAWI", "หนังสือรับรอง 50 ทวิ",
            "ส่งให้แพทย์ปีละครั้งตามหน้าจอ 99 ใช้ได้: {doctorName} {taxYear} {hospitalName}",
            "หนังสือรับรองการหักภาษี ณ ที่จ่าย (50 ทวิ) ปีภาษี {taxYear}",
            "<p>เรียน {doctorName}</p><p>{hospitalName} ขอนำส่งหนังสือรับรองการหักภาษี ณ ที่จ่าย " +
            "(50 ทวิ) ปีภาษี {taxYear} ตามไฟล์แนบ</p><p>ฝ่ายบัญชีแพทย์</p>"),
        ("HIS_DOCTOR_CHANGE", "แจ้งทีม HIS ข้อมูลแพทย์เปลี่ยน",
            "ส่งถึงอีเมลในหน้าจอ 98 เมื่อเพิ่มหรือแก้ไขข้อมูลแพทย์ ใช้ได้: {doctorCode} {doctorName} {changes} {hospitalName}",
            "[iDA] ข้อมูลแพทย์ {doctorCode} มีการเปลี่ยนแปลง",
            "<p>ถึงทีม HIS</p><p>ข้อมูลแพทย์ {doctorCode} {doctorName} ของ {hospitalName} " +
            "มีการเปลี่ยนแปลงดังนี้</p><p>{changes}</p><p>กรุณาปรับข้อมูลในระบบ HIS ให้ตรงกัน</p>"),
        ("APPROVAL_REQUEST", "แจ้งคำขอรออนุมัติ",
            "ส่งถึงผู้อนุมัติเมื่อมีคำขอใหม่ ใช้ได้: {requestNo} {requestType} {requestedBy}",
            "[iDA] คำขอ {requestNo} รอการอนุมัติ",
            "<p>มีคำขอ {requestType} เลขที่ {requestNo} จาก {requestedBy} รอการอนุมัติจากท่าน</p>"),
        ("APPROVAL_RESULT", "แจ้งผลการอนุมัติ",
            "ส่งถึงผู้ขอเมื่อคำขอถูกตัดสิน ใช้ได้: {requestNo} {requestType} {result} {comment}",
            "[iDA] ผลการพิจารณาคำขอ {requestNo}",
            "<p>คำขอ {requestType} เลขที่ {requestNo} ได้รับการพิจารณาแล้ว ผล: {result}</p><p>{comment}</p>"),
        ("EXPIRY_ALERT", "แจ้งเตือนรายการใกล้หมดอายุ",
            "ส่งตามจำนวนวันในหน้าจอ 102 ใช้ได้: {itemType} {itemName} {expiryDate}",
            "[iDA] {itemType} ใกล้หมดอายุ",
            "<p>{itemType} {itemName} จะหมดอายุในวันที่ {expiryDate} กรุณาตรวจสอบและต่ออายุ</p>"),
        ("PASSWORD_EXPIRY", "แจ้งเตือนรหัสผ่านใกล้หมดอายุ",
            "ส่งตามจำนวนวันในหน้าจอ 103 ใช้ได้: {displayName} {expiryDate}",
            "[iDA] รหัสผ่านของท่านจะหมดอายุในวันที่ {expiryDate}",
            "<p>เรียน {displayName}</p><p>รหัสผ่านเข้าระบบ iDA ของท่านจะหมดอายุในวันที่ {expiryDate} " +
            "กรุณาติดต่อผู้ดูแลระบบเพื่อตั้งรหัสผ่านใหม่</p>"),
    ];

    private async Task SeedEmailTemplatesAsync(CancellationToken ct)
    {
        var existing = await db.SysEmailTemplates.Select(t => t.Code).ToListAsync(ct);
        var missing = EmailTemplates.Where(t => !existing.Contains(t.Code)).ToList();
        if (missing.Count == 0) return;

        foreach (var t in missing)
            db.SysEmailTemplates.Add(new SysEmailTemplate
            {
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                Subject = t.Subject,
                Body = t.Body,
            });

        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} email templates", missing.Count);
    }

    private async Task VerifyRowLevelSecurityAsync(CancellationToken ct)
    {
        var branches = await registry.ListBranchesAsync(ct);
        foreach (var branch in branches)
        {
            var branchDb = contexts.ForBranch(branch);
            var unprotected = await branchDb.Database.SqlQueryRaw<string>("""
                SELECT c.relname AS "Value"
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = {0} AND c.relkind = 'r'
                  AND EXISTS (
                      SELECT 1
                      FROM pg_attribute a
                      WHERE a.attrelid = c.oid
                        AND a.attname = 'hospital_id'
                        AND NOT a.attisdropped)
                  AND NOT (
                      c.relrowsecurity
                      AND c.relforcerowsecurity
                      AND EXISTS (
                          SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid))
                ORDER BY 1
                """, branch.SchemaName).ToListAsync(ct);

            if (unprotected.Count > 0)
                throw new InvalidOperationException(
                    $"Row-level security is missing in {branch.ConnectionKey}: " +
                    string.Join(", ", unprotected));
        }
    }
}
