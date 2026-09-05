using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Auth;

public class RegisterRequestDto
{
    [Required]
    [MaxLength(100)]
    [DefaultValue("Abebe")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [DefaultValue("Kebede")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [DefaultValue("abebe@example.com")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [DefaultValue("Password@123")]
    public string Password { get; set; } = string.Empty;

    [DefaultValue("0912345678")]
    [RegularExpression(@"^$|^(\+251|0)[79]\d{8}$", ErrorMessage = "Please provide a valid Ethiopian phone number (e.g. +251911234567 or 0911234567).")]
    public string? PhoneNumber { get; set; }

    [DefaultValue(UserRole.Admin)]
    public UserRole Role { get; set; } = UserRole.Admin;
}
