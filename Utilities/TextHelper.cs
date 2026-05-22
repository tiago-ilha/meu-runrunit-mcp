using System.Net;
using System.Text.RegularExpressions;
using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Utilities;

internal static partial class TextHelper
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "o", "e", "de", "da", "do", "das", "dos", "em", "no", "na", "nos", "nas", "um", "uma",
        "para", "por", "com", "sem", "ao", "aos", "à", "às", "que", "se", "ou", "como", "mais",
        "the", "and", "or", "to", "of", "in", "on", "at", "for", "is", "are", "was", "be", "this",
        "task", "tarefa", "runrun", "runrunit", "http", "https", "www", "html", "div", "span",
        "br", "p", "nbsp", "null", "true", "false"
    };

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[A-Za-z][A-Za-z0-9_]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var decoded = WebUtility.HtmlDecode(html);
        var withoutTags = HtmlTagRegex().Replace(decoded, " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    public static IReadOnlyList<string> ExtractSearchTerms(string text, int maxTerms)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in IdentifierRegex().Matches(text))
        {
            var term = match.Value;
            if (term.Length < 3 || StopWords.Contains(term))
                continue;

            terms.Add(term);

            if (term.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) && term.Length > "Controller".Length)
                terms.Add(term[..^"Controller".Length]);

            if (terms.Count >= maxTerms)
                break;
        }

        return terms.OrderByDescending(t => t.Length).Take(maxTerms).ToList();
    }

    public static string BuildCombinedSearchText(
        string title,
        string? description,
        IEnumerable<CommentSnippet> comments)
    {
        var parts = new List<string> { title };

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(StripHtml(description));

        foreach (var comment in comments)
        {
            if (!string.IsNullOrWhiteSpace(comment.Text))
                parts.Add(StripHtml(comment.Text));
        }

        return string.Join(Environment.NewLine, parts);
    }
}
