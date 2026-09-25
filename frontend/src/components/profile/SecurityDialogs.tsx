import { useState, type FormEvent } from "react";
import { Circle, CircleCheck, LoaderCircle, TriangleAlert } from "lucide-react";
import { ApiError } from "../../api/http";
import { profileApi } from "../../api/profileApi";
import { Dialog } from "../Dialog";
import { TextField } from "../TextField";

const passwordRules = [
  { label: "At least 8 characters", test: (p: string) => p.length >= 8 },
  { label: "Contains a letter and a number", test: (p: string) => /[a-zA-Z]/.test(p) && /\d/.test(p) },
];

interface BaseProps {
  open: boolean;
  onClose: () => void;
  onUnauthorized: () => void;
}

export function ChangePasswordDialog({ open, onClose, onUnauthorized, onChanged }: BaseProps & { onChanged: () => void }) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Change password"
      description="You'll stay signed in here; every other device will be signed out."
    >
      {open && <ChangePasswordForm onClose={onClose} onUnauthorized={onUnauthorized} onChanged={onChanged} />}
    </Dialog>
  );
}

function ChangePasswordForm({ onClose, onUnauthorized, onChanged }: Omit<BaseProps, "open"> & { onChanged: () => void }) {
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [errors, setErrors] = useState<Partial<Record<"current" | "next" | "confirm", string>>>({});
  const [serverErrors, setServerErrors] = useState<string[] | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    const found: typeof errors = {};
    if (!current) found.current = "Please enter your current password.";
    if (!passwordRules.every((r) => r.test(next))) found.next = "The new password doesn't meet the requirements.";
    if (confirm !== next) found.confirm = "Passwords don't match.";
    setErrors(found);
    setServerErrors(null);
    if (Object.keys(found).length) return;

    setSaving(true);
    try {
      await profileApi.changePassword(current, next);
      onChanged();
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
      setServerErrors(err instanceof ApiError ? err.errors : ["Something went wrong. Please try again."]);
      setSaving(false);
    }
  }

  return (
    <form className="dialog-form" onSubmit={submit} noValidate>
      <TextField
        id="current-password"
        label="Current password"
        type="password"
        autoComplete="current-password"
        value={current}
        onChange={setCurrent}
        error={errors.current}
      />
      <TextField
        id="new-password"
        label="New password"
        type="password"
        autoComplete="new-password"
        maxLength={128}
        value={next}
        onChange={setNext}
        error={errors.next}
      />
      <ul className="auth-rules" aria-label="Password requirements">
        {passwordRules.map((rule) => {
          const ok = rule.test(next);
          return (
            <li key={rule.label} className={ok ? "is-ok" : ""}>
              {ok ? <CircleCheck size={14} strokeWidth={2.2} aria-hidden="true" /> : <Circle size={14} strokeWidth={2} aria-hidden="true" />}
              {rule.label}
              <span className="sr-only">{ok ? " (met)" : " (not met)"}</span>
            </li>
          );
        })}
      </ul>
      <TextField
        id="confirm-new-password"
        label="Confirm new password"
        type="password"
        autoComplete="new-password"
        maxLength={128}
        value={confirm}
        onChange={setConfirm}
        error={errors.confirm}
      />

      {serverErrors && (
        <div className="alert alert-error" role="alert">
          {serverErrors.map((m) => (
            <p key={m}>{m}</p>
          ))}
        </div>
      )}

      <div className="dialog-actions">
        <button type="button" className="btn btn-quiet" onClick={onClose} disabled={saving}>
          Cancel
        </button>
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving && <LoaderCircle className="spin" size={16} strokeWidth={2} aria-hidden="true" />}
          {saving ? "Updating…" : "Update password"}
        </button>
      </div>
    </form>
  );
}

const CONFIRM_WORD = "DELETE";

export function DeleteAccountDialog({ open, onClose, onUnauthorized, onDeleted }: BaseProps & { onDeleted: () => void }) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      tone="danger"
      title="Delete account"
      description={
        <div className="danger-note">
          <TriangleAlert size={18} strokeWidth={2} aria-hidden="true" />
          <p>
            Deleting your account will permanently remove your HealthRater assessment history, your profile photo and
            your login. This cannot be undone.
          </p>
        </div>
      }
    >
      {open && <DeleteAccountForm onClose={onClose} onUnauthorized={onUnauthorized} onDeleted={onDeleted} />}
    </Dialog>
  );
}

function DeleteAccountForm({ onClose, onUnauthorized, onDeleted }: Omit<BaseProps, "open"> & { onDeleted: () => void }) {
  const [word, setWord] = useState("");
  const [password, setPassword] = useState("");
  const [serverErrors, setServerErrors] = useState<string[] | null>(null);
  const [deleting, setDeleting] = useState(false);
  const confirmed = word === CONFIRM_WORD && password.length > 0;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!confirmed) return;
    setServerErrors(null);
    setDeleting(true);
    try {
      await profileApi.deleteAccount(password);
      onDeleted();
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
      setServerErrors(err instanceof ApiError ? err.errors : ["Something went wrong. Please try again."]);
      setDeleting(false);
    }
  }

  return (
    <form className="dialog-form" onSubmit={submit} noValidate>
      <TextField
        id="delete-confirm-word"
        label={`Type ${CONFIRM_WORD} to confirm`}
        autoComplete="off"
        value={word}
        onChange={setWord}
      />
      <TextField
        id="delete-password"
        label="Your password"
        type="password"
        autoComplete="current-password"
        value={password}
        onChange={setPassword}
      />

      {serverErrors && (
        <div className="alert alert-error" role="alert">
          {serverErrors.map((m) => (
            <p key={m}>{m}</p>
          ))}
        </div>
      )}

      <div className="dialog-actions">
        <button type="button" className="btn btn-quiet" onClick={onClose} disabled={deleting}>
          Keep my account
        </button>
        <button type="submit" className="btn btn-danger" disabled={!confirmed || deleting}>
          {deleting && <LoaderCircle className="spin" size={16} strokeWidth={2} aria-hidden="true" />}
          {deleting ? "Deleting…" : "Permanently delete"}
        </button>
      </div>
    </form>
  );
}
