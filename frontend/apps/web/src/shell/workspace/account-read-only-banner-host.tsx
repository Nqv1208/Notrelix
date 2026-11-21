import { isAccountReadOnly } from "@notrelix/features-workspace/core";
import {
  AccountReadOnlyBanner,
  useWorkspaceShellCopy,
  type ShellNavigation,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useWorkspaceViewer } from "./use-workspace-viewer";

/**
 * The read-only banner of a Suspended or Closed Account for every member, with the billing page for an
 * Account Owner or Admin. It stands in for the archived banner when both apply (the stronger state).
 */
export function AccountReadOnlyBannerHost({
  navigation,
}: {
  readonly navigation: ShellNavigation;
}) {
  const copy = useWorkspaceShellCopy();
  const { workspaceId, workspace } = useWorkspaceContext();
  const viewer = useWorkspaceViewer(workspaceId);
  if (!isAccountReadOnly(workspace)) return null;
  return (
    <AccountReadOnlyBanner
      copy={copy}
      status={workspace?.accountStatus === "Closed" ? "Closed" : "Suspended"}
      canManageBilling={viewer.role === "owner" || viewer.role === "admin"}
      navigation={navigation}
    />
  );
}
