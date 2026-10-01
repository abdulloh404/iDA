using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace Ida.Application.Common;

public static partial class RichText
{
    private static readonly HtmlSanitizer Sanitizer = Build();

    private static HtmlSanitizer Build()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3",
                     "ul", "ol", "li", "a", "blockquote", "hr",
                 })
            sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("target");
        sanitizer.AllowedAttributes.Add("rel");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");

        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedClasses.Clear();
        return sanitizer;
    }

    public static string Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html).Trim();

    public static bool IsBlank(string? html) => string.IsNullOrWhiteSpace(ToPlainText(html));

    public static string ToPlainText(string? html) =>
        string.IsNullOrEmpty(html)
            ? string.Empty
            : WebUtility.HtmlDecode(Tags().Replace(html, " ")).Replace(' ', ' ').Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}

