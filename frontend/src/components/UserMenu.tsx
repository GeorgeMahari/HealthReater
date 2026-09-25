import { useEffect, useId, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { CalendarDays, ChevronDown, LogOut, Settings, UserRound } from "lucide-react";
import type { User } from "../api/authApi";
import { Avatar } from "./Avatar";

interface UserMenuProps {
  user: User;
  onLogout: () => void;
}

/** Avatar button in the top bar that opens the account menu. */
export function UserMenu({ user, onLogout }: UserMenuProps) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuId = useId();

  useEffect(() => {
    if (!open) return;
    const onDown = (e: PointerEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setOpen(false);
        buttonRef.current?.focus();
      }
    };
    document.addEventListener("pointerdown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("pointerdown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const close = () => setOpen(false);

  return (
    <div className={`user-menu ${open ? "is-open" : ""}`} ref={rootRef}>
      <button
        ref={buttonRef}
        type="button"
        className="user-menu-trigger"
        aria-haspopup="true"
        aria-expanded={open}
        aria-controls={menuId}
        aria-label={`Account menu for ${user.name}`}
        onClick={() => setOpen((o) => !o)}
      >
        <Avatar name={user.name} src={user.avatarUrl} size={30} />
        <span className="nav-user-name">{user.name}</span>
        <ChevronDown className="user-menu-chevron" size={15} strokeWidth={2} aria-hidden="true" />
      </button>

      {open && (
        <div className="user-menu-panel" id={menuId}>
          <div className="user-menu-head">
            <strong>{user.name}</strong>
            <span>{user.email}</span>
          </div>
          <Link to="/profile" className="user-menu-item" onClick={close}>
            <UserRound size={16} strokeWidth={1.9} aria-hidden="true" />
            Profile
          </Link>
          <Link to="/profile#history" className="user-menu-item" onClick={close}>
            <CalendarDays size={16} strokeWidth={1.9} aria-hidden="true" />
            Health History
          </Link>
          <Link to="/profile#settings" className="user-menu-item" onClick={close}>
            <Settings size={16} strokeWidth={1.9} aria-hidden="true" />
            Account Settings
          </Link>
          <hr />
          <button
            type="button"
            className="user-menu-item"
            onClick={() => {
              close();
              onLogout();
            }}
          >
            <LogOut size={16} strokeWidth={1.9} aria-hidden="true" />
            Log out
          </button>
        </div>
      )}
    </div>
  );
}
