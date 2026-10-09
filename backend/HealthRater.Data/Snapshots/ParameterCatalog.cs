using HealthRater.Core.Models;
using K = HealthRater.Core.Scoring.HealthRatingEngine.Keys;

namespace HealthRater.Data.Snapshots;

/// <summary>What was fed into one parameter's scorer.</summary>
public record ParameterInput(double? RawValue, string? RawText = null, double? NormalizedValue = null);

public record ParameterDefinition(
    string Key,
    string Name,
    int SortOrder,
    string? Unit,
    Func<AssessmentInput, DerivedMetrics, ParameterInput> Extract);

/// <summary>
/// The current parameter set (ParameterSet, v2: 41 parameters) in official order: stable key,
/// display name, unit and how to read the raw input that produced each score. Keys are the
/// same as the engine's <c>parameterScores</c> keys returned by the API. Assessments saved
/// under v1 keep their own stored rows (including the combined "substanceUse" row).
/// </summary>
public static class ParameterCatalog
{
    private const string Scale = "1–10 scale";

    public static readonly IReadOnlyList<ParameterDefinition> All = Build();

    private static List<ParameterDefinition> Build()
    {
        var order = 0;
        ParameterDefinition P(string key, string name, string? unit, Func<AssessmentInput, DerivedMetrics, ParameterInput> extract)
            => new(key, name, ++order, unit, extract);

        return new List<ParameterDefinition>
        {
            // Basic information
            P(K.Sex, "Sex", null, (i, _) => new(null, i.Sex.ToString())),
            P(K.Age, "Age", "years", (i, _) => new(i.Age)),
            P(K.Height, "Height", "cm", (i, _) => new(i.HeightCm)),
            P(K.Weight, "Weight", "kg", (i, _) => new(i.WeightKg)),

            // Body metrics
            P(K.Waist, "Waist circumference", "cm", (i, _) => new(i.WaistCm)),
            P(K.Hip, "Hip circumference", "cm", (i, _) => new(i.HipCm)),
            P(K.BodyFat, "Body fat", "%", (i, _) => new(
                i.BodyFatPercent,
                i.BodyFatSource == BodyFatSource.Estimated ? "Estimated" : "Measured")),
            P(K.Bmi, "BMI", "kg/m²", (_, d) => new(d.Bmi)),
            P(K.WHtR, "Waist-to-height ratio (WHtR)", "ratio", (_, d) => new(d.WHtR)),
            P(K.WHR, "Waist-to-hip ratio (WHR)", "ratio", (_, d) => new(d.WHR)),

            // Cardiovascular
            P(K.RestingHeartRate, "Resting heart rate", "bpm", (i, _) => new(i.RestingHeartRateBpm)),
            P(K.HeartRateRecovery, "Heart Rate Recovery (HRR)", "bpm", (i, _) => new(
                i.HeartRateRecoveryBpm,
                $"Peak {i.PeakHeartRateBpm} bpm · after 60 s {i.HeartRateAfter60sBpm} bpm")),
            P(K.BloodPressure, "Blood pressure", "mmHg", (i, _) => new(null, $"{i.SystolicBpMmHg}/{i.DiastolicBpMmHg}")),

            // Energy & sleep
            P(K.EnergyLevel, "Energy level", Scale, (i, _) => new(i.EnergyLevel)),
            P(K.EnergyStability, "Energy stability", Scale, (i, _) => new(i.EnergyStability)),
            P(K.SleepQuality, "Average sleep quality", Scale, (i, _) => new(i.AverageSleepQuality)),
            P(K.CircadianHealth, "Circadian health", Scale, (i, _) => new(i.CircadianHealth)),

            // Mental & emotional
            P(K.Mood, "Average mood", Scale, (i, _) => new(i.AverageMood)),
            P(K.MoodStability, "Mood stability", Scale, (i, _) => new(i.MoodStability)),
            P(K.SocialLife, "Friends / social life", Scale, (i, _) => new(i.SocialLife)),
            P(K.JobSatisfaction, "Job satisfaction", Scale, (i, _) => new(i.JobSatisfaction)),
            P(K.HomeFamilySatisfaction, "Home & family satisfaction", Scale, (i, _) => new(i.HomeFamilySatisfaction)),

            // Lifestyle
            P(K.Hydration, "Daily water intake", "L/day", (i, _) => new(
                i.DailyWaterIntakeLiters,
                NormalizedValue: i.WeightKg > 0 ? Math.Round(i.DailyWaterIntakeLiters * 1000 / i.WeightKg, 1) : null)),
            P(K.Digestion, "Digestion & evacuation", Scale, (i, _) => new(i.DigestionAndEvacuation)),
            P(K.ImmuneHealth, "Immune health", Scale, (i, _) => new(i.ImmuneHealth)),
            P(K.Caffeine, "Caffeine", "servings/day", (i, _) => new(i.CaffeineServingsPerDay)),
            P(K.JunkFood, "Junk food", "servings/week", (i, _) => new(i.JunkFoodServingsPerWeek)),
            P(K.Overeating, "Overeating / gluttony", "episodes/week", (i, _) => new(i.OvereatingEpisodesPerWeek)),
            P(K.Alcohol, "Alcohol consumption", "frequency", (i, _) => new(null, i.AlcoholFrequency?.ToString())),
            P(K.Tobacco, "Tobacco / smoking", "frequency", (i, _) => new(null, i.TobaccoFrequency?.ToString())),
            P(K.Drugs, "Recreational drug use", "frequency", (i, _) => new(null, i.DrugsFrequency?.ToString())),
            P(K.VegetablesFiber, "Vegetables & fiber", "servings/day", (i, _) => new(i.VegetablesFiberServingsPerDay)),

            // Physical performance
            P(K.Neat, "NEAT / daily movement", "steps/day", (i, _) => new(i.DailyStepsNeat)),
            P(K.PhysicalTraining, "Physical training", "sessions/week", (i, _) => new(i.TrainingSessionsPerWeek)),
            P(K.FunctionalPower, "Functional power", "reps", (i, _) => new(
                i.PushUps + i.PullUps + i.BodyweightSquats,
                $"{i.PushUps} push-ups · {i.PullUps} pull-ups · {i.BodyweightSquats} squats")),
            P(K.Cooper, "12-minute Cooper run", "meters", (i, _) => new(i.CooperDistanceMeters)),

            // General health
            P(K.SkinHealth, "Skin health", Scale, (i, _) => new(i.SkinHealth)),
            P(K.JawSkullHealth, "Jaw & skull health", Scale, (i, _) => new(i.JawSkullHealth)),
            P(K.DentalHealth, "Dental health / caries", Scale, (i, _) => new(i.DentalHealth)),
            P(K.SpinalHealth, "Spinal health", Scale, (i, _) => new(i.SpinalHealth)),
            P(K.HairHealth, "Hair health", Scale, (i, _) => new(i.HairHealth)),
        };
    }
}
