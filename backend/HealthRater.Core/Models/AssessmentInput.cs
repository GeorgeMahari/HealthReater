using System.ComponentModel.DataAnnotations;
using HealthRater.Core.Scoring.BodyFat;

namespace HealthRater.Core.Models;

/// <summary>
/// The answers a user gives in the assessment questionnaire — everything except sex, age,
/// height and weight. Those four come from the user's profile (see <see cref="ProfileSnapshot"/>):
/// the API takes them from the signed-in user's profile, never from the request.
/// BMI, WHtR and WHR are NOT collected here — they are derived server-side from
/// height/weight/waist/hip (see DerivedMetricsCalculator).
/// </summary>
public class AssessmentAnswers
{
    // ---------- 1. Basic Information: sex, age, height and weight come from the profile ----------

    // ---------- 2. Body Metrics ----------
    [Range(30, 250, ErrorMessage = "Waist must be between 30 and 250 cm.")]
    public double WaistCm { get; set; }

    [Range(30, 250, ErrorMessage = "Hip must be between 30 and 250 cm.")]
    public double HipCm { get; set; }

    /// <summary>
    /// Measured body-fat percentage, or null when the user doesn't know it: HealthRater then
    /// estimates it (see BodyFatEstimation) and records the source as Estimated.
    /// </summary>
    [Range(2, 75, ErrorMessage = "Body fat percentage must be between 2 and 75.")]
    public double? BodyFatPercent { get; set; }

    // ---------- 3. Cardiovascular ----------
    [Range(30, 220, ErrorMessage = "Resting heart rate must be between 30 and 220 bpm.")]
    public int RestingHeartRateBpm { get; set; }

    /// <summary>Highest heart rate reached at the end of the standardized exercise.</summary>
    [Range(80, 230, ErrorMessage = "Peak heart rate must be between 80 and 230 bpm.")]
    public int PeakHeartRateBpm { get; set; }

    /// <summary>Heart rate measured exactly 60 seconds after exercise stops.</summary>
    [Range(40, 230, ErrorMessage = "Heart rate 60 seconds after exercise must be between 40 and 230 bpm.")]
    public int HeartRateAfter60sBpm { get; set; }

    /// <summary>
    /// Heart Rate Recovery: the drop in the first 60 seconds after exercise stops
    /// (peak minus after 60 s). Always calculated, never entered.
    /// </summary>
    public int HeartRateRecoveryBpm => PeakHeartRateBpm - HeartRateAfter60sBpm;

    [Range(60, 260, ErrorMessage = "Systolic blood pressure must be between 60 and 260 mmHg.")]
    public int SystolicBpMmHg { get; set; }

    [Range(30, 160, ErrorMessage = "Diastolic blood pressure must be between 30 and 160 mmHg.")]
    public int DiastolicBpMmHg { get; set; }

    // ---------- 4. Energy & Sleep ----------
    [Range(1, 10)] public int EnergyLevel { get; set; }
    [Range(1, 10)] public int EnergyStability { get; set; }
    [Range(1, 10)] public int AverageSleepQuality { get; set; }
    [Range(1, 10)] public int CircadianHealth { get; set; }

    // ---------- 5. Mental & Emotional ----------
    [Range(1, 10)] public int AverageMood { get; set; }
    [Range(1, 10)] public int MoodStability { get; set; }
    [Range(1, 10)] public int SocialLife { get; set; }
    [Range(1, 10)] public int JobSatisfaction { get; set; }
    [Range(1, 10)] public int HomeFamilySatisfaction { get; set; }

    // ---------- 6. Lifestyle ----------
    /// <summary>Liters/day. The web UI asks in ml/day and converts (1000 ml = 1 L) before sending.</summary>
    [Range(0, 10, ErrorMessage = "Daily water intake must be between 0 and 10,000 ml (10 L) per day.")]
    public double DailyWaterIntakeLiters { get; set; }

    [Range(1, 10)] public int DigestionAndEvacuation { get; set; }
    [Range(1, 10)] public int ImmuneHealth { get; set; }

    [Range(0, 20, ErrorMessage = "Caffeine servings/day must be between 0 and 20.")]
    public double CaffeineServingsPerDay { get; set; }

    [Range(0, 50, ErrorMessage = "Junk food servings/week must be between 0 and 50.")]
    public double JunkFoodServingsPerWeek { get; set; }

    [Range(0, 21, ErrorMessage = "Overeating episodes/week must be between 0 and 21.")]
    public double OvereatingEpisodesPerWeek { get; set; }

    // Alcohol, tobacco and recreational drugs are three independent parameters (v2 parameter
    // set). Before v2 they were one combined "alcohol / tobacco / drugs" answer.
    [Required(ErrorMessage = "Alcohol consumption is required.")]
    public SubstanceFrequency? AlcoholFrequency { get; set; }

