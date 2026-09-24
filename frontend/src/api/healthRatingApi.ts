import type { AssessmentInput, HealthRatingResult } from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

export class HealthRatingApiError extends Error {
  errors: string[];
  constructor(errors: string[]) {
    super(errors.join(" "));
    this.errors = errors;
  }
}

export function calculateHealthRating(input: AssessmentInput): Promise<HealthRatingResult> {
  return postAssessment("/api/health-rating/calculate", input);
}

/**
 * Signed-in users: scores the assessment AND stores it in their history. The response is
 * the saved snapshot, a superset of HealthRatingResult (adds id, completedAt, …).
 */
export function saveAssessment(input: AssessmentInput): Promise<HealthRatingResult> {
  return postAssessment("/api/assessments", input);
}

async function postAssessment(path: string, input: AssessmentInput): Promise<HealthRatingResult> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const errors: string[] = body?.errors
      ? flattenErrors(body.errors)
      : ["The server could not calculate your rating. Please try again."];
    throw new HealthRatingApiError(errors);
  }

  return (await response.json()) as HealthRatingResult;
}

// ASP.NET's built-in [ApiController] validation returns errors as
// { errors: { FieldName: ["msg1", "msg2"] } } while our own AssessmentValidator
// returns { errors: ["msg1", "msg2"] }. Normalize both shapes to a flat string list.
export function flattenErrors(errors: unknown): string[] {
  if (Array.isArray(errors)) {
    return errors.every((e) => typeof e === "string")
      ? (errors as string[])
      : errors.flatMap((e) => flattenErrors(e));
  }
  if (errors && typeof errors === "object") {
    return Object.values(errors as Record<string, unknown>).flatMap((v) =>
      flattenErrors(v)
    );
  }
  return [String(errors)];
}
