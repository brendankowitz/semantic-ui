using System.Security.Claims;

namespace SemanticUI.Api.Security;

public static class UserContextHelper
{
    /// <summary>
    /// Extracts the user ID from claims or returns a default development user ID.
    /// In development without JWT, always returns "dev-user-001".
    /// In production with JWT, extracts from "sub" or "userId" claims.
    /// </summary>
    public static string GetUserId(ClaimsPrincipal? user)
    {
        return user?.FindFirst("sub")?.Value
               ?? user?.FindFirst("userId")?.Value
               ?? "dev-user-001";
    }
}
