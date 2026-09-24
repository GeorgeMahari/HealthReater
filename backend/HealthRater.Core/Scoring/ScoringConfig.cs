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
/// </summary>
public static class ScoringConfig
{
    // ---------------- BMI ----------------
    public static class Bmi
    {
        public const double IdealCenter = 21.75; // midpoint of the 18.5-24.9 "normal" band
        public const double PenaltyPerUnit = 0.5; // score lost per BMI unit away from center
    }

    // ---------------- Body composition (body fat %) ----------------
    public static class BodyComposition
    {
        public const double MaleIdealCenter = 15.0;
        public const double FemaleIdealCenter = 23.0;
        public const double PenaltyPerPercentPoint = 0.4;
    }

    // ---------------- WHtR ----------------
    public static class Whtr
    {
        public const double IdealMax = 0.50; // below this: score 10
        public const double PenaltyPerUnitOver = 40.0;
    }

    // ---------------- WHR ----------------
    public static class Whr
    {
        public const double MaleIdealMax = 0.90;
        public const double FemaleIdealMax = 0.80;
        public const double PenaltyPerUnitOver = 20.0;
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

    // ---------------- Functional power (push-ups + pull-ups + bodyweight squats) ----------------
    // Provisional reps-total norm table considered "excellent" (score 10) by sex/age band.
    // Ratio of actual reps to this norm (capped) maps linearly to 1-10.
    public static class FunctionalPower
    {
        public static double GetExcellentNormTotalReps(Models.Sex sex, int age)
        {
            // Rough composite norm (pushups+pullups+squats) inspired by common
            // age/sex-adjusted fitness norm tables. Provisional & configurable.
            if (sex == Models.Sex.Male)
            {
                return age switch
                {
                    <= 29 => 150,
                    <= 39 => 130,
                    <= 49 => 110,
                    <= 59 => 90,
                    _ => 70
                };
            }
            return age switch
            {
                <= 29 => 110,
                <= 39 => 95,
                <= 49 => 80,
                <= 59 => 65,
                _ => 50
            };
        }
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
