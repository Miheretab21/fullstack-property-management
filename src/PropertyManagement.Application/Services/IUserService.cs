using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Users;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Services;

public interface IUserService
{
    Task<ServiceResult<List<UserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> UpdateUserRoleAsync(Guid id, UserRole newRole, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> UpdateUserProfileAsync(Guid id, UpdateUserProfileDto request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
