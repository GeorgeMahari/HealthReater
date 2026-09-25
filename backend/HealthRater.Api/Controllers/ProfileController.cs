using System.Security.Claims;
using HealthRater.Api.Dtos;
using HealthRater.Core.Auth;
using HealthRater.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HealthRater.Api.Controllers;

/// <summary>
/// The signed-in user's own account. There is deliberately no user id anywhere in these
/// routes: every action applies to the user in the session, so nobody can edit, read the
/// avatar of, or delete someone else's account.
/// </summary>
[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profiles;
    private readonly UserService _users;
    private readonly SessionService _sessions;

    public ProfileController(ProfileService profiles, UserService users, SessionService sessions)
    {
        _profiles = profiles;
        _users = users;
        _sessions = sessions;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update([FromBody] UpdateProfileRequest request)
    {
        var validation = AuthValidator.ValidateProfile(request.FirstName, request.LastName, request.Email);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors });
        }

        var (result, user) = await _profiles.UpdateAsync(CurrentUserId, request.FirstName!, request.LastName!, request.Email!);
        return result switch
        {
            ProfileUpdateResult.EmailTaken => Conflict(new { errors = new[] { "An account with this email already exists." } }),
            ProfileUpdateResult.NotFound => NotFound(),
            _ => Ok(UserResponse.From(user!)),
        };
    }

    /// <summary>Requires the current password. Signs out every other device.</summary>
    [HttpPut("password")]
    [EnableRateLimiting(AuthController.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (string.IsNullOrEmpty(request.CurrentPassword) || !PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { errors = new[] { "Your current password is incorrect." } });
        }

        var errors = AuthValidator.PasswordErrors(request.NewPassword).ToList();
        if (request.NewPassword == request.CurrentPassword)
        {
            errors.Add("The new password must be different from the current one.");
        }
        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        await _profiles.SetPasswordHashAsync(user.Id, PasswordHasher.Hash(request.NewPassword!));
        await _sessions.RevokeAllExceptAsync(user.Id, User.FindFirstValue(AuthController.SessionClaim));
        return NoContent();
    }

    /// <summary>
    /// Multipart upload, field "file". The format is detected from the bytes (JPG/PNG/WEBP
    /// only); the client's file name and content type are ignored.
    /// </summary>
    [HttpPost("avatar")]
    [RequestSizeLimit(ProfileService.MaxAvatarBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProfileService.MaxAvatarBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> UploadAvatar(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { errors = new[] { "Choose an image to upload." } });
        }
        if (file.Length > ProfileService.MaxAvatarBytes)
        {
            return BadRequest(new { errors = new[] { "The image must be 2 MB or smaller." } });
        }

        byte[] data;
        await using (var stream = file.OpenReadStream())
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer);
            data = buffer.ToArray();
        }

        var error = await _profiles.SetAvatarAsync(CurrentUserId, data);
        if (error is not null)
        {
            return BadRequest(new { errors = new[] { error } });
        }

        var user = await _users.FindByIdAsync(CurrentUserId);
        return Ok(UserResponse.From(user!));
    }

    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> RemoveAvatar()
    {
        await _profiles.RemoveAvatarAsync(CurrentUserId);
        var user = await _users.FindByIdAsync(CurrentUserId);
        return Ok(UserResponse.From(user!));
    }

    /// <summary>The signed-in user's own avatar image. The version query string only busts caches.</summary>
    [HttpGet("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvatar()
    {
        var avatar = await _profiles.GetAvatarAsync(CurrentUserId);
        if (avatar is null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'";
        return File(avatar.Data, avatar.ContentType);
    }

    /// <summary>Requires the password. Permanently deletes the account and all its assessments.</summary>
    [HttpDelete]
    [EnableRateLimiting(AuthController.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (string.IsNullOrEmpty(request.Password) || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return BadRequest(new { errors = new[] { "Your password is incorrect." } });
        }

        await _profiles.DeleteAccountAsync(user.Id);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
