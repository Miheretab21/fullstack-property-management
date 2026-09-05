using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Infrastructure.Settings;
using Xunit;

namespace PropertyManagement.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "SuperSecretPropertyManagementSecurityKey2026!#SecureJWTAuthenticationKey",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };

        var options = Options.Create(_jwtSettings);
        _sut = new JwtTokenGenerator(options);
    }

    [Fact]
    public void GenerateToken_ValidParameters_ReturnsValidJwtString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "tenant@test.com";
        var firstName = "John";
        var lastName = "Doe";
        var roles = new[] { "Tenant" };

        // Act
        var tokenString = _sut.GenerateToken(userId, email, firstName, lastName, roles);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));

        var jwtToken = handler.ReadJwtToken(tokenString);
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, jwtToken.Audiences.First());
        Assert.Equal(userId.ToString(), jwtToken.Subject);
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == email);
        Assert.Contains(jwtToken.Claims, c => c.Type == "role" && c.Value == "Tenant");
    }
}
