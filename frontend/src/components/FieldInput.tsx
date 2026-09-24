import type { CSSProperties } from "react";
import { Check } from "lucide-react";
import type { FieldDef } from "../data/sections";
import type { AssessmentInput } from "../types";
import { Select } from "./Select";
import { Tooltip } from "./Tooltip";

interface FieldInputProps {
  field: FieldDef;
  value: AssessmentInput[keyof AssessmentInput];
  onChange: (value: AssessmentInput[keyof AssessmentInput]) => void;
  error?: string;
}

// Visual "looks good" hint only — the real validation still lives in AssessmentPage.
function looksValid(field: FieldDef, value: unknown): boolean {
  if (field.kind === "scale") return false;
  if (value === "" || value === undefined || value === null) return false;
  if (field.kind === "number" && typeof value === "number") {
    if (field.min !== undefined && value < field.min) return false;
    if (field.max !== undefined && value > field.max) return false;
  }
  return true;
}

export function FieldInput({ field, value, onChange, error }: FieldInputProps) {
  const valid = !error && looksValid(field, value);
  const errorId = `${field.key}-error`;
  const stateClass = error ? "field-error" : valid ? "field-valid" : "";

  return (
    <div className={`field field-${field.kind} ${stateClass}`}>
      <label className="field-label" htmlFor={field.key}>
        {field.label}
        {field.unit && <span className="field-unit"> ({field.unit})</span>}
        {field.tooltip && <Tooltip text={field.tooltip} label={`About ${field.label}`} />}
      </label>

      {field.kind === "select" && (
        <div className="field-control">
          <Select
            id={field.key}
            value={(value as string) ?? ""}
            options={field.options ?? []}
            onChange={(v) => onChange(v as never)}
          />
        </div>
      )}

      {field.kind === "number" && (
        <div className="field-control">
          <input
            id={field.key}
            type="number"
            inputMode="decimal"
            value={value === "" || value === undefined ? "" : (value as number)}
            min={field.min}
            max={field.max}
            step={field.step ?? 1}
            placeholder={
              field.placeholder ??
              (field.min !== undefined && field.max !== undefined ? `${field.min} – ${field.max}` : undefined)
            }
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            onChange={(e) =>
              onChange((e.target.value === "" ? "" : Number(e.target.value)) as never)
            }
          />
          <span className="field-check" aria-hidden="true">
            <Check size={14} strokeWidth={2.6} />
          </span>
        </div>
      )}

      {field.kind === "scale" && (
        <div className="scale-input">
          <input
            id={field.key}
            type="range"
            min={1}
            max={10}
            step={1}
            value={(value as number) ?? 5}
            style={{ "--fill": `${((((value as number) ?? 5) - 1) / 9) * 100}%` } as CSSProperties}
            onChange={(e) => onChange(Number(e.target.value) as never)}
          />
          <span className="scale-value" aria-hidden="true">
            {value as number}
            <small>/10</small>
          </span>
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
