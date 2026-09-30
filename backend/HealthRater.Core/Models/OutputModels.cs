namespace HealthRater.Core.Models;

public class DerivedMetrics
{
    public double Bmi { get; set; }
    public double WHtR { get; set; }
    public double WHR { get; set; }
}

/// <summary>
/// One of the four architectural health-state groupings. RawScore is the sum of the
/// contributing 1-10 parameter scores; NormalizedScore rescales that sum to 0-100 for
/// display. These groupings are configurable architecture, not clinically validated
/// predictive models.
/// </summary>
public class StateScore
{
    public int RawScore { get; set; }
    public int MaxRawScore { get; set; }
    public double NormalizedScore { get; set; }
}

public class FourStates
{
    public StateScore EnergyStrengthStamina { get; set; } = new();
    public StateScore MentalEmotional { get; set; } = new();
    public StateScore Immunity { get; set; } = new();
    public StateScore Longevity { get; set; } = new();
}

public class HealthRatingResult
{
    public int TotalHealthRating { get; set; }
    /// <summary>ParameterSet.MaxTotalScore for the parameter set used (410 for v2).</summary>
    public int MaxHealthRating { get; set; }

    /// <summary>Number of scored parameters (ParameterSet.Count).</summary>
    public int ParameterCount { get; set; }

    public string ParameterSetVersion { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public Dictionary<string, int> ParameterScores { get; set; } = new();
    public FourStates FourStates { get; set; } = new();
    public DerivedMetrics DerivedMetrics { get; set; } = new();

    /// <summary>The body-fat value that was scored and whether it was measured or estimated.</summary>
    public BodyFatInfo BodyFat { get; set; } = new();

    /// <summary>The Heart Rate Recovery measurement that was scored.</summary>
    public HeartRateRecoveryInfo HeartRateRecovery { get; set; } = new();
}

public class BodyFatInfo
{
    public double Percent { get; set; }
    public BodyFatSource Source { get; set; }

    /// <summary>Estimation method id (e.g. "Deurenberg1991"); null when measured.</summary>
    public string? EstimationMethod { get; set; }
}

public class HeartRateRecoveryInfo
{
    public int PeakHeartRateBpm { get; set; }
    public int HeartRateAfter60sBpm { get; set; }

    /// <summary>Peak minus after 60 s.</summary>
    public int RecoveryBpm { get; set; }
}
