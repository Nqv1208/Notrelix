// Public API of features/work-management.
// Refactored and aligned under the final modular target architecture.
// Restructured to narrow down public API exports to only boundaries.

// 1. Screen / Shell components for App Router composition
export { BoardWorkspaceViewContent as BoardScreen } from "./boards/components/board-workspace-view-content"
export { BoardWorkspaceViewContent } from "./boards/components/board-workspace-view-content"

// 2. Public hooks needed at app / composition boundary
export { useFullBoard } from "./boards/hooks/queries/use-full-board"
export { useWorkspaceBoards } from "./boards/hooks/queries/use-workspace-boards"
export { useResolvedWorkspaceBoard } from "./boards/hooks/queries/use-resolved-workspace-board"

// 3. Public types used at boundaries
export type {
  Board,
  Card,
  BoardGroup,
  CardDetail,
  BoardMember
} from "./types"

// 4. Shared Utils / Helpers (needed for legacy compatibility)
export { generatePosition } from "./shared/utils/fractional-index"
