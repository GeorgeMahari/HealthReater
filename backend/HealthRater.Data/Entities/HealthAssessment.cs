using HealthRater.Core.Models;

namespace HealthRater.Data.Entities;

public enum AssessmentStatus
{
    Draft = 0,
    Completed = 1,
}

/// <summary>Score of one of the four health states, as calculated at assessment time.</summary>
public class StateScoreSnapshot
{
    public int RawScore { get; set; }
    public int MaxRawScore { get; set; }
    /// <summary>0–100.</summary>
    public double NormalizedScore { get; set; }
}

/// <summary>
/// One HealthRater scan. An immutable historical snapshot: the inputs, derived metrics
/// and every score are stored exactly as calculated at the time, so later changes to the
/// scoring configuration never alter past results. Rows are never updated in place by a
/// new scan — each scan is a new row.
/// </summary>
public class HealthAssessment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public AssessmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>UTC. Null while Draft.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Scoring configuration version used (<c>HealthRatingEngine.ScoringVersion</c>).</summary>
    public string ScoringVersion { get; set; } = "";

    /// <summary>
    /// Parameter set used (<c>ParameterSet.Version</c>): "v2-41" for new assessments, "v1-39" for
    /// assessments saved before alcohol, tobacco and drugs were split. Old rows keep their 39
    /// parameter scores and 390 maximum.
    /// </summary>
    public string ParameterSetVersion { get; set; } = "";

    // ---- Totals ----
    public int TotalHealthRating { get; set; }
    public int TotalPossibleScore { get; set; }
    public double Percentage { get; set; }

    // ---- Four states ----
    public StateScoreSnapshot EnergyStrengthStamina { get; set; } = new();
    public StateScoreSnapshot MentalEmotional { get; set; } = new();
    public StateScoreSnapshot Immunity { get; set; } = new();
    public StateScoreSnapshot Longevity { get; set; } = new();

    // ---- Derived metrics ----
    public double Bmi { get; set; }
    public double WaistToHeightRatio { get; set; }
    public double WaistToHipRatio { get; set; }

    // ---- Cardiovascular ----
    public int RestingHeartRate { get; set; }
    /// <summary>Heart Rate Recovery in bpm (peak minus HR 60 s after stopping).</summary>
    public int HeartRateRecovery { get; set; }
    /// <summary>Peak heart rate of the HRR protocol (null for assessments before the standardized protocol).</summary>
    public int? PeakHeartRate { get; set; }
    /// <summary>Heart rate exactly 60 s after exercise stopped (null for older assessments).</summary>
    public int? HeartRateAfter60Seconds { get; set; }
    public int BloodPressureSystolic { get; set; }
    public int BloodPressureDiastolic { get; set; }

    // ---- Profile context snapshot (taken from the profile at completion; never recalculated) ----
    public string SexAtAssessment { get; set; } = "";
    public int AgeAtAssessment { get; set; }
    /// <summary>Date of birth on the profile when the assessment was completed (null for older records).</summary>
    public DateOnly? DateOfBirthAtAssessment { get; set; }

    // ---- Body snapshot ----
    public double Height { get; set; }
    public string HeightUnit { get; set; } = "cm";
    public double Weight { get; set; }
    public string WeightUnit { get; set; } = "kg";
    public double Waist { get; set; }
    public string WaistUnit { get; set; } = "cm";
    public double Hip { get; set; }
    public string HipUnit { get; set; } = "cm";
    public double BodyFatPercentage { get; set; }
    /// <summary>Measured (entered by the user) or Estimated (calculated by HealthRater).</summary>
    public BodyFatSource BodyFatSource { get; set; } = BodyFatSource.Measured;
    /// <summary>Estimation method id when estimated, e.g. "Deurenberg1991".</summary>
    public string? BodyFatEstimationMethod { get; set; }

    /// <summary>
    /// The complete AssessmentInput as submitted (JSON), so the original answers can be
    /// shown exactly as entered even for inputs that don't have a dedicated column.
    /// </summary>
    public string InputSnapshotJson { get; set; } = "";

    public List<AssessmentParameterScore> ParameterScores { get; set; } = new();
}
