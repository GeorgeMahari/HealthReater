from healthrater import sample_profile
from healthrater.scoring.derived import calculate_derived_metrics


def test_bmi_matches_known_value():
    derived = calculate_derived_metrics(sample_profile.healthy())
    assert derived.bmi == 24.07


def test_bmi_reference_value():
    p = sample_profile.healthy()
    p.weight_kg = 70
    p.height_cm = 175
    derived = calculate_derived_metrics(p)
    assert 22.85 <= derived.bmi <= 22.87


def test_whtr_ratio():
    p = sample_profile.healthy()  # waist=82, height=180
    derived = calculate_derived_metrics(p)
    assert derived.whtr == round(82 / 180, 3)


def test_whr_ratio():
    p = sample_profile.healthy()  # waist=82, hip=98
    derived = calculate_derived_metrics(p)
    assert derived.whr == round(82 / 98, 3)


def test_unit_conversion_bmi_2m_100kg():
    p = sample_profile.healthy()
    p.height_cm = 200
    p.weight_kg = 100
    derived = calculate_derived_metrics(p)
    assert derived.bmi == 25.0
