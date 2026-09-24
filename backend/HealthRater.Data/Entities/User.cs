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

    /// <summary>Optional; relative path or absolute URL. Not collected yet.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Optional; not collected yet. Assessments snapshot age separately.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<HealthAssessment> HealthAssessments { get; set; } = new();

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
