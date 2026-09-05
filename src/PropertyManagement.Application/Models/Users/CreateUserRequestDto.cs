using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Users;

public class CreateUserRequestDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^(\+251|0)[79]\d{8}$", ErrorMessage = "Phone number must be a valid Ethiopian format (e.g., +251912345678 or 0912345678).")]
    public string? PhoneNumber { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Tenant;
}
