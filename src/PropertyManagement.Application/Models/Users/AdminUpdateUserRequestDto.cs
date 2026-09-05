using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Users;

public class AdminUpdateUserRequestDto
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [RegularExpression(@"^(\+251|0)[79]\d{8}$", ErrorMessage = "Phone number must be a valid Ethiopian format (e.g., +251912345678 or 0912345678).")]
    public string? PhoneNumber { get; set; }

    [Required]
    public UserRole Role { get; set; }
}
