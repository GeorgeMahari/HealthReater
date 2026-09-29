using System.ComponentModel.DataAnnotations;

namespace HealthRater.Core.Models;

/// <summary>
/// The answers a user gives in the assessment questionnaire — everything except sex and
/// age. Those two are profile context (see <see cref="ScoringContext"/>): the API takes
/// them from the signed-in user's profile, never from the request.
/// BMI, WHtR and WHR are NOT collected here — they are derived server-side from
/// height/weight/waist/hip (see DerivedMetricsCalculator).
/// </summary>
public class AssessmentAnswers
{
    // ---------- 1. Basic Information (sex & age come from the profile) ----------
    [Range(50, 250, ErrorMessage = "Height must be between 50 and 250 cm.")]
    public double HeightCm { get; set; }

    [Range(20, 400, ErrorMessage = "Weight must be between 20 and 400 kg.")]
    public double WeightKg { get; set; }

    // ---------- 2. Body Metrics ----------
    [Range(30, 250, ErrorMessage = "Waist must be between 30 and 250 cm.")]
    public double WaistCm { get; set; }

    [Range(30, 250, ErrorMessage = "Hip must be between 30 and 250 cm.")]
    public double HipCm { get; set; }

    [Range(0, 100, ErrorMessage = "Body fat percentage must be between 0 and 100.")]
    public double BodyFatPercent { get; set; }

    // ---------- 3. Cardiovascular ----------
    [Range(30, 220, ErrorMessage = "Resting heart rate must be between 30 and 220 bpm.")]
    public int RestingHeartRateBpm { get; set; }

    [Range(0, 100, ErrorMessage = "Heart-rate recovery (1-minute drop) must be between 0 and 100 bpm.")]
    public int HeartRateRecoveryBpm { get; set; }

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
    [Range(0, 10, ErrorMessage = "Daily water intake must be between 0 and 10 liters.")]
    public double DailyWaterIntakeLiters { get; set; }

    [Range(1, 10)] public int DigestionAndEvacuation { get; set; }
    [Range(1, 10)] public int ImmuneHealth { get; set; }

    [Range(0, 20, ErrorMessage = "Caffeine servings/day must be between 0 and 20.")]
    public double CaffeineServingsPerDay { get; set; }

    [Range(0, 50, ErrorMessage = "Junk food servings/week must be between 0 and 50.")]
    public double JunkFoodServingsPerWeek { get; set; }

    [Range(0, 21, ErrorMessage = "Overeating episodes/week must be between 0 and 21.")]
    public double OvereatingEpisodesPerWeek { get; set; }

    [Required]
    public SubstanceFrequency AlcoholTobaccoDrugsFrequency { get; set; }

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
/// Everything the scoring engine needs: the questionnaire answers plus the profile context
/// (sex and age at the time of the assessment). Sex and age are two of the 39 scored
/// parameters, and also context for sex/age-aware parameters (body fat, WHR, functional power).
/// </summary>
public class AssessmentInput : AssessmentAnswers
{
    [Required]
    public Sex Sex { get; set; }

    [Range(18, 100, ErrorMessage = "Age must be between 18 and 100.")]
    public int Age { get; set; }

    /// <summary>Combines questionnaire answers with the profile context into one scoring input.</summary>
    public static AssessmentInput From(AssessmentAnswers answers, ScoringContext context)
    {
        var input = new AssessmentInput { Sex = context.Sex, Age = context.Age };
        foreach (var property in typeof(AssessmentAnswers).GetProperties())
        {
            property.SetValue(input, property.GetValue(answers));
        }
        return input;
    }
}

/// <summary>Profile context used for scoring: the user's sex and their age on the day of the assessment.</summary>
public record ScoringContext(Sex Sex, int Age);
