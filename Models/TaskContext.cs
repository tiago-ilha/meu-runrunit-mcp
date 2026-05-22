using System.Text;

namespace MeuRunrunItMCP.Models;

public sealed record CommentSnippet(
    int Id,
    string Author,
    string? CreatedAt,
    string Text);

public sealed record TaskContext(
    int TaskId,
    string Title,
    string? Description,
    string? ProjectName,
    string? BoardName,
    string? Url,
    IReadOnlyList<CommentSnippet> Comments,
    string CombinedTextForSearch)
{
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Tarefa #{TaskId}: {Title}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(ProjectName))
            sb.AppendLine($"**Projeto:** {ProjectName}");
        if (!string.IsNullOrWhiteSpace(BoardName))
            sb.AppendLine($"**Quadro:** {BoardName}");
        if (!string.IsNullOrWhiteSpace(Url))
            sb.AppendLine($"**URL:** {Url}");
        if (sb.Length > 0 && sb[^1] is not '\n')
            sb.AppendLine();

        sb.AppendLine("## Descrição");
        sb.AppendLine(string.IsNullOrWhiteSpace(Description) ? "_Sem descrição._" : Description.Trim());
        sb.AppendLine();

        sb.AppendLine($"## Comentários ({Comments.Count})");
        if (Comments.Count == 0)
        {
            sb.AppendLine("_Nenhum comentário._");
        }
        else
        {
            foreach (var comment in Comments)
            {
                var when = string.IsNullOrWhiteSpace(comment.CreatedAt) ? "" : $" ({comment.CreatedAt})";
                sb.AppendLine($"### [{comment.Id}] {comment.Author}{when}");
                sb.AppendLine(comment.Text.Trim());
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Texto agregado para busca no código");
        sb.AppendLine("```");
        sb.AppendLine(CombinedTextForSearch);
        sb.AppendLine("```");

        return sb.ToString().TrimEnd();
    }
}
