import { useEffect, useState } from "react";
import { Button, cn } from "@notrelix/ui-web";
import { onSessionEnded, setToken, type Session } from "./api";
import { Audit } from "./screens/audit";
import { Plans } from "./screens/plans";
import { SignIn } from "./screens/sign-in";
import { TenantDetail } from "./screens/tenant-detail";
import { Tenants } from "./screens/tenants";
import { go, useHashRoute } from "./use-hash-route";

const NAV = [
  { path: "/", label: "Tenants" },
  { path: "/plans", label: "Plan catalog" },
  { path: "/audit", label: "Audit trail" },
] as const;

export function App() {
  const [session, setSession] = useState<Session | null>(null);
  const route = useHashRoute();

  useEffect(() => {
    onSessionEnded(() => setSession(null));
  }, []);

  if (!session) return <SignIn onSignedIn={setSession} />;

  const detail = /^\/tenants\/([0-9a-f-]{36})$/i.exec(route);
  const active = detail ? "/" : route;

  return (
    <div className="grid min-h-svh grid-cols-1 bg-background text-foreground md:grid-cols-[220px_1fr]">
      <aside className="flex flex-col gap-6 border-b bg-surface-sidebar p-4 md:border-r md:border-b-0">
        <div className="text-subheading">Notrelix operator</div>
        <nav aria-label="Operator console" className="flex gap-1 md:flex-col">
          {NAV.map((item) => (
            <a
              key={item.path}
              href={`#${item.path}`}
              aria-current={active === item.path ? "page" : undefined}
              className={cn(
                "rounded-md px-3 py-2 text-nav text-foreground hover:bg-state-hover",
                active === item.path &&
                  "bg-state-selected text-state-selected-foreground",
              )}
            >
              {item.label}
            </a>
          ))}
        </nav>
        <div className="mt-auto flex flex-col gap-2 text-compact text-muted-foreground">
          <span className="truncate">{session.operator.email}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              setToken(null);
              setSession(null);
              go("/");
            }}
          >
            Sign out
          </Button>
        </div>
      </aside>
      <main className="min-w-0 p-6 md:p-8">
        {detail ? (
          <TenantDetail accountId={detail[1]} />
        ) : route === "/plans" ? (
          <Plans />
        ) : route === "/audit" ? (
          <Audit />
        ) : (
          <Tenants />
        )}
      </main>
    </div>
  );
}
