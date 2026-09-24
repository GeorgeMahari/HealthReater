namespace HealthRater.Data.Entities;

/// <summary>
/// A server-side, revocable login session. The raw token only ever lives inside the
/// encrypted, HttpOnly auth cookie; the database stores its SHA-256 hash, so a leaked
/// database cannot be used to hijack sessions. Logging out revokes the row, which
/// invalidates the cookie on the next request even though the cookie itself is still valid.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Base64 SHA-256 of the raw token. Unique.</summary>
    public string TokenHash { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    /// <summary>Absolute session lifetime, independent of the cookie's sliding expiry.</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
