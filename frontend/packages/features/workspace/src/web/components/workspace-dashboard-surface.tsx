import { Clock, FileText, LayoutGrid, Users } from "lucide-react";
import { Avatar, AvatarFallback, cn } from "@notrelix/ui-web";
import type { WorkspaceActivityItem } from "../../core/types/workspace";
import {
  avatarColors,
  formatDateLabel,
  getInitials,
} from "./workspace-ui-models";

export interface WorkspaceDashboardSurfaceProps {
  workspaceName: string;
  pageCount: number;
  boardCount: number;
  memberCount: number;
  activities: WorkspaceActivityItem[];
  referenceDate?: string;
  status?: "idle" | "loading";
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
