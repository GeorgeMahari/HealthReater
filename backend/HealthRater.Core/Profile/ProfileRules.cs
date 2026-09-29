using HealthRater.Core.Models;
using HealthRater.Core.Validation;

namespace HealthRater.Core.Profile;

/// <summary>
/// Rules for the profile context (sex + date of birth). The date of birth is the source of
/// truth; age is always calculated from it for a given day and never stored on the profile.
/// </summary>
public static class ProfileRules
{
    public const int MinAge = 18;
    public const int MaxAge = 100;

    /// <summary>Completed years between <paramref name="dateOfBirth"/> and <paramref name="onDate"/>.</summary>
    public static int AgeOn(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (onDate < dateOfBirth.AddYears(age)) age--; // birthday not reached yet this year
        return age;
    }

    /// <summary>Today's date for age calculations (UTC, so every server computes the same age).</summary>
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool TryParseSex(string? value, out Sex sex)
    {
        sex = default;
        return value is "Male" or "Female" && Enum.TryParse(value, out sex);
    }

    /// <summary>Validates a sex / date-of-birth pair supplied by the user.</summary>
    public static ValidationOutcome Validate(string? sex, DateOnly? dateOfBirth, DateOnly today)
    {
        var outcome = new ValidationOutcome();
        if (!TryParseSex(sex, out _))
        {
            outcome.Errors.Add("Sex is required and must be Male or Female.");
        }

        if (dateOfBirth is null)
        {
            outcome.Errors.Add("Date of birth is required.");
        }
        else if (dateOfBirth > today)
        {
            outcome.Errors.Add("Date of birth can't be in the future.");
        }
        else
        {
            var age = AgeOn(dateOfBirth.Value, today);
            if (age < MinAge || age > MaxAge)
            {
                outcome.Errors.Add($"HealthRater is for people aged {MinAge}–{MaxAge}; this date of birth gives an age of {age}.");
            }
        }
        return outcome;
    }

    /// <summary>
    /// The scoring context for a profile on a given day, or an error message when the profile
    /// is incomplete or the calculated age is outside the supported range.
    /// </summary>
    public static (ScoringContext? Context, string? Error) ContextFor(Sex? sex, DateOnly? dateOfBirth, DateOnly today)
    {
        if (sex is null || dateOfBirth is null)
        {
            return (null, "Complete your profile (sex and date of birth) before starting an assessment.");
        }

        var age = AgeOn(dateOfBirth.Value, today);
        if (age < MinAge || age > MaxAge)
        {
            return (null, $"HealthRater assessments are for people aged {MinAge}–{MaxAge}. Please check the date of birth in your profile.");
        }
        return (new ScoringContext(sex.Value, age), null);
    }
}
