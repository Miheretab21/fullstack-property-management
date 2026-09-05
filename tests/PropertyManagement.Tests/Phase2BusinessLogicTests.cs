using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using PropertyManagement.Application.Models.Financial;
using PropertyManagement.Application.Models.Leases;
using PropertyManagement.Application.Models.Maintenance;
using PropertyManagement.Application.Models.Properties;
using PropertyManagement.Application.Models.Units;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Services;
using Xunit;

namespace PropertyManagement.Tests;

public class Phase2BusinessLogicTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private UserManager<ApplicationUser> CreateMockUserManager()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new ApplicationUser
            {
                Id = Guid.Parse(id),
                Email = "tenant@test.com",
                FirstName = "Test",
                LastName = "Tenant"
            });

        return userManagerMock.Object;
    }

    private async Task<(Property Property, Unit Unit)> SeedPropertyAndUnitAsync(ApplicationDbContext context, string unitNumber = "101")
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = "Grand Central",
            Address = "100 Main St",
            City = "Metropolis",
            OwnerId = Guid.NewGuid()
        };
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            UnitNumber = unitNumber,
            Bedrooms = 2,
            Bathrooms = 1,
            RentAmount = 1500,
            Status = UnitStatus.Vacant,
            Property = property
        };
        context.Properties.Add(property);
        context.Units.Add(unit);
        await context.SaveChangesAsync();
        return (property, unit);
    }

    [Fact]
    public async Task PropertyService_CreateAndGet_CalculatesUnitsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var propertyService = new PropertyService(context);
        var unitService = new UnitService(context);

        // Act - Create Property
        var propResult = await propertyService.CreatePropertyAsync(new CreatePropertyDto
        {
            Name = "Sunset Towers",
            Address = "123 Ocean Blvd",
            City = "Miami"
        }, Guid.NewGuid());

        Assert.True(propResult.Succeeded, propResult.Message);
        var propertyId = propResult.Data!.Id;

        // Add 2 units: 1 Vacant, 1 Occupied
        await unitService.CreateUnitAsync(new CreateUnitDto
        {
            PropertyId = propertyId,
            UnitNumber = "101",
            Bedrooms = 2,
            Bathrooms = 1,
            RentAmount = 1500,
            Status = UnitStatus.Vacant
        });

        await unitService.CreateUnitAsync(new CreateUnitDto
        {
            PropertyId = propertyId,
            UnitNumber = "102",
            Bedrooms = 1,
            Bathrooms = 1,
            RentAmount = 1200,
            Status = UnitStatus.Occupied
        });

        var detail = await propertyService.GetPropertyByIdAsync(propertyId);

        // Assert
        Assert.True(detail.Succeeded, detail.Message);
        Assert.Equal(2, detail.Data!.TotalUnits);
        Assert.Equal(1, detail.Data.OccupiedUnits);
        Assert.Equal(1, detail.Data.VacantUnits);
    }

    [Fact]
    public async Task UnitService_DuplicateUnitNumberInSameProperty_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var unitService = new UnitService(context);
        var propId = Guid.NewGuid();
        context.Properties.Add(new Property { Id = propId, Name = "Test Prop" });
        await context.SaveChangesAsync();

        // Act
        var first = await unitService.CreateUnitAsync(new CreateUnitDto
        {
            PropertyId = propId,
            UnitNumber = "Unit-A",
            RentAmount = 1000
        });

        var duplicate = await unitService.CreateUnitAsync(new CreateUnitDto
        {
            PropertyId = propId,
            UnitNumber = "unit-a", // case insensitive duplicate
            RentAmount = 1100
        });

        // Assert
        Assert.True(first.Succeeded, first.Message);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already exists", duplicate.Message);
    }

    [Fact]
    public async Task LeaseService_CreateLease_SetsUnitStatusToOccupiedAndCreatesPendingCharge()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var leaseService = new LeaseService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "202");
        var tenantId = Guid.NewGuid();

        // Act
        var result = await leaseService.CreateLeaseAsync(new CreateLeaseDto
        {
            UnitId = unit.Id,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 2000,
            SecurityDeposit = 2000
        });

        // Assert
        Assert.True(result.Succeeded, result.Message);
        var updatedUnit = await context.Units.FindAsync(unit.Id);
        Assert.Equal(UnitStatus.Occupied, updatedUnit!.Status);

        var transactions = await context.Transactions.Where(t => t.LeaseId == result.Data!.Id).ToListAsync();
        Assert.Single(transactions);
        Assert.Equal(TransactionStatus.Pending, transactions[0].Status);
        Assert.Equal(2000, transactions[0].Amount);
    }

    [Fact]
    public async Task LeaseService_DoubleLeasing_OverlappingDatesRejected()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var leaseService = new LeaseService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "303");
        var tenantId = Guid.NewGuid();

        // First Lease: Jan 1, 2026 to Dec 31, 2026
        var first = await leaseService.CreateLeaseAsync(new CreateLeaseDto
        {
            UnitId = unit.Id,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 1800
        });

        Assert.True(first.Succeeded, first.Message);

        // Attempt Second Lease overlapping: June 1, 2026 to May 31, 2027
        var overlapping = await leaseService.CreateLeaseAsync(new CreateLeaseDto
        {
            UnitId = unit.Id,
            TenantId = Guid.NewGuid(),
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2027, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 1850
        });

        // Assert
        Assert.False(overlapping.Succeeded);
        Assert.Contains("Double-lease conflict", overlapping.Message);
    }

    [Fact]
    public async Task LeaseService_TerminateLease_SetsUnitStatusBackToVacant()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var leaseService = new LeaseService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "404");
        var tenantId = Guid.NewGuid();

        var leaseResult = await leaseService.CreateLeaseAsync(new CreateLeaseDto
        {
            UnitId = unit.Id,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 1500
        });

        Assert.True(leaseResult.Succeeded, leaseResult.Message);

        // Act
        var termResult = await leaseService.TerminateLeaseAsync(leaseResult.Data!.Id);

        // Assert
        Assert.True(termResult.Succeeded, termResult.Message);
        var updatedUnit = await context.Units.FindAsync(unit.Id);
        Assert.Equal(UnitStatus.Vacant, updatedUnit!.Status);
    }

    [Fact]
    public async Task LeaseService_DeleteLease_RemovesLeaseAndResetsUnitStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var leaseService = new LeaseService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "405");
        var tenantId = Guid.NewGuid();

        var leaseResult = await leaseService.CreateLeaseAsync(new CreateLeaseDto
        {
            UnitId = unit.Id,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            MonthlyRent = 1800
        });

        Assert.True(leaseResult.Succeeded, leaseResult.Message);
        var leaseId = leaseResult.Data!.Id;

        // Verify Unit is Occupied
        var occupiedUnit = await context.Units.FindAsync(unit.Id);
        Assert.Equal(UnitStatus.Occupied, occupiedUnit!.Status);

        // Act - Delete lease
        var deleteResult = await leaseService.DeleteLeaseAsync(leaseId);

        // Assert
        Assert.True(deleteResult.Succeeded, deleteResult.Message);
        var deletedLease = await context.Leases.FindAsync(leaseId);
        Assert.Null(deletedLease);

        var resetUnit = await context.Units.FindAsync(unit.Id);
        Assert.Equal(UnitStatus.Vacant, resetUnit!.Status);
    }

    [Fact]
    public async Task FinancialService_RecordPaymentAndGetLedger_CalculatesBalanceCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var financialService = new FinancialService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "501");
        var leaseId = Guid.NewGuid();
        var lease = new Lease
        {
            Id = leaseId,
            UnitId = unit.Id,
            TenantId = Guid.NewGuid(),
            MonthlyRent = 1000,
            IsActive = true,
            Unit = unit
        };
        context.Leases.Add(lease);

        var tx1 = new Transaction
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            Amount = 1000,
            Status = TransactionStatus.Pending,
            PaymentDate = DateTime.UtcNow,
            Lease = lease
        };
        var tx2 = new Transaction
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            Amount = 1000,
            Status = TransactionStatus.Pending,
            PaymentDate = DateTime.UtcNow.AddMonths(1),
            Lease = lease
        };
        context.Transactions.AddRange(tx1, tx2);
        await context.SaveChangesAsync();

        // Act - Pay tx1
        var payResult = await financialService.RecordPaymentAsync(new RecordPaymentDto
        {
            TransactionId = tx1.Id,
            PaymentMethod = "Credit Card"
        });

        Assert.True(payResult.Succeeded, payResult.Message);
        Assert.Equal(TransactionStatus.Paid, payResult.Data!.Status);

        // Fetch Ledger
        var ledgerResult = await financialService.GetTenantLedgerSummaryAsync(leaseId);

        // Assert
        Assert.True(ledgerResult.Succeeded, ledgerResult.Message);
        Assert.Equal(2000, ledgerResult.Data!.TotalBilled);
        Assert.Equal(1000, ledgerResult.Data.TotalPaid);
        Assert.Equal(1000, ledgerResult.Data.OutstandingBalance);
    }

    [Fact]
    public async Task MaintenanceService_LifecycleFlow_ClosesTicketWithResolutionNotes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var maintenanceService = new MaintenanceService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "505");
        var tenantId = Guid.NewGuid();

        // 1. Submit Request
        var createResult = await maintenanceService.CreateRequestAsync(new CreateMaintenanceRequestDto
        {
            UnitId = unit.Id,
            Title = "Leaking Kitchen Sink",
            Description = "Water dripping under sink",
            Priority = MaintenancePriority.High
        }, tenantId);

        Assert.True(createResult.Succeeded, createResult.Message);
        Assert.Equal(MaintenanceStatus.Open, createResult.Data!.Status);

        // 2. Manager Updates to InProgress
        var inProgressResult = await maintenanceService.UpdateRequestStatusAsync(createResult.Data.Id, new UpdateMaintenanceStatusDto
        {
            Status = MaintenanceStatus.InProgress,
            AssignedTechnician = "Plumber Dave"
        });

        Assert.True(inProgressResult.Succeeded, inProgressResult.Message);
        Assert.Equal(MaintenanceStatus.InProgress, inProgressResult.Data!.Status);
        Assert.Equal("Plumber Dave", inProgressResult.Data.AssignedTechnician);

        // 3. Manager Closes ticket with resolution notes
        var closeResult = await maintenanceService.UpdateRequestStatusAsync(createResult.Data.Id, new UpdateMaintenanceStatusDto
        {
            Status = MaintenanceStatus.Closed,
            ResolutionNotes = "Replaced P-trap and tightened valves. Tested leak free."
        });

        Assert.True(closeResult.Succeeded, closeResult.Message);
        Assert.Equal(MaintenanceStatus.Closed, closeResult.Data!.Status);
        Assert.NotNull(closeResult.Data.ResolvedAtUtc);
        Assert.Contains("Replaced P-trap", closeResult.Data.ResolutionNotes);
    }

    [Fact]
    public async Task MaintenanceService_DeleteRequest_RemovesRequestSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var maintenanceService = new MaintenanceService(context, userManager);

        var (_, unit) = await SeedPropertyAndUnitAsync(context, "506");
        var tenantId = Guid.NewGuid();

        var createResult = await maintenanceService.CreateRequestAsync(new CreateMaintenanceRequestDto
        {
            UnitId = unit.Id,
            Title = "Broken Window Latch",
            Description = "Window does not lock properly",
            Priority = MaintenancePriority.Medium
        }, tenantId);

        Assert.True(createResult.Succeeded);
        var requestId = createResult.Data!.Id;

        // Act
        var deleteResult = await maintenanceService.DeleteRequestAsync(requestId);

        // Assert
        Assert.True(deleteResult.Succeeded, deleteResult.Message);
        var deleted = await context.MaintenanceRequests.FindAsync(requestId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DashboardService_CalculatesOccupancyAndRevenueAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var userManager = CreateMockUserManager();
        var dashboardService = new DashboardService(context, userManager);

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = "Skyline View",
            Address = "500 High St",
            City = "Seattle"
        };
        context.Properties.Add(property);

        var unit1 = new Unit { Id = Guid.NewGuid(), PropertyId = property.Id, UnitNumber = "A1", Status = UnitStatus.Occupied };
        var unit2 = new Unit { Id = Guid.NewGuid(), PropertyId = property.Id, UnitNumber = "A2", Status = UnitStatus.Vacant };
        context.Units.AddRange(unit1, unit2);

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            TenantId = Guid.NewGuid(),
            MonthlyRent = 2500,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow.AddDays(15), // Expiring in 15 days
            Unit = unit1
        };
        context.Leases.Add(lease);

        var txPaid = new Transaction
        {
            Id = Guid.NewGuid(),
            LeaseId = lease.Id,
            Amount = 2500,
            Status = TransactionStatus.Paid,
            PaymentDate = DateTime.UtcNow
        };
        context.Transactions.Add(txPaid);

        context.MaintenanceRequests.Add(new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = unit1.Id,
            TenantId = lease.TenantId,
            Title = "Broken Lock",
            Status = MaintenanceStatus.Open
        });

        await context.SaveChangesAsync();

        // Act
        var result = await dashboardService.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(1, result.Data!.TotalProperties);
        Assert.Equal(2, result.Data.TotalUnits);
        Assert.Equal(1, result.Data.OccupiedUnits);
        Assert.Equal(1, result.Data.VacantUnits);
        Assert.Equal(50.0m, result.Data.OccupancyRate);
        Assert.Equal(2500m, result.Data.TotalMonthlyProjectedRevenue);
        Assert.Equal(2500m, result.Data.CurrentMonthCollectedRevenue);
        Assert.Equal(1, result.Data.ActiveMaintenanceRequests);
        Assert.Equal(1, result.Data.ExpiringLeasesCount);
        Assert.Equal("A1", result.Data.ExpiringLeases[0].UnitNumber);
    }

    [Fact]
    public async Task PropertyAndUnitService_PreservesSubCityAndBuildingSpecs()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var propertyService = new PropertyService(context);
        var unitService = new UnitService(context);

        // Act - Create Building with SubCity
        var propResult = await propertyService.CreatePropertyAsync(new CreatePropertyDto
        {
            Name = "Noah Bole Tower",
            Address = "Woreda 03, Near Edna Mall",
            SubCity = "Bole",
            City = "Addis Ababa",
            TotalFloors = 8,
            ConstructionStatus = "Completed"
        }, Guid.NewGuid());

        Assert.True(propResult.Succeeded, propResult.Message);
        Assert.Equal("Bole", propResult.Data!.SubCity);
        Assert.Equal(8, propResult.Data.TotalFloors);

        // Act - Create Unit in that Building with Price, Area, Floor
        var unitResult = await unitService.CreateUnitAsync(new CreateUnitDto
        {
            PropertyId = propResult.Data.Id,
            UnitNumber = "402",
            FloorNumber = 4,
            SquareMeters = 95.5m,
            Bedrooms = 2,
            Bathrooms = 2,
            RentAmount = 28000m,
            Status = UnitStatus.Vacant,
            FinishingNotes = "Pre-lease painting completed"
        });

        Assert.True(unitResult.Succeeded, unitResult.Message);
        Assert.Equal("Noah Bole Tower", unitResult.Data!.PropertyName);
        Assert.Equal("Bole", unitResult.Data.SubCity);
        Assert.Equal(4, unitResult.Data.FloorNumber);
        Assert.Equal(95.5m, unitResult.Data.SquareMeters);
        Assert.Equal(28000m, unitResult.Data.RentAmount);

        // Verify GetUnitsAsync returns building info and subcity
        var allUnits = await unitService.GetUnitsAsync();
        Assert.True(allUnits.Succeeded);
        var foundUnit = allUnits.Data!.FirstOrDefault(u => u.UnitNumber == "402");
        Assert.NotNull(foundUnit);
        Assert.Equal("Noah Bole Tower", foundUnit.PropertyName);
        Assert.Equal("Bole", foundUnit.SubCity);
        Assert.Equal(4, foundUnit.FloorNumber);
        Assert.Equal(95.5m, foundUnit.SquareMeters);
    }
}
