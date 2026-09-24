"""PROVISIONAL SCORING CONFIGURATION.

Mirrors backend/HealthRater.Core/Scoring/ScoringConfig.cs field-for-field so the
Python and .NET engines produce equivalent results on the same input. None of these
thresholds are clinically validated official formulas — no such formulas were
supplied. They are reasonable, commonly-cited wellness heuristics intentionally
centralized here so they can be tuned without touching calculation code.
"""
from __future__ import annotations

from healthrater.models import Sex, SubstanceFrequency


class Bmi:
    IDEAL_CENTER = 21.75
    PENALTY_PER_UNIT = 0.5


class BodyComposition:
    MALE_IDEAL_CENTER = 15.0
    FEMALE_IDEAL_CENTER = 23.0
    PENALTY_PER_PERCENT_POINT = 0.4


class Whtr:
    IDEAL_MAX = 0.50
    PENALTY_PER_UNIT_OVER = 40.0


class Whr:
    MALE_IDEAL_MAX = 0.90
    FEMALE_IDEAL_MAX = 0.80
    PENALTY_PER_UNIT_OVER = 20.0


class BloodPressure:
    IDEAL_SYSTOLIC = 110
    HIGH_SYSTOLIC = 165
    IDEAL_DIASTOLIC = 70
    HIGH_DIASTOLIC = 95
    LOW_SYSTOLIC_FLOOR = 90
    LOW_DIASTOLIC_FLOOR = 55


class RestingHeartRate:
    IDEAL_MAX = 62
    PENALTY_PER_BPM_OVER = 0.22
    LOW_FLOOR = 40


class HeartRateRecovery:
    BPM_FOR_MAX_SCORE = 30


class Hydration:
    ML_PER_KG_IDEAL = 33.0
    PENALTY_PER_RATIO_UNIT_OFF = 10.0


class Caffeine:
    PENALTY_PER_SERVING = 1.5


class JunkFood:
    PENALTY_PER_SERVING_PER_WEEK = 0.5


class Overeating:
    PENALTY_PER_EPISODE_PER_WEEK = 1.2


class VegetablesFiber:
    POINTS_PER_SERVING = 2.0


class Neat:
    STEPS_PER_SCORE_POINT = 1000


class PhysicalTraining:
    POINTS_PER_SESSION = 1.8
    OVERTRAINING_SESSIONS_THRESHOLD = 7
    OVERTRAINING_SCORE_CAP = 8


class Cooper:
    METERS_FOR_MAX_SCORE = 3000


class FunctionalPower:
    @staticmethod
    def get_excellent_norm_total_reps(sex: Sex, age: int) -> float:
        if sex == Sex.MALE:
            if age <= 29:
                return 150
            if age <= 39:
                return 130
            if age <= 49:
                return 110
            if age <= 59:
                return 90
            return 70
        else:
            if age <= 29:
                return 110
            if age <= 39:
                return 95
            if age <= 49:
                return 80
            if age <= 59:
                return 65
            return 50


SUBSTANCE_SCORE_BY_FREQUENCY: dict[SubstanceFrequency, int] = {
    SubstanceFrequency.DAILY: 1,
    SubstanceFrequency.SEVERAL_TIMES_PER_WEEK: 3,
    SubstanceFrequency.WEEKLY: 5,
    SubstanceFrequency.MONTHLY: 7,
    SubstanceFrequency.RARELY: 8,
    SubstanceFrequency.NEVER: 10,
}


def clamp_1_to_10(value: float) -> int:
    """Round-half-away-from-zero then clamp to [1, 10], matching the C# implementation."""
    import math

    rounded = math.floor(value + 0.5) if value >= 0 else math.ceil(value - 0.5)
    return max(1, min(10, rounded))
