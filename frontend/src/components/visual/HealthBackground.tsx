import { useEffect, useRef } from "react";

export type BackgroundIntensity = "full" | "calm" | "rich";

interface HealthBackgroundProps {
  /** full = Home (100%), rich = Results (~85%), calm = Assessment (~65%). */
  intensity?: BackgroundIntensity;
}

// Hand-placed so the layout is deterministic (no Math.random between renders).
const PARTICLES = [
  { x: 8, y: 22, s: 3, d: 22, delay: -3 },
  { x: 18, y: 68, s: 2, d: 28, delay: -11 },
  { x: 27, y: 38, s: 2, d: 24, delay: -7 },
  { x: 39, y: 82, s: 3, d: 30, delay: -15 },
  { x: 52, y: 16, s: 2, d: 26, delay: -2 },
  { x: 61, y: 57, s: 3, d: 21, delay: -9 },
  { x: 72, y: 30, s: 2, d: 29, delay: -18 },
  { x: 81, y: 74, s: 3, d: 25, delay: -5 },
  { x: 90, y: 44, s: 2, d: 27, delay: -13 },
  { x: 95, y: 12, s: 2, d: 23, delay: -20 },
];

// Slow biometric curves; the first carries a single soft pulse — deliberately not an ECG trace.
const SIGNALS = [
  "M-40 610 C 180 560, 320 650, 520 600 S 760 540, 820 600 L 850 560 L 872 640 L 896 600 C 1020 570, 1180 640, 1480 580",
  "M-40 260 C 220 300, 420 200, 640 250 S 1040 330, 1480 230",
  "M-40 780 C 300 740, 520 820, 860 770 S 1240 720, 1480 790",
];

/**
 * Global "living health data" backdrop: warm base, drifting gradient orbs,
 * a faint measurement grid, a few floating points and soft biometric curves.
 *
 * Pointer tracking writes CSS variables directly on the root element (rAF
 * throttled), so moving the mouse never triggers a React re-render. It is only
 * attached on fine-pointer devices and when reduced motion is not requested.
 */
export function HealthBackground({ intensity = "full" }: HealthBackgroundProps) {
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;

    const finePointer = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!finePointer || reduced) return;

    let frame = 0;
    let x = 0;
    let y = 0;

    const apply = () => {
      frame = 0;
      const nx = x / window.innerWidth - 0.5;
      const ny = y / window.innerHeight - 0.5;
      root.style.setProperty("--px", nx.toFixed(3));
      root.style.setProperty("--py", ny.toFixed(3));
      root.style.setProperty("--cx", `${x}px`);
      root.style.setProperty("--cy", `${y}px`);
    };

    const onMove = (e: PointerEvent) => {
      x = e.clientX;
      y = e.clientY;
      root.dataset.pointer = "on";
      if (!frame) frame = requestAnimationFrame(apply);
    };
    const onLeave = () => {
      root.dataset.pointer = "off";
    };

    window.addEventListener("pointermove", onMove, { passive: true });
    document.documentElement.addEventListener("pointerleave", onLeave);
    return () => {
      window.removeEventListener("pointermove", onMove);
      document.documentElement.removeEventListener("pointerleave", onLeave);
      if (frame) cancelAnimationFrame(frame);
    };
  }, []);

  return (
    <div className="hb" data-intensity={intensity} ref={rootRef} aria-hidden="true">
      <div className="hb-orbs">
        <div className="hb-orb hb-orb-navy" />
        <div className="hb-orb hb-orb-emerald" />
        <div className="hb-orb hb-orb-mint" />
        <div className="hb-orb hb-orb-teal" />
      </div>

      <div className="hb-grid" />

      <svg className="hb-signals" viewBox="0 0 1440 900" preserveAspectRatio="xMidYMid slice">
        <defs>
          <linearGradient id="hb-line" x1="0" x2="1" y1="0" y2="0">
            <stop offset="0" stopColor="#1f7a64" stopOpacity="0" />
            <stop offset="0.35" stopColor="#1f7a64" stopOpacity="1" />
            <stop offset="0.7" stopColor="#14304a" stopOpacity="0.8" />
            <stop offset="1" stopColor="#14304a" stopOpacity="0" />
          </linearGradient>
        </defs>
        {SIGNALS.map((d, i) => (
          <g key={i} className={`hb-signal hb-signal-${i}`}>
            <path d={d} className="hb-signal-base" />
            <path d={d} className="hb-signal-flow" pathLength={1} />
          </g>
        ))}
      </svg>

      <div className="hb-particles">
        {PARTICLES.map((p, i) => (
          <span
            key={i}
            className="hb-particle"
            style={
              {
                left: `${p.x}%`,
                top: `${p.y}%`,
                "--s": `${p.s}px`,
                "--d": `${p.d}s`,
                animationDelay: `${p.delay}s`,
              } as React.CSSProperties
            }
          />
        ))}
      </div>

      <div className="hb-cursor" />
      <div className="hb-vignette" />
    </div>
  );
}
