using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring;

public static class DerivedMetricsCalculator
{
    public static DerivedMetrics Calculate(AssessmentInput input)
    {
        var heightM = input.HeightCm / 100.0;
        var bmi = input.WeightKg / (heightM * heightM);
        var whtr = input.WaistCm / input.HeightCm;
        var whr = input.WaistCm / input.HipCm;

        return new DerivedMetrics
        {
            Bmi = Math.Round(bmi, 2),
            WHtR = Math.Round(whtr, 3),
            WHR = Math.Round(whr, 3),
        };
    }
}
