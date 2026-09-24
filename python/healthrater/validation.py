from __future__ import annotations

from dataclasses import dataclass, field

from healthrater.models import AssessmentInput

ONE_TO_TEN_FIELDS = [
    "energy_level", "energy_stability", "average_sleep_quality", "circadian_health",
    "average_mood", "mood_stability", "social_life", "job_satisfaction",
    "home_family_satisfaction", "digestion_and_evacuation", "immune_health",
    "skin_health", "jaw_skull_health", "dental_health", "spinal_health", "hair_health",
]


@dataclass
class ValidationOutcome:
    errors: list[str] = field(default_factory=list)

    @property
    def is_valid(self) -> bool:
        return len(self.errors) == 0


def validate(input: AssessmentInput) -> ValidationOutcome:
    outcome = ValidationOutcome()

    if not (18 <= input.age <= 100):
        outcome.errors.append("Age must be between 18 and 100.")

    if not (50 <= input.height_cm <= 250):
        outcome.errors.append("Height must be between 50 and 250 cm.")

    if not (20 <= input.weight_kg <= 400):
        outcome.errors.append("Weight must be between 20 and 400 kg.")

    if input.waist_cm <= 0 or input.waist_cm > 250:
        outcome.errors.append("Waist must be a positive value up to 250 cm.")

    if input.hip_cm <= 0 or input.hip_cm > 250:
        outcome.errors.append("Hip must be a positive value up to 250 cm.")

    if not (0 <= input.body_fat_percent <= 100):
        outcome.errors.append("Body fat percentage must be between 0 and 100.")

    if not (60 <= input.systolic_bp_mmhg <= 260):
        outcome.errors.append("Systolic blood pressure must be between 60 and 260 mmHg.")

    if not (30 <= input.diastolic_bp_mmhg <= 160):
        outcome.errors.append("Diastolic blood pressure must be between 30 and 160 mmHg.")

    if input.systolic_bp_mmhg <= input.diastolic_bp_mmhg:
        outcome.errors.append("Systolic blood pressure must be greater than diastolic blood pressure.")

    if not (0 <= input.cooper_distance_meters <= 5000):
        outcome.errors.append("Cooper 12-minute run distance must be between 0 and 5000 meters.")

    for field_name in ONE_TO_TEN_FIELDS:
        value = getattr(input, field_name)
        if not (1 <= value <= 10):
            outcome.errors.append(f"{field_name} must be between 1 and 10.")

    return outcome
