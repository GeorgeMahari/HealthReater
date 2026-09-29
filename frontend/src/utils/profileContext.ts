import { ageFromDateOfBirth } from "./dates";

export type SexValue = "Male" | "Female" | "";

/** Client-side mirror of the server rules (the API re-validates everything). */
export function validateProfileContext(sex: SexValue, dateOfBirth: string) {
  const errors: { sex?: string; dateOfBirth?: string } = {};
  if (sex !== "Male" && sex !== "Female") errors.sex = "Please select your sex.";
  if (!dateOfBirth) {
    errors.dateOfBirth = "Please enter your date of birth.";
  } else {
    const age = ageFromDateOfBirth(dateOfBirth);
    if (Number.isNaN(age)) errors.dateOfBirth = "Please enter a valid date.";
    else if (age < 0) errors.dateOfBirth = "Date of birth can't be in the future.";
    else if (age < 18 || age > 100) errors.dateOfBirth = "HealthRater is for people aged 18–100.";
  }
  return errors;
}
