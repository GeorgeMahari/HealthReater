using HealthRater.Core.Models;
using HealthRater.Core.Validation;

namespace HealthRater.Core.Profile;

/// <summary>
/// Rules for the profile data used by assessments: sex, date of birth, height and weight.
/// The date of birth is the source of truth for age, which is always calculated for a given
/// day and never stored on the profile.
/// </summary>
public static class ProfileRules
{
    public const int MinAge = 18;
    public const int MaxAge = 100;
    public const double MinHeightCm = 50;
    public const double MaxHeightCm = 250;
    public const double MinWeightKg = 20;
    public const double MaxWeightKg = 400;

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

    /// <summary>Validates the profile data supplied by the user: sex, date of birth, height, weight.</summary>
    public static ValidationOutcome Validate(string? sex, DateOnly? dateOfBirth, double? heightCm, double? weightKg, DateOnly today)
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

        if (heightCm is null or < MinHeightCm or > MaxHeightCm || double.IsNaN(heightCm.Value))
        {
            outcome.Errors.Add($"Height is required and must be between {MinHeightCm} and {MaxHeightCm} cm.");
        }
        if (weightKg is null or < MinWeightKg or > MaxWeightKg || double.IsNaN(weightKg.Value))
        {
            outcome.Errors.Add($"Weight is required and must be between {MinWeightKg} and {MaxWeightKg} kg.");
        }
        return outcome;
    }

    /// <summary>
    /// The profile data for an assessment on a given day, or an error message when the profile
    /// is incomplete or the calculated age is outside the supported range.
    /// </summary>
    public static (ProfileSnapshot? Profile, string? Error) ContextFor(
        Sex? sex, DateOnly? dateOfBirth, double? heightCm, double? weightKg, DateOnly today)
    {
        if (sex is null || dateOfBirth is null || heightCm is null || weightKg is null)
        {
            return (null, "Complete your profile (sex, date of birth, height and weight) before starting an assessment.");
        }

        var age = AgeOn(dateOfBirth.Value, today);
        if (age < MinAge || age > MaxAge)
        {
            return (null, $"HealthRater assessments are for people aged {MinAge}–{MaxAge}. Please check the date of birth in your profile.");
        }
        return (new ProfileSnapshot(sex.Value, age, heightCm.Value, weightKg.Value), null);
    }
}
