// All timestamps arrive from the API in UTC; they are shown in the viewer's own time zone.
// Dates use the "25 September 2026" day-month-year form throughout the app.
const LOCALE = "en-GB";

export const viewerTimeZone = (): string => Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";

/** "25 September 2026" */
export const formatLongDate = (iso: string | Date) =>
  new Intl.DateTimeFormat(LOCALE, { day: "numeric", month: "long", year: "numeric" }).format(new Date(iso));

/** "25 Sep 2026" */
export const formatShortDate = (iso: string | Date) =>
  new Intl.DateTimeFormat(LOCALE, { day: "numeric", month: "short", year: "numeric" }).format(new Date(iso));

/** "25 Sep" */
export const formatDayMonth = (iso: string | Date) =>
  new Intl.DateTimeFormat(LOCALE, { day: "numeric", month: "short" }).format(new Date(iso));

/** "14:32" */
export const formatTime = (iso: string | Date) =>
  new Intl.DateTimeFormat(LOCALE, { hour: "2-digit", minute: "2-digit" }).format(new Date(iso));

/** "September 2026" */
export const formatMonthYear = (year: number, month: number) =>
  new Intl.DateTimeFormat(LOCALE, { month: "long", year: "numeric" }).format(new Date(year, month - 1, 1));

/** Local calendar key "yyyy-MM-dd" for a Date in the viewer's time zone. */
export const localDateKey = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

/**
 * Completed years from a "yyyy-MM-dd" date of birth to today (viewer's calendar). Only for
 * previews: the API calculates the authoritative age. NaN for an invalid date.
 */
export function ageFromDateOfBirth(dateOfBirth: string, today = new Date()): number {
  const [y, m, d] = dateOfBirth.split("-").map(Number);
  if (!y || !m || !d) return NaN;
  let age = today.getFullYear() - y;
  if (today.getMonth() + 1 < m || (today.getMonth() + 1 === m && today.getDate() < d)) age--;
  return age;
}

/** min/max for a date-of-birth input so the age is 18–100 today. */
export function dateOfBirthBounds(today = new Date()) {
  const shift = (years: number) => localDateKey(new Date(today.getFullYear() - years, today.getMonth(), today.getDate()));
  return { min: shift(100), max: shift(18) };
}
