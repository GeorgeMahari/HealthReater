from __future__ import annotations

from healthrater.models import AssessmentInput, DerivedMetrics


def calculate_derived_metrics(input: AssessmentInput) -> DerivedMetrics:
    height_m = input.height_cm / 100.0
    bmi = input.weight_kg / (height_m * height_m)
    whtr = input.waist_cm / input.height_cm
    whr = input.waist_cm / input.hip_cm

    return DerivedMetrics(
        bmi=round(bmi, 2),
        whtr=round(whtr, 3),
        whr=round(whr, 3),
    )
