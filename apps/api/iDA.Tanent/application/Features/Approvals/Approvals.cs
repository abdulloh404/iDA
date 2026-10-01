using Ida.Application.Common;
using Ida.Domain.Common;

namespace Ida.Application.Features.Approvals;

public enum ApprovalScope
{

    Mine,

    Pending,

    History,
}

public record ApprovalRequestListItem(
    Guid Id,
    string RequestNo,
    string RequestType,
    string RequestTypeNameTh,

    string Summary,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset UpdatedAt,
    ApprovalStatus Status,

    string? CurrentStepRole,
    string? CurrentStepRoleNameTh);

public record ApprovalStepDto(
    short StepSeq,
    string ApproverRole,
    string ApproverRoleNameTh,
    string? ApproverUser,
    string? Action,
    DateTimeOffset? ActionAt,
    string? Comment);

public record ApprovalRequestDetail(
    Guid Id,
    string RequestNo,
    string RequestType,
    string RequestTypeNameTh,
    string Summary,
    string TargetTable,
    Guid? TargetId,
    Guid? DoctorId,
    string? DoctorName,

    string Payload,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ClosedAt,
    ApprovalStatus Status,
    IReadOnlyList<ApprovalStepDto> Steps,

    bool CanDecide);

public record ListApprovalRequestsQuery(ApprovalScope Scope, ListRequest Request)
    : IQuery<PagedResult<ApprovalRequestListItem>>;

public record GetApprovalRequestQuery(Guid Id) : IQuery<ApprovalRequestDetail>;

public record DecideApprovalCommand(Guid Id, string Action, string? Comment)
    : ICommand<ApprovalRequestDetail>;

public static class ApprovalLabels
{
    public static readonly IReadOnlyDictionary<string, string> RequestTypes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DOCTOR_PROFILE"] = "ข้อมูลประวัติแพทย์",
            ["DOCTOR_CODE"] = "ข้อมูลรหัสแพทย์",
            ["BANK_ACCOUNT"] = "บัญชีธนาคารของแพทย์",
            ["SPECIALTY"] = "ความเชี่ยวชาญของแพทย์",
            ["CONTRACT"] = "สัญญาแพทย์",
            ["WELFARE"] = "สวัสดิการแพทย์",
        };

    public static readonly IReadOnlyDictionary<string, string> ApproverRoles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MD_OFFICE"] = "สำนักผู้อำนวยการแพทย์",
            ["DOCTOR_ACCOUNTING"] = "บัญชีแพทย์",
        };

    public static string RequestTypeTh(string code) =>
        RequestTypes.TryGetValue(code, out var name) ? name : code;

    public static string ApproverRoleTh(string code) =>
        ApproverRoles.TryGetValue(code, out var name) ? name : code;

    public const string OverrideRole = "GROUP_ADMIN";
}

