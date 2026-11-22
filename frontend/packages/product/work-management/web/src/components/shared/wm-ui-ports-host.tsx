import { useMemo, type ReactNode } from "react";
import { useCardImageUrl, useFullBoard } from "@notrelix/work-management-state";
import { WmUiPortsProvider, type WmUiPorts } from "./wm-ui-ports";

/**
 * Container: fills the ports of the pure Work Management surfaces with the state package's queries, and the
 * mention port with the hook the app builds from the collaboration feature. Mount once, inside the services
 * provider; `useMentionSuggestions` must be the same function on every render.
 */
export function WorkManagementUiPortsHost({
  useMentionSuggestions,
  children,
}: {
  useMentionSuggestions?: WmUiPorts["useMentionSuggestions"];
  children: ReactNode;
}) {
  const ports = useMemo<WmUiPorts>(
    () => ({
      useCardImageUrl,
      useRelationTarget: (boardId, workspaceId) =>
        useFullBoard(boardId, workspaceId),
      useMentionSuggestions:
        useMentionSuggestions ?? (() => NO_MENTION_SUGGESTIONS),
    }),
    [useMentionSuggestions],
  );
  return <WmUiPortsProvider ports={ports}>{children}</WmUiPortsProvider>;
}

const NO_MENTION_SUGGESTIONS = {
  people: [],
  status: "success" as const,
  retry: () => undefined,
};
