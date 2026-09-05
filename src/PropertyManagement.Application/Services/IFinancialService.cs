using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Financial;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Services;

public interface IFinancialService
{
    Task<ServiceResult<List<TransactionDto>>> GetTransactionsAsync(Guid? leaseId = null, Guid? tenantId = null, TransactionStatus? status = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<TransactionDto>> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<TransactionDto>> CreateRentChargeAsync(CreateRentChargeDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<TransactionDto>> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<TransactionDto>> CreateDirectPaymentAsync(CreateDirectPaymentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<TenantLedgerSummaryDto>> GetTenantLedgerSummaryAsync(Guid leaseId, CancellationToken cancellationToken = default);
}
