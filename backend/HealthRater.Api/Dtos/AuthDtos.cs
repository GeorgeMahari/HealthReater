namespace HealthRater.Api.Dtos;

public record RegisterRequest(string? FirstName, string? LastName, string? Email, string? Password);

public record LoginRequest(string? Email, string? Password);

/// <summary>
/// Public view of the signed-in user — never includes the password hash or session data.
/// <c>AvatarUrl</c> is an API-relative URL (versioned for caching), or null without an avatar.
/// </summary>
public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    DateTime CreatedAt,
    string? AvatarUrl)
{
    public static UserResponse From(HealthRater.Data.Entities.User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.DisplayName,
        user.Email,
        user.CreatedAt,
        user.AvatarUpdatedAt is { } updated ? $"/api/profile/avatar?v={updated.Ticks}" : null);
}

public record UpdateProfileRequest(string? FirstName, string? LastName, string? Email);

public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public record DeleteAccountRequest(string? Password);
