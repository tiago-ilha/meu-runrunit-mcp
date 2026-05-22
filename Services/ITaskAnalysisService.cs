using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Services;

public interface ITaskAnalysisService
{
    Task<TaskAnalysisReport> AnalyzeAsync(
        int taskId,
        string? projectRoot = null,
        string? extraQuery = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);
}
