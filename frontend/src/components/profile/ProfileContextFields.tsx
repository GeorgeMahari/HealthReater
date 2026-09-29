import { Select } from "../Select";
import { DatePicker } from "../DatePicker";
import { TextField } from "../TextField";
import { ageFromDateOfBirth, dateOfBirthBounds } from "../../utils/dates";
import type { ProfileFieldErrors, SexValue } from "../../utils/profileContext";

interface ProfileContextFieldsProps {
  sex: SexValue;
  dateOfBirth: string;
  onSexChange: (value: SexValue) => void;
  onDateOfBirthChange: (value: string) => void;
  heightCm: string;
  weightKg: string;
  onHeightChange: (value: string) => void;
  onWeightChange: (value: string) => void;
  errors: ProfileFieldErrors;
  idPrefix: string;
}

/** Profile data inputs: sex, date of birth (with a live age preview), height and weight. */
export function ProfileContextFields({
  sex,
  dateOfBirth,
  onSexChange,
  onDateOfBirthChange,
  heightCm,
  weightKg,
  onHeightChange,
  onWeightChange,
  errors,
  idPrefix,
}: ProfileContextFieldsProps) {
  const bounds = dateOfBirthBounds();
  const age = dateOfBirth ? ageFromDateOfBirth(dateOfBirth) : null;

  return (
    <>
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
      <div className={`field ${errors.dateOfBirth ? "field-error" : ""}`}>
        <label className="field-label" htmlFor={`${idPrefix}-dob`}>
          Date of birth
        </label>
        <DatePicker
          id={`${idPrefix}-dob`}
          value={dateOfBirth}
          onChange={onDateOfBirthChange}
          min={bounds.min}
          max={bounds.max}
          invalid={Boolean(errors.dateOfBirth)}
          describedBy={`${idPrefix}-dob-hint`}
        />
        <p className="field-hint" id={`${idPrefix}-dob-hint`}>
          {age !== null && age >= 0 ? `You are ${age} years old.` : "Your age is calculated from this date."}
        </p>
        {errors.dateOfBirth && (
          <p className="field-error-text" role="alert">
            {errors.dateOfBirth}
          </p>
        )}
      </div>
    </div>
    <div className="auth-name-row">
      <TextField
        id={`${idPrefix}-height`}
        label="Height (cm)"
        type="number"
        inputMode="decimal"
        min={50}
        max={250}
        step={0.5}
        placeholder="e.g. 178"
        value={heightCm}
        onChange={onHeightChange}
        error={errors.heightCm}
      />
      <TextField
        id={`${idPrefix}-weight`}
        label="Weight (kg)"
        type="number"
        inputMode="decimal"
        min={20}
        max={400}
        step={0.1}
        placeholder="e.g. 74.5"
        value={weightKg}
        onChange={onWeightChange}
        error={errors.weightKg}
      />
    </div>
    </>
  );
}
