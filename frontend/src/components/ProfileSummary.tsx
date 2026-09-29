import { Link } from "react-router-dom";
import { CalendarDays, Pencil, Ruler, UserRound, Weight } from "lucide-react";
import type { User } from "../api/authApi";

/**
 * Read-only "Basic Information" step of the assessment: the profile data (sex, age, height,
 * weight) that will be used for this assessment. The user confirms it or edits the profile —
 * it is never re-entered here.
 */
export function ProfileSummary({ user }: { user: User }) {
  const items = [
    { label: "Sex", value: user.sex ?? "—", icon: UserRound },
    { label: "Age", value: user.age !== null ? `${user.age} years` : "—", icon: CalendarDays },
    { label: "Height", value: user.heightCm !== null ? `${user.heightCm} cm` : "—", icon: Ruler },
    { label: "Weight", value: user.weightKg !== null ? `${user.weightKg} kg` : "—", icon: Weight },
  ];

  return (
    <div className="profile-summary">
      <dl className="profile-summary-grid">
        {items.map(({ label, value, icon: Icon }, i) => (
          <div key={label} className="profile-summary-item" style={{ animationDelay: `${i * 60}ms` }}>
            <span className="profile-summary-icon" aria-hidden="true">
              <Icon size={17} strokeWidth={1.9} />
            </span>
            <dt>{label}</dt>
            <dd>{value}</dd>
          </div>
        ))}
      </dl>
      <div className="profile-summary-foot">
        <p>
          These details come from your profile and are used automatically. Check that they're still up to date —
          especially your weight — then confirm to continue.
        </p>
        <Link to="/profile" state={{ edit: true }} className="btn btn-ghost btn-sm">
          <Pencil size={14} strokeWidth={2} aria-hidden="true" />
          Edit Profile
        </Link>
      </div>
    </div>
  );
}
