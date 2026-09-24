using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring.Scorers;

public static class FunctionalPowerScorer
{
    public static int Score(int pushUps, int pullUps, int bodyweightSquats, Sex sex, int age)
    {
        var total = pushUps + pullUps + bodyweightSquats;
        var norm = ScoringConfig.FunctionalPower.GetExcellentNormTotalReps(sex, age);
        if (norm <= 0) return 1;

        var ratio = total / norm;
        return ScoringConfig.Clamp1To10(ratio * 10);
    }
}
