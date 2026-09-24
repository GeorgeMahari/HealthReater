import { useEffect, useState } from "react";
import { useReducedMotion } from "./useReducedMotion";

/**
 * Animates from 0 up to `target` with an ease-out curve. Presentation only —
 * the final value is always exactly `target`.
 */
export function useCountUp(target: number, durationMs = 1200, delayMs = 0): number {
  const reduced = useReducedMotion();
  const [value, setValue] = useState(0);

  useEffect(() => {
    if (reduced) return;

    let frame = 0;
    let start: number | null = null;
    const tick = (now: number) => {
      if (start === null) start = now + delayMs;
      const t = Math.min(1, Math.max(0, (now - start) / durationMs));
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(target * eased);
      if (t < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [target, durationMs, delayMs, reduced]);

  return reduced ? target : value;
}
