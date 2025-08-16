namespace Life.Api.DTO;

/// <summary>
/// Contains the identifier of a newly created board.
/// </summary>
/// <param name="BoardId">Unique identifier for the board.</param>
/// <remarks>
/// Returned by <c>POST /boards</c> when a board is created successfully.
/// </remarks>
/// <example lang="csharp">
/// var response = new BoardResponse("a1b2c3d4e5f6");
/// Console.WriteLine($"Created board with ID: {response.BoardId}");
/// </example>
public sealed record BoardResponse(string BoardId);

/// <summary>
/// Represents a snapshot of a board's state at a specific generation.
/// </summary>
/// <param name="BoardId">Unique identifier for the board.</param>
/// <param name="Generation">Generation number corresponding to the returned grid.</param>
/// <param name="Grid">
/// The board grid as a jagged array of 0/1 values, where 1 = alive, 0 = dead.
/// Each inner array is a row.
/// </param>
/// <param name="Meta">
/// Optional metadata. For <c>/final</c> it contains:
/// <c>{ "status":"fixed" }</c> for fixed points, or
/// <c>{ "status":"cycle", "period": &lt;k&gt; }</c> for cycles.
/// Otherwise <see langword="null"/>.
/// </param>
/// <remarks>
/// Returned by <c>GET /boards/{id}</c>, <c>/next</c>, <c>/advance</c>, and <c>/final</c>.
/// </remarks>
/// <example lang="csharp">
/// var grid = new[] {
///     new[] { 0, 1, 0 },
///     new[] { 0, 1, 0 },
///     new[] { 0, 1, 0 }
/// };
/// var resp = new GridResponse("a1b2c3", 12, grid, new { status = "cycle", period = 2 });
/// Console.WriteLine($"Gen {resp.Generation}, cells[1][1]={resp.Grid[1][1]}");
/// </example>
public sealed record GridResponse(
    string BoardId,
    int Generation,
    int[][] Grid,
    object? Meta
);
