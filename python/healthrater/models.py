"""Data models for the HealthRater assessment — mirrors HealthRater.Core.Models."""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from typing import Optional


class Sex(str, Enum):
    MALE = "Male"
    FEMALE = "Female"


class BodyFatSource(str, Enum):
    MEASURED = "Measured"
    ESTIMATED = "Estimated"


class SubstanceFrequency(str, Enum):
    DAILY = "Daily"
    SEVERAL_TIMES_PER_WEEK = "SeveralTimesPerWeek"
    WEEKLY = "Weekly"
    MONTHLY = "Monthly"
    RARELY = "Rarely"
    NEVER = "Never"


@dataclass
class AssessmentInput:
    """Raw assessment input. Field grouping mirrors the 8 assessment UI sections.

    BMI, WHtR and WHR are NOT collected here — they are derived from
    height/weight/waist/hip (see scoring.derived.calculate_derived_metrics).
    """

    # 1. Basic Information
    sex: Sex
    age: int
    height_cm: float
    weight_kg: float

    # 2. Body Metrics
    waist_cm: float
    hip_cm: float
    # None = "I don't know": estimated with the Deurenberg equation (see scoring.body_fat)
    body_fat_percent: Optional[float]

    # 3. Cardiovascular
    resting_heart_rate_bpm: int
    # Standardized HRR protocol: peak HR and HR exactly 60 s after exercise stops
    peak_heart_rate_bpm: int
    heart_rate_after_60s_bpm: int
    systolic_bp_mmhg: int
    diastolic_bp_mmhg: int

    # 4. Energy & Sleep
    energy_level: int
    energy_stability: int
    average_sleep_quality: int
    circadian_health: int

    # 5. Mental & Emotional
    average_mood: int
    mood_stability: int
    social_life: int
    job_satisfaction: int
    home_family_satisfaction: int

    # 6. Lifestyle
    daily_water_intake_liters: float
    digestion_and_evacuation: int
    immune_health: int
    caffeine_servings_per_day: float
    junk_food_servings_per_week: float
    overeating_episodes_per_week: float
    # Three independent parameters (before v2: one combined alcohol/tobacco/drugs answer)
    alcohol_frequency: SubstanceFrequency
    tobacco_frequency: SubstanceFrequency
    drugs_frequency: SubstanceFrequency
    vegetables_fiber_servings_per_day: float

    # 7. Physical Performance
    daily_steps_neat: int
    training_sessions_per_week: float
    push_ups: int
    pull_ups: int
    bodyweight_squats: int
    cooper_distance_meters: int

    # 8. General Health
    skin_health: int
    jaw_skull_health: int
    dental_health: int
    spinal_health: int
    hair_health: int

    # Filled in when body fat is resolved (measured or estimated)
    body_fat_source: BodyFatSource = BodyFatSource.MEASURED
    body_fat_estimation_method: Optional[str] = None

    @property
    def heart_rate_recovery_bpm(self) -> int:
        """HRR = peak − heart rate 60 s after stopping (always calculated, never entered)."""
        return self.peak_heart_rate_bpm - self.heart_rate_after_60s_bpm


@dataclass
class DerivedMetrics:
    bmi: float
    whtr: float
    whr: float


@dataclass
class StateScore:
    raw_score: int
    max_raw_score: int
    normalized_score: float


@dataclass
class FourStates:
    energy_strength_stamina: StateScore
    mental_emotional: StateScore
    immunity: StateScore
    longevity: StateScore


@dataclass
class HealthRatingResult:
    total_health_rating: int
    max_health_rating: int
    percentage: float
    parameter_scores: dict[str, int]
    four_states: FourStates
    derived_metrics: DerivedMetrics
    parameter_count: int = 0
    parameter_set_version: str = ""
    body_fat_percent: float = 0.0
    body_fat_source: BodyFatSource = BodyFatSource.MEASURED
    heart_rate_recovery_bpm: int = 0
