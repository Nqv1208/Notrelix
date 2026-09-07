import { Users } from "lucide-react";
import type { WorkspaceSummary } from "../../core/types/workspace";
import {
  colorForWorkspace,
  formatWorkspacePlan,
} from "./workspace-ui-models";

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