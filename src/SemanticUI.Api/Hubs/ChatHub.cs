using Microsoft.AspNetCore.SignalR;
using SemanticUI.Core.Interfaces;
using SemanticUI.Core.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SemanticUI.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task JoinChatRoom(string chatId)
    {
        var userId = GetUserId();

        // Verify user has access to this chat
        if (!await _chatService.UserHasAccessAsync(chatId, userId))
        {
            throw new HubException("Access denied to this chat");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        _logger.LogInformation("User {UserId} joined chat {ChatId}", userId, chatId);
    }

    public async Task LeaveChatRoom(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        _logger.LogInformation("User {UserId} left chat {ChatId}", GetUserId(), chatId);
    }

    public async IAsyncEnumerable<StreamChunk> StreamChatResponse(
        string chatId,
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        _logger.LogInformation("Streaming response for chat {ChatId}, user {UserId}", chatId, userId);

        await foreach (var chunk in _chatService.StreamResponseAsync(
            chatId, userId, prompt, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task SubmitUIPayload(string componentId, JsonElement payload)
    {
        var userId = GetUserId();
        var chatId = await _chatService.GetChatIdForComponentAsync(componentId);

        if (chatId == null || !await _chatService.UserHasAccessAsync(chatId, userId))
        {
            throw new HubException("Invalid component or access denied");
        }

        _logger.LogInformation("Processing UI interaction for component {ComponentId}", componentId);

        var response = await _chatService.ProcessUIInteractionAsync(
            chatId, componentId, payload, userId);

        // Send response back to the chat room
        await Clients.Group(chatId).SendAsync("ReceiveMessage", new
        {
            role = "assistant",
            content = response.Content,
            timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}",
            userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected: {Exception}",
            userId, exception?.Message ?? "Normal disconnect");
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        // In development, use a default user ID
        // In production, extract from JWT token claims
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("userId")?.Value
                     ?? "dev-user-001";

        return userId;
    }
}
