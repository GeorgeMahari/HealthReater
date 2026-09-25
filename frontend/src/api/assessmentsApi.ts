import type { AssessmentDetail, AssessmentSummary, CalendarEntry } from "../types";
import { apiRequest } from "./http";

// No user id is ever sent: the API returns only the signed-in user's own assessments.
export const assessmentsApi = {
  list: () => apiRequest<AssessmentSummary[]>("/api/assessments"),

  calendar: (year: number, month: number, timeZone: string) =>
    apiRequest<CalendarEntry[]>(
      `/api/assessments/calendar?year=${year}&month=${month}&timeZone=${encodeURIComponent(timeZone)}`
    ),

  get: (id: string) => apiRequest<AssessmentDetail>(`/api/assessments/${encodeURIComponent(id)}`),
};
