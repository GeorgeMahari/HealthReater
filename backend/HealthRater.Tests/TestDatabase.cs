using HealthRater.Data;
using HealthRater.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Tests;

/// <summary>
/// A private SQLite database per test, built by applying the real migrations — so the
/// tests also prove the migrations produce a working schema. SQLite's ":memory:" mode is
/// used only here for isolation and speed; the application itself uses a file database.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public HealthRaterDbContext Context { get; }

    private TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Context = NewContext();
        Context.Database.Migrate();
    }

    public static TestDatabase Create() => new();

    /// <summary>A second context on the same database, e.g. to read back without the change tracker.</summary>
    public HealthRaterDbContext NewContext() =>
        new SqliteHealthRaterDbContext(new DbContextOptionsBuilder<SqliteHealthRaterDbContext>().UseSqlite(_connection).Options);

    public static User AddUser(HealthRaterDbContext db, string email)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "test-hash",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
