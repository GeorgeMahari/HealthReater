import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import type { AssessmentAnswers, HealthRatingResult } from "../types";
import { emptyAssessment } from "../data/emptyAssessment";

interface AssessmentContextValue {
  assessment: AssessmentAnswers;
  setAssessment: (updater: (prev: AssessmentAnswers) => AssessmentAnswers) => void;
  updateField: <K extends keyof AssessmentAnswers>(key: K, value: AssessmentAnswers[K]) => void;
  result: HealthRatingResult | null;
  setResult: (result: HealthRatingResult | null) => void;
  resetAssessment: () => void;
}

const AssessmentContext = createContext<AssessmentContextValue | null>(null);

export function AssessmentProvider({ children }: { children: ReactNode }) {
  const [assessment, setAssessmentState] = useState<AssessmentAnswers>(emptyAssessment);
  const [result, setResult] = useState<HealthRatingResult | null>(null);

  const value = useMemo<AssessmentContextValue>(
    () => ({
      assessment,
      setAssessment: (updater) => setAssessmentState(updater),
      updateField: (key, fieldValue) =>
        setAssessmentState((prev) => ({ ...prev, [key]: fieldValue })),
      result,
      setResult,
      resetAssessment: () => {
        setAssessmentState(emptyAssessment);
        setResult(null);
      },
    }),
    [assessment, result]
  );

  return <AssessmentContext.Provider value={value}>{children}</AssessmentContext.Provider>;
}

export function useAssessment() {
  const ctx = useContext(AssessmentContext);
  if (!ctx) throw new Error("useAssessment must be used within an AssessmentProvider");
  return ctx;
}
