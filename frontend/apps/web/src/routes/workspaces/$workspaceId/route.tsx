import { useEffect, useState } from "react";
import { Outlet, useParams, useLocation } from "@tanstack/react-router";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { WorkspaceGuard } from "@/shell/guards/workspace-guard";
import { WorkspaceProvider } from "@/providers/workspace-provider";
import { WorkspaceSidebar } from "@/shell/sidebar/sidebar";
import { WorkspaceTopbar } from "@/shell/topbar/topbar";
import { WorkspaceTabbedFrame } from "@/shell/workspace-tabbed-frame";
import { GlobalSearch } from "@/shell/global-search";

const CONTENT_ROUTES = ["/", "/dashboard", "/boards", "/docs"];

function isContentRoute(pathname: string, workspaceId: string): boolean {
  const path = pathname.replace(`/workspaces/${workspaceId}`, "") || "/";
  return CONTENT_ROUTES.some(
    (route) => path === route || path.startsWith(route + "/"),
  );
}

export function WorkspaceLayout() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });
  const location = useLocation();
  const useTabbedFrame = isContentRoute(location.pathname, workspaceId);
  const [searchOpen, setSearchOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setSearchOpen(true);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <AuthGuard>
      <WorkspaceProvider workspaceId={workspaceId}>
        <WorkspaceGuard workspaceId={workspaceId}>
          <>
            <div className="flex h-screen w-screen overflow-hidden bg-background">
              <WorkspaceSidebar onOpenSearch={() => setSearchOpen(true)} />
              <div className="flex-1 flex flex-col min-w-0">
                {!useTabbedFrame && (
                  <WorkspaceTopbar onOpenSearch={() => setSearchOpen(true)} />
                )}
                <main className="flex-1 overflow-y-auto min-h-0">
                  {useTabbedFrame ? (
                    <WorkspaceTabbedFrame>
                      <Outlet />
                    </WorkspaceTabbedFrame>
                  ) : (
                    <Outlet />
                  )}
                </main>
              </div>
            </div>
            <GlobalSearch open={searchOpen} onClose={() => setSearchOpen(false)} />
          </>
        </WorkspaceGuard>
      </WorkspaceProvider>
    </AuthGuard>
  );
}
