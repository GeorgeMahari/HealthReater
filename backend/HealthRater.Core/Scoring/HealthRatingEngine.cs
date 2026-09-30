using HealthRater.Core.Models;
using HealthRater.Core.Scoring.Scorers;

namespace HealthRater.Core.Scoring;

public static class HealthRatingEngine
{
    /// <summary>
    /// Identifies the scoring rules in ScoringConfig. Stored with every saved assessment;
    /// bump it whenever a threshold or formula changes so historical results stay traceable.
    /// </summary>
    public const string ScoringVersion = "2026.10-provisional";

    // Canonical parameter keys. The official set and order live in ParameterSet.
    public static class Keys
    {
        public const string Sex = "sex";
        public const string Age = "age";
        public const string Height = "height";
        public const string Weight = "weight";
        public const string Waist = "waist";
        public const string Hip = "hip";
        public const string BodyFat = "bodyFat";
        public const string Bmi = "bmi";
        public const string WHtR = "whtr";
        public const string WHR = "whr";
        public const string RestingHeartRate = "restingHeartRate";
        public const string HeartRateRecovery = "heartRateRecovery";
        public const string BloodPressure = "bloodPressure";
        public const string EnergyLevel = "energyLevel";
        public const string EnergyStability = "energyStability";
        public const string SleepQuality = "sleepQuality";
        public const string CircadianHealth = "circadianHealth";
        public const string Mood = "mood";
        public const string MoodStability = "moodStability";
        public const string SocialLife = "socialLife";
        public const string JobSatisfaction = "jobSatisfaction";
        public const string HomeFamilySatisfaction = "homeFamilySatisfaction";
        public const string Hydration = "hydration";
        public const string Digestion = "digestion";
        public const string ImmuneHealth = "immuneHealth";
        public const string Caffeine = "caffeine";
        public const string JunkFood = "junkFood";
        public const string Overeating = "overeating";
        public const string Alcohol = "alcohol";
        public const string Tobacco = "tobacco";
        public const string Drugs = "drugs";

        /// <summary>
        /// Legacy (v1, 39-parameter) combined "alcohol / tobacco / drugs" parameter. No longer
        /// scored; kept so assessments saved before the split can still be labelled.
        /// </summary>
        public const string SubstanceUse = "substanceUse";
        public const string VegetablesFiber = "vegetablesFiber";
        public const string Neat = "neat";
        public const string PhysicalTraining = "physicalTraining";
        public const string FunctionalPower = "functionalPower";
        public const string Cooper = "cooper";
        public const string SkinHealth = "skinHealth";
        public const string JawSkullHealth = "jawSkullHealth";
        public const string DentalHealth = "dentalHealth";
        public const string SpinalHealth = "spinalHealth";
        public const string HairHealth = "hairHealth";
    }

    /// <summary>
    /// Sex and Age are demographic/context parameters, not lifestyle scores in the usual
    /// sense. Per the spec every parameter must ultimately produce 1-10, so both are still
    /// scored (age via a simple healthy-range heuristic, sex neutrally), and both count
    /// toward the total.
    /// </summary>
    private static int ScoreSex(Sex sex) => 10; // sex itself carries no inherent health penalty

    private static int ScoreAge(int age)
    {
        // Gentle provisional curve: physiologic risk generally rises with age.
        // 18-30 -> 10, tapering roughly 1 point per decade after 30.
        if (age <= 30) return 10;
        var decadesOver = (age - 30) / 10.0;
        return ScoringConfig.Clamp1To10(10 - decadesOver);
    }

    /// <summary>
    /// Scores questionnaire answers for a person with the given profile data. Sex, age, height
    /// and weight are four of the scored parameters; sex and age also select the sex/age-specific
    /// references used for body fat, WHR and functional power (see ScoringReferenceCatalog).
    /// </summary>
    public static HealthRatingResult Calculate(AssessmentAnswers answers, ProfileSnapshot profile) =>
        Calculate(AssessmentInput.From(answers, profile));

