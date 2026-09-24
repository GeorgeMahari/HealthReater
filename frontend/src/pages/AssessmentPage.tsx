import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight, LoaderCircle } from "lucide-react";
import { sections, type FieldDef } from "../data/sections";
import { FieldInput } from "../components/FieldInput";
import { ProgressBar } from "../components/ProgressBar";
import { useAssessment } from "../context/AssessmentContext";
import { calculateHealthRating, HealthRatingApiError, saveAssessment } from "../api/healthRatingApi";
import { useAuth } from "../context/AuthContext";
import { sectionIcons } from "../data/uiMeta";
import type { AssessmentInput } from "../types";

const TOTAL_STEPS = sections.length + 1; // + final summary step
const stepTitles = [...sections.map((s) => s.title), "Review & Calculate"];
const stepIds = [...sections.map((s) => s.id), "review"];

function validateField(field: FieldDef, value: unknown): string | undefined {
  if (value === "" || value === undefined || value === null) {
    return `${field.label} is required.`;
  }
  if (field.kind === "number" && typeof value === "number") {
    if (field.min !== undefined && value < field.min) {
      return `${field.label} must be at least ${field.min}${field.unit ? " " + field.unit : ""}.`;
    }
    if (field.max !== undefined && value > field.max) {
      return `${field.label} must be at most ${field.max}${field.unit ? " " + field.unit : ""}.`;
    }
  }
  return undefined;
}

export function AssessmentPage() {
  const { assessment, updateField, setResult } = useAssessment();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string[] | null>(null);

  const isSummaryStep = step === sections.length;
  const currentSection = isSummaryStep ? null : sections[step];

  // Bring the new step into view (presentation only; skipped on first render).
  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [step]);

  function validateCurrentSection(): boolean {
    if (!currentSection) return true;
    const newErrors: Record<string, string> = {};
    for (const field of currentSection.fields) {
      const err = validateField(field, assessment[field.key]);
      if (err) newErrors[field.key] = err;
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  function goNext() {
    if (!validateCurrentSection()) return;
    setStep((s) => Math.min(s + 1, TOTAL_STEPS - 1));
  }

  function goBack() {
    setErrors({});
    setStep((s) => Math.max(s - 1, 0));
  }

  async function handleCalculate() {
    // Validate every section before submitting.
    for (const section of sections) {
      for (const field of section.fields) {
        const err = validateField(field, assessment[field.key]);
        if (err) {
          setSubmitError([`${section.title}: ${err}`]);
          return;
        }
      }
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      // Signed-in users get the result saved to their history; guests just get it calculated.
      const result = user
        ? await saveAssessment(assessment as AssessmentInput)
        : await calculateHealthRating(assessment as AssessmentInput);
      setResult(result);
      navigate("/results");
    } catch (err) {
      if (err instanceof HealthRatingApiError) {
        setSubmitError(err.errors);
      } else {
        setSubmitError(["Could not reach the HealthRater API. Is the backend running?"]);
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="shell assessment-page">
      <ProgressBar
        currentStep={step}
        totalSteps={TOTAL_STEPS}
        stepTitles={stepTitles}
        stepIds={stepIds}
      />

      {currentSection && (
        <div className="card section-card step-enter" key={currentSection.id}>
          <div className="section-card-head">
            <span className="section-card-index" aria-hidden="true">
              {String(step + 1).padStart(2, "0")}
            </span>
            <div>
              <h2>{currentSection.title}</h2>
              <p className="section-desc">{currentSection.description}</p>
            </div>
          </div>
          <div className="field-grid">
            {currentSection.fields.map((field) => (
              <FieldInput
                key={field.key}
                field={field}
                value={assessment[field.key]}
                onChange={(v) => updateField(field.key, v)}
                error={errors[field.key]}
              />
            ))}
          </div>
        </div>
      )}

      {isSummaryStep && (
        <div className="card section-card step-enter" key="review">
          <div className="section-card-head">
            <span className="section-card-index" aria-hidden="true">
              {String(step + 1).padStart(2, "0")}
            </span>
            <div>
              <h2>Review your answers</h2>
              <p className="section-desc">
                Everything below will be sent to the scoring engine. Go back to any section to make
                changes.
              </p>
            </div>
          </div>
          <div className="summary-list">
            {sections.map((section) => {
              const Icon = sectionIcons[section.id];
              return (
              <div key={section.id} className="summary-section">
                <h4>
                  {Icon && <Icon size={15} strokeWidth={2} aria-hidden="true" />}
                  {section.title}
                </h4>
                <dl>
                  {section.fields.map((field) => (
                    <div className="summary-row" key={field.key}>
                      <dt>{field.label}</dt>
                      <dd>
                        {String(assessment[field.key])}
                        {field.unit ? ` ${field.unit}` : ""}
                      </dd>
                    </div>
                  ))}
                </dl>
              </div>
              );
            })}
          </div>

          {submitError && (
            <div className="alert alert-error" role="alert">
              {submitError.map((e, i) => (
                <p key={i}>{e}</p>
              ))}
            </div>
          )}
        </div>
      )}

      <div className="step-nav">
        <button type="button" className="btn btn-ghost" onClick={goBack} disabled={step === 0}>
          <ArrowLeft size={16} strokeWidth={2} aria-hidden="true" />
          Back
        </button>
        {!isSummaryStep && (
          <button type="button" className="btn btn-primary" onClick={goNext}>
            Next
            <ArrowRight size={16} strokeWidth={2} aria-hidden="true" />
          </button>
        )}
        {isSummaryStep && (
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleCalculate}
            disabled={submitting}
          >
            {submitting && <LoaderCircle className="spin" size={16} strokeWidth={2} aria-hidden="true" />}
            {submitting ? "Calculating…" : "Calculate my Health Rating"}
          </button>
        )}
      </div>
    </div>
  );
}
