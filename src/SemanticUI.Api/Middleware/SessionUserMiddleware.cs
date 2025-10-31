using System.Security.Claims;

namespace SemanticUI.Api.Middleware;

/// <summary>
/// Middleware that assigns a simple GUID-based user ID via cookie for development.
/// Creates a ClaimsPrincipal with the user ID so all endpoints can access User.FindFirst("userId").
/// </summary>
public class SessionUserMiddleware
{
    private readonly RequestDelegate _next;
    private const string UserIdCookie = "X-User-Id";
    private const string UserIdClaim = "userId";

    public SessionUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<SessionUserMiddleware> logger)
    {
        string userId;

        // Try to get userId from query parameter first (for WebSocket/SignalR)
        if (context.Request.Query.TryGetValue("userId", out var queryUserId) && !string.IsNullOrEmpty(queryUserId))
        {
            userId = queryUserId.ToString();
            logger.LogInformation("SessionUserMiddleware: Got userId from query parameter: {UserId}, Path={Path}", userId, context.Request.Path);
        }
        // Then try to get from cookie
        else if (context.Request.Cookies.TryGetValue(UserIdCookie, out var cookieUserId) && !string.IsNullOrEmpty(cookieUserId))
        {
            userId = cookieUserId;
            logger.LogInformation("SessionUserMiddleware: Got userId from cookie: {UserId}, Path={Path}", userId, context.Request.Path);
        }
        // Generate a new one if neither exists
        else
        {
            userId = Guid.NewGuid().ToString();

            // Only set cookie for non-WebSocket requests
            if (!context.WebSockets.IsWebSocketRequest)
            {
                // Set the cookie (expires in 7 days)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                };

                context.Response.Cookies.Append(UserIdCookie, userId, cookieOptions);
            }

            logger.LogInformation("SessionUserMiddleware: New session created with userId={UserId}, Path={Path}", userId, context.Request.Path);
        }

        // Create a ClaimsPrincipal with the user ID so it's available via User claims
        var claims = new[] { new Claim(UserIdClaim, userId) };
        var identity = new ClaimsIdentity(claims, "SessionUserMiddleware");
        var principal = new ClaimsPrincipal(identity);

        context.User = principal;
        logger.LogInformation("SessionUserMiddleware: Set context.User.Claims userId={UserId}", userId);

        await _next(context);
    }
}
