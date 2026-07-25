import { useEffect, useRef } from "react"
import { AnimatePresence, motion } from "framer-motion"
import { AlertCircle } from "lucide-react"
import { Button } from "@notrelix/ui-web"
import { Skeleton } from "@notrelix/ui-web"
import {
  Sheet,
  SheetContent,
  SheetTitle,
} from "@notrelix/ui-web"
import { useCard } from "@notrelix/work-management-state"
import type { Board, CardDetail } from "@notrelix/work-management-core"
import { useIsMobile } from "@notrelix/ui-web"
import { TaskDetailHeader } from "./task-detail-header"
import { TaskDetailTabs } from "./task-detail-tabs"

export function TaskDetailPanel({
  board,
  cardId,
  open,
  onClose,
  onExitComplete,
}: {
  board: Board
  cardId: string | null
  open: boolean
  onClose: () => void
  onExitComplete?: () => void
}) {
  const isMobile = useIsMobile()
  const content = cardId ? <TaskDetailPanelContent board={board} cardId={cardId} onClose={onClose} /> : null

  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  })

  useEffect(() => {
    if (!open || isMobile) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") onCloseRef.current()
    }

    window.addEventListener("keydown", handleKeyDown)
    return () => window.removeEventListener("keydown", handleKeyDown)
  }, [isMobile, open])

  if (isMobile) {
    return (
      <Sheet open={open} onOpenChange={(nextOpen: any) => {
        if (!nextOpen) onClose()
      }}>
        <SheetContent side="right" className="w-full gap-0 p-0 sm:max-w-none" showCloseButton={false}>
          <SheetTitle className="sr-only">Task details</SheetTitle>
          {content}
        </SheetContent>
      </Sheet>
    )
  }

  return (
    <AnimatePresence mode="wait" onExitComplete={onExitComplete}>
      {open && cardId ? (
        <motion.aside
          key={cardId}
          initial={{ x: 40, opacity: 0 }}
          animate={{ x: 0, opacity: 1 }}
          exit={{ x: 40, opacity: 0 }}
          transition={{ duration: 0.18, ease: [0.2, 0, 0, 1] }}
          className="flex h-full min-h-0 w-full min-w-0 overflow-hidden border-l border-border bg-popover"
          aria-label="Task detail panel"
        >
          {content}
        </motion.aside>
      ) : null}
    </AnimatePresence>
  )
}

function TaskDetailPanelContent({
  board,
  cardId,
  onClose,
}: {
  board: Board
  cardId: string
  onClose: () => void
}) {
  const { card, isLoading, error } = useCard(cardId)

  if (isLoading) return <TaskDetailPanelSkeleton />
  if (error || !card) return <TaskDetailPanelError onClose={onClose} />

  return <TaskDetailBody board={board} card={card as CardDetail} onClose={onClose} />
}

function TaskDetailBody({ board, card, onClose }: { board: Board; card: CardDetail; onClose: () => void }) {
  return (
    <div className="flex h-full min-h-0 w-full min-w-0 flex-col overflow-hidden bg-popover">
      <TaskDetailHeader key={card.id} board={board} card={card} onClose={onClose} />
      <TaskDetailTabs card={card} />
    </div>
  )
}

function TaskDetailPanelSkeleton() {
  return (
    <div className="flex h-full w-full flex-col gap-4 bg-popover p-4">
      <Skeleton className="h-10 rounded-lg" />
      <Skeleton className="h-20 rounded-lg" />
      <Skeleton className="h-9 rounded-lg" />
      <Skeleton className="h-40 rounded-lg" />
    </div>
  )
}

function TaskDetailPanelError({ onClose }: { onClose: () => void }) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center bg-popover p-6 text-center">
      <AlertCircle className="mb-3 size-8 text-destructive" />
      <h2 className="text-sm font-semibold text-foreground">Task unavailable</h2>
      <p className="mt-2 max-w-xs text-sm text-muted-foreground">This task could not be loaded or no longer exists.</p>
      <Button type="button" variant="outline" size="sm" className="mt-4 bg-card" onClick={onClose}>
        Close panel
      </Button>
    </div>
  )
}
