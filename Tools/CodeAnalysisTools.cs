using System.ComponentModel;
using MeuRunrunItMCP.Services;
using ModelContextProtocol.Server;

namespace MeuRunrunItMCP.Tools;

[McpServerToolType]
public sealed class CodeAnalysisTools(ICodeIndexService codeIndexService)
{
    [McpServerTool, Description("Valida a raiz do repositório informada ou o padrão em CodeAnalysis:ProjectRoot.")]
    public string ValidateProjectRoot(
        [Description("Caminho absoluto da raiz do repositório (opcional; usa o padrão da config se omitido)")] string? projectRoot)
    {
        codeIndexService.ValidateProjectRoot(projectRoot);
        return $"ProjectRoot válido: `{codeIndexService.ResolveProjectRoot(projectRoot)}`";
    }

    [McpServerTool, Description("Busca arquivos relevantes no repositório com base em termos ou texto da tarefa.")]
    public string Search(
        [Description("Caminho absoluto da raiz do repositório a analisar")] string? projectRoot,
        [Description("Termos adicionais de busca (opcional)")] string? query,
        [Description("Texto da tarefa/comentários para extrair termos (opcional)")] string? taskContext,
        [Description("Número máximo de arquivos no resultado (opcional)")] int? maxResults)
    {
        var result = codeIndexService.Search(query, taskContext, projectRoot, maxResults);
        return result.ToMarkdown();
    }

    [McpServerTool, Description("Lê trecho de um arquivo (caminho relativo à raiz do repositório).")]
    public string ReadFile(
        [Description("Caminho absoluto da raiz do repositório")] string? projectRoot,
        [Description("Caminho relativo, ex: src/Controllers/HomeController.cs")] string relativePath,
        [Description("Linha inicial (1-based, opcional)")] int? startLine,
        [Description("Linha final (1-based, opcional)")] int? endLine)
    {
        return codeIndexService.ReadFile(relativePath, projectRoot, startLine, endLine);
    }
}
