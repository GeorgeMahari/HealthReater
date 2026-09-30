// Presentation-only metadata: icons, accents and groupings for the UI.
// Nothing here affects scoring — it only decides how API results are displayed.
import {
  Activity,
  Brain,
  ClipboardCheck,
  Dumbbell,
  HeartPulse,
  Hourglass,
  Leaf,
  Moon,
  Ruler,
  ScanFace,
  Shield,
  User,
  type LucideIcon,
} from "lucide-react";
import type { FourStates } from "../types";

export interface StateMeta {
  key: keyof FourStates;
  title: string;
  desc: string;
  icon: LucideIcon;
  /** Visual identity modifier (see .tone-* in CSS). */
  tone: "energy" | "mental" | "immunity" | "longevity";
  /** How many of the scored parameters feed this state (matches the backend FourStateCalculator). */
  parameterCount: number;
  tooltip?: string;
}

export const LONGEVITY_TOOLTIP =
  "How strongly your current metrics point toward a long, low-risk healthspan — combining body composition, blood pressure, heart health, activity and substance use. Not a clinical prediction.";

export const stateMeta: StateMeta[] = [
  {
    key: "energyStrengthStamina",
    title: "Energy, Strength & Stamina",
    desc: "Energy, sleep, circadian rhythm, heart fitness and physical performance.",
    icon: Activity,
    tone: "energy",
    parameterCount: 10,
  },
  {
    key: "mentalEmotional",
    title: "Mental & Emotional",
    desc: "Mood, stability, relationships, work and home satisfaction.",
    icon: Brain,
    tone: "mental",
    parameterCount: 7,
  },
  {
    key: "immunity",
    title: "Immunity",
    desc: "Hydration, digestion, diet quality, alcohol, tobacco and drugs.",
    icon: Shield,
    tone: "immunity",
    parameterCount: 13,
  },
  {
    key: "longevity",
    title: "Longevity",
    desc: "Body composition, blood pressure, heart health and lifestyle risk factors.",
    icon: Hourglass,
    tone: "longevity",
    parameterCount: 15,
    tooltip: LONGEVITY_TOOLTIP,
  },
];

export const sectionIcons: Record<string, LucideIcon> = {
  "basic-information": User,
  "body-metrics": Ruler,
  cardiovascular: HeartPulse,
  "energy-sleep": Moon,
  "mental-emotional": Brain,
  lifestyle: Leaf,
  "physical-performance": Dumbbell,
  "general-health": ScanFace,
  review: ClipboardCheck,
};

/** Maps each backend parameterScores key to its assessment section, for the grouped results view. */
export const parameterGroups: { sectionId: string; title: string; keys: string[] }[] = [
  { sectionId: "basic-information", title: "Basic Information", keys: ["sex", "age", "height", "weight"] },
  { sectionId: "body-metrics", title: "Body Metrics", keys: ["waist", "hip", "bodyFat", "bmi", "whtr", "whr"] },
  { sectionId: "cardiovascular", title: "Cardiovascular", keys: ["restingHeartRate", "heartRateRecovery", "bloodPressure"] },
  { sectionId: "energy-sleep", title: "Energy & Sleep", keys: ["energyLevel", "energyStability", "sleepQuality", "circadianHealth"] },
  {
    sectionId: "mental-emotional",
    title: "Mental & Emotional",
    keys: ["mood", "moodStability", "socialLife", "jobSatisfaction", "homeFamilySatisfaction"],
  },
  {
    sectionId: "lifestyle",
    title: "Lifestyle",
    keys: ["hydration", "digestion", "immuneHealth", "caffeine", "junkFood", "overeating", "alcohol", "tobacco", "drugs",
      // Legacy (v1): assessments saved before the split keep one combined parameter.
      "substanceUse", "vegetablesFiber"],
  },
  { sectionId: "physical-performance", title: "Physical Performance", keys: ["neat", "physicalTraining", "functionalPower", "cooper"] },
  {
    sectionId: "general-health",
    title: "General Health",
    keys: ["skinHealth", "jawSkullHealth", "dentalHealth", "spinalHealth", "hairHealth"],
  },
];

