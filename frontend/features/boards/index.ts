// Legacy compatibility layer.
// Implementation moved to "../work-management".
// New code must import from "@/features/work-management".
// Do not add implementation here.
// Do not use export-all.

export { BoardScreen } from "../work-management"
export { useFullBoard } from "../work-management"
export { useResolvedWorkspaceBoard } from "../work-management"
export { useWorkspaceBoards } from "../work-management"
export { generatePosition } from "../work-management"

export type { Board, Card, BoardGroup, CardDetail } from "../work-management"
