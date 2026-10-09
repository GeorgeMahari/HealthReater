using K = HealthRater.Core.Scoring.HealthRatingEngine.Keys;

namespace HealthRater.Core.Scoring;

/// <summary>
/// Single source of truth for the scored parameter set: which parameters exist, their
/// official order, how many there are and the resulting maximum Total Health Rating.
/// Nothing else in the backend should hard-code the parameter count or the maximum.
///
/// History:
///  • v1 (39 parameters, max 390): alcohol, tobacco and drugs were one combined
///    "substanceUse" parameter. Assessments saved under v1 keep their 39 scores and 390
///    maximum forever (see HealthAssessment.ParameterSetVersion).
///  • v2 (41 parameters, max 410): substance use split into three independent parameters —
///    alcohol, tobacco and recreational drugs.
/// </summary>
public static class ParameterSet
{
    /// <summary>Stored with every new assessment.</summary>
    public const string Version = "v2-41";

    /// <summary>Version label for assessments saved before the split (39 parameters).</summary>
    public const string LegacyVersion = "v1-39";

    public const int MaxScorePerParameter = 10;

    /// <summary>The scored parameters in official order.</summary>
    public static readonly IReadOnlyList<string> Keys = new[]
    {
        // Basic information (from the profile)
        K.Sex, K.Age, K.Height, K.Weight,
        // Body metrics
        K.Waist, K.Hip, K.BodyFat, K.Bmi, K.WHtR, K.WHR,
        // Cardiovascular
        K.RestingHeartRate, K.HeartRateRecovery, K.BloodPressure,
        // Energy & sleep
        K.EnergyLevel, K.EnergyStability, K.SleepQuality, K.CircadianHealth,
        // Mental & emotional
        K.Mood, K.MoodStability, K.SocialLife, K.JobSatisfaction, K.HomeFamilySatisfaction,
        // Lifestyle
        K.Hydration, K.Digestion, K.ImmuneHealth, K.Caffeine, K.JunkFood, K.Overeating,
        K.Alcohol, K.Tobacco, K.Drugs, K.VegetablesFiber,
        // Physical performance
        K.Neat, K.PhysicalTraining, K.FunctionalPower, K.Cooper,
        // General health
        K.SkinHealth, K.JawSkullHealth, K.DentalHealth, K.SpinalHealth, K.HairHealth,
    };

    public static int Count => Keys.Count;

    /// <summary>Count × 10.</summary>
    public static int MaxTotalScore => Count * MaxScorePerParameter;
}
