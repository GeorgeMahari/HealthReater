import type { CSSProperties, ReactNode } from "react";
import { HeartPulse, Ruler } from "lucide-react";
import { ScoreCard } from "../ScoreCard";
import { TotalRatingCard } from "../TotalRatingCard";
import { Tooltip } from "../Tooltip";
import { HealthSignalVisual } from "../visual/HealthSignalVisual";
import { parameterGroups, parameterLabels, sectionIcons, stateMeta, termTooltips } from "../../data/uiMeta";
import type { AssessmentInput, HealthRatingResult } from "../../types";

interface ResultsViewProps {
  result: HealthRatingResult;
  /** The answers the result was calculated from (body fat, blood pressure, heart rate values). */
  input: AssessmentInput;
  eyebrow: ReactNode;
  title: ReactNode;
  /** Optional line under the title (saved note, historical date, …). */
  note?: ReactNode;
  actions: ReactNode;
}

/**
 * The HealthRater results report. Purely presentational: it displays the numbers it is
 * given — a fresh calculation or a stored historical snapshot — and never re-scores.
 */
export function ResultsView({ result, input, eyebrow, title, note, actions }: ResultsViewProps) {
  const { totalHealthRating, maxHealthRating, percentage, fourStates, derivedMetrics, parameterScores } =
    result;

  // Ordering of existing values only — nothing is re-scored.
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
          {eyebrow}
        </p>
        <h1 className="reveal" style={{ "--i": 0 } as CSSProperties}>
          {title}
        </h1>

        {note}

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
            value={withUnit(input.bodyFatPercent, "%")}
            score={parameterScores.bodyFat}
          />
        </MetricPanel>

        <MetricPanel icon={<HeartPulse size={18} strokeWidth={1.8} />} title="Cardiovascular">
          <MetricRow
            label="Blood Pressure"
            value={bloodPressure(input)}
            unit="mmHg"
            score={parameterScores.bloodPressure}
          />
          <MetricRow
            label="Resting HR"
            value={withUnit(input.restingHeartRateBpm, "")}
            unit="bpm"
            score={parameterScores.restingHeartRate}
          />
          <MetricRow
            label="Heart Rate Recovery"
            tip={termTooltips.heartRateRecovery}
            value={withUnit(input.heartRateRecoveryBpm, "")}
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

      <div className="results-actions">{actions}</div>
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

function formatKey(key: string): string {
  if (parameterLabels[key]) return parameterLabels[key];
  const withSpaces = key.replace(/([A-Z])/g, " $1");
  return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1).toLowerCase();
}
