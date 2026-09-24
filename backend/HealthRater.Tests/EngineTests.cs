using HealthRater.Core.Scoring;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class EngineTests
{
    public static List<(string, Action)> All() => new()
    {
        ("TotalHealthRating: exactly 39 parameters are scored", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.Equal(39, result.ParameterScores.Count, "Parameter count");
        }),

        ("TotalHealthRating: sum is within [39, 390] bounds", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.InRange(result.TotalHealthRating, 39, 390, "Total health rating bounds");
        }),

        ("TotalHealthRating: is the literal sum of the 39 parameter scores (not a weighted average)", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var sum = result.ParameterScores.Values.Sum();
            Assert.Equal(sum, result.TotalHealthRating, "Total equals sum of parameter scores");
        }),

        ("TotalHealthRating: worst-case profile lands near the floor of 39", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.WorstCase());
            Assert.InRange(result.TotalHealthRating, 39, 120, "Worst-case total near floor");
        }),

        ("TotalHealthRating: max is always 390", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            Assert.Equal(390, result.MaxHealthRating, "Max health rating");
        }),

        ("Percentage: matches total/390*100", () =>
        {
            var result = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var expected = Math.Round(result.TotalHealthRating / 390.0 * 100, 2);
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
