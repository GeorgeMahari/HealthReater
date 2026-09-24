using System.Net.Mail;
using HealthRater.Core.Validation;

namespace HealthRater.Core.Auth;

public static class AuthValidator
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxNameLength = 80;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static ValidationOutcome ValidateRegistration(string? name, string? email, string? password)
    {
        var outcome = new ValidationOutcome();

        var trimmedName = name?.Trim() ?? "";
        if (trimmedName.Length == 0)
        {
            outcome.Errors.Add("Name is required.");
        }
        else if (trimmedName.Length > MaxNameLength)
        {
            outcome.Errors.Add($"Name must be at most {MaxNameLength} characters.");
        }

        if (!IsValidEmail(email))
        {
            outcome.Errors.Add("Please enter a valid email address.");
        }

        outcome.Errors.AddRange(PasswordErrors(password));
        return outcome;
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
        var trimmed = email.Trim();
        if (!MailAddress.TryCreate(trimmed, out var address)) return false;
        // MailAddress accepts display-name forms like "Bob <b@x.io>"; require a bare address with a dotted domain.
        return address.Address == trimmed && address.Host.Contains('.');
    }

    public static IEnumerable<string> PasswordErrors(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            yield return "Password is required.";
            yield break;
        }

        if (password.Length < MinPasswordLength)
            yield return $"Password must be at least {MinPasswordLength} characters.";
        if (password.Length > MaxPasswordLength)
            yield return $"Password must be at most {MaxPasswordLength} characters.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            yield return "Password must contain at least one letter and one number.";
    }
}
