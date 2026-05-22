using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Services;

public sealed class TaskAnalysisService(
    IRunrunItClient runrunItClient,
    ICodeIndexService codeIndexService) : ITaskAnalysisService
{
    public async Task<TaskAnalysisReport> AnalyzeAsync(
        int taskId,
        string? projectRoot = null,
        string? extraQuery = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        var task = await runrunItClient.GetTaskContextAsync(taskId, cancellationToken);
        var search = codeIndexService.Search(extraQuery, task.CombinedTextForSearch, projectRoot, maxResults);
        return new TaskAnalysisReport(task, search);
    }
}
