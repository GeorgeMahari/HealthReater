using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class DerivedMetricsTests
{
    public static List<(string, Action)> All() => new()
    {
        ("BMI: 180cm/78kg -> ~24.07", () =>
        {
            var input = SampleProfile.Healthy();
            var derived = DerivedMetricsCalculator.Calculate(input);
            Assert.Equal(24.07, derived.Bmi, "BMI value");
        }),

        ("BMI: known reference 70kg/175cm -> ~22.86", () =>
        {
            var input = SampleProfile.Healthy();
            input.WeightKg = 70;
            input.HeightCm = 175;
            var derived = DerivedMetricsCalculator.Calculate(input);
            Assert.InRange(derived.Bmi, 22.85, 22.87, "BMI reference value");
        }),

        ("WHtR: waist/height ratio computed correctly", () =>
        {
            var input = SampleProfile.Healthy(); // waist=82, height=180
            var derived = DerivedMetricsCalculator.Calculate(input);
            Assert.Equal(Math.Round(82.0 / 180.0, 3), derived.WHtR, "WHtR value");
        }),

        ("WHR: waist/hip ratio computed correctly", () =>
        {
            var input = SampleProfile.Healthy(); // waist=82, hip=98
            var derived = DerivedMetricsCalculator.Calculate(input);
            Assert.Equal(Math.Round(82.0 / 98.0, 3), derived.WHR, "WHR value");
        }),

        ("Unit sanity: height cm -> m conversion feeds BMI (2m/100kg -> BMI 25)", () =>
        {
            var input = SampleProfile.Healthy();
            input.HeightCm = 200;
            input.WeightKg = 100;
            var derived = DerivedMetricsCalculator.Calculate(input);
            Assert.Equal(25.0, derived.Bmi, "BMI unit conversion");
        }),
    };
}
