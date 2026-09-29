using HealthRater.Core.Models;

namespace HealthRater.Data.Entities;

/// <summary>
/// Account data only. Health information never lives here — every measurement is
/// snapshotted on <see cref="HealthAssessment"/> at the time it was taken.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Always stored normalized (trimmed, lower-case). Unique.</summary>
    public string Email { get; set; } = "";

    /// <summary>PBKDF2 hash produced by <c>HealthRater.Core.Auth.PasswordHasher</c>.</summary>
    public string PasswordHash { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// When the avatar was last changed; null when the user has none. The image itself is
    /// in <see cref="Avatar"/> (separate table). Also used to version the avatar URL.
    /// </summary>
    public DateTime? AvatarUpdatedAt { get; set; }

    /// <summary>Profile context for scoring. Required (with DateOfBirth) before an assessment.</summary>
    public Sex? Sex { get; set; }

    /// <summary>
    /// Source of truth for age: the current age is always calculated from it (never stored).
    /// Each assessment keeps its own snapshot, so changing this never alters past results.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserAvatar? Avatar { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<HealthAssessment> HealthAssessments { get; set; } = new();

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    /// <summary>True when both pieces of profile context needed for scoring are present.</summary>
    public bool ProfileCompleted => Sex is not null && DateOfBirth is not null;
}
