from healthrater import sample_profile
from healthrater.scoring.engine import calculate
from healthrater.scoring.four_states import ENERGY_STRENGTH_STAMINA_PARAMS, MENTAL_EMOTIONAL_PARAMS
from healthrater.scoring.keys import MAX_TOTAL_SCORE, PARAMETER_KEYS, TOTAL_PARAMETER_COUNT, Keys


def test_parameter_set_is_41_with_max_410():
    assert TOTAL_PARAMETER_COUNT == 41
    assert MAX_TOTAL_SCORE == 410
    assert len(set(PARAMETER_KEYS)) == 41


def test_exactly_41_parameters_scored():
    result = calculate(sample_profile.healthy())
    assert len(result.parameter_scores) == TOTAL_PARAMETER_COUNT
    assert list(result.parameter_scores) == PARAMETER_KEYS


def test_total_within_bounds():
    result = calculate(sample_profile.healthy())
    assert TOTAL_PARAMETER_COUNT <= result.total_health_rating <= MAX_TOTAL_SCORE


def test_total_is_literal_sum_not_weighted_average():
    result = calculate(sample_profile.healthy())
    assert sum(result.parameter_scores.values()) == result.total_health_rating


def test_worst_case_near_floor():
    result = calculate(sample_profile.worst_case())
    assert TOTAL_PARAMETER_COUNT <= result.total_health_rating <= 130


def test_max_is_410():
    result = calculate(sample_profile.healthy())
    assert result.max_health_rating == 410


def test_percentage_matches_total_over_410():
    result = calculate(sample_profile.healthy())
    expected = round(result.total_health_rating / 410.0 * 100, 2)
    assert result.percentage == expected


def test_four_states_healthy_beats_worst_case():
    healthy = calculate(sample_profile.healthy())
    worst = calculate(sample_profile.worst_case())
    assert healthy.four_states.energy_strength_stamina.normalized_score > worst.four_states.energy_strength_stamina.normalized_score
    assert healthy.four_states.mental_emotional.normalized_score > worst.four_states.mental_emotional.normalized_score
    assert healthy.four_states.immunity.normalized_score > worst.four_states.immunity.normalized_score
    assert healthy.four_states.longevity.normalized_score > worst.four_states.longevity.normalized_score


def test_four_states_normalized_within_0_100():
    result = calculate(sample_profile.healthy())
    for state in [
        result.four_states.energy_strength_stamina,
        result.four_states.mental_emotional,
        result.four_states.immunity,
        result.four_states.longevity,
    ]:
        assert 0 <= state.normalized_score <= 100


def test_sleep_quality_contributes_to_multiple_states():
    assert Keys.SLEEP_QUALITY in ENERGY_STRENGTH_STAMINA_PARAMS
    assert Keys.SLEEP_QUALITY in MENTAL_EMOTIONAL_PARAMS


def test_matches_dotnet_reference_output_for_healthy_sample():
    """Cross-engine parity check: same sample input must produce the same total,
    percentage and four-state scores as the .NET HealthRater.Core engine
    (verified manually via `dotnet run` against HealthRater.Api — see README)."""
    result = calculate(sample_profile.healthy())
    assert result.total_health_rating == 355
    assert result.percentage == 86.59
    assert result.four_states.energy_strength_stamina.normalized_score == 83.0
    assert result.four_states.mental_emotional.normalized_score == 77.14
    assert result.four_states.immunity.normalized_score == 83.85
    assert result.four_states.longevity.normalized_score == 91.33
    assert result.derived_metrics.bmi == 24.07
    assert result.derived_metrics.whtr == 0.456
    assert result.derived_metrics.whr == 0.837
