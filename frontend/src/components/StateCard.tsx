import type { CSSProperties } from "react";
import type { StateMeta } from "../data/uiMeta";
import { Tooltip } from "./Tooltip";

interface StateCardProps {
  meta: StateMeta;
  /** Stagger index for the entrance animation. */
  index?: number;
}

/** Home-page description card for one of the four health states. */
export function StateCard({ meta, index = 0 }: StateCardProps) {
  const Icon = meta.icon;
  return (
    <article
      className={`state-card tone-${meta.tone} reveal`}
      style={{ "--i": index } as CSSProperties}
    >
      <div className="state-card-icon" aria-hidden="true">
        <Icon size={20} strokeWidth={1.8} />
      </div>
      <h3>
        {meta.title}
        {meta.tooltip && <Tooltip text={meta.tooltip} label={`About ${meta.title}`} />}
      </h3>
      <p>{meta.desc}</p>
      <div className="state-card-meter" aria-label={`${meta.parameterCount} contributing parameters`}>
        <span className="state-card-dots" aria-hidden="true">
          {Array.from({ length: 13 }, (_, i) => (
            <i key={i} className={i < meta.parameterCount ? "on" : ""} />
          ))}
        </span>
        <span className="state-card-count">{meta.parameterCount} parameters</span>
      </div>
    </article>
  );
}
