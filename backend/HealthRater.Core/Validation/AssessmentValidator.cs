using System.ComponentModel.DataAnnotations;
using HealthRater.Core.Models;

namespace HealthRater.Core.Validation;

public class ValidationOutcome
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}

public static class AssessmentValidator
{
    public static ValidationOutcome Validate(AssessmentInput input)
    {
        var outcome = new ValidationOutcome();

        var context = new ValidationContext(input);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, context, results, validateAllProperties: true))
        {
            outcome.Errors.AddRange(results.Select(r => r.ErrorMessage ?? "Invalid value."));
        }

        // Cross-field business rules that [Range] alone can't express.
        if (input.SystolicBpMmHg <= input.DiastolicBpMmHg)
        {
            outcome.Errors.Add("Systolic blood pressure must be greater than diastolic blood pressure.");
        }

        if (input.BodyFatPercent is null)
        {
            outcome.Errors.Add(input.BodyFatEstimationError
                ?? "Body fat percentage is required, or choose \"I don't know\" to have it estimated.");
        }

        // Heart Rate Recovery: HR 60 s after stopping can't be above the peak, and a drop of more
        // than 100 bpm in one minute isn't physiologically plausible (likely a typo).
        if (input.PeakHeartRateBpm > 0 && input.HeartRateAfter60sBpm > input.PeakHeartRateBpm)
        {
            outcome.Errors.Add("Heart rate 60 seconds after exercise can't be higher than the peak heart rate.");
        }
        else if (input.HeartRateRecoveryBpm > 100)
        {
            outcome.Errors.Add("A heart-rate drop of more than 100 bpm in 60 seconds isn't plausible. Please re-check both readings.");
        }

        if (input.PeakHeartRateBpm > 0 && input.RestingHeartRateBpm > 0 && input.PeakHeartRateBpm <= input.RestingHeartRateBpm)
        {
            outcome.Errors.Add("Peak heart rate after exercise must be higher than the resting heart rate.");
        }

        if (input.WaistCm > 0 && input.HipCm > 0 && input.HeightCm <= 0)
        {
            outcome.Errors.Add("Height must be greater than zero to compute WHtR.");
        }

        return outcome;
    }
}
