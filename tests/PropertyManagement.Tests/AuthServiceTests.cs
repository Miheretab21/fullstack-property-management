using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Auth;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Infrastructure.Settings;
using Xunit;

namespace PropertyManagement.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly IOptions<JwtSettings> _jwtOptions;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        _jwtOptions = Options.Create(new JwtSettings
        {
            Secret = "SuperSecretPropertyManagementSecurityKey2026!#SecureJWTAuthenticationKey",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        });

        _sut = new AuthService(_userManagerMock.Object, _roleManagerMock.Object, _jwtTokenGeneratorMock.Object, _jwtOptions);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "existing@test.com",
            Password = "Password123",
            FirstName = "Alice",
            LastName = "Smith",
            Role = UserRole.Tenant
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(new ApplicationUser { Email = request.Email });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User with this email already exists.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessAndAuthToken()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "newuser@test.com",
            Password = "Password123",
            FirstName = "Bob",
            LastName = "Smith",
            Role = UserRole.PropertyManager
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock.Setup(m => m.RoleExistsAsync("PropertyManager"))
            .ReturnsAsync(true);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "PropertyManager"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "PropertyManager" });

        _jwtTokenGeneratorMock.Setup(m => m.GenerateToken(
                It.IsAny<Guid>(), request.Email, request.FirstName, request.LastName, It.IsAny<IEnumerable<string>>()))
            .Returns("mocked-jwt-token");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal("mocked-jwt-token", result.Data!.Token);
        Assert.Equal(request.Email, result.Data.Email);
        Assert.Contains("PropertyManager", result.Data.Roles);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "unknown@test.com",
            Password = "WrongPassword"
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "active@test.com",
            FirstName = "Active",
            LastName = "User",
            IsActive = true
        };

        var request = new LoginRequestDto
        {
            Email = "active@test.com",
            Password = "Password123"
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Tenant" });

        _jwtTokenGeneratorMock.Setup(m => m.GenerateToken(
                user.Id, user.Email!, user.FirstName, user.LastName, It.IsAny<IEnumerable<string>>()))
            .Returns("valid-login-token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal("valid-login-token", result.Data!.Token);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ReturnsDeactivatedError()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "inactive@test.com",
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false
        };

        var request = new LoginRequestDto
        {
            Email = "inactive@test.com",
            Password = "Password123"
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
