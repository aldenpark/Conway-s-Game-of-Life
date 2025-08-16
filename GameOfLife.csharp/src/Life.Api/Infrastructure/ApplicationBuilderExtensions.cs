using Life.Api.Data;
using Life.Api.Domain;
using Life.Api.DTO;
using Microsoft.AspNetCore.Builder;             // IApplicationBuilder
using Microsoft.AspNetCore.Routing;             // EndpointDataSource, RouteEndpoint, HttpMethodMetadata
using Microsoft.Extensions.Hosting;             // IHostEnvironment
using Microsoft.Extensions.DependencyInjection; // IServiceCollection
using System;                                   // Environment, AppContext
using System.Linq;                              // Enumerable, OfType, SelectMany, DefaultIfEmpty
using System.Diagnostics.CodeAnalysis;          // ExcludeFromCodeCoverage

namespace Life.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring the application builder.
/// This includes middleware for request logging, Swagger UI, and static file serving.
/// It also sets up the HTTP request pipeline for the Game of Life API.
/// </summary>
/// <remarks>
/// Uses <see cref="UseLifeInfra"/> to configure the application middleware.
/// This class provides methods to configure the application pipeline, including middleware for logging requests,
/// serving static files, and setting up Swagger UI for API documentation.
/// It also includes a method to register the application's services.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the application middleware and services.
    /// This method sets up the request pipeline, including middleware for logging, Swagger UI,
    /// and static file serving.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The configured application builder.</returns>
    /// <remarks>
    /// This method is used to configure the application's middleware and services.
    /// It sets up the request logging middleware, Swagger UI for API documentation,
    /// and static file serving for the Game of Life API.
    /// It also includes a method to register the application's services.
    /// </remarks>
    /// <example lang="csharp">
    /// app.UseLifeInfra();
    /// </example>
    public static IApplicationBuilder UseLifeInfra(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var epSrc = app.ApplicationServices.GetRequiredService<EndpointDataSource>();

        app.UseMiddleware<RequestLoggingMiddleware>();  // Log incoming requests
        app.UseSwagger();                               // Enable Swagger UI for API documentation
        app.UseSwaggerUI(c =>                           // Configure Swagger UI
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Conway's Game of Life API v1");
            c.RoutePrefix = "docs";                     // Set the route prefix for Swagger UI
        });

        /* ===== DEV DIAGNOSTICS ===== */
        // Log all routes in the console for debugging
        if (env.IsDevelopment())
        // if (app.Environment.IsDevelopment())
        {
            lifetime.ApplicationStarted.Register(() =>
            // app.Lifetime.ApplicationStarted.Register(() =>
            {
                // var epSrc = app.Services.GetRequiredService<EndpointDataSource>();
                foreach (var ep in epSrc.Endpoints.OfType<RouteEndpoint>())
                {
                    var methods = ep.Metadata.OfType<HttpMethodMetadata>()
                        .SelectMany(m => m.HttpMethods)
                        .DefaultIfEmpty("ANY");
                    Console.WriteLine($"[Route] {string.Join(',', methods),-6} {ep.RoutePattern.RawText}");
                }
            });
        }

        return app;
    }
}