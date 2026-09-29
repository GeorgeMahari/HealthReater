import { Select } from "../Select";
import { TextField } from "../TextField";
import { ageFromDateOfBirth, dateOfBirthBounds } from "../../utils/dates";
import type { SexValue } from "../../utils/profileContext";

interface ProfileContextFieldsProps {
  sex: SexValue;
  dateOfBirth: string;
  onSexChange: (value: SexValue) => void;
  onDateOfBirthChange: (value: string) => void;
  errors: { sex?: string; dateOfBirth?: string };
  idPrefix: string;
}

/** Sex + date of birth inputs with a live "you are N years old" preview. */
export function ProfileContextFields({
  sex,
  dateOfBirth,
  onSexChange,
  onDateOfBirthChange,
  errors,
  idPrefix,
}: ProfileContextFieldsProps) {
  const bounds = dateOfBirthBounds();
  const age = dateOfBirth ? ageFromDateOfBirth(dateOfBirth) : null;

  return (
    <div className="auth-name-row">
      <div className={`field ${errors.sex ? "field-error" : ""}`}>
        <label className="field-label" htmlFor={`${idPrefix}-sex`}>
          Sex
        </label>
        <Select
          id={`${idPrefix}-sex`}
          value={sex}
          placeholder="Select…"
          options={[
            { value: "Male", label: "Male" },
            { value: "Female", label: "Female" },
          ]}
          onChange={(v) => onSexChange(v as SexValue)}
        />
        {errors.sex && (
          <p className="field-error-text" role="alert">
            {errors.sex}
          </p>
        )}
      </div>
      <TextField
        id={`${idPrefix}-dob`}
        label="Date of birth"
        type="date"
        min={bounds.min}
        max={bounds.max}
        value={dateOfBirth}
        onChange={onDateOfBirthChange}
        error={errors.dateOfBirth}
        hint={age !== null && age >= 0 ? `You are ${age} years old.` : undefined}
      />
    </div>
  );
}
