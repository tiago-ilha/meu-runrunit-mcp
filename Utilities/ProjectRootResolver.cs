namespace MeuRunrunItMCP.Utilities;

internal static class ProjectRootResolver
{
    public static string Resolve(string? projectRootOverride, string? configuredDefault)
    {
        var candidate = string.IsNullOrWhiteSpace(projectRootOverride)
            ? configuredDefault
            : projectRootOverride;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException(
                "Informe projectRoot na chamada da ferramenta (caminho absoluto da raiz do repositório) " +
                "ou configure CodeAnalysis:ProjectRoot como padrão opcional.");
        }

        return Path.GetFullPath(candidate.Trim());
    }

    public static void Validate(string root)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"ProjectRoot não encontrado: {root}");

        if (LooksLikeCodeRepository(root))
            return;

        throw new InvalidOperationException(
            $"ProjectRoot '{root}' não parece um repositório de código " +
            "(esperado: .sln, .csproj, package.json, pom.xml, go.mod, pyproject.toml ou arquivos-fonte conhecidos).");
    }

    private static bool LooksLikeCodeRepository(string root)
    {
        if (HasFileInTree(root, "*.sln", maxDepth: 3)
            || HasFileInTree(root, "*.slnx", maxDepth: 3)
            || HasFileInTree(root, "*.csproj", maxDepth: 4)
            || File.Exists(Path.Combine(root, "package.json"))
            || File.Exists(Path.Combine(root, "pom.xml"))
            || File.Exists(Path.Combine(root, "go.mod"))
            || File.Exists(Path.Combine(root, "pyproject.toml"))
            || File.Exists(Path.Combine(root, "Cargo.toml"))
            || File.Exists(Path.Combine(root, "Web.config")))
        {
            return true;
        }

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Take(500)
            .Any(SourceFileCatalog.IsSupported);
    }

    private static bool HasFileInTree(string root, string pattern, int maxDepth)
    {
        foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            var depth = Path.GetRelativePath(root, file)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Length;

            if (depth <= maxDepth)
                return true;
        }

        return false;
    }
}
