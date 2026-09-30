"""Body-fat estimation for "I don't know my body fat" — mirrors HealthRater.Core.Scoring.BodyFat.

Deurenberg P, Weststrate JA, Seidell JC (1991), Br J Nutr 65(2):105-114:
    body fat % = 1.20 × BMI + 0.23 × age − 10.8 × sex − 5.4   (sex: 1 = male, 0 = female)
A population-based estimate; it can't tell muscle from fat.
"""
from __future__ import annotations

import math
from typing import Callable, Optional

from healthrater.models import AssessmentInput, BodyFatSource, Sex

MIN_PLAUSIBLE_PERCENT = 2.0
MAX_PLAUSIBLE_PERCENT = 75.0
DEURENBERG_METHOD = "Deurenberg1991"


def deurenberg(bmi: float, age: int, sex: Sex) -> float:
    return 1.20 * bmi + 0.23 * age - 10.8 * (1 if sex == Sex.MALE else 0) - 5.4


# Replaceable estimator: (input) -> (value, method id)
Estimator = Callable[[AssessmentInput], "tuple[float, str]"]


def _deurenberg_estimator(input: AssessmentInput) -> "tuple[float, str]":
    height_m = input.height_cm / 100.0
    bmi = input.weight_kg / (height_m * height_m) if height_m > 0 else math.nan
    return deurenberg(bmi, input.age, input.sex), DEURENBERG_METHOD


current_estimator: Estimator = _deurenberg_estimator


def resolve_body_fat(input: AssessmentInput) -> Optional[str]:
    """Estimates body fat when it's None. Returns an error message when no plausible estimate exists."""
    if input.body_fat_percent is not None:
        return None
    value, method = current_estimator(input)
    if math.isnan(value) or math.isinf(value) or not (MIN_PLAUSIBLE_PERCENT <= value <= MAX_PLAUSIBLE_PERCENT):
        return ("Body fat couldn't be estimated reliably from your height, weight and age. "
                "Please enter a measured body-fat percentage instead.")
    input.body_fat_percent = round(value, 2)
    input.body_fat_source = BodyFatSource.ESTIMATED
    input.body_fat_estimation_method = method
    return None
