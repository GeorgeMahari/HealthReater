export type Sex = "Male" | "Female";

export type SubstanceFrequency =
  | "Daily"
  | "SeveralTimesPerWeek"
  | "Weekly"
  | "Monthly"
  | "Rarely"
  | "Never";

/** Mirrors HealthRater.Core.Models.AssessmentInput field-for-field (camelCase JSON). */
export interface AssessmentInput {
  // Basic Information
  sex: Sex | "";
  age: number | "";
  heightCm: number | "";
  weightKg: number | "";

  // Body Metrics
  waistCm: number | "";
  hipCm: number | "";
  bodyFatPercent: number | "";

  // Cardiovascular
  restingHeartRateBpm: number | "";
  heartRateRecoveryBpm: number | "";
  systolicBpMmHg: number | "";
  diastolicBpMmHg: number | "";

  // Energy & Sleep
  energyLevel: number;
  energyStability: number;
  averageSleepQuality: number;
  circadianHealth: number;

  // Mental & Emotional
  averageMood: number;
  moodStability: number;
  socialLife: number;
  jobSatisfaction: number;
  homeFamilySatisfaction: number;

  // Lifestyle
  dailyWaterIntakeLiters: number | "";
  digestionAndEvacuation: number;
  immuneHealth: number;
  caffeineServingsPerDay: number | "";
  junkFoodServingsPerWeek: number | "";
  overeatingEpisodesPerWeek: number | "";
  alcoholTobaccoDrugsFrequency: SubstanceFrequency | "";
  vegetablesFiberServingsPerDay: number | "";

  // Physical Performance
  dailyStepsNeat: number | "";
  trainingSessionsPerWeek: number | "";
  pushUps: number | "";
  pullUps: number | "";
  bodyweightSquats: number | "";
  cooperDistanceMeters: number | "";

  // General Health
  skinHealth: number;
  jawSkullHealth: number;
  dentalHealth: number;
  spinalHealth: number;
  hairHealth: number;
}

export interface DerivedMetrics {
  bmi: number;
  /** ASP.NET camel-cases the C# property "WHtR" to "wHtR". */
  wHtR: number;
  whr: number;
}

export interface StateScore {
  rawScore: number;
  maxRawScore: number;
  normalizedScore: number;
}

export interface FourStates {
  energyStrengthStamina: StateScore;
  mentalEmotional: StateScore;
  immunity: StateScore;
  longevity: StateScore;
}

export interface HealthRatingResult {
  /** Present when the result was saved to the signed-in user's history. */
  id?: string;
  /** UTC ISO timestamp of a saved assessment. */
  completedAt?: string;
  totalHealthRating: number;
  maxHealthRating: number;
  percentage: number;
  parameterScores: Record<string, number>;
  fourStates: FourStates;
  derivedMetrics: DerivedMetrics;
}

export interface ApiError {
  errors: string[];
}
