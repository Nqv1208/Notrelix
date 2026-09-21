import { Link, useLocation } from "@tanstack/react-router";
import { useWorkspaceContext } from "../../providers/workspace-provider";
import {
  Avatar,
  AvatarFallback,
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  ScrollArea,
  cn,
} from "@notrelix/ui-web";
import {
  ChevronDown,
  PanelLeftClose,
  PanelLeftOpen,
  Search,
} from "lucide-react";
import type { ReactNode } from "react";
import type { WorkspaceMember } from "@notrelix/features-workspace/core";
import { WorkspaceSwitcher } from "./workspace-switcher";
import {
  PRIMARY_NAV_CONTRIBUTIONS,
  SUPPORT_NAV_CONTRIBUTIONS,
  WORKSPACE_NAV_CONTRIBUTIONS,
  isWorkspaceNavigationItemActive,
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

type SidebarVariant = "desktop" | "mobile";

interface WorkspaceSidebarProps {
  onOpenSearch: () => void;
  onNavigate?: () => void;
  onToggleCollapsed?: () => void;
  collapsed?: boolean;
  variant?: SidebarVariant;
}

function SidebarSection({
  title,
  collapsed,
  children,
}: {
  title: string;
  collapsed: boolean;
  children: ReactNode;
}) {
  return (
    <Collapsible defaultOpen className="mt-5">
      <CollapsibleTrigger
        aria-label={title}
        className={cn(
          "mb-2 flex w-full items-center justify-between px-2 text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground",
          collapsed && "justify-center px-0",
        )}
      >
        <span className={cn(collapsed && "sr-only")}>{title}</span>
        <ChevronDown className="size-3.5" />
      </CollapsibleTrigger>
      <CollapsibleContent>{children}</CollapsibleContent>
    </Collapsible>
  );
}

function NavigationLink({
  item,
  workspaceId,
  pathname,
  collapsed,
  onNavigate,
}: {
  item: NavigationContribution;
  workspaceId: string;
  pathname: string;
  collapsed: boolean;
  onNavigate?: () => void;
}) {
  const isActive = isWorkspaceNavigationItemActive(item, pathname, workspaceId);
  const className = cn(
    "flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
    collapsed && "justify-center px-0",
    isActive && "bg-accent text-accent-foreground",
  );
  const content = (
    <>
      <item.icon className="size-4 shrink-0" />
      <span className={cn("min-w-0 flex-1 truncate", collapsed && "sr-only")}>
        {item.label}
      </span>
    </>
  );

  const commonProps = {
    onClick: onNavigate,
    "aria-current": isActive ? ("page" as const) : undefined,
    title: collapsed ? item.label : undefined,
    className,
  };

  switch (item.to) {
    case "/workspaces/$workspaceId/dashboard":
      return (
        <Link
          to="/workspaces/$workspaceId/dashboard"
          params={{ workspaceId }}
          {...commonProps}
        >
          {content}
        </Link>
      );
    case "/workspaces/$workspaceId/chat":
      return (
        <Link
          to="/workspaces/$workspaceId/chat"
          params={{ workspaceId }}
          {...commonProps}
        >
          {content}
        </Link>
      );
    case "/workspaces/$workspaceId/members":
      return (
        <Link
          to="/workspaces/$workspaceId/members"
          params={{ workspaceId }}
          {...commonProps}
        >
          {content}
        </Link>
      );
    case "/workspaces/$workspaceId/billing":
      return (
        <Link
          to="/workspaces/$workspaceId/billing"
          params={{ workspaceId }}
          {...commonProps}
        >
          {content}
        </Link>
      );
    case "/workspaces/$workspaceId/settings":
      return (
        <Link
          to="/workspaces/$workspaceId/settings"
          params={{ workspaceId }}
          {...commonProps}
        >
          {content}
        </Link>
      );
  }
}

export function WorkspaceSidebar({
  onOpenSearch,
  onNavigate,
  onToggleCollapsed,
  collapsed = false,
  variant = "desktop",
}: WorkspaceSidebarProps) {
  const location = useLocation();
  const { workspaceId, members } = useWorkspaceContext();
  const isMobile = variant === "mobile";
  const isCollapsed = collapsed && !isMobile;

  return (
    <aside
      aria-label="Workspace navigation"
      className={cn(
        "flex h-full flex-col border-r bg-card text-card-foreground",
        isMobile ? "w-full" : isCollapsed ? "w-16" : "w-64",
      )}
    >
      <div className="border-b p-3">
        <WorkspaceSwitcher collapsed={isCollapsed} />
      </div>

      <div className={cn("p-3", isCollapsed && "px-2")}>
        <Button
          type="button"
          variant="outline"
          aria-label="Search workspace"
          title={isCollapsed ? "Search workspace" : undefined}
          onClick={onOpenSearch}
          className={cn(
            "flex h-9 w-full items-center gap-2 rounded-lg bg-muted px-3 text-sm text-muted-foreground transition hover:text-foreground",
            isCollapsed && "justify-center px-0",
          )}
        >
          <Search className="size-4 shrink-0" />
          <span
            className={cn("min-w-0 flex-1 text-left", isCollapsed && "sr-only")}
          >
            Search workspace
          </span>
        </Button>
      </div>

      <ScrollArea className={cn("min-h-0 flex-1 px-3", isCollapsed && "px-2")}>
        <nav aria-label="Workspace sections" className="space-y-1">
          {PRIMARY_NAV_CONTRIBUTIONS.map((item) => (
            <NavigationLink
              key={item.id}
              item={item}
              workspaceId={workspaceId}
              pathname={location.pathname}
              collapsed={isCollapsed}
              onNavigate={onNavigate}
            />
          ))}
        </nav>

        <SidebarSection title="Workspace" collapsed={isCollapsed}>
          <nav aria-label="Workspace resources" className="space-y-1">
            {WORKSPACE_NAV_CONTRIBUTIONS.map((item) => (
              <NavigationLink
                key={item.id}
                item={item}
                workspaceId={workspaceId}
                pathname={location.pathname}
                collapsed={isCollapsed}
                onNavigate={onNavigate}
              />
            ))}
          </nav>
        </SidebarSection>

        <SidebarSection title="Team online" collapsed={isCollapsed}>
          <div className="space-y-1">
            {members.map((member: WorkspaceMember, index: number) => (
              <div
                key={member.id}
                title={isCollapsed ? member.name : undefined}
                className={cn(
                  "flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm",
                  isCollapsed && "justify-center px-0",
                )}
              >
                <Avatar className="size-6">
                  <AvatarFallback
                    className={cn(
                      "text-[10px]",
                      avatarColors[index % avatarColors.length],
                    )}
                  >
                    {member.initials}
                  </AvatarFallback>
                </Avatar>
                <span
                  className={cn(
                    "min-w-0 flex-1 truncate text-muted-foreground",
                    isCollapsed && "sr-only",
                  )}
                >
                  {member.name}
                </span>
                <span
                  aria-label={`${member.name} is ${member.status}`}
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
      </ScrollArea>

      <div
        className={cn(
          "mt-auto shrink-0 border-t border-border/60 bg-muted/5 p-3",
          isCollapsed && "px-2",
        )}
      >
        <nav aria-label="Workspace administration" className="space-y-1">
          {SUPPORT_NAV_CONTRIBUTIONS.map((item) => (
            <NavigationLink
              key={item.id}
              item={item}
              workspaceId={workspaceId}
              pathname={location.pathname}
              collapsed={isCollapsed}
              onNavigate={onNavigate}
            />
          ))}
        </nav>

        {!isMobile && onToggleCollapsed ? (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            aria-label={
              isCollapsed
                ? "Expand workspace sidebar"
                : "Collapse workspace sidebar"
            }
            title={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            onClick={onToggleCollapsed}
            className={cn(
              "mt-2 h-9 w-full gap-2 text-muted-foreground hover:text-foreground",
              isCollapsed && "justify-center px-0",
            )}
          >
            {isCollapsed ? (
              <PanelLeftOpen className="size-4" />
            ) : (
              <PanelLeftClose className="size-4" />
            )}
            <span className={cn(isCollapsed && "sr-only")}>
              {isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            </span>
          </Button>
        ) : null}
      </div>
    </aside>
  );
}
