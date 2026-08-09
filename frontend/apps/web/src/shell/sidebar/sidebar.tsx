import { useState, useMemo } from "react";
import { Link, useLocation } from "@tanstack/react-router";
import { useWorkspaceContext } from "../../providers/workspace-provider";
import {
  createUseWorkspaceShellData,
  createUseWorkspaceMembers,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  Avatar,
  AvatarFallback,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@notrelix/ui-web";
import { ScrollArea } from "@notrelix/ui-web";
import { GlobalSearch } from "../global-search";
import { WorkspaceSwitcher } from "./workspace-switcher";
import {
  Bell,
  ChevronDown,
  Home,
  Inbox,
  LifeBuoy,
  MessageSquareText,
  Search,
  Settings,
  Star,
  UserRoundCheck,
} from "lucide-react";
import type { WorkspaceMember } from "@notrelix/features-workspace/core";
import { cn } from "@notrelix/ui-web";

const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
];

type NavItem = {
  label: string;
  icon: typeof Home;
  to: string;
  params: Record<string, string>;
};

function SidebarSection({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <Collapsible defaultOpen className="mt-5">
      <CollapsibleTrigger className="mb-2 flex w-full items-center justify-between px-2 text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">
        {title}
        <ChevronDown className="size-3.5" />
      </CollapsibleTrigger>
      <CollapsibleContent>{children}</CollapsibleContent>
    </Collapsible>
  );
}

export function WorkspaceSidebar() {
  const location = useLocation();
  const { api: runtimeClient } = useAppRuntime();
  const { workspaceId } = useWorkspaceContext();
  const [searchOpen, setSearchOpen] = useState(false);

  const useShellData = useMemo(
    () =>
      createUseWorkspaceShellData({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const useMembers = useMemo(
    () => createUseWorkspaceMembers({ api: runtimeClient.api }),
    [runtimeClient],
  );

  const { views = [] } = useShellData(workspaceId);
  const { data: members = [] } = useMembers(workspaceId);

  const pathname = location.pathname;

  const favorites = useMemo(
    () =>
      views.slice(0, 3).map((v) => ({
        id: v.id,
        title: v.name,
        href: `/workspaces/${workspaceId}`,
      })),
    [views, workspaceId],
  );

  const primaryNav: NavItem[] = [
    {
      label: "Home",
      icon: Home,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
    {
      label: "My Work",
      icon: UserRoundCheck,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
    {
      label: "Inbox",
      icon: Inbox,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
    {
      label: "Notifications",
      icon: Bell,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
    {
      label: "Chat Rooms",
      icon: MessageSquareText,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
  ];

  const supportNav: NavItem[] = [
    {
      label: "Help / Support",
      icon: LifeBuoy,
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
    },
    {
      label: "Settings",
      icon: Settings,
      to: "/workspaces/$workspaceId/settings",
      params: { workspaceId },
    },
  ];

  return (
    <aside className="w-64 border-r bg-card flex flex-col h-screen text-card-foreground">
      <div className="p-3 border-b">
        <WorkspaceSwitcher />
      </div>

      <div className="p-3">
        <button
          onClick={() => setSearchOpen(true)}
          className="flex h-9 w-full items-center gap-2 rounded-lg border border-border bg-muted px-3 text-sm text-muted-foreground transition hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <Search className="size-4" />
          <span className="min-w-0 flex-1 text-left">Search workspace</span>
        </button>
      </div>

      <ScrollArea className="min-h-0 flex-1 px-3">
        <nav className="space-y-1">
          {primaryNav.map((item) => {
            const isActive =
              item.to === "/workspaces/$workspaceId"
                ? pathname === `/workspaces/${workspaceId}`
                : pathname.startsWith(
                    item.to.replace("$workspaceId", workspaceId),
                  );
            return (
              <Link
                key={item.label}
                to={item.to as "/workspaces/$workspaceId"}
                params={item.params}
                className={cn(
                  "flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground",
                  isActive && "bg-accent text-accent-foreground",
                )}
              >
                <item.icon className="size-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{item.label}</span>
              </Link>
            );
          })}
        </nav>

        <div className="space-y-6 mt-6 pb-6">
          <SidebarSection title="Quick access">
            <div className="space-y-1">
              {favorites.map((item) => (
                <Link
                  key={item.id}
                  to="/workspaces/$workspaceId"
                  params={{ workspaceId }}
                  className="group flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
                >
                  <Star className="size-3.5 text-primary" />
                  <span className="min-w-0 flex-1 truncate">{item.title}</span>
                </Link>
              ))}
            </div>
          </SidebarSection>

          <SidebarSection title="Team online">
            <div className="space-y-1">
              {members.map((member: WorkspaceMember, i: number) => (
                <div
                  key={member.id}
                  className="flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm"
                >
                  <Avatar className="size-6">
                    <AvatarFallback
                      className={cn(
                        "text-[10px]",
                        avatarColors[i % avatarColors.length],
                      )}
                    >
                      {member.initials}
                    </AvatarFallback>
                  </Avatar>
                  <span className="min-w-0 flex-1 truncate text-muted-foreground">
                    {member.name}
                  </span>
                  <span
                    className={cn(
                      "size-2 rounded-full",
                      member.status === "active"
                        ? "bg-primary"
                        : member.status === "in-call"
                          ? "bg-accent-foreground"
                          : member.status === "idle"
                            ? "bg-muted-foreground"
                            : "bg-border",
                    )}
                  />
                </div>
              ))}
            </div>
          </SidebarSection>
        </div>
      </ScrollArea>

      <div className="mt-auto border-t border-border/60 bg-muted/5 p-3 shrink-0">
        <div className="space-y-1">
          {supportNav.map((item) => (
            <Link
              key={item.label}
              to={item.to as "/workspaces/$workspaceId"}
              params={item.params}
              className="flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground"
            >
              <item.icon className="size-4 shrink-0" />
              <span className="min-w-0 flex-1 truncate">{item.label}</span>
            </Link>
          ))}
        </div>
      </div>

      <GlobalSearch open={searchOpen} onClose={() => setSearchOpen(false)} />
    </aside>
  );
}
