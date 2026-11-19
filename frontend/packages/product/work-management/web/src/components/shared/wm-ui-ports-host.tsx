import type { ReactNode } from "react";
import { useCardImageUrl, useFullBoard } from "@notrelix/work-management-state";
import { WmUiPortsProvider, type WmUiPorts } from "./wm-ui-ports";

const PORTS: WmUiPorts = {
  useCardImageUrl,
  useRelationTarget: (boardId, workspaceId) =>
    useFullBoard(boardId, workspaceId),
};

/** Container: fills the ports of the pure Work Management surfaces with the state package's queries. Mount once, inside the services provider. */
export function WorkManagementUiPortsHost({
  children,
}: {
  children: ReactNode;
}) {
  return <WmUiPortsProvider ports={PORTS}>{children}</WmUiPortsProvider>;
}
