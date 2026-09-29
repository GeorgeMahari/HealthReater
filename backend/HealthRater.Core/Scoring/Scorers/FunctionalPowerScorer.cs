using HealthRater.Core.Models;
using HealthRater.Core.Scoring.References;

namespace HealthRater.Core.Scoring.Scorers;

public static class FunctionalPowerScorer
{
    /// <summary>
    /// Total of push-ups, pull-ups and bodyweight squats, scored against the sex- and
    /// age-specific "functionalPower" reference (an "excellent" norm total).
    /// </summary>
    public static int Score(int pushUps, int pullUps, int bodyweightSquats, Sex sex, int age)
    {
        var total = pushUps + pullUps + bodyweightSquats;
        return ScoringReferenceCatalog.Current.Score(HealthRatingEngine.Keys.FunctionalPower, total, new ScoringContext(sex, age));
    }
}
