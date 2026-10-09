using System.Text.Json;
using HealthRater.Core.Models;
using HealthRater.Data.Entities;
using HealthRater.Data.Services;

namespace HealthRater.Api.Dtos;

public record StateScoresResponse(double EnergyStrengthStamina, double MentalEmotional, double Immunity, double Longevity);

/// <summary>Body-fat estimate preview for "I don't know my body fat".</summary>
public record BodyFatEstimateResponse(double BodyFatPercent, string Source, string Method, string MethodName, string Disclaimer);

/// <summary>History list row — no parameter data.</summary>
public record AssessmentSummaryResponse(
    Guid Id,
    DateTime CompletedAt,
    int TotalHealthRating,
    int MaxHealthRating,
    double Percentage,
    StateScoresResponse FourStates)
{
    public static AssessmentSummaryResponse From(AssessmentSummary s) => new(
        s.Id, s.CompletedAt, s.TotalHealthRating, s.TotalPossibleScore, s.Percentage,
        new StateScoresResponse(s.EnergyStrengthStamina, s.MentalEmotional, s.Immunity, s.Longevity));
}

/// <summary>Calendar cell: just enough to draw a day. <c>Date</c> is in the requested time zone.</summary>
public record CalendarEntryResponse(
    string Date,
    Guid AssessmentId,
    DateTime CompletedAt,
    int TotalHealthRating,
    int MaxHealthRating,
    double EnergyStrengthStamina,
    double MentalEmotional,
    double Immunity,
    double Longevity);

/// <summary>Body data and the profile context recorded at the time of the assessment.</summary>
public record BodySnapshotResponse(
    string SexAtAssessment,
    int AgeAtAssessment,
    DateOnly? DateOfBirthAtAssessment,
    double Height,
    string HeightUnit,
    double Weight,
    string WeightUnit,
    double Waist,
    string WaistUnit,
    double Hip,
    string HipUnit,
    double BodyFatPercentage,
    string BodyFatSource,
    string? BodyFatEstimationMethod);

public record CardiovascularSnapshotResponse(
    int RestingHeartRate,
    int HeartRateRecovery,
    int? PeakHeartRate,
    int? HeartRateAfter60Seconds,
    int BloodPressureSystolic,
    int BloodPressureDiastolic);

public record ParameterDetailResponse(
    string Key,
    string Name,
    int Order,
    double? RawValue,
    string? RawText,
    double? NormalizedValue,
    string? Unit,
    int Score);

/// <summary>
/// The full historical snapshot. The top-level result fields use the same names as the
/// live calculation response (totalHealthRating, percentage, fourStates, derivedMetrics,
/// parameterScores), so the frontend can render a saved assessment with the Results page.
/// Values are exactly as stored — nothing is recalculated. v1 assessments (39 parameters, max
/// 390, combined substance parameter) are returned with their own count and maximum.
/// </summary>
public record AssessmentDetailResponse(
    Guid Id,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string ScoringVersion,
    string ParameterSetVersion,
    int ParameterCount,
    int TotalHealthRating,
    int MaxHealthRating,
    double Percentage,
    FourStates FourStates,
    DerivedMetrics DerivedMetrics,
    Dictionary<string, int> ParameterScores,
    BodySnapshotResponse Body,
    CardiovascularSnapshotResponse Cardiovascular,
    List<ParameterDetailResponse> Parameters,
    JsonElement Input)
{
    public static AssessmentDetailResponse From(HealthAssessment a)
    {
        var parameters = a.ParameterScores.OrderBy(p => p.SortOrder).ToList();
        using var input = JsonDocument.Parse(a.InputSnapshotJson);

        return new AssessmentDetailResponse(
            a.Id,
            a.Status.ToString(),
            a.CreatedAt,
            a.CompletedAt,
            a.ScoringVersion,
            a.ParameterSetVersion,
            parameters.Count,
            a.TotalHealthRating,
            a.TotalPossibleScore,
            a.Percentage,
            new FourStates
            {
                EnergyStrengthStamina = State(a.EnergyStrengthStamina),
                MentalEmotional = State(a.MentalEmotional),
                Immunity = State(a.Immunity),
                Longevity = State(a.Longevity),
            },
            new DerivedMetrics { Bmi = a.Bmi, WHtR = a.WaistToHeightRatio, WHR = a.WaistToHipRatio },
            parameters.ToDictionary(p => p.ParameterKey, p => p.Score),
            new BodySnapshotResponse(
                a.SexAtAssessment, a.AgeAtAssessment, a.DateOfBirthAtAssessment, a.Height, a.HeightUnit, a.Weight, a.WeightUnit,
                a.Waist, a.WaistUnit, a.Hip, a.HipUnit, a.BodyFatPercentage,
                a.BodyFatSource.ToString(), a.BodyFatEstimationMethod),
            new CardiovascularSnapshotResponse(
                a.RestingHeartRate, a.HeartRateRecovery, a.PeakHeartRate, a.HeartRateAfter60Seconds,
                a.BloodPressureSystolic, a.BloodPressureDiastolic),
            parameters.Select(p => new ParameterDetailResponse(
                p.ParameterKey, p.ParameterName, p.SortOrder, p.RawValue, p.RawText, p.NormalizedValue, p.Unit, p.Score)).ToList(),
            input.RootElement.Clone());
    }

    private static StateScore State(StateScoreSnapshot s) => new()
    {
        RawScore = s.RawScore,
        MaxRawScore = s.MaxRawScore,
        NormalizedScore = s.NormalizedScore,
    };
}
