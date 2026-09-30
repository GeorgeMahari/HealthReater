import { useEffect, useState } from "react";
import { getBodyFatEstimate } from "../../api/healthRatingApi";
import { ApiError } from "../../api/http";
import type { BodyFatEstimate } from "../../types";

export type EstimateState =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "ready"; estimate: BodyFatEstimate }
  | { kind: "error"; message: string };

/**
 * The body-fat estimate the API will use for "I don't know" (from the signed-in user's
 * profile). Only a preview — the value is calculated again on the server when submitting.
 */
export function useBodyFatEstimate(enabled: boolean): EstimateState {
  const [loaded, setLoaded] = useState<EstimateState | null>(null);

  useEffect(() => {
    if (!enabled || loaded) return;
    let ignore = false;
    getBodyFatEstimate()
      .then((estimate) => {
        if (!ignore) setLoaded({ kind: "ready", estimate });
      })
      .catch((err) => {
        if (ignore) return;
        setLoaded({
          kind: "error",
          message: err instanceof ApiError ? err.message : "The estimate couldn't be loaded. Please try again.",
        });
      });
    return () => {
      ignore = true;
    };
  }, [enabled, loaded]);

  if (!enabled) return { kind: "idle" };
  return loaded ?? { kind: "loading" };
}
