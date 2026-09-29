import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

/**
 * Lets the children render only when the signed-in user's profile has sex and date of
 * birth; otherwise sends them to /complete-profile and back here afterwards. Use inside
 * RequireAuth. The API enforces the same rule (409 on an incomplete profile).
 */
export function RequireCompleteProfile({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  if (user && !user.profileCompleted) {
    return <Navigate to="/complete-profile" replace state={{ from: location.pathname }} />;
  }
  return <>{children}</>;
}
