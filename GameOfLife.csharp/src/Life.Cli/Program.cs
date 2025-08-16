/*
Conway's Game of Life CLI
 
This is a simple command-line interface for running Conway's Game of Life.
It allows you to specify board dimensions, density, animation options, and more.
 
Usage:
dotnet run --project src/Life.Cli/Life.Cli.csproj -- [options]
*/

using System.Text;
using Life.Api.Domain; // GameOfLife, LifeConfig

// Args: --animate --width=80 --height=30 --density=0.25 --fps=15 --nowrap --seed=42 --steps=100 --chars=" .#"
var argsDict = ParseArgs(args);

int width   = GetInt("--width",  80);
int height  = GetInt("--height", 30);
double dens = GetDouble("--density", 0.25);
int steps   = GetInt("--steps", 100);
bool animate= GetBool("--animate", false);
int fps     = GetInt("--fps", 10);
bool wrap   = !GetBool("--nowrap", false);
int? seed   = GetNullableInt("--seed");
string chars= GetString("--chars", " #"); // dead, alive

if (GetBool("--help", false))
{
    PrintHelp();
    return;
}

// -- build engine -- (random initializer)
var life = GameOfLife.FromRandom(height, width, dens, seed, new LifeConfig(wrap));

bool stop = false;  // Flag to stop animation

if (animate)
{
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };  // Handle Ctrl+C to stop animation
    Console.CursorVisible = false;
    int delay = Math.Max(1, 1000 / Math.Max(1, fps));

    Console.Clear();
    for (int i = 0; i < steps && !stop; i++)
    {
        Console.SetCursorPosition(0, 0);
        Console.Write(Render(life.Grid, chars));
        Console.WriteLine($"\nGen: {i+1}  Size: {height}x{width}  Wrap: {wrap}  Seed: {(seed?.ToString() ?? "-")}");
        life.Step();            // Perform a single step in the Game of Life
        Thread.Sleep(delay);
    }
    Console.CursorVisible = true;
}
else
{
    life.StepN(steps);
    Console.WriteLine(Render(life.Grid, chars));
    Console.WriteLine($"\nGen: {steps}  Size: {height}x{width}  Wrap: {wrap}  Seed: {(seed?.ToString() ?? "-")}");
}

/// <summary>
/// Renders the game grid as a string using specified characters for dead and alive cells.
/// </summary>
/// <param name="grid">2D array representing the game grid</param>
/// <param name="chars">Two characters: first for dead cells, second for alive cells</param>
/// <returns>A string representation of the grid</returns>
/// <remarks>
/// This method converts the 2D grid into a string format where each cell is represented by the specified characters.
/// The characters can be used to customize the appearance of the grid in the output.
/// </remarks>
string Render(int[,] grid, string chars)
{
    char dead = chars[0];
    char alive = chars[1];

    int h = grid.GetLength(0), w = grid.GetLength(1);
    var sb = new StringBuilder(h * (w + 1));            // +1 for newline
    for (int i = 0; i < h; i++)
    {
        for (int j = 0; j < w; j++)
            sb.Append(grid[i, j] == 1 ? alive : dead);  // Append alive or dead character based on cell state
        if (i < h - 1) sb.Append('\n');                 // Add newline except for the last row
    }
    return sb.ToString();
}


// --- helpers ---

/** 
 * Helper methods to get argument values with defaults.
 * These methods retrieve values from the parsed arguments dictionary.
 */
Dictionary<string, string?> ParseArgs(string[] argv)
{
    var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var a in argv)
    {
        if (!a.StartsWith("--")) continue;
        var eq = a.IndexOf('=');
        if (eq > 0) d[a[..eq]] = a[(eq + 1)..];
        else d[a] = "true";
    }
    return d;
}

int GetInt(string key, int def) => argsDict.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : def;
double GetDouble(string key, double def) => argsDict.TryGetValue(key, out var v) && double.TryParse(v, out var x) ? x : def;
bool GetBool(string key, bool def) => argsDict.TryGetValue(key, out var v) ? (v == null || v.Equals("true", StringComparison.OrdinalIgnoreCase)) : def;
int? GetNullableInt(string key) => argsDict.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : null;
string GetString(string key, string def) => argsDict.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : def;

void PrintHelp()
{
    Console.WriteLine(@"
Conway's Game of Life (CLI)

Usage:
  dotnet run --project src/Life.Cli/Life.Cli.csproj -- [options]

Options:
  --width=INT         Board width (default 80)
  --height=INT        Board height (default 30)
  --density=FLOAT     Random fill density 0..1 (default 0.25)
  --steps=INT         Steps to run (default 100)
  --animate           Animate in terminal
  --fps=INT           Frames per second when --animate (default 10)
  --nowrap            Disable wrap-around edges (default: wrap enabled)
  --seed=INT          Seed for reproducibility
  --chars="" .#""       Two characters: dead,alive (default ' .#')
  --help              Show this help
");
}
