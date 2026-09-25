import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import { stateMeta } from "../../data/uiMeta";
import type { AssessmentSummary } from "../../types";
import { formatShortDate, formatTime } from "../../utils/dates";

const INITIAL = 8;

/** Every completed assessment, newest first; each opens its full historical result. */
export function HistoryList({ summaries }: { summaries: AssessmentSummary[] }) {
  const [showAll, setShowAll] = useState(false);
  const visible = showAll ? summaries : summaries.slice(0, INITIAL);

  return (
    <section className="profile-card history-list-card" aria-labelledby="history-list-title">
      <header className="card-head">
        <div>
          <p className="eyebrow">All assessments</p>
          <h3 id="history-list-title">
            {summaries.length} completed assessment{summaries.length === 1 ? "" : "s"}
          </h3>
        </div>
      </header>

      <ol className="history-list">
        {visible.map((s, i) => (
          <li key={s.id} style={{ animationDelay: `${Math.min(i, 8) * 40}ms` }}>
            <Link to={`/history/${s.id}`} className="history-item">
              <span className="history-date">
                <strong>{formatShortDate(s.completedAt)}</strong>
                <span>{formatTime(s.completedAt)}</span>
              </span>
              <span className="history-total">
                <strong>{s.totalHealthRating}</strong>
                <span>/ {s.maxHealthRating}</span>
              </span>
              <span className="history-states" aria-label="Four health states">
                {stateMeta.map((meta) => {
                  const value = Math.round(s.fourStates[meta.key]);
                  return (
                    <span key={meta.key} className={`history-state tone-${meta.tone}`} title={`${meta.title}: ${value} / 100`}>
                      <span className="sr-only">
                        {meta.title}: {value} out of 100
                      </span>
                      <span className="history-state-track" aria-hidden="true">
                        <span style={{ transform: `scaleY(${value / 100})` }} />
                      </span>
                    </span>
                  );
                })}
              </span>
              <span className="history-open">
                <span className="sr-only">Open full assessment</span>
                <ChevronRight size={18} strokeWidth={2} aria-hidden="true" />
              </span>
            </Link>
          </li>
        ))}
      </ol>

      {summaries.length > INITIAL && (
        <button type="button" className="btn btn-quiet btn-sm history-more" onClick={() => setShowAll((v) => !v)}>
          {showAll ? "Show fewer" : `Show all ${summaries.length}`}
        </button>
      )}
    </section>
  );
}
