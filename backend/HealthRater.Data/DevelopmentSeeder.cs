using HealthRater.Core.Auth;
using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
using HealthRater.Data.Entities;
using HealthRater.Data.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Data;

/// <summary>
/// DEVELOPMENT ONLY. Creates a clearly-labelled demo account with a few past assessments
/// so the history/calendar can be exercised. The API only calls this when running in the
/// Development environment AND <c>Database:SeedDevelopmentData</c> is true.
/// </summary>
public static class DevelopmentSeeder
{
    public const string DemoEmail = "demo@healthrater.test";
    public const string DemoPassword = "DemoPassword1";

    public static async Task SeedAsync(HealthRaterDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail)) return;

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = DemoEmail,
            PasswordHash = PasswordHasher.Hash(DemoPassword),
            FirstName = "Demo",
            LastName = "Test Data",
            Sex = Sex.Female,
            DateOfBirth = DateOnly.FromDateTime(now).AddYears(-34),
            HeightCm = 168,
            WeightKg = 64,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(user);

        // Three scans a couple of weeks apart, gradually improving — fictional test data.
        var scans = new (int DaysAgo, int Energy, int Mood, int Steps, int Cooper)[]
        {
            (28, 5, 5, 5000, 2100),
            (14, 6, 6, 7500, 2400),
            (1, 8, 7, 9500, 2750),
        };

        foreach (var scan in scans)
        {
            var input = BaseInput();
            input.EnergyLevel = scan.Energy;
            input.AverageMood = scan.Mood;
            input.DailyStepsNeat = scan.Steps;
            input.CooperDistanceMeters = scan.Cooper;

            var when = now.AddDays(-scan.DaysAgo);
            var assessment = AssessmentSnapshotBuilder.Build(
                user.Id, input, HealthRatingEngine.Calculate(input), when, user.DateOfBirth);
            db.HealthAssessments.Add(assessment);
        }

        await db.SaveChangesAsync();
    }

    private static AssessmentInput BaseInput() => new()
    {
        Sex = Sex.Female, Age = 34, HeightCm = 168, WeightKg = 64,
        WaistCm = 74, HipCm = 98, BodyFatPercent = 25,
        RestingHeartRateBpm = 64, PeakHeartRateBpm = 160, HeartRateAfter60sBpm = 138, SystolicBpMmHg = 118, DiastolicBpMmHg = 76,
        EnergyLevel = 6, EnergyStability = 6, AverageSleepQuality = 7, CircadianHealth = 6,
        AverageMood = 6, MoodStability = 6, SocialLife = 7, JobSatisfaction = 6, HomeFamilySatisfaction = 8,
        DailyWaterIntakeLiters = 2.0, DigestionAndEvacuation = 7, ImmuneHealth = 7,
        CaffeineServingsPerDay = 2, JunkFoodServingsPerWeek = 3, OvereatingEpisodesPerWeek = 1,
        AlcoholFrequency = SubstanceFrequency.Monthly, TobaccoFrequency = SubstanceFrequency.Never,
        DrugsFrequency = SubstanceFrequency.Never, VegetablesFiberServingsPerDay = 4,
        DailyStepsNeat = 7000, TrainingSessionsPerWeek = 3, PushUps = 15, PullUps = 2, BodyweightSquats = 35,
        CooperDistanceMeters = 2300,
        SkinHealth = 7, JawSkullHealth = 8, DentalHealth = 7, SpinalHealth = 7, HairHealth = 8,
    };
}
