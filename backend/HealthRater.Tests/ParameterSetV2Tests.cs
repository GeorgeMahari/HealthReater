using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
using HealthRater.Core.Scoring.BodyFat;
using HealthRater.Core.Scoring.Scorers;
using HealthRater.Core.Validation;
using HealthRater.Data.Entities;
using HealthRater.Data.Services;
using HealthRater.Data.Snapshots;
using HealthRater.Tests.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using K = HealthRater.Core.Scoring.HealthRatingEngine.Keys;

namespace HealthRater.Tests;

/// <summary>
/// The v2 parameter set: alcohol / tobacco / drugs as three parameters (41, max 410), body fat
/// "I don't know" estimation, the standardized Heart Rate Recovery protocol, and compatibility
/// with assessments saved under v1 (39 parameters, max 390).
/// </summary>
public static class ParameterSetV2Tests
{
    public static List<(string, Action)> All() => new()
    {
        // ---------- Body fat estimation ----------
        ("BodyFat: Deurenberg formula matches the published equation", () =>
        {
            var estimator = new DeurenbergBodyFatEstimator();
            // Male, 28, BMI 24.07: 1.20×24.07 + 0.23×28 − 10.8 − 5.4 = 19.124
            Assert.Equal(19.124, Math.Round(estimator.Estimate(new BodyFatEstimationInput(Sex.Male, 28, 180, 78, 24.07, 82, 98)), 3), "Male");
            // Female, 34, BMI 22.68: 1.20×22.68 + 0.23×34 − 5.4 = 29.636
            Assert.Equal(29.636, Math.Round(estimator.Estimate(new BodyFatEstimationInput(Sex.Female, 34, 168, 64, 22.68, 74, 98)), 3), "Female");
            Assert.Equal("Deurenberg1991", estimator.MethodId, "Method id");
        }),

        ("BodyFat: unknown body fat is estimated from the profile and marked Estimated", () =>
        {
            var answers = SampleProfile.Healthy();
            answers.BodyFatPercent = null;
            var input = AssessmentInput.From(answers, new ProfileSnapshot(Sex.Male, 28, 180, 78));
            Assert.Equal(BodyFatSource.Estimated, input.BodyFatSource, "Source");
            Assert.Equal("Deurenberg1991", input.BodyFatEstimationMethod, "Method");
            Assert.Equal(19.13, input.BodyFatPercent, "Estimated value (BMI 24.074…)");
            Assert.True(AssessmentValidator.Validate(input).IsValid, "Estimated input is valid");
        }),

        ("BodyFat: a known value is used as entered and marked Measured", () =>
        {
            var input = AssessmentInput.From(SampleProfile.Healthy(), new ProfileSnapshot(Sex.Male, 28, 180, 78));
            Assert.Equal(BodyFatSource.Measured, input.BodyFatSource, "Source");
            Assert.Equal(16.0, input.BodyFatPercent, "Value");
            Assert.True(input.BodyFatEstimationMethod is null, "No method");
            var result = HealthRatingEngine.Calculate(input);
            Assert.Equal(BodyFatSource.Measured, result.BodyFat.Source, "Result source");
        }),

        ("BodyFat: the estimated value is scored by the same engine as a measured one", () =>
        {
            var profile = new ProfileSnapshot(Sex.Male, 28, 180, 78);
            var unknown = SampleProfile.Healthy();
            unknown.BodyFatPercent = null;
            var estimated = HealthRatingEngine.Calculate(unknown, profile);

            var measured = SampleProfile.Healthy();
            measured.BodyFatPercent = 19.13;
            var same = HealthRatingEngine.Calculate(measured, profile);

            Assert.Equal(same.ParameterScores[K.BodyFat], estimated.ParameterScores[K.BodyFat], "Same score for the same value");
            Assert.Equal(same.TotalHealthRating, estimated.TotalHealthRating, "Same total");
            Assert.Equal(BodyFatSource.Estimated, estimated.BodyFat.Source, "Result reports Estimated");
            Assert.Equal(19.13, estimated.BodyFat.Percent, "Result reports the value");
        }),

        ("BodyFat: NaN, infinite, negative and implausible estimates are refused", () =>
        {
            var baseInput = new BodyFatEstimationInput(Sex.Male, 28, 180, 78, 24.07, 82, 98);
            Assert.True(BodyFatEstimation.TryEstimate(baseInput with { Bmi = double.NaN }).Estimate is null, "NaN BMI");
            Assert.True(BodyFatEstimation.TryEstimate(baseInput with { Bmi = double.PositiveInfinity }).Estimate is null, "Infinite BMI");
            Assert.True(BodyFatEstimation.TryEstimate(baseInput with { Bmi = 3.2, Age = 18 }).Estimate is null, "Negative estimate");
            Assert.True(BodyFatEstimation.TryEstimate(baseInput with { Bmi = 70, Age = 90 }).Estimate is null, "Estimate above 75%");
            var (ok, error) = BodyFatEstimation.TryEstimate(baseInput);
            Assert.True(ok is not null && error is null, "Normal input accepted");
        }),

        ("BodyFat: the estimator is replaceable (IBodyFatEstimator)", () =>
        {
            var original = BodyFatEstimation.Current;
            try
            {
                BodyFatEstimation.Current = new FixedEstimator(22.5);
                var answers = SampleProfile.Healthy();
                answers.BodyFatPercent = null;
                var input = AssessmentInput.From(answers, new ProfileSnapshot(Sex.Male, 28, 180, 78));
                Assert.Equal(22.5, input.BodyFatPercent, "Value from the swapped estimator");
                Assert.Equal("Fixed", input.BodyFatEstimationMethod, "Its method id is recorded");
            }
            finally
            {
                BodyFatEstimation.Current = original;
            }
        }),

        ("BodyFat: an impossible estimate is a validation error, not a saved score", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "tiny@example.com", ageToday: 18);
            user.HeightCm = 250; // BMI 3.2 → negative Deurenberg estimate
            user.WeightKg = 20;
            db.Context.SaveChanges();

            var answers = SampleProfile.Healthy();
            answers.BodyFatPercent = null;
            var outcome = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, answers).GetAwaiter().GetResult();
            Assert.True(outcome.Assessment is null, "Nothing saved");
            Assert.True(outcome.ValidationErrors!.Any(e => e.Contains("couldn't be estimated")), "Explains why");
            Assert.Equal(0, db.Context.HealthAssessments.Count(), "No row");
        }),

        ("BodyFat: source and method are stored with the assessment", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "bf@example.com");
            var service = new AssessmentService(db.Context);
            var unknown = SampleProfile.Healthy();
            unknown.BodyFatPercent = null;
            var estimated = service.CreateCompletedAsync(user.Id, unknown).GetAwaiter().GetResult().Assessment!;
            var measured = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;

            using var read = db.NewContext();
            var e = new AssessmentService(read).GetAsync(user.Id, estimated.Id).GetAwaiter().GetResult()!;
            var m = new AssessmentService(read).GetAsync(user.Id, measured.Id).GetAwaiter().GetResult()!;
            Assert.Equal(BodyFatSource.Estimated, e.BodyFatSource, "Estimated source stored");
            Assert.Equal("Deurenberg1991", e.BodyFatEstimationMethod, "Method stored");
            Assert.Equal(19.13, e.BodyFatPercentage, "Estimated value stored");
            Assert.Equal("Estimated", e.ParameterScores.Single(p => p.ParameterKey == K.BodyFat).RawText, "Parameter row marked");
            Assert.Equal(BodyFatSource.Measured, m.BodyFatSource, "Measured source stored");
            Assert.True(m.BodyFatEstimationMethod is null, "No method when measured");
            Assert.Equal(16.0, m.BodyFatPercentage, "Measured value stored");
        }),

        ("BodyFat: the preview estimate uses the signed-in user's profile", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "prev@example.com");
            var service = new AssessmentService(db.Context);
            var (estimate, profileError, _) = service.EstimateBodyFatAsync(user.Id, 82, 98).GetAwaiter().GetResult();
            Assert.True(profileError is null && estimate is not null, "Estimated");
            Assert.Equal(19.13, estimate!.BodyFatPercent, "Same value as on submission");

            var incomplete = TestDatabase.AddUser(db.Context, "new@example.com", completeProfile: false);
            var (none, error, _) = service.EstimateBodyFatAsync(incomplete.Id, null, null).GetAwaiter().GetResult();
            Assert.True(none is null && error is not null, "Incomplete profile refused");
        }),

        // ---------- Heart Rate Recovery ----------
        ("HRR: calculated as peak minus heart rate 60 s later", () =>
        {
            var input = SampleProfile.Healthy();
            input.PeakHeartRateBpm = 165;
            input.HeartRateAfter60sBpm = 140;
            Assert.Equal(25, input.HeartRateRecoveryBpm, "HRR");
            var result = HealthRatingEngine.Calculate(input);
            Assert.Equal(HeartRateScorer.ScoreRecovery(25), result.ParameterScores[K.HeartRateRecovery], "Scored from the drop");
            Assert.Equal(25, result.HeartRateRecovery.RecoveryBpm, "Result HRR");
            Assert.Equal(165, result.HeartRateRecovery.PeakHeartRateBpm, "Result peak");
            Assert.Equal(140, result.HeartRateRecovery.HeartRateAfter60sBpm, "Result after 60 s");
        }),

        ("HRR: heart rate after 60 s above the peak is rejected", () =>
        {
            var input = SampleProfile.Healthy();
            input.PeakHeartRateBpm = 150;
            input.HeartRateAfter60sBpm = 155;
            var outcome = AssessmentValidator.Validate(input);
            Assert.True(outcome.Errors.Any(e => e.Contains("can't be higher than the peak")), "Rejected with a clear message");
        }),

        ("HRR: out-of-range readings and implausible drops are rejected", () =>
        {
            void Rejects(int peak, int after, string label)
            {
                var input = SampleProfile.Healthy();
                input.PeakHeartRateBpm = peak;
                input.HeartRateAfter60sBpm = after;
                Assert.False(AssessmentValidator.Validate(input).IsValid, label);
            }
            Rejects(0, 0, "Missing readings");
            Rejects(250, 150, "Peak above 230");
            Rejects(120, 20, "After 60 s below 40");
            Rejects(210, 100, "Drop of 110 bpm in 60 s");

            var belowResting = SampleProfile.Healthy();
            belowResting.RestingHeartRateBpm = 100;
            belowResting.PeakHeartRateBpm = 95;
            belowResting.HeartRateAfter60sBpm = 80;
            Assert.True(AssessmentValidator.Validate(belowResting).Errors.Any(e => e.Contains("higher than the resting")),
                "Peak not above resting heart rate rejected");

            var equal = SampleProfile.Healthy();
            equal.PeakHeartRateBpm = 140;
            equal.HeartRateAfter60sBpm = 140;
            Assert.True(AssessmentValidator.Validate(equal).IsValid, "No drop (HRR 0) is valid, just scored low");
        }),

        ("HRR: peak, after-60 s and HRR are stored with the assessment", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "hrr@example.com");
            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            using var read = db.NewContext();
            var a = new AssessmentService(read).GetAsync(user.Id, saved.Id).GetAwaiter().GetResult()!;
            Assert.Equal(170, a.PeakHeartRate, "Peak");
            Assert.Equal(142, a.HeartRateAfter60Seconds, "After 60 s");
            Assert.Equal(28, a.HeartRateRecovery, "HRR");
            var row = a.ParameterScores.Single(p => p.ParameterKey == K.HeartRateRecovery);
            Assert.Equal(28.0, row.RawValue, "Row value is the HRR");
            Assert.Equal("Peak 170 bpm · after 60 s 142 bpm", row.RawText, "Row shows both readings");
        }),

        // ---------- Substances ----------
        ("Substances: alcohol, tobacco and drugs are scored independently", () =>
        {
            var baseline = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var smoker = SampleProfile.Healthy();
            smoker.TobaccoFrequency = SubstanceFrequency.Daily;
            var result = HealthRatingEngine.Calculate(smoker);

            Assert.Equal(1, result.ParameterScores[K.Tobacco], "Tobacco daily");
            Assert.Equal(baseline.ParameterScores[K.Alcohol], result.ParameterScores[K.Alcohol], "Alcohol unchanged");
            Assert.Equal(baseline.ParameterScores[K.Drugs], result.ParameterScores[K.Drugs], "Drugs unchanged");
            Assert.Equal(baseline.TotalHealthRating - 7, result.TotalHealthRating, "Only tobacco's 8 → 1 changes the total");
            Assert.False(result.ParameterScores.ContainsKey(K.SubstanceUse), "No combined parameter anymore");
        }),

        ("Substances: each one is required", () =>
        {
            foreach (var clear in new Action<AssessmentInput>[] { i => i.AlcoholFrequency = null, i => i.TobaccoFrequency = null, i => i.DrugsFrequency = null })
            {
                var input = SampleProfile.Healthy();
                clear(input);
                Assert.False(AssessmentValidator.Validate(input).IsValid, "Missing substance answer rejected");
            }
        }),

        ("Four States: substances sit in Immunity and Longevity only; state sizes 10/7/13/15", () =>
        {
            foreach (var key in new[] { K.Alcohol, K.Tobacco, K.Drugs })
            {
                Assert.True(FourStateCalculator.ImmunityParams.Contains(key), $"{key} in Immunity");
                Assert.True(FourStateCalculator.LongevityParams.Contains(key), $"{key} in Longevity");
                Assert.False(FourStateCalculator.EnergyStrengthStaminaParams.Contains(key), $"{key} not in Energy");
                Assert.False(FourStateCalculator.MentalEmotionalParams.Contains(key), $"{key} not in Mental");
            }
            var all = FourStateCalculator.EnergyStrengthStaminaParams.Concat(FourStateCalculator.MentalEmotionalParams)
                .Concat(FourStateCalculator.ImmunityParams).Concat(FourStateCalculator.LongevityParams);
            Assert.False(all.Contains(K.SubstanceUse), "Legacy key not used");
            Assert.True(all.All(ParameterSet.Keys.Contains), "Every state parameter exists in ParameterSet");
            Assert.Equal(10, FourStateCalculator.EnergyStrengthStaminaParams.Length, "Energy");
            Assert.Equal(7, FourStateCalculator.MentalEmotionalParams.Length, "Mental");
            Assert.Equal(13, FourStateCalculator.ImmunityParams.Length, "Immunity");
            Assert.Equal(15, FourStateCalculator.LongevityParams.Length, "Longevity");

            var baseline = HealthRatingEngine.Calculate(SampleProfile.Healthy());
            var drinker = SampleProfile.Healthy();
            drinker.AlcoholFrequency = SubstanceFrequency.Daily;
            var result = HealthRatingEngine.Calculate(drinker);
            Assert.Equal(baseline.FourStates.Immunity.RawScore - 7, result.FourStates.Immunity.RawScore, "Immunity reflects alcohol");
            Assert.Equal(baseline.FourStates.Longevity.RawScore - 7, result.FourStates.Longevity.RawScore, "Longevity reflects alcohol");
            Assert.Equal(baseline.FourStates.EnergyStrengthStamina.RawScore, result.FourStates.EnergyStrengthStamina.RawScore, "Energy unchanged");
            Assert.Equal(130, result.FourStates.Immunity.MaxRawScore, "Immunity max 13 × 10");
            Assert.Equal(150, result.FourStates.Longevity.MaxRawScore, "Longevity max 15 × 10");
        }),

        // ---------- Backward compatibility ----------
        ("Legacy: a v1 assessment keeps 39 parameters, max 390 and the combined substance row", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "legacy@example.com");
            var legacy = LegacyAssessment(user.Id);
            db.Context.HealthAssessments.Add(legacy);
            db.Context.SaveChanges();
            var service = new AssessmentService(db.Context);
            var current = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;

            using var read = db.NewContext();
            var reader = new AssessmentService(read);
            var old = reader.GetAsync(user.Id, legacy.Id).GetAwaiter().GetResult()!;
            Assert.Equal(ParameterSet.LegacyVersion, old.ParameterSetVersion, "Version kept");
            Assert.Equal(39, old.ParameterScores.Count, "39 rows kept");
            Assert.Equal(390, old.TotalPossibleScore, "Max 390 kept");
            Assert.Equal(legacy.TotalHealthRating, old.TotalHealthRating, "Total not re-scored");
            Assert.True(old.ParameterScores.Any(p => p.ParameterKey == K.SubstanceUse), "Combined substance row kept");
            Assert.False(old.ParameterScores.Any(p => p.ParameterKey == K.Alcohol), "Not split retroactively");
            Assert.True(old.PeakHeartRate is null && old.HeartRateAfter60Seconds is null, "No protocol readings invented");

            var list = reader.ListCompletedAsync(user.Id).GetAwaiter().GetResult();
            Assert.Equal(390, list.Single(s => s.Id == legacy.Id).TotalPossibleScore, "History shows 390 for the old one");
            Assert.Equal(410, list.Single(s => s.Id == current.Id).TotalPossibleScore, "History shows 410 for the new one");
        }),

        ("Legacy: the migration marks existing rows as v1-39 with measured body fat", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "migrate@example.com");
            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate("AddProfileHeightWeight"); // back to the schema before this change…
            migrator.Migrate();                          // …and forward again: the row is now "pre-existing"

            using var read = db.NewContext();
            var row = read.HealthAssessments.AsNoTracking().Single(a => a.Id == saved.Id);
            Assert.Equal("v1-39", row.ParameterSetVersion, "Default parameter set for existing rows");
            Assert.Equal(BodyFatSource.Measured, row.BodyFatSource, "Default body-fat source for existing rows");
            Assert.True(row.PeakHeartRate is null, "No peak for existing rows");
        }),
    };

    private sealed class FixedEstimator(double value) : IBodyFatEstimator
    {
        public string MethodId => "Fixed";
        public string MethodName => "Fixed test value";
        public double Estimate(BodyFatEstimationInput input) => value;
    }

    /// <summary>
    /// An assessment as it was stored before the split: 39 rows with the combined
    /// "substanceUse" parameter, max 390, HRR entered directly.
    /// </summary>
    private static HealthAssessment LegacyAssessment(Guid userId)
    {
        var input = SampleProfile.Healthy();
        var result = HealthRatingEngine.Calculate(input);
        var a = AssessmentSnapshotBuilder.Build(userId, input, result, DateTime.UtcNow.AddDays(-30));

        var split = a.ParameterScores.Where(p => p.ParameterKey is K.Alcohol or K.Tobacco or K.Drugs).ToList();
        var position = split.Min(p => p.SortOrder);
        foreach (var p in split) a.ParameterScores.Remove(p);
        a.ParameterScores.Add(new AssessmentParameterScore
        {
            Id = Guid.NewGuid(),
            AssessmentId = a.Id,
            ParameterKey = K.SubstanceUse,
            ParameterName = "Alcohol / tobacco / drugs",
            SortOrder = position,
            RawText = "Rarely",
            Unit = "frequency",
            Score = 8,
            CreatedAt = a.CreatedAt,
        });
        var order = 0;
        foreach (var p in a.ParameterScores.OrderBy(p => p.SortOrder).ThenBy(p => p.ParameterKey == K.SubstanceUse ? 0 : 1))
        {
            p.SortOrder = ++order;
        }

        a.ParameterSetVersion = ParameterSet.LegacyVersion;
        a.ScoringVersion = "2026.09-provisional";
        a.TotalPossibleScore = 390;
        a.TotalHealthRating = a.ParameterScores.Sum(p => p.Score);
        a.Percentage = Math.Round(a.TotalHealthRating / 390.0 * 100, 2);
        a.PeakHeartRate = null;
        a.HeartRateAfter60Seconds = null;
        a.InputSnapshotJson = """{"heartRateRecoveryBpm":28,"alcoholTobaccoDrugsFrequency":"Rarely","bodyFatPercent":16}""";
        return a;
    }
}
