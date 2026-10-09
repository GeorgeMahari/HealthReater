import { Check, ShieldAlert } from "lucide-react";
import type { FieldDef } from "../../data/sections";
import { HRR_PROTOCOL_STEPS, HRR_SAFETY_NOTE, termTooltips } from "../../data/uiMeta";
import { heartRateRecoveryOf, type AssessmentAnswers } from "../../types";
import { Tooltip } from "../Tooltip";
import { HRR_LIMITS } from "./heartRateRecovery";

interface HeartRateRecoveryFieldProps {
  field: FieldDef;
  peak: AssessmentAnswers["peakHeartRateBpm"];
  after60: AssessmentAnswers["heartRateAfter60sBpm"];
  onPeakChange: (value: number | "") => void;
  onAfter60Change: (value: number | "") => void;
  error?: string;
}

/**
 * Standardized Heart Rate Recovery: the user enters their peak heart rate and the heart rate
 * exactly 60 seconds after stopping; HRR (the drop) is calculated and shown read-only.
 */
export function HeartRateRecoveryField({ field, peak, after60, onPeakChange, onAfter60Change, error }: HeartRateRecoveryFieldProps) {
  const hrr = heartRateRecoveryOf({ peakHeartRateBpm: peak, heartRateAfter60sBpm: after60 });
  const errorId = "hrr-error";
  const valid = !error && hrr !== null && hrr >= 0 && hrr <= HRR_LIMITS.maxDrop;
  const num = (v: string) => (v === "" ? "" : Number(v));

  return (
    <div className={`field field-composite hrr-field ${error ? "field-error" : valid ? "field-valid" : ""}`}>
      <span className="field-label" id="hrr-label">
        {field.label}
        <Tooltip text={termTooltips.heartRateRecovery} label="About Heart Rate Recovery" />
      </span>

      <div className="hrr-inputs">
        <div className="hrr-input">
          <label className="hrr-sublabel" htmlFor="peakHeartRateBpm">
            Peak heart rate <span className="field-unit">(bpm)</span>
          </label>
          <div className="field-control">
            <input
              id="peakHeartRateBpm"
              className="input"
              type="number"
              inputMode="numeric"
              value={peak}
              min={HRR_LIMITS.peakMin}
              max={HRR_LIMITS.peakMax}
              placeholder={`${HRR_LIMITS.peakMin} – ${HRR_LIMITS.peakMax}`}
              aria-invalid={error ? true : undefined}
              aria-describedby={error ? errorId : undefined}
              onChange={(e) => onPeakChange(num(e.target.value))}
            />
          </div>
        </div>
        <div className="hrr-input">
          <label className="hrr-sublabel" htmlFor="heartRateAfter60sBpm">
            Heart rate after 60 s <span className="field-unit">(bpm)</span>
          </label>
          <div className="field-control">
            <input
              id="heartRateAfter60sBpm"
              className="input"
              type="number"
              inputMode="numeric"
              value={after60}
              min={HRR_LIMITS.after60Min}
              max={HRR_LIMITS.after60Max}
              placeholder={`${HRR_LIMITS.after60Min} – ${HRR_LIMITS.after60Max}`}
              aria-invalid={error ? true : undefined}
              aria-describedby={error ? errorId : undefined}
              onChange={(e) => onAfter60Change(num(e.target.value))}
            />
          </div>
        </div>
        <div className="hrr-input">
          <span className="hrr-sublabel" id="hrr-result-label">
            HRR <span className="field-unit">(calculated)</span>
          </span>
          <output
            className="hrr-result"
            htmlFor="peakHeartRateBpm heartRateAfter60sBpm"
            aria-labelledby="hrr-result-label"
            data-testid="hrr-result"
          >
            {hrr === null ? "—" : `${hrr} bpm`}
            {valid && (
              <span className="field-check hrr-check" aria-hidden="true">
                <Check size={14} strokeWidth={2.6} />
              </span>
            )}
          </output>
        </div>
      </div>

      {error && (
        <p className="field-error-text" id={errorId} role="alert">
          {error}
        </p>
      )}

      <details className="protocol-panel">
        <summary>How to measure Heart Rate Recovery</summary>
        <ol className="protocol-steps">
          {HRR_PROTOCOL_STEPS.map((step) => (
            <li key={step}>{step}</li>
          ))}
        </ol>
        <p className="protocol-safety" role="note">
          <ShieldAlert size={16} strokeWidth={2} aria-hidden="true" />
          {HRR_SAFETY_NOTE}
        </p>
      </details>
    </div>
  );
}
