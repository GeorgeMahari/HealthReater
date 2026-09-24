namespace HealthRater.Core.Scoring.Scorers;

public static class HeartRateScorer
{
    public static int ScoreResting(int restingHeartRateBpm)
    {
        if (restingHeartRateBpm < ScoringConfig.RestingHeartRate.LowFloor)
        {
            var under = ScoringConfig.RestingHeartRate.LowFloor - restingHeartRateBpm;
            return ScoringConfig.Clamp1To10(10 - under * 0.3);
        }

        if (restingHeartRateBpm <= ScoringConfig.RestingHeartRate.IdealMax)
        {
            return 10;
        }

        var over = restingHeartRateBpm - ScoringConfig.RestingHeartRate.IdealMax;
        return ScoringConfig.Clamp1To10(10 - over * ScoringConfig.RestingHeartRate.PenaltyPerBpmOver);
    }

    public static int ScoreRecovery(int heartRateRecoveryBpm)
    {
        var score = (heartRateRecoveryBpm / ScoringConfig.HeartRateRecovery.BpmForMaxScore) * 10;
        return ScoringConfig.Clamp1To10(score);
    }
}
