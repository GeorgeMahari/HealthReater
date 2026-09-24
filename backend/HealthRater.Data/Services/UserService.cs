using HealthRater.Core.Auth;
using HealthRater.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Data.Services;

public class UserService
{
    private readonly HealthRaterDbContext _db;

    public UserService(HealthRaterDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByEmailAsync(string email)
    {
        var normalized = AuthValidator.NormalizeEmail(email);
        return _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized);
    }

    public Task<User?> FindByIdAsync(Guid id) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>Returns null when the email is already registered.</summary>
    public async Task<User?> CreateAsync(string firstName, string lastName, string email, string passwordHash)
    {
        var normalized = AuthValidator.NormalizeEmail(email);
        if (await _db.Users.AnyAsync(u => u.Email == normalized)) return null;

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalized,
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Lost a race with a concurrent registration: the unique email index rejected it.
            _db.Entry(user).State = EntityState.Detached;
            return null;
        }

        return user;
    }
}
