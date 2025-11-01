namespace SemanticUI.Api.Models;

public class UIComponent
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid? MessageId { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? PropsJson { get; set; }
    public string? DependenciesJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Chat Chat { get; set; } = null!;
}
