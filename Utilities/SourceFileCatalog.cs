namespace MeuRunrunItMCP.Utilities;

internal static class SourceFileCatalog
{
    internal static readonly string[] Extensions =
    [
        ".cs", ".cshtml", ".vb", ".vbhtml", ".fs", ".fsx",
        ".js", ".jsx", ".ts", ".tsx", ".vue", ".svelte",
        ".py", ".java", ".kt", ".go", ".rs", ".rb", ".php",
        ".json", ".xml", ".config", ".asax", ".sql", ".md"
    ];

    public static bool IsSupported(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
