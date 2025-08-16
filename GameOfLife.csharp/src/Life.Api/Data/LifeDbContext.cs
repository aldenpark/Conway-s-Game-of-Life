using Microsoft.EntityFrameworkCore;

namespace Life.Api.Data;

/// <summary>
/// Represents the main database context for the Game of Life application.
/// This context manages the connection to the SQLite database and provides access to the game board entities.
/// </summary>
/// <remarks>
/// Uses <see cref="LifeDbContext"/> to manage game board entities.
/// </remarks>
public class LifeDbContext : DbContext
{
    public LifeDbContext(DbContextOptions<LifeDbContext> options) : base(options) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardState> States => Set<BoardState>();

    /// <summary>
    /// This method sets up the primary keys and relationships for the Board and BoardState entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the database model.</param>
    /// <remarks>
    /// This method is called by the Entity Framework Core runtime to configure the model for the database context.
    /// It defines the primary keys for the Board and BoardState entities,
    /// and establishes a one-to-one relationship between them.
    /// The BoardState entity has a foreign key to the Board entity, allowing for efficient
    /// retrieval of the board's state based on its ID.
    /// </remarks>
    /// <example>
    /// <![CDATA[
    /// var options = new DbContextOptionsBuilder<LifeDbContext>()
    ///     .UseSqlite("Data Source=gameoflife.db")
    ///     .Options;
    /// using var context = new LifeDbContext(options);
    /// context.Database.EnsureCreated(); // Ensures the database is created with the defined schema
    /// ]]>
    /// </example>
    /// <returns>The configured model builder.</returns>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>().HasKey(b => b.Id);
        modelBuilder.Entity<BoardState>().HasKey(s => s.BoardId);
        modelBuilder.Entity<BoardState>()
            .HasOne<Board>()
            .WithOne()
            .HasForeignKey<BoardState>(s => s.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
