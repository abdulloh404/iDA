using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

internal static class FeeApproval
{

    public static string? WhyLocked(IDecidableFee fee) =>
        fee.CycleClosed
            ? "รายการนี้ปิดรอบชำระแล้ว แก้ไขหรือลบไม่ได้"
            : fee.ApprovalStatus switch
            {
                ApprovalStatus.Approved => "รายการนี้อนุมัติแล้ว แก้ไขหรือลบไม่ได้",
                ApprovalStatus.Rejected => "รายการนี้ไม่ได้รับอนุมัติแล้ว ให้สร้างรายการใหม่แทน",
                ApprovalStatus.Cancelled => "รายการนี้ถูกยกเลิกคำขอแล้ว",
                _ => null,
            };

    public static void ResubmitIfReturned(IDecidableFee fee)
    {
        if (fee.ApprovalStatus != ApprovalStatus.Returned) return;
        fee.ApprovalStatus = ApprovalStatus.Pending;
        fee.DecisionComment = null;
        fee.DecidedBy = null;
        fee.DecidedAt = null;
    }

    public static string StatusTh(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Draft => "ร่าง",
        ApprovalStatus.Pending => "รออนุมัติ",
        ApprovalStatus.Approved => "อนุมัติ",
        ApprovalStatus.Returned => "ส่งกลับ",
        ApprovalStatus.Rejected => "ไม่อนุมัติ",
        ApprovalStatus.Cancelled => "ยกเลิกคำขอ",
        _ => status.ToString(),
    };

    public static string CycleTh(bool closed) => closed ? "ปิดรอบแล้ว" : "ยังไม่ปิดรอบ";

    public static void ValidatePeriod(ValidationFailure errors, int year, int month)
    {
        if (month is < 1 or > 12)
            errors.Required("periodMonth", "โปรดระบุค่าแพทย์รอบเดือน");

        if (year is < 2000 or > 2100)
            errors.Add("periodYear", "invalid", "โปรดระบุปีเป็น ค.ศ.");
    }
}

