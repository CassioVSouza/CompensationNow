using CompenseAgora.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CompenseAgora.Tests.Infrastructure;

/// <summary>
/// Creates a <see cref="CompenseAgoraDbContext"/> backed by an in-memory SQLite database, so tests can
/// exercise real relational behavior (unique constraints, FK Restrict) that EF Core's InMemory provider
/// silently ignores. The underlying SQLite connection must stay open for the lifetime of the context(s)
/// created from it, or the in-memory database is destroyed - implement IDisposable/IAsyncDisposable and
/// dispose this after the test.
/// </summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CompenseAgoraDbContext> _options;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CompenseAgoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CompenseAgoraDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh DbContext instance sharing the same underlying in-memory database and schema.</summary>
    public CompenseAgoraDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
