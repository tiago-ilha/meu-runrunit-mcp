using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Abstractions;

public interface ITaskAnalysisService
{
    Task<TaskAnalysisReport> AnalyzeAsync(
        int taskId,
        string? projectRoot = null,
        string? extraQuery = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);
}
