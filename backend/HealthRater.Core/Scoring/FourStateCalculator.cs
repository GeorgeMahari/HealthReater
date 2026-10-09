using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring;

/// <summary>
/// Configurable parameter -> state membership map. Parameters can contribute to
/// multiple states. This is architectural grouping only, not a clinically validated
/// predictive model (per spec).
///
/// Substance use: alcohol, tobacco and recreational drugs are three independent parameters.
/// Each sits in the same two states the former combined parameter belonged to: Immunity
/// (toxin load on immune, gut, skin and dental health) and Longevity (long-term
/// cardiovascular, cancer and mortality risk). So Immunity has 13 parameters and Longevity
/// 15; Energy/Strength/Stamina and Mental/Emotional are unchanged.
/// </summary>
public static class FourStateCalculator
{
    public static readonly string[] EnergyStrengthStaminaParams =
    {
        HealthRatingEngine.Keys.EnergyLevel,
        HealthRatingEngine.Keys.EnergyStability,
        HealthRatingEngine.Keys.SleepQuality,
        HealthRatingEngine.Keys.CircadianHealth,
        HealthRatingEngine.Keys.RestingHeartRate,
        HealthRatingEngine.Keys.HeartRateRecovery,
        HealthRatingEngine.Keys.Neat,
        HealthRatingEngine.Keys.PhysicalTraining,
        HealthRatingEngine.Keys.FunctionalPower,
        HealthRatingEngine.Keys.Cooper,
    };

    public static readonly string[] MentalEmotionalParams =
    {
        HealthRatingEngine.Keys.Mood,
        HealthRatingEngine.Keys.MoodStability,
        HealthRatingEngine.Keys.SocialLife,
        HealthRatingEngine.Keys.JobSatisfaction,
        HealthRatingEngine.Keys.HomeFamilySatisfaction,
        HealthRatingEngine.Keys.SleepQuality,
        HealthRatingEngine.Keys.CircadianHealth,
    };

    public static readonly string[] ImmunityParams =
    {
        HealthRatingEngine.Keys.Hydration,
        HealthRatingEngine.Keys.Digestion,
        HealthRatingEngine.Keys.ImmuneHealth,
        HealthRatingEngine.Keys.Caffeine,
        HealthRatingEngine.Keys.JunkFood,
        HealthRatingEngine.Keys.Overeating,
        HealthRatingEngine.Keys.Alcohol,
        HealthRatingEngine.Keys.Tobacco,
        HealthRatingEngine.Keys.Drugs,
        HealthRatingEngine.Keys.VegetablesFiber,
        HealthRatingEngine.Keys.SkinHealth,
        HealthRatingEngine.Keys.DentalHealth,
        HealthRatingEngine.Keys.HairHealth,
    };

    public static readonly string[] LongevityParams =
    {
        HealthRatingEngine.Keys.Age,
        HealthRatingEngine.Keys.BodyFat,
        HealthRatingEngine.Keys.Bmi,
        HealthRatingEngine.Keys.Waist,
        HealthRatingEngine.Keys.WHtR,
        HealthRatingEngine.Keys.WHR,
        HealthRatingEngine.Keys.BloodPressure,
        HealthRatingEngine.Keys.RestingHeartRate,
        HealthRatingEngine.Keys.HeartRateRecovery,
        HealthRatingEngine.Keys.PhysicalTraining,
        HealthRatingEngine.Keys.Neat,
        HealthRatingEngine.Keys.VegetablesFiber,
        HealthRatingEngine.Keys.Alcohol,
        HealthRatingEngine.Keys.Tobacco,
        HealthRatingEngine.Keys.Drugs,
    };

    public static FourStates Calculate(Dictionary<string, int> scores)
    {
        return new FourStates
        {
            EnergyStrengthStamina = Build(scores, EnergyStrengthStaminaParams),
            MentalEmotional = Build(scores, MentalEmotionalParams),
            Immunity = Build(scores, ImmunityParams),
            Longevity = Build(scores, LongevityParams),
        };
    }

    private static StateScore Build(Dictionary<string, int> scores, string[] keys)
    {
        var raw = keys.Sum(k => scores.TryGetValue(k, out var v) ? v : 0);
        var max = keys.Length * 10;
        return new StateScore
        {
            RawScore = raw,
            MaxRawScore = max,
            NormalizedScore = max == 0 ? 0 : Math.Round((raw / (double)max) * 100, 2),
        };
    }
}
