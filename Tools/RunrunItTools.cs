using System.ComponentModel;
using MeuRunrunItMCP.Services;
using ModelContextProtocol.Server;

namespace MeuRunrunItMCP.Tools;

[McpServerToolType]
public sealed class RunrunItTools(IRunrunItClient runrunItClient)
{
    [McpServerTool, Description("Retorna o contexto completo de uma tarefa Runrun.it (título, descrição e todos os comentários).")]
    public async Task<string> GetTaskContext(
        [Description("ID numérico da tarefa no Runrun.it")] int taskId,
        CancellationToken cancellationToken)
    {
        var context = await runrunItClient.GetTaskContextAsync(taskId, cancellationToken);
        return context.ToMarkdown();
    }
}
