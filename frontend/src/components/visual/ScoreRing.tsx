import { useEffect, useId, useState, type ReactNode } from "react";
import { useReducedMotion } from "../../hooks/useReducedMotion";

interface ScoreRingProps {
  /** 0–1. Omit for the idle/placeholder state. */
  fraction?: number;
  size?: number;
  stroke?: number;
  delayMs?: number;
  children?: ReactNode;
  className?: string;
}

/** Circular progress ring. The arc animates in once on mount (unless reduced motion). */
export function ScoreRing({
  fraction,
  size = 220,
  stroke = 10,
  delayMs = 0,
  children,
  className = "",
}: ScoreRingProps) {
  const id = useId();
  const reduced = useReducedMotion();
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const idle = fraction === undefined;
  const target = idle ? 0 : Math.min(1, Math.max(0, fraction));

  const [animated, setAnimated] = useState(0);
  const shown = reduced ? target : animated;
  useEffect(() => {
    if (reduced) return;
    const t = window.setTimeout(() => setAnimated(target), 60 + delayMs);
    return () => window.clearTimeout(t);
  }, [target, delayMs, reduced]);

  return (
    <div className={`ring ${idle ? "ring-idle" : ""} ${className}`} style={{ width: size, height: size }}>
      <svg viewBox={`0 0 ${size} ${size}`} aria-hidden="true">
        <defs>
          <linearGradient id={`${id}-g`} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#2a9d8f" />
            <stop offset="0.55" stopColor="#1f7a64" />
            <stop offset="1" stopColor="#14304a" />
          </linearGradient>
        </defs>
        <circle className="ring-track" cx={size / 2} cy={size / 2} r={r} strokeWidth={stroke} />
        <circle
          className="ring-ticks"
          cx={size / 2}
          cy={size / 2}
          r={r - stroke - 6}
          strokeDasharray="1 7"
        />
        {idle ? (
          <circle
            className="ring-idle-arc"
            cx={size / 2}
            cy={size / 2}
            r={r}
            strokeWidth={stroke}
            stroke={`url(#${id}-g)`}
            strokeDasharray={`${c * 0.18} ${c}`}
          />
        ) : (
          <circle
            className="ring-value"
            cx={size / 2}
            cy={size / 2}
            r={r}
            strokeWidth={stroke}
            stroke={`url(#${id}-g)`}
            strokeDasharray={c}
            strokeDashoffset={c * (1 - shown)}
          />
        )}
      </svg>
      <div className="ring-center">{children}</div>
    </div>
  );
}
