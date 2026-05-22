using System.Reflection;
using MeuRunrunItMCP.Options;
using MeuRunrunItMCP.Services;
using MeuRunrunItMCP.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeuRunrunItMCP;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

        builder.Services
            .Configure<RunrunItOptions>(builder.Configuration.GetSection(RunrunItOptions.SectionName))
            .Configure<CodeAnalysisOptions>(builder.Configuration.GetSection(CodeAnalysisOptions.SectionName));

        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddHttpClient<IRunrunItClient, RunrunItClient>();
        builder.Services.AddSingleton<ICodeIndexService, CodeIndexService>();
        builder.Services.AddSingleton<ITaskAnalysisService, TaskAnalysisService>();

        builder.Services.AddSingleton<RunrunItTools>();
        builder.Services.AddSingleton<CodeAnalysisTools>();
        builder.Services.AddSingleton<TaskAnalysisTools>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}
