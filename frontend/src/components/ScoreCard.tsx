import { useEffect, useState, type CSSProperties } from "react";
import type { StateMeta } from "../data/uiMeta";
import { useCountUp } from "../hooks/useCountUp";
import { useReducedMotion } from "../hooks/useReducedMotion";
import { Tooltip } from "./Tooltip";

interface ScoreCardProps {
  meta: StateMeta;
  normalizedScore: number;
  rawScore: number;
  maxRawScore: number;
  /** Stagger index for the sequential entrance. */
  index?: number;
}

/** Results-page card for one health state. Values are displayed as returned by the API. */
export function ScoreCard({ meta, normalizedScore, rawScore, maxRawScore, index = 0 }: ScoreCardProps) {
  const Icon = meta.icon;
  const reduced = useReducedMotion();
  const delay = 350 + index * 120;
  const shown = useCountUp(normalizedScore, 900, delay);
  const [animatedFill, setAnimatedFill] = useState(0);
  const fill = reduced ? normalizedScore : animatedFill;

  useEffect(() => {
    if (reduced) return;
    const t = window.setTimeout(() => setAnimatedFill(normalizedScore), delay);
    return () => window.clearTimeout(t);
  }, [normalizedScore, delay, reduced]);

  return (
    <article
      className={`score-card tone-${meta.tone} reveal`}
      style={{ "--i": index + 2 } as CSSProperties}
    >
      <div className="score-card-head">
        <div className="state-card-icon" aria-hidden="true">
          <Icon size={20} strokeWidth={1.8} />
        </div>
        <span className="score-card-pct" aria-hidden="true">
          {Math.round(shown)}
          <small>/100</small>
        </span>
      </div>
      <h3>
        {meta.title}
        {meta.tooltip && <Tooltip text={meta.tooltip} label={`About ${meta.title}`} />}
      </h3>
      <div
        className="score-card-track"
        role="meter"
        aria-label={`${meta.title} score`}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={normalizedScore}
      >
        <div className="score-card-fill" style={{ transform: `scaleX(${fill / 100})` }} />
      </div>
      <p className="score-card-sub">
        Raw: {rawScore} / {maxRawScore}
      </p>
    </article>
  );
}
