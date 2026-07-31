import { useMemo, useState } from "react"
import { Grid2X2, List, Paperclip, Search, Upload } from "lucide-react"
import { Badge } from "@notrelix/ui-web"
import { Button } from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"
import { Skeleton } from "@notrelix/ui-web"
import { ToggleGroup, ToggleGroupItem } from "@notrelix/ui-web"
import { useCardFiles } from "@notrelix/work-management-state"
import type { CardDetail, CardFile } from "@notrelix/work-management-core"
import { TaskDetailEmptyState } from "./task-detail-empty-state"

export function TaskFilesTab({ card }: { card: CardDetail }) {
  const { data = [], isLoading } = useCardFiles(card.id)
  const [query, setQuery] = useState("")
  const [mode, setMode] = useState<"grid" | "list">("grid")
  const filteredFiles = useMemo(() => {
    const normalized = query.trim().toLowerCase()
    if (!normalized) return data
    return data.filter((file: CardFile) => file.name.toLowerCase().includes(normalized))
  }, [data, query])

  return (
    <div className="flex flex-col gap-4 p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <Button variant="outline" size="sm" className="w-fit bg-card" disabled>
          <Upload className="size-4" />
          Add file
        </Button>
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              className="h-8 w-56 bg-card pl-8"
              placeholder="Search files"
              aria-label="Search files"
            />
          </div>
          <ToggleGroup type="single" value={mode} onValueChange={(value: any) => value && setMode(value as "grid" | "list")}>
            <ToggleGroupItem value="grid" size="sm" aria-label="Grid view">
              <Grid2X2 className="size-4" />
            </ToggleGroupItem>
            <ToggleGroupItem value="list" size="sm" aria-label="List view">
              <List className="size-4" />
            </ToggleGroupItem>
          </ToggleGroup>
        </div>
      </div>

      <button
        type="button"
        className="flex min-h-28 flex-col items-center justify-center rounded-lg border border-dashed border-border bg-card p-4 text-center text-sm text-muted-foreground transition hover:bg-muted"
        disabled
      >
        <Upload className="mb-2 size-5 text-primary" />
        Drag files here or click to upload
      </button>

      {isLoading ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <Skeleton className="h-24 rounded-lg" />
          <Skeleton className="h-24 rounded-lg" />
        </div>
      ) : filteredFiles.length === 0 ? (
        <TaskDetailEmptyState
          icon={Paperclip}
          title="No files attached"
          description="Attach specs, screenshots, or handoff assets so they stay with the task."
        />
      ) : mode === "grid" ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {filteredFiles.map((file: CardFile) => (
            <div key={file.id} className="rounded-lg border border-border bg-card p-3">
              <Paperclip className="mb-3 size-5 text-primary" />
              <p className="truncate text-sm font-medium text-foreground">{file.name}</p>
              <p className="mt-1 text-xs text-muted-foreground">{formatBytes(file.size)}</p>
              <Badge variant="secondary" className="mt-3 rounded-full">{file.source.toUpperCase()}</Badge>
            </div>
          ))}
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          {filteredFiles.map((file: CardFile) => (
            <div key={file.id} className="flex items-center justify-between gap-3 rounded-lg border border-border bg-card p-3">
              <span className="inline-flex min-w-0 items-center gap-2 text-sm text-foreground">
                <Paperclip className="size-4 text-primary" />
                <span className="truncate">{file.name}</span>
              </span>
              <span className="shrink-0 text-xs text-muted-foreground">{formatBytes(file.size)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${Math.round(value / 1024)} KB`
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}
