using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring.Scorers;

public static class BodyCompositionScorer
{
    public static int ScoreBmi(double bmi)
    {
        var penalty = Math.Abs(bmi - ScoringConfig.Bmi.IdealCenter) * ScoringConfig.Bmi.PenaltyPerUnit;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }

    public static int ScoreBodyFat(double bodyFatPercent, Sex sex)
    {
        var center = sex == Sex.Male
            ? ScoringConfig.BodyComposition.MaleIdealCenter
            : ScoringConfig.BodyComposition.FemaleIdealCenter;

        var penalty = Math.Abs(bodyFatPercent - center) * ScoringConfig.BodyComposition.PenaltyPerPercentPoint;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }

    public static int ScoreWHtR(double whtr)
    {
        var over = Math.Max(0, whtr - ScoringConfig.Whtr.IdealMax);
        var penalty = over * ScoringConfig.Whtr.PenaltyPerUnitOver;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }

    public static int ScoreWHR(double whr, Sex sex)
    {
        var idealMax = sex == Sex.Male ? ScoringConfig.Whr.MaleIdealMax : ScoringConfig.Whr.FemaleIdealMax;
        var over = Math.Max(0, whr - idealMax);
        var penalty = over * ScoringConfig.Whr.PenaltyPerUnitOver;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }
}
