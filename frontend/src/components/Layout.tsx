import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { LogOut, ShieldCheck } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { HealthBackground, type BackgroundIntensity } from "./visual/HealthBackground";

// The questionnaire gets a calmer backdrop so the form stays the focus.
function intensityFor(pathname: string): BackgroundIntensity {
  if (pathname.startsWith("/assessment")) return "calm";
  if (pathname.startsWith("/results")) return "rich";
  if (pathname.startsWith("/login") || pathname.startsWith("/signup")) return "rich";
  return "full";
}

export function Layout() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, loading, logout } = useAuth();
  const onAuthPage = location.pathname === "/login" || location.pathname === "/signup";

  async function handleLogout() {
    await logout();
    navigate("/");
  }

  return (
    <div className="app">
      <HealthBackground intensity={intensityFor(location.pathname)} />

      <header className="topbar">
        <div className="topbar-inner">
          <Link to="/" className="brand" aria-label="HealthRater home">
            <svg className="brand-logo" viewBox="0 0 32 32" aria-hidden="true">
              <circle cx="16" cy="16" r="14" />
              <path d="M6 17h5l2.5-5 4 10 2.5-5H26" />
            </svg>
            <span className="brand-mark">HealthRater</span>
            <span className="brand-tag">39-parameter health assessment</span>
          </Link>
          <nav className="nav-links" aria-label="Main">
            <Link
              to="/assessment"
              className={location.pathname === "/assessment" ? "nav-link active" : "nav-link"}
              aria-current={location.pathname === "/assessment" ? "page" : undefined}
            >
              Assessment
            </Link>

            {!loading && user && (
              <div className="nav-user">
                <span className="nav-avatar" aria-hidden="true">
                  {initials(user.name)}
                </span>
                <span className="nav-user-name">{user.name}</span>
                <button type="button" className="nav-link nav-logout" onClick={handleLogout}>
                  <LogOut size={15} strokeWidth={2} aria-hidden="true" />
                  Log out
                </button>
              </div>
            )}

            {!loading && !user && !onAuthPage && (
              <>
                <Link to="/login" state={{ from: location.pathname }} className="nav-link">
                  Log in
                </Link>
                <Link to="/signup" state={{ from: location.pathname }} className="btn btn-primary nav-cta">
                  Sign up
                </Link>
              </>
            )}
          </nav>
        </div>
      </header>

      <main className="page-transition" key={location.pathname}>
        <Outlet />
      </main>

      <footer className="site-footer">
        <p className="disclaimer">
          <ShieldCheck className="disclaimer-icon" size={16} strokeWidth={2} aria-hidden="true" />
          <span>
            HealthRater is for reflection and education, not diagnosis or medical advice. If something
            concerns you, speak with a qualified health professional.
          </span>
        </p>
      </footer>
    </div>
  );
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const letters = parts.length > 1 ? parts[0][0] + parts[parts.length - 1][0] : (parts[0] ?? "?").slice(0, 2);
  return letters.toUpperCase();
}
