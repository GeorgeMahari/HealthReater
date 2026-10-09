using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring.BodyFat;

/// <summary>Everything an estimator may use. Current estimators need only some of it.</summary>
public record BodyFatEstimationInput(
    Sex Sex,
    int Age,
    double HeightCm,
    double WeightKg,
    double Bmi,
    double WaistCm,
    double HipCm);

/// <summary>
/// A body-fat estimation method. Implementations must be documented, published equations —
/// never ad-hoc formulas. Swap the method in <see cref="BodyFatEstimation.Current"/> without
/// touching the UI or the scoring engine (e.g. RFM, CUN-BAE, or the US Navy method if neck
/// circumference is ever collected).
/// </summary>
public interface IBodyFatEstimator
{
    /// <summary>Stable id stored with each estimated assessment, e.g. "Deurenberg1991".</summary>
    string MethodId { get; }

    /// <summary>Short human-readable name, e.g. "Deurenberg et al. (1991) BMI equation".</summary>
    string MethodName { get; }

    double Estimate(BodyFatEstimationInput input);
}

/// <summary>
/// Deurenberg P, Weststrate JA, Seidell JC. "Body mass index as a measure of body fatness:
/// age- and sex-specific prediction formulas." Br J Nutr. 1991;65(2):105–114.
///
///   Body fat % = 1.20 × BMI + 0.23 × age − 10.8 × sex − 5.4      (sex: 1 = male, 0 = female)
///
/// Chosen because it needs only data HealthRater always has (BMI from height/weight, age,
/// sex). The US Navy circumference method was NOT used because it requires neck
/// circumference, which HealthRater does not collect.
///
/// Limitations: a population-based adult equation. It cannot tell muscle from fat, so it
/// overestimates body fat in very muscular people and can be off for unusual body
/// compositions, the very old, or extreme BMIs. It is an estimate, not a measurement.
/// </summary>
public sealed class DeurenbergBodyFatEstimator : IBodyFatEstimator
{
    public string MethodId => "Deurenberg1991";
    public string MethodName => "Deurenberg et al. (1991) equation";

    public double Estimate(BodyFatEstimationInput input)
    {
        var sex = input.Sex == Sex.Male ? 1 : 0;
        return 1.20 * input.Bmi + 0.23 * input.Age - 10.8 * sex - 5.4;
    }
}

public record BodyFatEstimate(double BodyFatPercent, string MethodId, string MethodName);

/// <summary>Runs the configured estimator and rejects results that can't be real.</summary>
public static class BodyFatEstimation
{
    /// <summary>Plausible adult body-fat range; estimates outside it are refused, not clamped.</summary>
    public const double MinPlausiblePercent = 2;
    public const double MaxPlausiblePercent = 75;

    public const string Disclaimer =
        "Body fat is estimated from available body measurements and demographic data. " +
        "This is an estimate and may differ from direct body-composition measurements.";

    public static IBodyFatEstimator Current { get; set; } = new DeurenbergBodyFatEstimator();

    /// <summary>
    /// The estimate rounded to 0.01 %, or an error message when the inputs can't produce a
    /// plausible value (NaN, infinity, negative or outside the plausible range).
    /// </summary>
    public static (BodyFatEstimate? Estimate, string? Error) TryEstimate(BodyFatEstimationInput input)
    {
        var value = Current.Estimate(input);
        if (double.IsNaN(value) || double.IsInfinity(value) || value < MinPlausiblePercent || value > MaxPlausiblePercent)
        {
            return (null,
                "Body fat couldn't be estimated reliably from your height, weight and age. " +
                "Please enter a measured body-fat percentage instead.");
        }
        return (new BodyFatEstimate(Math.Round(value, 2), Current.MethodId, Current.MethodName), null);
    }

    /// <summary>Builds the estimator input from a scoring input (BMI from height and weight).</summary>
    public static BodyFatEstimationInput InputFor(AssessmentInput input)
    {
        var heightM = input.HeightCm / 100.0;
        var bmi = heightM > 0 ? input.WeightKg / (heightM * heightM) : double.NaN;
        return new BodyFatEstimationInput(input.Sex, input.Age, input.HeightCm, input.WeightKg, bmi, input.WaistCm, input.HipCm);
    }
}
