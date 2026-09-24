import type { CSSProperties, ReactNode } from "react";
import { Link, Navigate } from "react-router-dom";
import { CalendarCheck, Download, HeartPulse, RotateCcw, Ruler } from "lucide-react";
import { useAssessment } from "../context/AssessmentContext";
import { useAuth } from "../context/AuthContext";
import { ScoreCard } from "../components/ScoreCard";
import { TotalRatingCard } from "../components/TotalRatingCard";
import { Tooltip } from "../components/Tooltip";
import { HealthSignalVisual } from "../components/visual/HealthSignalVisual";
import { parameterGroups, parameterLabels, sectionIcons, stateMeta, termTooltips } from "../data/uiMeta";
import type { AssessmentInput, HealthRatingResult } from "../types";

export function ResultsPage() {
  const { result, assessment, resetAssessment } = useAssessment();
  const { user } = useAuth();

  if (!result) {
    return <Navigate to="/assessment" replace />;
  }

  const { totalHealthRating, maxHealthRating, percentage, fourStates, derivedMetrics, parameterScores } =
    result;

  // Ordering of existing API values only — nothing is re-scored.
  const ranked = [...stateMeta].sort(
    (a, b) => fourStates[b.key].normalizedScore - fourStates[a.key].normalizedScore
  );
  const strongest = ranked[0];
  const focus = ranked[ranked.length - 1];

  const groupedKeys = new Set(parameterGroups.flatMap((g) => g.keys));
  const otherKeys = Object.keys(parameterScores).filter((k) => !groupedKeys.has(k));
  const groups = [
    ...parameterGroups,
    ...(otherKeys.length ? [{ sectionId: "other", title: "Other", keys: otherKeys }] : []),
  ];

  return (
    <div className="shell results-page">
      <header className="results-hero">
        <HealthSignalVisual className="results-visual" />
        <p className="eyebrow reveal" style={{ "--i": 0 } as CSSProperties}>
          <span className="status-dot" aria-hidden="true" />
          Your HealthRater result
        </p>
        <h1 className="reveal" style={{ "--i": 0 } as CSSProperties}>
          Your personal health report
        </h1>

        {result.completedAt ? (
          <p className="saved-note reveal" style={{ "--i": 1 } as CSSProperties}>
            <CalendarCheck size={15} strokeWidth={2} aria-hidden="true" />
            Saved to your history ·{" "}
            <time dateTime={result.completedAt}>{formatLocalDateTime(result.completedAt)}</time>
          </p>
        ) : (
          !user && (
            <p className="saved-note saved-note-guest reveal" style={{ "--i": 1 } as CSSProperties}>
              <Link to="/login" state={{ from: "/assessment" }}>
                Log in
              </Link>{" "}
              to keep every assessment in your personal health history.
            </p>
          )
        )}

        <TotalRatingCard
          total={totalHealthRating}
          max={maxHealthRating}
          percentage={percentage}
          className="reveal results-total"
        >
          {strongest.key !== focus.key && (
            <div className="insight-row">
              <span className={`insight tone-${strongest.tone}`}>
                <small>Strongest state</small>
                {strongest.title}
              </span>
              <span className={`insight tone-${focus.tone}`}>
                <small>Most room to grow</small>
                {focus.title}
              </span>
            </div>
          )}
        </TotalRatingCard>
      </header>

      <section aria-labelledby="states-heading">
        <div className="section-head">
          <p className="eyebrow">Supporting health states</p>
          <h2 id="states-heading">Four Health States</h2>
        </div>
        <div className="states-results-grid">
          {stateMeta.map((meta, i) => (
            <ScoreCard
              key={meta.key}
              meta={meta}
              index={i}
              normalizedScore={fourStates[meta.key].normalizedScore}
              rawScore={fourStates[meta.key].rawScore}
              maxRawScore={fourStates[meta.key].maxRawScore}
            />
          ))}
        </div>
      </section>

      <div className="metric-panels reveal-late">
        <MetricPanel icon={<Ruler size={18} strokeWidth={1.8} />} title="Body Metrics">
          <MetricRow label="BMI" tip={termTooltips.bmi} value={String(derivedMetrics.bmi)} score={parameterScores.bmi} />
          <MetricRow label="WHtR" tip={termTooltips.whtr} value={String(derivedMetrics.wHtR)} score={parameterScores.whtr} />
          <MetricRow label="WHR" tip={termTooltips.whr} value={String(derivedMetrics.whr)} score={parameterScores.whr} />
          <MetricRow
            label="Body Fat"
            value={withUnit(assessment.bodyFatPercent, "%")}
            score={parameterScores.bodyFat}
          />
        </MetricPanel>

        <MetricPanel icon={<HeartPulse size={18} strokeWidth={1.8} />} title="Cardiovascular">
          <MetricRow
            label="Blood Pressure"
            value={bloodPressure(assessment)}
            unit="mmHg"
            score={parameterScores.bloodPressure}
          />
          <MetricRow
            label="Resting HR"
            value={withUnit(assessment.restingHeartRateBpm, "")}
            unit="bpm"
            score={parameterScores.restingHeartRate}
          />
          <MetricRow
            label="Heart Rate Recovery"
            tip={termTooltips.heartRateRecovery}
            value={withUnit(assessment.heartRateRecoveryBpm, "")}
            unit="bpm drop"
            score={parameterScores.heartRateRecovery}
          />
        </MetricPanel>
      </div>

      <section className="reveal-late" aria-labelledby="params-heading">
        <div className="section-head">
          <p className="eyebrow">Detailed parameters</p>
          <h2 id="params-heading">All {Object.keys(parameterScores).length} Parameter Scores</h2>
        </div>
        <div className="param-groups">
          {groups.map((group) => {
            const Icon = sectionIcons[group.sectionId];
            const keys = group.keys.filter((k) => k in parameterScores);
            if (keys.length === 0) return null;
            return (
              <div className="param-group" key={group.sectionId}>
                <h3>
                  {Icon && <Icon size={15} strokeWidth={2} aria-hidden="true" />}
                  {group.title}
                </h3>
                <ul>
                  {keys.map((key) => (
                    <ParamRow key={key} name={key} score={parameterScores[key]} />
                  ))}
                </ul>
              </div>
            );
          })}
        </div>
      </section>

      <div className="results-actions">
        <Link to="/assessment" className="btn btn-ghost" onClick={() => resetAssessment()}>
          <RotateCcw size={16} strokeWidth={2} aria-hidden="true" />
          Retake assessment
        </Link>
        <button type="button" className="btn btn-quiet" onClick={() => saveResult(assessment, result)}>
          <Download size={16} strokeWidth={2} aria-hidden="true" />
          Save data
        </button>
      </div>
    </div>
  );
}

