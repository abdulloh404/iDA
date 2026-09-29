using Ida.Application.Common;

namespace Ida.Application.Features.IngestConfiguration;

public record InterfaceDto(string Code, string Name, string SourceSystem,
    string? EndpointUrl);
public record ScheduleDto(Guid Id, string Name, int IntervalValue, string IntervalUnit,
    DateTime NextRunAt, bool Enabled, int Revision, string[] DatasetCodes,
    string? LastStatus, DateTime? LastStartedAt, DateTime? CancelledAt);
public record IngestConfigurationDto(string HospitalId, InterfaceDto[] Interfaces,
    ScheduleDto[] Schedules, int CancelledScheduleCount, bool WorkerOnline,
    DateTime? WorkerLastSeenAt);
public record CancelledSchedulePageDto(ScheduleDto[] Items, bool HasMore);
public record InterfaceInput(string? EndpointUrl);
public record ScheduleInput(string Name, int IntervalValue, string IntervalUnit,
    bool Enabled, string[] DatasetCodes, int? Revision, DateTimeOffset? FirstRunAt);
public record ScheduleRevisionInput(int Revision);

public interface IIngestConfigurationStore
{
    Task<IngestConfigurationDto> Read(string hospital, CancellationToken ct);
    Task<CancelledSchedulePageDto> ReadCancelledSchedules(string hospital,
        DateTimeOffset? beforeAt, Guid? beforeId, CancellationToken ct);
    Task SaveInterface(string hospital, string code, InterfaceInput input,
        string actor, CancellationToken ct);
    Task<ScheduleDto> SaveSchedule(string hospital, Guid? id, ScheduleInput input,
        string actor, CancellationToken ct);
    Task CancelSchedule(string hospital, Guid id, int revision, string actor,
        CancellationToken ct);
}

public record GetIngestConfigurationQuery : IQuery<IngestConfigurationDto>;
public record GetCancelledIngestSchedulesQuery(DateTimeOffset? BeforeAt,
    Guid? BeforeId) : IQuery<CancelledSchedulePageDto>;
public record SaveIngestInterfaceCommand(string Code, InterfaceInput Input) : ICommand<IngestConfigurationDto>;
public record SaveIngestScheduleCommand(Guid? Id, ScheduleInput Input) : ICommand<ScheduleDto>;
public record CancelIngestScheduleCommand(Guid Id, int Revision) : ICommand<bool>;

public class IngestConfigurationHandlers(IIngestConfigurationStore store,
    ITenantContext tenant, ICurrentUser user) :
    IQueryHandler<GetIngestConfigurationQuery, IngestConfigurationDto>,
    IQueryHandler<GetCancelledIngestSchedulesQuery, CancelledSchedulePageDto>,
    ICommandHandler<SaveIngestInterfaceCommand, IngestConfigurationDto>,
    ICommandHandler<SaveIngestScheduleCommand, ScheduleDto>,
    ICommandHandler<CancelIngestScheduleCommand, bool>
{
    private void RequireAdminRole()
    {
        if (!user.IsInRole("GROUP_ADMIN") && !user.IsInRole("HOSPITAL_ADMIN"))
            throw ApiException.Forbidden(message: "เมนูนี้สำหรับผู้ดูแลระบบส่วนกลางหรือผู้ดูแลระบบโรงพยาบาล");
    }

    public Task<IngestConfigurationDto> Handle(GetIngestConfigurationQuery request,
        CancellationToken ct)
    {
        RequireAdminRole();
        return store.Read(tenant.HospitalId, ct);
    }

    public Task<CancelledSchedulePageDto> Handle(GetCancelledIngestSchedulesQuery request,
        CancellationToken ct)
    {
        RequireAdminRole();
        return store.ReadCancelledSchedules(tenant.HospitalId, request.BeforeAt,
            request.BeforeId, ct);
    }

    public async Task<IngestConfigurationDto> Handle(SaveIngestInterfaceCommand request,
        CancellationToken ct)
    {
        RequireAdminRole();
        await store.SaveInterface(tenant.HospitalId, request.Code, request.Input,
            user.UserName, ct);
        return await store.Read(tenant.HospitalId, ct);
    }

    public Task<ScheduleDto> Handle(SaveIngestScheduleCommand request,
        CancellationToken ct)
    {
        RequireAdminRole();
        return store.SaveSchedule(tenant.HospitalId, request.Id,
            request.Input, user.UserName, ct);
    }

    public async Task<bool> Handle(CancelIngestScheduleCommand request,
        CancellationToken ct)
    {
        RequireAdminRole();
        await store.CancelSchedule(tenant.HospitalId, request.Id, request.Revision,
            user.UserName, ct);
        return true;
    }
}

