using System.Net.Http.Headers;
using System.Text.Json;
using MeuRunrunItMCP.Abstractions;
using MeuRunrunItMCP.Models;
using MeuRunrunItMCP.Options;
using MeuRunrunItMCP.Utilities;
using Microsoft.Extensions.Options;

namespace MeuRunrunItMCP.Services;

public sealed class RunrunItClient(HttpClient httpClient, IOptions<RunrunItOptions> options) : IRunrunItClient
{
    public async Task<TaskContext> GetTaskContextAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        ValidateCredentials(config);

        using var taskRequest = CreateRequest(HttpMethod.Get, config, $"tasks/{taskId}");
        using var taskResponse = await httpClient.SendAsync(taskRequest, cancellationToken);
        await EnsureSuccessAsync(taskResponse, cancellationToken);

        await using var taskStream = await taskResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var taskDoc = await JsonDocument.ParseAsync(taskStream, cancellationToken: cancellationToken);
        var taskRoot = UnwrapRoot(taskDoc.RootElement);

        var title = GetString(taskRoot, "title") ?? $"Tarefa {taskId}";
        var descriptionRaw = await GetTaskDescriptionAsync(config, taskId, taskRoot, cancellationToken);
        var projectName = GetString(taskRoot, "project_name");
        var boardName = GetString(taskRoot, "board_name");
        var url = GetString(taskRoot, "url");

        var comments = await GetCommentsAsync(config, taskId, cancellationToken);
        var combined = TextHelper.BuildCombinedSearchText(title, descriptionRaw, comments);

        return new TaskContext(
            taskId,
            title,
            string.IsNullOrWhiteSpace(descriptionRaw) ? null : TextHelper.StripHtml(descriptionRaw),
            projectName,
            boardName,
            url,
            comments,
            combined);
    }

    private async Task<string?> GetTaskDescriptionAsync(
        RunrunItOptions config,
        int taskId,
        JsonElement taskRoot,
        CancellationToken cancellationToken)
    {
        var fromDescriptionsEndpoint = await GetDescriptionFromDescriptionsApiAsync(config, taskId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromDescriptionsEndpoint))
            return fromDescriptionsEndpoint;

        return ExtractDescriptionFromElement(taskRoot);
    }

    private async Task<string?> GetDescriptionFromDescriptionsApiAsync(
        RunrunItOptions config,
        int taskId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            config,
            "descriptions",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["subject_type"] = "Task",
                ["subject_id"] = taskId.ToString()
            });

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ParseDescriptionPayload(doc.RootElement);
    }

    private static string? ParseDescriptionPayload(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var text = ExtractDescriptionFromElement(item);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
        }

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        if (root.TryGetProperty("description", out var descriptionNode))
        {
            var direct = ExtractDescriptionFromProperty(descriptionNode);
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;
        }

        if (root.TryGetProperty("data", out var data))
            return ParseDescriptionPayload(data);

        if (root.TryGetProperty("descriptions", out var descriptions))
            return ParseDescriptionPayload(descriptions);

        return ExtractDescriptionFromElement(root);
    }

    private static string? ExtractDescriptionFromElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (element.TryGetProperty("description", out var descriptionNode))
        {
            var nested = ExtractDescriptionFromProperty(descriptionNode);
            if (!string.IsNullOrWhiteSpace(nested))
                return nested;
        }

        return GetString(element, "body")
            ?? GetString(element, "html")
            ?? GetString(element, "text");
    }

    private static string? ExtractDescriptionFromProperty(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Object => GetString(value, "description")
                ?? GetString(value, "body")
                ?? GetString(value, "html")
                ?? GetString(value, "text"),
            _ => null
        };

    private async Task<IReadOnlyList<CommentSnippet>> GetCommentsAsync(
        RunrunItOptions config,
        int taskId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, config, $"tasks/{taskId}/comments");
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return [];

        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ParseComments(doc.RootElement);
    }

    private static IReadOnlyList<CommentSnippet> ParseComments(JsonElement root)
    {
        var elements = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object when root.TryGetProperty("comments", out var comments) && comments.ValueKind == JsonValueKind.Array
                => comments.EnumerateArray(),
            JsonValueKind.Object when root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
                => data.EnumerateArray(),
            _ => Enumerable.Empty<JsonElement>()
        };

        var result = new List<CommentSnippet>();
        foreach (var element in elements)
        {
            var commentElement = element.TryGetProperty("comment", out var wrapped) ? wrapped : element;

            if (!CommentFilter.IsUserComment(commentElement))
                continue;

            var id = GetInt(commentElement, "id") ?? 0;
            var text = TextHelper.StripHtml(GetString(commentElement, "text") ?? GetString(commentElement, "body"));
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var author = GetCommentAuthor(commentElement);
            var createdAt = GetString(commentElement, "created_at")
                ?? GetString(commentElement, "happened_at");

            result.Add(new CommentSnippet(id, author, createdAt, text));
        }

        return result;
    }

    private static string GetCommentAuthor(JsonElement element)
    {
        if (element.TryGetProperty("user", out var user) && user.ValueKind == JsonValueKind.Object)
        {
            return GetString(user, "name")
                ?? GetString(user, "id")
                ?? GetString(user, "email")
                ?? "Desconhecido";
        }

        return GetString(element, "user_name")
            ?? GetString(element, "author_name")
            ?? GetString(element, "performer_name")
            ?? "Desconhecido";
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        RunrunItOptions config,
        string relativePath,
        IReadOnlyDictionary<string, string>? query = null)
    {
        var baseUrl = config.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{relativePath}";

        if (query is { Count: > 0 })
        {
            var queryString = string.Join(
                "&",
                query.Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
            url = $"{url}?{queryString}";
        }

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("App-Key", config.AppKey);
        request.Headers.Add("User-Token", config.UserToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static void ValidateCredentials(RunrunItOptions config)
    {
        if (string.IsNullOrWhiteSpace(config.AppKey) || string.IsNullOrWhiteSpace(config.UserToken))
        {
            throw new InvalidOperationException(
                "Credenciais Runrun.it ausentes. Configure RunrunIt:AppKey e RunrunIt:UserToken em User Secrets ou variáveis de ambiente.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Runrun.it retornou {(int)response.StatusCode} ({response.ReasonPhrase}). Corpo: {Truncate(body, 500)}");
    }

    private static JsonElement UnwrapRoot(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("task", out var task))
            return task;

        return root;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            return number;

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
