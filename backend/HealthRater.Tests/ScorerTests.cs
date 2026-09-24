using HealthRater.Core.Models;
using HealthRater.Core.Scoring.Scorers;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class ScorerTests
{
    public static List<(string, Action)> All() => new()
    {
        ("BloodPressure: 110/70 scores 10 (business rule)", () =>
        {
            Assert.Equal(10, BloodPressureScorer.Score(110, 70), "BP 110/70");
        }),

        ("BloodPressure: ~165/95 scores 1 (business rule)", () =>
        {
            Assert.Equal(1, BloodPressureScorer.Score(165, 95), "BP 165/95");
        }),

        ("BloodPressure: 130/85 scores mid-range", () =>
        {
            var score = BloodPressureScorer.Score(130, 85);
            Assert.InRange(score, 4, 8, "BP 130/85 mid-range");
        }),

        ("BloodPressure: severe hypertension caps at floor of 1", () =>
        {
            Assert.Equal(1, BloodPressureScorer.Score(220, 130), "BP severe hypertension");
        }),

        ("Cooper: >=3000m scores 10", () =>
        {
            Assert.Equal(10, CooperScorer.Score(3000), "Cooper 3000m");
            Assert.Equal(10, CooperScorer.Score(4000), "Cooper 4000m capped");
        }),

        ("Cooper: 1500m scores 5", () =>
        {
            Assert.Equal(5, CooperScorer.Score(1500), "Cooper 1500m");
        }),

        ("Cooper: 0m scores floor of 1", () =>
        {
            Assert.Equal(1, CooperScorer.Score(0), "Cooper 0m");
        }),

        ("FunctionalPower: young male at norm reps scores 10", () =>
        {
            // norm for male <=29 is 150 total reps
            var score = FunctionalPowerScorer.Score(60, 20, 70, Sex.Male, 25);
            Assert.Equal(10, score, "Functional power at norm");
        }),

        ("FunctionalPower: zero reps scores floor of 1", () =>
        {
            var score = FunctionalPowerScorer.Score(0, 0, 0, Sex.Male, 25);
            Assert.Equal(1, score, "Functional power zero reps");
        }),

        ("FunctionalPower: older age uses a lower (easier) norm than younger age", () =>
        {
            var youngScore = FunctionalPowerScorer.Score(50, 5, 50, Sex.Male, 25);
            var oldScore = FunctionalPowerScorer.Score(50, 5, 50, Sex.Male, 65);
            Assert.True(oldScore >= youngScore, "Age-adjusted functional power norm");
        }),

        ("RestingHeartRate: 58bpm (below ideal max) scores 10", () =>
        {
            Assert.Equal(10, HeartRateScorer.ScoreResting(58), "Resting HR 58");
        }),

        ("RestingHeartRate: 110bpm scores low", () =>
        {
            var score = HeartRateScorer.ScoreResting(110);
            Assert.InRange(score, 1, 4, "Resting HR 110");
        }),

        ("HeartRateRecovery: 30bpm drop scores 10", () =>
        {
            Assert.Equal(10, HeartRateScorer.ScoreRecovery(30), "HRR 30bpm");
        }),

        ("Substance: never scores 10, daily scores 1", () =>
        {
            Assert.Equal(10, SubstanceScorer.Score(SubstanceFrequency.Never), "Substance never");
            Assert.Equal(1, SubstanceScorer.Score(SubstanceFrequency.Daily), "Substance daily");
        }),

        ("PhysicalTraining: 5 sessions/week lands near max", () =>
        {
            var score = PhysicalTrainingScorer.Score(5);
            Assert.InRange(score, 8, 10, "Training 5 sessions/week");
        }),

        ("PhysicalTraining: overtraining (>7 sessions) is capped, not maxed", () =>
        {
            var score = PhysicalTrainingScorer.Score(14);
            Assert.Equal(8, score, "Overtraining cap");
        }),
    };
}
