import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { authApi, type User } from "../api/authApi";

interface AuthContextValue {
  user: User | null;
  /** True until the initial session check (GET /api/auth/me) has finished. */
  loading: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (firstName: string, lastName: string, email: string, password: string) => Promise<User>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

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
      login: async (email, password) => {
        const u = await authApi.login(email, password);
        setUser(u);
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
    }),
    [user, loading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
