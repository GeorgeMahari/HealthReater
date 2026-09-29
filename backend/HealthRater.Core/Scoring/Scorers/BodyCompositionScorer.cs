using HealthRater.Core.Models;
using HealthRater.Core.Scoring.References;

namespace HealthRater.Core.Scoring.Scorers;

public static class BodyCompositionScorer
{
    public static int ScoreBmi(double bmi)
    {
        var penalty = Math.Abs(bmi - ScoringConfig.Bmi.IdealCenter) * ScoringConfig.Bmi.PenaltyPerUnit;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }

    /// <summary>Sex- and age-specific: uses the configured "bodyFat" scoring reference.</summary>
    public static int ScoreBodyFat(double bodyFatPercent, ScoringContext context) =>
        ScoringReferenceCatalog.Current.Score(HealthRatingEngine.Keys.BodyFat, bodyFatPercent, context);

    public static int ScoreWHtR(double whtr)
    {
        var over = Math.Max(0, whtr - ScoringConfig.Whtr.IdealMax);
        var penalty = over * ScoringConfig.Whtr.PenaltyPerUnitOver;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }

    /// <summary>Sex- and age-specific: uses the configured "whr" scoring reference.</summary>
    public static int ScoreWHR(double whr, ScoringContext context) =>
        ScoringReferenceCatalog.Current.Score(HealthRatingEngine.Keys.WHR, whr, context);
}