function MetricPanel({ icon, title, children }: { icon: ReactNode; title: string; children: ReactNode }) {
  return (
    <section className="metric-panel card" aria-label={title}>
      <h2 className="metric-panel-title">
        <span className="metric-panel-icon" aria-hidden="true">
          {icon}
        </span>
        {title}
      </h2>
      <dl className="metric-rows">{children}</dl>
    </section>
  );
}

function MetricRow({
  label,
  value,
  unit,
  score,
  tip,
}: {
  label: string;
  value: string;
  unit?: string;
  score?: number;
  tip?: string;
}) {
  return (
    <div className="metric-row">
      <dt>
        {label}
        {tip && <Tooltip text={tip} label={`About ${label}`} />}
      </dt>
      <dd className="metric-value">
        {value}
        {unit && value !== "—" && <small> {unit}</small>}
      </dd>
      {score !== undefined && (
        <dd className={`metric-score ${scoreTone(score)}`}>
          <ScoreBar score={score} />
          <span>{score}/10</span>
        </dd>
      )}
    </div>
  );
}

function ParamRow({ name, score }: { name: string; score: number }) {
  const tip = termTooltips[name];
  return (
    <li className={`param-row ${scoreTone(score)}`}>
      <span className="param-name">
        {formatKey(name)}
        {tip && <Tooltip text={tip} label={`About ${formatKey(name)}`} />}
      </span>
      <ScoreBar score={score} />
      <strong>{score}</strong>
    </li>
  );
}

function ScoreBar({ score }: { score: number }) {
  return (
    <span className="score-bar" aria-hidden="true">
      <span style={{ transform: `scaleX(${Math.max(0, Math.min(10, score)) / 10})` }} />
    </span>
  );
}

function scoreTone(score: number): string {
  if (score >= 8) return "tone-good";
  if (score >= 5) return "tone-mid";
  return "tone-low";
}

function withUnit(value: number | string | "", unit: string): string {
  return value === "" || value === undefined ? "—" : `${value}${unit}`;
}

function bloodPressure(a: AssessmentInput): string {
  return a.systolicBpMmHg === "" || a.diastolicBpMmHg === ""
    ? "—"
    : `${a.systolicBpMmHg}/${a.diastolicBpMmHg}`;
}

/** Client-side export of the answers and the API result as JSON. */
function saveResult(assessment: AssessmentInput, result: HealthRatingResult) {
  const payload = { savedAt: new Date().toISOString(), assessment, result };
  const blob = new Blob([JSON.stringify(payload, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `healthrater-result-${new Date().toISOString().slice(0, 10)}.json`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

/** e.g. "25 September 2026, 14:32" in the viewer's own time zone and locale. */
function formatLocalDateTime(isoUtc: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "long", timeStyle: "short" }).format(new Date(isoUtc));
}

function formatKey(key: string): string {
  if (parameterLabels[key]) return parameterLabels[key];
  const withSpaces = key.replace(/([A-Z])/g, " $1");
  return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1).toLowerCase();
}
