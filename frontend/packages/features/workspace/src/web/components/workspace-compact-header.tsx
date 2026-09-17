import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

import type {
  WorkspaceMember,
  WorkspaceSummary,
} from "../../core/types/workspace";
import { WorkspaceCompactHeaderSurface } from "./workspace-compact-header-surface";

export function WorkspaceCompactHeader({
  workspace,
  members,
}: {
  workspace: WorkspaceSummary;
  members: WorkspaceMember[];
}) {
  const navigate = useNavigate();

  const openSettings = () => {
    void navigate({
      to: "/workspaces/$workspaceId/settings",
      params: { workspaceId: workspace.id },
    });
  };

  const handleCopyLink = () => {
    if (typeof window === "undefined") return;

    const workspaceUrl = `${window.location.origin}/workspaces/${workspace.id}`;
    void navigator.clipboard
      .writeText(workspaceUrl)
      .then(() => toast.success("Workspace link copied to clipboard"))
      .catch(() => toast.error("Failed to copy workspace link"));
  };

  return (
    <WorkspaceCompactHeaderSurface
      workspace={workspace}
      members={members}
      onCopyLink={handleCopyLink}
      onOpenSettings={openSettings}
      onInvite={() => {
        void navigate({
          to: "/workspaces/$workspaceId/members",
          params: { workspaceId: workspace.id },
        });
      }}
    />
  );
}
