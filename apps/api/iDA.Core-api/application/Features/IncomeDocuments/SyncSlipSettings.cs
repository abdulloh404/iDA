using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.IncomeDocuments;

public record SyncSlipSettingsCommand : ICommand<SyncSlipSettingsResult>;

public record SyncSlipSettingsResult(int Created, int SkippedNoEmail);

public class SyncSlipSettingsHandler(
    IRepository<DocSlipSetting> settings,
    IRepository<DoctorCode> codes,
    ICoreDirectory core,
    IQueryExecutor exec,
    IUnitOfWork uow)
    : ICommandHandler<SyncSlipSettingsCommand, SyncSlipSettingsResult>
{
    public async Task<SyncSlipSettingsResult> Handle(SyncSlipSettingsCommand command,
        CancellationToken ct)
    {
        var configured = settings.Query().Select(s => s.DoctorCodeId);

        var missing = await exec.ToListAsync(codes.Query()
            .Where(c => c.Status == RecordStatus.Active && !configured.Contains(c.Id))
            .Select(c => new { c.Id, c.DoctorId }), ct);

        if (missing.Count == 0) return new SyncSlipSettingsResult(0, 0);

        var doctorIds = missing.Select(m => m.DoctorId).Distinct().ToList();
        var emails = await core.DoctorEmailsAsync(doctorIds, ct);

        var created = 0;
        var skipped = 0;
        foreach (var code in missing)
        {
            var mine = emails.Where(e => e.DoctorId == code.DoctorId).ToList();
            var primary = mine.Where(e => e.ContactType == "EMAIL")
                .OrderByDescending(e => e.IsPrimary)
                .Select(e => SlipSettingSpec.NormaliseEmail(e.ContactValue))
                .FirstOrDefault(e => e is not null);

            if (primary is null)
            {
                skipped++;
                continue;
            }

            var backup = mine.Where(e => e.ContactType == "EMAIL_ALT")
                .Select(e => SlipSettingSpec.NormaliseEmail(e.ContactValue))
                .FirstOrDefault(e => e is not null && e != primary);

            settings.Add(new DocSlipSetting
            {
                DoctorCodeId = code.Id,
                Email = primary,
                BackupEmail = backup,
            });
            created++;
        }

        if (created > 0) await uow.SaveChangesAsync(ct);
        return new SyncSlipSettingsResult(created, skipped);
    }
}
