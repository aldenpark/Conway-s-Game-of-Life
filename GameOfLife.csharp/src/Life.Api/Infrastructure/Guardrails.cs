namespace Life.Api.Infrastructure;

/// <summary>
/// Interface for guardrails to enforce constraints on game board dimensions and cell counts.
/// Provides properties for maximum dimensions and cell counts, and a method to check if given dimensions are
/// </summary>
/// <remarks>
/// The IGuardrails interface defines the contract for guardrails that restrict the size and complexity of
/// game boards in the Game of Life application. It includes properties for maximum dimensions and cell counts,
/// as well as a method to validate if given dimensions are within the allowed limits.
/// Implementations of this interface can enforce specific rules for board sizes and cell counts.
/// </remarks>
public interface IGuardrails
{
    int MaxDim { get; }
    int MaxCells { get; }
    string BoundsMsg { get; }
    bool Allowed(int h, int w);
}

/// <summary>
/// Implementation of guardrails for the Game of Life application.
/// Enforces constraints on the maximum dimensions and cell counts for game boards.
/// Provides methods to check if given dimensions are within the allowed limits.
/// </summary>
/// <remarks>
/// The Guardrails class implements the IGuardrails interface to enforce constraints on game board dimensions
/// and cell counts. It provides properties for maximum dimensions and cell counts, and a method to check if given dimensions
/// are within the allowed limits. This implementation can be used to ensure that game boards do not exceed specified limits,
/// preventing excessive resource usage and ensuring a consistent user experience.
/// </remarks>
public sealed class Guardrails : IGuardrails
{
    public int MaxDim { get; }
    public int MaxCells { get; }
    public string BoundsMsg => $"height/width <= {MaxDim} and height*width <= {MaxCells}";

    /// <summary>
    /// Initializes a new instance of the Guardrails class with specified maximum dimensions and cell counts.
    /// </summary>
    /// <param name="maxDim">Maximum height and width for game boards</param>
    /// <param name="maxCells">Maximum total number of cells in the game board</param>
    /// <remarks>/// This constructor sets the maximum dimensions and cell counts for game boards.
    /// It is used to enforce constraints on the size and complexity of game boards in the Game of Life application.
    /// The bounds message provides a human-readable description of the constraints.
    /// </remarks>
    /// <example lang="csharp">
    /// var guardrails = new Guardrails(100, 10000);    // Creates guardrails with max dimensions of 100 and max cells of 10,000.
    /// </example>
    public Guardrails(int maxDim, int maxCells) => (MaxDim, MaxCells) = (maxDim, maxCells);

    /// <summary>
    /// Checks if the given height and width are within the allowed limits.
    /// </summary>
    /// <param name="h">Height of the game board</param>
    /// <param name="w">Width of the game board</param>
    /// <returns>True if the dimensions are allowed, false otherwise.</returns>
    /// <remarks>
    /// This method checks if the provided height and width are within the maximum dimensions and cell counts defined by the guardrails.
    /// It returns true if the dimensions are valid, and false if they exceed the allowed limits.
    /// This is used to validate game board dimensions before creating or updating boards in the Game of Life application.
    /// </remarks>
    /// <example lang="csharp">
    /// var guardrails = new Guardrails(100, 10000);
    /// bool isValid = guardrails.Allowed(50, 200); // Returns true
    /// isValid = guardrails.Allowed(150, 50); // Returns false
    /// </example>
    public bool Allowed(int h, int w) =>
        h > 0 && w > 0 && h <= MaxDim && w <= MaxDim && (long)h * w <= MaxCells;
}
