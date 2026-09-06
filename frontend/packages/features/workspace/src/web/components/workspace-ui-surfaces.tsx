import { useState } from "react";
import {
  Bot,
  ChevronDown,
  Clock,
  FileText,
  LayoutGrid,
  MessageSquareText,
  MoreHorizontal,
  Plus,
  Settings,
  Share2,
  Sparkles,
  Star,
  UserPlus,
  Users,
  Workflow,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Badge,
  Button,
  Separator,
  cn,
} from "@notrelix/ui-web";
import type {
  WorkspaceActivityItem,
  WorkspaceInvitation,
  WorkspaceMember,
  WorkspaceSummary,
  WorkspaceView,
} from "../../core/types/workspace";

const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
];

const workspaceColors = [
  "#6161ff",
  "#2a9d99",
  "#ff8940",
  "#8b5cf6",
  "#0f9f6e",
  "#dc3f6d",
] as const;

function colorForWorkspace(id: string) {
  const hash = Array.from(id).reduce(
    (value, character) => value + character.charCodeAt(0),
    0,
  );
  return workspaceColors[hash % workspaceColors.length];
}

function formatWorkspacePlan(workspace: WorkspaceSummary) {
  if (workspace.isPersonal) return "Personal";
  return workspace.plan.charAt(0).toUpperCase() + workspace.plan.slice(1);
}

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

export interface WorkspaceDirectorySurfaceProps {
  workspaces: readonly WorkspaceSummary[];
  onOpenWorkspace?: (workspaceId: string) => void;
}

export function WorkspaceDirectorySurface({
  workspaces,
  onOpenWorkspace,
}: WorkspaceDirectorySurfaceProps) {
  return (
    <section aria-labelledby="workspace-directory-title">
      <div className="mb-3 flex items-center justify-between">
        <h2
          id="workspace-directory-title"
          className="text-sm font-semibold text-foreground"
        >
          Your workspaces
        </h2>
        <span className="text-xs text-muted-foreground">
          {workspaces.length} total
        </span>
      </div>

      {workspaces.length === 0 ? (
        <div className="rounded-2xl border border-border bg-card p-4 text-sm text-muted-foreground">
          No workspaces are available for this account.
        </div>
      ) : (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {workspaces.map((workspace) => {
            const memberLabel =
              workspace.memberCount === 1 ? "member" : "members";

            return (
              <button
                key={workspace.id}
                type="button"
                onClick={() => onOpenWorkspace?.(workspace.id)}
                className="group rounded-2xl border border-border bg-card p-4 text-left transition hover:-translate-y-0.5 hover:shadow-[rgba(205,208,223,0.35)_0px_2px_24px]"
              >
                <div className="mb-5 flex items-center justify-between">
                  <span
                    className="flex size-11 items-center justify-center rounded-xl text-sm font-semibold text-white"
                    style={{ backgroundColor: colorForWorkspace(workspace.id) }}
                  >
                    {(workspace.icon || workspace.name).charAt(0).toUpperCase()}
                  </span>
                  <Users className="size-4 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
                </div>
                <h3 className="line-clamp-1 text-sm font-semibold text-foreground">
                  {workspace.name}
                </h3>
                <p className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                  <Users className="size-3.5" />
                  {workspace.memberCount} {memberLabel} ·{" "}
                  {formatWorkspacePlan(workspace)}
                </p>
              </button>
            );
          })}
        </div>
      )}
    </section>
  );
}

export interface WorkspaceCompactHeaderSurfaceProps {
  workspace: WorkspaceSummary;
  members: WorkspaceMember[];
  onCopyLink?: () => void;
  onOpenSettings?: () => void;
  onInvite?: () => void;
  onShare?: () => void;
  onFavorite?: () => void;
}

