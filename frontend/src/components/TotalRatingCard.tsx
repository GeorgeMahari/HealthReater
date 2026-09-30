import { useCountUp } from "../hooks/useCountUp";
import { ScoreRing } from "./visual/ScoreRing";
import { MAX_TOTAL_SCORE } from "../config/parameters";

interface TotalRatingCardProps {
  /** Omit on the Home page — the card then shows the "X / 410" placeholder. */
  total?: number;
  max?: number;
  percentage?: number;
  children?: React.ReactNode;
  className?: string;
}

function AnimatedTotal({ total, max, percentage }: { total: number; max: number; percentage: number }) {
  const shownTotal = useCountUp(total, 1300, 150);
  const shownPct = useCountUp(percentage, 1300, 150);
  return (
    <>
      <span className="trc-number">{Math.round(shownTotal)}</span>
      <span className="trc-max">/ {max}</span>
      <span className="trc-pct-chip">{Number(shownPct.toFixed(2))}%</span>
    </>
  );
}

/**
 * The product's central object. Presentation only — every figure shown comes
 * straight from the API result; nothing is recomputed here except the ring's
 * fill fraction (total / max).
 */
export function TotalRatingCard({
  total,
  max = MAX_TOTAL_SCORE,
  percentage,
  children,
  className = "",
}: TotalRatingCardProps) {
  const hasResult = total !== undefined && percentage !== undefined;
  const label = hasResult
    ? `Total Health Rating: ${total} out of ${max}, ${percentage}%`
    : `Total Health Rating: not yet calculated, out of ${max}`;

  return (
    <section className={`trc ${hasResult ? "trc-live" : "trc-placeholder"} ${className}`} aria-label={label}>
      <div className="trc-pattern" aria-hidden="true" />
      <p className="trc-label">
        <span className="status-dot" aria-hidden="true" />
        TOTAL HEALTH RATING
      </p>

      <ScoreRing fraction={hasResult ? total / max : undefined} size={232} stroke={11} delayMs={150}>
        <div className="trc-center" aria-hidden="true">
          {hasResult ? (
            <AnimatedTotal total={total} max={max} percentage={percentage} />
          ) : (
            <>
              <span className="trc-number trc-number-x">X</span>
              <span className="trc-max">/ {max}</span>
              <span className="trc-pct-chip trc-pct-chip-muted">awaiting data</span>
            </>
          )}
        </div>
      </ScoreRing>

      {children && <div className="trc-body">{children}</div>}
    </section>
  );
}
