using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Users;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IApplicationDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<ServiceResult<List<UserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList(),
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc
            });
        }

        return ServiceResult<List<UserDto>>.Success(userDtos);
    }

    public async Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Failure("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return ServiceResult<UserDto>.Success(userDto);
    }

    public async Task<ServiceResult<UserDto>> UpdateUserRoleAsync(Guid id, UserRole newRole, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Failure("User not found.");
        }

        var newRoleName = newRole.ToString();
        if (!await _roleManager.RoleExistsAsync(newRoleName))
        {
            return ServiceResult<UserDto>.Failure($"Role '{newRoleName}' does not exist.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("Failed to remove existing roles.", removeResult.Errors.Select(e => e.Description));
        }

        var addResult = await _userManager.AddToRoleAsync(user, newRoleName);
        if (!addResult.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("Failed to assign new role.", addResult.Errors.Select(e => e.Description));
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = updatedRoles.ToList(),
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return ServiceResult<UserDto>.Success(userDto, "User role updated successfully.");
    }

    public async Task<ServiceResult> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult.Failure("User not found.");
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure("Failed to update user status.", result.Errors.Select(e => e.Description));
        }

        var statusString = user.IsActive ? "activated" : "deactivated";
        return ServiceResult.Success($"User {statusString} successfully.");
    }

    public async Task<ServiceResult<UserDto>> UpdateUserProfileAsync(Guid id, UpdateUserProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Failure("User not found.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("Failed to update profile.", result.Errors.Select(e => e.Description));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return ServiceResult<UserDto>.Success(userDto, "User profile updated successfully.");
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser != null)
        {
            return ServiceResult<UserDto>.Failure("A user with this email address already exists.");
        }

        var roleName = request.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return ServiceResult<UserDto>.Failure($"Role '{roleName}' does not exist.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("Failed to create user.", createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("User created, but failed to assign role.", roleResult.Errors.Select(e => e.Description));
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = new List<string> { roleName },
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return ServiceResult<UserDto>.Success(userDto, "User created successfully.");
    }

    public async Task<ServiceResult<UserDto>> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult<UserDto>.Failure("User not found.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<UserDto>.Failure("Failed to update user profile.", updateResult.Errors.Select(e => e.Description));
        }

        var roleName = request.Role.ToString();
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(roleName))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return ServiceResult<UserDto>.Success(userDto, "User updated successfully.");
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ServiceResult.Failure("User not found.");
        }

        // Prevent deleting a user with active leases
        var hasActiveLeases = await _context.Leases.AnyAsync(l => l.TenantId == id && l.IsActive, cancellationToken);
        if (hasActiveLeases)
        {
            return ServiceResult.Failure("Cannot delete a user who has active leases. Please terminate or reassign the leases first, or deactivate the user account instead.");
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return ServiceResult.Failure("Failed to delete user.", deleteResult.Errors.Select(e => e.Description));
        }

        return ServiceResult.Success("User deleted successfully.");
    }
}
