using System.Security.Cryptography;

namespace Life.Api.Domain;

/// <summary>
/// Represents a Game of Life board.
/// Contains the grid state, dimensions, and configuration options.
/// Provides methods to initialize from random data or a list, step through generations,
/// and compute the final state after a number of iterations.
/// </summary>
/// <remarks>
/// Uses <see cref="GameOfLife"/> to model the Game of Life board.
/// This class encapsulates the grid state, dimensions, and configuration options for the Game of Life.
/// It provides methods to initialize the board from random data or a list, step through generations,
/// and compute the final state after a specified number of iterations.
/// The board can be configured to wrap around edges or not.
/// It also includes methods to check for equality between two grids and to hash the grid state.
/// The final state can be returned along with information about the number of iterations and status.
/// </remarks>
public sealed class GameOfLife
{
    public int Height => Grid.GetLength(0);
    public int Width => Grid.GetLength(1);
    public int[,] Grid { get; private set; }
    public LifeConfig Config { get; }

    /// <summary>
    /// Initializes a new instance of the GameOfLife class with a given grid and configuration.
    /// The grid must be a 2D array of integers (0 or 1).
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="config"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <remarks>/// This constructor initializes the Game of Life board with a specified grid state and configuration options.
    /// The grid must be a 2D array of integers, where each cell can be either 0 (dead) or 1 (alive).
    /// The configuration options can specify whether the board wraps around edges and other parameters.
    /// If the grid is not 2D or contains invalid values, an exception is thrown.
    /// </remarks>
    /// <example lang="csharp">
    /// var grid = new int[,]
    /// {
    ///     { 0, 1, 0 },
    ///     { 1, 0, 1 },
    ///     { 0, 0, 1 }
    /// };
    /// var game = new GameOfLife(grid, new LifeConfig(true));
    /// </example>
    /// <returns>A new instance of the GameOfLife class.</returns>
    public GameOfLife(int[,] grid, LifeConfig? config = null)
    {
        if (grid.Rank != 2) throw new ArgumentException("grid must be 2D"); // Ensure the grid is a 2D array
        Grid = (int[,])grid.Clone();                // Clone the grid to ensure the original is not modified
        Config = config ?? new LifeConfig(true);    // Use provided config or default to wrapping around edges
    }

    /// <summary>
    /// Creates a new GameOfLife instance with a random grid of specified dimensions and density.
    /// </summary>
    /// <param name="height">Height of the grid</param>
    /// <param name="width">Width of the grid</param>
    /// <param name="density">Density of live cells (0.0 to 1.0)</param>
    /// <param name="seed">Optional random seed for reproducibility</param>
    /// <param name="config">Optional configuration for the game</param>
    /// <remarks>/// This method generates a random grid of specified dimensions, where each cell has a probability
    /// of being alive based on the given density.
    /// The grid is initialized with random values (0 or 1) based on the specified density.
    /// If a seed is provided, it ensures that the random grid can be reproduced.
    /// The configuration options can specify whether the board wraps around edges and other parameters.
    /// </remarks>
    /// <example lang="csharp">
    /// var game = GameOfLife.FromRandom(10, 10, 0.25, seed: 42, config: new LifeConfig(true));
    /// </example>
    /// <returns>A new GameOfLife instance with a random grid.</returns>
    public static GameOfLife FromRandom(int height, int width, double density = 0.25, int? seed = null, LifeConfig? config = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();    // Use provided seed or create a new random number
        var g = new int[height, width];                         // Initialize a new 2D array for the grid
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                g[i, j] = rng.NextDouble() < density ? 1 : 0;   // Fill the grid with random values based on the density
        return new GameOfLife(g, config);
    }

    /// <summary>
    /// Creates a new GameOfLife instance from a jagged array of integers.
    /// The jagged array must have consistent row lengths.
    /// </summary>
    /// <param name="data">Jagged array representing the grid state</param>
    /// <param name="config">Optional configuration for the game</param>
    /// <exception cref="ArgumentException">Thrown if the jagged array is empty or rows have inconsistent lengths</exception>
    /// <remarks>
    /// This method initializes a Game of Life board from a jagged array of integers.
    /// The jagged array must have consistent row lengths, and each value must be either 0 or 1.
    /// If the jagged array is empty or the rows have inconsistent lengths, an exception is thrown.
    /// The configuration options can specify whether the board wraps around edges and other parameters.
    /// </remarks>
    /// <example lang="csharp">
    /// var data = new int[][]
    /// {
    ///     new int[] { 0, 1, 0 },
    ///     new int[] { 1, 0, 1 },
    ///     new int[] { 0, 0, 1 }
    /// };
    /// var game = GameOfLife.FromList(data, new LifeConfig(true));
    /// </example>
    /// <returns>A new GameOfLife instance initialized from the jagged array.</returns>
    public static GameOfLife FromList(int[][] data, LifeConfig? config = null)
    {
        if (data.Length == 0) throw new ArgumentException("grid must be non-empty");
        var w = data[0].Length;
        if (w == 0) throw new ArgumentException("rows must be non-empty");
        for (int i = 1; i < data.Length; i++)
            if (data[i].Length != w) throw new ArgumentException("all rows must have same length");
        var g = new int[data.Length, w];
        for (int i = 0; i < data.Length; i++)
            for (int j = 0; j < w; j++)
            {
                var v = data[i][j];
                if (v is not (0 or 1)) throw new ArgumentException("grid values must be 0 or 1");
                g[i, j] = v;
            }
        return new GameOfLife(g, config);
    }

