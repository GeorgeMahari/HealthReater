/**
 * Single source of truth for the scored parameter set on the frontend. Mirrors
 * HealthRater.Core.Scoring.ParameterSet — never hard-code the count or the maximum elsewhere.
 *
 * v2 (current): 41 parameters, max 410 (alcohol, tobacco and drugs are three parameters).
 * v1 (legacy):  39 parameters, max 390 (one combined "alcohol / tobacco / drugs" parameter).
 * Saved assessments carry their own maxHealthRating, so older ones still show "/ 390".
 */
export const PARAMETER_SET_VERSION = "v2-41";
export const TOTAL_PARAMETER_COUNT = 41;
export const MAX_SCORE_PER_PARAMETER = 10;
export const MAX_TOTAL_SCORE = TOTAL_PARAMETER_COUNT * MAX_SCORE_PER_PARAMETER;

export const LEGACY_PARAMETER_SET_VERSION = "v1-39";
