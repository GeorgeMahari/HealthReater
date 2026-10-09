import { Link } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { stateMeta, termTooltips } from "../data/uiMeta";
import { HealthSignalVisual } from "../components/visual/HealthSignalVisual";
import { TotalRatingCard } from "../components/TotalRatingCard";
import { StateCard } from "../components/StateCard";
import { AssessmentJourney } from "../components/AssessmentJourney";
import { Tooltip } from "../components/Tooltip";
import { MAX_SCORE_PER_PARAMETER, MAX_TOTAL_SCORE, TOTAL_PARAMETER_COUNT } from "../config/parameters";

/** A metric abbreviation with its explanatory tooltip (hover, focus or tap). */
function Term({ term, label }: { term: string; label: string }) {
  return (
    <span className="term">
      {label}
      <Tooltip text={termTooltips[term]} label={`What is ${label}?`} />
    </span>
  );
}

export function HomePage() {
  return (
    <div className="shell home">
      <section className="hero-section">
        <div className="hero-copy">
          <p className="eyebrow reveal" style={{ "--i": 0 } as React.CSSProperties}>
            <span className="status-dot" aria-hidden="true" />
            {TOTAL_PARAMETER_COUNT} parameters · 1 comprehensive score
          </p>
          <h1 className="reveal" style={{ "--i": 1 } as React.CSSProperties}>
            Know your whole-body health, <span className="text-accent">in one number.</span>
          </h1>
          <p className="hero-sub reveal" style={{ "--i": 2 } as React.CSSProperties}>
            HealthRater scores {TOTAL_PARAMETER_COUNT} parameters (with <Term term="bmi" label="BMI" />,{" "}
            <Term term="whtr" label="WHtR" /> and <Term term="whr" label="WHR" /> calculated automatically) into a
            Total Health Rating out of {MAX_TOTAL_SCORE} — plus four supporting health states so you know exactly
            where to focus.
          </p>
          <div className="hero-actions reveal" style={{ "--i": 3 } as React.CSSProperties}>
            <Link to="/assessment" className="btn btn-primary btn-lg hero-cta">
              Start your assessment
              <ArrowRight size={18} strokeWidth={2} aria-hidden="true" />
            </Link>
            <span className="hero-meta">8 stages · about 5 minutes</span>
          </div>
        </div>
        <HealthSignalVisual className="hero-visual" />
      </section>

      <section className="rating-row">
        <TotalRatingCard className="reveal">
          <p className="rating-preview-note">
            Your score is the direct sum of {TOTAL_PARAMETER_COUNT} individually-scored parameters (1–
            {MAX_SCORE_PER_PARAMETER} each) — never a
            weighted average, and never rescaled to 0–100 for the main total. A percentage is shown
            alongside it for convenience.
          </p>
        </TotalRatingCard>
      </section>

      <section aria-labelledby="states-heading">
        <div className="section-head">
          <p className="eyebrow">Supporting health states</p>
          <h2 id="states-heading">Four lenses on the same {TOTAL_PARAMETER_COUNT} signals</h2>
        </div>
        <div className="states-grid">
          {stateMeta.map((s, i) => (
            <StateCard key={s.key} meta={s} index={i} />
          ))}
        </div>
      </section>

      <section className="sections-overview" aria-labelledby="journey-heading">
        <div className="section-head">
          <p className="eyebrow">Your assessment journey</p>
          <h2 id="journey-heading">What we'll ask about</h2>
        </div>
        <AssessmentJourney />
      </section>
    </div>
  );
}
