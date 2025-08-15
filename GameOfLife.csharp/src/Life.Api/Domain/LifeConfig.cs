namespace Life.Api.Domain;

/// <summary>
/// Represents the configuration for the Game of Life.
/// This configuration includes settings such as whether the board wraps around edges.
/// </summary>
/// <remarks>
/// Uses <see cref="LifeConfig"/> to define the board's wrap state.
/// This class is used to configure the behavior of the Game of Life, such as whether the board wraps around edges.
/// It can be extended in the future to include additional configuration options.
/// </remarks>
/// <example>
/// <code>
/// var config = new LifeConfig(wrap: true);
/// </code>
/// </example>
public sealed record LifeConfig(bool Wrap = true);
