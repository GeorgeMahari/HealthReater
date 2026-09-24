using HealthRater.Core.Models;

namespace HealthRater.Tests;

public static class SampleProfile
{
    /// <summary>A healthy, mid-20s male profile used as the canonical sample test profile.</summary>
    public static AssessmentInput Healthy() => new()
    {
        Sex = Sex.Male,
        Age = 28,
        HeightCm = 180,
        WeightKg = 78,
        WaistCm = 82,
        HipCm = 98,
        BodyFatPercent = 16,
        RestingHeartRateBpm = 58,
        HeartRateRecoveryBpm = 28,
        SystolicBpMmHg = 115,
        DiastolicBpMmHg = 74,
        EnergyLevel = 8,
        EnergyStability = 7,
        AverageSleepQuality = 8,
        CircadianHealth = 7,
        AverageMood = 8,
        MoodStability = 7,
        SocialLife = 8,
        JobSatisfaction = 7,
        HomeFamilySatisfaction = 9,
        DailyWaterIntakeLiters = 2.6,
        DigestionAndEvacuation = 8,
        ImmuneHealth = 8,
        CaffeineServingsPerDay = 1,
        JunkFoodServingsPerWeek = 2,
        OvereatingEpisodesPerWeek = 1,
        AlcoholTobaccoDrugsFrequency = SubstanceFrequency.Rarely,
        VegetablesFiberServingsPerDay = 4,
        DailyStepsNeat = 9000,
        TrainingSessionsPerWeek = 5,
        PushUps = 40,
        PullUps = 12,
        BodyweightSquats = 50,
        CooperDistanceMeters = 2800,
        SkinHealth = 8,
        JawSkullHealth = 9,
        DentalHealth = 8,
        SpinalHealth = 7,
        HairHealth = 8,
    };

    /// <summary>A worst-case profile: every scorable field at its lowest end.</summary>
    public static AssessmentInput WorstCase()
    {
        var p = Healthy();
        p.BodyFatPercent = 40;
        p.RestingHeartRateBpm = 110;
        p.HeartRateRecoveryBpm = 3;
        p.SystolicBpMmHg = 170;
        p.DiastolicBpMmHg = 100;
        p.EnergyLevel = 1; p.EnergyStability = 1; p.AverageSleepQuality = 1; p.CircadianHealth = 1;
        p.AverageMood = 1; p.MoodStability = 1; p.SocialLife = 1; p.JobSatisfaction = 1; p.HomeFamilySatisfaction = 1;
        p.DailyWaterIntakeLiters = 0.2;
        p.DigestionAndEvacuation = 1; p.ImmuneHealth = 1;
        p.CaffeineServingsPerDay = 10;
        p.JunkFoodServingsPerWeek = 25;
        p.OvereatingEpisodesPerWeek = 10;
        p.AlcoholTobaccoDrugsFrequency = SubstanceFrequency.Daily;
        p.VegetablesFiberServingsPerDay = 0;
        p.DailyStepsNeat = 500;
        p.TrainingSessionsPerWeek = 0;
        p.PushUps = 0; p.PullUps = 0; p.BodyweightSquats = 0;
        p.CooperDistanceMeters = 500;
        p.SkinHealth = 1; p.JawSkullHealth = 1; p.DentalHealth = 1; p.SpinalHealth = 1; p.HairHealth = 1;
        return p;
    }
}
