namespace SemanticUI.Api.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? MetadataJson { get; set; }

    public virtual Chat Chat { get; set; } = null!;
}
