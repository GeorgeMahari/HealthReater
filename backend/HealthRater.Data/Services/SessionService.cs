using System.Security.Cryptography;
using System.Text;
using HealthRater.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Data.Services;

/// <summary>
/// Issues and checks server-side sessions (<see cref="RefreshToken"/> rows). Only the
/// SHA-256 hash of a token is persisted; the raw value is handed to the caller once.
/// </summary>
public class SessionService
{
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(30);

    private readonly HealthRaterDbContext _db;

    public SessionService(HealthRaterDbContext db)
    {
        _db = db;
    }

    /// <summary>Creates a session and returns the raw token (store it only in the auth cookie).</summary>
    public async Task<string> CreateAsync(Guid userId)
    {
        var now = DateTime.UtcNow;

        // Housekeeping: drop this user's dead sessions so the table doesn't grow forever.
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && (t.RevokedAt != null || t.ExpiresAt <= now))
            .ExecuteDeleteAsync();

        var raw = Base64Url(RandomNumberGenerator.GetBytes(32));
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            CreatedAt = now,
            ExpiresAt = now + AbsoluteLifetime,
        });
        await _db.SaveChangesAsync();
        return raw;
    }

    /// <summary>Returns the owning user id when the token is known, unexpired, unrevoked and the user is active.</summary>
    public async Task<Guid?> ValidateAsync(string? rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return null;

        var hash = Hash(rawToken);
        var now = DateTime.UtcNow;
        var session = await _db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > now && t.User.IsActive)
            .Select(t => new { t.UserId })
            .FirstOrDefaultAsync();
        return session?.UserId;
    }

    public async Task RevokeAsync(string? rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return;

        var hash = Hash(rawToken);
        await _db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
    }

    /// <summary>Revokes every active session of the user except the one identified by <paramref name="keepRawToken"/>.</summary>
    public Task RevokeAllExceptAsync(Guid userId, string? keepRawToken)
    {
        var keepHash = string.IsNullOrEmpty(keepRawToken) ? "" : Hash(keepRawToken);
        return _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.TokenHash != keepHash)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
    }

    public static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
