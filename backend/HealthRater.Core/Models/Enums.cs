namespace HealthRater.Core.Models;

public enum Sex
{
    Male,
    Female
}

/// <summary>
/// Frequency scale used for substance use (alcohol / tobacco / drugs).
/// Ordered from worst to best for clarity; scoring maps this explicitly.
/// </summary>
public enum SubstanceFrequency
{
    Daily,
    SeveralTimesPerWeek,
    Weekly,
    Monthly,
    Rarely,
    Never
}
