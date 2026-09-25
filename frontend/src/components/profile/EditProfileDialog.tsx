import { useState, type FormEvent } from "react";
import { LoaderCircle } from "lucide-react";
import type { User } from "../../api/authApi";
import { ApiError } from "../../api/http";
import { profileApi } from "../../api/profileApi";
import { Dialog } from "../Dialog";
import { TextField } from "../TextField";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type Errors = Partial<Record<"firstName" | "lastName" | "email", string>>;

interface EditProfileDialogProps {
  open: boolean;
  user: User;
  onClose: () => void;
  onSaved: (user: User) => void;
  onUnauthorized: () => void;
}

/** Name and email editing. Id, creation date and security fields are not editable here. */
export function EditProfileDialog({ open, user, onClose, onSaved, onUnauthorized }: EditProfileDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title="Edit profile" description="Update how your name and email appear in HealthRater.">
      {/* Remount the form each time the dialog opens so it starts from the current values. */}
      {open && <EditProfileForm user={user} onClose={onClose} onSaved={onSaved} onUnauthorized={onUnauthorized} />}
    </Dialog>
  );
}

function EditProfileForm({ user, onClose, onSaved, onUnauthorized }: Omit<EditProfileDialogProps, "open">) {
  const [firstName, setFirstName] = useState(user.firstName);
  const [lastName, setLastName] = useState(user.lastName);
  const [email, setEmail] = useState(user.email);
  const [errors, setErrors] = useState<Errors>({});
  const [serverErrors, setServerErrors] = useState<string[] | null>(null);
  const [saving, setSaving] = useState(false);

  const unchanged =
    firstName.trim() === user.firstName && lastName.trim() === user.lastName && email.trim().toLowerCase() === user.email;

  async function submit(e: FormEvent) {
    e.preventDefault();
    const found: Errors = {};
    if (!firstName.trim()) found.firstName = "Please enter your first name.";
    if (!lastName.trim()) found.lastName = "Please enter your last name.";
    if (!EMAIL_PATTERN.test(email.trim())) found.email = "Please enter a valid email address.";
    setErrors(found);
    setServerErrors(null);
    if (Object.keys(found).length) return;

    setSaving(true);
    try {
      onSaved(await profileApi.update(firstName.trim(), lastName.trim(), email.trim()));
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
      setServerErrors(err instanceof ApiError ? err.errors : ["Something went wrong. Please try again."]);
      setSaving(false);
    }
  }

  return (
    <form className="dialog-form" onSubmit={submit} noValidate>
      <div className="auth-name-row">
        <TextField
          id="profile-first-name"
          label="First name"
          autoComplete="given-name"
          maxLength={80}
          value={firstName}
          onChange={setFirstName}
          error={errors.firstName}
        />
        <TextField
          id="profile-last-name"
          label="Last name"
          autoComplete="family-name"
          maxLength={80}
          value={lastName}
          onChange={setLastName}
          error={errors.lastName}
        />
      </div>
      <TextField
        id="profile-email"
        label="Email"
        type="email"
        autoComplete="email"
        value={email}
        onChange={setEmail}
        error={errors.email}
        hint="You'll use this email to log in."
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
        <button type="submit" className="btn btn-primary" disabled={saving || unchanged}>
          {saving && <LoaderCircle className="spin" size={16} strokeWidth={2} aria-hidden="true" />}
          {saving ? "Saving…" : "Save changes"}
        </button>
      </div>
    </form>
  );
}
