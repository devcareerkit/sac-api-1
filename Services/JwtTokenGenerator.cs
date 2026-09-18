using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DsacReporting.Api.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("email", user.Email),
            // ClaimTypes.Role is required for ASP.NET's IsInRole()/[Authorize(Roles=...)] to
            // work server-side, but its string value is a long XML-namespace URI, not "role" —
            // add a plain "role" claim too so external consumers (e.g. the frontend) can read
            // it without knowing that mapping.
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
        };

        if (user.EntityId.HasValue)
        {
            claims.Add(new Claim("entity_id", user.EntityId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }
}
