using System.Text.RegularExpressions;
using MeuRunrunItMCP.Abstractions;
using MeuRunrunItMCP.Models;
using MeuRunrunItMCP.Options;
using MeuRunrunItMCP.Utilities;
using Microsoft.Extensions.Options;

namespace MeuRunrunItMCP.Services;

public sealed partial class CodeIndexService(IOptions<CodeAnalysisOptions> options) : ICodeIndexService
{
    private static readonly string[] PriorityFolders =
    [
        "Controllers", "Areas", "Views", "Models", "App_Start",
        "Services", "Repositories", "src", "app", "lib", "pages", "components"
    ];

    private static readonly string[] IgnoredDirectoryNames =
    [
        "bin", "obj", "packages", ".git", ".vs", ".idea", "node_modules",
        "TestResults", "dist", "build", "coverage", ".next", "vendor"
    ];

    [GeneratedRegex(@"connectionString\s*=\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringRegex();

    public string ResolveProjectRoot(string? projectRoot = null) =>
        ProjectRootResolver.Resolve(projectRoot, options.Value.ProjectRoot);

    public void ValidateProjectRoot(string? projectRoot = null) =>
        ProjectRootResolver.Validate(ResolveProjectRoot(projectRoot));

    public CodeSearchResult Search(
        string? query,
        string? taskContextText,
        string? projectRoot = null,
        int? maxResults = null)
    {
        ValidateProjectRoot(projectRoot);
        var config = options.Value;
        var root = ResolveProjectRoot(projectRoot);
        var limit = maxResults ?? config.MaxSearchResults;

        var terms = BuildTerms(query, taskContextText, config.MaxTerms);
        if (terms.Count == 0)
            return new CodeSearchResult(root, terms, []);

        var hits = new List<(CodeHit Hit, int Score)>();
        foreach (var file in EnumerateSourceFiles(root))
        {
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            var matchCount = 0;
            var sampleLines = new List<int>();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!terms.Any(term => line.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    continue;

                matchCount++;
                if (sampleLines.Count < 5)
                    sampleLines.Add(i + 1);
            }

            if (matchCount == 0)
                continue;

            var reason = BuildReason(relativePath, terms);
            var score = matchCount + GetPriorityBonus(relativePath);
            hits.Add((new CodeHit(relativePath, matchCount, sampleLines, reason), score));
        }

        var ordered = hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.Hit.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(h => h.Hit)
            .ToList();

        return new CodeSearchResult(root, terms, ordered);
    }

    public string ReadFile(
        string relativePath,
        string? projectRoot = null,
        int? startLine = null,
        int? endLine = null)
    {
        ValidateProjectRoot(projectRoot);
        var root = ResolveProjectRoot(projectRoot);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Caminho fora do ProjectRoot.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Arquivo não encontrado: {relativePath}");

        var lines = File.ReadAllLines(fullPath);
        var maxLines = options.Value.MaxFileReadLines;
        var start = Math.Max(1, startLine ?? 1);
        var end = Math.Min(lines.Length, endLine ?? Math.Min(lines.Length, start + maxLines - 1));

        if (end < start)
            end = start;

        if (end - start + 1 > maxLines)
            end = start + maxLines - 1;

        var slice = lines.Skip(start - 1).Take(end - start + 1).ToArray();
        var content = string.Join(Environment.NewLine, slice);

        if (fullPath.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
            content = ConnectionStringRegex().Replace(content, "connectionString=\"***\"");

        var language = GetMarkdownLanguage(fullPath);
        return $"""
            # {relativePath} (linhas {start}-{end} de {lines.Length})

            ```{language}
            {content}
            ```
            """;
    }

    private static IReadOnlyList<string> BuildTerms(string? query, string? taskContextText, int maxTerms)
    {
        var combined = string.Join(' ', new[] { query, taskContextText }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var terms = TextHelper.ExtractSearchTerms(combined, maxTerms).ToList();

        if (!string.IsNullOrWhiteSpace(query))
        {
            foreach (var part in query.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length >= 3 && !terms.Contains(part, StringComparer.OrdinalIgnoreCase))
                    terms.Add(part);
            }
        }

        return terms.Distinct(StringComparer.OrdinalIgnoreCase).Take(maxTerms).ToList();
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (!SourceFileCatalog.IsSupported(file))
                continue;

            if (IsUnderIgnoredDirectory(root, file))
                continue;

            yield return file;
        }
    }

    private static bool IsUnderIgnoredDirectory(string root, string filePath)
    {
        var relative = Path.GetRelativePath(root, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => IgnoredDirectoryNames.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    private static int GetPriorityBonus(string relativePath) =>
        PriorityFolders.Any(folder =>
            relativePath.Contains($"/{folder}/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase))
            ? 10
            : 0;

    private static string BuildReason(string relativePath, IReadOnlyList<string> terms)
    {
        var folderHint = PriorityFolders.FirstOrDefault(folder =>
            relativePath.Contains($"/{folder}/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase));

        var folderText = folderHint is null ? "arquivo do repositório" : $"pasta {folderHint}";
        return $"Correspondência com termos [{string.Join(", ", terms)}] em {folderText}.";
    }

    private static string GetMarkdownLanguage(string fullPath) =>
        Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".cshtml" or ".vbhtml" or ".html" or ".vue" or ".svelte" => "html",
            ".json" => "json",
            ".xml" or ".config" => "xml",
            ".md" => "markdown",
            ".py" => "python",
            ".js" or ".jsx" => "javascript",
            ".ts" or ".tsx" => "typescript",
            ".sql" => "sql",
            _ => "csharp"
        };
}
