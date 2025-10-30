namespace SemanticUI.Api.Models;

public class UIInteraction
{
    public Guid Id { get; set; }
    public Guid ComponentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
