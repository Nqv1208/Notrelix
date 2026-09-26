import { useEffect, useState } from "react";
import { Outlet, useParams, useLocation } from "@tanstack/react-router";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { WorkspaceGuard } from "@/shell/guards/workspace-guard";
import { WorkspaceProvider } from "@/providers/workspace-provider";
import { WorkspaceSidebar } from "@/shell/sidebar/sidebar";
import { WorkspaceTopbar } from "@/shell/topbar/topbar";
import { WorkspaceTabbedFrame } from "@/shell/workspace-tabbed-frame";
import { GlobalSearch } from "@/shell/global-search";
import { AppHeader } from "@/shell/home/app-header";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@notrelix/ui-web";

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
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

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
      <WorkspaceGuard workspaceId={workspaceId}>
        <WorkspaceProvider workspaceId={workspaceId}>
          <>
            <div className="flex h-screen w-screen flex-col overflow-hidden bg-background">
              <AppHeader
                workspaceId={workspaceId}
                onOpenNavigation={() => setMobileSidebarOpen(true)}
              />
              <div className="flex min-h-0 flex-1 overflow-hidden">
                <div className="hidden h-full shrink-0 md:flex">
                  <WorkspaceSidebar
                    collapsed={sidebarCollapsed}
                    onOpenSearch={() => setSearchOpen(true)}
                    onToggleCollapsed={() =>
                      setSidebarCollapsed((value) => !value)
                    }
                  />
                </div>
                <Sheet
                  open={mobileSidebarOpen}
                  onOpenChange={setMobileSidebarOpen}
                >
                  <SheetContent
                    side="left"
                    className="w-[280px] gap-0 p-0 sm:max-w-[280px] md:hidden"
                  >
                    <SheetHeader className="sr-only">
                      <SheetTitle>Workspace navigation</SheetTitle>
                      <SheetDescription>
                        Navigate this workspace.
                      </SheetDescription>
                    </SheetHeader>
                    <WorkspaceSidebar
                      variant="mobile"
                      onOpenSearch={() => {
                        setMobileSidebarOpen(false);
                        setSearchOpen(true);
                      }}
                      onNavigate={() => setMobileSidebarOpen(false)}
                    />
                  </SheetContent>
                </Sheet>
                <div className="flex min-w-0 flex-1 flex-col">
                  {!useTabbedFrame && (
                    <WorkspaceTopbar onOpenSearch={() => setSearchOpen(true)} />
                  )}
                  <main className="min-h-0 flex-1 overflow-y-auto">
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
            </div>
            <GlobalSearch
              open={searchOpen}
              onClose={() => setSearchOpen(false)}
            />
          </>
        </WorkspaceProvider>
      </WorkspaceGuard>
    </AuthGuard>
  );
}
