import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, ChevronLeft, ChevronRight, X } from "lucide-react";
import { assessmentsApi } from "../../api/assessmentsApi";
import { ApiError } from "../../api/http";
import { stateMeta } from "../../data/uiMeta";
import type { CalendarEntry } from "../../types";
import { formatLongDate, formatMonthYear, formatTime, localDateKey, viewerTimeZone } from "../../utils/dates";
import { ErrorState } from "../StatusViews";

const WEEKDAYS = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];

interface HealthCalendarProps {
  /** Month to open on (1-based), e.g. the month of the latest assessment. */
  initialYear: number;
  initialMonth: number;
  onUnauthorized: () => void;
}

type MonthState = { status: "ready"; entries: CalendarEntry[] } | { status: "error"; message: string };

/**
 * Month calendar of completed assessments. Green marks a day on which an assessment was
 * completed — it says nothing about the result. Data comes from the lightweight calendar
 * endpoint, grouped by the viewer's local day.
 */
export function HealthCalendar({ initialYear, initialMonth, onUnauthorized }: HealthCalendarProps) {
  const [view, setView] = useState({ year: initialYear, month: initialMonth });
  const [months, setMonths] = useState<Record<string, MonthState>>({});
  const [selected, setSelected] = useState<string | null>(null);
  const timeZone = useMemo(() => viewerTimeZone(), []);
  const requested = useRef(new Set<string>());
  const monthKey = `${view.year}-${view.month}`;
  const current = months[monthKey];

  const load = useCallback(
    async (year: number, month: number) => {
      const key = `${year}-${month}`;
      try {
        const entries = await assessmentsApi.calendar(year, month, timeZone);
        setMonths((m) => ({ ...m, [key]: { status: "ready", entries } }));
      } catch (err) {
        if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
        setMonths((m) => ({
          ...m,
          [key]: { status: "error", message: err instanceof ApiError ? err.message : "Please try again." },
        }));
      }
    },
    [timeZone, onUnauthorized]
  );

  // A month without an entry is loading; each month is fetched once and then cached.
  useEffect(() => {
    if (requested.current.has(monthKey)) return;
    requested.current.add(monthKey);
    void load(view.year, view.month);
  }, [monthKey, view.year, view.month, load]);

  function retry() {
    requested.current.delete(monthKey);
    setMonths((m) => {
      const next = { ...m };
      delete next[monthKey];
      return next;
    });
    requested.current.add(monthKey);
    void load(view.year, view.month);
  }

  const byDay = useMemo(() => {
    const map = new Map<string, CalendarEntry[]>();
    if (current?.status === "ready") {
      for (const entry of current.entries) {
        const list = map.get(entry.date) ?? [];
        list.push(entry);
        map.set(entry.date, list);
      }
    }
    return map;
  }, [current]);

  const today = new Date();
  const todayKey = localDateKey(today);
  const isCurrentMonth = view.year === today.getFullYear() && view.month === today.getMonth() + 1;
  const firstWeekday = (new Date(view.year, view.month - 1, 1).getDay() + 6) % 7; // Monday = 0
  const daysInMonth = new Date(view.year, view.month, 0).getDate();
  const monthTotal = current?.status === "ready" ? current.entries.length : 0;

  function shift(delta: number) {
    const d = new Date(view.year, view.month - 1 + delta, 1);
    setView({ year: d.getFullYear(), month: d.getMonth() + 1 });
    setSelected(null);
  }

  const selectedEntries = selected ? byDay.get(selected) ?? [] : [];

  return (
    <div className="calendar">
      <div className="calendar-head">
        <button type="button" className="icon-btn" onClick={() => shift(-1)} aria-label="Previous month">
          <ChevronLeft size={18} strokeWidth={2} />
        </button>
        <div className="calendar-title" aria-live="polite">
          <h3>{formatMonthYear(view.year, view.month)}</h3>
          <span>
            {current?.status === "ready"
              ? monthTotal === 0
                ? "No assessments this month"
                : `${monthTotal} assessment${monthTotal === 1 ? "" : "s"} this month`
              : " "}
          </span>
        </div>
        <button
          type="button"
          className="icon-btn"
          onClick={() => shift(1)}
          disabled={isCurrentMonth}
          aria-label="Next month"
        >
          <ChevronRight size={18} strokeWidth={2} />
        </button>
      </div>

      {current?.status === "error" ? (
        <ErrorState title="Calendar unavailable" message={current.message} onRetry={retry} />
      ) : (
        <div className={`calendar-grid-wrap ${current?.status !== "ready" ? "is-loading" : ""}`} aria-busy={current?.status !== "ready"}>
          <div className="calendar-weekdays" aria-hidden="true">
            {WEEKDAYS.map((d) => (
              <span key={d}>{d}</span>
            ))}
          </div>
          <div className="calendar-grid" key={monthKey}>
            {Array.from({ length: firstWeekday }, (_, i) => (
              <span key={`pad-${i}`} className="cal-day cal-pad" aria-hidden="true" />
            ))}
            {Array.from({ length: daysInMonth }, (_, i) => {
              const day = i + 1;
              const key = `${view.year}-${String(view.month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
              const entries = byDay.get(key);
              const isToday = key === todayKey;
              if (!entries) {
                return (
                  <span key={key} className={`cal-day ${isToday ? "is-today" : ""}`}>
                    <span className="cal-num">{day}</span>
                  </span>
                );
              }
              const count = entries.length;
              return (
                <button
                  key={key}
                  type="button"
                  className={`cal-day has-scan ${isToday ? "is-today" : ""} ${selected === key ? "is-selected" : ""}`}
                  aria-pressed={selected === key}
                  aria-label={`${formatLongDate(key + "T12:00:00")}, ${count} assessment${count === 1 ? "" : "s"}`}
                  onClick={() => setSelected(selected === key ? null : key)}
                >
                  <span className="cal-num">{day}</span>
                  <span className="cal-dots" aria-hidden="true">
                    {Array.from({ length: Math.min(count, 3) }, (_, j) => (
                      <i key={j} />
                    ))}
                    {count > 3 && <b>+{count - 3}</b>}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      )}

      <p className="calendar-legend">
        <span className="legend-dot" aria-hidden="true" /> Assessment completed on this day
        <span className="legend-note">· select a highlighted day to see its scans</span>
      </p>

      {selected && selectedEntries.length > 0 && (
        <DayPanel date={selected} entries={selectedEntries} onClose={() => setSelected(null)} />
      )}
    </div>
  );
}

function DayPanel({ date, entries, onClose }: { date: string; entries: CalendarEntry[]; onClose: () => void }) {
  return (
    <section className="day-panel" aria-label={`Assessments on ${formatLongDate(date + "T12:00:00")}`}>
      <header className="day-panel-head">
        <div>
          <p className="eyebrow">Selected day</p>
          <h3>{formatLongDate(date + "T12:00:00")}</h3>
          {entries.length > 1 && <span className="day-panel-count">{entries.length} scans</span>}
        </div>
        <button type="button" className="icon-btn" onClick={onClose} aria-label="Close day details">
          <X size={16} strokeWidth={2} />
        </button>
      </header>

      {entries.map((entry) => (
        <article key={entry.assessmentId} className="day-scan">
          <div className="day-scan-top">
            <span className="day-scan-time">{formatTime(entry.completedAt)}</span>
            <span className="day-scan-total">
              <strong>{entry.totalHealthRating}</strong> / {entry.maxHealthRating}
            </span>
          </div>
          <ul className="state-bars">
            {stateMeta.map((meta) => {
              const value = Math.round(entry[meta.key]);
              return (
                <li key={meta.key} className={`tone-${meta.tone}`}>
                  <span className="state-bars-label">{meta.title}</span>
                  <span className="state-bars-track" aria-hidden="true">
                    <span style={{ transform: `scaleX(${value / 100})` }} />
                  </span>
                  <span className="state-bars-value">
                    {value}
                    <small> / 100</small>
                  </span>
                </li>
              );
            })}
          </ul>
          <Link to={`/history/${entry.assessmentId}`} className="btn btn-ghost btn-sm day-scan-open">
            View Full Assessment
            <ArrowRight size={15} strokeWidth={2} aria-hidden="true" />
          </Link>
        </article>
      ))}
    </section>
  );
}
