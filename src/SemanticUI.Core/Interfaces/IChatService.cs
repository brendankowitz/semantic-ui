using SemanticUI.Core.Models;
using System.Text.Json;

namespace SemanticUI.Core.Interfaces;

public interface IChatService
{
    IAsyncEnumerable<StreamChunk> StreamResponseAsync(
        string chatId,
        string userId,
        string prompt,
        CancellationToken cancellationToken);

    Task<bool> UserHasAccessAsync(string chatId, string userId);

    Task<string?> GetChatIdForComponentAsync(string componentId);

    Task<ProcessedResponse> ProcessUIInteractionAsync(
        string chatId,
        string componentId,
        JsonElement payload,
        string userId);
}

public class ProcessedResponse
{
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object?>? Metadata { get; set; }
}
