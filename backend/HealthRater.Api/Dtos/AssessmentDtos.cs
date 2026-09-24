using System.Text.Json;
using HealthRater.Core.Models;
using HealthRater.Data.Entities;
using HealthRater.Data.Services;

namespace HealthRater.Api.Dtos;

public record StateScoresResponse(double EnergyStrengthStamina, double MentalEmotional, double Immunity, double Longevity);

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
    double EnergyStrengthStamina,
    double MentalEmotional,
    double Immunity,
    double Longevity);

public record BodySnapshotResponse(
    string Sex,
    int AgeAtAssessment,
    double Height,
    string HeightUnit,
    double Weight,
    string WeightUnit,
    double Waist,
    string WaistUnit,
    double Hip,
    string HipUnit,
    double BodyFatPercentage);

public record CardiovascularSnapshotResponse(
    int RestingHeartRate,
    int HeartRateRecovery,
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
/// Values are exactly as stored — nothing is recalculated.
/// </summary>
public record AssessmentDetailResponse(
    Guid Id,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string ScoringVersion,
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
                a.Sex, a.AgeAtAssessment, a.Height, a.HeightUnit, a.Weight, a.WeightUnit,
                a.Waist, a.WaistUnit, a.Hip, a.HipUnit, a.BodyFatPercentage),
            new CardiovascularSnapshotResponse(
                a.RestingHeartRate, a.HeartRateRecovery, a.BloodPressureSystolic, a.BloodPressureDiastolic),
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
