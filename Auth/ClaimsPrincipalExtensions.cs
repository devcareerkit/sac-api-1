using System.Security.Claims;

namespace DsacReporting.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Guid? GetEntityId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("entity_id");
        return string.IsNullOrEmpty(value) ? null : Guid.Parse(value);
    }

    public static bool IsDsacStaff(this ClaimsPrincipal user) =>
        user.IsInRole("dsac_me") || user.IsInRole("dsac_exec");
}
