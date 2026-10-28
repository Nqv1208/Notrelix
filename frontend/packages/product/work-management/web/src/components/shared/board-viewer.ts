import { createContext, useContext } from "react";

/**
 * The signed-in member, provided by the host that shows a board or its item sheet.
 * The sheet reads it to tell the named approver and the requester from everyone else;
 * absent, it offers neither Approve nor Cancel.
 */
export const BoardViewerContext = createContext<string | null>(null);

export function useBoardViewerId(): string | null {
  return useContext(BoardViewerContext);
}
