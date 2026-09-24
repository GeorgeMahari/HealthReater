import { useState, type FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { Circle, CircleCheck, Eye, EyeOff, LoaderCircle, LogIn, ShieldCheck, UserPlus } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { AuthApiError } from "../api/authApi";
import { HealthSignalVisual } from "../components/visual/HealthSignalVisual";

type Mode = "login" | "signup";

interface AuthPageProps {
  mode: Mode;
}

// Mirrors the server rules in AuthValidator — the API remains the authority.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const passwordRules = [
  { label: "At least 8 characters", test: (p: string) => p.length >= 8 },
  { label: "Contains a letter and a number", test: (p: string) => /[a-zA-Z]/.test(p) && /\d/.test(p) },
];

type Errors = Partial<Record<"firstName" | "lastName" | "email" | "password" | "confirm", string>>;

export function AuthPage({ mode }: AuthPageProps) {
  const { user, loading, login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<Errors>({});
  const [serverErrors, setServerErrors] = useState<string[] | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const isSignup = mode === "signup";

  // Clear a field's error as soon as the user edits it.
  const edit = (field: keyof Errors, setter: (v: string) => void) => (value: string) => {
    setter(value);
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  if (!loading && user && !submitting) {
    return <Navigate to={from} replace />;
  }

  function validate(): Errors {
    const e: Errors = {};
    if (isSignup && !firstName.trim()) e.firstName = "Please enter your first name.";
    if (isSignup && !lastName.trim()) e.lastName = "Please enter your last name.";
    if (!EMAIL_PATTERN.test(email.trim())) e.email = "Please enter a valid email address.";
    if (!password) e.password = "Please enter your password.";
    else if (isSignup && !passwordRules.every((r) => r.test(password)))
      e.password = "Password doesn't meet the requirements below.";
    if (isSignup && confirm !== password) e.confirm = "Passwords don't match.";
    return e;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const found = validate();
    setErrors(found);
    setServerErrors(null);
    if (Object.keys(found).length > 0) return;

    setSubmitting(true);
    try {
      if (isSignup) await register(firstName.trim(), lastName.trim(), email.trim(), password);
      else await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      setServerErrors(err instanceof AuthApiError ? err.errors : ["Something went wrong. Please try again."]);
      setSubmitting(false);
    }
  }

  const switchState = { state: location.state };

  return (
    <div className="shell auth-page">
      <aside className="auth-aside">
        <HealthSignalVisual className="auth-visual" />
        <p className="eyebrow">
          <span className="status-dot" aria-hidden="true" />
          Your HealthRater account
        </p>
        <h2>Keep your health data in one place.</h2>
        <p>Sign in to pick up where you left off. The assessment itself stays open to everyone.</p>
      </aside>

      <section className="auth-card card step-enter" key={mode} aria-labelledby="auth-title">
        <nav className="auth-tabs" aria-label="Log in or sign up">
          <Link
            to="/login"
            {...switchState}
            aria-current={!isSignup ? "page" : undefined}
            className={`auth-tab ${!isSignup ? "is-active" : ""}`}
          >
            Log in
          </Link>
          <Link
            to="/signup"
            {...switchState}
            aria-current={isSignup ? "page" : undefined}
            className={`auth-tab ${isSignup ? "is-active" : ""}`}
          >
            Sign up
          </Link>
          <span className="auth-tab-indicator" data-side={isSignup ? "right" : "left"} aria-hidden="true" />
        </nav>

        <h1 id="auth-title">{isSignup ? "Create your account" : "Welcome back"}</h1>
        <p className="auth-sub">
          {isSignup
            ? "It takes less than a minute. We only need your name, email and a password."
            : "Log in with the email and password you signed up with."}
        </p>

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          {isSignup && (
            <div className="auth-name-row">
              <AuthField
                id="firstName"
                label="First name"
                error={errors.firstName}
                input={{
                  type: "text",
                  autoComplete: "given-name",
                  value: firstName,
                  onChange: edit("firstName", setFirstName),
                  maxLength: 80,
                }}
              />
              <AuthField
                id="lastName"
                label="Last name"
                error={errors.lastName}
                input={{
                  type: "text",
                  autoComplete: "family-name",
                  value: lastName,
                  onChange: edit("lastName", setLastName),
                  maxLength: 80,
                }}
              />
            </div>
          )}

          <AuthField
            id="email"
            label="Email"
            error={errors.email}
            input={{ type: "email", autoComplete: "email", value: email, onChange: edit("email", setEmail), placeholder: "you@example.com" }}
          />

          <AuthField
            id="password"
            label="Password"
            error={errors.password}
            input={{
              type: showPassword ? "text" : "password",
              autoComplete: isSignup ? "new-password" : "current-password",
              value: password,
              onChange: edit("password", setPassword),
              maxLength: 128,
            }}
            trailing={
              <button
                type="button"
                className="auth-reveal"
                onClick={() => setShowPassword((s) => !s)}
                aria-label={showPassword ? "Hide password" : "Show password"}
                aria-pressed={showPassword}
              >
                {showPassword ? <EyeOff size={17} strokeWidth={1.9} /> : <Eye size={17} strokeWidth={1.9} />}
              </button>
            }
          />

          {isSignup && (
            <>
              <ul className="auth-rules" aria-label="Password requirements">
                {passwordRules.map((rule) => {
                  const ok = rule.test(password);
                  return (
                    <li key={rule.label} className={ok ? "is-ok" : ""}>
                      {ok ? <CircleCheck size={14} strokeWidth={2.2} /> : <Circle size={14} strokeWidth={2} />}
                      {rule.label}
                      <span className="sr-only">{ok ? " (met)" : " (not met)"}</span>
                    </li>
                  );
                })}
              </ul>

              <AuthField
                id="confirm"
                label="Confirm password"
                error={errors.confirm}
                input={{
                  type: showPassword ? "text" : "password",
                  autoComplete: "new-password",
                  value: confirm,
                  onChange: edit("confirm", setConfirm),
                  maxLength: 128,
                }}
              />
            </>
          )}

          {serverErrors && (
            <div className="alert alert-error" role="alert">
              {serverErrors.map((e, i) => (
                <p key={i}>{e}</p>
              ))}
            </div>
          )}

          <button type="submit" className="btn btn-primary auth-submit" disabled={submitting}>
            {submitting ? (
              <LoaderCircle className="spin" size={17} strokeWidth={2} aria-hidden="true" />
            ) : isSignup ? (
              <UserPlus size={17} strokeWidth={2} aria-hidden="true" />
            ) : (
              <LogIn size={17} strokeWidth={2} aria-hidden="true" />
            )}
            {submitting ? (isSignup ? "Creating account…" : "Logging in…") : isSignup ? "Create account" : "Log in"}
          </button>
        </form>

        <p className="auth-switch">
          {isSignup ? "Already have an account? " : "New to HealthRater? "}
          <Link to={isSignup ? "/login" : "/signup"} {...switchState}>
            {isSignup ? "Log in" : "Create an account"}
          </Link>
        </p>

        <p className="auth-note">
          <ShieldCheck size={14} strokeWidth={2} aria-hidden="true" />
          Passwords are stored only as salted hashes, never in plain text.
        </p>
      </section>
    </div>
  );
}

interface AuthFieldProps {
  id: string;
  label: string;
  error?: string;
  trailing?: React.ReactNode;
  input: {
    type: string;
    autoComplete: string;
    value: string;
    onChange: (v: string) => void;
    placeholder?: string;
    maxLength?: number;
  };
}

function AuthField({ id, label, error, trailing, input }: AuthFieldProps) {
  const errorId = `${id}-error`;
  return (
    <div className={`field ${error ? "field-error" : ""}`}>
      <label className="field-label" htmlFor={id}>
        {label}
      </label>
      <div className={`field-control ${trailing ? "has-trailing" : ""}`}>
        <input
          id={id}
          name={id}
          className="input"
          type={input.type}
          autoComplete={input.autoComplete}
          value={input.value}
          placeholder={input.placeholder}
          maxLength={input.maxLength}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          onChange={(e) => input.onChange(e.target.value)}
        />
        {trailing}
      </div>
      {error && (
        <p className="field-error-text" id={errorId} role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
