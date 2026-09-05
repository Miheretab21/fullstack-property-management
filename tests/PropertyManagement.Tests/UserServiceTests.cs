using Microsoft.AspNetCore.Identity;
using Moq;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Users;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Services;
using Xunit;

namespace PropertyManagement.Tests;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _dbContextMock = new Mock<IApplicationDbContext>();

        _sut = new UserService(_userManagerMock.Object, _roleManagerMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserFound_ReturnsUserDtoWithRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "manager@test.com",
            FirstName = "Alice",
            LastName = "Johnson",
            PhoneNumber = "123456",
            IsActive = true
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "PropertyManager" });

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal("manager@test.com", result.Data!.Email);
        Assert.Contains("PropertyManager", result.Data.Roles);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_RoleDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _roleManagerMock.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UpdateUserRoleAsync(userId, UserRole.Admin);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("does not exist", result.Message);
    }

    [Fact]
    public async Task ToggleUserStatusAsync_InvertsIsActiveFlag()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            IsActive = true
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.ToggleUserStatusAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(user.IsActive);
        Assert.Contains("deactivated", result.Message);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "existing@example.com",
            Password = "Password123!",
            Role = UserRole.Tenant
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(new ApplicationUser { Email = request.Email });

        // Act
        var result = await _sut.CreateUserAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.Message);
    }
}
