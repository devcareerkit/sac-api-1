using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private static readonly PasswordHasher<User> Hasher = new();

    public AuthController(AppDbContext db, IJwtTokenGenerator jwtGenerator)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var verifyResult = Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var (token, expiresAt) = _jwtGenerator.GenerateToken(user);
        return Ok(new LoginResponseDto { Token = token, ExpiresAt = expiresAt });
    }
}
