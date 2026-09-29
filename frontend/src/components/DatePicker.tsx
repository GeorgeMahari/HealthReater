import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";
import { Select } from "./Select";
import { formatLongDate } from "../utils/dates";

const WEEKDAYS = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
const MONTHS = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

interface DatePickerProps {
  id: string;
  /** "yyyy-MM-dd" or "" when empty. */
  value: string;
  onChange: (value: string) => void;
  /** Earliest / latest selectable day, "yyyy-MM-dd". */
  min: string;
  max: string;
  placeholder?: string;
  invalid?: boolean;
  describedBy?: string;
}

const pad = (n: number) => String(n).padStart(2, "0");
const toKey = (y: number, m: number, d: number) => `${y}-${pad(m)}-${pad(d)}`;
const parse = (key: string) => {
  const [y, m, d] = key.split("-").map(Number);
  return { y, m, d };
};

/**
 * Styled date picker (the native date popup can't be themed): a trigger that shows the
 * chosen date and a calendar popover with month/year selectors, month arrows and a day
 * grid. Days outside [min, max] are disabled. Arrow keys move between days.
 */
export function DatePicker({ id, value, onChange, min, max, placeholder = "Select a date", invalid, describedBy }: DatePickerProps) {
  const lo = parse(min);
  const hi = parse(max);
  const initial = value ? parse(value) : hi;
  const [open, setOpen] = useState(false);
  const [view, setView] = useState({ year: initial.y, month: initial.m });
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const gridRef = useRef<HTMLDivElement>(null);
  const dialogId = useId();

  const years = useMemo(() => {
    const list = [];
    for (let y = hi.y; y >= lo.y; y--) list.push({ value: String(y), label: String(y) });
    return list;
  }, [hi.y, lo.y]);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: globalThis.KeyboardEvent) => {
      if (e.key === "Escape" && !e.defaultPrevented) {
        e.preventDefault(); // close only the calendar, not a surrounding <dialog>
        setOpen(false);
        triggerRef.current?.focus();
      }
    };
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  // When opening, move keyboard focus to the selected (or first enabled) day.
  useEffect(() => {
    if (!open) return;
    const target =
      gridRef.current?.querySelector<HTMLButtonElement>(".date-day.is-selected") ??
      gridRef.current?.querySelector<HTMLButtonElement>(".date-day:not(:disabled)");
    target?.focus();
  }, [open]);

  function toggle() {
    if (!open) {
      const start = value ? parse(value) : hi;
      setView({ year: start.y, month: start.m });
    }
    setOpen((o) => !o);
  }

  function shiftMonth(delta: number) {
    const d = new Date(view.year, view.month - 1 + delta, 1);
    const year = d.getFullYear();
    const month = d.getMonth() + 1;
    if (toKey(year, month, 1) > toKey(hi.y, hi.m, 1) || toKey(year, month, 31) < toKey(lo.y, lo.m, 1)) return;
    setView({ year, month });
  }

  function choose(key: string) {
    onChange(key);
    setOpen(false);
    triggerRef.current?.focus();
  }

  function onGridKey(e: KeyboardEvent<HTMLDivElement>) {
    const steps: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
    if (!(e.key in steps)) return;
    e.preventDefault();
    const buttons = Array.from(gridRef.current?.querySelectorAll<HTMLButtonElement>(".date-day") ?? []);
    const index = buttons.indexOf(document.activeElement as HTMLButtonElement);
    const next = buttons[index + steps[e.key]];
    if (next && !next.disabled) next.focus();
  }

  const firstWeekday = (new Date(view.year, view.month - 1, 1).getDay() + 6) % 7;
  const daysInMonth = new Date(view.year, view.month, 0).getDate();
  const canPrev = toKey(view.year, view.month, 1) > toKey(lo.y, lo.m, 1);
  const canNext = toKey(view.year, view.month, 1) < toKey(hi.y, hi.m, 1);

  return (
    <div className={`date-picker ${open ? "is-open" : ""}`} ref={rootRef}>
      <button
        ref={triggerRef}
        id={id}
        type="button"
        className={`date-trigger ${invalid ? "is-invalid" : ""}`}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? dialogId : undefined}
        aria-describedby={describedBy}
        onClick={toggle}
      >
        <span className={value ? "" : "select-placeholder"}>{value ? formatLongDate(`${value}T12:00:00`) : placeholder}</span>
        <CalendarDays size={17} strokeWidth={1.9} aria-hidden="true" />
      </button>

      {open && (
        <div className="date-popover" id={dialogId} role="dialog" aria-label="Choose date of birth">
          <div className="date-head">
            <button type="button" className="icon-btn" onClick={() => shiftMonth(-1)} disabled={!canPrev} aria-label="Previous month">
              <ChevronLeft size={17} strokeWidth={2} />
            </button>
            <div className="date-selects">
              <Select
                id={`${id}-month`}
                value={String(view.month)}
                options={MONTHS.map((name, i) => ({ value: String(i + 1), label: name }))}
                onChange={(m) => setView((v) => ({ ...v, month: Number(m) }))}
              />
              <Select
                id={`${id}-year`}
                value={String(view.year)}
                options={years}
                onChange={(y) => setView((v) => ({ ...v, year: Number(y) }))}
              />
            </div>
            <button type="button" className="icon-btn" onClick={() => shiftMonth(1)} disabled={!canNext} aria-label="Next month">
              <ChevronRight size={17} strokeWidth={2} />
            </button>
          </div>

          <div className="date-weekdays" aria-hidden="true">
            {WEEKDAYS.map((d) => (
              <span key={d}>{d}</span>
            ))}
          </div>
          <div className="date-grid" ref={gridRef} onKeyDown={onGridKey} key={`${view.year}-${view.month}`}>
            {Array.from({ length: firstWeekday }, (_, i) => (
              <span key={`pad-${i}`} aria-hidden="true" />
            ))}
            {Array.from({ length: daysInMonth }, (_, i) => {
              const key = toKey(view.year, view.month, i + 1);
              const disabled = key < min || key > max;
              const selected = key === value;
              return (
                <button
                  key={key}
                  type="button"
                  className={`date-day ${selected ? "is-selected" : ""}`}
                  disabled={disabled}
                  aria-pressed={selected}
                  aria-label={formatLongDate(`${key}T12:00:00`)}
                  onClick={() => choose(key)}
                >
                  {i + 1}
                </button>
              );
            })}
          </div>
          <p className="date-foot">Ages 18–100 · use the month and year menus to jump quickly</p>
        </div>
      )}
    </div>
  );
}
