using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Users;

public class UpdateRoleRequestDto
{
    [Required]
    public UserRole Role { get; set; }
}
