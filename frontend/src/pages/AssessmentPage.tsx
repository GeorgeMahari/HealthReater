import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ProfileSummary } from "../components/ProfileSummary";
import { ArrowLeft, ArrowRight, Check, LoaderCircle } from "lucide-react";
import { sections, type FieldDef } from "../data/sections";
import { FieldInput } from "../components/FieldInput";
import { ProgressBar } from "../components/ProgressBar";
import { useAssessment } from "../context/AssessmentContext";
import { saveAssessment } from "../api/healthRatingApi";
import { ApiError } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { sectionIcons, termTooltips } from "../data/uiMeta";
import { Tooltip } from "../components/Tooltip";
import { BodyFatField } from "../components/assessment/BodyFatField";
import { useBodyFatEstimate } from "../components/assessment/bodyFatEstimate";
import { HeartRateRecoveryField } from "../components/assessment/HeartRateRecoveryField";
import { validateHeartRateRecovery } from "../components/assessment/heartRateRecovery";
import { heartRateRecoveryOf, type AssessmentAnswers } from "../types";

const TOTAL_STEPS = sections.length + 1; // + final summary step
const stepTitles = [...sections.map((s) => s.title), "Review & Calculate"];
const stepIds = [...sections.map((s) => s.id), "review"];

function validateField(field: FieldDef, answers: AssessmentAnswers): string | undefined {
  if (field.kind === "bodyFat") {
    if (answers.bodyFatMode === "") return "Choose whether you know your body fat percentage.";
    if (answers.bodyFatMode === "unknown") return undefined; // estimated by the API
  }
  if (field.kind === "heartRateRecovery") {
    return validateHeartRateRecovery(answers.peakHeartRateBpm, answers.heartRateAfter60sBpm, answers.restingHeartRateBpm);
  }
  const value = answers[field.key];
  if (value === "" || value === undefined || value === null) {
    return `${field.label} is required.`;
  }
  if ((field.kind === "number" || field.kind === "bodyFat") && typeof value === "number") {
    if (field.min !== undefined && value < field.min) {
      return `${field.label} must be at least ${field.min}${field.unit ? " " + field.unit : ""}.`;
    }
    if (field.max !== undefined && value > field.max) {
      return `${field.label} must be at most ${field.max}${field.unit ? " " + field.unit : ""}.`;
    }
  }
  return undefined;
}

const termLabel = (term: string) => ({ bmi: "BMI", whtr: "WHtR", whr: "WHR" })[term] ?? term;

function BodyFatSummaryRow({ answers }: { answers: AssessmentAnswers }) {
  const estimate = useBodyFatEstimate(answers.bodyFatMode === "unknown");
  return (
    <div className="summary-row">
      <dt>Body fat</dt>
      <dd>
        {answers.bodyFatMode === "unknown" ? (
          <>
            {estimate.kind === "ready" ? `${estimate.estimate.bodyFatPercent} %` : "—"}{" "}
            <span className="source-badge source-estimated">Estimated</span>
          </>
        ) : (
          <>
            {String(answers.bodyFatPercent)} % <span className="source-badge source-measured">Measured</span>
          </>
        )}
      </dd>
    </div>
  );
}

function HeartRateRecoverySummaryRows({ answers }: { answers: AssessmentAnswers }) {
  const hrr = heartRateRecoveryOf(answers);
  return (
    <>
      <div className="summary-row">
        <dt>Peak heart rate</dt>
        <dd>{String(answers.peakHeartRateBpm)} bpm</dd>
      </div>
      <div className="summary-row">
        <dt>Heart rate after 60 s</dt>
        <dd>{String(answers.heartRateAfter60sBpm)} bpm</dd>
      </div>
      <div className="summary-row">
        <dt>Heart Rate Recovery</dt>
        <dd>{hrr === null ? "—" : `${hrr} bpm`}</dd>
      </div>
    </>
  );
}

