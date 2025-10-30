namespace SemanticUI.Core.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
