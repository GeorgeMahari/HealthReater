namespace HealthRater.Api.Dtos;

public record RegisterRequest(string? FirstName, string? LastName, string? Email, string? Password);

public record LoginRequest(string? Email, string? Password);

/// <summary>Public view of a user — never includes the password hash or session data.</summary>
public record UserResponse(Guid Id, string FirstName, string LastName, string Name, string Email);
