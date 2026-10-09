import { stateMeta } from "../../data/uiMeta";
import type { AssessmentSummary } from "../../types";
import { formatDayMonth, formatShortDate, formatTime } from "../../utils/dates";
import { niceDomain } from "../../utils/chartScale";
import { LineChart, type ChartPoint } from "./LineChart";
import { MAX_TOTAL_SCORE } from "../../config/parameters";

const TOTAL_COLOR = "#1d8a5e";

/** The current maximum, plus older maxima when the history mixes parameter sets (e.g. v1 assessments). */
function maxLabel(summaries: AssessmentSummary[]): { max: number; text: string } {
  const maxima = [...new Set(summaries.map((s) => s.maxHealthRating))].sort((a, b) => b - a);
  const max = maxima[0] ?? MAX_TOTAL_SCORE;
  const others = maxima.slice(1);
  return {
    max,
    text: others.length ? `${max} (${others.join(", ")} for assessments recorded with an earlier parameter set)` : `${max}`,
  };
}

/**
 * Score history: Total Health Rating over time, plus one small chart per health state
 * (small multiples rather than four overlapping lines, so no series depends on colour to
 * be told apart). Purely a record of past scores — no trend interpretation.
 */
export function ScoreHistory({
  summaries,
}: {
  summaries: AssessmentSummary[];
}) {
  const ordered = [...summaries].sort(
    (a, b) => Date.parse(a.completedAt) - Date.parse(b.completedAt),
  );
  const base = ordered.map((s) => ({
    id: s.id,
    x: Date.parse(s.completedAt),
    label: `${formatShortDate(s.completedAt)}, ${formatTime(s.completedAt)}`,
    tick: formatDayMonth(s.completedAt),
  }));

  const totals: ChartPoint[] = base.map((b, i) => ({
    ...b,
    y: ordered[i].totalHealthRating,
  }));
  const { max, text: maxText } = maxLabel(ordered);
  const { domain, ticks } = niceDomain(
    totals.map((p) => p.y),
    0,
    max,
  );

  return (
    <section
      className="profile-card score-history"
      aria-labelledby="score-history-title"
    >
      <header className="card-head">
        <div>
          <p className="eyebrow">Score history</p>
          <h3 id="score-history-title">Total Health Rating over time</h3>
          <p className="card-sub">
            Out of {maxText}, as recorded at each of your {ordered.length}{" "}
            assessments.
          </p>
        </div>
      </header>

      <LineChart
        points={totals}
        domain={domain}
        ticks={ticks}
        color={TOTAL_COLOR}
        height={240}
        formatValue={(v) => `${v} / ${max}`}
        ariaLabel={`Total Health Rating for ${ordered.length} assessments, from ${totals[0].y} to ${totals[totals.length - 1].y} out of ${maxText}`}
      />

      <h4 className="multiples-title">Four health states</h4>
      <div className="multiples">
        {stateMeta.map((meta) => {
          const Icon = meta.icon;
          const points = base.map((b, i) => ({
            ...b,
            y: Math.round(ordered[i].fourStates[meta.key] * 10) / 10,
          }));
          const latest = points[points.length - 1].y;
          return (
            <div key={meta.key} className={`multiple tone-${meta.tone}`}>
              <div className="multiple-head">
                <span className="multiple-icon" aria-hidden="true">
                  <Icon size={14} strokeWidth={2} />
                </span>
                <span className="multiple-name">{meta.title}</span>
                <span className="multiple-latest">
                  {Math.round(latest)}
                  <small> / 100</small>
                </span>
              </div>
              <LineChart
                compact
                points={points}
                domain={[0, 100]}
                ticks={[0, 50, 100]}
                color="var(--tone)"
                height={120}
                formatValue={(v) => `${v} / 100`}
                ariaLabel={`${meta.title} score for ${points.length} assessments, latest ${Math.round(latest)} out of 100`}
              />
            </div>
          );
        })}
      </div>

      <p className="chart-note">
        These charts show the scores you recorded. They aren't a prediction or a
        medical assessment.
      </p>

      {/* Table view of the same data for screen readers (wrapped: sr-only can't clip a <table> itself). */}
      <div className="sr-only">
        <table>
          <caption>Score history</caption>
          <thead>
            <tr>
              <th scope="col">Date</th>
              <th scope="col">Total</th>
              {stateMeta.map((m) => (
                <th scope="col" key={m.key}>
                  {m.title} (of 100)
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {ordered.map((s, i) => (
              <tr key={s.id}>
                <th scope="row">{base[i].label}</th>
                <td>
                  {s.totalHealthRating} / {s.maxHealthRating}
                </td>
                {stateMeta.map((m) => (
                  <td key={m.key}>{Math.round(s.fourStates[m.key])}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
