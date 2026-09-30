from healthrater import sample_profile
from healthrater.models import BodyFatSource, SubstanceFrequency
from healthrater.scoring import body_fat
from healthrater.scoring.engine import calculate
from healthrater.scoring.four_states import IMMUNITY_PARAMS, LONGEVITY_PARAMS
from healthrater.scoring.keys import Keys
from healthrater.validation import validate


def test_unknown_body_fat_is_estimated_with_deurenberg():
    p = sample_profile.healthy()
    p.body_fat_percent = None
    result = calculate(p)
    assert result.body_fat_source == BodyFatSource.ESTIMATED
    assert result.body_fat_percent == 19.13  # 1.2×24.07 + 0.23×28 − 10.8 − 5.4
    assert p.body_fat_estimation_method == "Deurenberg1991"


def test_known_body_fat_is_measured():
    result = calculate(sample_profile.healthy())
    assert result.body_fat_source == BodyFatSource.MEASURED
    assert result.body_fat_percent == 16


def test_implausible_estimate_is_rejected():
    p = sample_profile.healthy()
    p.body_fat_percent = None
    p.age, p.height_cm, p.weight_kg = 18, 250, 20  # BMI 3.2 → negative estimate
    assert any("couldn't be estimated" in e for e in validate(p).errors)


def test_estimator_is_replaceable():
    original = body_fat.current_estimator
    try:
        body_fat.current_estimator = lambda _input: (22.5, "Fixed")
        p = sample_profile.healthy()
        p.body_fat_percent = None
        assert calculate(p).body_fat_percent == 22.5
    finally:
        body_fat.current_estimator = original


def test_hrr_is_peak_minus_after_60s_and_validated():
    p = sample_profile.healthy()
    assert p.heart_rate_recovery_bpm == 28
    p.heart_rate_after_60s_bpm = p.peak_heart_rate_bpm + 5
    assert not validate(p).is_valid


def test_substances_are_independent_and_in_immunity_and_longevity():
    base = calculate(sample_profile.healthy())
    p = sample_profile.healthy()
    p.tobacco_frequency = SubstanceFrequency.DAILY
    result = calculate(p)
    assert result.parameter_scores[Keys.TOBACCO] == 1
    assert result.parameter_scores[Keys.ALCOHOL] == base.parameter_scores[Keys.ALCOHOL]
    assert result.total_health_rating == base.total_health_rating - 7
    for key in (Keys.ALCOHOL, Keys.TOBACCO, Keys.DRUGS):
        assert key in IMMUNITY_PARAMS and key in LONGEVITY_PARAMS
    assert len(IMMUNITY_PARAMS) == 13 and len(LONGEVITY_PARAMS) == 15