    public static HealthRatingResult Calculate(AssessmentInput input)
    {
        input.ResolveBodyFat();
        var bodyFat = input.BodyFatPercent
            ?? throw new InvalidOperationException(input.BodyFatEstimationError ?? "Body fat is missing; validate the input first.");
        var derived = DerivedMetricsCalculator.Calculate(input);
        var context = new ScoringContext(input.Sex, input.Age);

        var scores = new Dictionary<string, int>
        {
            [Keys.Sex] = ScoreSex(input.Sex),
            [Keys.Age] = ScoreAge(input.Age),
            [Keys.Height] = 10, // raw anthropometric datum, not itself a health signal
            [Keys.Weight] = 10, // weight's health signal is captured via BMI/WHtR/body fat
            [Keys.Waist] = 10,  // captured via WHtR/WHR
            [Keys.Hip] = 10,    // captured via WHR
            [Keys.BodyFat] = BodyCompositionScorer.ScoreBodyFat(bodyFat, context),
            [Keys.Bmi] = BodyCompositionScorer.ScoreBmi(derived.Bmi),
            [Keys.WHtR] = BodyCompositionScorer.ScoreWHtR(derived.WHtR),
            [Keys.WHR] = BodyCompositionScorer.ScoreWHR(derived.WHR, context),
            [Keys.RestingHeartRate] = HeartRateScorer.ScoreResting(input.RestingHeartRateBpm),
            [Keys.HeartRateRecovery] = HeartRateScorer.ScoreRecovery(input.HeartRateRecoveryBpm),
            [Keys.BloodPressure] = BloodPressureScorer.Score(input.SystolicBpMmHg, input.DiastolicBpMmHg),
            [Keys.EnergyLevel] = input.EnergyLevel,
            [Keys.EnergyStability] = input.EnergyStability,
            [Keys.SleepQuality] = input.AverageSleepQuality,
            [Keys.CircadianHealth] = input.CircadianHealth,
            [Keys.Mood] = input.AverageMood,
            [Keys.MoodStability] = input.MoodStability,
            [Keys.SocialLife] = input.SocialLife,
            [Keys.JobSatisfaction] = input.JobSatisfaction,
            [Keys.HomeFamilySatisfaction] = input.HomeFamilySatisfaction,
            [Keys.Hydration] = HydrationScorer.Score(input.DailyWaterIntakeLiters, input.WeightKg),
            [Keys.Digestion] = input.DigestionAndEvacuation,
            [Keys.ImmuneHealth] = input.ImmuneHealth,
            [Keys.Caffeine] = CaffeineScorer.Score(input.CaffeineServingsPerDay),
            [Keys.JunkFood] = JunkFoodScorer.Score(input.JunkFoodServingsPerWeek),
            [Keys.Overeating] = OvereatingScorer.Score(input.OvereatingEpisodesPerWeek),
            [Keys.Alcohol] = SubstanceScorer.ScoreAlcohol(Required(input.AlcoholFrequency, "Alcohol consumption")),
            [Keys.Tobacco] = SubstanceScorer.ScoreTobacco(Required(input.TobaccoFrequency, "Tobacco / smoking")),
            [Keys.Drugs] = SubstanceScorer.ScoreDrugs(Required(input.DrugsFrequency, "Recreational drug use")),
            [Keys.VegetablesFiber] = VegetablesFiberScorer.Score(input.VegetablesFiberServingsPerDay),
            [Keys.Neat] = NeatScorer.Score(input.DailyStepsNeat),
            [Keys.PhysicalTraining] = PhysicalTrainingScorer.Score(input.TrainingSessionsPerWeek),
            [Keys.FunctionalPower] = FunctionalPowerScorer.Score(
                input.PushUps, input.PullUps, input.BodyweightSquats, input.Sex, input.Age),
            [Keys.Cooper] = CooperScorer.Score(input.CooperDistanceMeters),
            [Keys.SkinHealth] = input.SkinHealth,
            [Keys.JawSkullHealth] = input.JawSkullHealth,
            [Keys.DentalHealth] = input.DentalHealth,
            [Keys.SpinalHealth] = input.SpinalHealth,
            [Keys.HairHealth] = input.HairHealth,
        };

        if (scores.Count != ParameterSet.Count || !ParameterSet.Keys.All(scores.ContainsKey))
        {
            throw new InvalidOperationException("The engine's scores don't match ParameterSet.");
        }

        var total = scores.Values.Sum();
        var fourStates = FourStateCalculator.Calculate(scores);

        return new HealthRatingResult
        {
            TotalHealthRating = total,
            MaxHealthRating = ParameterSet.MaxTotalScore,
            ParameterCount = ParameterSet.Count,
            ParameterSetVersion = ParameterSet.Version,
            Percentage = Math.Round((total / (double)ParameterSet.MaxTotalScore) * 100, 2),
            ParameterScores = scores,
            FourStates = fourStates,
            DerivedMetrics = derived,
            BodyFat = new BodyFatInfo
            {
                Percent = bodyFat,
                Source = input.BodyFatSource,
                EstimationMethod = input.BodyFatEstimationMethod,
            },
            HeartRateRecovery = new HeartRateRecoveryInfo
            {
                PeakHeartRateBpm = input.PeakHeartRateBpm,
                HeartRateAfter60sBpm = input.HeartRateAfter60sBpm,
                RecoveryBpm = input.HeartRateRecoveryBpm,
            },
        };
    }

    private static SubstanceFrequency Required(SubstanceFrequency? value, string name) =>
        value ?? throw new InvalidOperationException($"{name} is missing; validate the input first.");
}
