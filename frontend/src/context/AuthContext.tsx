import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { authApi, type User } from "../api/authApi";

interface AuthContextValue {
  user: User | null;
  /** True until the initial session check (GET /api/auth/me) has finished. */
  loading: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (firstName: string, lastName: string, email: string, password: string) => Promise<User>;
  logout: () => Promise<void>;
  /** Replace the cached user after a profile/avatar change. */
  updateUser: (user: User) => void;
  /** Call when an API request answers 401: clears the user so protected pages redirect to login. */
  expireSession: () => void;
  /** Clears local auth state without calling the API (e.g. after the account was deleted). */
  clearUser: () => void;
  /** True after the session expired, until the next successful login. */
  sessionExpired: boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [sessionExpired, setSessionExpired] = useState(false);

  useEffect(() => {
    let cancelled = false;
    authApi
      .me()
      .then((u) => !cancelled && setUser(u))
      .catch(() => !cancelled && setUser(null))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      sessionExpired,
      login: async (email, password) => {
        const u = await authApi.login(email, password);
        setUser(u);
        setSessionExpired(false);
        return u;
      },
      register: async (firstName, lastName, email, password) => {
        const u = await authApi.register(firstName, lastName, email, password);
        setUser(u);
        return u;
      },
      logout: async () => {
        try {
          await authApi.logout();
        } finally {
          setUser(null);
        }
      },
      updateUser: (u) => setUser(u),
      expireSession: () => {
        setUser(null);
        setSessionExpired(true);
      },
      clearUser: () => setUser(null),
    }),
    [user, loading, sessionExpired]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
