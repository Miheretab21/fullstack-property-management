using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Auth;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Settings;

namespace PropertyManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ServiceResult<AuthResponseDto>.Failure("User with this email already exists.");
        }

        var roleName = request.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return ServiceResult<AuthResponseDto>.Failure($"Role '{roleName}' does not exist.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return ServiceResult<AuthResponseDto>.Failure(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description);
            return ServiceResult<AuthResponseDto>.Failure(errors);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FirstName, user.LastName, roles);

        var response = new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList(),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        };

        return ServiceResult<AuthResponseDto>.Success(response, "User registered successfully.");
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthResponseDto>.Failure("Your account has been deactivated. Please contact an administrator.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email!, user.FirstName, user.LastName, roles);

        var response = new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList(),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        };

        return ServiceResult<AuthResponseDto>.Success(response, "Login successful.");
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ServiceResult.Failure("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return ServiceResult.Failure("Failed to change password.", errors);
        }

        return ServiceResult.Success("Password changed successfully.");
    }
}
