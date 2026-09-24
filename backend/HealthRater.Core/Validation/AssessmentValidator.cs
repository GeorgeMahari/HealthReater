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

        if (input.WaistCm > 0 && input.HipCm > 0 && input.HeightCm <= 0)
        {
            outcome.Errors.Add("Height must be greater than zero to compute WHtR.");
        }

        return outcome;
    }
}
