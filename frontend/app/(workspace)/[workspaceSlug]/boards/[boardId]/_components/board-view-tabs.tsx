import Link from "next/link"
import { CalendarDays, FileText, GanttChart, LayoutGrid, Table2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import type { Board, ViewMode } from "@/features/boards/types"

export function BoardViewTabs({
  workspaceSlug,
  board,
  activeView,
}: {
  workspaceSlug: string
  board: Board
  activeView: ViewMode
}) {
  return (
    <div className="mb-5 flex flex-col gap-3 rounded-2xl border border-border bg-card p-3 sm:flex-row sm:items-center sm:justify-between">
      <Tabs value={activeView}>
        <TabsList>
          <TabsTrigger value="table" asChild>
            <Link href={`/${workspaceSlug}/boards/${board.id}` as never}>
              <Table2 className="size-4" />
              Main table
            </Link>
          </TabsTrigger>
          <TabsTrigger value="kanban" asChild>
            <Link href={`/${workspaceSlug}/boards/${board.id}/kanban` as never}>
              <LayoutGrid className="size-4" />
              Kanban
            </Link>
          </TabsTrigger>
          <TabsTrigger value="calendar" asChild>
            <Link href={`/${workspaceSlug}/boards/${board.id}/calendar` as never}>
              <CalendarDays className="size-4" />
              Calendar
            </Link>
          </TabsTrigger>
          <TabsTrigger value="timeline" asChild>
            <Link href={`/${workspaceSlug}/boards/${board.id}/timeline` as never}>
              <GanttChart className="size-4" />
              Timeline
            </Link>
          </TabsTrigger>
        </TabsList>
      </Tabs>

      {board.linkedPageId ? (
        <Button variant="outline" className="w-fit bg-card" asChild>
          <Link href={`/${workspaceSlug}/boards/${board.id}?doc=${board.linkedPageId}` as never}>
            <FileText className="size-4" />
            Doc
          </Link>
        </Button>
      ) : null}
    </div>
  )
}
