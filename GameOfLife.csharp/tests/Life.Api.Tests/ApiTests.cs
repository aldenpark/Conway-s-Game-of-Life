using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Life.Api.Data;
using Life.Api.DTO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Life.Api.Tests;

/// <summary>
/// Tests for the Game of Life API endpoints.
/// This class contains integration tests for various API endpoints related to the Game of Life.
/// It includes tests for uploading boards, retrieving board states, and checking the final state of boards.
/// </summary>
/// <remarks>
/// The ApiEndpointTests class is designed to validate the API endpoints of the Game of Life application.
/// It includes tests for uploading boards with different configurations, retrieving the current state of boards,
/// and checking the final state of boards after a number of generations.
/// The tests cover scenarios such as uploading a random board, retrieving the final state of a board,
/// and ensuring that the API behaves correctly when interacting with the Game of Life domain logic.
/// The class uses FluentAssertions for expressive assertions and readability.
/// </remarks>
public sealed class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _conn;

    /// <summary>
    /// Initializes a new instance of the ApiEndpointTests class.
    /// This constructor sets up the in-memory SQLite database connection and configures the WebApplicationFactory
    /// to use the in-memory database for testing.
    /// </summary>
    public ApiEndpointTests(WebApplicationFactory<Program> baseFactory)
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

                // Ensure schema exists on THIS connection
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LifeDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    /// <summary>
    /// Disposes the SqliteConnection used for testing.
    /// This method is called to clean up resources after the tests are run.
    /// </summary>
    public void Dispose() => _conn.Dispose();

    // ---------- helpers ----------

    /// <summary>
    /// Helper method to create a 3x3 block pattern.
    /// This method returns a 2D array representing a 3x3 block pattern used in tests.
    /// </summary>
    private static int[][] Block3x3() => new[]
    {
        new[] {0,1,0},
        new[] {1,1,0},
        new[] {0,0,0}
    };

    /// <summary>
    /// Helper method to create a 3x3 blinker pattern in vertical orientation.
    /// This method returns a 2D array representing a vertical blinker pattern used in tests.
    /// </summary>
    private static int[][] Blinker3x3Vertical() => new[]
    {
        new[] {0,1,0},
        new[] {0,1,0},
        new[] {0,1,0}
    };

    /// <summary>
    /// Helper method to create a 3x3 blinker pattern in horizontal orientation.
    /// This method returns a 2D array representing a horizontal blinker pattern used in tests.
    /// </summary>
    private static int[][] Blinker3x3Horizontal() => new[]
    {
        new[] {0,0,0},
        new[] {1,1,1},
        new[] {0,0,0}
    };

    // ---------- root/health/docs ----------

    /// <summary>
    /// Tests that the root endpoint returns a 200 OK status code.
    /// This test verifies that the root endpoint of the API is accessible and returns a successful response.
    /// </summary>
    [Fact]
    public async Task Root_Returns_Ok()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests that the health endpoint returns a 200 OK status code.
    /// This test verifies that the health endpoint of the API is accessible and returns a successful response.
    /// </summary>
    [Fact]
    public async Task Health_Returns_Ok()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests that the docs endpoint redirects to the Swagger UI.
    /// This test verifies that the docs endpoint of the API redirects to the Swagger UI documentation.
    /// </summary>
    [Fact]
    public async Task Docs_Redirects_To_Swagger()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/docs");
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Be("/swagger");
    }

    // ---------- POST /boards ----------

    /// <summary>
    /// Tests that uploading a random board works correctly.
    /// This test creates a random board with specified dimensions and density, and checks that the upload is successful.
    /// </summary>
    [Fact]
    public async Task Upload_With_Grid_Works()
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = Block3x3(),
            Wrap = false
        });

        up.IsSuccessStatusCode.Should().BeTrue();
        (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that uploading a random board with specified dimensions and density works correctly.
    /// This test creates a random board with specified height, width, density, and wrap state,
    /// and checks that the upload is successful.
    /// </summary>
    [Fact]
    public async Task Upload_Missing_Both_Grid_And_Dimensions_Is_400()
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest());
        up.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that uploading a random board with specified dimensions and density works correctly.
    /// This test creates a random board with specified height, width, density, and wrap state,
    /// and checks that the upload is successful.
    /// </summary>
    [Fact]
    public async Task Upload_NonRectangular_Grid_Is_400()
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = new[] { new[] { 0, 1 }, new[] { 1 } }, // jagged row lengths
            Wrap = true
        });
        up.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- GET /boards/{id} ----------

    /// <summary>
    /// Tests that retrieving a board by ID returns a 404 Not Found status code if the board does not exist.
    /// This test verifies that the API correctly handles requests for non-existent boards.
    /// </summary>
    [Fact]
    public async Task Get_Unknown_Board_Is_404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/boards/does-not-exist");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Tests that retrieving a board by ID returns the correct board state.
    /// This test uploads a board and then retrieves it by its ID, checking that the returned grid matches the uploaded state.
    /// </summary>
    [Fact]
    public async Task Get_Returns_Uploaded_Grid_At_Generation_0()
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = Block3x3(),
            Wrap = false
        });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var resp = await client.GetAsync($"/boards/{id}");
        resp.IsSuccessStatusCode.Should().BeTrue();

        var payload = await resp.Content.ReadFromJsonAsync<GridResponse>();
        payload!.Generation.Should().Be(0);
        payload.Grid.Should().BeEquivalentTo(Block3x3(), o => o.WithStrictOrdering());
        payload.Meta.Should().BeNull();
    }

    // ---------- GET /boards/{id}/next ----------

    /// <summary>
    /// Tests that the next generation of a board is returned correctly.
    /// This test uploads a board and then retrieves the next generation, checking that the returned grid matches the expected state after applying the Game of Life rules.
    /// </summary>
    [Fact]
    public async Task Next_Increments_Generation_And_Applies_Rules()
    {
        var client = _factory.CreateClient();
        // blinker (vertical) -> next ==> horizontal
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = Blinker3x3Vertical(),
            Wrap = false
        });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var next = await client.GetAsync($"/boards/{id}/next");
        next.IsSuccessStatusCode.Should().BeTrue();

        var payload = await next.Content.ReadFromJsonAsync<GridResponse>();
        payload!.Generation.Should().Be(1);
        payload.Grid.Should().BeEquivalentTo(Blinker3x3Horizontal(), o => o.WithStrictOrdering());
    }

    // ---------- GET /boards/{id}/advance?n=... ----------

    /// <summary>
    /// Tests that advancing a board by a specified number of generations works correctly.
    /// This test uploads a board and then retrieves the state after advancing by 2 generations,
    /// checking that the returned grid matches the expected state after applying the Game of Life rules.
    /// </summary>
    [Fact]
    public async Task Advance_Increments_By_N()
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = Blinker3x3Vertical(),
            Wrap = false
        });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var adv = await client.GetAsync($"/boards/{id}/advance?n=2");
        adv.IsSuccessStatusCode.Should().BeTrue();

        var payload = await adv.Content.ReadFromJsonAsync<GridResponse>();
        payload!.Generation.Should().Be(2);
        // period-2 oscillator -> back to original
        payload.Grid.Should().BeEquivalentTo(Blinker3x3Vertical(), o => o.WithStrictOrdering());
    }

    /// <summary>
    /// Tests that advancing a board with an out-of-range n parameter returns a 400 Bad Request status code.
    /// This test verifies that the API correctly handles requests with invalid n values,
    /// such as negative values or values exceeding a predefined limit.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public async Task Advance_OutOfRange_n_Is_400(int n)
    {
        var client = _factory.CreateClient();
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest { Height = 5, Width = 5, Density = 0.2, Wrap = true });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var resp = await client.GetAsync($"/boards/{id}/advance?n={n}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- GET /boards/{id}/final?max_iters=... ----------

    /// <summary>
    /// Tests that the final state of a board is returned correctly for a fixed point.
    /// This test uploads a board that is a fixed point (2x2 block) and checks that the final state
    /// is recognized as fixed after a number of generations.
    /// </summary>
    [Fact]
    public async Task Final_Returns_Fixed_Meta_For_Block()
    {
        var client = _factory.CreateClient();
        // 2x2 block is a fixed point
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = new[]
            {
                new[] {1,1},
                new[] {1,1}
            },
            Wrap = false
        });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var resp = await client.GetAsync($"/boards/{id}/final?max_iters=50");
        resp.IsSuccessStatusCode.Should().BeTrue();

        var payload = await resp.Content.ReadFromJsonAsync<GridResponse>();
        payload!.Meta.Should().NotBeNull();
        payload.Meta!.ToString()!.Should().Contain("fixed");
    }

    /// <summary>
    /// Tests that the final state of a board is returned correctly for a cycle.
    /// This test uploads a board that is a cycle (3x3 blinker) and checks that the final state
    /// is recognized as a cycle with the correct period after a number of generations.
    /// </summary>
    [Fact]
    public async Task Final_422_If_No_Conclusion_Within_MaxIters()
    {
        var client = _factory.CreateClient();
        // blinker needs 2 steps to detect its cycle; force max_iters=1 to trigger 422
        var up = await client.PostAsJsonAsync("/boards", new UploadBoardRequest
        {
            Grid = Blinker3x3Vertical(),
            Wrap = false
        });
        var id = (await up.Content.ReadFromJsonAsync<BoardResponse>())!.BoardId;

        var resp = await client.GetAsync($"/boards/{id}/final?max_iters=1");
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
