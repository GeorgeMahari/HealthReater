using System.Text;
using HealthRater.Core.Models;
using HealthRater.Core.Profile;
using HealthRater.Core.Scoring;
using HealthRater.Core.Scoring.References;
using HealthRater.Data.Services;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

/// <summary>Profile context (sex + date of birth → age) and sex/age-aware scoring references.</summary>
public static class ProfileContextTests
{
    public static List<(string, Action)> All() => new()
    {
        // ---------- Age from date of birth ----------
        ("Context: age is calculated from the date of birth (birthday edge cases)", () =>
        {
            var dob = new DateOnly(2005, 4, 11);
            Assert.Equal(21, ProfileRules.AgeOn(dob, new DateOnly(2026, 9, 29)), "After birthday");
            Assert.Equal(20, ProfileRules.AgeOn(dob, new DateOnly(2026, 4, 10)), "Day before birthday");
            Assert.Equal(21, ProfileRules.AgeOn(dob, new DateOnly(2026, 4, 11)), "On birthday");
            Assert.Equal(22, ProfileRules.AgeOn(dob, new DateOnly(2027, 9, 29)), "A year later");
            Assert.Equal(19, ProfileRules.AgeOn(new DateOnly(2004, 2, 29), new DateOnly(2024, 2, 28)), "Leap-day birthday");
        }),

        ("Context: profile validation requires Male/Female, a date of birth giving age 18–100, height and weight", () =>
        {
            var today = new DateOnly(2026, 9, 29);
            Assert.True(ProfileRules.Validate("Male", new DateOnly(2005, 4, 11), 180, 78, today).IsValid, "Valid male profile");
            Assert.True(ProfileRules.Validate("Female", new DateOnly(1930, 1, 1), 165, 60, today).IsValid, "Age 96 allowed");
            Assert.False(ProfileRules.Validate(null, new DateOnly(2005, 4, 11), 180, 78, today).IsValid, "Missing sex");
            Assert.False(ProfileRules.Validate("Other", new DateOnly(2005, 4, 11), 180, 78, today).IsValid, "Unsupported sex value");
            Assert.False(ProfileRules.Validate("male", new DateOnly(2005, 4, 11), 180, 78, today).IsValid, "Exact value required");
            Assert.False(ProfileRules.Validate("Male", null, 180, 78, today).IsValid, "Missing date of birth");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2027, 1, 1), 180, 78, today).IsValid, "Future date of birth");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2009, 1, 1), 180, 78, today).IsValid, "Age 17");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(1925, 1, 1), 180, 78, today).IsValid, "Age 101");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2005, 4, 11), null, 78, today).IsValid, "Missing height");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2005, 4, 11), 180, null, today).IsValid, "Missing weight");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2005, 4, 11), 30, 78, today).IsValid, "Height below 50 cm");
            Assert.False(ProfileRules.Validate("Male", new DateOnly(2005, 4, 11), 180, 500, today).IsValid, "Weight above 400 kg");
        }),

        ("Context: an incomplete profile yields no scoring context", () =>
        {
            var today = new DateOnly(2026, 9, 29);
            Assert.True(ProfileRules.ContextFor(null, new DateOnly(2005, 4, 11), 180, 78, today).Profile is null, "No sex");
            Assert.True(ProfileRules.ContextFor(Sex.Male, null, 180, 78, today).Profile is null, "No date of birth");
            Assert.True(ProfileRules.ContextFor(Sex.Male, new DateOnly(2005, 4, 11), null, 78, today).Profile is null, "No height");
            Assert.True(ProfileRules.ContextFor(Sex.Male, new DateOnly(2005, 4, 11), 180, null, today).Profile is null, "No weight");
            var (context, error) = ProfileRules.ContextFor(Sex.Male, new DateOnly(2005, 4, 11), 180, 78, today);
            Assert.Equal(new ProfileSnapshot(Sex.Male, 21, 180, 78), context, "Complete profile gives Male, 21, 180 cm, 78 kg");
            Assert.True(error is null, "No error");
        }),

        // ---------- Scoring references ----------
        ("Scoring: the default reference file loads and covers every sex and age 18–100", () =>
        {
            var catalog = ScoringReferenceCatalog.LoadEmbedded();
            foreach (var key in new[] { "bodyFat", "whr", "functionalPower" })
            {
                Assert.True(catalog.Covers(key), $"{key} covered");
                foreach (var sex in Enum.GetValues<Sex>())
                    for (var age = 18; age <= 100; age++)
                        catalog.Find(key, sex, age); // throws if missing or ambiguous
            }
            Assert.True(catalog.References.All(r => r.Status == ReferenceStatus.Provisional),
                "All shipped references are marked provisional (no official tables supplied)");
        }),

        ("Scoring: male and female use different body-fat and WHR references", () =>
        {
            var catalog = ScoringReferenceCatalog.LoadEmbedded();
            var male = new ScoringContext(Sex.Male, 30);
            var female = new ScoringContext(Sex.Female, 30);
            Assert.Equal(10, catalog.Score("bodyFat", 15, male), "Male 15% at the male reference centre");
            Assert.Equal(7, catalog.Score("bodyFat", 23, male), "Male 23% scored against the male reference");
            Assert.Equal(10, catalog.Score("bodyFat", 23, female), "Female 23% at the female reference centre");
            Assert.True(catalog.Score("whr", 0.88, male) > catalog.Score("whr", 0.88, female), "WHR 0.88 scores lower for a woman");
        }),

        ("Scoring: different ages use different functional-power references", () =>
        {
            var catalog = ScoringReferenceCatalog.LoadEmbedded();
            var reps = 100;
            var at20 = catalog.Score("functionalPower", reps, new ScoringContext(Sex.Male, 20));
            var at60 = catalog.Score("functionalPower", reps, new ScoringContext(Sex.Male, 60));
            var female20 = catalog.Score("functionalPower", reps, new ScoringContext(Sex.Female, 20));
            var female60 = catalog.Score("functionalPower", reps, new ScoringContext(Sex.Female, 60));
            Assert.Equal(7, at20, "Male 20: 100 / 150 norm");
            Assert.Equal(10, at60, "Male 60: 100 / 70 norm, capped");
            Assert.Equal(9, female20, "Female 20: 100 / 110 norm");
            Assert.Equal(10, female60, "Female 60: 100 / 50 norm, capped");
        }),

        ("Scoring: the shipped references reproduce the previous formulas exactly", () =>
        {
            var catalog = ScoringReferenceCatalog.LoadEmbedded();
            foreach (var sex in Enum.GetValues<Sex>())
            {
                for (var bf = 3.0; bf <= 50; bf += 0.5)
                {
                    var center = sex == Sex.Male ? 15.0 : 23.0;
                    var expected = ScoringConfig.Clamp1To10(10 - Math.Abs(bf - center) * 0.4);
                    Assert.Equal(expected, catalog.Score("bodyFat", bf, new ScoringContext(sex, 40)), $"bodyFat {sex} {bf}");
                }
                for (var whr = 0.6; whr <= 1.3; whr += 0.01)
                {
                    var idealMax = sex == Sex.Male ? 0.90 : 0.80;
                    var expected = ScoringConfig.Clamp1To10(10 - Math.Max(0, whr - idealMax) * 20);
                    Assert.Equal(expected, catalog.Score("whr", whr, new ScoringContext(sex, 40)), $"whr {sex} {whr:F2}");
                }
            }
        }),

        ("Scoring: age-specific bands and sex-specific precedence can be configured without code", () =>
        {
            const string json = """
            {
              "version": "test",
              "references": [
                { "parameterKey": "bodyFat", "sex": null, "minAge": 18, "maxAge": 100, "method": "IdealCenter", "center": 20, "penaltyPerUnit": 1 },
                { "parameterKey": "bodyFat", "sex": "Male", "minAge": 18, "maxAge": 39, "method": "Bands",
                  "bands": [ { "max": 10, "score": 6 }, { "min": 10, "max": 20, "score": 10 }, { "min": 20, "score": 4 } ] },
                { "parameterKey": "bodyFat", "sex": "Male", "minAge": 40, "maxAge": 100, "method": "Bands",
                  "bands": [ { "max": 12, "score": 6 }, { "min": 12, "max": 25, "score": 10 }, { "min": 25, "score": 5 } ] }
              ]
            }
            """;
            var catalog = ScoringReferenceCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(4, catalog.Score("bodyFat", 22, new ScoringContext(Sex.Male, 25)), "Male 25: band 20+");
            Assert.Equal(10, catalog.Score("bodyFat", 22, new ScoringContext(Sex.Male, 60)), "Male 60: band 12–25");
            Assert.Equal(8, catalog.Score("bodyFat", 22, new ScoringContext(Sex.Female, 25)), "Female falls back to the sex-neutral reference");
        }),

        ("Scoring: a reference file with gaps or overlaps is rejected when loaded", () =>
        {
            static bool Rejects(string json)
            {
                try { ScoringReferenceCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))); return false; }
                catch (InvalidOperationException) { return true; }
            }
            Assert.True(Rejects("""{ "references": [ { "parameterKey": "bodyFat", "sex": "Male", "minAge": 18, "maxAge": 50, "method": "RatioToNorm", "norm": 1 } ] }"""),
                "Gap: female and ages 51–100 missing");
            Assert.True(Rejects("""{ "references": [ { "parameterKey": "x", "minAge": 18, "maxAge": 100, "method": "RatioToNorm", "norm": 1 }, { "parameterKey": "x", "minAge": 50, "maxAge": 100, "method": "RatioToNorm", "norm": 2 } ] }"""),
                "Overlap for ages 50–100");
            Assert.True(Rejects("""{ "references": [ { "parameterKey": "x", "minAge": 18, "maxAge": 100, "method": "IdealCenter" } ] }"""),
                "Missing method parameters");
        }),

        // ---------- Engine invariants ----------
        ("Scoring: still exactly 39 scored parameters, 390 maximum, four states intact", () =>
        {
            var answers = SampleProfile.Healthy();
            foreach (var context in new[] { new ProfileSnapshot(Sex.Male, 20, 180, 78), new ProfileSnapshot(Sex.Male, 60, 180, 78), new ProfileSnapshot(Sex.Female, 20, 165, 60), new ProfileSnapshot(Sex.Female, 60, 165, 60) })
            {
                var result = HealthRatingEngine.Calculate(answers, context);
                Assert.Equal(39, result.ParameterScores.Count, $"39 parameters for {context}");
                Assert.Equal(390, result.MaxHealthRating, "Max 390");
                Assert.Equal(result.ParameterScores.Values.Sum(), result.TotalHealthRating, "Total is the sum");
                Assert.True(new[] { "sex", "age", "height", "weight" }.All(result.ParameterScores.ContainsKey),
                    "Sex, age, height and weight stay parameters #1–#4 of the 39 (no extra parameters)");
                Assert.True(result.FourStates.Longevity.MaxRawScore > 0 && result.FourStates.EnergyStrengthStamina.MaxRawScore > 0, "Four states computed");
            }
            var young = HealthRatingEngine.Calculate(answers, new ProfileSnapshot(Sex.Male, 20, 180, 78));
            var old = HealthRatingEngine.Calculate(answers, new ProfileSnapshot(Sex.Male, 60, 180, 78));
            Assert.True(young.ParameterScores["functionalPower"] != old.ParameterScores["functionalPower"], "Age changes functional power");
            Assert.True(young.FourStates.Longevity.RawScore != old.FourStates.Longevity.RawScore, "Age reaches the Longevity state");
        }),

        // ---------- Saving assessments with the profile context ----------
        ("Assessments: an incomplete profile can't submit an assessment", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "new@example.com", completeProfile: false);
            var outcome = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult();
            Assert.True(outcome.Assessment is null && outcome.ProfileError is not null, "Refused with a profile error");
            Assert.Equal(0, db.Context.HealthAssessments.Count(), "Nothing saved");
        }),

        ("Assessments: sex, age, height and weight come from the profile, never from the submitted data", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com", Sex.Male, ageToday: 21);
            var spoofed = SampleProfile.Healthy();
            spoofed.Sex = Sex.Female; // a client trying to override the profile
            spoofed.Age = 90;
            spoofed.HeightCm = 150;
            spoofed.WeightKg = 120;
            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, spoofed).GetAwaiter().GetResult().Assessment!;
            var expected = HealthRatingEngine.Calculate(spoofed, new ProfileSnapshot(Sex.Male, 21, 180, 78));
            Assert.Equal("Male", saved.SexAtAssessment, "SexAtAssessment from the profile");
            Assert.Equal(21, saved.AgeAtAssessment, "AgeAtAssessment from the profile");
            Assert.Equal(user.DateOfBirth, saved.DateOfBirthAtAssessment, "DateOfBirthAtAssessment stored");
            Assert.Equal(expected.TotalHealthRating, saved.TotalHealthRating, "Scored with the profile context");
            Assert.Equal(21.0, saved.ParameterScores.Single(p => p.ParameterKey == "age").RawValue, "Age parameter raw value from profile");
            Assert.Equal(180.0, saved.Height, "Height from the profile");
            Assert.Equal(78.0, saved.Weight, "Weight from the profile");
            Assert.Equal(expected.DerivedMetrics.Bmi, saved.Bmi, "BMI calculated from the profile height and weight");
        }),

        ("Assessments: a birthday or profile change never alters earlier assessments", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com", Sex.Male);
            var dob = new DateOnly(2005, 4, 11);
            db.Context.Users.Single().DateOfBirth = dob;
            db.Context.SaveChanges();
            var service = new AssessmentService(db.Context);

            var first = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy(), new DateOnly(2026, 9, 29)).GetAwaiter().GetResult().Assessment!;
            var nextYear = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy(), new DateOnly(2027, 9, 29)).GetAwaiter().GetResult().Assessment!;
            Assert.Equal(21, first.AgeAtAssessment, "2026 assessment: age 21");
            Assert.Equal(22, nextYear.AgeAtAssessment, "2027 assessment uses the new current age: 22");

            // The user corrects their profile: different sex and date of birth.
            new ProfileService(db.Context).UpdateAsync(user.Id, "Test", "User", "a@example.com", Sex.Female, new DateOnly(1970, 1, 1), 170, 90).GetAwaiter().GetResult();
            var afterChange = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy(), new DateOnly(2027, 10, 1)).GetAwaiter().GetResult().Assessment!;

            using var read = db.NewContext();
            var reloaded = new AssessmentService(read).GetAsync(user.Id, first.Id).GetAwaiter().GetResult()!;
            Assert.Equal(21, reloaded.AgeAtAssessment, "2026 assessment still age 21");
            Assert.Equal("Male", reloaded.SexAtAssessment, "2026 assessment still Male");
            Assert.Equal(dob, reloaded.DateOfBirthAtAssessment, "2026 assessment keeps its date of birth");
            Assert.Equal(first.TotalHealthRating, reloaded.TotalHealthRating, "2026 total unchanged");
            Assert.Equal("Female", afterChange.SexAtAssessment, "New assessment uses the updated sex");
            Assert.Equal(57, afterChange.AgeAtAssessment, "New assessment uses the updated date of birth");
            Assert.Equal(90.0, afterChange.Weight, "New assessment uses the updated weight");
            Assert.Equal(78.0, reloaded.Weight, "2026 assessment keeps its weight");
        }),

        ("Assessments: an out-of-range age on the profile is refused", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            db.Context.Users.Single().DateOfBirth = new DateOnly(1920, 1, 1);
            db.Context.SaveChanges();
            var outcome = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy(), new DateOnly(2026, 9, 29)).GetAwaiter().GetResult();
            Assert.True(outcome.Assessment is null && outcome.ProfileError is not null, "Age 106 refused");
        }),
    };
}
