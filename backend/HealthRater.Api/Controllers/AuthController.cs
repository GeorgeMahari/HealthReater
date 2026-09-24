using System.Security.Claims;
using HealthRater.Api.Dtos;
using HealthRater.Core.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HealthRater.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public const string RateLimitPolicy = "auth";
    private const string InvalidCredentials = "Invalid email or password.";

    // Verified against when the email is unknown, so a failed login takes the same
    // time whether or not the account exists (no user enumeration via timing).
    private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString());

    private readonly IUserStore _users;

    public AuthController(IUserStore users)
    {
        _users = users;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
    {
        var validation = AuthValidator.ValidateRegistration(request.Name, request.Email, request.Password);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors });
        }

        var user = await _users.CreateAsync(request.Name!, request.Email!, PasswordHasher.Hash(request.Password!));
        if (user is null)
        {
            return Conflict(new { errors = new[] { "An account with this email already exists." } });
        }

        await SignInAsync(user);
        return CreatedAtAction(nameof(Me), ToResponse(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { errors = new[] { "Email and password are required." } });
        }

        var user = await _users.FindByEmailAsync(request.Email);
        var passwordOk = PasswordHasher.Verify(request.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !passwordOk)
        {
            return Unauthorized(new { errors = new[] { InvalidCredentials } });
        }

        await SignInAsync(user);
        return Ok(ToResponse(user));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>
    /// Current session. Anonymous visitors get 204 rather than 401 so the frontend's
    /// session check on every page load doesn't surface as a console error.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<UserResponse>> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }

        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = Guid.TryParse(id, out var guid) ? await _users.FindByIdAsync(guid) : null;
        if (user is null)
        {
            // Cookie refers to a user that no longer exists — clear it.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        return Ok(ToResponse(user));
    }

    private Task SignInAsync(UserRecord user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    private static UserResponse ToResponse(UserRecord user) => new(user.Id, user.Name, user.Email);
}
