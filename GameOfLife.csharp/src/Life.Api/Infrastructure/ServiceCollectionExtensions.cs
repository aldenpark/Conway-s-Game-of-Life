using Life.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Life.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring the application builder.
/// This includes middleware for request logging, Swagger UI, and static file serving.
/// It also sets up the HTTP request pipeline for the Game of Life API.
/// </summary>
/// <remarks>
/// Uses <see cref="AddLifeServices"/> to configure the application middleware.
/// This class provides methods to configure the application pipeline, including middleware for logging requests,
/// serving static files, and setting up Swagger UI for API documentation.
/// It also includes a method to register the application's services.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required for the Game of Life API.
    /// This method sets up the database context, repositories, and other services needed by the application.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <remarks>
    /// This method is used to register the application's services, including the database context,
    /// repositories, and any other services required by the Game of Life API.
    /// It ensures that the database is created and ready for use when the application starts.
    /// </remarks>
    /// <example lang="csharp">
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddLifeServices();
    /// </example>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddLifeServices(this WebApplicationBuilder builder)
    {
        var svcs = builder.Services;

        // config / guardrails
        // Use environment variables for configuration if available, otherwise use defaults
        var dbPath = Environment.GetEnvironmentVariable("LIFE_DB_PATH") ?? "life.db";
        var maxDim = int.TryParse(Environment.GetEnvironmentVariable("LIFE_MAX_DIM"), out var md) ? md : 1000;
        var maxCells = int.TryParse(Environment.GetEnvironmentVariable("LIFE_MAX_CELLS"), out var mc) ? mc : 1_000_000;

        // Register guardrails with the configured maximum dimensions and cell counts
        svcs.AddSingleton<IGuardrails>(new Guardrails(maxDim, maxCells));

        // EF Core (SQLite)
        svcs.AddDbContext<LifeDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        svcs.AddScoped<BoardRepository>();

        // OpenAPI
        svcs.AddEndpointsApiExplorer();
        svcs.AddSwaggerGen();

        // Ensure DB exists on startup
        using var scope = svcs.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeDbContext>();
        db.Database.EnsureCreated();

        return svcs;
    }
}
