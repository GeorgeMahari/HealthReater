using HealthRater.Core.Auth;
using HealthRater.Core.Imaging;
using HealthRater.Core.Models;
using HealthRater.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Data.Services;

public enum ProfileUpdateResult
{
    Updated,
    NotFound,
    EmailTaken,
}

/// <summary>
/// Self-service account management. Every method acts only on the user id passed in,
/// which the API always takes from the authenticated session.
/// </summary>
public class ProfileService
{
    public const int MaxAvatarBytes = 2 * 1024 * 1024;
    public const int MinAvatarSide = 32;
    public const int MaxAvatarSide = 4096;

    private readonly HealthRaterDbContext _db;

    public ProfileService(HealthRaterDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Updates name and (optionally changed) email, and, when supplied, sex and date of birth.
    /// Inputs must already be validated. Existing assessments are never touched: they keep the
    /// sex/age snapshot taken when they were completed.
    /// </summary>
    public async Task<(ProfileUpdateResult Result, User? User)> UpdateAsync(
        Guid userId, string firstName, string lastName, string email,
        Sex? sex = null, DateOnly? dateOfBirth = null, double? heightCm = null, double? weightKg = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (ProfileUpdateResult.NotFound, null);

        var normalized = AuthValidator.NormalizeEmail(email);
        if (normalized != user.Email && await _db.Users.AnyAsync(u => u.Email == normalized && u.Id != userId))
        {
            return (ProfileUpdateResult.EmailTaken, null);
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.Email = normalized;
        if (sex is not null) user.Sex = sex;
        if (dateOfBirth is not null) user.DateOfBirth = dateOfBirth;
        if (heightCm is not null) user.HeightCm = heightCm;
        if (weightKg is not null) user.WeightKg = weightKg;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (ProfileUpdateResult.EmailTaken, null); // unique index won a race
        }
        return (ProfileUpdateResult.Updated, user);
    }

    public async Task<bool> SetPasswordHashAsync(Guid userId, string passwordHash)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return false;
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Validates the bytes (format from magic bytes, size, dimensions) and stores them as the
    /// user's avatar. Returns an error message, or null on success.
    /// </summary>
    public async Task<string?> SetAvatarAsync(Guid userId, byte[] data)
    {
        if (data.Length == 0) return "The file is empty.";
        if (data.Length > MaxAvatarBytes) return "The image must be 2 MB or smaller.";
        if (!ImageInspector.TryInspect(data, out var info)) return "Only JPG, PNG or WEBP images are supported.";
        if (info.Width < MinAvatarSide || info.Height < MinAvatarSide)
            return $"The image must be at least {MinAvatarSide}×{MinAvatarSide} pixels.";
        if (info.Width > MaxAvatarSide || info.Height > MaxAvatarSide)
            return $"The image must be at most {MaxAvatarSide}×{MaxAvatarSide} pixels.";

        var user = await _db.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return "Account not found.";

        var now = DateTime.UtcNow;
        user.Avatar ??= new UserAvatar { UserId = userId };
        user.Avatar.ContentType = info.ContentType;
        user.Avatar.Data = data;
        user.Avatar.Width = info.Width;
        user.Avatar.Height = info.Height;
        user.Avatar.UpdatedAt = now;
        user.AvatarUpdatedAt = now;
        user.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task RemoveAvatarAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Avatar is null) return;
        _db.UserAvatars.Remove(user.Avatar);
        user.AvatarUpdatedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<UserAvatar?> GetAvatarAsync(Guid userId) =>
        _db.UserAvatars.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

    /// <summary>Permanently deletes the account; sessions, avatar and all assessments cascade.</summary>
    public async Task<bool> DeleteAccountAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}