export function AssessmentPage() {
  const { assessment, updateField, setResult } = useAssessment();
  const { user, expireSession } = useAuth();
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
      const err = validateField(field, assessment);
      if (err) newErrors[field.key] = err;
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  /** Updates an answer and clears the error shown for the field it belongs to. */
  function change<K extends keyof AssessmentAnswers>(fieldKey: string, key: K, value: AssessmentAnswers[K]) {
    updateField(key, value);
    setErrors((prev) => {
      if (!(fieldKey in prev)) return prev;
      const next = { ...prev };
      delete next[fieldKey];
      return next;
    });
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
        const err = validateField(field, assessment);
        if (err) {
          setSubmitError([`${section.title}: ${err}`]);
          return;
        }
      }
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      // Only the answers are sent; the API adds sex, age, height and weight from the profile,
      // estimates body fat when it's unknown, and saves the result.
      const result = await saveAssessment(assessment);
      setResult(result);
      navigate("/results");
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return expireSession();
      if (err instanceof ApiError && err.status === 409) {
        return navigate("/complete-profile", { state: { from: "/assessment" } });
      }
      setSubmitError(
        err instanceof ApiError && err.status === 0
          ? ["Could not reach the HealthRater API. Is the backend running?"]
          : err instanceof ApiError
            ? err.errors
            : ["Something went wrong. Please try again."]
      );
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
              {currentSection.terms && (
                <p className="section-terms">
                  {currentSection.terms.map((term) => (
                    <span key={term} className="term-chip">
                      {termLabel(term)}
                      <Tooltip text={termTooltips[term]} label={`What is ${termLabel(term)}?`} />
                    </span>
                  ))}
                </p>
              )}
            </div>
          </div>
          {currentSection.fields.length === 0 && user ? (
            <ProfileSummary user={user} />
          ) : (
          <div className="field-grid">
            {currentSection.fields.map((field) =>
              field.kind === "bodyFat" ? (
                <BodyFatField
                  key={field.key}
                  field={field}
                  mode={assessment.bodyFatMode}
                  value={assessment.bodyFatPercent}
                  onModeChange={(mode) => change(field.key, "bodyFatMode", mode)}
                  onValueChange={(v) => change(field.key, "bodyFatPercent", v)}
                  error={errors[field.key]}
                />
              ) : field.kind === "heartRateRecovery" ? (
                <HeartRateRecoveryField
                  key={field.key}
                  field={field}
                  peak={assessment.peakHeartRateBpm}
                  after60={assessment.heartRateAfter60sBpm}
                  onPeakChange={(v) => change(field.key, "peakHeartRateBpm", v)}
                  onAfter60Change={(v) => change(field.key, "heartRateAfter60sBpm", v)}
                  error={errors[field.key]}
                />
              ) : (
                <FieldInput
                  key={field.key}
                  field={field}
                  value={assessment[field.key]}
                  onChange={(v) => change(field.key, field.key, v)}
                  error={errors[field.key]}
                />
              )
            )}
          </div>
          )}
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
                  {section.fields.length === 0 && user && (
                    <>
                      <div className="summary-row"><dt>Sex</dt><dd>{user.sex}</dd></div>
                      <div className="summary-row"><dt>Age</dt><dd>{user.age} years</dd></div>
                      <div className="summary-row"><dt>Height</dt><dd>{user.heightCm} cm</dd></div>
                      <div className="summary-row"><dt>Weight</dt><dd>{user.weightKg} kg</dd></div>
                    </>
                  )}
                  {section.fields.map((field) =>
                    field.kind === "bodyFat" ? (
                      <BodyFatSummaryRow key={field.key} answers={assessment} />
                    ) : field.kind === "heartRateRecovery" ? (
                      <HeartRateRecoverySummaryRows key={field.key} answers={assessment} />
                    ) : (
                      <div className="summary-row" key={field.key}>
                        <dt>{field.label}</dt>
                        <dd>
                          {field.kind === "select"
                            ? (field.options?.find((o) => o.value === assessment[field.key])?.label ?? String(assessment[field.key]))
                            : String(assessment[field.key])}
                          {field.unit && field.kind !== "select" ? ` ${field.unit}` : ""}
                        </dd>
                      </div>
                    )
                  )}
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
            {currentSection?.fields.length === 0 ? (
              <>
                <Check size={16} strokeWidth={2.2} aria-hidden="true" />
                Confirm and continue
              </>
            ) : (
              <>
                Next
                <ArrowRight size={16} strokeWidth={2} aria-hidden="true" />
              </>
            )}
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
