using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Extensions;
using PropertyManagement.API.Middlewares;
using PropertyManagement.Infrastructure;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Seeding;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Infrastructure Services (EF Core, Identity, JWT, DbContext)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Controllers & JSON serialization (serialize enums as strings)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 3. Swagger & OpenAPI with JWT Support
builder.Services.AddOpenApi();
builder.Services.AddSwaggerWithJwt();

// 4. CORS Policy for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDevClient", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 5. Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 6. Interactive API Documentation (Swagger & Scalar)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Property Management API Reference")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Property Management API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// 7. CORS
app.UseCors("AllowAngularDevClient");

// 8. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 9. Map API Controllers
app.MapControllers();

// 10. Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        logger.LogInformation("Seeding database roles and initial admin user...");
        await DatabaseSeeder.SeedDatabaseAsync(userManager, roleManager, logger, context);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply database migrations or seed automatically. Verify PostgreSQL connection in appsettings.json.");
    }
}

app.Run();
