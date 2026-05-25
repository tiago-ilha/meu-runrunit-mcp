using MeuRunrunItMCP.Models;

namespace MeuRunrunItMCP.Abstractions;

public interface IRunrunItClient
{
    Task<TaskContext> GetTaskContextAsync(int taskId, CancellationToken cancellationToken = default);
}