    [Required(ErrorMessage = "Tobacco / smoking is required.")]
    public SubstanceFrequency? TobaccoFrequency { get; set; }

    [Required(ErrorMessage = "Recreational drug use is required.")]
    public SubstanceFrequency? DrugsFrequency { get; set; }

    [Range(0, 15, ErrorMessage = "Vegetable/fiber servings/day must be between 0 and 15.")]
    public double VegetablesFiberServingsPerDay { get; set; }

    // ---------- 7. Physical Performance ----------
    [Range(0, 40000, ErrorMessage = "Daily steps (NEAT) must be between 0 and 40000.")]
    public int DailyStepsNeat { get; set; }

    [Range(0, 21, ErrorMessage = "Training sessions/week must be between 0 and 21.")]
    public double TrainingSessionsPerWeek { get; set; }

    [Range(0, 300, ErrorMessage = "Push-ups must be between 0 and 300.")]
    public int PushUps { get; set; }

    [Range(0, 100, ErrorMessage = "Pull-ups must be between 0 and 100.")]
    public int PullUps { get; set; }

    [Range(0, 300, ErrorMessage = "Bodyweight squats must be between 0 and 300.")]
    public int BodyweightSquats { get; set; }

    [Range(0, 5000, ErrorMessage = "Cooper 12-minute run distance must be between 0 and 5000 meters.")]
    public int CooperDistanceMeters { get; set; }

    // ---------- 8. General Health ----------
    [Range(1, 10)] public int SkinHealth { get; set; }
    [Range(1, 10)] public int JawSkullHealth { get; set; }
    [Range(1, 10)] public int DentalHealth { get; set; }
    [Range(1, 10)] public int SpinalHealth { get; set; }
    [Range(1, 10)] public int HairHealth { get; set; }
}

/// <summary>
/// Everything the scoring engine needs: the questionnaire answers plus the profile data at
/// the time of the assessment (sex, age, height, weight). These four are parameters #1–#4 of
/// the parameter set (see ParameterSet); sex and age are also context for sex/age-aware parameters (body fat, WHR,
/// functional power), and height/weight feed BMI, WHtR and hydration.
/// </summary>
public class AssessmentInput : AssessmentAnswers
{
    [Required]
    public Sex Sex { get; set; }

    [Range(18, 100, ErrorMessage = "Age must be between 18 and 100.")]
    public int Age { get; set; }

    [Range(50, 250, ErrorMessage = "Height must be between 50 and 250 cm.")]
    public double HeightCm { get; set; }

    [Range(20, 400, ErrorMessage = "Weight must be between 20 and 400 kg.")]
    public double WeightKg { get; set; }

    /// <summary>Measured (entered) or Estimated (BodyFatPercent was calculated by HealthRater).</summary>
    public BodyFatSource BodyFatSource { get; set; } = BodyFatSource.Measured;

    /// <summary>Estimation method id when BodyFatSource is Estimated, e.g. "Deurenberg1991".</summary>
    public string? BodyFatEstimationMethod { get; set; }

    /// <summary>Set when body fat was unknown and couldn't be estimated plausibly.</summary>
    public string? BodyFatEstimationError { get; set; }

    /// <summary>
    /// Combines questionnaire answers with the profile data into one scoring input. When the
    /// body-fat percentage is unknown (null) it is estimated here with the configured
    /// estimator, so the estimated value is scored exactly like a measured one.
    /// </summary>
    public static AssessmentInput From(AssessmentAnswers answers, ProfileSnapshot profile)
    {
        var input = new AssessmentInput
        {
            Sex = profile.Sex,
            Age = profile.Age,
            HeightCm = profile.HeightCm,
            WeightKg = profile.WeightKg,
        };
        foreach (var property in typeof(AssessmentAnswers).GetProperties().Where(p => p.CanWrite))
        {
            property.SetValue(input, property.GetValue(answers));
        }
        input.ResolveBodyFat();
        return input;
    }

    /// <summary>Estimates body fat when it wasn't entered. Idempotent.</summary>
    public void ResolveBodyFat()
    {
        if (BodyFatPercent is not null) return;
        var (estimate, error) = BodyFatEstimation.TryEstimate(BodyFatEstimation.InputFor(this));
        if (estimate is null)
        {
            BodyFatEstimationError = error;
            return;
        }
        BodyFatPercent = estimate.BodyFatPercent;
        BodyFatSource = BodyFatSource.Estimated;
        BodyFatEstimationMethod = estimate.MethodId;
        BodyFatEstimationError = null;
    }
}

/// <summary>Context for sex/age-specific scoring references: sex and age on the day of the assessment.</summary>
public record ScoringContext(Sex Sex, int Age);

/// <summary>The profile data used for one assessment: sex, age that day, height and weight.</summary>
public record ProfileSnapshot(Sex Sex, int Age, double HeightCm, double WeightKg)
{
    public ScoringContext Context => new(Sex, Age);
}
