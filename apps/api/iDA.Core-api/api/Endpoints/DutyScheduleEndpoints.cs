using Ida.Api.Auth;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutySchedules;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutyScheduleEndpoints
{
    public static void MapDutyScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<DutySchedule, DutyScheduleListItem, DutyScheduleDetail,
            DutyScheduleInput>(Resource("duty-schedules"));

        app.MapCrud<DutyShift, DutyShiftRow, DutyShiftDetail,
            DutyShiftInput>(Resource("duty-shifts"));

        app.MapCrud<DutyShiftDoctor, DutyShiftDoctorRow, DutyShiftDoctorDetail,
            DutyShiftDoctorInput>(Resource("duty-shift-doctors"));

        var group = app.MapGroup("/api/duty-schedules").WithTags("ตารางเวรและการลงชื่อเข้าเวร");

        group.MapPost("/generate", async (GenerateDutyScheduleCommand command, ISender mediator,
                CancellationToken ct) => Results.Ok(await mediator.Send(command, ct)))
            .WithName("duty_schedules_generate")
            .WithDescription("สร้างตารางเวรของเดือน โดยดึงเวรจากอัตราค่าเวร (kind = DUTY) " +
                "หรืออัตราประกันรายได้ (kind = GUARANTEE_*) ที่ใช้งานอยู่")
            .Produces<DutyScheduleDetail>()
            .RequirePermission("duty-schedules.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteDutyScheduleCommand(id), ct);
                return Results.NoContent();
            })
            .WithName("duty_schedules_delete_cascade")
            .WithDescription("ลบตารางเวรพร้อมเวรและการลงเวลาทั้งหมดในใบนั้น")
            .RequirePermission("duty-schedules.delete");

        group.MapPost("/{id:guid}/submit", async (Guid id, ISender mediator,
                CancellationToken ct) =>
                Results.Ok(await mediator.Send(new SubmitDutyScheduleCommand(id), ct)))
            .WithName("duty_schedules_submit")
            .WithDescription("ส่งตารางเวรให้ฝ่ายบัญชีคำนวณรายเดือน — ต้องลงเวลาครบทุกเวรก่อน")
            .Produces<DutyScheduleDetail>()
            .RequirePermission("duty-schedules.submit");

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectScheduleRequest body,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new RejectDutyScheduleCommand(id, body.Reason), ct)))
            .WithName("duty_schedules_reject")
            .WithDescription("ตีกลับตารางเวรให้สำนักแพทย์แก้ — ถ้าคำนวณรายเดือนแล้วต้องถอนก่อน")
            .Produces<DutyScheduleDetail>()
            .RequirePermission("duty-schedules.reject");
    }

    public record RejectScheduleRequest(string? Reason);

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'.");
}

