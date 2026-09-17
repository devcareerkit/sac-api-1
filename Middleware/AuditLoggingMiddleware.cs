using Microsoft.AspNetCore.Http;
using DsacReporting.Api.Data;

namespace DsacReporting.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    public AuditLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        await _next(context);
        // Minimal placeholder: real implementation would record mutating requests
    }
}
