from __future__ import annotations

from healthrater.models import FourStates, StateScore
from healthrater.scoring.keys import Keys

ENERGY_STRENGTH_STAMINA_PARAMS = [
    Keys.ENERGY_LEVEL, Keys.ENERGY_STABILITY, Keys.SLEEP_QUALITY, Keys.CIRCADIAN_HEALTH,
    Keys.RESTING_HEART_RATE, Keys.HEART_RATE_RECOVERY, Keys.NEAT, Keys.PHYSICAL_TRAINING,
    Keys.FUNCTIONAL_POWER, Keys.COOPER,
]

MENTAL_EMOTIONAL_PARAMS = [
    Keys.MOOD, Keys.MOOD_STABILITY, Keys.SOCIAL_LIFE, Keys.JOB_SATISFACTION,
    Keys.HOME_FAMILY_SATISFACTION, Keys.SLEEP_QUALITY, Keys.CIRCADIAN_HEALTH,
]

IMMUNITY_PARAMS = [
    Keys.HYDRATION, Keys.DIGESTION, Keys.IMMUNE_HEALTH, Keys.CAFFEINE, Keys.JUNK_FOOD,
    Keys.OVEREATING, Keys.ALCOHOL, Keys.TOBACCO, Keys.DRUGS, Keys.VEGETABLES_FIBER, Keys.SKIN_HEALTH,
    Keys.DENTAL_HEALTH, Keys.HAIR_HEALTH,
]

LONGEVITY_PARAMS = [
    Keys.AGE, Keys.BODY_FAT, Keys.BMI, Keys.WAIST, Keys.WHTR, Keys.WHR, Keys.BLOOD_PRESSURE,
    Keys.RESTING_HEART_RATE, Keys.HEART_RATE_RECOVERY, Keys.PHYSICAL_TRAINING, Keys.NEAT,
    Keys.VEGETABLES_FIBER, Keys.ALCOHOL, Keys.TOBACCO, Keys.DRUGS,
]


def _build(scores: dict[str, int], keys: list[str]) -> StateScore:
    raw = sum(scores.get(k, 0) for k in keys)
    max_raw = len(keys) * 10
    normalized = round((raw / max_raw) * 100, 2) if max_raw else 0.0
    return StateScore(raw_score=raw, max_raw_score=max_raw, normalized_score=normalized)


def calculate_four_states(scores: dict[str, int]) -> FourStates:
    return FourStates(
        energy_strength_stamina=_build(scores, ENERGY_STRENGTH_STAMINA_PARAMS),
        mental_emotional=_build(scores, MENTAL_EMOTIONAL_PARAMS),
        immunity=_build(scores, IMMUNITY_PARAMS),
        longevity=_build(scores, LONGEVITY_PARAMS),
    )
