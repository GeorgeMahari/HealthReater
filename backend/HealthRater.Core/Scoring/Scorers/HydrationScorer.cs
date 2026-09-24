namespace HealthRater.Core.Scoring.Scorers;

public static class HydrationScorer
{
    public static int Score(double dailyWaterIntakeLiters, double bodyWeightKg)
    {
        var idealLiters = (ScoringConfig.Hydration.MlPerKgIdeal * bodyWeightKg) / 1000.0;
        if (idealLiters <= 0) return 1;

        var ratio = dailyWaterIntakeLiters / idealLiters;
        var penalty = Math.Abs(ratio - 1) * ScoringConfig.Hydration.PenaltyPerRatioUnitOff;
        return ScoringConfig.Clamp1To10(10 - penalty);
    }
}
