import { useEffect, useState } from "react";
import { ArrowLeftRight } from "lucide-react";
import { assessmentsApi } from "../../api/assessmentsApi";
import { ApiError } from "../../api/http";
import { stateMeta } from "../../data/uiMeta";
import type { AssessmentDetail, AssessmentSummary } from "../../types";
import { formatShortDate, formatTime } from "../../utils/dates";
import { Select } from "../Select";
import { ErrorState, Skeleton } from "../StatusViews";

interface Row {
  label: string;
  /** A fixed unit, or one that depends on the assessment (the total's maximum differs by parameter set). */
  unit?: string | ((d: AssessmentDetail) => string);
  decimals: number;
  get: (d: AssessmentDetail) => number | [number, number];
}

const rows: Row[] = [
  { label: "Total Health Rating", unit: (d) => `/ ${d.maxHealthRating}`, decimals: 0, get: (d) => d.totalHealthRating },
  ...stateMeta.map<Row>((m) => ({ label: m.title, unit: "/ 100", decimals: 1, get: (d) => d.fourStates[m.key].normalizedScore })),
  { label: "BMI", decimals: 2, get: (d) => d.derivedMetrics.bmi },
  { label: "WHtR", decimals: 3, get: (d) => d.derivedMetrics.wHtR },
  { label: "WHR", decimals: 3, get: (d) => d.derivedMetrics.whr },
  { label: "Body fat", unit: "%", decimals: 1, get: (d) => d.body.bodyFatPercentage },
  { label: "Resting heart rate", unit: "bpm", decimals: 0, get: (d) => d.cardiovascular.restingHeartRate },
  {
    label: "Blood pressure",
    unit: "mmHg",
    decimals: 0,
    get: (d) => [d.cardiovascular.bloodPressureSystolic, d.cardiovascular.bloodPressureDiastolic],
  },
];

const unitOf = (row: Row, d: AssessmentDetail) => (typeof row.unit === "function" ? row.unit(d) : row.unit);

const fmt = (v: number, decimals: number) => Number(v.toFixed(decimals)).toString();

function signed(delta: number, decimals: number) {
  const rounded = Number(delta.toFixed(decimals));
  if (rounded === 0) return "0";
  return `${rounded > 0 ? "+" : "−"}${fmt(Math.abs(rounded), decimals)}`;
}

const optionLabel = (s: AssessmentSummary) =>
  `${formatShortDate(s.completedAt)}, ${formatTime(s.completedAt)} · ${s.totalHealthRating}/${s.maxHealthRating}`;

/**
 * Side-by-side comparison of two saved assessments. Differences are shown as plain
 * recorded changes — HealthRater makes no judgement about whether a change is good or bad.
 */
export function CompareAssessments({ summaries, onUnauthorized }: { summaries: AssessmentSummary[]; onUnauthorized: () => void }) {
  // summaries are newest first: default to comparing the previous scan with the latest.
  const [firstId, setFirstId] = useState(summaries[1]?.id ?? "");
  const [secondId, setSecondId] = useState(summaries[0]?.id ?? "");
  const [details, setDetails] = useState<Record<string, AssessmentDetail>>({});
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const missing = [firstId, secondId].filter((id) => id && !details[id]);
    if (!missing.length) return;
    let cancelled = false;
    Promise.all(missing.map((id) => assessmentsApi.get(id)))
      .then((loaded) => {
        if (!cancelled) setDetails((d) => Object.fromEntries([...Object.entries(d), ...loaded.map((x) => [x.id, x])]));
      })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
        setError(err instanceof ApiError ? err.message : "Please try again.");
      });
    return () => {
      cancelled = true;
    };
  }, [firstId, secondId, details, onUnauthorized]);

  const options = summaries.map((s) => ({ value: s.id, label: optionLabel(s) }));
  const a = details[firstId];
  const b = details[secondId];
  // Always show the earlier scan as "previous", whichever order they were picked in.
  const [prev, next] = a && b && Date.parse(a.completedAt) > Date.parse(b.completedAt) ? [b, a] : [a, b];

  return (
    <section className="profile-card compare-card" aria-labelledby="compare-title">
      <header className="card-head">
        <div>
          <p className="eyebrow">Compare</p>
          <h3 id="compare-title">Compare two assessments</h3>
        </div>
      </header>

      <div className="compare-pickers">
        <div className="field">
          <label className="field-label" htmlFor="compare-a">
            First assessment
          </label>
          <Select id="compare-a" value={firstId} options={options.filter((o) => o.value !== secondId)} onChange={setFirstId} />
        </div>
        <ArrowLeftRight className="compare-swap" size={18} strokeWidth={2} aria-hidden="true" />
        <div className="field">
          <label className="field-label" htmlFor="compare-b">
            Second assessment
          </label>
          <Select id="compare-b" value={secondId} options={options.filter((o) => o.value !== firstId)} onChange={setSecondId} />
        </div>
      </div>

      {error ? (
        <ErrorState title="Comparison unavailable" message={error} onRetry={() => { setError(null); setDetails({}); }} />
      ) : !prev || !next ? (
        <div className="compare-loading" aria-busy="true">
          {rows.map((r) => (
            <Skeleton key={r.label} height={18} />
          ))}
        </div>
      ) : (
        <div className="table-scroll">
          <table className="compare-table">
            <thead>
              <tr>
                <th scope="col">Measure</th>
                <th scope="col">
                  Previous
                  <span>{formatShortDate(prev.completedAt)}</span>
                </th>
                <th scope="col">
                  Later
                  <span>{formatShortDate(next.completedAt)}</span>
                </th>
                <th scope="col">Difference</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const p = row.get(prev);
                const n = row.get(next);
                const show = (v: number | [number, number]) =>
                  Array.isArray(v) ? `${v[0]}/${v[1]}` : fmt(v, row.decimals);
                const diff = Array.isArray(p) && Array.isArray(n)
                  ? `${signed(n[0] - p[0], 0)} / ${signed(n[1] - p[1], 0)}`
                  : signed((n as number) - (p as number), row.decimals);
                return (
                  <tr key={row.label}>
                    <th scope="row">{row.label}</th>
                    <td>
                      {show(p)} {row.unit && <small>{unitOf(row, prev)}</small>}
                    </td>
                    <td>
                      {show(n)} {row.unit && <small>{unitOf(row, next)}</small>}
                    </td>
                    <td className="compare-diff">{diff}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <p className="chart-note">
        Differences are the recorded change between the two scans. HealthRater doesn't label them as better or worse.
      </p>
    </section>
  );
}
