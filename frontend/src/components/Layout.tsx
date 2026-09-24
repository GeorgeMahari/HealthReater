import { Link, Outlet, useLocation } from "react-router-dom";
import { HealthBackground, type BackgroundIntensity } from "./visual/HealthBackground";

// The questionnaire gets a calmer backdrop so the form stays the focus.
function intensityFor(pathname: string): BackgroundIntensity {
  if (pathname.startsWith("/assessment")) return "calm";
  if (pathname.startsWith("/results")) return "rich";
  return "full";
}

export function Layout() {
  const location = useLocation();

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
          </nav>
        </div>
      </header>

      <main className="page-transition" key={location.pathname}>
        <Outlet />
      </main>
    </div>
  );
}
