import { useEffect } from "react"
import { Sheet, SheetContent } from "@notrelix/ui-web"
import type { Board, Card } from "@notrelix/work-management-core"
import { TaskDetailHeader } from "../../card-detail/task-detail-header"
import { TaskDetailTabs } from "../../card-detail/task-detail-tabs"

interface KanbanCardDetailPanelProps {
  board: Board
  card: any | null
  workspaceId: string
  onClose: () => void
}

export function KanbanCardDetailPanel({
  board,
  card,
  workspaceId,
  onClose,
}: KanbanCardDetailPanelProps) {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose()
    }
    document.addEventListener("keydown", handleEscape)
    return () => document.removeEventListener("keydown", handleEscape)
  }, [onClose])

  if (!card) return null

  return (
    <Sheet open={!!card} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
        <TaskDetailHeader card={card} board={board}  onClose={() => {}} />
        <TaskDetailTabs card={card} />
      </SheetContent>
    </Sheet>
  )
}
