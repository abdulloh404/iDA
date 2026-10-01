using System.Text.RegularExpressions;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public static class DatabaseSql
{
    public static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    public static string Rewrite(string sql, string schema) => Rewrite(sql, schema, schema);

    public static string Rewrite(string sql, NpgsqlConnection connection)
    {
        var searchPath = new NpgsqlConnectionStringBuilder(connection.ConnectionString).SearchPath?.Split(',', StringSplitOptions.TrimEntries);
        return searchPath switch
        {
            { Length: 1 } => Rewrite(sql, searchPath[0], searchPath[0]),
            { Length: 2 } => Rewrite(sql, searchPath[1], searchPath[0]),
            _ => sql
        };
    }

    public static string Rewrite(string sql, string coreSchema, string buSchema)
    {
        return Regex.Replace(sql, "(?<![a-zA-Z0-9_])(?<quote>\"?)(?<schema>core|bu)\\k<quote>\\.", match =>
        {
            var schema = match.Groups["schema"].Value == "core" ? coreSchema : buSchema;
            return match.Groups["quote"].Value.Length == 0 ? schema + "." : Quote(schema) + ".";
        });
    }
}
