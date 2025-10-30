using SemanticUI.Core.Models;

namespace SemanticUI.Core.DTOs;

public class ChatApiResponse
{
    public string ChatId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public Message? Data { get; set; }
    public UIDefinition? UiComponent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
