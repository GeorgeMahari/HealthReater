export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

/** Turns an API-relative path (e.g. a user's avatarUrl) into an absolute URL. */
export function apiUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

/**
 * An API failure with a user-friendly message. `status` is 0 when the API could not be
 * reached at all. Raw exception text is never surfaced to the UI.
 */
export class ApiError extends Error {
  status: number;
  errors: string[];
  constructor(status: number, errors: string[]) {
    super(errors.join(" "));
    this.status = status;
    this.errors = errors;
  }
  get isUnauthorized() {
    return this.status === 401;
  }
  get isNotFound() {
    return this.status === 404;
  }
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  json?: unknown;
  form?: FormData;
}

const FALLBACK: Record<number, string> = {
  0: "We couldn't reach HealthRater. Check your connection and that the API is running.",
  401: "Your session has expired. Please log in again.",
  403: "You don't have access to this.",
  404: "We couldn't find what you were looking for.",
  413: "That file is too large.",
  429: "Too many attempts. Please wait a minute and try again.",
};

/** Authenticated JSON/multipart request to the HealthRater API (session cookie included). */
export async function apiRequest<T>(path: string, { method = "GET", json, form }: RequestOptions = {}): Promise<T> {
  let response: Response;
  try {
    response = await fetch(apiUrl(path), {
      method,
      credentials: "include",
      headers: json !== undefined ? { "Content-Type": "application/json" } : undefined,
      body: form ?? (json !== undefined ? JSON.stringify(json) : undefined),
    });
  } catch {
    throw new ApiError(0, [FALLBACK[0]]);
  }

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    const errors =
      response.status !== 401 && data?.errors
        ? flattenErrors(data.errors)
        : [FALLBACK[response.status] ?? "Something went wrong on our side. Please try again."];
    throw new ApiError(response.status, errors);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

// ASP.NET's built-in [ApiController] validation returns errors as
// { errors: { FieldName: ["msg1", "msg2"] } } while our own AssessmentValidator
// returns { errors: ["msg1", "msg2"] }. Normalize both shapes to a flat string list.
export function flattenErrors(errors: unknown): string[] {
  if (Array.isArray(errors)) {
    return errors.every((e) => typeof e === "string")
      ? (errors as string[])
      : errors.flatMap((e) => flattenErrors(e));
  }
  if (errors && typeof errors === "object") {
    return Object.values(errors as Record<string, unknown>).flatMap((v) =>
      flattenErrors(v)
    );
  }
  return [String(errors)];
}
