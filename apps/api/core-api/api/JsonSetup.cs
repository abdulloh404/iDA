using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ida.Api;

public class UpperSnakeCaseJsonNamingPolicy : JsonNamingPolicy
{
    public static readonly UpperSnakeCaseJsonNamingPolicy Instance = new();

    public override string ConvertName(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(name[i - 1])) sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }
}

public static class JsonSetup
{
    public static void ConfigureIdaJson(this JsonSerializerOptions options)
    {

        options.Converters.Add(new JsonStringEnumConverter(
            UpperSnakeCaseJsonNamingPolicy.Instance));
    }
}

