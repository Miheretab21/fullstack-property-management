using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Maintenance;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Services;

public interface IMaintenanceService
{
    Task<ServiceResult<List<MaintenanceRequestDto>>> GetRequestsAsync(Guid? tenantId = null, Guid? unitId = null, MaintenanceStatus? status = null, MaintenancePriority? priority = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<MaintenanceRequestDto>> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MaintenanceRequestDto>> CreateRequestAsync(CreateMaintenanceRequestDto dto, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ServiceResult<MaintenanceRequestDto>> UpdateRequestStatusAsync(Guid id, UpdateMaintenanceStatusDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteRequestAsync(Guid id, CancellationToken cancellationToken = default);
}
