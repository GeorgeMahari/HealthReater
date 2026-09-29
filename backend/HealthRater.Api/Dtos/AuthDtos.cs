namespace HealthRater.Api.Dtos;

public record RegisterRequest(string? FirstName, string? LastName, string? Email, string? Password);

public record LoginRequest(string? Email, string? Password);

/// <summary>
/// Public view of the signed-in user — never includes the password hash or session data.
/// <c>AvatarUrl</c> is an API-relative URL (versioned for caching), or null without an avatar.
/// <c>Age</c> is calculated today from <c>DateOfBirth</c>; it is not stored.
/// <c>ProfileCompleted</c> is true when sex and date of birth are both set — required before
/// an assessment can be started.
/// </summary>
public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    DateTime CreatedAt,
    string? AvatarUrl,
    string? Sex,
    DateOnly? DateOfBirth,
    int? Age,
    double? HeightCm,
    double? WeightKg,
    bool ProfileCompleted)
{
    public static UserResponse From(HealthRater.Data.Entities.User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.DisplayName,
        user.Email,
        user.CreatedAt,
        user.AvatarUpdatedAt is { } updated ? $"/api/profile/avatar?v={updated.Ticks}" : null,
        user.Sex?.ToString(),
        user.DateOfBirth,
        user.DateOfBirth is { } dob ? HealthRater.Core.Profile.ProfileRules.AgeOn(dob, HealthRater.Core.Profile.ProfileRules.Today()) : null,
        user.HeightCm,
        user.WeightKg,
        user.ProfileCompleted);
}

/// <summary>
/// Name/email are always required. The profile data — Sex ("Male"/"Female"), DateOfBirth
/// ("yyyy-MM-dd"), HeightCm, WeightKg — is optional, but when any of it is sent all four must be
/// valid. It only affects future assessments.
/// </summary>
public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Sex = null,
    DateOnly? DateOfBirth = null,
    double? HeightCm = null,
    double? WeightKg = null);

public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public record DeleteAccountRequest(string? Password);
