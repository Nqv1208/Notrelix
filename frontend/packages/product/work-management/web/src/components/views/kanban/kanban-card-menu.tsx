"use client"

import { Copy, CopyCheck, Trash2 } from "lucide-react"
import { toast } from "sonner"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web"

export function KanbanCardMenu({
  cardId,
  onDuplicate,
  onDelete,
  children,
}: {
  cardId: string
  onDuplicate: () => void
  onDelete: () => void
  children: React.ReactNode
}) {
  const handleCopyLink = () => {
    const link = `${window.location.origin}${window.location.pathname}?taskId=${cardId}`
    void navigator.clipboard.writeText(link)
    toast.success("Card link copied to clipboard.")
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {children}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        <DropdownMenuItem onClick={handleCopyLink}>
          <Copy className="mr-2 size-4 text-muted-foreground" />
          Copy link
        </DropdownMenuItem>
        <DropdownMenuItem onClick={onDuplicate}>
          <CopyCheck className="mr-2 size-4 text-muted-foreground" />
          Duplicate card
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="text-destructive focus:bg-destructive/10 focus:text-destructive" onClick={onDelete}>
          <Trash2 className="mr-2 size-4" />
          Archive card
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
