import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

/** Renders children only for a signed-in user; otherwise redirects to /login and comes back afterwards. */
export function RequireAuth({ children, fallback }: { children: ReactNode; fallback?: ReactNode }) {
  const { user, loading, sessionExpired } = useAuth();
  const location = useLocation();

  if (loading) return <>{fallback ?? null}</>;
  if (!user) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname + location.hash, expired: sessionExpired }}
      />
    );
  }
  return <>{children}</>;
}
