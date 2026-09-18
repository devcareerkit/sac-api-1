using System.IdentityModel.Tokens.Jwt;
using DsacReporting.Api.Auth;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace DsacReporting.Api.Tests;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(string secret = "test-secret-at-least-32-chars-long!!") =>
        new(Options.Create(new JwtSettings { Secret = secret }));

    [Fact]
    public void GenerateToken_IncludesRoleAndEntityIdClaims()
    {
        var generator = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            Role = "entity_officer",
            EntityId = Guid.NewGuid(),
            FullName = "Thandi",
            PasswordHash = "irrelevant-for-this-test"
        };

        var (token, expiresAt) = generator.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == "email").Value);
        Assert.Equal(user.Role, jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
        Assert.Equal(user.Role, jwt.Claims.First(c => c.Type == "role").Value);
        Assert.Equal(user.EntityId.ToString(), jwt.Claims.First(c => c.Type == "entity_id").Value);
        Assert.True(expiresAt > DateTimeOffset.UtcNow.AddHours(7));
        Assert.True(expiresAt <= DateTimeOffset.UtcNow.AddHours(8).AddMinutes(1));
    }

    [Fact]
    public void GenerateToken_OmitsEntityIdClaim_WhenUserHasNoEntity()
    {
        var generator = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sipho@example.com",
            Role = "dsac_me",
            EntityId = null,
            FullName = "Sipho",
            PasswordHash = "irrelevant-for-this-test"
        };

        var (token, _) = generator.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "entity_id");
    }
}
