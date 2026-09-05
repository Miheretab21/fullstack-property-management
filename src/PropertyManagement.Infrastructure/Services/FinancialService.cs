using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Financial;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class FinancialService : IFinancialService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService? _currentUserService;

    public FinancialService(
        IApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<List<TransactionDto>>> GetTransactionsAsync(Guid? leaseId = null, Guid? tenantId = null, TransactionStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Lease)
                .ThenInclude(l => l!.Unit)
                    .ThenInclude(u => u!.Property)
            .AsQueryable();

        // If caller is PropertyManager (and not Admin and not Tenant), filter by assigned property
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            query = query.Where(t => t.Lease != null && t.Lease.Unit != null && t.Lease.Unit.Property != null && t.Lease.Unit.Property.AssignedManagerId == managerId);
        }

        if (leaseId.HasValue)
        {
            query = query.Where(t => t.LeaseId == leaseId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.Lease != null && t.Lease.TenantId == tenantId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.PaymentDate)
            .ToListAsync(cancellationToken);

        var dtos = new List<TransactionDto>();
        foreach (var t in transactions)
        {
            var tenantName = "Unknown";
            if (t.Lease != null)
            {
                var tenant = await _userManager.FindByIdAsync(t.Lease.TenantId.ToString());
                if (tenant != null)
                {
                    tenantName = $"{tenant.FirstName} {tenant.LastName}".Trim();
                }
            }

            dtos.Add(new TransactionDto
            {
                Id = t.Id,
                LeaseId = t.LeaseId,
                UnitId = t.Lease?.UnitId ?? Guid.Empty,
                UnitNumber = t.Lease?.Unit?.UnitNumber ?? string.Empty,
                PropertyName = t.Lease?.Unit?.Property?.Name ?? string.Empty,
                TenantId = t.Lease?.TenantId ?? Guid.Empty,
                TenantName = tenantName,
                Amount = t.Amount,
                PaymentDate = t.PaymentDate,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc
            });
        }

        return ServiceResult<List<TransactionDto>>.Success(dtos);
    }

    public async Task<ServiceResult<TransactionDto>> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Lease)
                .ThenInclude(l => l!.Unit)
                    .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (t == null)
        {
            return ServiceResult<TransactionDto>.Failure("Transaction not found.");
        }

        var tenantName = "Unknown";
        if (t.Lease != null)
        {
            var tenant = await _userManager.FindByIdAsync(t.Lease.TenantId.ToString());
            if (tenant != null)
            {
                tenantName = $"{tenant.FirstName} {tenant.LastName}".Trim();
            }
        }

        var dto = new TransactionDto
        {
            Id = t.Id,
            LeaseId = t.LeaseId,
            UnitId = t.Lease?.UnitId ?? Guid.Empty,
            UnitNumber = t.Lease?.Unit?.UnitNumber ?? string.Empty,
            PropertyName = t.Lease?.Unit?.Property?.Name ?? string.Empty,
            TenantId = t.Lease?.TenantId ?? Guid.Empty,
            TenantName = tenantName,
            Amount = t.Amount,
            PaymentDate = t.PaymentDate,
            PaymentMethod = t.PaymentMethod,
            Status = t.Status,
            CreatedAtUtc = t.CreatedAtUtc
        };

        return ServiceResult<TransactionDto>.Success(dto);
    }

    public async Task<ServiceResult<TransactionDto>> CreateRentChargeAsync(CreateRentChargeDto dto, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(l => l.Id == dto.LeaseId, cancellationToken);

        if (lease == null)
        {
            return ServiceResult<TransactionDto>.Failure("Referenced Lease not found.");
        }

        if (!lease.IsActive)
        {
            return ServiceResult<TransactionDto>.Failure("Cannot create rent charge for an inactive lease.");
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            LeaseId = lease.Id,
            Amount = dto.Amount,
            PaymentDate = dto.DueDate.HasValue 
                ? DateTime.SpecifyKind(dto.DueDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow,
            PaymentMethod = "Invoice / Due",
            Status = TransactionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(lease.TenantId.ToString());

        var resultDto = new TransactionDto
        {
            Id = transaction.Id,
            LeaseId = transaction.LeaseId,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
            PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
            TenantId = lease.TenantId,
            TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown",
            Amount = transaction.Amount,
            PaymentDate = transaction.PaymentDate,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status,
            CreatedAtUtc = transaction.CreatedAtUtc
        };

        return ServiceResult<TransactionDto>.Success(resultDto, "Rent charge generated successfully.");
    }

    public async Task<ServiceResult<TransactionDto>> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Lease)
                .ThenInclude(l => l!.Unit)
                    .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(t => t.Id == dto.TransactionId, cancellationToken);

        if (transaction == null)
        {
            return ServiceResult<TransactionDto>.Failure("Transaction not found.");
        }

        if (transaction.Status == TransactionStatus.Paid)
        {
            return ServiceResult<TransactionDto>.Failure("Transaction is already marked as Paid.");
        }

        var method = !string.IsNullOrWhiteSpace(dto.PaymentMethod) ? dto.PaymentMethod.Trim() : "Bank Transfer";
        if (!string.IsNullOrWhiteSpace(dto.CheckNumber))
        {
            method += $" #{dto.CheckNumber.Trim()}";
        }
        if (!string.IsNullOrWhiteSpace(dto.BankName))
        {
            method += $" ({dto.BankName.Trim()})";
        }

        transaction.Status = TransactionStatus.Paid;
        transaction.PaymentMethod = method;
        transaction.PaymentDate = dto.PaymentDate.HasValue
            ? DateTime.SpecifyKind(dto.PaymentDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var tenantName = "Unknown";
        if (transaction.Lease != null)
        {
            var tenant = await _userManager.FindByIdAsync(transaction.Lease.TenantId.ToString());
            if (tenant != null)
            {
                tenantName = $"{tenant.FirstName} {tenant.LastName}".Trim();
            }
        }

        var resultDto = new TransactionDto
        {
            Id = transaction.Id,
            LeaseId = transaction.LeaseId,
            UnitId = transaction.Lease?.UnitId ?? Guid.Empty,
            UnitNumber = transaction.Lease?.Unit?.UnitNumber ?? string.Empty,
            PropertyName = transaction.Lease?.Unit?.Property?.Name ?? string.Empty,
            TenantId = transaction.Lease?.TenantId ?? Guid.Empty,
            TenantName = tenantName,
            Amount = transaction.Amount,
            PaymentDate = transaction.PaymentDate,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status,
            CreatedAtUtc = transaction.CreatedAtUtc
        };

        return ServiceResult<TransactionDto>.Success(resultDto, "Payment recorded successfully.");
    }

    public async Task<ServiceResult<TransactionDto>> CreateDirectPaymentAsync(CreateDirectPaymentDto dto, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(l => l.Id == dto.LeaseId, cancellationToken);

        if (lease == null)
        {
            return ServiceResult<TransactionDto>.Failure("Referenced Lease not found.");
        }

        var method = !string.IsNullOrWhiteSpace(dto.PaymentMethod) ? dto.PaymentMethod.Trim() : "Check";
        if (!string.IsNullOrWhiteSpace(dto.CheckNumber))
        {
            method += $" #{dto.CheckNumber.Trim()}";
        }
        if (!string.IsNullOrWhiteSpace(dto.BankName))
        {
            method += $" ({dto.BankName.Trim()})";
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            LeaseId = lease.Id,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate.HasValue 
                ? DateTime.SpecifyKind(dto.PaymentDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow,
            PaymentMethod = method,
            Status = dto.IsCleared ? TransactionStatus.Paid : TransactionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(lease.TenantId.ToString());

        var resultDto = new TransactionDto
        {
            Id = transaction.Id,
            LeaseId = transaction.LeaseId,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
            PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
            TenantId = lease.TenantId,
            TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown",
            Amount = transaction.Amount,
            PaymentDate = transaction.PaymentDate,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status,
            CreatedAtUtc = transaction.CreatedAtUtc
        };

        var message = dto.IsCleared 
            ? "Payment recorded and marked as Paid (Money Received)." 
            : "Payment recorded as Pending clearance.";

        return ServiceResult<TransactionDto>.Success(resultDto, message);
    }

    public async Task<ServiceResult<TenantLedgerSummaryDto>> GetTenantLedgerSummaryAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .Include(l => l.Transactions)
            .FirstOrDefaultAsync(l => l.Id == leaseId, cancellationToken);

        if (lease == null)
        {
            return ServiceResult<TenantLedgerSummaryDto>.Failure("Lease not found.");
        }

        var tenant = await _userManager.FindByIdAsync(lease.TenantId.ToString());
        var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

        var totalBilled = lease.Transactions.Sum(t => t.Amount);
        var totalPaid = lease.Transactions.Where(t => t.Status == TransactionStatus.Paid).Sum(t => t.Amount);

        var transactionDtos = lease.Transactions
            .OrderByDescending(t => t.PaymentDate)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                LeaseId = t.LeaseId,
                UnitId = lease.UnitId,
                UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
                PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
                TenantId = lease.TenantId,
                TenantName = tenantName,
                Amount = t.Amount,
                PaymentDate = t.PaymentDate,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc
            }).ToList();

        var summary = new TenantLedgerSummaryDto
        {
            LeaseId = lease.Id,
            UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
            PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
            MonthlyRent = lease.MonthlyRent,
            TotalBilled = totalBilled,
            TotalPaid = totalPaid,
            Transactions = transactionDtos
        };

        return ServiceResult<TenantLedgerSummaryDto>.Success(summary);
    }
}
