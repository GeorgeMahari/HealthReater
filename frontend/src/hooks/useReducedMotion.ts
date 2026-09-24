import { useEffect, useState } from "react";

const QUERY = "(prefers-reduced-motion: reduce)";

function getInitial(): boolean {
  return typeof window !== "undefined" && window.matchMedia(QUERY).matches;
}

/** True when the user has asked the OS to minimise motion. */
export function useReducedMotion(): boolean {
  const [reduced, setReduced] = useState(getInitial);

  useEffect(() => {
    const mql = window.matchMedia(QUERY);
    const onChange = () => setReduced(mql.matches);
    mql.addEventListener("change", onChange);
    return () => mql.removeEventListener("change", onChange);
  }, []);

  return reduced;
}
