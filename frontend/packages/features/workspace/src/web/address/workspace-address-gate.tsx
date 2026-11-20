import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import type { WorkspaceAddressService } from "../../core/api/workspace-address.service";
import { workspaceQueryKeys } from "../../query/keys";
import {
  WorkspaceScopeStateSurface,
  type WorkspaceScopeState,
} from "../shell/workspace-scope-state-surface";
import { useWorkspaceShellCopy } from "../shell/use-workspace-shell-copy";

export interface WorkspaceAddressGateProps {
  readonly service: WorkspaceAddressService;
  readonly accountId: string;
  readonly slug: string;
  /** A live address: go to the workspace, whose route checks sign-in and membership. */
  readonly onOpen: (workspaceId: string) => void;
  readonly exitHref: string;
  readonly onExit: () => void;
}

/**
 * Opens a workspace by its address. While the address resolves, and while the redirect runs,
 * it shows the shell's loading state. A deleted address and a made-up one both end in the
 * shell's refusal state (same screen, same copy); a failed lookup is the error state with a retry.
 * There is no board for the refusal, so it reuses the workspace scope states.
 */
export function WorkspaceAddressGate({
  service,
  accountId,
  slug,
  onOpen,
  exitHref,
  onExit,
}: WorkspaceAddressGateProps) {
  const copy = useWorkspaceShellCopy();
  const query = useQuery({
    queryKey: workspaceQueryKeys.address(accountId, slug),
    queryFn: () => service.resolve(accountId, slug),
    retry: false,
    gcTime: 0,
  });

  const openId = query.data?.kind === "found" ? query.data.workspaceId : null;
  useEffect(() => {
    if (openId) onOpen(openId);
  }, [openId, onOpen]);

  const state: WorkspaceScopeState = query.isError
    ? "error"
    : query.data?.kind === "refused"
      ? "forbidden"
      : "loading";

  return (
    <WorkspaceScopeStateSurface
      copy={copy}
      state={state}
      onRetry={() => void query.refetch()}
      exitHref={exitHref}
      onExit={onExit}
    />
  );
}
