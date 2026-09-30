import type { AssessmentAnswers } from "../types";

/**
 * "bodyFat" and "heartRateRecovery" are composite fields with their own components
 * (BodyFatField, HeartRateRecoveryField); the rest are rendered by FieldInput.
 */
export type FieldKind = "select" | "number" | "scale" | "bodyFat" | "heartRateRecovery";

export interface FieldDef {
  key: keyof AssessmentAnswers;
  label: string;
  kind: FieldKind;
  unit?: string;
  min?: number;
  max?: number;
  step?: number;
  options?: { value: string; label: string }[];
  tooltip?: string;
  placeholder?: string;
}

export interface SectionDef {
  id: string;
  title: string;
  description: string;
  /** Terms (termTooltips keys) shown with an info tooltip under the description. */
  terms?: string[];
  fields: FieldDef[];
}

/** One frequency scale, used by each of the three independent substance questions. */
export const substanceFrequencyOptions: { value: string; label: string }[] = [
  { value: "Never", label: "Never" },
  { value: "Rarely", label: "Rarely" },
  { value: "Monthly", label: "Monthly" },
  { value: "Weekly", label: "Weekly" },
  { value: "SeveralTimesPerWeek", label: "Several times a week" },
  { value: "Daily", label: "Daily" },
];

export const sections: SectionDef[] = [
  {
    id: "basic-information",
    title: "Basic Information",
    description: "Your sex, age, height and weight come from your profile — review them and confirm.",
    // Read-only step: shows the profile data instead of asking for it (see ProfileSummary).
    fields: [],
  },
  {
    id: "body-metrics",
    title: "Body Metrics",
    description: "Waist, hip and body fat. BMI, WHtR and WHR are calculated automatically from these.",
    terms: ["bmi", "whtr", "whr"],
    fields: [
      {
        key: "waistCm",
        label: "Waist circumference",
        kind: "number",
        unit: "cm",
        min: 30,
        max: 250,
        tooltip: "Measured at the navel. Used with your height to calculate WHtR (waist-to-height ratio), a simple marker of central body fat.",
      },
      {
        key: "hipCm",
        label: "Hip circumference",
        kind: "number",
        unit: "cm",
        min: 30,
        max: 250,
        tooltip: "Measured at the widest point of the hips. Used with your waist to calculate WHR (waist-to-hip ratio).",
      },
      {
        key: "bodyFatPercent",
        label: "Body fat",
        kind: "bodyFat",
        unit: "%",
        min: 2,
        max: 75,
        step: 0.1,
        tooltip: "Measured with a body-composition scale, DEXA, calipers or similar. If you don't know it, HealthRater estimates it for you.",
      },
    ],
  },
  {
    id: "cardiovascular",
    title: "Cardiovascular",
    description: "Averaged over several days where possible, especially blood pressure.",
    fields: [
      { key: "restingHeartRateBpm", label: "Resting heart rate", kind: "number", unit: "bpm", min: 30, max: 220 },
      {
        key: "peakHeartRateBpm",
        label: "Heart Rate Recovery (HRR)",
        kind: "heartRateRecovery",
        unit: "bpm",
      },
      { key: "systolicBpMmHg", label: "Systolic blood pressure", kind: "number", unit: "mmHg", min: 60, max: 260 },
      { key: "diastolicBpMmHg", label: "Diastolic blood pressure", kind: "number", unit: "mmHg", min: 30, max: 160 },
    ],
  },
  {
    id: "energy-sleep",
    title: "Energy & Sleep",
    description: "How energized you feel, and how well your sleep and body clock support that.",
    fields: [
      { key: "energyLevel", label: "Energy level", kind: "scale" },
      { key: "energyStability", label: "Energy stability", kind: "scale", tooltip: "How steady your energy stays through the day, versus sharp crashes or spikes." },
      { key: "averageSleepQuality", label: "Average sleep quality", kind: "scale" },
      {
        key: "circadianHealth",
        label: "Circadian health",
        kind: "scale",
        tooltip: "How well-aligned your sleep/wake, meal and light-exposure timing is with a regular daily rhythm.",
      },
    ],
  },
  {
    id: "mental-emotional",
    title: "Mental & Emotional",
    description: "Mood and the relationships and roles that shape it.",
    fields: [
      { key: "averageMood", label: "Average mood", kind: "scale" },
      { key: "moodStability", label: "Mood stability", kind: "scale" },
      { key: "socialLife", label: "Friends / social life", kind: "scale" },
      { key: "jobSatisfaction", label: "Job satisfaction", kind: "scale" },
      { key: "homeFamilySatisfaction", label: "Home & family satisfaction", kind: "scale" },
    ],
  },
  {
    id: "lifestyle",
    title: "Lifestyle",
    description: "Daily habits: hydration, digestion, alcohol, tobacco, drugs and diet quality.",
    fields: [
      { key: "dailyWaterIntakeLiters", label: "Daily water intake", kind: "number", unit: "L/day", min: 0, max: 10, step: 0.1 },
      {
        key: "digestionAndEvacuation",
        label: "Digestion & evacuation",
        kind: "scale",
        tooltip: "How regular and comfortable your digestion and bowel movements are.",
      },
      { key: "immuneHealth", label: "Immune health", kind: "scale", tooltip: "How rarely and how mildly you get sick compared to those around you." },
      { key: "caffeineServingsPerDay", label: "Caffeine", kind: "number", unit: "servings/day", min: 0, max: 20 },
      { key: "junkFoodServingsPerWeek", label: "Junk food", kind: "number", unit: "servings/week", min: 0, max: 50 },
      { key: "overeatingEpisodesPerWeek", label: "Overeating / gluttony", kind: "number", unit: "episodes/week", min: 0, max: 21 },
      {
        key: "alcoholFrequency",
        label: "Alcohol consumption",
        kind: "select",
        options: substanceFrequencyOptions,
        tooltip: "How often you drink alcohol (any amount). Scored on its own, separately from tobacco and drugs.",
      },
      {
        key: "tobaccoFrequency",
        label: "Tobacco / smoking",
        kind: "select",
        options: substanceFrequencyOptions,
        tooltip: "How often you smoke or use tobacco or nicotine products (cigarettes, cigars, vapes, heated tobacco, snus).",
      },
      {
        key: "drugsFrequency",
        label: "Recreational drug use",
        kind: "select",
        options: substanceFrequencyOptions,
        tooltip: "How often you use recreational or non-prescribed drugs.",
      },
      { key: "vegetablesFiberServingsPerDay", label: "Vegetables & fiber", kind: "number", unit: "servings/day", min: 0, max: 15, step: 0.5 },
    ],
  },
  {
    id: "physical-performance",
    title: "Physical Performance",
    description: "Daily movement plus structured training and fitness tests.",
    fields: [
      {
        key: "dailyStepsNeat",
        label: "NEAT / daily movement",
        kind: "number",
        unit: "steps/day",
        min: 0,
        max: 40000,
        tooltip: "NEAT (Non-Exercise Activity Thermogenesis): everyday movement outside workouts — walking, chores, stairs — tracked here as daily steps.",
      },
      { key: "trainingSessionsPerWeek", label: "Physical training", kind: "number", unit: "sessions/week", min: 0, max: 21, step: 0.5 },
      { key: "pushUps", label: "Push-ups (max reps)", kind: "number", unit: "reps", min: 0, max: 300 },
      { key: "pullUps", label: "Pull-ups (max reps)", kind: "number", unit: "reps", min: 0, max: 100 },
      {
        key: "bodyweightSquats",
        label: "Bodyweight squats (max reps)",
        kind: "number",
        unit: "reps",
        min: 0,
        max: 300,
        tooltip: "Functional Power combines push-ups, pull-ups and bodyweight squats into one score, benchmarked against sex- and age-adjusted norms.",
      },
      {
        key: "cooperDistanceMeters",
        label: "12-minute Cooper run",
        kind: "number",
        unit: "meters",
        min: 0,
        max: 5000,
        tooltip: "The total distance you can run in 12 minutes — a classic cardiovascular fitness test. About 3,000m or more scores full marks.",
      },
    ],
  },
  {
    id: "general-health",
    title: "General Health",
    description: "The visible, physical markers that round out the full picture.",
    fields: [
      { key: "skinHealth", label: "Skin health", kind: "scale" },
      { key: "jawSkullHealth", label: "Jaw & skull health", kind: "scale" },
      { key: "dentalHealth", label: "Dental health / caries", kind: "scale" },
      { key: "spinalHealth", label: "Spinal health", kind: "scale" },
      { key: "hairHealth", label: "Hair health", kind: "scale" },
    ],
  },
];

export const longevityTooltip =
  "How strongly your current metrics point toward a long, low-risk healthspan — combining body composition, blood pressure, heart health, activity and substance use.";
