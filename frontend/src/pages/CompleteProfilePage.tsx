import { useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { LoaderCircle, ShieldCheck, UserRoundCheck } from "lucide-react";
import { ApiError } from "../api/http";
import { profileApi } from "../api/profileApi";
import { useAuth } from "../context/AuthContext";
import { ProfileContextFields } from "../components/profile/ProfileContextFields";
import { validateProfileContext, type SexValue } from "../utils/profileContext";

/**
 * Asked once, before the first assessment: sex and date of birth. Afterwards they are
 * used automatically for every assessment and can be changed on the profile page.
 */
export function CompleteProfilePage() {
  const { user, updateUser, expireSession } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/assessment";

  const [sex, setSex] = useState<SexValue>(user?.sex ?? "");
  const [dateOfBirth, setDateOfBirth] = useState(user?.dateOfBirth ?? "");
  const [errors, setErrors] = useState<{ sex?: string; dateOfBirth?: string }>({});
  const [serverErrors, setServerErrors] = useState<string[] | null>(null);
  const [saving, setSaving] = useState(false);

  if (!user) return null;

  async function submit(e: FormEvent) {
    e.preventDefault();
    const found = validateProfileContext(sex, dateOfBirth);
    setErrors(found);
    setServerErrors(null);
    if (Object.keys(found).length || !user) return;

    setSaving(true);
    try {
      const updated = await profileApi.update(user.firstName, user.lastName, user.email, {
        sex: sex as "Male" | "Female",
        dateOfBirth,
      });
      updateUser(updated);
      navigate(from, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return expireSession();
      setServerErrors(err instanceof ApiError ? err.errors : ["Something went wrong. Please try again."]);
      setSaving(false);
    }
  }

  return (
    <div className="shell complete-profile-page">
      <section className="auth-card card step-enter" aria-labelledby="complete-title">
        <span className="complete-icon" aria-hidden="true">
          <UserRoundCheck size={22} strokeWidth={1.9} />
        </span>
        <p className="eyebrow">One-time setup</p>
        <h1 id="complete-title">Complete your profile</h1>
        <p className="auth-sub">
          Hi {user.firstName}! HealthRater needs your sex and date of birth to score your assessment — for example,
          body-fat and fitness references differ by sex and age. You'll only be asked once.
        </p>

        <form className="auth-form" onSubmit={submit} noValidate>
          <ProfileContextFields
            idPrefix="complete"
            sex={sex}
            dateOfBirth={dateOfBirth}
            onSexChange={(v) => {
              setSex(v);
              setErrors((x) => ({ ...x, sex: undefined }));
            }}
            onDateOfBirthChange={(v) => {
              setDateOfBirth(v);
              setErrors((x) => ({ ...x, dateOfBirth: undefined }));
            }}
            errors={errors}
          />

          {serverErrors && (
            <div className="alert alert-error" role="alert">
              {serverErrors.map((m) => (
                <p key={m}>{m}</p>
              ))}
            </div>
          )}

          <button type="submit" className="btn btn-primary auth-submit" disabled={saving}>
            {saving && <LoaderCircle className="spin" size={17} strokeWidth={2} aria-hidden="true" />}
            {saving ? "Saving…" : "Save and continue"}
          </button>
        </form>

        <p className="auth-note">
          <ShieldCheck size={14} strokeWidth={2} aria-hidden="true" />
          Your age is calculated from your date of birth. You can change both later in your profile; past
          assessments keep the values recorded at the time.
        </p>
      </section>
    </div>
  );
}
