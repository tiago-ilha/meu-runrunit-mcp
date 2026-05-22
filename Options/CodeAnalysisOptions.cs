namespace MeuRunrunItMCP.Options;

public sealed class CodeAnalysisOptions
{
    public const string SectionName = "CodeAnalysis";

    /// <summary>Padrão opcional quando a ferramenta não recebe projectRoot na chamada.</summary>
    public string? ProjectRoot { get; set; }

    public int MaxSearchResults { get; set; } = 25;

    public int MaxFileReadLines { get; set; } = 200;

    public int MaxTerms { get; set; } = 30;
}
