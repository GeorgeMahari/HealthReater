import { useEffect, useId, useRef, useState, type KeyboardEvent, type PointerEvent } from "react";

export interface ChartPoint {
  id: string;
  /** Epoch milliseconds. */
  x: number;
  y: number;
  /** Human label for the tooltip, e.g. "25 Sep 2026, 14:32". */
  label: string;
  /** Short x-axis label, e.g. "25 Sep". */
  tick: string;
}

interface LineChartProps {
  points: ChartPoint[];
  domain: [number, number];
  ticks: number[];
  /** Series colour (marks only — text always uses text tokens). */
  color: string;
  height: number;
  /** Formats a value for the tooltip and end label, e.g. v => `${v} / 390`. */
  formatValue: (v: number) => string;
  /** Accessible summary of the chart. */
  ariaLabel: string;
  compact?: boolean;
}

function useWidth<T extends HTMLElement>() {
  const ref = useRef<T>(null);
  const [width, setWidth] = useState(0);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setWidth(Math.round(entry.contentRect.width)));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);
  return [ref, width] as const;
}

/**
 * Single-series line chart: 2px line, 10% area wash, ringed markers, hairline grid, an
 * end-value label, and a crosshair tooltip that snaps to the nearest scan (pointer or
 * arrow keys). Values are plotted exactly as recorded.
 */
export function LineChart({ points, domain, ticks, color, height, formatValue, ariaLabel, compact = false }: LineChartProps) {
  const [wrapRef, width] = useWidth<HTMLDivElement>();
  const [active, setActive] = useState<number | null>(null);
  const gradientId = useId();

  const m = compact ? { l: 30, r: 40, t: 10, b: 22 } : { l: 42, r: 64, t: 14, b: 30 };
  const innerW = Math.max(0, width - m.l - m.r);
  const innerH = height - m.t - m.b;

  const xs = points.map((p) => p.x);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const sx = (x: number) => m.l + (maxX === minX ? innerW / 2 : ((x - minX) / (maxX - minX)) * innerW);
  const sy = (y: number) => m.t + innerH - ((y - domain[0]) / (domain[1] - domain[0])) * innerH;

  const coords = points.map((p) => ({ ...p, cx: sx(p.x), cy: sy(p.y) }));
  const line = coords.map((c, i) => `${i ? "L" : "M"}${c.cx.toFixed(1)},${c.cy.toFixed(1)}`).join(" ");
  const baseline = sy(domain[0]);
  const area = coords.length > 1
    ? `${line} L${coords[coords.length - 1].cx.toFixed(1)},${baseline} L${coords[0].cx.toFixed(1)},${baseline} Z`
    : "";

  // X labels: first and last always; in the full chart up to ~6 evenly spaced in between.
  const maxLabels = compact ? 2 : Math.max(2, Math.floor(innerW / 90));
  const labelIdx = new Set<number>();
  if (coords.length) {
    const step = Math.max(1, Math.ceil((coords.length - 1) / (maxLabels - 1)));
    for (let i = 0; i < coords.length; i += step) labelIdx.add(i);
    labelIdx.add(coords.length - 1);
  }

  function nearest(clientX: number, rectLeft: number) {
    const x = clientX - rectLeft;
    let best = 0;
    coords.forEach((c, i) => {
      if (Math.abs(c.cx - x) < Math.abs(coords[best].cx - x)) best = i;
    });
    return best;
  }

  function onPointerMove(e: PointerEvent<SVGSVGElement>) {
    setActive(nearest(e.clientX, e.currentTarget.getBoundingClientRect().left));
  }

  function onKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    if (!coords.length) return;
    if (e.key === "ArrowRight" || e.key === "ArrowLeft") {
      e.preventDefault();
      const dir = e.key === "ArrowRight" ? 1 : -1;
      setActive((a) => Math.min(coords.length - 1, Math.max(0, (a ?? coords.length - 1) + (a === null ? 0 : dir))));
    } else if (e.key === "Escape") {
      setActive(null);
    }
  }

  const last = coords[coords.length - 1];
  const hovered = active !== null ? coords[active] : null;

  return (
    <div
      ref={wrapRef}
      className={`line-chart ${compact ? "is-compact" : ""}`}
      style={{ height }}
      tabIndex={0}
      role="group"
      aria-label={`${ariaLabel}. Use the left and right arrow keys to read each value.`}
      onKeyDown={onKeyDown}
      onFocus={() => setActive((a) => a ?? coords.length - 1)}
      onBlur={() => setActive(null)}
    >
      {width > 0 && (
        <svg width={width} height={height} onPointerMove={onPointerMove} onPointerLeave={() => setActive(null)} aria-hidden="true">
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor={color} stopOpacity="0.14" />
              <stop offset="1" stopColor={color} stopOpacity="0.02" />
            </linearGradient>
          </defs>

          {ticks.map((t) => (
            <g key={t} className="chart-grid">
              <line x1={m.l} x2={width - m.r} y1={sy(t)} y2={sy(t)} />
              <text x={m.l - 8} y={sy(t)} dy="0.32em" textAnchor="end">
                {t}
              </text>
            </g>
          ))}

          {coords.map((c, i) =>
            labelIdx.has(i) ? (
              <text
                key={c.id}
                className="chart-xlabel"
                x={c.cx}
                y={height - 6}
                textAnchor={coords.length === 1 ? "middle" : i === 0 ? "start" : i === coords.length - 1 ? "end" : "middle"}
              >
                {c.tick}
              </text>
            ) : null
          )}

          {area && <path d={area} fill={`url(#${gradientId})`} className="chart-area" />}
          {coords.length > 1 && <path d={line} className="chart-line" stroke={color} pathLength={1} />}

          {hovered && <line className="chart-crosshair" x1={hovered.cx} x2={hovered.cx} y1={m.t} y2={baseline} />}

          {coords.map((c, i) => (
            <circle
              key={c.id}
              className={`chart-dot ${active === i ? "is-active" : ""}`}
              cx={c.cx}
              cy={c.cy}
              r={active === i ? 5.5 : 4}
              fill={color}
            />
          ))}

          {last && (
            <text className="chart-end" x={last.cx + 10} y={last.cy} dy="0.32em">
              {formatValue(last.y).split(" ")[0]}
            </text>
          )}
        </svg>
      )}

      {hovered && (
        <div
          className={`chart-tooltip ${hovered.cx > width - 150 ? "flip" : ""}`}
          style={{ left: hovered.cx, top: hovered.cy }}
          role="status"
        >
          <strong>{formatValue(hovered.y)}</strong>
          <span>{hovered.label}</span>
        </div>
      )}
    </div>
  );
}
