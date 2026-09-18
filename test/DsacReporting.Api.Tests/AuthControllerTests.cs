using DsacReporting.Api.Auth;
using DsacReporting.Api.Controllers;
using DsacReporting.Api.Data;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace DsacReporting.Api.Tests;

public class AuthControllerTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateController(AppDbContext db)
    {
        var jwtGenerator = new JwtTokenGenerator(
            Options.Create(new JwtSettings { Secret = "test-secret-at-least-32-chars-long!!" }));
        return new AuthController(db, jwtGenerator);
    }

    [Fact]
    public async Task Login_ReturnsToken_ForCorrectCredentials()
    {
        using var db = CreateDb(nameof(Login_ReturnsToken_ForCorrectCredentials));
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            FullName = "Thandi",
            Role = "entity_officer",
            EntityId = Guid.NewGuid()
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Login(new LoginRequestDto { Email = "thandi@example.com", Password = "Password123!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.Token));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        using var db = CreateDb(nameof(Login_ReturnsUnauthorized_ForWrongPassword));
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "thandi@example.com",
            FullName = "Thandi",
            Role = "entity_officer"
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Login(new LoginRequestDto { Email = "thandi@example.com", Password = "WrongPassword!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForUnknownEmail()
    {
        using var db = CreateDb(nameof(Login_ReturnsUnauthorized_ForUnknownEmail));
        var controller = CreateController(db);

        var result = await controller.Login(new LoginRequestDto { Email = "nobody@example.com", Password = "Password123!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