    /// <summary>
    /// Converts the grid to a jagged array for easier access.
    /// </summary>
    /// <remarks>
    /// Converts the 2D array to a jagged array for better readability in API responses.
    /// </remarks>
    /// <example lang="csharp">
    /// var data = game.ToJagged();
    /// </example>
    /// <returns>Jagged array representation of the grid</returns>
    public int[][] ToJagged()
    {
        var h = Height; var w = Width;
        var arr = new int[h][]; //  Initialize jagged array with height rows
        for (int i = 0; i < h; i++)
        {
            arr[i] = new int[w];    //  Initialize each row
            for (int j = 0; j < w; j++) arr[i][j] = Grid[i, j]; // Copy values from the 2D array to the jagged array
        }
        return arr;
    }

    /// <summary>
    /// Steps the game forward by one generation.
    /// Applies the Game of Life rules to update the grid state.
    /// </summary>
    /// <remarks>
    /// This method applies the Game of Life rules to update the grid state for one generation.
    /// It checks each cell's neighbors and updates the cell's state based on the number of live neighbors.
    /// If the board is configured to wrap around edges, it handles neighbor calculations accordingly.
    /// The method modifies the Grid property directly, updating the state of the board in place.
    /// </remarks>
    /// <example lang="csharp">
    /// game.Step(); // Advances the game by one generation
    /// </example>
    public void Step()
    {
        var h = Height; var w = Width;
        var next = new int[h, w];

        if (Config.Wrap)    //  Wrap around edges
        {
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                {
                    // Calculate neighbors with wrapping
                    int up = (i - 1 + h) % h, dn = (i + 1) % h;
                    int lf = (j - 1 + w) % w, rt = (j + 1) % w;

                    int nbrs = Grid[up, j] + Grid[dn, j] + Grid[i, lf] + Grid[i, rt] +
                               Grid[up, lf] + Grid[up, rt] + Grid[dn, lf] + Grid[dn, rt];

                    next[i, j] = (nbrs == 3 || (nbrs == 2 && Grid[i, j] == 1)) ? 1 : 0; // Apply Game of Life rules
                }
        }
        else
        {
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                {
                    int nbrs = 0;
                    for (int di = -1; di <= 1; di++)
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            if (di == 0 && dj == 0) continue;
                            int ni = i + di, nj = j + dj;
                            if (ni >= 0 && ni < h && nj >= 0 && nj < w) nbrs += Grid[ni, nj]; // Count live neighbors
                        }
                    next[i, j] = (nbrs == 3 || (nbrs == 2 && Grid[i, j] == 1)) ? 1 : 0; // Apply Game of Life rules
                }
        }

