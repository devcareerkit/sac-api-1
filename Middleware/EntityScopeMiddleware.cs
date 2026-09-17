using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DsacReporting.Api.Middleware;

/// <summary>
/// Ensures that users with role `entity_officer` can only operate within their assigned EntityId.
/// This is a minimal placeholder: it expects an `X-Entity-Id` header for requests and will
/// reject requests where the header does not match the user's `entity_id` claim.
/// </summary>
public class EntityScopeMiddleware
{
    private readonly RequestDelegate _next;
    public EntityScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true && user.IsInRole("entity_officer"))
        {
            var claimEntity = user.FindFirst("entity_id")?.Value;
            var header = context.Request.Headers["X-Entity-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header) && !string.IsNullOrEmpty(claimEntity))
            {
                if (header != claimEntity)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Forbidden: entity scope mismatch.");
                    return;
                }
            }
            // If no header provided, you might inject the claim or fail depending on policy.
        }

        await _next(context);
    }
}
