using FluentAssertions;
using Life.Api.Domain;

namespace Life.Api.Tests;

// <summary>
// Tests for the Game of Life domain logic.
// This class contains unit tests for various Game of Life scenarios.
// It includes tests for fixed points, cycles, and the correctness of the Game of Life implementation.
// </summary>
// <remarks>
// The GameOfLifeTests class is designed to validate the core logic of the Game of Life.
// It includes tests for specific patterns such as blocks and blinkers, ensuring that the Game of Life behaves as expected.
// The tests cover scenarios like fixed points and cycles, checking that the final state of the game matches the expected results.
// The class uses FluentAssertions for expressive assertions and readability.
// </remarks>
public class GameOfLifeTests
{
    /// <summary>
    /// Tests that a block pattern is recognized as a fixed point.
    /// This test creates a 2x2 block of alive cells and checks that the Game of Life implementation
    /// recognizes it as a fixed point after a number of generations.
    /// </summary>
    [Fact]
    public void Block_Is_Fixed_Point()
    {
        var block = new int[,] { { 1, 1 }, { 1, 1 } };
        var life = new GameOfLife(block, new LifeConfig(false));
        var (final, info) = life.FinalState(10);
        info.Status.Should().Be("fixed");
        GameOfLife.Equal(final, block).Should().BeTrue();
    }

    /// <summary>
    /// Tests that a blinker pattern has a period of 2.
    /// This test creates a vertical blinker pattern and checks that the Game of Life implementation
    /// recognizes it as a cycle with a period of 2 after a number of generations.
    /// </summary>
    [Fact]
    public void Blinker_Period_2()
    {
        var start = new int[,] {
            {0,1,0},
            {0,1,0},
            {0,1,0}
        };
        var life = new GameOfLife(start, new LifeConfig(false));
        var (_, info) = life.FinalState(10);
        info.Status.Should().Be("cycle");
        info.Period.Should().Be(2);
    }

    /// <summary>
    /// Tests that a Game of Life instance can step through generations correctly.
    /// This test initializes a Game of Life instance with a random grid and checks that stepping through
    /// generations produces the expected results.
    /// </summary>
    [Fact]
    public void StepN_Equals_Repeated_Step()
    {
        var rnd = new Random(0);
        var g = new int[10, 10];
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                g[i, j] = rnd.NextDouble() < 0.2 ? 1 : 0;

        var a = new GameOfLife(g);
        var b = new GameOfLife(g);
        a.StepN(25);
        for (int k = 0; k < 25; k++) b.Step();

        GameOfLife.Equal(a.Grid, b.Grid).Should().BeTrue();
    }
}
