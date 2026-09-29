import { useNavigate } from "@tanstack/react-router";
import type { WorkspaceSummary } from "../../core/types/workspace";
import { WorkspaceDirectorySurface } from "./workspace-directory-surface";

export function WorkspaceDirectory({
  workspaces,
}: {
  workspaces: readonly WorkspaceSummary[];
}) {
  const navigate = useNavigate();

  return (
    <WorkspaceDirectorySurface
      workspaces={workspaces}
      onOpenWorkspace={(workspaceId) =>
        navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId },
        })
      }
    />
  );
}
