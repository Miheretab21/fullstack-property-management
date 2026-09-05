using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Units;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Services;

public interface IUnitService
{
    Task<ServiceResult<List<UnitDto>>> GetUnitsAsync(Guid? propertyId = null, UnitStatus? status = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<UnitDto>> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<UnitDto>> CreateUnitAsync(CreateUnitDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<UnitDto>> UpdateUnitAsync(Guid id, UpdateUnitDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<UnitDto>> UpdateUnitStatusAsync(Guid id, UnitStatus newStatus, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default);
}
