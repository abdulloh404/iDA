using Ida.Domain.Common;

namespace Ida.Application.Common;

public static class ApprovalStatusLabels
{
    public static string ToThai(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Draft => "ร่าง",
        ApprovalStatus.Pending => "รออนุมัติ",
        ApprovalStatus.Approved => "อนุมัติแล้ว",
        ApprovalStatus.Returned => "ส่งกลับให้แก้ไข",
        ApprovalStatus.Rejected => "ไม่อนุมัติ",
        ApprovalStatus.Cancelled => "ยกเลิกคำขอ",
        _ => status.ToString(),
    };
}
