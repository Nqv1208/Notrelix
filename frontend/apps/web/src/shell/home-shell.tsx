import { useState, type ReactNode } from "react";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@notrelix/ui-web";
import { AppHeader } from "./home/app-header";
import { AppSidebar } from "./home/app-sidebar";
import type { HomeSidebarData } from "./home/types";

export function HomeShell({
  data,
  onCreateWorkspace,
  children,
}: {
  data: HomeSidebarData;
  onCreateWorkspace?: (name: string) => void;
  children: ReactNode;
}) {
  const primaryWorkspaceId = data.workspaces[0]?.id;
  const [mobileNavigationOpen, setMobileNavigationOpen] = useState(false);

  return (
    <div className="flex h-screen w-screen flex-col overflow-hidden bg-background">
      <AppHeader
        workspaceId={primaryWorkspaceId}
        onOpenNavigation={() => setMobileNavigationOpen(true)}
      />

      <Sheet open={mobileNavigationOpen} onOpenChange={setMobileNavigationOpen}>
        <SheetContent
          side="left"
          className="w-[300px] gap-0 p-0 sm:max-w-[300px]"
        >
          <SheetHeader className="sr-only">
            <SheetTitle>Home navigation</SheetTitle>
            <SheetDescription>
              Navigate your Notrelix workspace.
            </SheetDescription>
          </SheetHeader>
          <AppSidebar
            data={data}
            mode="mobile"
            onCreateWorkspace={onCreateWorkspace}
          />
        </SheetContent>
      </Sheet>

      <div className="flex min-h-0 flex-1 overflow-hidden p-2">
        <div className="hidden h-full md:block">
          <AppSidebar data={data} onCreateWorkspace={onCreateWorkspace} />
        </div>
        <main className="min-w-0 flex-1 overflow-y-auto rounded-xl bg-card p-4 shadow-sm sm:p-6 md:rounded-l-none lg:p-8">
          {children}
        </main>
      </div>
    </div>
  );
}
