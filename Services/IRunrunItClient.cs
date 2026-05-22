using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Services;

public interface IRunrunItClient
{
    Task<TaskContext> GetTaskContextAsync(int taskId, CancellationToken cancellationToken = default);
}
