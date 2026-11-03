using System.Reflection;
using PenangGoApi.DTOs;
using Xunit;

namespace PenangGoApi.Tests;

public class UpdateUserRequestTests
{
    [Theory]
    [InlineData("Id")]
    [InlineData("IsAdmin")]
    [InlineData("PasswordHash")]
    [InlineData("PasswordSalt")]
    [InlineData("RefreshTokenHash")]
    [InlineData("SessionToken")]
    public void UpdateUserRequest_ExcludesSensitiveFields(string propertyName)
    {
        var props = typeof(UpdateUserRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name);

        Assert.DoesNotContain(propertyName, props);
    }

    [Fact]
    public void UpdateUserRequest_AllowsProfileFields()
    {
        var props = typeof(UpdateUserRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Contains("Name", props);
        Assert.Contains("PhoneNumber", props);
        Assert.Contains("Bio", props);
        Assert.Contains("AccessibilityPreference", props);
    }
}