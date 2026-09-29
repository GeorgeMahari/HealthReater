import { useEffect, useState, type CSSProperties } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { CalendarDays, Camera, KeyRound, LogOut, Mail, Pencil, Trash, TriangleAlert } from "lucide-react";
import type { User } from "../api/authApi";
import { assessmentsApi } from "../api/assessmentsApi";
import { ApiError } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { useAssessment } from "../context/AssessmentContext";
import { Avatar } from "../components/Avatar";
import { ErrorState, Skeleton } from "../components/StatusViews";
import { HealthSignalVisual } from "../components/visual/HealthSignalVisual";
import { AvatarDialog } from "../components/profile/AvatarDialog";
import { CompareAssessments } from "../components/profile/CompareAssessments";
import { EditProfileDialog } from "../components/profile/EditProfileDialog";
import { HealthCalendar } from "../components/profile/HealthCalendar";
import { HistoryList } from "../components/profile/HistoryList";
import { ScoreHistory } from "../components/profile/ScoreHistory";
import { ChangePasswordDialog, DeleteAccountDialog } from "../components/profile/SecurityDialogs";
import type { AssessmentSummary } from "../types";
import { formatLongDate, formatMonthYear } from "../utils/dates";

type HistoryState =
  | { status: "loading" }
  | { status: "ready"; summaries: AssessmentSummary[] }
  | { status: "error"; message: string };

type OpenDialog = "edit" | "avatar" | "password" | "delete" | null;

/** Profile, account settings and the signed-in user's full assessment history. Rendered inside RequireAuth. */
export function ProfilePage() {
  const { user, updateUser, expireSession, logout, clearUser } = useAuth();
  const { resetAssessment } = useAssessment();
  const navigate = useNavigate();
  const location = useLocation();
  const [history, setHistory] = useState<HistoryState>({ status: "loading" });
  // "Edit Profile" on the assessment page links here with { edit: true } to open the editor.
  const [dialog, setDialog] = useState<OpenDialog>(() =>
    (location.state as { edit?: boolean } | null)?.edit ? "edit" : null
  );
  const [notice, setNotice] = useState<string | null>(null);

  const [reload, setReload] = useState(0);

  useEffect(() => {
    let ignore = false;
    assessmentsApi
      .list()
      .then((summaries) => {
        if (!ignore) setHistory({ status: "ready", summaries });
      })
      .catch((err) => {
        if (ignore) return;
        if (err instanceof ApiError && err.isUnauthorized) return expireSession();
        setHistory({ status: "error", message: err instanceof ApiError ? err.message : "Please try again." });
      });
    return () => {
      ignore = true;
    };
  }, [reload, expireSession]);

  // Honour #history / #settings links from the account menu once the page has content.
  useEffect(() => {
    if (!location.hash || history.status === "loading") return;
    document.getElementById(location.hash.slice(1))?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [location.hash, history.status]);

  useEffect(() => {
    if (!notice) return;
    const t = window.setTimeout(() => setNotice(null), 4500);
    return () => window.clearTimeout(t);
  }, [notice]);

  if (!user) return null;

  const closeDialog = () => setDialog(null);
  const saved = (u: User, message: string) => {
    updateUser(u);
    setDialog(null);
    setNotice(message);
  };

  const summaries = history.status === "ready" ? history.summaries : [];
  const latest = summaries[0] ? new Date(summaries[0].completedAt) : new Date();

  return (
    <div className="shell profile-page">
      <div className="profile-notice" role="status" aria-live="polite">
        {notice && <p className="saved-note">{notice}</p>}
      </div>

      <div className="profile-top">
        <section className="profile-card profile-id reveal" aria-labelledby="profile-name">
          <div className="profile-avatar-wrap">
            <Avatar name={user.name} src={user.avatarUrl} size={104} className="avatar-xl" />
            <button type="button" className="avatar-edit" onClick={() => setDialog("avatar")} aria-label="Change profile photo">
              <Camera size={16} strokeWidth={2} />
            </button>
          </div>
          <h1 id="profile-name">{user.name}</h1>
          <p className="profile-email">
            <Mail size={14} strokeWidth={2} aria-hidden="true" />
            {user.email}
          </p>
          <p className="profile-since">
            <CalendarDays size={14} strokeWidth={2} aria-hidden="true" />
            Member since {formatMonthYear(new Date(user.createdAt).getFullYear(), new Date(user.createdAt).getMonth() + 1)}
          </p>

          {user.profileCompleted ? (
            <dl className="profile-facts" aria-label="Profile used for scoring">
              <div>
                <dt>Sex</dt>
                <dd>{user.sex}</dd>
              </div>
              <div>
                <dt>Date of birth</dt>
                <dd>{formatLongDate(`${user.dateOfBirth}T12:00:00`)}</dd>
              </div>
              <div>
                <dt>Age</dt>
                <dd>{user.age}</dd>
              </div>
            </dl>
          ) : (
            <div className="profile-incomplete" role="status">
              <p>Add your sex and date of birth to start assessments.</p>
              <Link to="/complete-profile" state={{ from: "/profile" }} className="btn btn-primary btn-sm">
                Complete profile
              </Link>
            </div>
          )}

          <div className="profile-stats">
            <div>
              <strong>{history.status === "ready" ? summaries.length : "–"}</strong>
              <span>assessments</span>
            </div>
            <div>
              <strong>{summaries[0] ? summaries[0].totalHealthRating : "–"}</strong>
              <span>latest rating</span>
            </div>
          </div>

          <button type="button" className="btn btn-ghost profile-edit" onClick={() => setDialog("edit")}>
            <Pencil size={15} strokeWidth={2} aria-hidden="true" />
            Edit Profile
          </button>
        </section>

        <section className="profile-card calendar-card reveal" style={{ "--i": 1 } as CSSProperties} aria-label="Assessment calendar">
          {history.status === "loading" ? (
            <CalendarSkeleton />
          ) : (
            <HealthCalendar
              initialYear={latest.getFullYear()}
              initialMonth={latest.getMonth() + 1}
              onUnauthorized={expireSession}
            />
          )}
        </section>
      </div>

      <section id="history" className="profile-section" aria-labelledby="history-title">
        <div className="section-head">
          <p className="eyebrow">Your record</p>
          <h2 id="history-title">Health History</h2>
        </div>

        {history.status === "loading" && <HistorySkeleton />}
        {history.status === "error" && (
          <ErrorState
            title="We couldn't load your history"
            message={history.message}
            onRetry={() => {
              setHistory({ status: "loading" });
              setReload((r) => r + 1);
            }}
          />
        )}
        {history.status === "ready" && summaries.length === 0 && (
          <div className="profile-card empty-history">
            <HealthSignalVisual className="empty-visual" />
            <h3>No HealthRater assessments yet.</h3>
            <p>Complete your first assessment to start building your health history.</p>
            <Link to="/assessment" className="btn btn-primary" onClick={() => resetAssessment()}>
              Start Assessment
            </Link>
          </div>
        )}
        {history.status === "ready" && summaries.length > 0 && (
          <div className="history-stack">
            {summaries.length >= 2 && <ScoreHistory summaries={summaries} />}
            <HistoryList summaries={summaries} />
            {summaries.length >= 2 && <CompareAssessments summaries={summaries} onUnauthorized={expireSession} />}
          </div>
        )}
      </section>

      <section id="settings" className="profile-section" aria-labelledby="settings-title">
        <div className="section-head">
          <p className="eyebrow">Security</p>
          <h2 id="settings-title">Account Settings</h2>
        </div>
        <div className="profile-card settings-list">
          <SettingRow icon={<Pencil size={17} strokeWidth={1.9} />} title="Profile details" text="Your name and login email.">
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => setDialog("edit")}>
              Edit
            </button>
          </SettingRow>
          <SettingRow icon={<Camera size={17} strokeWidth={1.9} />} title="Profile photo" text="JPG, PNG or WEBP, up to 10 MB.">
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => setDialog("avatar")}>
              {user.avatarUrl ? "Change" : "Add photo"}
            </button>
          </SettingRow>
          <SettingRow icon={<KeyRound size={17} strokeWidth={1.9} />} title="Password" text="Changing it signs out your other devices.">
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => setDialog("password")}>
              Change password
            </button>
          </SettingRow>
          <SettingRow icon={<LogOut size={17} strokeWidth={1.9} />} title="Log out" text="End your session on this device.">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={async () => {
                await logout();
                navigate("/");
              }}
            >
              Log out
            </button>
          </SettingRow>
          <SettingRow
            danger
            icon={<TriangleAlert size={17} strokeWidth={1.9} />}
            title="Delete account"
            text="Permanently removes your account and your entire assessment history."
          >
            <button type="button" className="btn btn-danger-outline btn-sm" onClick={() => setDialog("delete")}>
              <Trash size={15} strokeWidth={2} aria-hidden="true" />
              Delete account
            </button>
          </SettingRow>
        </div>
      </section>

      <EditProfileDialog
        open={dialog === "edit"}
        user={user}
        onClose={closeDialog}
        onSaved={(u) => saved(u, "Profile updated.")}
        onUnauthorized={expireSession}
      />
      <AvatarDialog open={dialog === "avatar"} user={user} onClose={closeDialog} onSaved={saved} onUnauthorized={expireSession} />
      <ChangePasswordDialog
        open={dialog === "password"}
        onClose={closeDialog}
        onUnauthorized={expireSession}
        onChanged={() => {
          setDialog(null);
          setNotice("Password updated. Your other devices have been signed out.");
        }}
      />
      <DeleteAccountDialog
        open={dialog === "delete"}
        onClose={closeDialog}
        onUnauthorized={expireSession}
        onDeleted={() => {
          clearUser();
          navigate("/", { replace: true });
        }}
      />
    </div>
  );
}

