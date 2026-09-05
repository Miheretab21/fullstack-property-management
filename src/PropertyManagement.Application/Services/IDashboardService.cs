using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Dashboard;

namespace PropertyManagement.Application.Services;

public interface IDashboardService
{
    Task<ServiceResult<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}
