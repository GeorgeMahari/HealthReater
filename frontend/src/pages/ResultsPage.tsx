import type { CSSProperties } from "react";
import { Link, Navigate } from "react-router-dom";
import { CalendarCheck, Download, RotateCcw } from "lucide-react";
import { useAssessment } from "../context/AssessmentContext";
import { useAuth } from "../context/AuthContext";
import { ResultsView } from "../components/results/ResultsView";
import { saveResult } from "../utils/exportResult";
import { formatLongDate, formatTime } from "../utils/dates";

/** The result of the assessment just completed (kept in AssessmentContext). */
export function ResultsPage() {
  const { result, assessment, resetAssessment } = useAssessment();
  const { user } = useAuth();

  if (!result) {
    return <Navigate to="/assessment" replace />;
  }

  const note = result.completedAt ? (
    <p className="saved-note reveal" style={{ "--i": 1 } as CSSProperties}>
      <CalendarCheck size={15} strokeWidth={2} aria-hidden="true" />
      Saved to your history ·{" "}
      <time dateTime={result.completedAt}>
        {formatLongDate(result.completedAt)}, {formatTime(result.completedAt)}
      </time>
    </p>
  ) : (
    !user && (
      <p className="saved-note saved-note-guest reveal" style={{ "--i": 1 } as CSSProperties}>
        <Link to="/login" state={{ from: "/assessment" }}>
          Log in
        </Link>{" "}
        to keep every assessment in your personal health history.
      </p>
    )
  );

  return (
    <ResultsView
      result={result}
      input={assessment}
      eyebrow="Your HealthRater result"
      title="Your personal health report"
      note={note}
      actions={
        <>
          <Link to="/assessment" className="btn btn-ghost" onClick={() => resetAssessment()}>
            <RotateCcw size={16} strokeWidth={2} aria-hidden="true" />
            Retake assessment
          </Link>
          <button type="button" className="btn btn-quiet" onClick={() => saveResult(assessment, result)}>
            <Download size={16} strokeWidth={2} aria-hidden="true" />
            Save data
          </button>
        </>
      }
    />
  );
}
