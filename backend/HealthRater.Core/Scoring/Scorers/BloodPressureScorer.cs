namespace HealthRater.Core.Scoring.Scorers;

public static class BloodPressureScorer
{
    public static int Score(int systolic, int diastolic)
    {
        var sysScore = ScoreLimb(systolic, ScoringConfig.BloodPressure.IdealSystolic,
            ScoringConfig.BloodPressure.HighSystolic, ScoringConfig.BloodPressure.LowSystolicFloor);

        var diaScore = ScoreLimb(diastolic, ScoringConfig.BloodPressure.IdealDiastolic,
            ScoringConfig.BloodPressure.HighDiastolic, ScoringConfig.BloodPressure.LowDiastolicFloor);

        return ScoringConfig.Clamp1To10((sysScore + diaScore) / 2.0);
    }

    private static double ScoreLimb(double value, double ideal, double high, double lowFloor)
    {
        if (value <= ideal)
        {
            // Mild hypotension penalty below the low floor; otherwise full marks.
            if (value < lowFloor)
            {
                var under = lowFloor - value;
                return Math.Clamp(10 - under * 0.3, 1, 10);
            }
            return 10;
        }

        var ratio = (value - ideal) / (high - ideal);
        return Math.Clamp(10 - ratio * 9, 1, 10);
    }
}
