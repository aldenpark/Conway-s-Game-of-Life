using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Life.Api.Domain;

namespace Life.Api.Data;

/// <summary>
/// Repository for managing game boards in the database.
/// </summary>
/// <remarks>
/// Uses Entity Framework Core to persist boards and their compressed grid states.
/// </remarks>
public sealed class BoardRepository
{
    private readonly LifeDbContext _db;
    public BoardRepository(LifeDbContext db) => _db = db;

    // ---- Board operations ----
    /// <summary>
    /// Saves a board's state to the database.
    /// If the board already exists, updates its state.
    /// </summary>
    /// <param name="id">Unique identifier for the board</param>
    /// <param name="grid">Current grid state of the board</param>
    /// <param name="height">Height of the board</param>
    /// <param name="width">Width of the board</param>
    /// <param name="wrap">Whether the board wraps around edges</param>
    /// <param name="generation">Current generation number</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <remarks>
    /// The grid is stored as a JSON string in the database.
    /// The board's dimensions and wrap state are also saved.
    /// If the board does not exist, it is created.
    /// If it exists, its state is updated.
    /// </remarks>
    public async Task SaveAsync(string id, int[,] grid, int height, int width, bool wrap, int generation)
    {
        var board = await _db.Boards.FindAsync(id);
        if (board is null)
        {
            board = new Board { Id = id, Height = height, Width = width, Wrap = wrap, Generation = generation };
            await _db.Boards.AddAsync(board);
        }
        else
        {
            board.Height = height;
            board.Width = width;
            board.Wrap = wrap;
            board.Generation = generation;
            _db.Boards.Update(board);
        }

        var gridJson = SerializeGrid(grid);
        var state = await _db.States.FindAsync(id);
        if (state is null)
        {
            await _db.States.AddAsync(new BoardState { BoardId = id, GridJson = gridJson });
        }
        else
        {
            state.GridJson = gridJson;
            _db.States.Update(state);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Loads a board's state from the database by its ID.
    /// </summary>
    /// <param name="id">Unique identifier for the board</param>
    /// <returns>A LoadedBoard object containing the board's state, or null if not found</returns>
    /// <remarks>
    /// Retrieves the board's dimensions, wrap state, generation, and grid data.
    /// The grid is deserialized from JSON into a 2D array.
    /// If the board or its state does not exist, returns null.
    /// </remarks>
    public async Task<LoadedBoard?> GetAsync(string id)
    {
        var board = await _db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (board is null) return null;

        var state = await _db.States.AsNoTracking().FirstOrDefaultAsync(s => s.BoardId == id);
        if (state is null) return null;

        var grid = DeserializeGrid(state.GridJson);
        return new LoadedBoard(board.Id, board.Height, board.Width, board.Wrap, board.Generation, grid);
    }

    // ---- JSON helpers ----
    /// <summary>
    /// Serializes a 2D grid array to a JSON string.
    /// </summary>
    /// <returns>JSON string representation of the grid</returns>
    /// <remarks>
    /// Converts the 2D array to a jagged array for better readability.
    /// The resulting JSON is compact and does not include indentation.
    /// </remarks>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        // arrays only — no special naming policy required
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a 2D grid array to a JSON string.
    /// </summary>
    /// <param name="grid">2D array representing the grid</param>
    /// <returns>JSON string representation of the grid</returns>
    /// <remarks>
    /// Converts the 2D array to a jagged array for better readability.
    /// The resulting JSON is compact and does not include indentation.
    /// </remarks>
    private static string SerializeGrid(int[,] grid)
    {
        // Store as jagged int[][] for readability
        var h = grid.GetLength(0);
        var w = grid.GetLength(1);
        var jag = new int[h][];
        for (int i = 0; i < h; i++)
        {
            jag[i] = new int[w];
            for (int j = 0; j < w; j++) jag[i][j] = grid[i, j];
        }
        return JsonSerializer.Serialize(jag, JsonOpts);
    }

    /// <summary>
    /// Deserializes a JSON string to a 2D grid array.
    /// </summary>
    /// <param name="json">JSON string representing the grid</param>
    /// <returns>2D array representing the grid</returns>
    /// <remarks>
    /// Validates the JSON structure to ensure it represents a rectangular grid.
    /// Throws an exception if the JSON is invalid or does not conform to expected dimensions.
    /// </remarks>
    private static int[,] DeserializeGrid(string json)
    {
        var jag = JsonSerializer.Deserialize<int[][]>(json, JsonOpts)
                  ?? throw new InvalidDataException("Invalid grid JSON");
        if (jag.Length == 0) throw new InvalidDataException("Grid JSON has no rows");
        var w = jag[0].Length;
        if (w == 0) throw new InvalidDataException("Grid JSON has empty row");
        // validate rectangle
        for (int i = 1; i < jag.Length; i++)
            if (jag[i].Length != w) throw new InvalidDataException("Grid JSON rows must have same length");

        var h = jag.Length;
        var grid = new int[h, w];
        for (int i = 0; i < h; i++)
            for (int j = 0; j < w; j++)
                grid[i, j] = jag[i][j];
        return grid;
    }
}

/**
 * Represents a response containing the ID of a created board.
 * Used to return the board ID after successful creation.
 */
/// <summary>
/// Represents a loaded board with its state and configuration.
/// Contains the board's ID, dimensions, wrap state, generation, and grid data.
/// Provides a method to convert the grid to a jagged array for easier access.
/// </summary>
/// <param name="Id">Unique identifier for the board</param>
/// <param name="Height">Height of the board</param>
/// <param name="Width">Width of the board</param>
/// <param name="Wrap">Whether the board wraps around edges</param>
/// <param name="Generation">Current generation number</param>
/// <param name="Grid">2D array representing the board's grid state</param>
/// <remarks>
/// The grid is stored as a 2D array for efficient access.
/// The GridToJagged method converts it to a jagged array for API responses.
/// This allows for better compatibility with JSON serialization and client-side processing.
/// </remarks>
public sealed record LoadedBoard(string Id, int Height, int Width, bool Wrap, int Generation, int[,] Grid)
{
    /// <summary>
    /// Converts the grid to a jagged array.
    /// </summary>
    /// <returns>Jagged array representation of the grid</returns>
    /// <remarks>
    /// Converts the 2D array to a jagged array for better readability in API responses.
    /// </remarks>
    public int[][] GridToJagged()
    {
        var arr = new int[Height][];
        for (int i = 0; i < Height; i++)
        {
            arr[i] = new int[Width];
            for (int j = 0; j < Width; j++) arr[i][j] = Grid[i, j];
        }
        return arr;
    }
}
