using Microsoft.EntityFrameworkCore;

namespace Life.Api.Data;

/// <summary>
/// Represents a game board entity in the database.
/// Contains properties for the board's dimensions, wrap state, and current generation.
/// </summary>
/// <remarks>
/// The Board entity is used to track the board's metadata, while the BoardState entity stores
/// the serialized grid state in JSON format.
/// This separation allows for efficient updates to the board's state without modifying its metadata.
/// </remarks>
[Index(nameof(Id), IsUnique = true)]
public class Board
{
    public string Id { get; set; } = default!;
    public int Height { get; set; }
    public int Width  { get; set; }
    public bool Wrap  { get; set; }
    public int Generation { get; set; }
}

/// <summary>
/// Represents the state of a game board in the database.
/// </summary>
/// <remarks>
/// The BoardState entity stores the grid as a JSON string for easy serialization and deserialization.
/// This allows for flexible grid sizes and structures without needing to define a fixed schema.
/// </remarks>
public class BoardState
{
    public string BoardId { get; set; } = default!;
    // Store the grid as JSON (TEXT) for readability and ease of tooling
    public string GridJson { get; set; } = default!;
}
