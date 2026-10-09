namespace HealthRater.Core.Models;

public enum Sex
{
    Male,
    Female
}

/// <summary>
/// Frequency scale used for each substance parameter (alcohol, tobacco, recreational drugs —
/// each answered and scored independently). Ordered from worst to best for clarity; scoring
/// maps this explicitly.
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

/// <summary>Where an assessment's body-fat percentage came from.</summary>
public enum BodyFatSource
{
    /// <summary>Entered by the user (e.g. from a scale, DEXA, calipers).</summary>
    Measured,

    /// <summary>Calculated by HealthRater from body measurements and demographics.</summary>
    Estimated,
}
