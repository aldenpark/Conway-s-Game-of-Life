/*
Conway's Game of Life API

This is the entry point for the Game of Life API application.
It sets up the web host, configures services, and runs the application.
 
Usage:
dotnet run --project src/Life.Api/Life.Api.csproj
*/

using Life.Api.Infrastructure;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(5000)); // Bind to port 5000
builder.AddLifeServices();                  // Add custom services (Entity Framework (EF), Guardrails, Swagger, etc.)
builder.Services.AddControllers();          // Adds services for controllers ([ApiController] + [HttpGet]/[HttpPost]), enabling API endpoints
builder.Services.AddSwaggerGen(c =>         // Add Swagger generation to the builder
{
    // include this assembly's XML docs
    var asm = Assembly.GetExecutingAssembly();
    var xml = $"{asm.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);

    // Configure Swagger to use XML comments for API documentation
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Conway's Game of Life API",
        Version = "v1",
        Description = "API for Conway's Game of Life, supporting board creation, updates, and querying."
    });
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // if other projects (Domain/DTOs) with their own XML docs, include them too:
    foreach (var extraXml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
        c.IncludeXmlComments(extraXml, includeControllerXmlComments: true);
});
builder.Logging.ClearProviders();           // Clear default logging providers (e.g., Console, Debug)
builder.Logging.AddJsonConsole();           // Add JSON console logging for structured logs

var app = builder.Build();
app.UseLifeInfra();                         // middleware + swagger
// app.UseDefaultFiles();   // serves index.html by default
// app.UseStaticFiles();    // serves wwwroot/*
app.MapControllers();                       // Map API controllers
// app.MapFallbackToFile("/index.html"); // SPA routes -> index.html

app.Run();

public partial class Program { }  // This partial class is required for testing purposes.