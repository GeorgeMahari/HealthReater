import { useEffect, useRef, useState } from "react";
import { ImageUp, LoaderCircle, Trash } from "lucide-react";
import type { User } from "../../api/authApi";
import { ApiError } from "../../api/http";
import { profileApi } from "../../api/profileApi";
import { Avatar } from "../Avatar";
import { Dialog } from "../Dialog";
import { AVATAR_TYPES, prepareAvatar } from "./avatarImage";

interface AvatarDialogProps {
  open: boolean;
  user: User;
  onClose: () => void;
  onSaved: (user: User, message: string) => void;
  onUnauthorized: () => void;
}

export function AvatarDialog({ open, user, onClose, onSaved, onUnauthorized }: AvatarDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Profile photo"
      description="JPG, PNG or WEBP. We crop it to a square and remove any embedded photo metadata."
    >
      {open && <AvatarForm user={user} onClose={onClose} onSaved={onSaved} onUnauthorized={onUnauthorized} />}
    </Dialog>
  );
}

function AvatarForm({ user, onClose, onSaved, onUnauthorized }: Omit<AvatarDialogProps, "open">) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<{ blob: Blob; url: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<"preparing" | "saving" | "removing" | null>(null);

  // Release the preview's object URL when it's replaced or the dialog closes.
  useEffect(() => () => {
    if (preview) URL.revokeObjectURL(preview.url);
  }, [preview]);

  async function choose(file: File | undefined) {
    if (!file) return;
    setError(null);
    setBusy("preparing");
    try {
      const blob = await prepareAvatar(file);
      setPreview({ blob, url: URL.createObjectURL(blob) });
    } catch (err) {
      setError(err instanceof Error ? err.message : "That image couldn't be used.");
    } finally {
      setBusy(null);
      if (fileRef.current) fileRef.current.value = "";
    }
  }

  async function run(action: "saving" | "removing") {
    setError(null);
    setBusy(action);
    try {
      const updated =
        action === "saving" && preview ? await profileApi.uploadAvatar(preview.blob) : await profileApi.removeAvatar();
      onSaved(updated, action === "saving" ? "Profile photo updated." : "Profile photo removed.");
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) return onUnauthorized();
      setError(err instanceof ApiError ? err.message : "Something went wrong. Please try again.");
      setBusy(null);
    }
  }

  return (
    <div className="dialog-form">
      <div className="avatar-editor">
        <Avatar name={user.name} src={preview?.url ?? user.avatarUrl} size={128} className="avatar-xl" />
        <div className="avatar-editor-side">
          <p className="avatar-editor-label">{preview ? "New photo preview" : user.avatarUrl ? "Current photo" : "No photo yet"}</p>
          <input
            ref={fileRef}
            type="file"
            accept={AVATAR_TYPES.join(",")}
            className="sr-only"
            id="avatar-file"
            onChange={(e) => choose(e.target.files?.[0])}
          />
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => fileRef.current?.click()}
            disabled={busy !== null}
          >
            {busy === "preparing" ? (
              <LoaderCircle className="spin" size={15} strokeWidth={2} aria-hidden="true" />
            ) : (
              <ImageUp size={15} strokeWidth={2} aria-hidden="true" />
            )}
            {preview || user.avatarUrl ? "Choose another image" : "Upload image"}
          </button>
          {user.avatarUrl && !preview && (
            <button type="button" className="btn btn-quiet btn-sm btn-danger-text" onClick={() => run("removing")} disabled={busy !== null}>
              <Trash size={15} strokeWidth={2} aria-hidden="true" />
              {busy === "removing" ? "Removing…" : "Remove photo"}
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="alert alert-error" role="alert">
          <p>{error}</p>
        </div>
      )}

      <div className="dialog-actions">
        <button type="button" className="btn btn-quiet" onClick={onClose} disabled={busy === "saving"}>
          Cancel
        </button>
        <button type="button" className="btn btn-primary" onClick={() => run("saving")} disabled={!preview || busy !== null}>
          {busy === "saving" && <LoaderCircle className="spin" size={16} strokeWidth={2} aria-hidden="true" />}
          {busy === "saving" ? "Saving…" : "Save photo"}
        </button>
      </div>
    </div>
  );
}
