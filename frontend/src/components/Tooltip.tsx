import { useEffect, useId, useRef, useState } from "react";

interface TooltipProps {
  text: string;
  /** Accessible name for the trigger button. */
  label?: string;
}

type Align = "center" | "start" | "end";

/**
 * Hover / focus / tap tooltip. The bubble stays mounted so it can fade in,
 * is linked to the trigger with aria-describedby, and closes on Escape.
 */
export function Tooltip({ text, label = "More information" }: TooltipProps) {
  const [open, setOpen] = useState(false);
  const [align, setAlign] = useState<Align>("center");
  const wrapRef = useRef<HTMLSpanElement>(null);
  const id = useId();

  const show = () => {
    const rect = wrapRef.current?.getBoundingClientRect();
    if (rect) {
      const half = 130;
      if (rect.left < half) setAlign("start");
      else if (window.innerWidth - rect.right < half) setAlign("end");
      else setAlign("center");
    }
    setOpen(true);
  };

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
    const onDown = (e: PointerEvent) => {
      if (!wrapRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("keydown", onKey);
    document.addEventListener("pointerdown", onDown);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("pointerdown", onDown);
    };
  }, [open]);

  return (
    <span
      className={`tooltip-wrap ${open ? "tooltip-open" : ""}`}
      data-align={align}
      ref={wrapRef}
      onMouseEnter={show}
      onMouseLeave={() => setOpen(false)}
    >
      <button
        type="button"
        className="tooltip-icon"
        aria-label={label}
        aria-describedby={id}
        aria-expanded={open}
        onFocus={show}
        onBlur={() => setOpen(false)}
        onClick={(e) => {
          e.preventDefault();
          if (open) setOpen(false);
          else show();
        }}
      >
        i
      </button>
      <span className="tooltip-bubble" role="tooltip" id={id}>
        {text}
      </span>
    </span>
  );
}
