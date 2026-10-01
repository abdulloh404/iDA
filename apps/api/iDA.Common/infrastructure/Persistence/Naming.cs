using System.Text;
using Npgsql;

namespace Ida.Infrastructure.Persistence;

public static class Naming
{

    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {

                var previousIsLower = i > 0 && !char.IsUpper(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (i > 0 && (previousIsLower || nextIsLower)) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsDigit(c))
            {

                if (i > 0 && !char.IsDigit(name[i - 1])) sb.Append('_');
                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}

public static class ColumnTypes
{

    private static readonly Dictionary<string, string> Exact = new(StringComparer.Ordinal)
    {
        ["TaxId"] = "char(13)",
        ["NationalIdLast4"] = "char(4)",
        ["AccountNoLast4"] = "char(4)",
        ["NationalIdHash"] = "char(64)",
        ["AccountNoHash"] = "char(64)",
        ["HospitalId"] = "varchar(20)",
        ["Code"] = "varchar(20)",
        ["Postcode"] = "varchar(10)",
        ["TaxAddrPostcode"] = "varchar(10)",
        ["BranchNo"] = "varchar(10)",
        ["Hn"] = "varchar(30)",
        ["PatientHn"] = "varchar(30)",
        ["RequestId"] = "varchar(64)",
        ["EntityId"] = "varchar(64)",
        ["TenantDbName"] = "varchar(63)",

        ["Payload"] = "jsonb",
        ["OldValue"] = "jsonb",
        ["NewValue"] = "jsonb",

        ["PublishChannels"] = "varchar(20)[]",
    };

    private static readonly HashSet<string> Unbounded = new(StringComparer.Ordinal)
    {
        "Remark", "Detail", "Description", "Conclusion", "PaymentCondition",
        "ErrorMessage", "ChangeReason", "Comment",
    };

    public static string For(string propertyName)
    {
        if (Exact.TryGetValue(propertyName, out var exact)) return exact;
        if (Unbounded.Contains(propertyName)) return "text";

        if (propertyName.EndsWith("By", StringComparison.Ordinal)) return "varchar(100)";
        if (propertyName.EndsWith("Url", StringComparison.Ordinal)) return "varchar(500)";
        if (propertyName.EndsWith("Hash", StringComparison.Ordinal)) return "char(64)";
        if (propertyName.EndsWith("Code", StringComparison.Ordinal)) return "varchar(50)";
        if (propertyName.EndsWith("Prefix", StringComparison.Ordinal)) return "varchar(20)";
        return "varchar(200)";
    }
}

public class UpperSnakeNameTranslator : INpgsqlNameTranslator
{
    public string TranslateTypeName(string clrName) => Naming.ToSnakeCase(clrName);

    public string TranslateMemberName(string clrName) =>
        Naming.ToSnakeCase(clrName).ToUpperInvariant();
}
