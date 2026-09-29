namespace HealthRater.Core.Scoring;

/// <summary>
/// PROVISIONAL SCORING CONFIGURATION.
///
/// None of the thresholds below are clinically validated official HealthRater formulas —
/// no such formulas were supplied. They are reasonable, commonly-cited wellness
/// heuristics (e.g. WHO/ACSM-style BMI bands, standard blood-pressure categories,
/// ACSM push-up norm tables) intentionally centralized here, as plain static fields, so
/// they can be tuned without touching UI or controller code. Every field is grouped by
/// the scorer that consumes it. See README "Scoring Methodology" for the full write-up.
///
/// Sex- and age-specific references (body fat, WHR, functional power) are NOT here: they
/// live in Scoring/References/scoring-references.json so they can differ per sex and age
/// group and be replaced with official tables without code changes.
/// </summary>
public static class ScoringConfig
{
    // ---------------- BMI ----------------
    public static class Bmi
    {
        public const double IdealCenter = 21.75; // midpoint of the 18.5-24.9 "normal" band
        public const double PenaltyPerUnit = 0.5; // score lost per BMI unit away from center
    }

    // ---------------- WHtR ----------------
    public static class Whtr
    {
        public const double IdealMax = 0.50; // below this: score 10
        public const double PenaltyPerUnitOver = 40.0;
    }

    // ---------------- Blood pressure ----------------
    public static class BloodPressure
    {
        // 110/70 -> 10, ~165/95 -> 1 (per business rule; interpolated linearly per limb)
        public const double IdealSystolic = 110;
        public const double HighSystolic = 165;
        public const double IdealDiastolic = 70;
        public const double HighDiastolic = 95;
        public const double LowSystolicFloor = 90;  // below this, hypotension penalty kicks in
        public const double LowDiastolicFloor = 55;
    }

    // ---------------- Resting heart rate ----------------
    public static class RestingHeartRate
    {
        public const double IdealMax = 62; // <=62 bpm: score 10
        public const double PenaltyPerBpmOver = 0.22;
        public const double LowFloor = 40; // below this (non-athletic bradycardia risk): penalize
    }

    // ---------------- Heart rate recovery (1-min bpm drop) ----------------
    public static class HeartRateRecovery
    {
        public const double BpmForMaxScore = 30; // >=30 bpm drop -> score 10
    }

    // ---------------- Hydration ----------------
    public static class Hydration
    {
        public const double MlPerKgIdeal = 33.0; // ideal ~33ml water per kg bodyweight
        public const double PenaltyPerRatioUnitOff = 10.0;
    }

    // ---------------- Caffeine ----------------
    public static class Caffeine
    {
        public const double PenaltyPerServing = 1.5;
    }

    // ---------------- Junk food ----------------
    public static class JunkFood
    {
        public const double PenaltyPerServingPerWeek = 0.5;
    }

    // ---------------- Overeating ----------------
    public static class Overeating
    {
        public const double PenaltyPerEpisodePerWeek = 1.2;
    }

    // ---------------- Vegetables & fiber ----------------
    public static class VegetablesFiber
    {
        public const double PointsPerServing = 2.0; // 5 servings/day -> score 10
    }

    // ---------------- NEAT (daily steps) ----------------
    public static class Neat
    {
        public const double StepsPerScorePoint = 1000; // 10,000 steps -> score 10
    }

    // ---------------- Physical training ----------------
    public static class PhysicalTraining
    {
        public const double PointsPerSession = 1.8; // 4-6 sessions/week lands near 10
        public const double OvertrainingSessionsThreshold = 7;
        public const int OvertrainingScoreCap = 8;
    }

    // ---------------- Cooper 12-minute run ----------------
    public static class Cooper
    {
        public const double MetersForMaxScore = 3000; // >=3000m -> score 10 (per business rule)
    }

    // ---------------- Alcohol / tobacco / drugs ----------------
    public static class Substance
    {
        public static readonly Dictionary<Models.SubstanceFrequency, int> ScoreByFrequency = new()
        {
            [Models.SubstanceFrequency.Daily] = 1,
            [Models.SubstanceFrequency.SeveralTimesPerWeek] = 3,
            [Models.SubstanceFrequency.Weekly] = 5,
            [Models.SubstanceFrequency.Monthly] = 7,
            [Models.SubstanceFrequency.Rarely] = 8,
            [Models.SubstanceFrequency.Never] = 10,
        };
    }

    public static int Clamp1To10(double value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 1, 10);
    }
}
