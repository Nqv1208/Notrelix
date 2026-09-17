import { Link, useLocation } from "@tanstack/react-router";
import { useWorkspaceContext } from "../../providers/workspace-provider";
import {
  Avatar,
  AvatarFallback,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@notrelix/ui-web";
import { ScrollArea } from "@notrelix/ui-web";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { ChevronDown, Search } from "lucide-react";
import type { WorkspaceMember } from "@notrelix/features-workspace/core";
import { cn } from "@notrelix/ui-web";
import {
  PRIMARY_NAV_CONTRIBUTIONS,
  SUPPORT_NAV_CONTRIBUTIONS,
  type NavigationContribution,
} from "../navigation-contributions";

const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
];

type NavItem = NavigationContribution;

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

export function WorkspaceSidebar({
  onOpenSearch,
}: {
  onOpenSearch: () => void;
}) {
  const location = useLocation();
  const { workspaceId, members } = useWorkspaceContext();

  const pathname = location.pathname;

  const primaryNav: NavItem[] = PRIMARY_NAV_CONTRIBUTIONS.map((c) => ({
    ...c,
    to: c.to.replace("$workspaceId", workspaceId),
  }));

  const supportNav: NavItem[] = SUPPORT_NAV_CONTRIBUTIONS.map((c) => ({
    ...c,
    to: c.to.replace("$workspaceId", workspaceId),
  }));

  return (
    <aside className="w-64 border-r bg-card flex flex-col h-screen text-card-foreground">
      <div className="p-3 border-b">
        <WorkspaceSwitcher />
      </div>

      <div className="p-3">
        <button
          onClick={onOpenSearch}
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
              item.to === `/workspaces/${workspaceId}`
                ? pathname === `/workspaces/${workspaceId}`
                : pathname.startsWith(item.to);
            return (
              <Link
                key={item.id}
                to={item.to as "/workspaces/$workspaceId"}
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
              key={item.id}
              to={item.to as "/workspaces/$workspaceId"}
              className="flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground"
            >
              <item.icon className="size-4 shrink-0" />
              <span className="min-w-0 flex-1 truncate">{item.label}</span>
            </Link>
          ))}
        </div>
      </div>

    </aside>
  );
}
