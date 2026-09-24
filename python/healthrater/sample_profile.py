from __future__ import annotations

import copy

from healthrater.models import AssessmentInput, Sex, SubstanceFrequency


def healthy() -> AssessmentInput:
    """A healthy, mid-20s male profile — identical to the C# SampleProfile.Healthy()."""
    return AssessmentInput(
        sex=Sex.MALE,
        age=28,
        height_cm=180,
        weight_kg=78,
        waist_cm=82,
        hip_cm=98,
        body_fat_percent=16,
        resting_heart_rate_bpm=58,
        heart_rate_recovery_bpm=28,
        systolic_bp_mmhg=115,
        diastolic_bp_mmhg=74,
        energy_level=8,
        energy_stability=7,
        average_sleep_quality=8,
        circadian_health=7,
        average_mood=8,
        mood_stability=7,
        social_life=8,
        job_satisfaction=7,
        home_family_satisfaction=9,
        daily_water_intake_liters=2.6,
        digestion_and_evacuation=8,
        immune_health=8,
        caffeine_servings_per_day=1,
        junk_food_servings_per_week=2,
        overeating_episodes_per_week=1,
        alcohol_tobacco_drugs_frequency=SubstanceFrequency.RARELY,
        vegetables_fiber_servings_per_day=4,
        daily_steps_neat=9000,
        training_sessions_per_week=5,
        push_ups=40,
        pull_ups=12,
        bodyweight_squats=50,
        cooper_distance_meters=2800,
        skin_health=8,
        jaw_skull_health=9,
        dental_health=8,
        spinal_health=7,
        hair_health=8,
    )


def worst_case() -> AssessmentInput:
    p = copy.deepcopy(healthy())
    p.body_fat_percent = 40
    p.resting_heart_rate_bpm = 110
    p.heart_rate_recovery_bpm = 3
    p.systolic_bp_mmhg = 170
    p.diastolic_bp_mmhg = 100
    p.energy_level = p.energy_stability = p.average_sleep_quality = p.circadian_health = 1
    p.average_mood = p.mood_stability = p.social_life = p.job_satisfaction = p.home_family_satisfaction = 1
    p.daily_water_intake_liters = 0.2
    p.digestion_and_evacuation = p.immune_health = 1
    p.caffeine_servings_per_day = 10
    p.junk_food_servings_per_week = 25
    p.overeating_episodes_per_week = 10
    p.alcohol_tobacco_drugs_frequency = SubstanceFrequency.DAILY
    p.vegetables_fiber_servings_per_day = 0
    p.daily_steps_neat = 500
    p.training_sessions_per_week = 0
    p.push_ups = p.pull_ups = p.bodyweight_squats = 0
    p.cooper_distance_meters = 500
    p.skin_health = p.jaw_skull_health = p.dental_health = p.spinal_health = p.hair_health = 1
    return p