function SettingRow({ icon, title, text, children, danger = false }: {
  icon: React.ReactNode;
  title: string;
  text: string;
  children: React.ReactNode;
  danger?: boolean;
}) {
  return (
    <div className={`setting-row ${danger ? "is-danger" : ""}`}>
      <span className="setting-icon" aria-hidden="true">
        {icon}
      </span>
      <div className="setting-text">
        <strong>{title}</strong>
        <span>{text}</span>
      </div>
      {children}
    </div>
  );
}

function CalendarSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading calendar">
      <Skeleton width={160} height={20} className="sk-center" />
      <div className="calendar-grid sk-grid">
        {Array.from({ length: 35 }, (_, i) => (
          <Skeleton key={i} height={44} radius={10} />
        ))}
      </div>
    </div>
  );
}

function HistorySkeleton() {
  return (
    <div className="history-stack" aria-busy="true" aria-label="Loading history">
      <Skeleton height={300} radius={20} />
      <Skeleton height={220} radius={20} />
    </div>
  );
}

/** Loading placeholder for the whole page while the session check runs. */
export function ProfileSkeleton() {
  return (
    <div className="shell profile-page" aria-busy="true" aria-label="Loading profile">
      <div className="profile-top">
        <div className="profile-card profile-id">
          <Skeleton width={104} height={104} radius={999} />
          <Skeleton width={180} height={24} className="sk-gap" />
          <Skeleton width={220} height={14} className="sk-gap" />
        </div>
        <div className="profile-card calendar-card">
          <CalendarSkeleton />
        </div>
      </div>
    </div>
  );
}