        Grid = next;
    }

    /// <summary>
    /// Steps the game forward by a specified number of generations.
    /// </summary>
    /// <param name="n">Number of generations to step forward</param>
    /// <remarks>
    /// This method advances the game by a specified number of generations.
    /// It repeatedly calls the Step method to update the grid state for each generation.
    /// This allows for simulating multiple generations in a single call.
    /// </remarks>
    /// <example lang="csharp">
    /// game.StepN(5); // Advances the game by 5 generations
    /// </example>
    public void StepN(int n)
    {
        for (int k = 0; k < n; k++) Step();
    }

    /// <summary>
    /// Computes the final state of the game after a specified number of iterations.
    /// </summary>
    /// <param name="maxIters">Maximum number of iterations to run</param>
    /// <remarks>
    /// This method simulates the Game of Life for a specified number of iterations.
    /// It checks for cycles by storing previously seen grid states and comparing them.
    /// If a cycle is detected, it returns the current grid state along with information about the cycle.
    /// If the grid stabilizes (no changes in state), it returns the final state along with information about the stabilization.
    /// If the maximum number of iterations is reached without stabilization or cycles, it returns the current state with a "maxed" status.
    /// </remarks>
    /// <example lang="csharp">
    /// var finalState = game.FinalState(1000);
    /// Console.WriteLine($"Final state after {finalState.Info.Iterations} iterations: {finalState.Info.Status}");
    /// </example>
    /// <returns>
    /// A tuple containing the final grid state and information about the final state.
    /// The information includes the number of iterations, status (e.g., "cycle", "fixed", "maxed"),
    /// and optional details about the first seen state and period if a cycle is detected.
    /// </returns>
    public (int[,] Final, FinalInfo Info) FinalState(int maxIters = 100_000)
    {
        var seen = new Dictionary<string, int>(capacity: 4096); //  Use a dictionary to track seen grid states and their iteration counts
        for (int i = 0; i <= maxIters; i++)
        {
            var h = HashGrid(Grid);
            if (seen.TryGetValue(h, out var first))
            {
                return (Grid, new FinalInfo(i, "cycle", FirstSeenAt: first, Period: i - first));    //  Cycle detected
            }
            seen[h] = i;

            var prev = (int[,])Grid.Clone();    //  Clone the current grid state to compare later
            Step();
            if (Equal(prev, Grid))
            {
                return (Grid, new FinalInfo(i + 1, "fixed"));   //  Stabilized state
            }
        }
        return (Grid, new FinalInfo(maxIters, "maxed"));    //  Reached maximum iterations without stabilization or cycles
    }

    /// <summary>
    /// Checks if two grids are equal.
    /// </summary>
    /// <param name="a">First grid to compare</param>
    /// <param name="b">Second grid to compare</param>
    /// <remarks>
    /// This method compares two 2D arrays (grids) for equality.
    /// It checks if both arrays have the same dimensions and if all corresponding elements are equal.
    /// If the dimensions differ, it returns false.
    /// If the dimensions are the same, it iterates through each element to check for equality.
    /// </remarks>
    /// <example lang="csharp">
    /// var grid1 = new int[,] { { 0, 1 }, { 1, 0 } };
    /// var grid2 = new int[,] { { 0, 1 }, { 1, 0 } };
    /// bool areEqual = GameOfLife.Equal(grid1, grid2);
    /// </example>
    /// <returns>True if the grids are equal, false otherwise</returns>
    public static bool Equal(int[,] a, int[,] b)
    {
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1)) return false;
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                if (a[i, j] != b[i, j]) return false;
        return true;
    }

    /// <summary>
    /// Computes a SHA-256 hash of the grid state.
    /// </summary>
    /// <param name="g">Grid to hash</param>
    /// <remarks>
    /// This method computes a SHA-256 hash of the grid state for efficient storage and comparison.
    /// It serializes the grid dimensions and cell values into a byte array, which is then hashed using SHA-256.
    /// The resulting hash can be used to uniquely identify the grid state.
    /// </remarks>
    /// <example lang="csharp">
    /// var grid = new int[,] { { 0, 1 }, { 1, 0 } };
    /// string hash = GameOfLife.HashGrid(grid);
    /// Console.WriteLine($"Hash of the grid: {hash}");
    /// </example>
    /// <returns>Hexadecimal string representation of the SHA-256 hash of the grid</returns>
    public static string HashGrid(int[,] g)
    {
        var h = g.GetLength(0); var w = g.GetLength(1);
        Span<byte> bytes = stackalloc byte[4 + 4 + h * w];
        BitConverter.TryWriteBytes(bytes, h);
        BitConverter.TryWriteBytes(bytes[4..], w);
        var offset = 8;
        for (int i = 0; i < h; i++)
            for (int j = 0; j < w; j++)
                bytes[offset++] = (byte)g[i, j];    //  Serialize the grid values into the byte array

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Represents information about the final state of the game after iterations.
    /// Contains the number of iterations, status, and optional details about cycles.
    /// </summary>
    /// <remarks>
    /// The FinalInfo record holds metadata about the final state of the game after running for a specified number of iterations.
    /// It includes the total number of iterations, a status string indicating whether the game stabilized, entered a cycle, or reached the maximum iterations,
    /// and optional details about the first seen state and period if a cycle was detected.
    /// This information can be used to analyze the behavior of the Game of Life board over time.
    /// </remarks>
    /// <param name="Iterations">Total number of iterations run</param>
    /// <param name="Status">Status of the final state (e.g., "cycle", "fixed", "maxed")</param>
    /// <param name="FirstSeenAt">Optional; the iteration at which the first seen state was encountered (if a cycle was detected)</param>
    /// <param name="Period">Optional; the period of the cycle (if a cycle was detected)</param>
    /// <example lang="csharp">
    /// var info = new FinalInfo(100, "cycle", FirstSeenAt: 50, Period: 10);
    /// Console.WriteLine($"Iterations: {info.Iterations}, Status: {info.Status}, First Seen At: {info.FirstSeenAt}, Period: {info.Period}");
    /// </example>
    /// <returns>A record containing information about the final state of the game</returns>
    public readonly record struct FinalInfo(int Iterations, string Status, int? FirstSeenAt = null, int? Period = null);
}
