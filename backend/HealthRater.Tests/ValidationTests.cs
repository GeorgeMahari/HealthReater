using HealthRater.Core.Validation;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class ValidationTests
{
    public static List<(string, Action)> All() => new()
    {
        ("Validation: a healthy sample profile is valid", () =>
        {
            var outcome = AssessmentValidator.Validate(SampleProfile.Healthy());
            Assert.True(outcome.IsValid, "Healthy profile validity");
        }),

        ("Validation: age below 18 is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.Age = 17;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Age below 18 rejected");
        }),

        ("Validation: age above 100 is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.Age = 101;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Age above 100 rejected");
        }),

        ("Validation: a 1-10 field outside range is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.EnergyLevel = 11;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Out-of-range 1-10 field rejected");
        }),

        ("Validation: body fat above 100% is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.BodyFatPercent = 150;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Body fat > 100% rejected");
        }),

        ("Validation: negative anthropometric value is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.WaistCm = -5;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Negative waist rejected");
        }),

        ("Validation: systolic <= diastolic is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.SystolicBpMmHg = 90;
            input.DiastolicBpMmHg = 95;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Systolic <= diastolic rejected");
        }),

        ("Validation: negative Cooper distance is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.CooperDistanceMeters = -100;
            var outcome = AssessmentValidator.Validate(input);
            Assert.False(outcome.IsValid, "Negative Cooper distance rejected");
        }),
    };
}
