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

/** Four-state normalized scores (0–100) as stored with a saved assessment. */
export interface StateScores {
  energyStrengthStamina: number;
  mentalEmotional: number;
  immunity: number;
  longevity: number;
}

/** GET /api/assessments row. */
export interface AssessmentSummary {
  id: string;
  completedAt: string;
  totalHealthRating: number;
  maxHealthRating: number;
  percentage: number;
  fourStates: StateScores;
}

/** GET /api/assessments/calendar row; `date` is the local day (yyyy-MM-dd) in the requested time zone. */
export interface CalendarEntry extends StateScores {
  date: string;
  assessmentId: string;
  completedAt: string;
  totalHealthRating: number;
}

export interface AssessmentParameter {
  key: string;
  name: string;
  order: number;
  rawValue: number | null;
  rawText: string | null;
  normalizedValue: number | null;
  unit: string | null;
  score: number;
}

/** GET /api/assessments/{id}: the full stored snapshot (never recalculated). */
export interface AssessmentDetail extends HealthRatingResult {
  id: string;
  status: string;
  createdAt: string;
  completedAt: string;
  scoringVersion: string;
  body: {
    sex: string;
    ageAtAssessment: number;
    height: number;
    heightUnit: string;
    weight: number;
    weightUnit: string;
    waist: number;
    waistUnit: string;
    hip: number;
    hipUnit: string;
    bodyFatPercentage: number;
  };
  cardiovascular: {
    restingHeartRate: number;
    heartRateRecovery: number;
    bloodPressureSystolic: number;
    bloodPressureDiastolic: number;
  };
  parameters: AssessmentParameter[];
  input: AssessmentInput;
}
