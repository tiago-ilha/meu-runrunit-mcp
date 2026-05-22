using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Services;

public interface ICodeIndexService
{
    string ResolveProjectRoot(string? projectRoot = null);

    void ValidateProjectRoot(string? projectRoot = null);

    CodeSearchResult Search(
        string? query,
        string? taskContextText,
        string? projectRoot = null,
        int? maxResults = null);

    string ReadFile(
        string relativePath,
        string? projectRoot = null,
        int? startLine = null,
        int? endLine = null);
}
