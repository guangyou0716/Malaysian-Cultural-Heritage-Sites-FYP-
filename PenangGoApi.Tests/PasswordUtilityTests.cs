using PenangGoApp.DataAccess;
using Xunit;

namespace PenangGoApi.Tests;

public class PasswordUtilityTests
{
    [Theory]
    [InlineData("StrongPass1!")]
    [InlineData("Abcdef12@xyz")]
    public void CreatePasswordHash_ProducesVerifiableHash(string password)
    {
        var (hash, salt) = PasswordUtility.CreatePasswordHash(password);

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.False(string.IsNullOrEmpty(salt));
        Assert.NotEqual(hash, password);
        Assert.True(PasswordUtility.VerifyPassword(password, hash, salt));
    }

    [Fact]
    public void VerifyPassword_RejectsWrongPassword()
    {
        var (hash, salt) = PasswordUtility.CreatePasswordHash("Correct1!pass");
        Assert.False(PasswordUtility.VerifyPassword("Wrong1!pass", hash, salt));
    }

    [Theory]
    [InlineData("sh1A!", false)]               // too short
    [InlineData("lowercase1!", false)]         // no uppercase
    [InlineData("UPPERCASE1!", false)]         // no lowercase
    [InlineData("NoDigitsHere!", false)]       // no digit
    [InlineData("NoSpecialChar1", false)]      // no special char
    [InlineData("GoodPass1!", true)]
    public void IsPasswordStrong_EnforcesPolicy(string password, bool expected)
    {
        Assert.Equal(expected, PasswordUtility.IsPasswordStrong(password));
    }
}