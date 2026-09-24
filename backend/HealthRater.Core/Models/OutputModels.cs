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
    public int MaxHealthRating { get; set; } = 390;
    public double Percentage { get; set; }
    public Dictionary<string, int> ParameterScores { get; set; } = new();
    public FourStates FourStates { get; set; } = new();
    public DerivedMetrics DerivedMetrics { get; set; } = new();
}
