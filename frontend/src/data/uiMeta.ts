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
  /** How many of the 39 parameters feed this state (matches the backend grouping). */
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
    desc: "Hydration, digestion, diet quality and substance use.",
    icon: Shield,
    tone: "immunity",
    parameterCount: 11,
  },
  {
    key: "longevity",
    title: "Longevity",
    desc: "Body composition, blood pressure, heart health and lifestyle risk factors.",
    icon: Hourglass,
    tone: "longevity",
    parameterCount: 13,
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
    keys: ["hydration", "digestion", "immuneHealth", "caffeine", "junkFood", "overeating", "substanceUse", "vegetablesFiber"],
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
  jawSkullHealth: "Jaw & skull health",
  homeFamilySatisfaction: "Home & family",
  vegetablesFiber: "Vegetables & fiber",
  restingHeartRate: "Resting heart rate",
  heartRateRecovery: "Heart-rate recovery",
};

export const termTooltips: Record<string, string> = {
  bmi: "Body Mass Index — weight (kg) divided by height (m) squared. Calculated automatically.",
  whtr: "Waist-to-Height Ratio — waist ÷ height. A simple marker of central body fat; around 0.5 or below is generally favourable.",
  whr: "Waist-to-Hip Ratio — waist ÷ hip. Another view of where body fat is stored.",
  neat: "Non-Exercise Activity Thermogenesis: movement from daily life (walking, chores, stairs), tracked here as daily steps.",
  functionalPower: "Push-ups, pull-ups and bodyweight squats combined into one score, benchmarked against sex- and age-adjusted norms.",
  heartRateRecovery: "How many bpm your heart rate falls in the first minute after intense exercise. A bigger drop means a fitter heart.",
  circadianHealth: "How regular your sleep/wake, meal and light-exposure timing is across the day.",
  digestion: "How regular and comfortable your digestion and bowel movements are.",
};
