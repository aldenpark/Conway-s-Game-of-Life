namespace Life.Api.DTO;

/// <summary>
/// Represents a request to upload a game board.
/// </summary>
/// <remarks>
/// This class is used to validate and process board upload requests.
/// It includes methods to check if the grid is valid and to extract dimensions.
/// The grid is expected to be a 2D array of integers, with each cell being either 0 or 1.
/// The height and width can be specified, but if the grid is provided, they will be derived from it.
/// The density and seed can be used for generating random boards.
/// The wrap state indicates whether the board wraps around edges.
/// </remarks>
public sealed class UploadBoardRequest
{
    public int[][]? Grid { get; init; }
    public int? Height { get; init; }
    public int? Width { get; init; }
    public double Density { get; init; } = 0.25;
    public int? Seed { get; init; }
    public bool Wrap { get; init; } = true;

    /// <summary>
    /// Validates the grid and extracts its dimensions.
    /// </summary>
    /// <param name="h">Output height of the grid</param>
    /// <param name="w">Output width of the grid</param>
    /// <param name="error">Output error message if validation fails</param>
    /// <returns>True if the grid is valid, false otherwise</returns>
    /// <remarks>
    /// This method checks if the grid is a non-empty 2D array with consistent row lengths.
    /// It also ensures that all values in the grid are either 0 or 1.
    /// If the grid is valid, it sets the height and width outputs accordingly.
    /// If the grid is null or empty, it checks the provided height and width.
    /// If both are null, it returns false with an appropriate error message.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new UploadBoardRequest { Grid = [[1, 0], [0, 1]] };
    /// if (request.IsValidGrid(out int h, out int w, out string? error))
    /// {
    ///     Console.WriteLine($"Grid is valid with height {h} and width {w}.");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Invalid grid: {error}");
    /// }
    /// </code>
    /// </example>
    public bool IsValidGrid(out int h, out int w, out string? error)
    {
        h = 0; w = 0; error = null;
        if (Grid is null || Grid.Length == 0) { error = "grid must be a non-empty 2D array"; return false; }
        w = Grid[0].Length;
        if (w == 0) { error = "rows must be non-empty"; return false; }
        for (int i = 1; i < Grid.Length; i++) if (Grid[i].Length != w) { error = "all rows must have same length"; return false; }
        for (int i = 0; i < Grid.Length; i++)
            for (int j = 0; j < w; j++)
                if (Grid[i][j] is not (0 or 1)) { error = "grid values must be 0 or 1"; return false; }
        h = Grid.Length;
        return true;
    }
}
