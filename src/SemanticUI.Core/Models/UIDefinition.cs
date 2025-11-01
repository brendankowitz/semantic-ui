namespace SemanticUI.Core.Models;

public class UIDefinition
{
    public string ComponentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "form", "table", "chart", "custom"
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, object> Props { get; set; } = new();
    public Dictionary<string, string> Dependencies { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
