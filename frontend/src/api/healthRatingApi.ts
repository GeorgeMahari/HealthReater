import type { AssessmentAnswers, AssessmentDetail, BodyFatEstimate } from "../types";
import { apiRequest } from "./http";

/**
 * The request body: the answers without UI-only fields; unknown body fat is sent as null.
 * Water is entered in ml/day but the API scores liters/day, so it's converted (1000 ml = 1 L).
 */
export function toRequest(answers: AssessmentAnswers) {
  const { bodyFatMode, dailyWaterIntakeMl, ...rest } = answers;
  return {
    ...rest,
    bodyFatPercent: bodyFatMode === "unknown" ? null : rest.bodyFatPercent,
    dailyWaterIntakeLiters: dailyWaterIntakeMl === "" ? "" : dailyWaterIntakeMl / 1000,
  };
}

/**
 * Scores the answers and saves them to the signed-in user's history. Sex, age, height and
 * weight come from the user's profile on the server; an unknown body fat is estimated there.
 * Throws ApiError (401 signed out, 409 profile incomplete).
 */
export function saveAssessment(answers: AssessmentAnswers): Promise<AssessmentDetail> {
  return apiRequest<AssessmentDetail>("/api/assessments", { method: "POST", json: toRequest(answers) });
}

/**
 * The body-fat estimate the API will use for "I don't know", from the signed-in user's
 * profile. Throws ApiError (409 profile incomplete, 422 no plausible estimate).
 */
export function getBodyFatEstimate(waistCm?: number | "", hipCm?: number | ""): Promise<BodyFatEstimate> {
  const params = new URLSearchParams();
  if (typeof waistCm === "number") params.set("waistCm", String(waistCm));
  if (typeof hipCm === "number") params.set("hipCm", String(hipCm));
  const query = params.toString();
  return apiRequest<BodyFatEstimate>(`/api/assessments/body-fat-estimate${query ? `?${query}` : ""}`);
}
