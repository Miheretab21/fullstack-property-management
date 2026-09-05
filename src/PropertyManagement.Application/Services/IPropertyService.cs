using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Properties;

namespace PropertyManagement.Application.Services;

public interface IPropertyService
{
    Task<ServiceResult<List<PropertyDto>>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<PropertyDetailDto>> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PropertyDto>> CreatePropertyAsync(CreatePropertyDto dto, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PropertyDto>> UpdatePropertyAsync(Guid id, UpdatePropertyDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeletePropertyAsync(Guid id, CancellationToken cancellationToken = default);
}
