import { Link } from "@tanstack/react-router";
import { ArrowUpRight, Users } from "lucide-react";
import type { WorkspaceSummary } from "../../core/types/workspace";

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

export function WorkspaceDirectory({
  workspaces,
}: {
  workspaces: readonly WorkspaceSummary[];
}) {
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
              <Link
                key={workspace.id}
                to="/workspaces/$workspaceId"
                params={{ workspaceId: workspace.id }}
                className="group rounded-2xl border border-border bg-card p-4 transition hover:-translate-y-0.5 hover:shadow-[rgba(205,208,223,0.35)_0px_2px_24px]"
              >
                <div className="mb-5 flex items-center justify-between">
                  <span
                    className="flex size-11 items-center justify-center rounded-xl text-sm font-semibold text-white"
                    style={{ backgroundColor: colorForWorkspace(workspace.id) }}
                  >
                    {(workspace.icon || workspace.name).charAt(0).toUpperCase()}
                  </span>
                  <ArrowUpRight className="size-4 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
                </div>
                <h3 className="line-clamp-1 text-sm font-semibold text-foreground">
                  {workspace.name}
                </h3>
                <p className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                  <Users className="size-3.5" />
                  {workspace.memberCount} {memberLabel} ·{" "}
                  {formatWorkspacePlan(workspace)}
                </p>
              </Link>
            );
          })}
        </div>
      )}
    </section>
  );
}
