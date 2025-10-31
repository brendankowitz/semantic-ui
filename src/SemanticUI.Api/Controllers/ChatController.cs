using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemanticUI.Api.Data;
using SemanticUI.Api.Security;
using SemanticUI.Core.DTOs;

namespace SemanticUI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AppDbContext dbContext, ILogger<ChatController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetChats()
    {
        var userId = UserContextHelper.GetUserId(User);

        var chats = await _dbContext.Chats
            .Where(c => c.UserId == userId && !c.IsArchived)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<object>>
        {
            Success = true,
            Data = chats.Cast<object>().ToList()
        });
    }

    [HttpGet("{chatId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetChat(Guid chatId)
    {
        var userId = UserContextHelper.GetUserId(User);

        var chat = await _dbContext.Chats
            .Where(c => c.Id == chatId && c.UserId == userId)
            .Include(c => c.Messages)
            .Include(c => c.UIComponents)
            .FirstOrDefaultAsync();

        if (chat == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Chat not found"
            });
        }

        var result = new
        {
            chat.Id,
            chat.Title,
            chat.CreatedAt,
            chat.UpdatedAt,
            Messages = chat.Messages.OrderBy(m => m.CreatedAt).Select(m => new
            {
                m.Id,
                m.Role,
                m.Content,
                m.CreatedAt
            }),
            UIComponents = chat.UIComponents.Where(c => c.IsActive).Select(c => new
            {
                c.Id,
                c.ComponentType,
                c.Code,
                c.PropsJson,
                c.DependenciesJson,
                c.CreatedAt
            })
        };

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateChat([FromBody] CreateChatRequest request)
    {
        var userId = UserContextHelper.GetUserId(User);
        _logger.LogInformation("CreateChat: userId={UserId}, title={Title}", userId, request.Title);

        var chat = new Models.Chat
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title ?? "New Chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Chats.Add(chat);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Chat created: chatId={ChatId}, userId={UserId}", chat.Id, userId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                chat.Id,
                chat.Title,
                chat.CreatedAt
            }
        });
    }

    [HttpDelete("{chatId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteChat(Guid chatId)
    {
        var userId = UserContextHelper.GetUserId(User);

        var chat = await _dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

        if (chat == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Chat not found"
            });
        }

        chat.IsArchived = true;
        await _dbContext.SaveChangesAsync();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Chat archived successfully"
        });
    }
}

public class CreateChatRequest
{
    public string? Title { get; set; }
}
