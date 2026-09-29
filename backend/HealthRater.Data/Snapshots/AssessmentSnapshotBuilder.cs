using System.Text.Json;
using System.Text.Json.Serialization;
using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
using HealthRater.Data.Entities;

namespace HealthRater.Data.Snapshots;

/// <summary>
/// Turns a validated input plus the engine's result into an immutable, self-contained
/// <see cref="HealthAssessment"/> row set. Everything needed to redisplay the result
/// later is copied — nothing is recomputed when the assessment is read back.
/// </summary>
public static class AssessmentSnapshotBuilder
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <param name="dateOfBirth">The profile's date of birth at completion, kept as part of the snapshot.</param>
    public static HealthAssessment Build(
        Guid userId, AssessmentInput input, HealthRatingResult result, DateTime utcNow, DateOnly? dateOfBirth = null)
    {
        var assessment = new HealthAssessment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = AssessmentStatus.Completed,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            CompletedAt = utcNow,
            ScoringVersion = HealthRatingEngine.ScoringVersion,

            TotalHealthRating = result.TotalHealthRating,
            TotalPossibleScore = result.MaxHealthRating,
            Percentage = result.Percentage,

            EnergyStrengthStamina = Copy(result.FourStates.EnergyStrengthStamina),
            MentalEmotional = Copy(result.FourStates.MentalEmotional),
            Immunity = Copy(result.FourStates.Immunity),
            Longevity = Copy(result.FourStates.Longevity),

            Bmi = result.DerivedMetrics.Bmi,
            WaistToHeightRatio = result.DerivedMetrics.WHtR,
            WaistToHipRatio = result.DerivedMetrics.WHR,

            RestingHeartRate = input.RestingHeartRateBpm,
            HeartRateRecovery = input.HeartRateRecoveryBpm,
            BloodPressureSystolic = input.SystolicBpMmHg,
            BloodPressureDiastolic = input.DiastolicBpMmHg,

            SexAtAssessment = input.Sex.ToString(),
            AgeAtAssessment = input.Age,
            DateOfBirthAtAssessment = dateOfBirth,
            Height = input.HeightCm,
            HeightUnit = "cm",
            Weight = input.WeightKg,
            WeightUnit = "kg",
            Waist = input.WaistCm,
            WaistUnit = "cm",
            Hip = input.HipCm,
            HipUnit = "cm",
            BodyFatPercentage = input.BodyFatPercent,

            InputSnapshotJson = JsonSerializer.Serialize(input, JsonOptions),
        };

        foreach (var definition in ParameterCatalog.All)
        {
            if (!result.ParameterScores.TryGetValue(definition.Key, out var score))
            {
                throw new InvalidOperationException($"Engine returned no score for parameter '{definition.Key}'.");
            }

            var raw = definition.Extract(input, result.DerivedMetrics);
            assessment.ParameterScores.Add(new AssessmentParameterScore
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessment.Id,
                ParameterKey = definition.Key,
                ParameterName = definition.Name,
                SortOrder = definition.SortOrder,
                RawValue = raw.RawValue,
                RawText = raw.RawText,
                NormalizedValue = raw.NormalizedValue,
                Unit = definition.Unit,
                Score = score,
                CreatedAt = utcNow,
            });
        }

        return assessment;
    }

    private static StateScoreSnapshot Copy(StateScore state) => new()
    {
        RawScore = state.RawScore,
        MaxRawScore = state.MaxRawScore,
        NormalizedScore = state.NormalizedScore,
    };
}
