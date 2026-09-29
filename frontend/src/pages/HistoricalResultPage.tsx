import { useEffect, useState, type CSSProperties } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ClockArrowLeft, Download, Plus } from "lucide-react";
import { assessmentsApi } from "../api/assessmentsApi";
import { ApiError } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { useAssessment } from "../context/AssessmentContext";
import { ResultsView } from "../components/results/ResultsView";
import { saveResult } from "../utils/exportResult";
import { ErrorState, Skeleton } from "../components/StatusViews";
import { formatLongDate, formatTime } from "../utils/dates";
import type { AssessmentDetail } from "../types";

type State =
  | { kind: "loading" }
  | { kind: "ready"; detail: AssessmentDetail }
  | { kind: "not-found" }
  | { kind: "error"; message: string };

/**
 * A saved assessment, shown with the Results design. Every number comes from the stored
 * snapshot returned by the API — nothing is recalculated with today's scoring rules.
 */
export function HistoricalResultPage() {
  const { id = "" } = useParams();
  // Keyed by id so moving between saved assessments starts from a fresh loading state.
  return <HistoricalResult key={id} id={id} />;
}

function HistoricalResult({ id }: { id: string }) {
  const { expireSession } = useAuth();
  const { resetAssessment } = useAssessment();
  const [state, setState] = useState<State>({ kind: "loading" });

  const [reload, setReload] = useState(0);

  useEffect(() => {
    let ignore = false;
    assessmentsApi
      .get(id)
      .then((detail) => {
        if (!ignore) setState({ kind: "ready", detail });
      })
      .catch((err) => {
        if (ignore) return;
        if (err instanceof ApiError && err.isUnauthorized) return expireSession();
        if (err instanceof ApiError && err.isNotFound) return setState({ kind: "not-found" });
        setState({ kind: "error", message: err instanceof ApiError ? err.message : "Please try again." });
      });
    return () => {
      ignore = true;
    };
  }, [id, reload, expireSession]);

  if (state.kind === "loading") return <HistoricalSkeleton />;

  if (state.kind !== "ready") {
    return (
      <div className="shell history-status">
        <ErrorState
          title={state.kind === "not-found" ? "Assessment not found" : "We couldn't load this assessment"}
          message={
            state.kind === "not-found"
              ? "It may have been deleted, or the link is not part of your history."
              : state.message
          }
          onRetry={
            state.kind === "error"
              ? () => {
                  setState({ kind: "loading" });
                  setReload((r) => r + 1);
                }
              : undefined
          }
        >
          <Link to="/profile#history" className="btn btn-quiet btn-sm">
            <ArrowLeft size={15} strokeWidth={2} aria-hidden="true" />
            Back to history
          </Link>
        </ErrorState>
      </div>
    );
  }

  const { detail } = state;
  return (
    <>
      <div className="shell history-back">
        <Link to="/profile#history" className="btn btn-quiet btn-sm">
          <ArrowLeft size={15} strokeWidth={2} aria-hidden="true" />
          Back to history
        </Link>
      </div>
      <ResultsView
        result={detail}
        input={detail.input}
        eyebrow="Saved HealthRater result"
        title={`Assessment from ${formatLongDate(detail.completedAt)}`}
        note={
          <p className="saved-note saved-note-history reveal" style={{ "--i": 1 } as CSSProperties}>
            <ClockArrowLeft size={15} strokeWidth={2} aria-hidden="true" />
            Historical result · completed at{" "}
            <time dateTime={detail.completedAt}>{formatTime(detail.completedAt)}</time> ·{" "}
            {detail.body.sexAtAssessment}, {detail.body.ageAtAssessment} at the time · scores as recorded then
          </p>
        }
        actions={
          <>
            <Link to="/profile#history" className="btn btn-ghost">
              <ArrowLeft size={16} strokeWidth={2} aria-hidden="true" />
              Back to history
            </Link>
            <Link to="/assessment" className="btn btn-quiet" onClick={() => resetAssessment()}>
              <Plus size={16} strokeWidth={2} aria-hidden="true" />
              New assessment
            </Link>
            <button type="button" className="btn btn-quiet" onClick={() => saveResult(detail.input, detail)}>
              <Download size={16} strokeWidth={2} aria-hidden="true" />
              Save data
            </button>
          </>
        }
      />
    </>
  );
}

function HistoricalSkeleton() {
  return (
    <div className="shell results-page" aria-busy="true" aria-label="Loading assessment">
      <div className="results-hero">
        <Skeleton width={180} height={12} />
        <Skeleton width="min(520px, 90%)" height={40} className="sk-gap" />
        <Skeleton width="min(540px, 100%)" height={380} radius={20} className="sk-gap" />
      </div>
      <div className="states-results-grid">
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} height={150} radius={20} />
        ))}
      </div>
    </div>
  );
}
