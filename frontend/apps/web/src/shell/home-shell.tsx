import type { ReactNode } from "react";
import { AppHeader } from "./home/app-header";
import { AppSidebar } from "./home/app-sidebar";
import type { HomeSidebarData } from "./home/types";

export function HomeShell({
  data,
  children,
}: {
  data: HomeSidebarData;
  children: ReactNode;
}) {
  const primaryWorkspaceId = data.workspaces[0]?.id;

  return (
    <div className="flex h-screen w-screen flex-col overflow-hidden bg-background">
      <AppHeader workspaceId={primaryWorkspaceId} />
      <div className="flex min-h-0 flex-1 overflow-hidden px-2 pb-1 pt-2">
        <AppSidebar data={data} />
        <main className="min-w-0 flex-1 overflow-y-auto rounded-r-xl bg-card p-8 shadow-sm">
          {children}
        </main>
      </div>
    </div>
  );
}
