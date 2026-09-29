import { flattenErrors } from "./http";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  /** "First Last", for display. */
  name: string;
  email: string;
  /** UTC ISO timestamp. */
  createdAt: string;
  /** API-relative, versioned URL of the user's own avatar, or null. */
  avatarUrl: string | null;
  /** Profile context used for scoring. */
  sex: "Male" | "Female" | null;
  /** "yyyy-MM-dd"; the source of truth for age. */
  dateOfBirth: string | null;
  /** Current age, calculated by the API from dateOfBirth. */
  age: number | null;
  heightCm: number | null;
  weightKg: number | null;
  /** Sex, date of birth, height and weight are all set; required before an assessment can start. */
  profileCompleted: boolean;
}

export class AuthApiError extends Error {
  errors: string[];
  status: number;
  constructor(errors: string[], status: number) {
    super(errors.join(" "));
    this.errors = errors;
    this.status = status;
  }
}

// The session lives in an HttpOnly cookie set by the API, so every call must send credentials.
async function request<T>(path: string, body?: unknown): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api/auth/${path}`, {
      method: body === undefined && path === "me" ? "GET" : "POST",
      credentials: "include",
      headers: body === undefined ? undefined : { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new AuthApiError(["Could not reach the HealthRater API. Is the backend running?"], 0);
  }

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    const errors = data?.errors
      ? flattenErrors(data.errors)
      : response.status === 429
        ? ["Too many attempts. Please wait a minute and try again."]
        : ["Something went wrong. Please try again."];
    throw new AuthApiError(errors, response.status);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const authApi = {
  /** Resolves to null when nobody is signed in (API answers 204). */
  me: async () => (await request<User | undefined>("me")) ?? null,
  login: (email: string, password: string) => request<User>("login", { email, password }),
  register: (firstName: string, lastName: string, email: string, password: string) =>
    request<User>("register", { firstName, lastName, email, password }),
  logout: () => request<void>("logout", {}),
};
