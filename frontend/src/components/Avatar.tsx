import { useState } from "react";
import { apiUrl } from "../api/http";

interface AvatarProps {
  name: string;
  /** API-relative avatar URL, a blob: preview URL, or null for initials. */
  src: string | null;
  size?: number;
  className?: string;
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const letters = parts.length > 1 ? parts[0][0] + parts[parts.length - 1][0] : (parts[0] ?? "?").slice(0, 2);
  return letters.toUpperCase();
}

/** Round profile picture; falls back to initials when there is no image or it fails to load. */
export function Avatar({ name, src, size = 40, className = "" }: AvatarProps) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const resolved = src && !src.startsWith("blob:") && !src.startsWith("data:") ? apiUrl(src) : src;
  const showImage = resolved && failedSrc !== resolved;

  return (
    <span
      className={`avatar ${className}`}
      style={{ width: size, height: size, fontSize: Math.round(size * 0.38) }}
      aria-hidden="true"
    >
      {showImage ? (
        <img src={resolved} alt="" width={size} height={size} onError={() => setFailedSrc(resolved)} />
      ) : (
        initials(name)
      )}
    </span>
  );
}
