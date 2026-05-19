"use client"

import Link from "next/link"
import { Clock3, Copy, MoreHorizontal, Share2, Star } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { useToggleFavorite } from "@/features/docs/hooks/use-favorites"
import type { PageDetail } from "@/features/docs/types"

interface PageToolbarProps {
  page: PageDetail
  workspaceSlug: string
}

export function PageToolbar({ page, workspaceSlug }: PageToolbarProps) {
  const toggleFavorite = useToggleFavorite(page.workspaceId)

  return (
    <div className="sticky top-14 z-30 flex h-14 items-center justify-between border-b border-border bg-card/90 px-4 backdrop-blur-xl">
      <div className="flex min-w-0 items-center gap-2">
        <Link href={`/${workspaceSlug}/docs`} className="text-sm font-medium text-muted-foreground hover:text-foreground">
          Docs
        </Link>
        <Separator orientation="vertical" className="h-5" />
        <span className="min-w-0 truncate text-sm font-semibold text-foreground">{page.icon} {page.title}</span>
      </div>
      <div className="flex items-center gap-1">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => toggleFavorite.mutate({ pageId: page.id, isFavorited: !page.isFavorited })}
              aria-label={page.isFavorited ? "Remove favorite" : "Favorite page"}
            >
              <Star className={page.isFavorited ? "size-4 fill-amber-500 text-amber-500" : "size-4"} />
            </Button>
          </TooltipTrigger>
          <TooltipContent>Favorite</TooltipContent>
        </Tooltip>
        <Button variant="outline" size="sm" className="hidden bg-card sm:inline-flex" onClick={() => toast.success("Share link copied")}>
          <Share2 className="size-4" />
          Share
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" aria-label="More page actions">
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => navigator.clipboard?.writeText(window.location.href)}>
              <Copy className="size-4" /> Copy link
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link href={`/${workspaceSlug}/docs/${page.id}/history`}>
                <Clock3 className="size-4" /> Version history
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  )
}
