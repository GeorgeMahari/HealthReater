import { useEffect, useState, type CSSProperties } from "react";
import { sections } from "../data/sections";
import { sectionIcons } from "../data/uiMeta";
import { useReducedMotion } from "../hooks/useReducedMotion";

/**
 * "What we'll ask about" — the eight assessment stages shown as a connected
 * journey. A soft highlight walks through the stages on its own; hovering a
 * stage takes over and reveals its description.
 */
export function AssessmentJourney() {
  const reduced = useReducedMotion();
  const [active, setActive] = useState(0);
  const [hovering, setHovering] = useState(false);

  useEffect(() => {
    if (reduced || hovering) return;
    const t = window.setInterval(() => setActive((a) => (a + 1) % sections.length), 2600);
    return () => window.clearInterval(t);
  }, [reduced, hovering]);

  const current = sections[active];

  return (
    <div className="journey" onMouseLeave={() => setHovering(false)}>
      <ol
        className="journey-rail"
        style={{ "--progress": active / (sections.length - 1) } as CSSProperties}
      >
        {sections.map((s, i) => {
          const Icon = sectionIcons[s.id];
          const state = i === active ? "is-active" : i < active ? "is-past" : "";
          return (
            <li
              key={s.id}
              className={`journey-step ${state}`}
              onMouseEnter={() => {
                setHovering(true);
                setActive(i);
              }}
            >
              <span className="journey-node" aria-hidden="true">
                {Icon && <Icon size={18} strokeWidth={1.8} />}
              </span>
              <span className="journey-num" aria-hidden="true">
                {String(i + 1).padStart(2, "0")}
              </span>
              <span className="journey-title">{s.title}</span>
              <span className="journey-count">{s.fields.length} questions</span>
            </li>
          );
        })}
      </ol>

      <div className="journey-detail" aria-hidden="true" key={current.id}>
        <span className="journey-detail-step">
          Stage {active + 1} of {sections.length}
        </span>
        <p>{current.description}</p>
      </div>
    </div>
  );
}
