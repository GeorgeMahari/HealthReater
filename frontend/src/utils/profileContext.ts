import { ageFromDateOfBirth } from "./dates";

export type SexValue = "Male" | "Female" | "";

export type ProfileFieldErrors = { sex?: string; dateOfBirth?: string; heightCm?: string; weightKg?: string };

/** Client-side mirror of the server rules (the API re-validates everything). */
export function validateProfileContext(sex: SexValue, dateOfBirth: string, heightCm: string, weightKg: string) {
  const errors: ProfileFieldErrors = {};
  if (sex !== "Male" && sex !== "Female") errors.sex = "Please select your sex.";
  if (!dateOfBirth) {
    errors.dateOfBirth = "Please enter your date of birth.";
  } else {
    const age = ageFromDateOfBirth(dateOfBirth);
    if (Number.isNaN(age)) errors.dateOfBirth = "Please enter a valid date.";
    else if (age < 0) errors.dateOfBirth = "Date of birth can't be in the future.";
    else if (age < 18 || age > 100) errors.dateOfBirth = "HealthRater is for people aged 18–100.";
  }
  const height = Number(heightCm);
  if (heightCm.trim() === "" || Number.isNaN(height) || height < 50 || height > 250) {
    errors.heightCm = "Enter your height between 50 and 250 cm.";
  }
  const weight = Number(weightKg);
  if (weightKg.trim() === "" || Number.isNaN(weight) || weight < 20 || weight > 400) {
    errors.weightKg = "Enter your weight between 20 and 400 kg.";
  }
  return errors;
}
