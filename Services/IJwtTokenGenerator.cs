using DsacReporting.Api.Data.Entities;

namespace DsacReporting.Api.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user);
}
