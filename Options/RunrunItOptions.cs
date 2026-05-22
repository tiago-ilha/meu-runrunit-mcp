namespace MeuRunrunItMCP.Options;

public sealed class RunrunItOptions
{
    public const string SectionName = "RunrunIt";

    public string BaseUrl { get; set; } = "https://runrun.it/api/v1.0";

    public string AppKey { get; set; } = "";

    public string UserToken { get; set; } = "";
}
