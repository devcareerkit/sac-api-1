using Microsoft.AspNetCore.Identity;
using Xunit;

namespace DsacReporting.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void VerifyHashedPassword_Succeeds_ForCorrectPassword()
    {
        var hasher = new PasswordHasher<object>();
        var target = new object();
        var hash = hasher.HashPassword(target, "Password123!");

        var result = hasher.VerifyHashedPassword(target, hash, "Password123!");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_Fails_ForWrongPassword()
    {
        var hasher = new PasswordHasher<object>();
        var target = new object();
        var hash = hasher.HashPassword(target, "Password123!");

        var result = hasher.VerifyHashedPassword(target, hash, "WrongPassword!");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
