export type Sex = "Male" | "Female";

export type BodyFatSource = "Measured" | "Estimated";

/** "known" = the user enters a measured value; "unknown" = HealthRater estimates it. */
export type BodyFatMode = "known" | "unknown";

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
  /** UI choice, not sent to the API: whether the user knows their body fat. */
  bodyFatMode: BodyFatMode | "";
  /** Sent as null when bodyFatMode is "unknown" (the API then estimates it). */
  bodyFatPercent: number | "";

  // Cardiovascular
  restingHeartRateBpm: number | "";
  /** Standardized HRR protocol: peak HR at the end of exercise… */
  peakHeartRateBpm: number | "";
  /** …and HR exactly 60 s after stopping. HRR = peak − after 60 s (calculated, never entered). */
  heartRateAfter60sBpm: number | "";
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
  /** Entered in ml/day; sent to the API as dailyWaterIntakeLiters (ml ÷ 1000). */
  dailyWaterIntakeMl: number | "";
  digestionAndEvacuation: number;
  immuneHealth: number;
  caffeineServingsPerDay: number | "";
  junkFoodServingsPerWeek: number | "";
  overeatingEpisodesPerWeek: number | "";
  alcoholFrequency: SubstanceFrequency | "";
  tobaccoFrequency: SubstanceFrequency | "";
  drugsFrequency: SubstanceFrequency | "";
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

/**
 * What the questionnaire collects. Sex, age, height and weight are NOT asked: the API takes
 * them from the signed-in user's profile (and ignores them if sent).
 */
export type AssessmentAnswers = Omit<AssessmentInput, "sex" | "age" | "heightCm" | "weightKg">;

/** Heart Rate Recovery from the two protocol readings, or null until both are valid. */
export function heartRateRecoveryOf(a: Pick<AssessmentAnswers, "peakHeartRateBpm" | "heartRateAfter60sBpm">): number | null {
  return a.peakHeartRateBpm === "" || a.heartRateAfter60sBpm === "" ? null : a.peakHeartRateBpm - a.heartRateAfter60sBpm;
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
  /** The maximum for the parameter set this result was scored with (410 now, 390 for v1). */
  maxHealthRating: number;
  percentage: number;
  parameterScores: Record<string, number>;
  fourStates: FourStates;
  derivedMetrics: DerivedMetrics;
}

/** GET /api/assessments/body-fat-estimate */
export interface BodyFatEstimate {
  bodyFatPercent: number;
  source: "Estimated";
  method: string;
  methodName: string;
  disclaimer: string;
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
  maxHealthRating: number;
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
  /** "v2-41" (current) or "v1-39" (saved before alcohol, tobacco and drugs were split). */
  parameterSetVersion: string;
  parameterCount: number;
  body: {
    /** Profile context recorded when the assessment was completed (never recalculated). */
    sexAtAssessment: string;
    ageAtAssessment: number;
    dateOfBirthAtAssessment: string | null;
    height: number;
    heightUnit: string;
    weight: number;
    weightUnit: string;
    waist: number;
    waistUnit: string;
    hip: number;
    hipUnit: string;
    bodyFatPercentage: number;
    bodyFatSource: BodyFatSource;
    bodyFatEstimationMethod: string | null;
  };
  cardiovascular: {
    restingHeartRate: number;
    heartRateRecovery: number;
    /** Null for assessments saved before the standardized HRR protocol. */
    peakHeartRate: number | null;
    heartRateAfter60Seconds: number | null;
    bloodPressureSystolic: number;
    bloodPressureDiastolic: number;
  };
  parameters: AssessmentParameter[];
  /** The answers as stored. Older (v1) snapshots use the fields of that time. */
  input: AssessmentInput;
}
