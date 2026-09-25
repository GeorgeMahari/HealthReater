import type { InputHTMLAttributes, ReactNode } from "react";

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "onChange"> {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  hint?: ReactNode;
}

/** Labelled text input using the shared field styles, with an associated error message. */
export function TextField({ id, label, value, onChange, error, hint, ...input }: TextFieldProps) {
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;
  const describedBy = [error ? errorId : null, hint ? hintId : null].filter(Boolean).join(" ") || undefined;

  return (
    <div className={`field ${error ? "field-error" : ""}`}>
      <label className="field-label" htmlFor={id}>
        {label}
      </label>
      <div className="field-control">
        <input
          {...input}
          id={id}
          name={id}
          className="input"
          value={value}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          onChange={(e) => onChange(e.target.value)}
        />
      </div>
      {hint && (
        <p className="field-hint" id={hintId}>
          {hint}
        </p>
      )}
      {error && (
        <p className="field-error-text" id={errorId} role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
