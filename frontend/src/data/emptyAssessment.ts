import type { AssessmentAnswers } from "../types";

// Sex, age, height and weight are not part of the questionnaire: they come from the profile.
export const emptyAssessment: AssessmentAnswers = {

  waistCm: "",
  hipCm: "",
  bodyFatPercent: "",

  restingHeartRateBpm: "",
  heartRateRecoveryBpm: "",
  systolicBpMmHg: "",
  diastolicBpMmHg: "",

  energyLevel: 5,
  energyStability: 5,
  averageSleepQuality: 5,
  circadianHealth: 5,

  averageMood: 5,
  moodStability: 5,
  socialLife: 5,
  jobSatisfaction: 5,
  homeFamilySatisfaction: 5,

  dailyWaterIntakeLiters: "",
  digestionAndEvacuation: 5,
  immuneHealth: 5,
  caffeineServingsPerDay: "",
  junkFoodServingsPerWeek: "",
  overeatingEpisodesPerWeek: "",
  alcoholTobaccoDrugsFrequency: "",
  vegetablesFiberServingsPerDay: "",

  dailyStepsNeat: "",
  trainingSessionsPerWeek: "",
  pushUps: "",
  pullUps: "",
  bodyweightSquats: "",
  cooperDistanceMeters: "",

  skinHealth: 5,
  jawSkullHealth: 5,
  dentalHealth: 5,
  spinalHealth: 5,
  hairHealth: 5,
};
