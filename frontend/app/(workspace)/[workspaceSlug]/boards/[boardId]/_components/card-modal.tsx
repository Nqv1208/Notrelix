"use client"

import { useRouter } from "next/navigation"
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog"
import { CardDetail } from "./card-detail"

export function CardModal({ workspaceSlug, boardId, cardId }: { workspaceSlug: string; boardId: string; cardId: string }) {
  const router = useRouter()

  return (
    <Dialog open onOpenChange={(open) => !open && router.back()}>
      <DialogContent className="max-h-[92vh] max-w-[1120px] overflow-hidden p-0" showCloseButton>
        <DialogTitle className="sr-only">Card detail</DialogTitle>
        <CardDetail workspaceSlug={workspaceSlug} boardId={boardId} cardId={cardId} mode="modal" />
      </DialogContent>
    </Dialog>
  )
}
