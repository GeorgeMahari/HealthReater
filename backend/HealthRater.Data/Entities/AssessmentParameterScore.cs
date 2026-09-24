namespace HealthRater.Data.Entities;

/// <summary>One of the 39 scored parameters of a <see cref="HealthAssessment"/>.</summary>
public class AssessmentParameterScore
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public HealthAssessment Assessment { get; set; } = null!;

    /// <summary>Stable machine key (e.g. "bmi", "cooper") — never the display name.</summary>
    public string ParameterKey { get; set; } = "";

    /// <summary>Display name at the time of the assessment.</summary>
    public string ParameterName { get; set; } = "";

    /// <summary>Official 1–39 order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Numeric input when the parameter has a single numeric value.</summary>
    public double? RawValue { get; set; }

    /// <summary>Non-numeric or composite input, e.g. "Male", "115/74", "40 push-ups · 12 pull-ups · 50 squats".</summary>
    public string? RawText { get; set; }

    /// <summary>Value the scorer actually evaluated when it differs from the raw input (e.g. hydration in ml/kg).</summary>
    public double? NormalizedValue { get; set; }

    public string? Unit { get; set; }

    /// <summary>1–10, enforced by a check constraint.</summary>
    public int Score { get; set; }

    public DateTime CreatedAt { get; set; }
}
