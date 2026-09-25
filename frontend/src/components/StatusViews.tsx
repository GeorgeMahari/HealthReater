import type { CSSProperties, ReactNode } from "react";
import { CloudOff, RotateCw } from "lucide-react";

/** Shimmering placeholder block. */
export function Skeleton({ width, height = 14, radius, className = "" }: {
  width?: number | string;
  height?: number | string;
  radius?: number;
  className?: string;
}) {
  return (
    <span
      className={`skeleton ${className}`}
      style={{ width, height, borderRadius: radius } as CSSProperties}
      aria-hidden="true"
    />
  );
}

/** Friendly error card with an optional retry. Never shows raw exception text. */
export function ErrorState({ title = "Something went wrong", message, onRetry, children }: {
  title?: string;
  message: string;
  onRetry?: () => void;
  children?: ReactNode;
}) {
  return (
    <div className="error-state" role="alert">
      <span className="error-state-icon" aria-hidden="true">
        <CloudOff size={20} strokeWidth={1.8} />
      </span>
      <div>
        <strong>{title}</strong>
        <p>{message}</p>
        <div className="error-state-actions">
          {onRetry && (
            <button type="button" className="btn btn-ghost btn-sm" onClick={onRetry}>
              <RotateCw size={15} strokeWidth={2} aria-hidden="true" />
              Try again
            </button>
          )}
          {children}
        </div>
      </div>
    </div>
  );
}
