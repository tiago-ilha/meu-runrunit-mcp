namespace MeuRunrunItMCP.Models;

public sealed record CodeHit(
    string RelativePath,
    int MatchCount,
    IReadOnlyList<int> SampleLineNumbers,
    string Reason);

public sealed record CodeSearchResult(
    string ProjectRoot,
    IReadOnlyList<string> SearchTerms,
    IReadOnlyList<CodeHit> Hits)
{
    public string ToMarkdown()
    {
        if (Hits.Count == 0)
            return $"Nenhum arquivo encontrado em `{ProjectRoot}` para os termos: {string.Join(", ", SearchTerms)}.";

        var lines = new List<string>
        {
            $"# Busca no código ({Hits.Count} arquivo(s))",
            "",
            $"**Raiz:** `{ProjectRoot}`",
            $"**Termos:** {string.Join(", ", SearchTerms)}",
            ""
        };

        var index = 1;
        foreach (var hit in Hits)
        {
            var sampleLines = hit.SampleLineNumbers.Count > 0
                ? $" — linhas: {string.Join(", ", hit.SampleLineNumbers)}"
                : "";
            lines.Add($"{index}. `{hit.RelativePath}` ({hit.MatchCount} ocorrência(s){sampleLines})");
            lines.Add($"   - {hit.Reason}");
            index++;
        }

        return string.Join(Environment.NewLine, lines);
    }
}
