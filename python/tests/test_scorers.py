from healthrater.models import Sex, SubstanceFrequency
from healthrater.scoring import scorers as s


def test_blood_pressure_ideal_scores_10():
    assert s.score_blood_pressure(110, 70) == 10


def test_blood_pressure_high_scores_1():
    assert s.score_blood_pressure(165, 95) == 1


def test_blood_pressure_midrange():
    score = s.score_blood_pressure(130, 85)
    assert 4 <= score <= 8


def test_blood_pressure_severe_hypertension_floors_at_1():
    assert s.score_blood_pressure(220, 130) == 1


def test_cooper_3000m_scores_10():
    assert s.score_cooper(3000) == 10
    assert s.score_cooper(4000) == 10  # capped


def test_cooper_1500m_scores_5():
    assert s.score_cooper(1500) == 5


def test_cooper_zero_floors_at_1():
    assert s.score_cooper(0) == 1


def test_functional_power_at_norm_scores_10():
    # norm for male <=29 is 150 total reps
    assert s.score_functional_power(60, 20, 70, Sex.MALE, 25) == 10


def test_functional_power_zero_floors_at_1():
    assert s.score_functional_power(0, 0, 0, Sex.MALE, 25) == 1


def test_functional_power_age_adjusted_norm_is_easier_for_older():
    young = s.score_functional_power(50, 5, 50, Sex.MALE, 25)
    old = s.score_functional_power(50, 5, 50, Sex.MALE, 65)
    assert old >= young


def test_resting_heart_rate_low_scores_10():
    assert s.score_resting_heart_rate(58) == 10


def test_resting_heart_rate_high_scores_low():
    score = s.score_resting_heart_rate(110)
    assert 1 <= score <= 4


def test_heart_rate_recovery_30bpm_scores_10():
    assert s.score_heart_rate_recovery(30) == 10


def test_substance_never_and_daily():
    assert s.score_substance(SubstanceFrequency.NEVER) == 10
    assert s.score_substance(SubstanceFrequency.DAILY) == 1


def test_physical_training_5_sessions_near_max():
    score = s.score_physical_training(5)
    assert 8 <= score <= 10


def test_physical_training_overtraining_capped():
    assert s.score_physical_training(14) == 8
