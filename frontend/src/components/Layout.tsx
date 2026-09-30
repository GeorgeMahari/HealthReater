import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { ShieldCheck } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { HealthBackground, type BackgroundIntensity } from "./visual/HealthBackground";
import { UserMenu } from "./UserMenu";
import { TOTAL_PARAMETER_COUNT } from "../config/parameters";

// The questionnaire gets a calmer backdrop so the form stays the focus.
function intensityFor(pathname: string): BackgroundIntensity {
  if (pathname.startsWith("/assessment")) return "calm";
  if (pathname.startsWith("/results")) return "rich";
  if (pathname.startsWith("/login") || pathname.startsWith("/signup")) return "rich";
  if (pathname.startsWith("/profile")) return "calm";
  if (pathname.startsWith("/history")) return "rich";
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
            <span className="brand-tag">{TOTAL_PARAMETER_COUNT}-parameter health assessment</span>
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
              <UserMenu user={user} onLogout={handleLogout} />
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
