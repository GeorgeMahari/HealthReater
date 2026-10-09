from __future__ import annotations

from healthrater.models import AssessmentInput, HealthRatingResult, Sex
from healthrater.scoring import config as cfg
from healthrater.scoring import scorers as s
from healthrater.scoring.derived import calculate_derived_metrics
from healthrater.scoring.four_states import calculate_four_states
from healthrater.scoring.body_fat import resolve_body_fat
from healthrater.scoring.keys import MAX_TOTAL_SCORE, PARAMETER_KEYS, PARAMETER_SET_VERSION, TOTAL_PARAMETER_COUNT, Keys


def _score_sex(sex: Sex) -> int:
    return 10  # sex itself carries no inherent health penalty


def _score_age(age: int) -> int:
    if age <= 30:
        return 10
    decades_over = (age - 30) / 10.0
    return cfg.clamp_1_to_10(10 - decades_over)


def calculate(input: AssessmentInput) -> HealthRatingResult:
    error = resolve_body_fat(input)
    if error or input.body_fat_percent is None:
        raise ValueError(error or "Body fat is missing; validate the input first.")
    derived = calculate_derived_metrics(input)

    scores: dict[str, int] = {
        Keys.SEX: _score_sex(input.sex),
        Keys.AGE: _score_age(input.age),
        Keys.HEIGHT: 10,  # raw anthropometric datum, not itself a health signal
        Keys.WEIGHT: 10,  # weight's health signal is captured via BMI/WHtR/body fat
        Keys.WAIST: 10,   # captured via WHtR/WHR
        Keys.HIP: 10,     # captured via WHR
        Keys.BODY_FAT: s.score_body_fat(input.body_fat_percent, input.sex),
        Keys.BMI: s.score_bmi(derived.bmi),
        Keys.WHTR: s.score_whtr(derived.whtr),
        Keys.WHR: s.score_whr(derived.whr, input.sex),
        Keys.RESTING_HEART_RATE: s.score_resting_heart_rate(input.resting_heart_rate_bpm),
        Keys.HEART_RATE_RECOVERY: s.score_heart_rate_recovery(input.heart_rate_recovery_bpm),
        Keys.BLOOD_PRESSURE: s.score_blood_pressure(input.systolic_bp_mmhg, input.diastolic_bp_mmhg),
        Keys.ENERGY_LEVEL: input.energy_level,
        Keys.ENERGY_STABILITY: input.energy_stability,
        Keys.SLEEP_QUALITY: input.average_sleep_quality,
        Keys.CIRCADIAN_HEALTH: input.circadian_health,
        Keys.MOOD: input.average_mood,
        Keys.MOOD_STABILITY: input.mood_stability,
        Keys.SOCIAL_LIFE: input.social_life,
        Keys.JOB_SATISFACTION: input.job_satisfaction,
        Keys.HOME_FAMILY_SATISFACTION: input.home_family_satisfaction,
        Keys.HYDRATION: s.score_hydration(input.daily_water_intake_liters, input.weight_kg),
        Keys.DIGESTION: input.digestion_and_evacuation,
        Keys.IMMUNE_HEALTH: input.immune_health,
        Keys.CAFFEINE: s.score_caffeine(input.caffeine_servings_per_day),
        Keys.JUNK_FOOD: s.score_junk_food(input.junk_food_servings_per_week),
        Keys.OVEREATING: s.score_overeating(input.overeating_episodes_per_week),
        Keys.ALCOHOL: s.score_alcohol(input.alcohol_frequency),
        Keys.TOBACCO: s.score_tobacco(input.tobacco_frequency),
        Keys.DRUGS: s.score_drugs(input.drugs_frequency),
        Keys.VEGETABLES_FIBER: s.score_vegetables_fiber(input.vegetables_fiber_servings_per_day),
        Keys.NEAT: s.score_neat(input.daily_steps_neat),
        Keys.PHYSICAL_TRAINING: s.score_physical_training(input.training_sessions_per_week),
        Keys.FUNCTIONAL_POWER: s.score_functional_power(
            input.push_ups, input.pull_ups, input.bodyweight_squats, input.sex, input.age
        ),
        Keys.COOPER: s.score_cooper(input.cooper_distance_meters),
        Keys.SKIN_HEALTH: input.skin_health,
        Keys.JAW_SKULL_HEALTH: input.jaw_skull_health,
        Keys.DENTAL_HEALTH: input.dental_health,
        Keys.SPINAL_HEALTH: input.spinal_health,
        Keys.HAIR_HEALTH: input.hair_health,
    }

    if list(scores) != PARAMETER_KEYS:
        raise AssertionError("The engine's scores don't match PARAMETER_KEYS.")

    total = sum(scores.values())
    four_states = calculate_four_states(scores)

    return HealthRatingResult(
        total_health_rating=total,
        max_health_rating=MAX_TOTAL_SCORE,
        percentage=round((total / float(MAX_TOTAL_SCORE)) * 100, 2),
        parameter_scores=scores,
        four_states=four_states,
        derived_metrics=derived,
        parameter_count=TOTAL_PARAMETER_COUNT,
        parameter_set_version=PARAMETER_SET_VERSION,
        body_fat_percent=input.body_fat_percent,
        body_fat_source=input.body_fat_source,
        heart_rate_recovery_bpm=input.heart_rate_recovery_bpm,
    )
