using HealthRater.Core.Scoring;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class EngineTests
{
    public static List<(string, Action)> All() => new()
    {
        ("ParameterSet: 41 parameters, 10 points each, maximum 410", () =>
        {
            Assert.Equal(41, ParameterSet.Count, "Parameter count");
            Assert.Equal(10, ParameterSet.MaxScorePerParameter, "Max per parameter");
            Assert.Equal(410, ParameterSet.MaxTotalScore, "Max total");
            Assert.Equal(41, ParameterSet.Keys.Distinct().Count(), "Keys are unique");
            Assert.Equal("v2-41", ParameterSet.Version, "Version");
        }),

        ("TotalHealthRating: exactly ParameterSet.Count (41) parameters are scored", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.Equal(ParameterSet.Count, result.ParameterScores.Count, "Parameter count");
            Assert.True(ParameterSet.Keys.SequenceEqual(result.ParameterScores.Keys), "Same keys and order as ParameterSet");
            Assert.Equal(ParameterSet.Count, result.ParameterCount, "Result reports the count");
            Assert.Equal(ParameterSet.Version, result.ParameterSetVersion, "Result reports the version");
        }),

        ("TotalHealthRating: sum is within [41, 410] bounds", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.InRange(result.TotalHealthRating, ParameterSet.Count, ParameterSet.MaxTotalScore, "Total health rating bounds");
        }),

        ("TotalHealthRating: is the literal sum of the parameter scores (not a weighted average)", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var sum = result.ParameterScores.Values.Sum();
            Assert.Equal(sum, result.TotalHealthRating, "Total equals sum of parameter scores");
        }),

        ("TotalHealthRating: worst-case profile lands near the floor of 41", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.WorstCase());
            Assert.InRange(result.TotalHealthRating, ParameterSet.Count, 130, "Worst-case total near floor");
        }),

        ("TotalHealthRating: max is 410 (ParameterSet.MaxTotalScore)", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.Equal(410, result.MaxHealthRating, "Max health rating");
            Assert.Equal(ParameterSet.MaxTotalScore, result.MaxHealthRating, "Max from the single source of truth");
        }),

        ("Percentage: matches total/410*100", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var expected = Math.Round(result.TotalHealthRating / 410.0 * 100, 2);
            Assert.Equal(expected, result.Percentage, "Percentage calculation");
        }),

        ("FourStates: healthy profile scores higher than worst-case in every state", () =>
        {
            var healthy = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var worst = HealthRatingEngine.Calculate(SampleProfile.WorstCase());

            Assert.True(healthy.FourStates.EnergyStrengthStamina.NormalizedScore >
                        worst.FourStates.EnergyStrengthStamina.NormalizedScore, "Energy state comparison");
            Assert.True(healthy.FourStates.MentalEmotional.NormalizedScore >
                        worst.FourStates.MentalEmotional.NormalizedScore, "Mental state comparison");
            Assert.True(healthy.FourStates.Immunity.NormalizedScore >
                        worst.FourStates.Immunity.NormalizedScore, "Immunity state comparison");
            Assert.True(healthy.FourStates.Longevity.NormalizedScore >
                        worst.FourStates.Longevity.NormalizedScore, "Longevity state comparison");
        }),

        ("FourStates: normalized scores are within 0-100", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.InRange(result.FourStates.EnergyStrengthStamina.NormalizedScore, 0, 100, "Energy normalized range");
            Assert.InRange(result.FourStates.MentalEmotional.NormalizedScore, 0, 100, "Mental normalized range");
            Assert.InRange(result.FourStates.Immunity.NormalizedScore, 0, 100, "Immunity normalized range");
            Assert.InRange(result.FourStates.Longevity.NormalizedScore, 0, 100, "Longevity normalized range");
        }),

        ("FourStates: a parameter can contribute to multiple states (sleep quality)", () =>
        {
            var inEnergy = FourStateCalculator.EnergyStrengthStaminaParams.Contains(HealthRatingEngine.Keys.SleepQuality);
            var inMental = FourStateCalculator.MentalEmotionalParams.Contains(HealthRatingEngine.Keys.SleepQuality);
            Assert.True(inEnergy && inMental, "Sleep quality shared across states");
        }),
    };
}
