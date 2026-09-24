from healthrater import sample_profile
from healthrater.validation import validate


def test_healthy_profile_is_valid():
    assert validate(sample_profile.healthy()).is_valid


def test_age_below_18_rejected():
    p = sample_profile.healthy()
    p.age = 17
    assert not validate(p).is_valid


def test_age_above_100_rejected():
    p = sample_profile.healthy()
    p.age = 101
    assert not validate(p).is_valid


def test_out_of_range_1_to_10_field_rejected():
    p = sample_profile.healthy()
    p.energy_level = 11
    assert not validate(p).is_valid


def test_body_fat_above_100_rejected():
    p = sample_profile.healthy()
    p.body_fat_percent = 150
    assert not validate(p).is_valid


def test_negative_waist_rejected():
    p = sample_profile.healthy()
    p.waist_cm = -5
    assert not validate(p).is_valid


def test_systolic_not_greater_than_diastolic_rejected():
    p = sample_profile.healthy()
    p.systolic_bp_mmhg = 90
    p.diastolic_bp_mmhg = 95
    assert not validate(p).is_valid


def test_negative_cooper_distance_rejected():
    p = sample_profile.healthy()
    p.cooper_distance_meters = -100
    assert not validate(p).is_valid


def test_invalid_sex_type_error():
    import pytest
    from healthrater.models import Sex
    # Sex is an Enum — an invalid value raises ValueError at construction time.
    with pytest.raises(ValueError):
        Sex("Unknown")
