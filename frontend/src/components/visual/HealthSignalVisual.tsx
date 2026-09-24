interface HealthSignalVisualProps {
  className?: string;
}

const TICKS = Array.from({ length: 72 }, (_, i) => i);

// Abstract nodes — descriptive words only, never numbers that could read as patient data.
const NODES = [
  { angle: -40, r: 150, label: "rhythm" },
  { angle: 70, r: 118, label: "balance" },
  { angle: 190, r: 150, label: "recovery" },
];

function polar(angleDeg: number, r: number) {
  const a = (angleDeg * Math.PI) / 180;
  return { x: 200 + r * Math.cos(a), y: 200 + r * Math.sin(a) };
}

function arc(r: number, startDeg: number, endDeg: number) {
  const s = polar(startDeg, r);
  const e = polar(endDeg, r);
  const large = endDeg - startDeg > 180 ? 1 : 0;
  return `M ${s.x} ${s.y} A ${r} ${r} 0 ${large} 1 ${e.x} ${e.y}`;
}

/**
 * Decorative "health is measurable data" motif: concentric measurement rings,
 * slow orbiting points, partial progress arcs and a soft pulse through the centre.
 * Purely ornamental — hidden from assistive tech.
 */
export function HealthSignalVisual({ className = "" }: HealthSignalVisualProps) {
  return (
    <div className={`hsv ${className}`} aria-hidden="true">
      <svg viewBox="0 0 400 400">
        <defs>
          <linearGradient id="hsv-arc" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#1f7a64" />
            <stop offset="1" stopColor="#14304a" />
          </linearGradient>
          <radialGradient id="hsv-core" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#bfe8d9" stopOpacity="0.9" />
            <stop offset="1" stopColor="#bfe8d9" stopOpacity="0" />
          </radialGradient>
        </defs>

        <circle cx="200" cy="200" r="120" fill="url(#hsv-core)" className="hsv-core" />

        <g className="hsv-rings">
          <circle cx="200" cy="200" r="186" />
          <circle cx="200" cy="200" r="150" />
          <circle cx="200" cy="200" r="118" />
          <circle cx="200" cy="200" r="78" />
        </g>

        <g className="hsv-ticks">
          {TICKS.map((i) => {
            const long = i % 6 === 0;
            const a = polar(i * 5, 186);
            const b = polar(i * 5, long ? 176 : 181);
            return <line key={i} x1={a.x} y1={a.y} x2={b.x} y2={b.y} />;
          })}
        </g>

        <g className="hsv-arcs">
          <path d={arc(150, -90, 150)} className="hsv-arc hsv-arc-1" pathLength={1} />
          <path d={arc(118, 10, 220)} className="hsv-arc hsv-arc-2" pathLength={1} />
          <path d={arc(78, 120, 330)} className="hsv-arc hsv-arc-3" pathLength={1} />
        </g>

        <g className="hsv-orbit hsv-orbit-outer">
          <circle cx="200" cy="14" r="4" className="hsv-orbit-dot" />
        </g>
        <g className="hsv-orbit hsv-orbit-inner">
          <circle cx="200" cy="122" r="3" className="hsv-orbit-dot hsv-orbit-dot-alt" />
        </g>

        <path className="hsv-pulse-base" d="M 96 200 L 150 200 C 160 200, 164 196, 170 196 L 182 200 L 192 168 L 204 230 L 214 190 L 222 200 L 304 200" />
        <path
          className="hsv-pulse"
          pathLength={1}
          d="M 96 200 L 150 200 C 160 200, 164 196, 170 196 L 182 200 L 192 168 L 204 230 L 214 190 L 222 200 L 304 200"
        />

        {NODES.map((n) => {
          const p = polar(n.angle, n.r);
          return (
            <g key={n.label} className="hsv-node" transform={`translate(${p.x} ${p.y})`}>
              <circle r="10" className="hsv-node-halo" />
              <circle r="3.5" className="hsv-node-dot" />
              <text x="12" y="4" className="hsv-node-label">
                {n.label}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
