import { Check, LoaderCircle } from "lucide-react";
import type { FieldDef } from "../../data/sections";
import { BODY_FAT_ESTIMATE_TOOLTIP } from "../../data/uiMeta";
import type { AssessmentAnswers, BodyFatMode } from "../../types";
import { Tooltip } from "../Tooltip";
import { useBodyFatEstimate } from "./bodyFatEstimate";

interface BodyFatFieldProps {
  field: FieldDef;
  mode: AssessmentAnswers["bodyFatMode"];
  value: AssessmentAnswers["bodyFatPercent"];
  onModeChange: (mode: BodyFatMode) => void;
  onValueChange: (value: number | "") => void;
  error?: string;
}

const choices: { mode: BodyFatMode; label: string }[] = [
  { mode: "known", label: "I know my body fat" },
  { mode: "unknown", label: "I don't know" },
];

/** Body fat: enter a measured value, or let HealthRater estimate it. */
export function BodyFatField({ field, mode, value, onModeChange, onValueChange, error }: BodyFatFieldProps) {
  const estimate = useBodyFatEstimate(mode === "unknown");
  const errorId = `${field.key}-error`;
  const valid = !error && mode === "known" && typeof value === "number" && value >= (field.min ?? 0) && value <= (field.max ?? 100);

  return (
    <div className={`field field-composite ${error ? "field-error" : valid ? "field-valid" : ""}`}>
      <span className="field-label" id="bodyFat-label">
        {field.label}
        {field.unit && <span className="field-unit"> ({field.unit})</span>}
        {field.tooltip && <Tooltip text={field.tooltip} label={`About ${field.label}`} />}
      </span>

      <div className="segmented" role="radiogroup" aria-labelledby="bodyFat-label">
        {choices.map((c) => (
          <button
            key={c.mode}
            type="button"
            role="radio"
            id={`bodyFat-${c.mode}`}
            aria-checked={mode === c.mode}
            className={`segmented-option ${mode === c.mode ? "segmented-active" : ""}`}
            onClick={() => onModeChange(c.mode)}
          >
            {mode === c.mode && <Check size={14} strokeWidth={2.6} aria-hidden="true" />}
            {c.label}
          </button>
        ))}
      </div>

      {mode === "known" && (
        <div className="field-control">
          <label className="sr-only" htmlFor={field.key}>
            Measured body fat (%)
          </label>
          <input
            id={field.key}
            className="input"
            type="number"
            inputMode="decimal"
            value={value}
            min={field.min}
            max={field.max}
            step={field.step ?? 0.1}
            placeholder={`${field.min} – ${field.max}`}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            onChange={(e) => onValueChange(e.target.value === "" ? "" : Number(e.target.value))}
          />
          <span className="field-check" aria-hidden="true">
            <Check size={14} strokeWidth={2.6} />
          </span>
        </div>
      )}

      {mode === "unknown" && (
        <div className="estimate-box" aria-live="polite">
          {estimate.kind === "loading" && (
            <p className="estimate-loading">
              <LoaderCircle className="spin" size={15} strokeWidth={2} aria-hidden="true" />
              Estimating from your profile…
            </p>
          )}
          {estimate.kind === "ready" && (
            <>
              <p className="estimate-value">
                <strong>{estimate.estimate.bodyFatPercent}%</strong>
                <span className="source-badge source-estimated">Estimated</span>
                <Tooltip text={BODY_FAT_ESTIMATE_TOOLTIP} label="About estimated body fat" />
              </p>
              <p className="estimate-note">
                Calculated from your BMI, age and sex using the {estimate.estimate.methodName}. It's scored like a
                measured value and marked as estimated in your results.
              </p>
            </>
          )}
          {estimate.kind === "error" && (
            <p className="estimate-error" role="alert">
              {estimate.message}
            </p>
          )}
        </div>
      )}

      {error && (
        <p className="field-error-text" id={errorId} role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