/** Friendlier labels for parameter keys whose camelCase split reads poorly. */
export const parameterLabels: Record<string, string> = {
  bmi: "BMI",
  whtr: "WHtR",
  whr: "WHR",
  neat: "NEAT",
  cooper: "Cooper run",
  substanceUse: "Alcohol / tobacco / drugs",
  alcohol: "Alcohol consumption",
  tobacco: "Tobacco / smoking",
  drugs: "Recreational drug use",
  jawSkullHealth: "Jaw & skull health",
  homeFamilySatisfaction: "Home & family",
  vegetablesFiber: "Vegetables & fiber",
  restingHeartRate: "Resting heart rate",
  heartRateRecovery: "Heart Rate Recovery (HRR)",
};

/** Shown next to an estimated body-fat value (assessment step and results). */
export const BODY_FAT_ESTIMATE_TOOLTIP =
  "Body fat is estimated from available body measurements and demographic data. This is an estimate and may differ from direct body-composition measurements.";

/** Standardized Heart Rate Recovery protocol shown in the assessment. */
export const HRR_PROTOCOL_STEPS = [
  "Warm up for 3–5 minutes at an easy pace.",
  "Exercise hard for about 3 minutes (fast running, stair climbing, cycling or step-ups) until breathing is heavy and talking is difficult.",
  "At the very end of the effort, read your heart rate on a chest strap, watch or pulse monitor: this is your peak heart rate.",
  "Stop exercising and start a timer immediately.",
  "Stay still in the same position (standing or sitting). Don't walk around or cool down.",
  "Read your heart rate again at exactly 60 seconds: this is your heart rate after 60 seconds.",
  "Enter both values. HealthRater calculates HRR = peak heart rate − heart rate after 60 seconds. Repeat the test the same way each time so results are comparable.",
];

export const HRR_SAFETY_NOTE =
  "If you have a medical condition, symptoms, or have been advised to avoid strenuous exercise, do not perform this test without appropriate medical guidance.";

export const termTooltips: Record<string, string> = {
  bmi: "BMI (Body Mass Index) is your weight relative to your height (kg ÷ m²). It's a quick screening metric, but it doesn't distinguish muscle from fat. Informational only, not a diagnosis.",
  whtr: "WHtR (Waist-to-Height Ratio) compares your waist circumference with your height. It's an indicator of central (abdominal) fat. Informational only, not a diagnosis.",
  whr: "WHR (Waist-to-Hip Ratio) compares your waist circumference with your hip circumference. It describes how your body fat is distributed. Informational only, not a diagnosis.",
  neat: "Non-Exercise Activity Thermogenesis: movement from daily life (walking, chores, stairs), tracked here as daily steps.",
  functionalPower: "Push-ups, pull-ups and bodyweight squats combined into one score, benchmarked against sex- and age-adjusted norms.",
  heartRateRecovery:
    "Heart Rate Recovery (HRR) is the decrease in heart rate during the first 60 seconds after exercise stops. HealthRater uses the difference between peak heart rate and heart rate measured exactly 60 seconds later.",
  alcohol: "How often you drink alcohol. Scored independently of tobacco and drugs.",
  tobacco: "How often you smoke or use tobacco or nicotine products. Scored independently of alcohol and drugs.",
  drugs: "How often you use recreational or non-prescribed drugs. Scored independently of alcohol and tobacco.",
  substanceUse: "Recorded before alcohol, tobacco and drugs became three separate parameters (earlier 39-parameter assessments).",
  circadianHealth: "How regular your sleep/wake, meal and light-exposure timing is across the day.",
  digestion: "How regular and comfortable your digestion and bowel movements are.",
};
