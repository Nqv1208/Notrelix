import { useEffect, useState } from "react";

/** The location hash as a path: "#/tenants/abc" is "/tenants/abc". The console has four screens; no router is needed. */
function read(): string {
  const hash = window.location.hash.replace(/^#/, "");
  return hash === "" ? "/" : hash;
}

export function useHashRoute(): string {
  const [route, setRoute] = useState(read);
  useEffect(() => {
    const handler = () => setRoute(read());
    window.addEventListener("hashchange", handler);
    return () => window.removeEventListener("hashchange", handler);
  }, []);
  return route;
}

export function go(path: string): void {
  window.location.hash = path;
}
