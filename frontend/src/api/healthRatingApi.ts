import type { AssessmentAnswers, HealthRatingResult } from "../types";
import { apiRequest } from "./http";

/**
 * Scores the answers and saves them to the signed-in user's history. Sex and age come from
 * the user's profile on the server. Throws ApiError (401 signed out, 409 profile incomplete).
 */
export function saveAssessment(answers: AssessmentAnswers): Promise<HealthRatingResult> {
  return apiRequest<HealthRatingResult>("/api/assessments", { method: "POST", json: answers });
}
