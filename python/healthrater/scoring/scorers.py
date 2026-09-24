from __future__ import annotations

from healthrater.models import Sex, SubstanceFrequency
from healthrater.scoring import config as cfg


# ---------------- Body composition ----------------

def score_bmi(bmi: float) -> int:
    penalty = abs(bmi - cfg.Bmi.IDEAL_CENTER) * cfg.Bmi.PENALTY_PER_UNIT
    return cfg.clamp_1_to_10(10 - penalty)


def score_body_fat(body_fat_percent: float, sex: Sex) -> int:
    center = (
        cfg.BodyComposition.MALE_IDEAL_CENTER
        if sex == Sex.MALE
        else cfg.BodyComposition.FEMALE_IDEAL_CENTER
    )
    penalty = abs(body_fat_percent - center) * cfg.BodyComposition.PENALTY_PER_PERCENT_POINT
    return cfg.clamp_1_to_10(10 - penalty)


def score_whtr(whtr: float) -> int:
    over = max(0.0, whtr - cfg.Whtr.IDEAL_MAX)
    penalty = over * cfg.Whtr.PENALTY_PER_UNIT_OVER
    return cfg.clamp_1_to_10(10 - penalty)


def score_whr(whr: float, sex: Sex) -> int:
    ideal_max = cfg.Whr.MALE_IDEAL_MAX if sex == Sex.MALE else cfg.Whr.FEMALE_IDEAL_MAX
    over = max(0.0, whr - ideal_max)
    penalty = over * cfg.Whr.PENALTY_PER_UNIT_OVER
    return cfg.clamp_1_to_10(10 - penalty)


# ---------------- Blood pressure ----------------

def _score_limb(value: float, ideal: float, high: float, low_floor: float) -> float:
    if value <= ideal:
        if value < low_floor:
            under = low_floor - value
            return max(1.0, min(10.0, 10 - under * 0.3))
        return 10.0
    ratio = (value - ideal) / (high - ideal)
    return max(1.0, min(10.0, 10 - ratio * 9))


def score_blood_pressure(systolic: int, diastolic: int) -> int:
    sys_score = _score_limb(
        systolic, cfg.BloodPressure.IDEAL_SYSTOLIC, cfg.BloodPressure.HIGH_SYSTOLIC,
        cfg.BloodPressure.LOW_SYSTOLIC_FLOOR,
    )
    dia_score = _score_limb(
        diastolic, cfg.BloodPressure.IDEAL_DIASTOLIC, cfg.BloodPressure.HIGH_DIASTOLIC,
        cfg.BloodPressure.LOW_DIASTOLIC_FLOOR,
    )
    return cfg.clamp_1_to_10((sys_score + dia_score) / 2.0)


# ---------------- Heart rate ----------------

def score_resting_heart_rate(bpm: int) -> int:
    if bpm < cfg.RestingHeartRate.LOW_FLOOR:
        under = cfg.RestingHeartRate.LOW_FLOOR - bpm
        return cfg.clamp_1_to_10(10 - under * 0.3)
    if bpm <= cfg.RestingHeartRate.IDEAL_MAX:
        return 10
    over = bpm - cfg.RestingHeartRate.IDEAL_MAX
    return cfg.clamp_1_to_10(10 - over * cfg.RestingHeartRate.PENALTY_PER_BPM_OVER)


def score_heart_rate_recovery(bpm_drop: int) -> int:
    score = (bpm_drop / cfg.HeartRateRecovery.BPM_FOR_MAX_SCORE) * 10
    return cfg.clamp_1_to_10(score)


# ---------------- Hydration ----------------

def score_hydration(daily_water_intake_liters: float, body_weight_kg: float) -> int:
    ideal_liters = (cfg.Hydration.ML_PER_KG_IDEAL * body_weight_kg) / 1000.0
    if ideal_liters <= 0:
        return 1
    ratio = daily_water_intake_liters / ideal_liters
    penalty = abs(ratio - 1) * cfg.Hydration.PENALTY_PER_RATIO_UNIT_OFF
    return cfg.clamp_1_to_10(10 - penalty)


# ---------------- Lifestyle ----------------

def score_caffeine(servings_per_day: float) -> int:
    return cfg.clamp_1_to_10(10 - servings_per_day * cfg.Caffeine.PENALTY_PER_SERVING)


def score_junk_food(servings_per_week: float) -> int:
    return cfg.clamp_1_to_10(10 - servings_per_week * cfg.JunkFood.PENALTY_PER_SERVING_PER_WEEK)


def score_overeating(episodes_per_week: float) -> int:
    return cfg.clamp_1_to_10(10 - episodes_per_week * cfg.Overeating.PENALTY_PER_EPISODE_PER_WEEK)


def score_vegetables_fiber(servings_per_day: float) -> int:
    return cfg.clamp_1_to_10(servings_per_day * cfg.VegetablesFiber.POINTS_PER_SERVING)


def score_neat(daily_steps: int) -> int:
    return cfg.clamp_1_to_10(daily_steps / cfg.Neat.STEPS_PER_SCORE_POINT)


def score_physical_training(sessions_per_week: float) -> int:
    if sessions_per_week > cfg.PhysicalTraining.OVERTRAINING_SESSIONS_THRESHOLD:
        return cfg.PhysicalTraining.OVERTRAINING_SCORE_CAP
    return cfg.clamp_1_to_10(sessions_per_week * cfg.PhysicalTraining.POINTS_PER_SESSION)


def score_substance(frequency: SubstanceFrequency) -> int:
    return cfg.SUBSTANCE_SCORE_BY_FREQUENCY[frequency]


def score_cooper(distance_meters: int) -> int:
    return cfg.clamp_1_to_10((distance_meters / cfg.Cooper.METERS_FOR_MAX_SCORE) * 10)


def score_functional_power(push_ups: int, pull_ups: int, bodyweight_squats: int, sex: Sex, age: int) -> int:
    total = push_ups + pull_ups + bodyweight_squats
    norm = cfg.FunctionalPower.get_excellent_norm_total_reps(sex, age)
    if norm <= 0:
        return 1
    ratio = total / norm
    return cfg.clamp_1_to_10(ratio * 10)
