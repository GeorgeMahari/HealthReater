using HealthRater.Core.Scoring;
using HealthRater.Data.Entities;
using HealthRater.Data.Services;
using HealthRater.Data.Snapshots;
using HealthRater.Tests.Framework;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Tests;

public static class PersistenceTests
{
    public static List<(string, Action)> All() => new()
    {
        ("Persistence: parameter catalog covers exactly the engine's 41 keys, in ParameterSet order", () =>
        {
            var engineKeys = HealthRatingEngine.Calculate(SampleProfile.Healthy()).ParameterScores.Keys.ToList();
            var catalogKeys = ParameterCatalog.All.Select(p => p.Key).ToList();
            Assert.Equal(ParameterSet.Count, catalogKeys.Count, "Catalog size");
            Assert.True(engineKeys.SequenceEqual(catalogKeys), "Catalog keys match engine keys and order");
            Assert.True(ParameterSet.Keys.SequenceEqual(catalogKeys), "Catalog keys match ParameterSet");
            Assert.Equal(ParameterSet.Count, catalogKeys.Distinct().Count(), "Keys are unique");
        }),

        ("Persistence: saved assessment stores the full snapshot and all 41 scored parameters", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var input = SampleProfile.Healthy();
            var expected = HealthRatingEngine.Calculate(input);

            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, input).GetAwaiter().GetResult().Assessment!;

            using var read = db.NewContext();
            var loaded = new AssessmentService(read).GetAsync(user.Id, saved.Id).GetAwaiter().GetResult()!;
            Assert.Equal(AssessmentStatus.Completed, loaded.Status, "Status");
            Assert.Equal(expected.TotalHealthRating, loaded.TotalHealthRating, "Total");
            Assert.Equal(410, loaded.TotalPossibleScore, "Max");
            Assert.Equal(ParameterSet.Version, loaded.ParameterSetVersion, "Parameter set version");
            Assert.Equal(expected.Percentage, loaded.Percentage, "Percentage");
            Assert.Equal(expected.FourStates.Longevity.NormalizedScore, loaded.Longevity.NormalizedScore, "Longevity state");
            Assert.Equal(expected.DerivedMetrics.WHtR, loaded.WaistToHeightRatio, "WHtR");
            Assert.Equal(input.Age, loaded.AgeAtAssessment, "Age snapshot");
            Assert.Equal(input.SystolicBpMmHg, loaded.BloodPressureSystolic, "Systolic snapshot");
            Assert.Equal("cm", loaded.HeightUnit, "Height unit");
            Assert.Equal(HealthRatingEngine.ScoringVersion, loaded.ScoringVersion, "Scoring version");
            Assert.Equal(DateTimeKind.Utc, loaded.CompletedAt!.Value.Kind, "CompletedAt read back as UTC");

            Assert.Equal(41, loaded.ParameterScores.Count, "41 parameter rows");
            foreach (var p in loaded.ParameterScores)
            {
                Assert.Equal(expected.ParameterScores[p.ParameterKey], p.Score, $"Score of {p.ParameterKey}");
                Assert.InRange(p.Score, 1, 10, $"Score range of {p.ParameterKey}");
            }
            var bp = loaded.ParameterScores.Single(p => p.ParameterKey == "bloodPressure");
            Assert.Equal($"{input.SystolicBpMmHg}/{input.DiastolicBpMmHg}", bp.RawText, "Blood pressure raw text");
            var hydration = loaded.ParameterScores.Single(p => p.ParameterKey == "hydration");
            Assert.True(hydration.NormalizedValue is > 0, "Hydration normalized to ml/kg");
        }),

        ("Persistence: repeated assessments by one user are all kept (never overwritten)", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var service = new AssessmentService(db.Context);

            var first = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            var worse = SampleProfile.Healthy();
            worse.AverageMood = 2;
            worse.EnergyLevel = 3;
            var second = service.CreateCompletedAsync(user.Id, worse).GetAwaiter().GetResult().Assessment!;

            var list = service.ListCompletedAsync(user.Id).GetAwaiter().GetResult();
            Assert.Equal(2, list.Count, "Both assessments listed");
            Assert.Equal(second.Id, list[0].Id, "Newest first");
            Assert.True(first.TotalHealthRating != second.TotalHealthRating, "Different totals kept");
            Assert.Equal(first.TotalHealthRating,
                service.GetAsync(user.Id, first.Id).GetAwaiter().GetResult()!.TotalHealthRating,
                "First assessment unchanged");
            Assert.Equal(82, db.Context.AssessmentParameterScores.Count(), "2 × 41 parameter rows");
        }),

        ("Persistence: a user can never read or delete another user's assessment", () =>
        {
            using var db = TestDatabase.Create();
            var alice = TestDatabase.AddUser(db.Context, "alice@example.com");
            var bob = TestDatabase.AddUser(db.Context, "bob@example.com");
            var service = new AssessmentService(db.Context);
            var bobs = service.CreateCompletedAsync(bob.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;

            Assert.True(service.GetAsync(alice.Id, bobs.Id).GetAwaiter().GetResult() is null, "Alice cannot read Bob's");
            Assert.Equal(0, service.ListCompletedAsync(alice.Id).GetAwaiter().GetResult().Count, "Alice's list is empty");
            Assert.False(service.DeleteAsync(alice.Id, bobs.Id).GetAwaiter().GetResult(), "Alice cannot delete Bob's");
            Assert.True(service.GetAsync(bob.Id, bobs.Id).GetAwaiter().GetResult() is not null, "Bob's is still there");
        }),

        ("Persistence: deleting an assessment removes its parameter scores too", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var service = new AssessmentService(db.Context);
            var saved = service.CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            db.Context.ChangeTracker.Clear();

            Assert.True(service.DeleteAsync(user.Id, saved.Id).GetAwaiter().GetResult(), "Owner can delete");
            Assert.Equal(0, db.Context.HealthAssessments.Count(), "Assessment gone");
            Assert.Equal(0, db.Context.AssessmentParameterScores.Count(), "Parameter rows cascaded");
        }),

        ("Persistence: drafts are excluded from history", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var draft = AssessmentSnapshotBuilder.Build(user.Id, SampleProfile.Healthy(),
                HealthRatingEngine.Calculate(SampleProfile.Healthy()), DateTime.UtcNow);
            draft.Status = AssessmentStatus.Draft;
            draft.CompletedAt = null;
            db.Context.HealthAssessments.Add(draft);
            db.Context.SaveChanges();

            Assert.Equal(0, new AssessmentService(db.Context).ListCompletedAsync(user.Id).GetAwaiter().GetResult().Count,
                "Draft not listed");
        }),

        ("Persistence: stored historical scores are returned as-is, never recalculated", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;

            // Simulate an assessment scored under an older configuration.
            db.Context.Database.ExecuteSqlRaw(
                "UPDATE HealthAssessments SET TotalHealthRating = 250, ScoringVersion = 'old' WHERE Id = {0}", saved.Id);
            db.Context.Database.ExecuteSqlRaw(
                "UPDATE AssessmentParameterScores SET Score = 3 WHERE AssessmentId = {0} AND ParameterKey = 'bmi'", saved.Id);

            using var read = db.NewContext();
            var loaded = new AssessmentService(read).GetAsync(user.Id, saved.Id).GetAwaiter().GetResult()!;
            Assert.Equal(250, loaded.TotalHealthRating, "Stored total returned");
            Assert.Equal("old", loaded.ScoringVersion, "Stored scoring version returned");
            Assert.Equal(3, loaded.ParameterScores.Single(p => p.ParameterKey == "bmi").Score, "Stored parameter score returned");
        }),

        ("Persistence: the database rejects parameter scores outside 1–10", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var saved = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            var threw = false;
            try
            {
                db.Context.Database.ExecuteSqlRaw(
                    "UPDATE AssessmentParameterScores SET Score = 11 WHERE AssessmentId = {0}", saved.Id);
            }
            catch (Exception)
            {
                threw = true;
            }
            Assert.True(threw, "Check constraint enforced");
        }),

        ("Persistence: month range query returns only that month's scans", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            AddCompleted(db, user.Id, new DateTime(2026, 8, 31, 23, 30, 0, DateTimeKind.Utc));
            var inSept = AddCompleted(db, user.Id, new DateTime(2026, 9, 25, 9, 0, 0, DateTimeKind.Utc));
            AddCompleted(db, user.Id, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

            var sept = new AssessmentService(db.Context).ListCompletedBetweenAsync(user.Id,
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)).GetAwaiter().GetResult();
            Assert.Equal(1, sept.Count, "Only September scan");
            Assert.Equal(inSept, sept[0].Id, "Correct scan");
        }),
    };

    private static Guid AddCompleted(TestDatabase db, Guid userId, DateTime completedAtUtc)
    {
        var input = SampleProfile.Healthy();
        var assessment = AssessmentSnapshotBuilder.Build(userId, input, HealthRatingEngine.Calculate(input), completedAtUtc);
        db.Context.HealthAssessments.Add(assessment);
        db.Context.SaveChanges();
        return assessment.Id;
    }
}
