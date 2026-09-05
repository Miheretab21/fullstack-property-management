using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        ApplicationDbContext? dbContext = null)
    {
        try
        {
            // Seed Roles
            var roles = Enum.GetNames<UserRole>();
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                    logger.LogInformation("Seeded role: {Role}", roleName);
                }
            }

            // Seed Default Admin User
            const string adminEmail = "admin@propertymgt.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    PhoneNumber = "+1234567890",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                    logger.LogInformation("Seeded default Admin user ({Email})", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create default Admin user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed Default Property Manager User
            const string managerEmail = "manager@propertymgt.com";
            var existingManager = await userManager.FindByEmailAsync(managerEmail);

            if (existingManager == null)
            {
                var managerUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = managerEmail,
                    Email = managerEmail,
                    FirstName = "Abebe",
                    LastName = "Bekele",
                    PhoneNumber = "0911223344",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(managerUser, "Manager@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, UserRole.PropertyManager.ToString());
                    logger.LogInformation("Seeded default Property Manager user ({Email})", managerEmail);
                }
                else
                {
                    logger.LogError("Failed to create default Property Manager user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed Default Tenant User
            const string tenantEmail = "tenant@propertymgt.com";
            var existingTenant = await userManager.FindByEmailAsync(tenantEmail);

            if (existingTenant == null)
            {
                var tenantUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = tenantEmail,
                    Email = tenantEmail,
                    FirstName = "Sara",
                    LastName = "Hailu",
                    PhoneNumber = "0922334455",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(tenantUser, "Tenant@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(tenantUser, UserRole.Tenant.ToString());
                    logger.LogInformation("Seeded default Tenant user ({Email})", tenantEmail);
                }
                else
                {
                    logger.LogError("Failed to create default Tenant user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed/Assign demo manager to demo properties if unassigned
            if (dbContext != null)
            {
                var manager = await userManager.FindByEmailAsync(managerEmail);
                if (manager != null)
                {
                    var unassignedProperties = await dbContext.Properties
                        .Where(p => p.AssignedManagerId == null)
                        .ToListAsync();

                    if (unassignedProperties.Any())
                    {
                        foreach (var prop in unassignedProperties)
                        {
                            prop.AssignedManagerId = manager.Id;
                        }
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Assigned {Count} demo properties to manager {Email}", unassignedProperties.Count, managerEmail);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
