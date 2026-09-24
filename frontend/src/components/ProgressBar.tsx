import { Check } from "lucide-react";
import { sectionIcons } from "../data/uiMeta";

interface ProgressBarProps {
  currentStep: number; // 0-indexed
  totalSteps: number;
  stepTitles: string[];
  /** Section ids per step, used for icons ("review" for the summary step). */
  stepIds: string[];
}

export function ProgressBar({ currentStep, totalSteps, stepTitles, stepIds }: ProgressBarProps) {
  const pct = Math.round(((currentStep + 1) / totalSteps) * 100);
  const CurrentIcon = sectionIcons[stepIds[currentStep]];

  return (
    <div className="progress-wrap">
      <div className="progress-head">
        <div className="progress-current">
          <span className="progress-icon" aria-hidden="true">
            {CurrentIcon && <CurrentIcon size={20} strokeWidth={1.8} />}
          </span>
          <div>
            <p className="progress-step">
              Step {currentStep + 1} of {totalSteps}
            </p>
            <p className="progress-title" key={currentStep}>
              {stepTitles[currentStep]}
            </p>
          </div>
        </div>
        <span className="progress-pct" aria-hidden="true">
          {pct}%
        </span>
      </div>

      <div
        className="progress-track"
        role="progressbar"
        aria-label="Assessment progress"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={pct}
      >
        <div className="progress-fill" style={{ transform: `scaleX(${pct / 100})` }} />
      </div>

      <ol className="progress-steps" aria-hidden="true">
        {stepTitles.map((title, i) => {
          const Icon = sectionIcons[stepIds[i]];
          const state = i < currentStep ? "is-done" : i === currentStep ? "is-current" : "";
          return (
            <li key={title} className={`progress-dot ${state}`} title={title}>
              {i < currentStep ? <Check size={12} strokeWidth={2.6} /> : Icon && <Icon size={12} strokeWidth={2} />}
            </li>
          );
        })}
      </ol>
    </div>
  );
}
