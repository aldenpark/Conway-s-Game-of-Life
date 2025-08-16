using Life.Api.Data;
using Life.Api.Domain;
using Life.Api.DTO;
using Life.Api.Infrastructure; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Annotations;

namespace Life.Api.Controllers;


/// <summary>
/// Provides endpoints for creating, retrieving, and manipulating game boards.
/// </summary>
/// <remarks>
/// Uses <see cref="BoardRepository"/> for persistence and <see cref="GameOfLife"/> for evolution.
/// </remarks> 
/// <example>
/// Example usage:
/// - Create a new board: POST /boards with grid or height/width parameters.
/// - Retrieve a board: GET /boards/{boardId}
/// - Advance to the next generation: GET /boards/{boardId}/next
/// - Advance multiple generations: GET /boards/{boardId}/advance?n={n}
/// - Get final state after max iterations: GET /boards/{boardId}/final?max_iters={max_iters}
/// </example>
[ApiController]
[Route("")]
public class BoardsController : ControllerBase
{

    /// <summary>Root endpoint to check API status.</summary>
    /// <returns>Returns a simple status message indicating the API is running.</returns>
    /// <remarks>This endpoint is used to verify that the API is operational.</remarks>
    /// <example>
    /// <code lang="json">
    /// { "status": "ok", "message": "Conway API is running. See /docs." }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces("application/json")]
    [HttpGet("")]
    public IActionResult Root() =>
        Ok(new { status = "ok", message = "Conway API is running. See /docs." });

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    /// <returns>Returns a simple JSON object indicating the API is healthy.</returns>
    /// <remarks>
    /// This endpoint is typically used by load balancers or monitoring systems to check the health of the API.
    /// It does not perform any complex operations and is designed to be fast and reliable.
    /// It returns a JSON object with a single property "ok" set to true.
    /// </remarks>
    /// <example>
    /// <code lang="json">
    /// { "ok": true }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces("application/json")]
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true });

    /// <summary>
    /// Creates a new game board.
    /// Accepts either a grid or height/width parameters to initialize the board.
    /// </summary>
    /// <param name="req">Request object containing grid or dimensions</param>
    /// <param name="repo">Repository to access board data</param>
    /// <param name="g">Guardrails for validating board dimensions and cell limits</param>
    /// <returns>Returns the ID of the created board.</returns>
    /// <remarks>
    /// Provide either a rectangular <c>grid</c> of 0/1 values, or <c>height</c>/<c>width</c> to generate randomly.
    /// </remarks>
    /// <example>
    /// Example request body:
    /// { "grid": [[0, 1], [1, 0]], "height": null, "width": null, "wrap": true, "density": 0.5, "seed": 12345 }
    /// <code lang="json">
    /// { "id": "a1b2c3d4e5f6" }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("boards")]
    public async Task<ActionResult<BoardResponse>> Create(
        [FromBody] UploadBoardRequest req,
        [FromServices] BoardRepository repo,
        [FromServices] IGuardrails g)
    {
        GameOfLife life;
        if (req.Grid is null)
        {
            if (req.Height is null || req.Width is null) return BadRequest(new { error = "Provide grid OR height+width." });
            if (!g.Allowed(req.Height.Value, req.Width.Value)) return BadRequest(new { error = g.BoundsMsg });
            life = GameOfLife.FromRandom(req.Height.Value, req.Width.Value, req.Density, req.Seed, new LifeConfig(req.Wrap));
        }
        else
        {
            if (!req.IsValidGrid(out var h, out var w, out var err)) return BadRequest(new { error = err });
            if (!g.Allowed(h, w)) return BadRequest(new { error = g.BoundsMsg });
            life = GameOfLife.FromList(req.Grid, new LifeConfig(req.Wrap));
        }

        var id = Guid.NewGuid().ToString("N");
        await repo.SaveAsync(id, life.Grid, life.Height, life.Width, life.Config.Wrap, generation: 0);
        return Ok(new BoardResponse(id));
    }

    /// <summary>
    /// Retrieves a game board by its ID.
    /// If the board exists, returns its current state including grid, generation, and metadata.
    /// If the board does not exist, returns a 404 error.
    /// </summary>
    /// <param name="boardId">Unique identifier for the board</param>
    /// <param name="repo">Repository to access board data</param>
    /// <returns>Returns the current state of the board.</returns>
    /// <remarks>
    /// This endpoint allows clients to retrieve the current state of a game board by its unique ID.
    /// The response includes the board's grid, generation number, and any additional metadata.
    /// If the board does not exist, a 404 Not Found error is returned.
    /// </remarks>
    /// <example>
    /// <code lang="json">
    /// {
    ///   "id": "a1b2c3d4e5f6",
    ///   "generation": 0,
    ///   "grid": [[0, 1], [1, 0]],
    ///   "meta": null
    /// }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("boards/{boardId}")]
    public async Task<IActionResult> Get(string boardId, [FromServices] BoardRepository repo)
    {
        var b = await repo.GetAsync(boardId);
        return b is null ? NotFound(new { error = "Board not found" })
                         : Ok(new GridResponse(boardId, b.Generation, b.GridToJagged(), null));
    }

    /// <summary>
    /// Advances the game board to the next generation.
    /// </summary>
    /// <param name="boardId">Unique identifier for the board</param>
    /// <param name="repo">Repository to access board data</param>
    /// <returns>Returns the updated state of the board after advancing to the next generation.</returns>
    /// <remarks>
    /// This endpoint allows clients to advance the game board to the next generation.
    /// It retrieves the current state of the board, applies the Game of Life rules to compute the next generation,
    /// and saves the updated state back to the database.
    /// If the board does not exist, a 404 Not Found error is returned.
    /// </remarks>
    /// <example>
    /// <code lang="json">
    /// {
    ///   "id": "a1b2c3d4e5f6",
    ///   "generation": 1,
    ///   "grid": [[0, 1], [1, 0]],
    ///   "meta": null
    /// }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("boards/{boardId}/next")]
    public async Task<IActionResult> Next(string boardId, [FromServices] BoardRepository repo)
    {
        var b = await repo.GetAsync(boardId);
        if (b is null) return NotFound(new { error = "Board not found" });
        var life = new GameOfLife(b.Grid, new LifeConfig(b.Wrap));
        life.Step();
        var gen = b.Generation + 1;
        await repo.SaveAsync(boardId, life.Grid, b.Height, b.Width, b.Wrap, gen);
        return Ok(new GridResponse(boardId, gen, life.ToJagged(), null));
    }

    /// <summary>
    /// Advances the game board by a specified number of generations.
    /// </summary>
    /// <param name="boardId">Unique identifier for the board</param>
    /// <param name="n">Number of generations to advance</param>
    /// <param name="repo">Repository to access board data</param>
    /// <returns>Returns the updated state of the board after advancing by n generations.</returns>
    /// <remarks>
    /// This endpoint allows clients to advance the game board by a specified number of generations.
    /// It retrieves the current state of the board, applies the Game of Life rules to compute the next n generations,
    /// and saves the updated state back to the database.
    /// If the board does not exist, a 404 Not Found error is returned.
    /// If n is less than 0 or greater than 1,000,000, a 400 Bad Request error is returned.
    /// </remarks>
    /// <example>
    /// <code lang="json">
    /// {
    ///   "id": "a1b2c3d4e5f6",
    ///   "generation": 5,
    ///   "grid": [[0, 1], [1, 0]],
    ///   "meta": null
    /// }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("boards/{boardId}/advance")]
    public async Task<IActionResult> Advance(string boardId, int n, [FromServices] BoardRepository repo)
    {
        if (n < 0 || n > 1_000_000) return BadRequest(new { error = "n must be between 0 and 1_000_000" });
        var b = await repo.GetAsync(boardId);
        if (b is null) return NotFound(new { error = "Board not found" });
        var life = new GameOfLife(b.Grid, new LifeConfig(b.Wrap));
        life.StepN(n);
        var gen = b.Generation + n;
        await repo.SaveAsync(boardId, life.Grid, b.Height, b.Width, b.Wrap, gen);
        return Ok(new GridResponse(boardId, gen, life.ToJagged(), null));
    }

    /// <summary>
    /// Retrieves the final state of the game board after a specified number of iterations.
    /// </summary>
    /// <param name="boardId">Unique identifier for the board</param>
    /// <param name="max_iters">Maximum number of iterations to compute</param>
    /// <param name="repo">Repository to access board data</param>
    /// <returns>Returns the final state of the board after max_iters iterations.</returns>
    /// <remarks>
    /// This endpoint allows clients to compute the final state of a game board after a specified number of iterations.
    /// It retrieves the current state of the board, applies the Game of Life rules to compute the final state,
    /// and saves the updated state back to the database.
    /// If the board does not exist, a 404 Not Found error is returned.
    /// If the maximum iterations are reached without finding a stable state or cycle, an Unprocessable Entity error is returned.
    /// </remarks>
    /// <example>
    /// <code lang="json">
    /// {
    ///   "id": "a1b2c3d4e5f6",
    ///   "generation": 100,
    ///   "grid": [[0, 1], [1, 0]],
    ///   "meta": {
    ///     "status": "cycle",
    ///     "period": 10
    ///   }
    /// }
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GridResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("boards/{boardId}/final")]
    public async Task<IActionResult> Final(string boardId, int max_iters, [FromServices] BoardRepository repo)
    {
        if (max_iters < 1 || max_iters > 5_000_000) return BadRequest(new { error = "max_iters must be in [1, 5_000_000]" });
        var b = await repo.GetAsync(boardId);
        if (b is null) return NotFound(new { error = "Board not found" });

        var life = new GameOfLife(b.Grid, new LifeConfig(b.Wrap));
        var (finalGrid, info) = life.FinalState(max_iters);
        if (info.Status == "maxed") return UnprocessableEntity(new { error = $"No stable/cycle within {max_iters} iterations" });

        var gen = b.Generation + info.Iterations;
        await repo.SaveAsync(boardId, finalGrid, b.Height, b.Width, b.Wrap, gen);
        object meta = info.Status == "cycle" ? new { status = "cycle", period = info.Period } : new { status = "fixed" };
        return Ok(new GridResponse(boardId, gen, life.ToJagged(), meta));
    }
}
