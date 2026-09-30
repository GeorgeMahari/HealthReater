/** Ranges mirror the API's validation (AssessmentAnswers / AssessmentValidator). */
export const HRR_LIMITS = { peakMin: 80, peakMax: 230, after60Min: 40, after60Max: 230, maxDrop: 100 } as const;

/** Client-side check mirroring the API's HRR validation; returns an error message or undefined. */
export function validateHeartRateRecovery(peak: number | "", after60: number | "", resting?: number | ""): string | undefined {
  if (peak === "" || after60 === "") return "Enter both your peak heart rate and your heart rate after 60 seconds.";
  if (peak < HRR_LIMITS.peakMin || peak > HRR_LIMITS.peakMax)
    return `Peak heart rate must be between ${HRR_LIMITS.peakMin} and ${HRR_LIMITS.peakMax} bpm.`;
  if (after60 < HRR_LIMITS.after60Min || after60 > HRR_LIMITS.after60Max)
    return `Heart rate after 60 seconds must be between ${HRR_LIMITS.after60Min} and ${HRR_LIMITS.after60Max} bpm.`;
  if (after60 > peak) return "Heart rate after 60 seconds can't be higher than the peak heart rate.";
  if (peak - after60 > HRR_LIMITS.maxDrop)
    return "A drop of more than 100 bpm in 60 seconds isn't plausible. Please re-check both readings.";
  if (typeof resting === "number" && peak <= resting)
    return "Peak heart rate after exercise must be higher than your resting heart rate.";
  return undefined;
}
