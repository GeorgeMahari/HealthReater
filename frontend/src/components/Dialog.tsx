import { useEffect, useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: ReactNode;
  children: ReactNode;
  tone?: "default" | "danger";
  /** Blocks closing via Escape/backdrop while something is in progress. */
  busy?: boolean;
}

/**
 * Modal built on the native <dialog> element: it gets a real modal top layer, focus
 * containment and Escape handling from the browser. Focus returns to the opener on close.
 */
export function Dialog({ open, onClose, title, description, children, tone = "default", busy = false }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      className={`dialog dialog-${tone}`}
      aria-labelledby={titleId}
      aria-describedby={description ? descId : undefined}
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onClose();
      }}
      onClick={(e) => {
        // A click on the backdrop lands on the <dialog> element itself.
        if (e.target === e.currentTarget && !busy) onClose();
      }}
    >
      {open && (
        <div className="dialog-body">
          <header className="dialog-head">
            <h2 id={titleId}>{title}</h2>
            <button type="button" className="dialog-close" onClick={onClose} disabled={busy} aria-label="Close">
              <X size={18} strokeWidth={2} />
            </button>
          </header>
          {description && (
            <div className="dialog-desc" id={descId}>
              {description}
            </div>
          )}
          {children}
        </div>
      )}
    </dialog>
  );
}
