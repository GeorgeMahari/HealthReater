"""Command-line entry point for the HealthRater Python application.

Usage:
    python -m healthrater.cli                 # run the built-in healthy sample profile
    python -m healthrater.cli --worst-case     # run the built-in worst-case profile
    python -m healthrater.cli --json path.json # run a JSON assessment file
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import sys

from healthrater import sample_profile
from healthrater.models import AssessmentInput, Sex, SubstanceFrequency
from healthrater.scoring.engine import calculate
from healthrater.validation import validate


def _input_from_json(path: str) -> AssessmentInput:
    with open(path, "r", encoding="utf-8") as f:
        raw = json.load(f)

    raw["sex"] = Sex(raw["sex"])
    raw["alcohol_tobacco_drugs_frequency"] = SubstanceFrequency(raw["alcohol_tobacco_drugs_frequency"])
    return AssessmentInput(**raw)


def _print_result(input: AssessmentInput) -> int:
    outcome = validate(input)
    if not outcome.is_valid:
        print("Validation failed:")
        for err in outcome.errors:
            print(f"  - {err}")
        return 1

    result = calculate(input)

    print("HealthRater — Python Assessment Result")
    print("=" * 45)
    print(f"TOTAL HEALTH RATING: {result.total_health_rating} / {result.max_health_rating}")
    print(f"Percentage: {result.percentage}%")
    print()
    print("Four States (normalized 0-100):")
    print(f"  Energy, Strength & Stamina: {result.four_states.energy_strength_stamina.normalized_score}")
    print(f"  Mental & Emotional:         {result.four_states.mental_emotional.normalized_score}")
    print(f"  Immunity:                   {result.four_states.immunity.normalized_score}")
    print(f"  Longevity:                  {result.four_states.longevity.normalized_score}")
    print()
    print("Derived Metrics:")
    print(f"  BMI:  {result.derived_metrics.bmi}")
    print(f"  WHtR: {result.derived_metrics.whtr}")
    print(f"  WHR:  {result.derived_metrics.whr}")
    print()
    print("Full JSON:")
    print(json.dumps(dataclasses.asdict(result), default=str, indent=2))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="HealthRater Python assessment CLI")
    parser.add_argument("--worst-case", action="store_true", help="Run the built-in worst-case sample profile")
    parser.add_argument("--json", type=str, help="Path to a JSON assessment input file")
    args = parser.parse_args()

    if args.json:
        input = _input_from_json(args.json)
    elif args.worst_case:
        input = sample_profile.worst_case()
    else:
        input = sample_profile.healthy()

    return _print_result(input)


if __name__ == "__main__":
    sys.exit(main())
