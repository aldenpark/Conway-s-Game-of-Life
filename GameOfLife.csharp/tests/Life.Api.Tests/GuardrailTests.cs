using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Life.Api.Data;
using Life.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

using Life.Api.Infrastructure;

namespace Life.Api.Tests;

/// <summary>
/// Integration tests for the Game of Life API.
/// This class uses a WebApplicationFactory to create an in-memory test server.
/// It includes tests for uploading boards and retrieving final board states.
/// The tests cover scenarios such as uploading a random board and retrieving the final state of a board.
/// The class implements IDisposable to clean up resources after tests are run.
/// </summary>
/// <remarks>
/// The ApiTests class is designed to validate the API endpoints of the Game of Life application.
/// It includes tests for uploading boards, retrieving board states, and checking the final state of boards.
/// The tests use FluentAssertions for expressive assertions and readability.
/// The class also sets up an in-memory SQLite database for testing purposes.
/// It ensures that the API behaves correctly under various scenarios, including edge cases.
/// The class implements IDisposable to clean up resources after tests are run.
/// </remarks>
public sealed class GuardrailTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _conn;

    /// <summary>
    /// Initializes a new instance of the GuardrailTests class.
    /// This constructor sets up the in-memory SQLite database connection and configures the WebApplicationFactory
    /// to use the in-memory database for testing.
    /// </summary>
    /// <param name="baseFactory">The base WebApplicationFactory to be used for testing.</param>
    public GuardrailTests(WebApplicationFactory<Program> baseFactory)
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        _factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext to use our shared in-memory connection
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LifeDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<LifeDbContext>(opts => opts.UseSqlite(_conn));

                // Replace IGuardrails with a strict test version
                var gr = services.SingleOrDefault(d => d.ServiceType.Name == "IGuardrails");
                if (gr is not null) services.Remove(gr);
                services.AddSingleton(typeof(IGuardrails), new TestGuardrails(maxDim: 5, maxCells: 16));

                // Ensure schema exists
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LifeDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    /// <summary>
    /// Disposes of the resources used by the GuardrailTests class.
    /// This method is called to clean up resources after the tests are run.
    /// It closes the SQLite connection to release any resources held by the in-memory database.
    /// </summary>
    public void Dispose() => _conn.Dispose();

    /// <summary>
    /// Tests that the guardrails enforce the maximum dimensions and cell counts.
    /// This test verifies that the API correctly rejects requests that exceed the defined limits for board dimensions
    /// and total cell counts.
    /// </summary>
    private sealed class TestGuardrails : IGuardrails
    {
        public int MaxDim { get; }
        public int MaxCells { get; }
        public string BoundsMsg => $"Max {MaxDim} per side, {MaxCells} cells total";
        public TestGuardrails(int maxDim, int maxCells) { MaxDim = maxDim; MaxCells = maxCells; }
        public bool Allowed(int h, int w) => h > 0 && w > 0 && h <= MaxDim && w <= MaxDim && (h * w) <= MaxCells;
    }

    /// <summary>
    /// Tests that uploading a random board with dimensions exceeding the guardrails is rejected with a 400 Bad Request status code.
    /// This test verifies that the API correctly enforces the maximum dimensions and cell counts defined by the guardrails.
    /// </summary>
    [Fact]
    public async Task Random_Too_Big_Rejected_400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Height = 10, // exceeds TestGuardrails maxDim=5
            Width = 10,
            Density = 0.2,
            Wrap = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that uploading a grid with dimensions exceeding the guardrails is rejected with a 400 Bad Request status code.
    /// This test verifies that the API correctly enforces the maximum dimensions and cell counts defined by the guardrails.
    /// </summary>
    [Fact]
    public async Task Grid_Too_Big_Rejected_400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = new[]
            {
                new[] {1,1,1,1,1,1}, // width 6 > maxDim 5
                new[] {1,1,1,1,1,1}
            },
            Wrap = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
