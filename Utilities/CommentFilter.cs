using System.Text.Json;

namespace MeuRunrunItMCP.Utilities;

internal static class CommentFilter
{
    private static readonly string[] SystemUserIds =
    [
        "runrun-it", "runrunit", "runrun.it", "system", "sistema", "automation", "automacao",
        "bot", "noreply", "no-reply", "notifications", "notificacoes"
    ];

    private static readonly string[] SystemAuthorNames =
    [
        "runrun.it", "runrunit", "sistema", "automação", "automacao", "automation", "bot"
    ];

    private static readonly string[] SystemCommentTypes =
    [
        "system", "automation", "automated", "activity", "event", "history", "notification"
    ];

    public static bool IsUserComment(JsonElement comment)
    {
        if (HasExplicitSystemMarker(comment))
            return false;

        if (TryGetUserIdentity(comment, out var userId, out var userEmail))
            return !IsRunrunItSystemIdentity(userId, userEmail);

        if (TryGetGuestIdentity(comment, out var guestId, out var guestEmail))
            return !IsRunrunItSystemIdentity(guestId, guestEmail);

        var authorName = GetString(comment, "author_name")
            ?? GetString(comment, "user_name")
            ?? GetString(comment, "performer_name");

        if (!string.IsNullOrWhiteSpace(authorName) && IsSystemAuthorName(authorName))
            return false;

        return false;
    }

    private static bool HasExplicitSystemMarker(JsonElement comment)
    {
        if (GetBool(comment, "is_system") == true
            || GetBool(comment, "system") == true
            || GetBool(comment, "automated") == true
            || GetBool(comment, "is_automated") == true
            || GetBool(comment, "from_system") == true)
        {
            return true;
        }

        var commentType = GetString(comment, "comment_type")
            ?? GetString(comment, "type")
            ?? GetString(comment, "kind");

        if (!string.IsNullOrWhiteSpace(commentType)
            && SystemCommentTypes.Contains(commentType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var channel = GetString(comment, "channel_name");
        if (!string.IsNullOrWhiteSpace(channel)
            && channel.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return HasNullUserId(comment);
    }

    private static bool HasNullUserId(JsonElement comment)
    {
        if (!comment.TryGetProperty("user_id", out var userIdProp))
            return false;

        return userIdProp.ValueKind == JsonValueKind.Null
            || (userIdProp.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(userIdProp.GetString()));
    }

    private static bool TryGetUserIdentity(JsonElement comment, out string? id, out string? email)
    {
        id = null;
        email = null;

        if (comment.TryGetProperty("user", out var user) && user.ValueKind == JsonValueKind.Object)
        {
            id = GetString(user, "id");
            email = GetString(user, "email");
            if (!string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(email))
                return true;
        }

        id = GetString(comment, "user_id");
        email = GetString(comment, "user_email");
        return !string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(email);
    }

    private static bool TryGetGuestIdentity(JsonElement comment, out string? id, out string? email)
    {
        id = null;
        email = null;

        if (comment.TryGetProperty("guest", out var guest) && guest.ValueKind == JsonValueKind.Object)
        {
            id = GetString(guest, "id");
            email = GetString(guest, "email");
            if (!string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(email))
                return true;
        }

        id = GetString(comment, "guest_id");
        email = GetString(comment, "guest_email");
        return !string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(email);
    }

    private static bool IsRunrunItSystemIdentity(string? id, string? email)
    {
        if (!string.IsNullOrWhiteSpace(id) && MatchesSystemToken(id))
            return true;

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (MatchesSystemToken(email))
                return true;

            if (email.Contains("noreply", StringComparison.OrdinalIgnoreCase)
                || email.Contains("no-reply", StringComparison.OrdinalIgnoreCase)
                || email.Contains("sistema@", StringComparison.OrdinalIgnoreCase)
                || email.Contains("notifications@", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSystemAuthorName(string authorName) =>
        MatchesSystemToken(authorName);

    private static bool MatchesSystemToken(string value)
    {
        var normalized = value.Trim();
        foreach (var token in SystemUserIds)
        {
            if (normalized.Equals(token, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var name in SystemAuthorNames)
        {
            if (normalized.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
