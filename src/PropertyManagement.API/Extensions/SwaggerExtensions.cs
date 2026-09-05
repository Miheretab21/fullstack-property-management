using Microsoft.OpenApi;

namespace PropertyManagement.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Property Management API",
                Version = "v1",
                Description = "Property Management System Web API (ASP.NET Core .NET 10.0)"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT Bearer token only. Example: eyJhbGciOiJIUzI1NiIsInR5cCI6..."
            });

            options.AddSecurityRequirement(doc =>
            {
                var requirement = new OpenApiSecurityRequirement();
                var schemeRef = new OpenApiSecuritySchemeReference("Bearer", doc, null);
                requirement.Add(schemeRef, new List<string>());
                return requirement;
            });
        });

        return services;
    }
}
