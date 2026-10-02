using System.Text;
using System.Text.RegularExpressions;

namespace Ida.Start;

internal static class RootEnvironment
{
    public static string ApiDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Ida.Start.csproj"))) return directory.FullName;
        throw new InvalidOperationException("Cannot locate apps/api/Ida.Start.csproj. Run the launcher from the source checkout.");
    }

    public static IReadOnlyDictionary<string, string> Read(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"Root .env was not found at {path}.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var lineNumber = 0;
        foreach (var raw in File.ReadLines(path))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
            var separator = line.IndexOf('=');
            if (separator < 1) throw InvalidLine(lineNumber);
            var key = line[..separator].Trim();
            if (!Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_]*$")) throw InvalidLine(lineNumber);
            if (!values.TryAdd(key, ParseValue(line[(separator + 1)..].Trim(), lineNumber)))
                throw new InvalidOperationException($"Duplicate variable {key} in root .env at line {lineNumber}.");
        }
        return values;
    }

    private static string ParseValue(string value, int lineNumber)
    {
        if (value.Length == 0) return value;
        if (value[0] is not ('\'' or '"'))
        {
            for (var index = 0; index < value.Length; index++)
                if (value[index] == '#' && (index == 0 || char.IsWhiteSpace(value[index - 1]))) return value[..index].TrimEnd();
            return value;
        }
        var quote = value[0];
        var result = new StringBuilder();
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (character == quote)
            {
                var remainder = value[(index + 1)..].TrimStart();
                if (remainder.Length > 0 && !remainder.StartsWith('#')) throw InvalidLine(lineNumber);
                return result.ToString();
            }
            if (quote == '"' && character == '\\' && index + 1 < value.Length)
            {
                var next = value[index + 1];
                if (next is '"' or '\\' or 'n' or 'r' or 't')
                {
                    result.Append(next switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => next });
                    index++;
                    continue;
                }
            }
            result.Append(character);
        }
        throw InvalidLine(lineNumber);
    }

    private static InvalidOperationException InvalidLine(int lineNumber) => new($"Invalid root .env syntax at line {lineNumber}.");
}
