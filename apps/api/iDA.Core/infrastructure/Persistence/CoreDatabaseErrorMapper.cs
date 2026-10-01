using Ida.Application.Common;

namespace Ida.Infrastructure.Persistence;

public sealed class CoreDatabaseErrorMapper : IDatabaseErrorMapper
{
    private static readonly Dictionary<string, (string Field, string Message)> Known =
        new(StringComparer.Ordinal)
        {
            ["uq_doctor_national_id_hash"] =
                ("nationalId", "เลขบัตรประชาชนนี้มีประวัติแพทย์อยู่แล้ว"),
            ["uq_doctor_global_code"] =
                ("doctorGlobalCode", "รหัสแพทย์กลางนี้ถูกใช้งานแล้ว"),
            ["uq_doctor_contact_value"] =
                ("contactValue", "ข้อมูลติดต่อนี้ถูกใช้กับแพทย์รายอื่นแล้ว"),
        };

    public ApiException ToApiException(string? constraintName)
    {
        if (constraintName is not null && Known.TryGetValue(constraintName, out var known))
            return ApiException.DuplicateCode(known.Field, known.Message);

        return new DefaultDatabaseErrorMapper().ToApiException(constraintName);
    }
}
