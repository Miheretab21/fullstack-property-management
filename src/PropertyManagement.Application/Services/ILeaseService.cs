using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Leases;

namespace PropertyManagement.Application.Services;

public interface ILeaseService
{
    Task<ServiceResult<List<LeaseDto>>> GetLeasesAsync(Guid? tenantId = null, Guid? unitId = null, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<LeaseDto>> GetLeaseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<LeaseDto>> CreateLeaseAsync(CreateLeaseDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<LeaseDto>> UpdateLeaseAsync(Guid id, UpdateLeaseDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> TerminateLeaseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteLeaseAsync(Guid id, CancellationToken cancellationToken = default);
}
