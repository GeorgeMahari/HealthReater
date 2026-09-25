namespace HealthRater.Data.Entities;

/// <summary>
/// A user's profile picture, stored in the database (one row per user) rather than on
/// disk: no filesystem paths to expose or clean up, it's deleted with the account, and it
/// works the same with SQLite or SQL Server. Kept in its own table so loading a User never
/// drags the image bytes along.
/// </summary>
public class UserAvatar
{
    /// <summary>Primary key and foreign key to <see cref="User"/>.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Detected from the bytes: image/jpeg, image/png or image/webp.</summary>
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime UpdatedAt { get; set; }
}
