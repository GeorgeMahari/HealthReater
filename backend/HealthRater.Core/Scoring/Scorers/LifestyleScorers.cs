using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring.Scorers;

public static class CaffeineScorer
{
    public static int Score(double servingsPerDay) =>
        ScoringConfig.Clamp1To10(10 - servingsPerDay * ScoringConfig.Caffeine.PenaltyPerServing);
}

public static class JunkFoodScorer
{
    public static int Score(double servingsPerWeek) =>
        ScoringConfig.Clamp1To10(10 - servingsPerWeek * ScoringConfig.JunkFood.PenaltyPerServingPerWeek);
}

public static class OvereatingScorer
{
    public static int Score(double episodesPerWeek) =>
        ScoringConfig.Clamp1To10(10 - episodesPerWeek * ScoringConfig.Overeating.PenaltyPerEpisodePerWeek);
}

public static class VegetablesFiberScorer
{
    public static int Score(double servingsPerDay) =>
        ScoringConfig.Clamp1To10(servingsPerDay * ScoringConfig.VegetablesFiber.PointsPerServing);
}

public static class NeatScorer
{
    public static int Score(int dailySteps) =>
        ScoringConfig.Clamp1To10(dailySteps / ScoringConfig.Neat.StepsPerScorePoint);
}

public static class PhysicalTrainingScorer
{
    public static int Score(double sessionsPerWeek)
    {
        if (sessionsPerWeek > ScoringConfig.PhysicalTraining.OvertrainingSessionsThreshold)
        {
            return ScoringConfig.PhysicalTraining.OvertrainingScoreCap;
        }
        return ScoringConfig.Clamp1To10(sessionsPerWeek * ScoringConfig.PhysicalTraining.PointsPerSession);
    }
}

public static class SubstanceScorer
{
    public static int ScoreAlcohol(SubstanceFrequency frequency) =>
        ScoringConfig.Substance.AlcoholScoreByFrequency[frequency];

    public static int ScoreTobacco(SubstanceFrequency frequency) =>
        ScoringConfig.Substance.TobaccoScoreByFrequency[frequency];

    public static int ScoreDrugs(SubstanceFrequency frequency) =>
        ScoringConfig.Substance.DrugsScoreByFrequency[frequency];
}

public static class CooperScorer
{
    public static int Score(int distanceMeters) =>
        ScoringConfig.Clamp1To10((distanceMeters / ScoringConfig.Cooper.MetersForMaxScore) * 10);
}
