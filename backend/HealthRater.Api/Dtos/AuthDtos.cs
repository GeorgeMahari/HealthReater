namespace HealthRater.Api.Dtos;

public record RegisterRequest(string? Name, string? Email, string? Password);

public record LoginRequest(string? Email, string? Password);

/// <summary>Public view of a user — never includes the password hash.</summary>
public record UserResponse(Guid Id, string Name, string Email);
