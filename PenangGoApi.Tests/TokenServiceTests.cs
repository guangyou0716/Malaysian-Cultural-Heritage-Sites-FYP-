using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using PenangGoApp.DataAccess;
using PenangGoApi.Services;
using Xunit;

namespace PenangGoApi.Tests;

public class TokenServiceTests
{
    private static TokenService CreateService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-long-enough-for-hmac!!"
            })
            .Build());

    private static User SampleUser() => new()
    {
        Id = 7,
        Name = "Test User",
        Email = "test@penanggo.com",
        IsAdmin = false
    };

    [Fact]
    public void GenerateAccessToken_ProducesTokenWithIdentityClaims()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(SampleUser());
        Assert.False(string.IsNullOrWhiteSpace(token));

        var principal = service.ValidateToken(token);
        Assert.NotNull(principal);
        Assert.Equal("7", principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("test@penanggo.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal("User", principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public void GenerateAccessToken_AdminUserGetsAdminRoleClaim()
    {
        var admin = SampleUser();
        admin.IsAdmin = true;

        var service = CreateService();
        var principal = service.ValidateToken(service.GenerateAccessToken(admin));

        Assert.Equal("Admin", principal!.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public void ValidateToken_RejectsTamperedToken()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(SampleUser());
        var tampered = token[..^2] + (token[^1] == 'A' ? "BC" : "AA");

        Assert.Null(service.ValidateToken(tampered));
    }

    [Fact]
    public void HashRefreshToken_IsStableAndNotPlaintext()
    {
        var service = CreateService();
        var hash = service.HashRefreshToken("refresh-token-value");

        Assert.NotEqual("refresh-token-value", hash);
        Assert.Equal(hash, service.HashRefreshToken("refresh-token-value"));
        Assert.NotEqual(hash, service.HashRefreshToken("other-token"));
    }

    [Fact]
    public void GenerateRefreshToken_IsUniquePerCall()
    {
        var service = CreateService();
        Assert.NotEqual(service.GenerateRefreshToken(), service.GenerateRefreshToken());
    }
}