export function WorkspaceCompactHeaderSurface({
  workspace,
  members,
  onCopyLink,
  onOpenSettings,
  onInvite,
  onShare,
  onFavorite,
}: WorkspaceCompactHeaderSurfaceProps) {
  const [open, setOpen] = useState(false);

  return (
    <header className="border-b border-border bg-card px-4 py-2.5 sm:px-6">
      <div className="flex min-h-10 flex-wrap items-center gap-2">
        <div className="relative">
          <Button
            variant="ghost"
            className="h-10 rounded-xl px-2 text-left"
            onClick={() => setOpen((value) => !value)}
          >
            <span className="mr-2 flex size-8 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
              {workspace.icon}
            </span>
            <span className="max-w-[220px] truncate text-lg font-semibold tracking-[-0.01em] text-foreground">
              {workspace.name}
            </span>
            <ChevronDown className="ml-1 size-4 text-muted-foreground" />
          </Button>
          {open ? (
            <div className="absolute left-0 top-11 z-20 w-64 rounded-xl border border-border bg-popover p-1 shadow-lg">
              <p className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
                Workspace actions
              </p>
              <button
                type="button"
                onClick={onFavorite}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <Star className="size-4" />
                Add to favorites
              </button>
              <button
                type="button"
                onClick={onCopyLink}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <MessageSquareText className="size-4" />
                Copy workspace link
              </button>
              <Separator className="my-1" />
              <button
                type="button"
                onClick={onOpenSettings}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <Settings className="size-4" />
                Workspace Settings
              </button>
            </div>
          ) : null}
        </div>

        <Button
          variant="ghost"
          size="icon"
          aria-label="Favorite workspace"
          onClick={onFavorite}
        >
          <Star className="size-4" />
        </Button>
        <Badge variant="secondary" className="rounded-full capitalize">
          {workspace.plan}
        </Badge>
        <Separator
          orientation="vertical"
          className="mx-1 hidden h-6 md:block"
        />
        <Button variant="ghost" size="sm" className="rounded-full">
          <Sparkles className="size-4" />
          AI suggestions
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full">
          <Workflow className="size-4" />
          Automate
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full">
          <Bot className="size-4" />
          Agents
        </Button>
        <div className="ml-auto flex items-center gap-2">
          <div className="hidden -space-x-2 md:flex">
            {members.slice(0, 4).map((member, i) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback
                  className={cn(
                    "text-[10px]",
                    avatarColors[i % avatarColors.length],
                  )}
                >
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Button size="sm" className="rounded-full" onClick={onInvite}>
            <UserPlus className="size-4 mr-2" />
            Invite
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="bg-card"
            onClick={onShare}
          >
            <Share2 className="size-4" />
            Share
          </Button>
          <Button
            variant="ghost"
            size="icon"
            aria-label="More workspace actions"
          >
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  );
}

export interface WorkspaceViewTabsSurfaceProps {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  onSelectView?: (view: WorkspaceView) => void;
  onAddView?: () => void;
}

export function WorkspaceViewTabsSurface({
  views,
  activeViewId,
  onSelectView,
  onAddView,
}: WorkspaceViewTabsSurfaceProps) {
  return (
    <div className="border-b border-border bg-card">
      <div className="flex min-w-0 items-center gap-2 px-4 sm:px-6">
        <div
          role="tablist"
          aria-label="Workspace views"
          className="flex h-12 min-w-0 flex-1 items-center gap-1.5 overflow-x-auto py-1 whitespace-nowrap scrollbar-none"
        >
          {views.map((view) => {
            const active = view.id === activeViewId;
            return (
              <button
                key={view.id}
                type="button"
                role="tab"
                aria-selected={active}
                onClick={() => onSelectView?.(view)}
                className={cn(
                  "relative inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted/80 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                  active && "bg-muted/40 font-semibold text-foreground",
                )}
              >
                {view.name}
                {active ? (
                  <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" />
                ) : null}
              </button>
            );
          })}
        </div>
        <Button
          variant="ghost"
          size="sm"
          className="h-9 rounded-full px-2.5"
          onClick={onAddView}
        >
          <Plus className="size-4" />
          <span className="sr-only sm:not-sr-only">Add view</span>
        </Button>
        <Button variant="ghost" size="icon" aria-label="More view actions">
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>
  );
}

export interface PendingInvitationsMenuSurfaceProps {
  invitations: readonly WorkspaceInvitation[];
  status?: "idle" | "loading";
  onAccept?: (invitation: WorkspaceInvitation) => void;
  onDismiss?: () => void;
}

export function PendingInvitationsMenuSurface({
  invitations,
  status = "idle",
  onAccept,
  onDismiss,
}: PendingInvitationsMenuSurfaceProps) {
  const [open, setOpen] = useState(false);
  const hasInvitations = invitations.length > 0;

  return (
    <div className="relative">
      <button
        type="button"
        aria-label="Pending workspace invitations"
        onClick={() => setOpen((value) => !value)}
        className={cn(
          "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          hasInvitations && "text-primary animate-pulse",
        )}
      >
        <UserPlus className="size-[18px]" />
        {hasInvitations ? (
          <span className="absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full border border-card bg-emerald-500 text-[9px] font-bold text-white shadow-sm animate-bounce">
            {invitations.length}
          </span>
        ) : null}
      </button>
      {open ? (
        <div className="absolute right-0 top-10 z-20 w-80 overflow-hidden rounded-2xl border border-border/40 bg-card/95 p-0 shadow-xl backdrop-blur-md">
          <div className="flex items-center justify-between border-b border-border/40 px-4 py-3 bg-muted/30">
            <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
              <UserPlus className="size-4 text-primary" />
              Invitations ({invitations.length})
            </h4>
          </div>
          <div className="max-h-80 overflow-y-auto divide-y divide-border/40">
            {status === "loading" ? (
              <div className="flex flex-col items-center justify-center py-8 text-center text-xs text-muted-foreground gap-2">
                <span>Loading invitations...</span>
              </div>
            ) : !hasInvitations ? (
              <div className="flex flex-col items-center justify-center py-8 px-4 text-center text-xs text-muted-foreground gap-2">
                <div className="rounded-full bg-muted p-2 text-muted-foreground/60">
                  <UserPlus className="size-5" />
                </div>
                <p className="font-medium text-foreground/80">
                  No pending invitations
                </p>
              </div>
            ) : (
              invitations.map((invite) => (
                <div
                  key={invite.id}
                  className="space-y-3 p-4 transition-colors hover:bg-muted/10"
                >
                  <div className="space-y-1.5">
                    <h5 className="text-sm font-bold text-foreground leading-snug">
                      Workspace: {invite.workspaceName}
                    </h5>
                    <div className="space-y-1 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1.5">
                        <UserPlus className="size-3.5 text-primary/75" />
                        <span>
                          Invited by:{" "}
                          <strong className="text-foreground/90 font-medium">
                            {invite.inviterName}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Users className="size-3.5 text-primary/75" />
                        <span>
                          Role:{" "}
                          <strong className="text-foreground/90 font-medium capitalize">
                            {invite.role}
                          </strong>
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 pt-1">
                    <Button
                      size="sm"
                      onClick={() => onAccept?.(invite)}
                      className="flex-1 h-8 rounded-lg text-xs font-semibold gap-1.5 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm"
                    >
                      Accept
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={onDismiss}
                      className="h-8 w-8 p-0 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80"
                    >
                      ✕
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}

export interface WorkspaceDashboardSurfaceProps {
  workspaceName: string;
  pageCount: number;
  boardCount: number;
  memberCount: number;
  activities: WorkspaceActivityItem[];
  referenceDate?: string;
  status?: "idle" | "loading";
}

function formatDateLabel(value: string): string {
  return value.slice(0, 10);
}

export function WorkspaceDashboardSurface({
  workspaceName,
  pageCount,
  boardCount,
  memberCount,
  activities,
  status = "idle",
}: WorkspaceDashboardSurfaceProps) {
  const isLoading = status === "loading";
  const stats = [
    { key: "pages", label: "Pages", icon: FileText, value: pageCount },
    {
      key: "boards",
      label: "Active Boards",
      icon: LayoutGrid,
      value: boardCount,
    },
    { key: "members", label: "Team Members", icon: Users, value: memberCount },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">{workspaceName}</h2>
        <p className="text-sm text-muted-foreground mt-1">
          Here&apos;s what&apos;s happening in your workspace.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <div
              key={stat.key}
              className="rounded-xl border border-border/60 bg-card/50 p-5 flex items-center gap-4"
            >
              <div className="size-10 rounded-xl flex items-center justify-center bg-muted">
                <Icon className="size-5" />
              </div>
              <div>
                {isLoading ? (
                  <div className="h-7 w-12 bg-muted rounded animate-pulse" />
                ) : (
                  <p className="text-2xl font-bold">{stat.value}</p>
                )}
                <p className="text-xs text-muted-foreground">{stat.label}</p>
              </div>
            </div>
          );
        })}
      </div>

      <div className="rounded-xl border border-border/60 bg-card/50 p-5">
        <div className="flex items-center gap-2 mb-4">
          <Clock className="size-4 text-muted-foreground" />
          <h3 className="font-semibold text-sm">Recent Activity</h3>
        </div>
        {isLoading ? (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-10 bg-muted rounded animate-pulse" />
            ))}
          </div>
        ) : activities.length === 0 ? (
          <p className="text-sm text-muted-foreground py-4">No activity yet.</p>
        ) : (
          <div className="space-y-1">
            {activities.slice(0, 7).map((item, i) => (
              <div
                key={item.id}
                className="flex items-start gap-3 p-2 rounded-md hover:bg-muted/30 transition-colors"
              >
                <Avatar className="size-7 mt-0.5">
                  <AvatarFallback
                    className={cn(
                      "text-[10px]",
                      avatarColors[i % avatarColors.length],
                    )}
                  >
                    {getInitials(item.actor)}
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <p className="text-sm leading-snug">
                    <span className="font-medium text-foreground">
                      {item.actor}
                    </span>{" "}
                    <span className="text-muted-foreground">{item.action}</span>{" "}
                    <span className="font-medium text-foreground">
                      {item.target}
                    </span>
                  </p>
                  <p className="text-[11px] text-muted-foreground mt-0.5">
                    {formatDateLabel(item.createdAt)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
