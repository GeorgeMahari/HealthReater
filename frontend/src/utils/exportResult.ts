import type { AssessmentInput, HealthRatingResult } from "../types";

/** Client-side export of the answers and the API result as JSON. */
export function saveResult(input: AssessmentInput, result: HealthRatingResult) {
  const payload = { savedAt: new Date().toISOString(), assessment: input, result };
  const blob = new Blob([JSON.stringify(payload, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `healthrater-result-${new Date().toISOString().slice(0, 10)}.json`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
